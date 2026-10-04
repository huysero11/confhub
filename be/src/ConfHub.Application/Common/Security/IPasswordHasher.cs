namespace ConfHub.Application.Common.Security;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string passwordHash, string password);
}
