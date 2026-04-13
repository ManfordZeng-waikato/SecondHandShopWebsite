namespace SecondHandShop.Application.Security;

/// <summary>
/// Custom JWT claim types for admin tokens. Kept in Application so authorization policies and handlers share one source of truth.
/// </summary>
public static class AdminJwtClaimTypes
{
    /// <summary>
    /// When value is "true", the token is restricted to session-only endpoints until the admin completes a forced password change.
    /// </summary>
    public const string PasswordChangeRequired = "pwd_chg_req";

    /// <summary>
    /// Monotonic server-side token version. Used to revoke previously-issued JWTs immediately.
    /// </summary>
    public const string TokenVersion = "tkv";
}
