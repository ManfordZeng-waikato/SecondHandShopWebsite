using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Application.Abstractions.Security;

public interface IJwtTokenService
{
    (string Token, DateTimeOffset ExpiresAt) CreateToken(AdminUser user);
}
