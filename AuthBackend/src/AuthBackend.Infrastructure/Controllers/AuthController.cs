using AuthBackend.Application.DTOs;
using AuthBackend.Application.UseCases;
using AuthBackend.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthBackend.Infrastructure.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RegistrarUsuarioUseCase _registrar;
    private readonly IniciarSesionUseCase _iniciarSesion;
    private readonly CerrarSesionUseCase _cerrarSesion;
    private readonly SolicitarRecuperacionPasswordUseCase _solicitarRecuperacion;
    private readonly RestablecerPasswordUseCase _restablecerPassword;
    private readonly ISesionRepository _sesionRepository;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(
        RegistrarUsuarioUseCase registrar,
        IniciarSesionUseCase iniciarSesion,
        CerrarSesionUseCase cerrarSesion,
        SolicitarRecuperacionPasswordUseCase solicitarRecuperacion,
        RestablecerPasswordUseCase restablecerPassword,
        ISesionRepository sesionRepository,
        IJwtTokenService jwtTokenService)
    {
        _registrar = registrar;
        _iniciarSesion = iniciarSesion;
        _cerrarSesion = cerrarSesion;
        _solicitarRecuperacion = solicitarRecuperacion;
        _restablecerPassword = restablecerPassword;
        _sesionRepository = sesionRepository;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegistroUsuarioDto dto)
    {
        var usuario = await _registrar.Ejecutar(dto);
        return StatusCode(201, new UsuarioResponseDto(usuario));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var tokens = await _iniciarSesion.Ejecutar(dto);
        return Ok(tokens);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
    {
        var sesion = await _sesionRepository.BuscarPorToken(dto.RefreshToken);
        if (sesion == null || !sesion.EstaVigente())
            return Unauthorized(new { error = "Token de refresco inválido o expirado" });

        var usuarioRepo = HttpContext.RequestServices
            .GetRequiredService<Domain.Interfaces.IUsuarioRepository>();
        var usuario = await usuarioRepo.BuscarPorId(sesion.UsuarioId);

        if (usuario == null)
            return Unauthorized(new { error = "Usuario no encontrado" });

        // Rotate refresh token: revoke old, issue new
        await _sesionRepository.FinalizarSesion(dto.RefreshToken);
        var newRefreshToken = _jwtTokenService.GenerarRefreshToken();
        var nuevaSesion = new Domain.Entities.Sesion(usuario.Id, newRefreshToken, DateTime.UtcNow.AddDays(7));
        await _sesionRepository.GuardarSesion(nuevaSesion);

        var newAccessToken = _jwtTokenService.GenerarAccessToken(usuario);
        return Ok(new { accessToken = newAccessToken, refreshToken = newRefreshToken, expiresIn = 900 });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutDto dto)
    {
        await _cerrarSesion.Ejecutar(dto.RefreshToken);
        return Ok(new { message = "Sesión cerrada correctamente" });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] RecuperacionPasswordDto dto)
    {
        await _solicitarRecuperacion.Ejecutar(dto);
        return Ok(new { message = "Si el email está registrado, recibirás instrucciones para restablecer tu contraseña" });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] RestablecerPasswordDto dto)
    {
        await _restablecerPassword.Ejecutar(dto);
        return Ok(new { message = "Contraseña restablecida correctamente" });
    }
}
