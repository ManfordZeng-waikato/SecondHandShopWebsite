using MediatR;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Abstractions.Security;
using SecondHandShop.Application.Contracts.Admin;

namespace SecondHandShop.Application.UseCases.Admin.RefreshSession;

public sealed class RefreshAdminSessionCommandHandler(
    IAdminUserRepository adminUserRepository,
    IJwtTokenService jwtTokenService) : IRequestHandler<RefreshAdminSessionCommand, LoginAdminResponse>
{
    public async Task<LoginAdminResponse> Handle(RefreshAdminSessionCommand request, CancellationToken cancellationToken)
    {
        var user = await adminUserRepository.GetByIdAsync(request.AdminUserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAccessException();
        }

        var (token, expiresAt) = jwtTokenService.CreateToken(user);
        return new LoginAdminResponse(token, expiresAt, user.MustChangePassword);
    }
}
