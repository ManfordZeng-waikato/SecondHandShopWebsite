using SecondHandShop.Application.Contracts.Inquiries;

namespace SecondHandShop.Application.UseCases.Inquiries;

public interface IInquiryService
{
    Task<Guid> CreateInquiryAsync(CreateInquiryCommand command, CancellationToken cancellationToken = default);
}
