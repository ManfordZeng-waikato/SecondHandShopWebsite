namespace SecondHandShop.Application.UseCases.Categories;

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string Slug,
    List<CategoryDto> Children);
