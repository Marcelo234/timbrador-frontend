using AuthBackend.Application.DTOs;
using AuthBackend.Application.UseCases;
using AuthBackend.Domain.Entities;
using AuthBackend.Domain.Interfaces;
using AuthBackend.Infrastructure.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AuthBackend.Infrastructure.Tests;

// Feature: sistema-autenticacion — Unit tests para AuthController
public class AuthControllerTests
{
    private static AuthController CreateController(
        Mock<RegistrarUsuarioUseCase>? registrar = null,
        Mock<IniciarSesionUseCase>? iniciarSesion = null,
        Mock<CerrarSesionUseCase>? cerrarSesion = null,
        Mock<SolicitarRecuperacionPasswordUseCase>? solicitarRec = null,
        Mock<RestablecerPasswordUseCase>? restablecer = null,
        Mock<ISesionRepository>? sesionRepo = null,
        Mock<IJwtTokenService>? jwt = null)
    {
        var usuarioRepo = new Mock<IUsuarioRepository>();
        var encoder = new Mock<IPasswordEncoder>();
        var emailService = new Mock<IEmailService>();
        var tokenRepo = new Mock<ITokenRepository>();

        registrar ??= new Mock<RegistrarUsuarioUseCase>(usuarioRepo.Object, encoder.Object);
        iniciarSesion ??= new Mock<IniciarSesionUseCase>(usuarioRepo.Object, sesionRepo?.Object ?? new Mock<ISesionRepository>().Object, encoder.Object, jwt?.Object ?? new Mock<IJwtTokenService>().Object);
        cerrarSesion ??= new Mock<CerrarSesionUseCase>(sesionRepo?.Object ?? new Mock<ISesionRepository>().Object);
        solicitarRec ??= new Mock<SolicitarRecuperacionPasswordUseCase>(usuarioRepo.Object, tokenRepo.Object, emailService.Object);
        restablecer ??= new Mock<RestablecerPasswordUseCase>(usuarioRepo.Object, tokenRepo.Object, sesionRepo?.Object ?? new Mock<ISesionRepository>().Object, encoder.Object);
        sesionRepo ??= new Mock<ISesionRepository>();
        jwt ??= new Mock<IJwtTokenService>();

        return new AuthController(
            registrar.Object,
            iniciarSesion.Object,
            cerrarSesion.Object,
            solicitarRec.Object,
            restablecer.Object,
            sesionRepo.Object,
            jwt.Object);
    }

    // Register retorna 201 con UsuarioResponseDto
    [Fact]
    public async Task Register_DtoValido_Retorna201ConUsuario()
    {
        var usuarioRepo = new Mock<IUsuarioRepository>();
        usuarioRepo.Setup(r => r.ExisteUsuario(It.IsAny<string>())).ReturnsAsync(false);
        usuarioRepo.Setup(r => r.GuardarUsuario(It.IsAny<Usuario>())).Returns(Task.CompletedTask);

        var encoder = new Mock<IPasswordEncoder>();
        encoder.Setup(e => e.HashPassword(It.IsAny<string>())).Returns("hashed");

        var uc = new RegistrarUsuarioUseCase(usuarioRepo.Object, encoder.Object);
        var controller = new AuthController(
            uc,
            new Mock<IniciarSesionUseCase>(usuarioRepo.Object, new Mock<ISesionRepository>().Object, encoder.Object, new Mock<IJwtTokenService>().Object).Object,
            new Mock<CerrarSesionUseCase>(new Mock<ISesionRepository>().Object).Object,
            new Mock<SolicitarRecuperacionPasswordUseCase>(usuarioRepo.Object, new Mock<ITokenRepository>().Object, new Mock<IEmailService>().Object).Object,
            new Mock<RestablecerPasswordUseCase>(usuarioRepo.Object, new Mock<ITokenRepository>().Object, new Mock<ISesionRepository>().Object, encoder.Object).Object,
            new Mock<ISesionRepository>().Object,
            new Mock<IJwtTokenService>().Object);

        var dto = new RegistroUsuarioDto
        {
            Nombres = "Juan", Apellidos = "Pérez", Cedula = "1234567890",
            Email = "juan@test.com", Password = "Password123", ConfirmarPassword = "Password123"
        };

        var result = await controller.Register(dto);

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, created.StatusCode);
        Assert.IsType<UsuarioResponseDto>(created.Value);
    }

    // Login retorna 200 con TokenResponseDto
    [Fact]
    public async Task Login_CredencialesValidas_Retorna200ConTokens()
    {
        var usuarioRepo = new Mock<IUsuarioRepository>();
        var usuario = new Usuario("Juan", "Pérez", "1234567890", "juan@test.com", "hashed", EstadosUsuario.Activo);
        usuarioRepo.Setup(r => r.BuscarPorEmail("juan@test.com")).ReturnsAsync(usuario);

        var encoder = new Mock<IPasswordEncoder>();
        encoder.Setup(e => e.CompararPassword("Password123", "hashed")).Returns(true);

        var sesionRepo = new Mock<ISesionRepository>();
        var jwt = new Mock<IJwtTokenService>();
        jwt.Setup(j => j.GenerarAccessToken(It.IsAny<Usuario>())).Returns("access");
        jwt.Setup(j => j.GenerarRefreshToken()).Returns("refresh");

        var uc = new IniciarSesionUseCase(usuarioRepo.Object, sesionRepo.Object, encoder.Object, jwt.Object);
        var controller = new AuthController(
            new Mock<RegistrarUsuarioUseCase>(usuarioRepo.Object, encoder.Object).Object,
            uc,
            new Mock<CerrarSesionUseCase>(sesionRepo.Object).Object,
            new Mock<SolicitarRecuperacionPasswordUseCase>(usuarioRepo.Object, new Mock<ITokenRepository>().Object, new Mock<IEmailService>().Object).Object,
            new Mock<RestablecerPasswordUseCase>(usuarioRepo.Object, new Mock<ITokenRepository>().Object, sesionRepo.Object, encoder.Object).Object,
            sesionRepo.Object,
            jwt.Object);

        var result = await controller.Login(new LoginDto { Email = "juan@test.com", Password = "Password123" });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        var tokens = Assert.IsType<TokenResponseDto>(ok.Value);
        Assert.Equal("access", tokens.AccessToken);
    }

    // ForgotPassword retorna 200 con mensaje genérico
    [Fact]
    public async Task ForgotPassword_SiempreRetorna200()
    {
        var usuarioRepo = new Mock<IUsuarioRepository>();
        usuarioRepo.Setup(r => r.BuscarPorEmail(It.IsAny<string>())).ReturnsAsync((Usuario?)null);

        var uc = new SolicitarRecuperacionPasswordUseCase(
            usuarioRepo.Object, new Mock<ITokenRepository>().Object, new Mock<IEmailService>().Object);

        var encoder = new Mock<IPasswordEncoder>();
        var controller = new AuthController(
            new Mock<RegistrarUsuarioUseCase>(usuarioRepo.Object, encoder.Object).Object,
            new Mock<IniciarSesionUseCase>(usuarioRepo.Object, new Mock<ISesionRepository>().Object, encoder.Object, new Mock<IJwtTokenService>().Object).Object,
            new Mock<CerrarSesionUseCase>(new Mock<ISesionRepository>().Object).Object,
            uc,
            new Mock<RestablecerPasswordUseCase>(usuarioRepo.Object, new Mock<ITokenRepository>().Object, new Mock<ISesionRepository>().Object, encoder.Object).Object,
            new Mock<ISesionRepository>().Object,
            new Mock<IJwtTokenService>().Object);

        var result = await controller.ForgotPassword(new RecuperacionPasswordDto { Email = "noexiste@test.com" });

        Assert.IsType<OkObjectResult>(result);
    }
}
