namespace SecondHandShop.Application.UseCases.Catalog.ProductCategories;

public sealed record ProductCategorySelectionDto(
    Guid ProductId,
    Guid MainCategoryId,
    IReadOnlyList<Guid> SelectedCategoryIds);
