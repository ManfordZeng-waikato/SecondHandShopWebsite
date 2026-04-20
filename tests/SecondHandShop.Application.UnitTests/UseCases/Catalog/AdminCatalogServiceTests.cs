using FluentAssertions;
using Moq;
using SecondHandShop.Application.Abstractions.Common;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Abstractions.Storage;
using SecondHandShop.Application.Common.Exceptions;
using SecondHandShop.Application.UseCases.Catalog;
using SecondHandShop.Domain.Common;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;
using SecondHandShop.TestCommon.Time;

namespace SecondHandShop.Application.UnitTests.UseCases.Catalog;

public class AdminCatalogServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 4, 16, 1, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task UpdateProductStatusAsync_ShouldRestoreOffShelfProductToAvailable()
    {
        var product = CreateProduct();
        product.OffShelf(Guid.NewGuid(), UtcNow.AddMinutes(-5));

        var productRepository = new Mock<IProductRepository>();
        productRepository
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = CreateSut(productRepository: productRepository.Object, unitOfWork: unitOfWork.Object);

        await sut.UpdateProductStatusAsync(product.Id, ProductStatus.Available, Guid.NewGuid());

        product.Status.Should().Be(ProductStatus.Available);
        product.OffShelvedAt.Should().BeNull();
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProductStatusAsync_ShouldRejectDirectTransitionToSold()
    {
        var product = CreateProduct();

        var productRepository = new Mock<IProductRepository>();
        productRepository
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var sut = CreateSut(productRepository: productRepository.Object);

        var act = () => sut.UpdateProductStatusAsync(product.Id, ProductStatus.Sold, Guid.NewGuid());

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Use the mark-sold endpoint to record a sale. Status cannot be set to Sold directly.");
    }

    [Fact]
    public async Task UpdateProductStatusAsync_ShouldRejectRestoringSoldProductWithoutRevertFlow()
    {
        var product = CreateProduct();
        _ = product.MarkAsSold(180m, UtcNow.AddMinutes(-20), Guid.NewGuid(), UtcNow);

        var productRepository = new Mock<IProductRepository>();
        productRepository
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var sut = CreateSut(productRepository: productRepository.Object);

        var act = () => sut.UpdateProductStatusAsync(product.Id, ProductStatus.Available, Guid.NewGuid());

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Sold products must be reverted via the revert-sale endpoint*");
    }

    [Fact]
    public async Task AddProductImageAsync_ShouldRejectObjectKeysFromAnotherProduct()
    {
        var product = CreateProduct();

        var productRepository = new Mock<IProductRepository>();
        productRepository
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var sut = CreateSut(productRepository: productRepository.Object);

        var act = () => sut.AddProductImageAsync(new AddProductImageRequest(
            product.Id,
            $"products/{Guid.NewGuid():N}/photo.webp",
            "Cover",
            0,
            true,
            Guid.NewGuid()));

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("ObjectKey does not belong to the target product.");
    }

    [Fact]
    public async Task AddProductImageAsync_ShouldSyncCoverKeyAndImageCount_WhenPrimaryImageIsAdded()
    {
        var product = CreateProduct();
        var existingImage = ProductImage.Create(
            product.Id,
            $"products/{product.Id:N}/existing.webp",
            "Existing",
            2,
            false,
            Guid.NewGuid(),
            UtcNow.AddMinutes(-10));
        ProductImage? addedImage = null;

        var productRepository = new Mock<IProductRepository>();
        productRepository
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var productImageRepository = new Mock<IProductImageRepository>();
        productImageRepository
            .Setup(x => x.ListByProductIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingImage]);
        productImageRepository
            .Setup(x => x.AddAsync(It.IsAny<ProductImage>(), It.IsAny<CancellationToken>()))
            .Callback<ProductImage, CancellationToken>((image, _) => addedImage = image)
            .Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = CreateSut(
            productRepository: productRepository.Object,
            productImageRepository: productImageRepository.Object,
            unitOfWork: unitOfWork.Object);

        await sut.AddProductImageAsync(new AddProductImageRequest(
            product.Id,
            $"products/{product.Id:N}/new-cover.webp",
            "New cover",
            1,
            true,
            Guid.NewGuid()));

        addedImage.Should().NotBeNull();
        product.CoverImageKey.Should().Be($"products/{product.Id:N}/new-cover.webp");
        product.ImageCount.Should().Be(2);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static AdminCatalogService CreateSut(
        IProductRepository? productRepository = null,
        ICategoryRepository? categoryRepository = null,
        IProductImageRepository? productImageRepository = null,
        IObjectStorageService? objectStorageService = null,
        IUnitOfWork? unitOfWork = null)
    {
        return new AdminCatalogService(
            productRepository ?? Mock.Of<IProductRepository>(),
            categoryRepository ?? Mock.Of<ICategoryRepository>(),
            productImageRepository ?? Mock.Of<IProductImageRepository>(),
            objectStorageService ?? Mock.Of<IObjectStorageService>(),
            unitOfWork ?? Mock.Of<IUnitOfWork>(),
            new FakeClock(UtcNow));
    }

    private static Product CreateProduct()
    {
        return Product.Create(
            "Vintage leather bag",
            "vintage-leather-bag",
            "Soft grain leather bag.",
            220m,
            Guid.NewGuid(),
            Guid.NewGuid(),
            UtcNow,
            ProductCondition.Good);
    }

}
