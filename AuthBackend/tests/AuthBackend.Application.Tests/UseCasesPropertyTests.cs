using AuthBackend.Application.DTOs;
using AuthBackend.Application.UseCases;
using AuthBackend.Domain.Entities;
using AuthBackend.Domain.Exceptions;
using AuthBackend.Domain.Interfaces;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Moq;
using Xunit;

namespace AuthBackend.Application.Tests;

// Feature: sistema-autenticacion — Property tests para Use Cases
public class UseCasesPropertyTests
{
    // ─── Helpers ────────────────────────────────────────────────────────────────

    private static RegistroUsuarioDto DtoRegistroValido(string email = "user@test.com") => new()
    {
        Nombres = "Juan",
        Apellidos = "Pérez",
        Cedula = "1234567890",
        Email = email,
        Password = "Password123",
        ConfirmarPassword = "Password123"
    };

    private static Usuario UsuarioActivo(string email = "user@test.com") =>
        new("Juan", "Pérez", "1234567890", email, "hashed_pw", EstadosUsuario.Activo);

    private static Usuario UsuarioInactivo(string email = "user@test.com", int estado = EstadosUsuario.Inactivo) =>
        new("Juan", "Pérez", "1234567890", email, "hashed_pw", estado);

    // ─── RegistrarUsuarioUseCase ─────────────────────────────────────────────────

    // Property 1: Registro exitoso crea usuario con ID único
    [Fact]
    public async Task RegistrarUsuario_DtoValido_CreaUsuarioConIdUnico()
    {
        var repo = new Mock<IUsuarioRepository>();
        repo.Setup(r => r.ExisteUsuario(It.IsAny<string>())).ReturnsAsync(false);
        Usuario? guardado = null;
        repo.Setup(r => r.GuardarUsuario(It.IsAny<Usuario>()))
            .Callback<Usuario>(u => guardado = u)
            .Returns(Task.CompletedTask);

        var encoder = new Mock<IPasswordEncoder>();
        encoder.Setup(e => e.HashPassword(It.IsAny<string>())).Returns("hashed");

        var uc = new RegistrarUsuarioUseCase(repo.Object, encoder.Object);
        var result = await uc.Ejecutar(DtoRegistroValido());

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        repo.Verify(r => r.GuardarUsuario(It.IsAny<Usuario>()), Times.Once);
    }

    // Property 1b: Estado inicial siempre es Activo
    [Property(MaxTest = 100)]
    public Property RegistrarUsuario_EstadoInicialEsActivo()
    {
        return Prop.ForAll(
            ArbMap.Default.ArbFor<string>().Filter(s => s != null && s.Length > 0),
            email =>
            {
                var repo = new Mock<IUsuarioRepository>();
                repo.Setup(r => r.ExisteUsuario(It.IsAny<string>())).ReturnsAsync(false);
                Usuario? guardado = null;
                repo.Setup(r => r.GuardarUsuario(It.IsAny<Usuario>()))
                    .Callback<Usuario>(u => guardado = u)
                    .Returns(Task.CompletedTask);

                var encoder = new Mock<IPasswordEncoder>();
                encoder.Setup(e => e.HashPassword(It.IsAny<string>())).Returns("hashed");

                var uc = new RegistrarUsuarioUseCase(repo.Object, encoder.Object);
                uc.Ejecutar(DtoRegistroValido(email + "@test.com")).GetAwaiter().GetResult();

                return guardado != null && guardado.EstadoId == EstadosUsuario.Activo;
            });
    }

    // Property 2: Email duplicado lanza UsuarioYaExisteException
    [Fact]
    public async Task RegistrarUsuario_EmailDuplicado_LanzaExcepcion()
    {
        var repo = new Mock<IUsuarioRepository>();
        repo.Setup(r => r.ExisteUsuario(It.IsAny<string>())).ReturnsAsync(true);

        var encoder = new Mock<IPasswordEncoder>();
        var uc = new RegistrarUsuarioUseCase(repo.Object, encoder.Object);

        await Assert.ThrowsAsync<UsuarioYaExisteException>(() => uc.Ejecutar(DtoRegistroValido()));
    }

    // ─── IniciarSesionUseCase ────────────────────────────────────────────────────

    // Property 6: Login exitoso genera ambos tokens y guarda sesión
    [Fact]
    public async Task IniciarSesion_CredencialesValidas_GeneraTokensYGuardaSesion()
    {
        var repo = new Mock<IUsuarioRepository>();
        repo.Setup(r => r.BuscarPorEmail("user@test.com")).ReturnsAsync(UsuarioActivo());

        var sesionRepo = new Mock<ISesionRepository>();
        var encoder = new Mock<IPasswordEncoder>();
        encoder.Setup(e => e.CompararPassword("Password123", "hashed_pw")).Returns(true);

        var jwt = new Mock<IJwtTokenService>();
        jwt.Setup(j => j.GenerarAccessToken(It.IsAny<Usuario>())).Returns("access_token");
        jwt.Setup(j => j.GenerarRefreshToken()).Returns("refresh_token");

        var uc = new IniciarSesionUseCase(repo.Object, sesionRepo.Object, encoder.Object, jwt.Object);
        var result = await uc.Ejecutar(new LoginDto { Email = "user@test.com", Password = "Password123" });

        Assert.Equal("access_token", result.AccessToken);
        Assert.Equal("refresh_token", result.RefreshToken);
        Assert.Equal(900, result.ExpiresIn);
        sesionRepo.Verify(r => r.GuardarSesion(It.IsAny<Sesion>()), Times.Once);
    }

    // Property 7: Credenciales inválidas lanzan CredencialesInvalidasException
    [Fact]
    public async Task IniciarSesion_PasswordIncorrecta_LanzaCredencialesInvalidas()
    {
        var repo = new Mock<IUsuarioRepository>();
        repo.Setup(r => r.BuscarPorEmail(It.IsAny<string>())).ReturnsAsync(UsuarioActivo());

        var encoder = new Mock<IPasswordEncoder>();
        encoder.Setup(e => e.CompararPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var uc = new IniciarSesionUseCase(repo.Object, new Mock<ISesionRepository>().Object, encoder.Object, new Mock<IJwtTokenService>().Object);

        await Assert.ThrowsAsync<CredencialesInvalidasException>(() =>
            uc.Ejecutar(new LoginDto { Email = "user@test.com", Password = "wrong" }));
    }

    // Property 7: Email inexistente lanza CredencialesInvalidasException (mismo mensaje)
    [Fact]
    public async Task IniciarSesion_EmailInexistente_LanzaCredencialesInvalidas()
    {
        var repo = new Mock<IUsuarioRepository>();
        repo.Setup(r => r.BuscarPorEmail(It.IsAny<string>())).ReturnsAsync((Usuario?)null);

        var uc = new IniciarSesionUseCase(repo.Object, new Mock<ISesionRepository>().Object, new Mock<IPasswordEncoder>().Object, new Mock<IJwtTokenService>().Object);

        await Assert.ThrowsAsync<CredencialesInvalidasException>(() =>
            uc.Ejecutar(new LoginDto { Email = "noexiste@test.com", Password = "any" }));
    }

    // Property 7b: Usuario inactivo lanza UsuarioInactivoException y NO guarda sesión
    [Property(MaxTest = 50)]
    public Property IniciarSesion_UsuarioInactivo_LanzaExcepcionYNoGuardaSesion()
    {
        var estadosInactivos = new[] { EstadosUsuario.Inactivo, EstadosUsuario.Suspendido };

        return Prop.ForAll(
            ArbMap.Default.ArbFor<int>().Filter(i => estadosInactivos.Contains(i)),
            estadoId =>
            {
                var repo = new Mock<IUsuarioRepository>();
                repo.Setup(r => r.BuscarPorEmail(It.IsAny<string>()))
                    .ReturnsAsync(UsuarioInactivo(estado: estadoId));

                var encoder = new Mock<IPasswordEncoder>();
                encoder.Setup(e => e.CompararPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

                var sesionRepo = new Mock<ISesionRepository>();
                var uc = new IniciarSesionUseCase(repo.Object, sesionRepo.Object, encoder.Object, new Mock<IJwtTokenService>().Object);

                try
                {
                    uc.Ejecutar(new LoginDto { Email = "user@test.com", Password = "Password123" }).GetAwaiter().GetResult();
                    return false; // No debería llegar aquí
                }
                catch (UsuarioInactivoException)
                {
                    sesionRepo.Verify(r => r.GuardarSesion(It.IsAny<Sesion>()), Times.Never);
                    return true;
                }
            });
    }

    // ─── CerrarSesionUseCase ─────────────────────────────────────────────────────

    // Unit test: FinalizarSesion es llamado con el token correcto
    [Fact]
    public async Task CerrarSesion_LlamaFinalizarSesionConTokenCorrecto()
    {
        var sesionRepo = new Mock<ISesionRepository>();
        var uc = new CerrarSesionUseCase(sesionRepo.Object);

        await uc.Ejecutar("mi_refresh_token");

        sesionRepo.Verify(r => r.FinalizarSesion("mi_refresh_token"), Times.Once);
    }

    // ─── SolicitarRecuperacionPasswordUseCase ────────────────────────────────────

    // Property 16: Email existente → EmailService invocado exactamente una vez
    [Fact]
    public async Task SolicitarRecuperacion_EmailExistente_EnviaEmail()
    {
        var repo = new Mock<IUsuarioRepository>();
        repo.Setup(r => r.BuscarPorEmail("user@test.com")).ReturnsAsync(UsuarioActivo());

        var tokenRepo = new Mock<ITokenRepository>();
        var emailService = new Mock<IEmailService>();

        var uc = new SolicitarRecuperacionPasswordUseCase(repo.Object, tokenRepo.Object, emailService.Object);
        await uc.Ejecutar(new RecuperacionPasswordDto { Email = "user@test.com" });

        emailService.Verify(e => e.EnviarRecuperacionPassword("user@test.com", It.IsAny<string>()), Times.Once);
        tokenRepo.Verify(t => t.GuardarToken(It.IsAny<TokenRecuperacion>()), Times.Once);
    }

    // Property 17: Email no registrado → no lanza excepción, no invoca EmailService
    [Fact]
    public async Task SolicitarRecuperacion_EmailNoExistente_NoEnviaEmailNiLanzaExcepcion()
    {
        var repo = new Mock<IUsuarioRepository>();
        repo.Setup(r => r.BuscarPorEmail(It.IsAny<string>())).ReturnsAsync((Usuario?)null);

        var emailService = new Mock<IEmailService>();
        var uc = new SolicitarRecuperacionPasswordUseCase(repo.Object, new Mock<ITokenRepository>().Object, emailService.Object);

        // No debe lanzar excepción
        await uc.Ejecutar(new RecuperacionPasswordDto { Email = "noexiste@test.com" });

        emailService.Verify(e => e.EnviarRecuperacionPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ─── RestablecerPasswordUseCase ──────────────────────────────────────────────

    // Property 18: Token válido → actualiza contraseña e invalida token
    [Fact]
    public async Task RestablecerPassword_TokenValido_ActualizaPasswordEInvalidaToken()
    {
        var usuario = UsuarioActivo("user@test.com");
        var tokenRec = new TokenRecuperacion(usuario.Id, "valid_token");

        var usuarioRepo = new Mock<IUsuarioRepository>();
        usuarioRepo.Setup(r => r.BuscarPorEmail("user@test.com")).ReturnsAsync(usuario);

        var tokenRepo = new Mock<ITokenRepository>();
        tokenRepo.Setup(t => t.BuscarToken("valid_token")).ReturnsAsync(tokenRec);

        var sesionRepo = new Mock<ISesionRepository>();
        var encoder = new Mock<IPasswordEncoder>();
        encoder.Setup(e => e.HashPassword("NewPass123")).Returns("new_hashed");

        var uc = new RestablecerPasswordUseCase(usuarioRepo.Object, tokenRepo.Object, sesionRepo.Object, encoder.Object);
        await uc.Ejecutar(new RestablecerPasswordDto { Token = "valid_token", Email = "user@test.com", NuevaPassword = "NewPass123" });

        usuarioRepo.Verify(r => r.ActualizarUsuario(It.IsAny<Usuario>()), Times.Once);
        tokenRepo.Verify(t => t.InvalidarToken("valid_token"), Times.Once);
    }

    // Property 19: Restablecimiento exitoso revoca todas las sesiones
    [Fact]
    public async Task RestablecerPassword_Exitoso_RevocaTodasLasSesiones()
    {
        var usuario = UsuarioActivo("user@test.com");
        var tokenRec = new TokenRecuperacion(usuario.Id, "valid_token");

        var usuarioRepo = new Mock<IUsuarioRepository>();
        usuarioRepo.Setup(r => r.BuscarPorEmail("user@test.com")).ReturnsAsync(usuario);

        var tokenRepo = new Mock<ITokenRepository>();
        tokenRepo.Setup(t => t.BuscarToken("valid_token")).ReturnsAsync(tokenRec);

        var sesionRepo = new Mock<ISesionRepository>();
        var encoder = new Mock<IPasswordEncoder>();
        encoder.Setup(e => e.HashPassword(It.IsAny<string>())).Returns("hashed");

        var uc = new RestablecerPasswordUseCase(usuarioRepo.Object, tokenRepo.Object, sesionRepo.Object, encoder.Object);
        await uc.Ejecutar(new RestablecerPasswordDto { Token = "valid_token", Email = "user@test.com", NuevaPassword = "NewPass123" });

        sesionRepo.Verify(r => r.RevocarTodasLasSesionesDelUsuario(usuario.Id), Times.Once);
    }

    // Token inválido lanza TokenInvalidoException
    [Fact]
    public async Task RestablecerPassword_TokenInvalido_LanzaExcepcion()
    {
        var tokenRepo = new Mock<ITokenRepository>();
        tokenRepo.Setup(t => t.BuscarToken(It.IsAny<string>())).ReturnsAsync((TokenRecuperacion?)null);

        var uc = new RestablecerPasswordUseCase(new Mock<IUsuarioRepository>().Object, tokenRepo.Object, new Mock<ISesionRepository>().Object, new Mock<IPasswordEncoder>().Object);

        await Assert.ThrowsAsync<TokenInvalidoException>(() =>
            uc.Ejecutar(new RestablecerPasswordDto { Token = "bad_token", Email = "user@test.com", NuevaPassword = "NewPass123" }));
    }
}
