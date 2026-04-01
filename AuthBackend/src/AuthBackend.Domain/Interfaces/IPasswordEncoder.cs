namespace AuthBackend.Domain.Interfaces;

public interface IPasswordEncoder
{
    string HashPassword(string password);
    bool CompararPassword(string password, string hash);
}
