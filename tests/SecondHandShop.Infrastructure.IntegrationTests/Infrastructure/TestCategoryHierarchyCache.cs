using Microsoft.EntityFrameworkCore;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Infrastructure.Persistence;

namespace SecondHandShop.Infrastructure.IntegrationTests.Infrastructure;

/// <summary>
/// Non-caching implementation of <see cref="ICategoryHierarchyCache"/> for integration tests.
/// Always hits the database so tests see the latest state without cache invalidation concerns.
/// </summary>
internal sealed class TestCategoryHierarchyCache(SecondHandShopDbContext dbContext) : ICategoryHierarchyCache
{
    public async Task<CategoryHierarchySnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        var nodes = await dbContext.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => new CategoryHierarchyNode(c.Id, c.Slug, c.ParentId))
            .ToListAsync(cancellationToken);

        return Build(nodes);
    }

    public void Invalidate() { }

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
            if (!node.ParentId.HasValue) continue;
            if (!childIdsByParent.TryGetValue(node.ParentId.Value, out var list))
            {
                list = [];
                childIdsByParent[node.ParentId.Value] = list;
            }
            list.Add(node.Id);
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
                if (childIdsByParent.TryGetValue(current, out var children))
                    foreach (var childId in children)
                        queue.Enqueue(childId);
            }
            descendantsById[node.Id] = descendants;
        }

        return new CategoryHierarchySnapshot(nodes, byId, bySlug, descendantsById);
    }
}
