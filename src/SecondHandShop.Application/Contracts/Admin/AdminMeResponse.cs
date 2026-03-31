namespace SecondHandShop.Application.Contracts.Admin;

/// <summary>
/// Current admin session info for the SPA. Built from the database after JWT identifies the user
/// so flags like MustChangePassword stay authoritative even if the token was issued earlier.
/// </summary>
public sealed record AdminMeResponse(
    bool IsAuthenticated,
    Guid UserId,
    string UserName,
    string Email,
    string Role,
    bool MustChangePassword);
