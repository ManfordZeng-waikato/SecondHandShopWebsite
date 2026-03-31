using MediatR;
using SecondHandShop.Application.Contracts.Admin;

namespace SecondHandShop.Application.UseCases.Admin.ChangeInitialPassword;

public sealed record ChangeAdminInitialPasswordCommand(
    Guid AdminUserId,
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword) : IRequest<ChangeAdminInitialPasswordResponse>;
