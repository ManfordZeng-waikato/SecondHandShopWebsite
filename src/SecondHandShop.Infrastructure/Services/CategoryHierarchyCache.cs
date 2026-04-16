using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Infrastructure.Persistence;

namespace SecondHandShop.Infrastructure.Services;

internal sealed class CategoryHierarchyCache(
    SecondHandShopDbContext dbContext,
    IMemoryCache memoryCache) : ICategoryHierarchyCache
{
    private const string CacheKey = "category-hierarchy-v1";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private static readonly SemaphoreSlim PopulateLock = new(1, 1);

    public async Task<CategoryHierarchySnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        if (memoryCache.TryGetValue<CategoryHierarchySnapshot>(CacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        await PopulateLock.WaitAsync(cancellationToken);

        try
        {
            if (memoryCache.TryGetValue<CategoryHierarchySnapshot>(CacheKey, out cached) && cached is not null)
            {
                return cached;
            }

            var nodes = await dbContext.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .Select(c => new CategoryHierarchyNode(c.Id, c.Slug, c.ParentId))
                .ToListAsync(cancellationToken);

            var snapshot = Build(nodes);
            memoryCache.Set(CacheKey, snapshot, CacheDuration);
            return snapshot;
        }
        finally
        {
            PopulateLock.Release();
        }
    }

    public void Invalidate() => memoryCache.Remove(CacheKey);

    private static CategoryHierarchySnapshot Build(IReadOnlyList<CategoryHierarchyNode> nodes)
    {
        var byId = new Dictionary<Guid, CategoryHierarchyNode>(nodes.Count);
        var bySlug = new Dictionary<string, CategoryHierarchyNode>(nodes.Count);
        foreach (var node in nodes)
        {
            byId[node.Id] = node;
            bySlug[node.Slug] = node;
        }

        var childIdsByParent = new Dictionary<Guid, List<Guid>>();
        foreach (var node in nodes)
        {
            if (!node.ParentId.HasValue)
            {
                continue;
            }

            if (!childIdsByParent.TryGetValue(node.ParentId.Value, out var childIds))
            {
                childIds = new List<Guid>();
                childIdsByParent[node.ParentId.Value] = childIds;
            }
            childIds.Add(node.Id);
        }

        var descendantsById = new Dictionary<Guid, IReadOnlyList<Guid>>(nodes.Count);
        var queue = new Queue<Guid>();
        foreach (var node in nodes)
        {
            var descendants = new List<Guid>();
            queue.Clear();
            queue.Enqueue(node.Id);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                descendants.Add(current);
                if (childIdsByParent.TryGetValue(current, out var childIds))
                {
                    foreach (var childId in childIds)
                    {
                        queue.Enqueue(childId);
                    }
                }
            }
            descendantsById[node.Id] = descendants;
        }

        return new CategoryHierarchySnapshot(nodes, byId, bySlug, descendantsById);
    }
}
