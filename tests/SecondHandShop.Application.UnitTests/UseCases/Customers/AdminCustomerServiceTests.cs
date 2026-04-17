using FluentAssertions;
using Moq;
using SecondHandShop.Application.Abstractions.Common;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Common.Exceptions;
using SecondHandShop.Application.UseCases.Customers;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Application.UnitTests.UseCases.Customers;

public class AdminCustomerServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 4, 16, 1, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateCustomerAsync_ShouldCreateManualCustomer_WhenRequestIsValid()
    {
        Customer? added = null;
        var repository = new Mock<ICustomerRepository>();
        repository
            .Setup(x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Callback<Customer, CancellationToken>((customer, _) => added = customer)
            .Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = new AdminCustomerService(repository.Object, unitOfWork.Object, new StubClock(UtcNow));

        var id = await sut.CreateCustomerAsync(new CreateCustomerRequest(
            " Alice ",
            "Alice@Example.com",
            "021 123 4567",
            CustomerStatus.New,
            null));

        added.Should().NotBeNull();
        added!.Id.Should().Be(id);
        added.PrimarySource.Should().Be(CustomerSource.Manual);
        added.Email.Should().Be("alice@example.com");
        added.Name.Should().Be("Alice");
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateCustomerAsync_ShouldThrowConflict_WhenEmailAlreadyExists()
    {
        var existing = Customer.Create("Alice", "alice@example.com", null, CustomerSource.Inquiry, UtcNow);
        var repository = new Mock<ICustomerRepository>();
        repository
            .Setup(x => x.GetByEmailAsync("alice@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var sut = new AdminCustomerService(repository.Object, Mock.Of<IUnitOfWork>(), new StubClock(UtcNow));

        var act = () => sut.CreateCustomerAsync(new CreateCustomerRequest(
            "Alice",
            "Alice@Example.com",
            null,
            null,
            null));

        var ex = await act.Should().ThrowAsync<CustomerConflictException>();
        ex.Which.ConflictField.Should().Be("email");
        ex.Which.ExistingCustomerId.Should().Be(existing.Id);
    }

    [Fact]
    public async Task CreateCustomerAsync_ShouldThrowConflict_WhenPhoneAlreadyExists()
    {
        var existing = Customer.Create("Alice", null, "021 123 4567", CustomerSource.Inquiry, UtcNow);
        var repository = new Mock<ICustomerRepository>();
        repository
            .Setup(x => x.GetByPhoneNumberAsync("021 123 4567", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var sut = new AdminCustomerService(repository.Object, Mock.Of<IUnitOfWork>(), new StubClock(UtcNow));

        var act = () => sut.CreateCustomerAsync(new CreateCustomerRequest(
            "Alice",
            null,
            "021 123 4567",
            null,
            null));

        var ex = await act.Should().ThrowAsync<CustomerConflictException>();
        ex.Which.ConflictField.Should().Be("phoneNumber");
        ex.Which.ExistingCustomerId.Should().Be(existing.Id);
    }

    [Fact]
    public async Task CreateCustomerAsync_ShouldApplyStatusAndNotes_WhenProvided()
    {
        Customer? added = null;
        var repository = new Mock<ICustomerRepository>();
        repository
            .Setup(x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Callback<Customer, CancellationToken>((customer, _) => added = customer)
            .Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = new AdminCustomerService(repository.Object, unitOfWork.Object, new StubClock(UtcNow));

        await sut.CreateCustomerAsync(new CreateCustomerRequest(
            "Alice",
            "alice@example.com",
            null,
            CustomerStatus.Contacted,
            "  VIP lead  "));

        added.Should().NotBeNull();
        added!.Status.Should().Be(CustomerStatus.Contacted);
        added.Notes.Should().Be("VIP lead");
    }

    [Fact]
    public async Task UpdateCustomerAsync_ShouldUpdateFields_WhenRequestIsValid()
    {
        var customer = Customer.Create("Alice", "alice@example.com", "021 123 4567", CustomerSource.Inquiry, UtcNow);
        var repository = new Mock<ICustomerRepository>();
        repository
            .Setup(x => x.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        repository
            .Setup(x => x.GetByPhoneNumberAsync("021 765 4321", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = new AdminCustomerService(repository.Object, unitOfWork.Object, new StubClock(UtcNow.AddMinutes(5)));

        await sut.UpdateCustomerAsync(customer.Id, new UpdateCustomerRequest(
            " Alice Updated ",
            "021 765 4321",
            CustomerStatus.Contacted,
            "  Follow up  "));

        customer.Name.Should().Be("Alice Updated");
        customer.PhoneNumber.Should().Be("021 765 4321");
        customer.Status.Should().Be(CustomerStatus.Contacted);
        customer.Notes.Should().Be("Follow up");
    }

    [Fact]
    public async Task UpdateCustomerAsync_ShouldThrowConflict_WhenPhoneBelongsToAnotherCustomer()
    {
        var customer = Customer.Create("Alice", "alice@example.com", "021 123 4567", CustomerSource.Inquiry, UtcNow);
        var other = Customer.Create("Bob", null, "021 765 4321", CustomerSource.Manual, UtcNow);
        var repository = new Mock<ICustomerRepository>();
        repository
            .Setup(x => x.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        repository
            .Setup(x => x.GetByPhoneNumberAsync("021 765 4321", It.IsAny<CancellationToken>()))
            .ReturnsAsync(other);

        var sut = new AdminCustomerService(repository.Object, Mock.Of<IUnitOfWork>(), new StubClock(UtcNow));

        var act = () => sut.UpdateCustomerAsync(customer.Id, new UpdateCustomerRequest(
            null,
            "021 765 4321",
            null,
            null));

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("The phone number is already used by another customer.");
    }

    [Fact]
    public async Task UpdateCustomerAsync_ShouldThrowNotFound_WhenCustomerDoesNotExist()
    {
        var repository = new Mock<ICustomerRepository>();
        repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var sut = new AdminCustomerService(repository.Object, Mock.Of<IUnitOfWork>(), new StubClock(UtcNow));

        var customerId = Guid.NewGuid();
        var act = () => sut.UpdateCustomerAsync(customerId, new UpdateCustomerRequest(
            "Alice",
            null,
            null,
            null));

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Customer '{customerId}' was not found.");
    }

    private sealed class StubClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
