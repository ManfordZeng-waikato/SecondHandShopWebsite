namespace SecondHandShop.Application.Abstractions.Persistence;

public sealed record CategoryHierarchyNode(Guid Id, string Slug, Guid? ParentId);

public sealed class CategoryHierarchySnapshot
{
    public CategoryHierarchySnapshot(
        IReadOnlyList<CategoryHierarchyNode> nodes,
        IReadOnlyDictionary<Guid, CategoryHierarchyNode> byId,
        IReadOnlyDictionary<string, CategoryHierarchyNode> bySlug,
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> descendantsById)
    {
        Nodes = nodes;
        ById = byId;
        BySlug = bySlug;
        DescendantsById = descendantsById;
    }

    public IReadOnlyList<CategoryHierarchyNode> Nodes { get; }
    public IReadOnlyDictionary<Guid, CategoryHierarchyNode> ById { get; }
    public IReadOnlyDictionary<string, CategoryHierarchyNode> BySlug { get; }
    public IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> DescendantsById { get; }

    public CategoryHierarchyNode? FindBySlug(string slug) =>
        BySlug.TryGetValue(slug, out var node) ? node : null;

    public IReadOnlyList<Guid> GetDescendantIds(Guid categoryId) =>
        DescendantsById.TryGetValue(categoryId, out var ids) ? ids : Array.Empty<Guid>();
}

public interface ICategoryHierarchyCache
{
    Task<CategoryHierarchySnapshot> GetAsync(CancellationToken cancellationToken = default);
    void Invalidate();
}
