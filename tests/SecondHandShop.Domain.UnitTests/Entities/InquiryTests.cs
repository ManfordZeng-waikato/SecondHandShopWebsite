using FluentAssertions;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Domain.UnitTests.Entities;

public class InquiryTests
{
    private static readonly DateTime UtcNow = new(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldNormaliseValues_AndStartAsPending()
    {
        var inquiry = Inquiry.Create(
            productId: Guid.NewGuid(),
            customerId: Guid.NewGuid(),
            customerName: "  Alice  ",
            email: " Alice@Example.com ",
            phoneNumber: null,
            requestIpAddress: " 203.0.113.7 ",
            messageHash: "  ABCDEF  ",
            message: "  Hello!  ",
            utcNow: UtcNow);

        inquiry.CustomerName.Should().Be("Alice");
        inquiry.Email.Should().Be("Alice@Example.com");
        inquiry.RequestIpAddress.Should().Be("203.0.113.7");
        inquiry.MessageHash.Should().Be("abcdef", "hash is lower-cased for de-dup comparison");
        inquiry.Message.Should().Be("Hello!");
        inquiry.EmailDeliveryStatus.Should().Be(EmailDeliveryStatus.Pending);
        inquiry.EmailSendAttempts.Should().Be(0);
        inquiry.CreatedAt.Should().Be(UtcNow);
    }

    [Fact]
    public void Create_ShouldThrow_WhenNeitherEmailNorPhoneProvided()
    {
        var act = () => Inquiry.Create(
            Guid.NewGuid(), Guid.NewGuid(), null, null, null, null, "hash", "msg", UtcNow);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*contact method*");
    }

    [Fact]
    public void Create_ShouldThrow_WhenEmailIsInvalid()
    {
        var act = () => Inquiry.Create(
            Guid.NewGuid(), Guid.NewGuid(), null, "not-an-email", null, null, "hash", "msg", UtcNow);

        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("email");
    }

    [Fact]
    public void Create_ShouldThrow_WhenPhoneContainsLetters()
    {
        var act = () => Inquiry.Create(
            Guid.NewGuid(), Guid.NewGuid(), null, null, "call-me", null, "hash", "msg", UtcNow);

        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("phoneNumber");
    }

    [Fact]
    public void MarkEmailSent_ShouldTransitionToSent_AndClearTransientState()
    {
        var inquiry = CreateInquiry();
        inquiry.MarkEmailFailed("smtp-timeout", UtcNow.AddMinutes(5));

        inquiry.MarkEmailSent(UtcNow.AddMinutes(10));

        inquiry.EmailDeliveryStatus.Should().Be(EmailDeliveryStatus.Sent);
        inquiry.DeliveredAt.Should().Be(UtcNow.AddMinutes(10));
        inquiry.DeliveryError.Should().BeNull();
        inquiry.NextRetryAt.Should().BeNull();
    }

    [Fact]
    public void MarkEmailFailed_ShouldIncrementAttempts_AndPersistError()
    {
        var inquiry = CreateInquiry();

        inquiry.MarkEmailFailed("recipient rejected", nextRetryAt: null);

        inquiry.EmailDeliveryStatus.Should().Be(EmailDeliveryStatus.Failed);
        inquiry.DeliveryError.Should().Be("recipient rejected");
        inquiry.EmailSendAttempts.Should().Be(1);
        inquiry.DeliveredAt.Should().BeNull();

        inquiry.MarkEmailFailed("   ", nextRetryAt: UtcNow.AddMinutes(10));
        inquiry.EmailSendAttempts.Should().Be(2);
        inquiry.DeliveryError.Should().Be("Unknown mail delivery error.",
            "blank error messages are replaced with a default label");
    }

    [Fact]
    public void RecordTransientFailure_ShouldKeepStatusPending_ButQueueRetry()
    {
        var inquiry = CreateInquiry();

        inquiry.RecordTransientFailure("timeout", UtcNow.AddMinutes(3));

        inquiry.EmailDeliveryStatus.Should().Be(EmailDeliveryStatus.Pending);
        inquiry.DeliveryError.Should().Be("timeout");
        inquiry.NextRetryAt.Should().Be(UtcNow.AddMinutes(3));
        inquiry.EmailSendAttempts.Should().Be(1);
    }

    private static Inquiry CreateInquiry() => Inquiry.Create(
        Guid.NewGuid(), Guid.NewGuid(), "Alice", "alice@example.com", null, null,
        "hash", "msg", UtcNow);
}
