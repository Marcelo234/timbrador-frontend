using AuthBackend.Application.DTOs;
using AuthBackend.Domain.Entities;
using AuthBackend.Domain.Exceptions;
using AuthBackend.Domain.Interfaces;

namespace AuthBackend.Application.UseCases;

public class RegistrarUsuarioUseCase
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordEncoder _passwordEncoder;

    public RegistrarUsuarioUseCase(IUsuarioRepository usuarioRepository, IPasswordEncoder passwordEncoder)
    {
        _usuarioRepository = usuarioRepository;
        _passwordEncoder = passwordEncoder;
    }

    public async Task<Usuario> Ejecutar(RegistroUsuarioDto dto)
    {
        if (await _usuarioRepository.ExisteUsuario(dto.Email))
            throw new UsuarioYaExisteException($"El email {dto.Email} ya está registrado");

        var passwordHash = _passwordEncoder.HashPassword(dto.Password);

        var usuario = new Usuario(
            dto.Nombres,
            dto.Apellidos,
            dto.Cedula,
            dto.Email,
            passwordHash,
            EstadosUsuario.Activo
        );

        await _usuarioRepository.GuardarUsuario(usuario);
        return usuario;
    }
}
