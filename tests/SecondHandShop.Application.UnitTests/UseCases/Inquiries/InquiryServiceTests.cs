using FluentAssertions;
using Moq;
using SecondHandShop.Application.Abstractions.Common;
using SecondHandShop.Application.Abstractions.Messaging;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Abstractions.Security;
using SecondHandShop.Application.Contracts.Inquiries;
using SecondHandShop.Application.UseCases.Customers;
using SecondHandShop.Application.UseCases.Inquiries;
using SecondHandShop.Domain.Common;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;
using SecondHandShop.TestCommon.Time;

namespace SecondHandShop.Application.UnitTests.UseCases.Inquiries;

public class InquiryServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 4, 16, 1, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateInquiryAsync_ShouldPersistInquiry_AndNotifyDispatcher_WhenRequestIsValid()
    {
        var category = CreateCategory(isActive: true);
        var product = CreateProduct(category.Id);
        var customer = Customer.Create("Alice", "alice@example.com", "021 000 000", CustomerSource.Inquiry, UtcNow);

        var productRepository = new Mock<IProductRepository>();
        productRepository
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var categoryRepository = new Mock<ICategoryRepository>();
        categoryRepository
            .Setup(x => x.GetByIdAsync(product.CategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var inquiryRepository = new Mock<IInquiryRepository>();
        inquiryRepository
            .Setup(x => x.CountRecentByIpAndProductAsync(It.IsAny<string>(), product.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        inquiryRepository
            .Setup(x => x.CountRecentByEmailAndProductAsync("alice@example.com", product.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        inquiryRepository
            .Setup(x => x.ExistsRecentByMessageHashAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        inquiryRepository
            .Setup(x => x.GetIpCooldownUntilAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTime?)null);

        Domain.Entities.Inquiry? savedInquiry = null;
        inquiryRepository
            .Setup(x => x.AddAsync(It.IsAny<Domain.Entities.Inquiry>(), It.IsAny<CancellationToken>()))
            .Callback<Domain.Entities.Inquiry, CancellationToken>((inquiry, _) => savedInquiry = inquiry)
            .Returns(Task.CompletedTask);

        var customerResolutionService = new Mock<ICustomerResolutionService>();
        customerResolutionService
            .Setup(x => x.GetOrCreateCustomerAsync(
                "Alice",
                "alice@example.com",
                "021 000 000",
                CustomerSource.Inquiry,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var turnstileValidator = new Mock<ITurnstileValidator>();
        turnstileValidator
            .Setup(x => x.ValidateAsync("turnstile-ok", "127.0.0.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TurnstileValidationResult { IsSuccess = true });

        var dispatchSignal = new Mock<IInquiryDispatchSignal>();
        var transaction = new Mock<IDatabaseTransaction>();
        transaction
            .Setup(x => x.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);
        unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var clock = new FakeClock(UtcNow);

        var sut = new InquiryService(
            productRepository.Object,
            categoryRepository.Object,
            inquiryRepository.Object,
            customerResolutionService.Object,
            turnstileValidator.Object,
            dispatchSignal.Object,
            unitOfWork.Object,
            clock);

        var inquiryId = await sut.CreateInquiryAsync(new CreateInquiryCommand(
            product.Id,
            " Alice ",
            "Alice@Example.com",
            "021 000 000",
            "Is this still available?",
            "turnstile-ok",
            "127.0.0.1"));

        inquiryId.Should().NotBeEmpty();
        savedInquiry.Should().NotBeNull();
        savedInquiry!.CustomerId.Should().Be(customer.Id);
        dispatchSignal.Verify(x => x.Notify(), Times.Once);
        transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateInquiryAsync_ShouldRejectUnavailableProducts()
    {
        var category = CreateCategory(isActive: true);
        var product = CreateProduct(category.Id);
        product.OffShelf(updatedByAdminUserId: null, utcNow: UtcNow);

        var productRepository = new Mock<IProductRepository>();
        productRepository
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var categoryRepository = new Mock<ICategoryRepository>();
        var inquiryRepository = new Mock<IInquiryRepository>();
        var customerResolutionService = new Mock<ICustomerResolutionService>();
        var turnstileValidator = new Mock<ITurnstileValidator>();
        turnstileValidator
            .Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TurnstileValidationResult { IsSuccess = true });

        var sut = new InquiryService(
            productRepository.Object,
            categoryRepository.Object,
            inquiryRepository.Object,
            customerResolutionService.Object,
            turnstileValidator.Object,
            Mock.Of<IInquiryDispatchSignal>(),
            Mock.Of<IUnitOfWork>(),
            new FakeClock(UtcNow));

        var act = () => sut.CreateInquiryAsync(new CreateInquiryCommand(
            product.Id,
            "Alice",
            "alice@example.com",
            null,
            "Still available?",
            "turnstile-ok",
            "127.0.0.1"));

        await act.Should().ThrowAsync<DomainRuleViolationException>()
            .WithMessage("Inquiries can only be submitted for available products.");
    }

    [Fact]
    public async Task CreateInquiryAsync_ShouldBlockIp_AndPersistCooldown_WhenAntiSpamLimitIsExceeded()
    {
        var category = CreateCategory(isActive: true);
        var product = CreateProduct(category.Id);

        var productRepository = new Mock<IProductRepository>();
        productRepository
            .Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var categoryRepository = new Mock<ICategoryRepository>();
        categoryRepository
            .Setup(x => x.GetByIdAsync(product.CategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var inquiryRepository = new Mock<IInquiryRepository>();
        inquiryRepository
            .Setup(x => x.GetIpCooldownUntilAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTime?)null);
        inquiryRepository
            .Setup(x => x.CountRecentByIpAndProductAsync("127.0.0.1", product.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var turnstileValidator = new Mock<ITurnstileValidator>();
        turnstileValidator
            .Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TurnstileValidationResult { IsSuccess = true });

        var transaction = new Mock<IDatabaseTransaction>();
        transaction
            .Setup(x => x.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);
        unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = new InquiryService(
            productRepository.Object,
            categoryRepository.Object,
            inquiryRepository.Object,
            Mock.Of<ICustomerResolutionService>(),
            turnstileValidator.Object,
            Mock.Of<IInquiryDispatchSignal>(),
            unitOfWork.Object,
            new FakeClock(UtcNow));

        var act = () => sut.CreateInquiryAsync(new CreateInquiryCommand(
            product.Id,
            "Alice",
            "alice@example.com",
            null,
            "Still available?",
            "turnstile-ok",
            "127.0.0.1"));

        await act.Should().ThrowAsync<InquiryRateLimitExceededException>()
            .WithMessage("*temporarily blocked for 5 minutes*");

        inquiryRepository.Verify(
            x => x.UpsertIpCooldownAsync("127.0.0.1", UtcNow.AddMinutes(5), UtcNow, It.IsAny<CancellationToken>()),
            Times.Once);
        transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Product CreateProduct(Guid categoryId)
    {
        return Product.Create(
            "Vintage leather bag",
            "vintage-leather-bag",
            "Soft grain leather bag.",
            220m,
            categoryId,
            Guid.NewGuid(),
            UtcNow,
            ProductCondition.Good);
    }

    private static Category CreateCategory(bool isActive)
    {
        return Category.Create("Bags", "bags", null, 1, isActive, null, UtcNow);
    }

}
