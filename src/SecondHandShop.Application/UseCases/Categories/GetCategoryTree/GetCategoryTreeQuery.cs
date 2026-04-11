using MediatR;
using SecondHandShop.Application.UseCases.Categories;

namespace SecondHandShop.Application.UseCases.Categories.GetCategoryTree;

public sealed record GetCategoryTreeQuery() : IRequest<IReadOnlyList<CategoryDto>>;
