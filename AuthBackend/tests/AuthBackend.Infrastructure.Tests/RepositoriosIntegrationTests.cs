using AuthBackend.Domain.Entities;
using AuthBackend.Infrastructure.Persistence;
using AuthBackend.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AuthBackend.Infrastructure.Tests;

// Feature: sistema-autenticacion — Integration tests para repositorios con BD en memoria
public class RepositoriosIntegrationTests : IDisposable
{
    private readonly AuthDbContext _context;
    private readonly SesionRepository _sesionRepo;
    private readonly TokenRepository _tokenRepo;
    private readonly UsuarioRepository _usuarioRepo;

    public RepositoriosIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AuthDbContext(options);
        _sesionRepo = new SesionRepository(_context);
        _tokenRepo = new TokenRepository(_context);
        _usuarioRepo = new UsuarioRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    private Usuario CrearUsuario(string email = "user@test.com") =>
        new("Juan", "Pérez", "1234567890", email, "hashed_pw", EstadosUsuario.Activo);

    // ─── UsuarioRepository ───────────────────────────────────────────────────────

    [Fact]
    public async Task GuardarUsuario_LuegoBuscarPorEmail_RetornaUsuarioCorrecto()
    {
        var usuario = CrearUsuario("test@example.com");
        await _usuarioRepo.GuardarUsuario(usuario);

        var encontrado = await _usuarioRepo.BuscarPorEmail("test@example.com");

        Assert.NotNull(encontrado);
        Assert.Equal(usuario.Id, encontrado.Id);
        Assert.Equal("test@example.com", encontrado.Email);
    }

    [Fact]
    public async Task ExisteUsuario_EmailRegistrado_RetornaTrue()
    {
        await _usuarioRepo.GuardarUsuario(CrearUsuario("existe@test.com"));
        Assert.True(await _usuarioRepo.ExisteUsuario("existe@test.com"));
    }

    [Fact]
    public async Task ExisteUsuario_EmailNoRegistrado_RetornaFalse()
    {
        Assert.False(await _usuarioRepo.ExisteUsuario("noexiste@test.com"));
    }

    // ─── SesionRepository ────────────────────────────────────────────────────────

    [Fact]
    public async Task GuardarSesion_LuegoBuscarPorToken_RetornaSesionCorrecta()
    {
        var usuario = CrearUsuario();
        await _usuarioRepo.GuardarUsuario(usuario);

        var sesion = new Sesion(usuario.Id, "token_abc", DateTime.UtcNow.AddDays(7));
        await _sesionRepo.GuardarSesion(sesion);

        var encontrada = await _sesionRepo.BuscarPorToken("token_abc");

        Assert.NotNull(encontrada);
        Assert.Equal(sesion.Id, encontrada.Id);
        Assert.Equal(usuario.Id, encontrada.UsuarioId);
    }

    [Fact]
    public async Task FinalizarSesion_EstableceRevocadoYFechaRevocacion()
    {
        var usuario = CrearUsuario();
        await _usuarioRepo.GuardarUsuario(usuario);

        var sesion = new Sesion(usuario.Id, "token_revocar", DateTime.UtcNow.AddDays(7));
        await _sesionRepo.GuardarSesion(sesion);

        await _sesionRepo.FinalizarSesion("token_revocar");

        var revocada = await _sesionRepo.BuscarPorToken("token_revocar");
        Assert.NotNull(revocada);
        Assert.True(revocada.Revocado);
        Assert.NotNull(revocada.FechaRevocacion);
    }

    // Property 13: Tokens revocados persisten en BD
    [Fact]
    public async Task FinalizarSesion_SesionRevocadaPermanecEnBD()
    {
        var usuario = CrearUsuario();
        await _usuarioRepo.GuardarUsuario(usuario);

        var sesion = new Sesion(usuario.Id, "token_persistir", DateTime.UtcNow.AddDays(7));
        await _sesionRepo.GuardarSesion(sesion);

        await _sesionRepo.FinalizarSesion("token_persistir");

        // La sesión revocada sigue existiendo en BD
        var aun = await _sesionRepo.BuscarPorToken("token_persistir");
        Assert.NotNull(aun);
        Assert.True(aun.Revocado);
    }

    [Fact]
    public async Task RevocarTodasLasSesiones_RevocaSoloSesionesDelUsuarioIndicado()
    {
        var usuario1 = CrearUsuario("u1@test.com");
        var usuario2 = CrearUsuario("u2@test.com");
        await _usuarioRepo.GuardarUsuario(usuario1);
        await _usuarioRepo.GuardarUsuario(usuario2);

        var sesionU1 = new Sesion(usuario1.Id, "token_u1", DateTime.UtcNow.AddDays(7));
        var sesionU2 = new Sesion(usuario2.Id, "token_u2", DateTime.UtcNow.AddDays(7));
        await _sesionRepo.GuardarSesion(sesionU1);
        await _sesionRepo.GuardarSesion(sesionU2);

        await _sesionRepo.RevocarTodasLasSesionesDelUsuario(usuario1.Id);

        var s1 = await _sesionRepo.BuscarPorToken("token_u1");
        var s2 = await _sesionRepo.BuscarPorToken("token_u2");

        Assert.True(s1!.Revocado);
        Assert.False(s2!.Revocado); // La sesión de usuario2 no debe verse afectada
    }

    // ─── TokenRepository ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GuardarToken_LuegoBuscarToken_RetornaTokenCorrecto()
    {
        var usuario = CrearUsuario();
        await _usuarioRepo.GuardarUsuario(usuario);

        var token = new TokenRecuperacion(usuario.Id, "recovery_token_xyz");
        await _tokenRepo.GuardarToken(token);

        var encontrado = await _tokenRepo.BuscarToken("recovery_token_xyz");

        Assert.NotNull(encontrado);
        Assert.Equal(token.Id, encontrado.Id);
        Assert.False(encontrado.Usado);
    }

    [Fact]
    public async Task InvalidarToken_EstableceUsadoTrue()
    {
        var usuario = CrearUsuario();
        await _usuarioRepo.GuardarUsuario(usuario);

        var token = new TokenRecuperacion(usuario.Id, "token_invalidar");
        await _tokenRepo.GuardarToken(token);

        await _tokenRepo.InvalidarToken("token_invalidar");

        var invalidado = await _tokenRepo.BuscarToken("token_invalidar");
        Assert.NotNull(invalidado);
        Assert.True(invalidado.Usado);
        Assert.NotNull(invalidado.FechaUso);
    }
}
