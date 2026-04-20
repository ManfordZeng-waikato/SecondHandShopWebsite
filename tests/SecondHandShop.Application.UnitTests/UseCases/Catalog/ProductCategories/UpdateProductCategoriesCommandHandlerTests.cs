using FluentAssertions;
using Moq;
using SecondHandShop.Application.Abstractions.Common;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Common.Exceptions;
using SecondHandShop.Application.UseCases.Catalog.ProductCategories;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;
using SecondHandShop.TestCommon.Time;

namespace SecondHandShop.Application.UnitTests.UseCases.Catalog.ProductCategories;

public class UpdateProductCategoriesCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 4, 16, 1, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_ShouldAddMainCategory_DeduplicateIds_AndFilterEmptyGuid()
    {
        var mainCategoryId = Guid.NewGuid();
        var extraCategoryId = Guid.NewGuid();
        var product = CreateProduct(Guid.NewGuid());

        var productRepository = new Mock<IProductRepository>();
        productRepository
            .Setup(x => x.GetByIdWithCategoriesAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var categoryRepository = new Mock<ICategoryRepository>();
        categoryRepository
            .Setup(x => x.ListByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<Guid> ids, CancellationToken _) =>
                ids.Select(id => Category.Create($"Category-{id}", $"slug-{id:N}", null, 1, true, null, UtcNow)).ToList());

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = new UpdateProductCategoriesCommandHandler(
            productRepository.Object,
            categoryRepository.Object,
            unitOfWork.Object,
            new FakeClock(UtcNow));

        var result = await sut.Handle(new UpdateProductCategoriesCommand(
            product.Id,
            mainCategoryId,
            [Guid.Empty, mainCategoryId, extraCategoryId, extraCategoryId],
            Guid.NewGuid()), CancellationToken.None);

        result.MainCategoryId.Should().Be(mainCategoryId);
        result.SelectedCategoryIds.Should().BeEquivalentTo([mainCategoryId, extraCategoryId]);
        product.ProductCategories.Select(x => x.CategoryId).Should().BeEquivalentTo([mainCategoryId, extraCategoryId]);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenAnyCategoryIdIsInvalid()
    {
        var mainCategoryId = Guid.NewGuid();
        var invalidCategoryId = Guid.NewGuid();
        var product = CreateProduct(Guid.NewGuid());

        var productRepository = new Mock<IProductRepository>();
        productRepository
            .Setup(x => x.GetByIdWithCategoriesAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var categoryRepository = new Mock<ICategoryRepository>();
        categoryRepository
            .Setup(x => x.ListByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([Category.Create("Main", "main", null, 1, true, null, UtcNow)]);

        var sut = new UpdateProductCategoriesCommandHandler(
            productRepository.Object,
            categoryRepository.Object,
            Mock.Of<IUnitOfWork>(),
            new FakeClock(UtcNow));

        var act = () => sut.Handle(new UpdateProductCategoriesCommand(
            product.Id,
            mainCategoryId,
            [invalidCategoryId],
            Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage($"One or more category ids are invalid: {invalidCategoryId}, {mainCategoryId}");
    }

    [Fact]
    public async Task Handle_ShouldRemoveOldCategories_AndAddNewOnes()
    {
        var oldCategoryId = Guid.NewGuid();
        var mainCategoryId = Guid.NewGuid();
        var newCategoryId = Guid.NewGuid();
        var product = CreateProduct(oldCategoryId);
        product.ProductCategories.Add(ProductCategory.Create(product.Id, oldCategoryId));

        var productRepository = new Mock<IProductRepository>();
        productRepository
            .Setup(x => x.GetByIdWithCategoriesAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var categoryRepository = new Mock<ICategoryRepository>();
        categoryRepository
            .Setup(x => x.ListByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<Guid> ids, CancellationToken _) =>
                ids.Select(id => Category.Create($"Category-{id}", $"slug-{id:N}", null, 1, true, null, UtcNow)).ToList());

        var sut = new UpdateProductCategoriesCommandHandler(
            productRepository.Object,
            categoryRepository.Object,
            Mock.Of<IUnitOfWork>(),
            new FakeClock(UtcNow));

        var result = await sut.Handle(new UpdateProductCategoriesCommand(
            product.Id,
            mainCategoryId,
            [newCategoryId],
            Guid.NewGuid()), CancellationToken.None);

        result.SelectedCategoryIds.Should().BeEquivalentTo([mainCategoryId, newCategoryId]);
        product.ProductCategories.Select(x => x.CategoryId).Should().BeEquivalentTo([mainCategoryId, newCategoryId]);
        product.ProductCategories.Select(x => x.CategoryId).Should().NotContain(oldCategoryId);
    }

    private static Product CreateProduct(Guid categoryId)
    {
        return Product.Create(
            "Vintage bag",
            "vintage-bag",
            "Soft grain leather bag.",
            250m,
            categoryId,
            Guid.NewGuid(),
            UtcNow,
            ProductCondition.Good);
    }

}
