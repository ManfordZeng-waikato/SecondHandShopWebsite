using SecondHandShop.Application.Abstractions.Security;

namespace SecondHandShop.Infrastructure.Services;

public class PasswordHasherService : IPasswordHasher
{
    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool Verify(string hashed, string password)
    {
        return BCrypt.Net.BCrypt.Verify(password, hashed);
    }
}
