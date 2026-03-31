using Microsoft.Extensions.Logging;
using MediatR;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Abstractions.Security;
using SecondHandShop.Application.Contracts.Admin;

namespace SecondHandShop.Application.UseCases.Admin.Login;

public class LoginAdminCommandHandler(
    IAdminUserRepository adminUserRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    ILogger<LoginAdminCommandHandler> logger) : IRequestHandler<LoginAdminCommand, LoginAdminResponse>
{
    private const string DummyHash = "$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";

    public async Task<LoginAdminResponse> Handle(LoginAdminCommand request, CancellationToken cancellationToken)
    {
        var user = await adminUserRepository.GetByUserNameAsync(request.UserName, cancellationToken);

        var hashToVerify = user?.PasswordHash ?? DummyHash;
        var passwordValid = passwordHasher.Verify(hashToVerify, request.Password);

        if (user is null || !user.IsActive || !passwordValid)
        {
            logger.LogWarning("Failed login attempt for user '{UserName}'.", request.UserName);
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        logger.LogInformation("Admin user '{UserName}' logged in successfully.", user.UserName);
        var (token, expiresAt) = jwtTokenService.CreateToken(user);
        return new LoginAdminResponse(token, expiresAt, user.MustChangePassword);
    }
}
