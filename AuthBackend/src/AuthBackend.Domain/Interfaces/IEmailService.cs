namespace AuthBackend.Domain.Interfaces;

public interface IEmailService
{
    Task EnviarRecuperacionPassword(string email, string token);
}
