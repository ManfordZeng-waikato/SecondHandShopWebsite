namespace SecondHandShop.Application.UseCases.Inquiries;

public sealed class InquiryTurnstileValidationException : Exception
{
    public InquiryTurnstileValidationException(string message)
        : base(message)
    {
    }
}
