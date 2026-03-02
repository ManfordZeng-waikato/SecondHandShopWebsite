namespace SecondHandShop.Application.Abstractions.Security;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string hashed, string password);
}
