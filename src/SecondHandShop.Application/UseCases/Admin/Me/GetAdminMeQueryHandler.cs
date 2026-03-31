using MediatR;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Contracts.Admin;

namespace SecondHandShop.Application.UseCases.Admin.Me;

public sealed class GetAdminMeQueryHandler(IAdminUserRepository adminUserRepository)
    : IRequestHandler<GetAdminMeQuery, AdminMeResponse?>
{
    public async Task<AdminMeResponse?> Handle(GetAdminMeQuery request, CancellationToken cancellationToken)
    {
        var user = await adminUserRepository.GetByIdAsync(request.AdminUserId, cancellationToken);

        // JWT proves identity at issue time; DB decides whether the account still exists and may use the API.
        if (user is null || !user.IsActive)
            return null;

        // MustChangePassword and Role come from DB, not from token claims, so admin workflow changes apply without re-login.
        return new AdminMeResponse(
            IsAuthenticated: true,
            UserId: user.Id,
            UserName: user.UserName,
            Email: user.Email,
            Role: user.Role,
            MustChangePassword: user.MustChangePassword);
    }
}
