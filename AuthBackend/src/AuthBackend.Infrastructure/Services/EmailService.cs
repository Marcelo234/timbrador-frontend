using AuthBackend.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthBackend.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task EnviarRecuperacionPassword(string email, string token)
    {
        var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:4200";
        var resetUrl = $"{frontendBaseUrl}/reset-password?token={token}";

        // DEV MODE: log instead of sending real email
        _logger.LogInformation("=== RECUPERACIÓN DE CONTRASEÑA ===");
        _logger.LogInformation("Para: {Email}", email);
        _logger.LogInformation("Enlace: {Url}", resetUrl);
        _logger.LogInformation("==================================");

        Console.WriteLine($"\n>>> RESET PASSWORD LINK for {email}:\n>>> {resetUrl}\n");

        return Task.CompletedTask;
    }
}
