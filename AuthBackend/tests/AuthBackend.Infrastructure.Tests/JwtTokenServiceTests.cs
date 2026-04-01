using System.IdentityModel.Tokens.Jwt;
using System.Text;
using AuthBackend.Domain.Entities;
using AuthBackend.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace AuthBackend.Infrastructure.Tests;

// Feature: sistema-autenticacion — Property tests para JwtTokenService
public class JwtTokenServiceTests
{
    private readonly JwtTokenService _service;
    private readonly string _secretKey = "super-secret-key-for-testing-minimum-256-bits-long!!";

    public JwtTokenServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = _secretKey,
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience"
            })
            .Build();
        _service = new JwtTokenService(config);
    }

    private static Usuario UsuarioPrueba() =>
        new("Juan", "Pérez", "1234567890", "juan@test.com", "hashed", EstadosUsuario.Activo);

    // Property 8: AccessToken expira entre now+15min y now+15min+1seg
    [Fact]
    public void GenerarAccessToken_ExpiraEn15Minutos()
    {
        var usuario = UsuarioPrueba();
        var antes = DateTime.UtcNow;

        var tokenStr = _service.GenerarAccessToken(usuario);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenStr);

        var expiracion = token.ValidTo;
        // JWT trunca a segundos, así que permitimos 1 segundo de margen inferior
        var minExpected = antes.AddMinutes(15).AddSeconds(-1);
        var maxExpected = antes.AddMinutes(15).AddSeconds(5);

        Assert.True(expiracion >= minExpected && expiracion <= maxExpected,
            $"Expiración {expiracion} fuera del rango [{minExpected}, {maxExpected}]");
    }

    // Property 9: Claims correctos en AccessToken
    [Fact]
    public void GenerarAccessToken_ContieneClaimsCorrectos()
    {
        var usuario = UsuarioPrueba();
        var tokenStr = _service.GenerarAccessToken(usuario);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenStr);

        Assert.Equal(usuario.Id.ToString(), token.Subject);
        Assert.Equal(usuario.Email, token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(usuario.Nombres, token.Claims.First(c => c.Type == "nombres").Value);
        Assert.Equal(usuario.Apellidos, token.Claims.First(c => c.Type == "apellidos").Value);
        Assert.NotEmpty(token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value);
    }

    // Token es válido con la clave correcta
    [Fact]
    public void GenerarAccessToken_EsValidoConClaveCorrecta()
    {
        var usuario = UsuarioPrueba();
        var tokenStr = _service.GenerarAccessToken(usuario);

        var handler = new JwtSecurityTokenHandler();
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey)),
            ValidateIssuer = true,
            ValidIssuer = "TestIssuer",
            ValidateAudience = true,
            ValidAudience = "TestAudience",
            ClockSkew = TimeSpan.Zero
        };

        var principal = handler.ValidateToken(tokenStr, validationParams, out _);
        Assert.NotNull(principal);
    }

    // RefreshToken es Base64 de 64 bytes (88 chars con padding)
    [Fact]
    public void GenerarRefreshToken_Es88CaracteresBase64()
    {
        var token = _service.GenerarRefreshToken();
        var bytes = Convert.FromBase64String(token);
        Assert.Equal(64, bytes.Length);
    }

    // Dos RefreshTokens consecutivos son distintos
    [Fact]
    public void GenerarRefreshToken_DosTokensConsecutivosSonDistintos()
    {
        var t1 = _service.GenerarRefreshToken();
        var t2 = _service.GenerarRefreshToken();
        Assert.NotEqual(t1, t2);
    }
}
