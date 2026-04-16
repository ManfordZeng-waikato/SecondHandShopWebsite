using FluentAssertions;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Domain.UnitTests.Entities;

public class CustomerTests
{
    private static readonly DateTime UtcNow = new(2026, 4, 16, 1, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void MergeContact_ShouldFillMissingFields_WithoutOverwritingExistingValues()
    {
        var customer = Customer.Create(
            name: "Alice",
            email: "alice@example.com",
            phoneNumber: null,
            source: CustomerSource.Inquiry,
            utcNow: UtcNow);

        customer.MergeContact(
            name: "Alice Renamed",
            email: "new@example.com",
            phoneNumber: "021 123 4567",
            utcNow: UtcNow.AddMinutes(10));

        customer.Name.Should().Be("Alice");
        customer.Email.Should().Be("alice@example.com");
        customer.PhoneNumber.Should().Be("021 123 4567");
        customer.LastContactAtUtc.Should().Be(UtcNow.AddMinutes(10));
    }

    [Fact]
    public void Create_ShouldRequireAtLeastOneContactMethod()
    {
        var act = () => Customer.Create(
            name: "No Contact",
            email: null,
            phoneNumber: null,
            source: CustomerSource.Manual,
            utcNow: UtcNow);

        act.Should().Throw<ArgumentException>()
            .WithMessage("At least one contact method*");
    }
}
