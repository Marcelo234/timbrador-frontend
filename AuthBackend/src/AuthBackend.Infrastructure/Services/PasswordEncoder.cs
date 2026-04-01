using AuthBackend.Domain.Interfaces;

namespace AuthBackend.Infrastructure.Services;

public class PasswordEncoder : IPasswordEncoder
{
    public string HashPassword(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    public bool CompararPassword(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
