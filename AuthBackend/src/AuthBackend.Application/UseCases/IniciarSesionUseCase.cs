using AuthBackend.Application.DTOs;
using AuthBackend.Domain.Entities;
using AuthBackend.Domain.Exceptions;
using AuthBackend.Domain.Interfaces;

namespace AuthBackend.Application.UseCases;

public class IniciarSesionUseCase
{
    private readonly IUsuarioRepository _usuarioRepository; //Busca usuarios en la base de datos
    private readonly ISesionRepository _sesionRepository; //Guarda sesiones (refresh tokens)
    private readonly IPasswordEncoder _passwordEncoder; //Compara contraseñas
    private readonly IJwtTokenService _jwtTokenService; //Genera tokens

    public IniciarSesionUseCase(
        IUsuarioRepository usuarioRepository,
        ISesionRepository sesionRepository,
        IPasswordEncoder passwordEncoder,
        IJwtTokenService jwtTokenService)
    {
        _usuarioRepository = usuarioRepository;
        _sesionRepository = sesionRepository;
        _passwordEncoder = passwordEncoder;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<TokenResponseDto> Ejecutar(LoginDto dto)
    {
        var usuario = await _usuarioRepository.BuscarPorEmail(dto.Email);

        if (usuario == null || !_passwordEncoder.CompararPassword(dto.Password, usuario.PasswordHash))
            throw new CredencialesInvalidasException("Credenciales inválidas");

        if (!usuario.EstaActivo()) 
            throw new UsuarioInactivoException("La cuenta de usuario no está activa");

        var accessToken = _jwtTokenService.GenerarAccessToken(usuario); //Para autenticacion (corta duracion)
        var refreshToken = _jwtTokenService.GenerarRefreshToken(); //Para renovar sesion

        var sesion = new Sesion(usuario.Id, refreshToken, DateTime.UtcNow.AddDays(7));
        await _sesionRepository.GuardarSesion(sesion);

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 900//segundos - 15min
        };
    }
}
