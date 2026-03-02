using MediatR;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Abstractions.Security;
using SecondHandShop.Application.Contracts.Admin;

namespace SecondHandShop.Application.UseCases.Admin.Login;

public class LoginAdminCommandHandler(
    IAdminUserRepository adminUserRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService) : IRequestHandler<LoginAdminCommand, LoginAdminResponse>
{
    public async Task<LoginAdminResponse> Handle(LoginAdminCommand request, CancellationToken cancellationToken)
    {
        var user = await adminUserRepository.GetByUserNameAsync(request.UserName, cancellationToken);

        if (user is null || !user.IsActive || !passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        var (token, expiresAt) = jwtTokenService.CreateToken(user);
        return new LoginAdminResponse(token, expiresAt);
    }
}
