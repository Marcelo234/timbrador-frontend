using AuthBackend.Application.DTOs;
using AuthBackend.Domain.Entities;
using AuthBackend.Domain.Interfaces;

namespace AuthBackend.Application.UseCases;

public class SolicitarRecuperacionPasswordUseCase
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ITokenRepository _tokenRepository;
    private readonly IEmailService _emailService;

    public SolicitarRecuperacionPasswordUseCase(
        IUsuarioRepository usuarioRepository,
        ITokenRepository tokenRepository,
        IEmailService emailService)
    {
        _usuarioRepository = usuarioRepository;
        _tokenRepository = tokenRepository;
        _emailService = emailService;
    }

    public async Task Ejecutar(RecuperacionPasswordDto dto)
    {
        var usuario = await _usuarioRepository.BuscarPorEmail(dto.Email);

        if (usuario != null)
        {
            var token = Guid.NewGuid().ToString("N");
            var tokenRecuperacion = new TokenRecuperacion(usuario.Id, token);
            await _tokenRepository.GuardarToken(tokenRecuperacion);
            await _emailService.EnviarRecuperacionPassword(dto.Email, token);
        }
        // Siempre retornar éxito para no revelar si el email existe
    }
}
