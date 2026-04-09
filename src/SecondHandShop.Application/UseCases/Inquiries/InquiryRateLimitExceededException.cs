namespace SecondHandShop.Application.UseCases.Inquiries;

public sealed class InquiryRateLimitExceededException : Exception
{
    public InquiryRateLimitExceededException(string message)
        : base(message)
    {
    }

    public InquiryRateLimitExceededException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
