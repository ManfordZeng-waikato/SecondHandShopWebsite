using FluentAssertions;
using Moq;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.UseCases.Catalog.ProductCategories;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Application.UnitTests.UseCases.Catalog.ProductCategories;

public class GetProductCategorySelectionQueryHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 4, 16, 1, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_ShouldReturnDistinctSortedCategoryIds_IncludingMainCategory()
    {
        var mainCategoryId = Guid.NewGuid();
        var extraCategoryId = Guid.NewGuid();
        var product = Product.Create(
            "Vintage bag",
            "vintage-bag",
            "Soft grain leather bag.",
            250m,
            mainCategoryId,
            Guid.NewGuid(),
            UtcNow,
            ProductCondition.Good);
        product.ProductCategories.Add(ProductCategory.Create(product.Id, extraCategoryId));
        product.ProductCategories.Add(ProductCategory.Create(product.Id, mainCategoryId));

        var repository = new Mock<IProductRepository>();
        repository
            .Setup(x => x.GetByIdWithCategoriesAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var sut = new GetProductCategorySelectionQueryHandler(repository.Object);

        var result = await sut.Handle(new GetProductCategorySelectionQuery(product.Id), CancellationToken.None);

        result.ProductId.Should().Be(product.Id);
        result.MainCategoryId.Should().Be(mainCategoryId);
        result.SelectedCategoryIds.Should().Equal(new[] { extraCategoryId, mainCategoryId }.Order());
    }
}
