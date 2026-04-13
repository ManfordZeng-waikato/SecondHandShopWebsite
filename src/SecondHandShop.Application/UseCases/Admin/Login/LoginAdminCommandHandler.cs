using Microsoft.Extensions.Logging;
using MediatR;
using SecondHandShop.Application.Abstractions.Common;
using SecondHandShop.Application.Abstractions.Messaging;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Abstractions.Security;
using SecondHandShop.Application.Contracts.Admin;

namespace SecondHandShop.Application.UseCases.Admin.Login;

public class LoginAdminCommandHandler(
    IClock clock,
    IAdminLoginNotificationQueue adminLoginNotificationQueue,
    IAdminUserRepository adminUserRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IUnitOfWork unitOfWork,
    ILogger<LoginAdminCommandHandler> logger) : IRequestHandler<LoginAdminCommand, LoginAdminResponse>
{
    private const string DummyHash = "$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";
    private const int MaxFailedLoginAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<LoginAdminResponse> Handle(LoginAdminCommand request, CancellationToken cancellationToken)
    {
        var user = await adminUserRepository.GetByUserNameAsync(request.UserName, cancellationToken);
        var utcNow = clock.UtcNow;

        var hashToVerify = user?.PasswordHash ?? DummyHash;
        var passwordValid = passwordHasher.Verify(hashToVerify, request.Password);

        if (user is null || !user.IsActive)
        {
            logger.LogWarning(
                "Failed login attempt for user '{UserName}'.",
                TruncateForLog(request.UserName));
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        if (user.IsLockedOut(utcNow))
        {
            if (!passwordValid)
            {
                user.RegisterFailedLogin(MaxFailedLoginAttempts, LockoutDuration, utcNow);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            logger.LogWarning(
                "Blocked login attempt for locked admin user '{UserName}' until {LockedUntilUtc}.",
                TruncateForLog(user.UserName),
                user.LockedUntilUtc);
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        if (!passwordValid)
        {
            user.RegisterFailedLogin(MaxFailedLoginAttempts, LockoutDuration, utcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogWarning(
                "Failed login attempt for user '{UserName}'.",
                TruncateForLog(user.UserName));
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        user.RegisterSuccessfulLogin(utcNow, request.SourceIpAddress);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Admin user '{UserName}' logged in successfully.", user.UserName);
        var (token, expiresAt) = jwtTokenService.CreateToken(user);

        adminLoginNotificationQueue.Enqueue(
            new AdminLoginNotificationMessage(
                user.Id,
                user.UserName,
                user.DisplayName,
                user.Email,
                utcNow,
                request.SourceIpAddress));

        return new LoginAdminResponse(token, expiresAt, user.MustChangePassword);
    }

    private static string TruncateForLog(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "(empty)";

        var trimmed = value.Trim();
        return trimmed.Length > 64 ? trimmed[..64] : trimmed;
    }
}
