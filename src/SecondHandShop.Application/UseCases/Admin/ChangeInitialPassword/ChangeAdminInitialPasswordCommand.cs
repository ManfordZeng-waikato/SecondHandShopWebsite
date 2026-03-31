using MediatR;

namespace SecondHandShop.Application.UseCases.Admin.ChangeInitialPassword;

public sealed record ChangeAdminInitialPasswordCommand(
    Guid AdminUserId,
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword) : IRequest<Unit>;
