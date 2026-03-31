namespace SecondHandShop.Application.Contracts.Admin;

/// <summary>
/// New cookie-backed JWT after a successful forced password change (full AdminFullAccess until expiry).
/// </summary>
public sealed record ChangeAdminInitialPasswordResponse(string Token, DateTimeOffset ExpiresAt);
