using MediatR;
using SecondHandShop.Application.Abstractions.Common;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Abstractions.Security;
using SecondHandShop.Application.Contracts.Admin;
using SecondHandShop.Application.Security;

namespace SecondHandShop.Application.UseCases.Admin.ChangeInitialPassword;

public sealed class ChangeAdminInitialPasswordCommandHandler(
    IAdminUserRepository adminUserRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IUnitOfWork unitOfWork) : IRequestHandler<ChangeAdminInitialPasswordCommand, ChangeAdminInitialPasswordResponse>
{
    private const string DummyHash = "$2a$11$xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";

    public async Task<ChangeAdminInitialPasswordResponse> Handle(
        ChangeAdminInitialPasswordCommand request,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(request.NewPassword, request.ConfirmNewPassword, StringComparison.Ordinal))
        {
            throw new ArgumentException("New password and confirmation do not match.");
        }

        AdminPasswordPolicy.Validate(request.NewPassword);

        var user = await adminUserRepository.GetByIdAsync(request.AdminUserId, cancellationToken);
        var hashToVerify = user?.PasswordHash ?? DummyHash;
        var currentOk = passwordHasher.Verify(hashToVerify, request.CurrentPassword);

        if (user is null || !user.IsActive || !currentOk)
        {
            // Do not distinguish unknown user vs bad password (same as login).
            throw new UnauthorizedAccessException();
        }

        if (!user.MustChangePassword)
        {
            // Not a forced-first-login account; keep message generic to avoid account enumeration.
            throw new InvalidOperationException("This operation is not available for your account.");
        }

        if (string.Equals(request.CurrentPassword, request.NewPassword, StringComparison.Ordinal))
        {
            throw new ArgumentException("New password must be different from the current password.");
        }

        var newHash = passwordHasher.Hash(request.NewPassword);
        user.CompleteForcedPasswordChange(newHash);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var (token, expiresAt) = jwtTokenService.CreateToken(user);
        return new ChangeAdminInitialPasswordResponse(token, expiresAt);
    }
}
