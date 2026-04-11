using MediatR;

namespace SecondHandShop.Application.UseCases.Catalog.ProductCategories;

public sealed record GetProductCategorySelectionQuery(Guid ProductId) : IRequest<ProductCategorySelectionDto>;
