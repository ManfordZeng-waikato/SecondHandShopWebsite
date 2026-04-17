using FluentAssertions;
using Moq;
using SecondHandShop.Application.Abstractions.Common;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Common.Exceptions;
using SecondHandShop.Application.Contracts.Sales;
using SecondHandShop.Application.UseCases.Customers;
using SecondHandShop.Application.UseCases.Sales;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Application.UnitTests.UseCases.Sales;

public class AdminSaleServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 4, 16, 1, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task MarkAsSoldAsync_ShouldResolveCustomerFromBuyerContact_WhenCustomerIdIsMissing()
    {
        var product = CreateProduct();
        var resolvedCustomer = Customer.Create("Alice", "alice@example.com", "021 123 4567", CustomerSource.Sale, UtcNow);
        ProductSale? addedSale = null;

        var productRepository = new Mock<IProductRepository>();
        productRepository
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var productSaleRepository = new Mock<IProductSaleRepository>();
        productSaleRepository
            .Setup(x => x.AddAsync(It.IsAny<ProductSale>(), It.IsAny<CancellationToken>()))
            .Callback<ProductSale, CancellationToken>((sale, _) => addedSale = sale)
            .Returns(Task.CompletedTask);

        var customerResolutionService = new Mock<ICustomerResolutionService>();
        customerResolutionService
            .Setup(x => x.GetOrCreateCustomerAsync(
                " Alice ",
                "Alice@Example.com",
                "021 123 4567",
                CustomerSource.Sale,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolvedCustomer);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = CreateSut(
            productRepository: productRepository.Object,
            productSaleRepository: productSaleRepository.Object,
            customerResolutionService: customerResolutionService.Object,
            unitOfWork: unitOfWork.Object);

        var result = await sut.MarkAsSoldAsync(new MarkProductSoldRequest(
            product.Id,
            180m,
            UtcNow.AddMinutes(-15),
            Guid.NewGuid(),
            BuyerName: " Alice ",
            BuyerPhone: "021 123 4567",
            BuyerEmail: "Alice@Example.com",
            PaymentMethod: nameof(PaymentMethod.Cash),
            Notes: "  Paid in cash  "));

        addedSale.Should().NotBeNull();
        addedSale!.CustomerId.Should().Be(resolvedCustomer.Id);
        addedSale.BuyerName.Should().Be("Alice");
        addedSale.BuyerEmail.Should().Be("Alice@Example.com");
        addedSale.PaymentMethod.Should().Be(PaymentMethod.Cash);
        addedSale.Notes.Should().Be("Paid in cash");
        product.Status.Should().Be(ProductStatus.Sold);
        result.CustomerId.Should().Be(resolvedCustomer.Id);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkAsSoldAsync_ShouldRejectInquiryBelongingToAnotherProduct()
    {
        var product = CreateProduct();
        var inquiry = Inquiry.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Alice",
            "alice@example.com",
            null,
            "127.0.0.1",
            "hash-123",
            "Still available?",
            UtcNow);

        var productRepository = new Mock<IProductRepository>();
        productRepository
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var inquiryRepository = new Mock<IInquiryRepository>();
        inquiryRepository
            .Setup(x => x.GetByIdAsync(inquiry.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inquiry);

        var sut = CreateSut(
            productRepository: productRepository.Object,
            inquiryRepository: inquiryRepository.Object);

        var act = () => sut.MarkAsSoldAsync(new MarkProductSoldRequest(
            product.Id,
            180m,
            UtcNow.AddMinutes(-10),
            Guid.NewGuid(),
            InquiryId: inquiry.Id));

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("The selected inquiry does not belong to this product.");
    }

    [Fact]
    public async Task MarkAsSoldAsync_ShouldRejectUnsupportedPaymentMethod()
    {
        var product = CreateProduct();

        var productRepository = new Mock<IProductRepository>();
        productRepository
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var sut = CreateSut(productRepository: productRepository.Object);

        var act = () => sut.MarkAsSoldAsync(new MarkProductSoldRequest(
            product.Id,
            180m,
            UtcNow.AddMinutes(-10),
            Guid.NewGuid(),
            PaymentMethod: "barter"));

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("Unsupported payment method 'barter'.");
    }

    [Fact]
    public async Task RevertSaleAsync_ShouldCancelCurrentSale_AndPersistChanges()
    {
        var product = CreateProduct();
        var currentSale = product.MarkAsSold(
            finalSoldPrice: 180m,
            soldAtUtc: UtcNow.AddMinutes(-30),
            adminUserId: Guid.NewGuid(),
            utcNow: UtcNow,
            buyerEmail: "buyer@example.com");

        var productRepository = new Mock<IProductRepository>();
        productRepository
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var productSaleRepository = new Mock<IProductSaleRepository>();
        productSaleRepository
            .Setup(x => x.GetByIdAsync(currentSale.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentSale);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = CreateSut(
            productRepository: productRepository.Object,
            productSaleRepository: productSaleRepository.Object,
            unitOfWork: unitOfWork.Object);

        await sut.RevertSaleAsync(new RevertProductSaleRequest(
            product.Id,
            SaleCancellationReason.BuyerBackedOut,
            "  Buyer cancelled after checkout  ",
            Guid.NewGuid()));

        product.Status.Should().Be(ProductStatus.Available);
        product.CurrentSaleId.Should().BeNull();
        currentSale.Status.Should().Be(SaleRecordStatus.Cancelled);
        currentSale.CancellationReason.Should().Be(SaleCancellationReason.BuyerBackedOut);
        currentSale.CancellationNote.Should().Be("Buyer cancelled after checkout");
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevertSaleAsync_ShouldRejectProductsThatAreNotCurrentlySold()
    {
        var product = CreateProduct();

        var productRepository = new Mock<IProductRepository>();
        productRepository
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var sut = CreateSut(productRepository: productRepository.Object);

        var act = () => sut.RevertSaleAsync(new RevertProductSaleRequest(
            product.Id,
            SaleCancellationReason.AdminMistake,
            null,
            Guid.NewGuid()));

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Only sold products can be reverted to available.");
    }

    private static AdminSaleService CreateSut(
        IProductRepository? productRepository = null,
        IProductSaleRepository? productSaleRepository = null,
        ICustomerRepository? customerRepository = null,
        IInquiryRepository? inquiryRepository = null,
        ICustomerResolutionService? customerResolutionService = null,
        IUnitOfWork? unitOfWork = null)
    {
        return new AdminSaleService(
            productRepository ?? Mock.Of<IProductRepository>(),
            productSaleRepository ?? Mock.Of<IProductSaleRepository>(),
            customerRepository ?? Mock.Of<ICustomerRepository>(),
            inquiryRepository ?? Mock.Of<IInquiryRepository>(),
            customerResolutionService ?? Mock.Of<ICustomerResolutionService>(),
            unitOfWork ?? Mock.Of<IUnitOfWork>(),
            new StubClock(UtcNow));
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

    private sealed class StubClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
