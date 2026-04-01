using AuthBackend.Application.DTOs;
using AuthBackend.Domain.Exceptions;
using AuthBackend.Domain.Interfaces;

namespace AuthBackend.Application.UseCases;

public class RestablecerPasswordUseCase
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ITokenRepository _tokenRepository;
    private readonly ISesionRepository _sesionRepository;
    private readonly IPasswordEncoder _passwordEncoder;

    public RestablecerPasswordUseCase(
        IUsuarioRepository usuarioRepository,
        ITokenRepository tokenRepository,
        ISesionRepository sesionRepository,
        IPasswordEncoder passwordEncoder)
    {
        _usuarioRepository = usuarioRepository;
        _tokenRepository = tokenRepository;
        _sesionRepository = sesionRepository;
        _passwordEncoder = passwordEncoder;
    }

    public async Task Ejecutar(RestablecerPasswordDto dto)
    {
        var tokenRecuperacion = await _tokenRepository.BuscarToken(dto.Token);

        if (tokenRecuperacion == null || !tokenRecuperacion.EsValido())
            throw new TokenInvalidoException("Token de recuperación inválido o expirado");

        var usuario = await _usuarioRepository.BuscarPorEmail(dto.Email);
        if (usuario == null || usuario.Id != tokenRecuperacion.UsuarioId)
            throw new TokenInvalidoException("Token no corresponde al usuario");

        var nuevoHash = _passwordEncoder.HashPassword(dto.NuevaPassword);
        usuario.ActualizarPassword(nuevoHash);
        await _usuarioRepository.ActualizarUsuario(usuario);

        tokenRecuperacion.MarcarComoUsado();
        await _tokenRepository.InvalidarToken(dto.Token);

        await _sesionRepository.RevocarTodasLasSesionesDelUsuario(usuario.Id);
    }
}
