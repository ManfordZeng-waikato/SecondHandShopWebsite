using FluentAssertions;
using Moq;
using SecondHandShop.Application.Abstractions.Common;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Common.Exceptions;
using SecondHandShop.Application.UseCases.Customers;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Application.UnitTests.UseCases.Customers;

public class CustomerResolutionServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 4, 16, 1, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetOrCreateCustomerAsync_ShouldMergeIntoExistingCustomer_WhenEmailMatches()
    {
        var existing = Customer.Create("Alice", "alice@example.com", null, CustomerSource.Inquiry, UtcNow);
        var repository = new Mock<ICustomerRepository>();
        repository
            .Setup(x => x.GetByEmailAsync("alice@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var sut = new CustomerResolutionService(repository.Object, new StubClock(UtcNow.AddMinutes(5)));

        var result = await sut.GetOrCreateCustomerAsync(
            " Alice Updated ",
            "Alice@Example.com",
            "021 123 4567",
            CustomerSource.Sale);

        result.Should().BeSameAs(existing);
        result.Name.Should().Be("Alice");
        result.Email.Should().Be("alice@example.com");
        result.PhoneNumber.Should().Be("021 123 4567");
        repository.Verify(x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetOrCreateCustomerAsync_ShouldThrowConflict_WhenEmailAndPhoneMapToDifferentCustomers()
    {
        var repository = new Mock<ICustomerRepository>();
        repository
            .Setup(x => x.GetByEmailAsync("alice@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Customer.Create("Alice", "alice@example.com", null, CustomerSource.Inquiry, UtcNow));
        repository
            .Setup(x => x.GetByPhoneNumberAsync("021 123 4567", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Customer.Create("Bob", null, "021 123 4567", CustomerSource.Sale, UtcNow));

        var sut = new CustomerResolutionService(repository.Object, new StubClock(UtcNow));

        var act = () => sut.GetOrCreateCustomerAsync(
            "Conflict",
            "alice@example.com",
            "021 123 4567",
            CustomerSource.Sale);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Email and phone number belong to different customers.");
    }

    [Fact]
    public async Task GetOrCreateCustomerAsync_ShouldCreateCustomer_WhenNoMatchExists()
    {
        var repository = new Mock<ICustomerRepository>();
        Customer? added = null;
        repository
            .Setup(x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Callback<Customer, CancellationToken>((customer, _) => added = customer)
            .Returns(Task.CompletedTask);

        var sut = new CustomerResolutionService(repository.Object, new StubClock(UtcNow));

        var result = await sut.GetOrCreateCustomerAsync(
            "Alice",
            "Alice@Example.com",
            null,
            CustomerSource.Sale);

        result.PrimarySource.Should().Be(CustomerSource.Sale);
        result.Email.Should().Be("alice@example.com");
        added.Should().BeSameAs(result);
    }

    private sealed class StubClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
