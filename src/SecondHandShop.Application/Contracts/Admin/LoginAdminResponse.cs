namespace SecondHandShop.Application.Contracts.Admin;

public sealed record LoginAdminResponse(string Token, DateTimeOffset ExpiresAt, bool RequiresPasswordChange);
