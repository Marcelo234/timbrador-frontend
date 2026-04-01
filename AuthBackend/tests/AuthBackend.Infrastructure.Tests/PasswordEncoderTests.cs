using AuthBackend.Infrastructure.Services;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Xunit;

namespace AuthBackend.Infrastructure.Tests;

// Feature: sistema-autenticacion — Property tests para PasswordEncoder
public class PasswordEncoderTests
{
    private readonly PasswordEncoder _encoder = new();

    // Property 5: Hash es diferente al original y verificable
    [Property(MaxTest = 10)] // BCrypt es lento, limitamos iteraciones
    public Property HashPassword_DiferenteAlOriginalYVerificable()
    {
        return Prop.ForAll(
            ArbMap.Default.ArbFor<string>().Filter(s => s != null && s.Length > 0 && s.Length <= 72),
            password =>
            {
                var hash = _encoder.HashPassword(password);
                return hash != password && _encoder.CompararPassword(password, hash);
            });
    }

    [Fact]
    public void HashPassword_MismaPassword_GeneraHashesDiferentes()
    {
        // BCrypt genera salt aleatorio, dos hashes del mismo password deben ser distintos
        var hash1 = _encoder.HashPassword("Password123");
        var hash2 = _encoder.HashPassword("Password123");
        Assert.NotEqual(hash1, hash2);
        Assert.True(_encoder.CompararPassword("Password123", hash1));
        Assert.True(_encoder.CompararPassword("Password123", hash2));
    }

    [Fact]
    public void CompararPassword_PasswordIncorrecto_RetornaFalse()
    {
        var hash = _encoder.HashPassword("CorrectPassword");
        Assert.False(_encoder.CompararPassword("WrongPassword", hash));
    }
}
