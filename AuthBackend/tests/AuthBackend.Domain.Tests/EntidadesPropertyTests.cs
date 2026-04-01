using AuthBackend.Domain.Entities;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Xunit;

namespace AuthBackend.Domain.Tests;

// Feature: sistema-autenticacion — Property tests para entidades de dominio
// Validates: Requirements 2.3, 2.4, 4.1, 4.3, 5.1
public class EntidadesPropertyTests
{
    // Property 8: Sesion creada con 7 días debe tener FechaExpiracion correcta
    [Property(MaxTest = 100)]
    public Property Sesion_FechaExpiracion_EsSieteDiasDespues()
    {
        return Prop.ForAll<Guid, string>(
            ArbMap.Default.ArbFor<Guid>(),
            ArbMap.Default.ArbFor<string>().Filter(s => s != null),
            (usuarioId, token) =>
            {
                var antes = DateTime.UtcNow;
                var expiracion = DateTime.UtcNow.AddDays(7);
                var sesion = new Sesion(usuarioId, token, expiracion);
                var despues = DateTime.UtcNow;

                return sesion.FechaExpiracion >= antes.AddDays(7) &&
                       sesion.FechaExpiracion <= despues.AddDays(7);
            });
    }

    // Property 12: Después de Revocar(), EstaVigente() debe retornar false
    [Property(MaxTest = 100)]
    public Property Sesion_DespuesDeRevocar_NoEstaVigente()
    {
        return Prop.ForAll<Guid, string>(
            ArbMap.Default.ArbFor<Guid>(),
            ArbMap.Default.ArbFor<string>().Filter(s => s != null),
            (usuarioId, token) =>
            {
                var sesion = new Sesion(usuarioId, token, DateTime.UtcNow.AddDays(7));
                sesion.Revocar();

                return !sesion.EstaVigente() &&
                       sesion.Revocado &&
                       sesion.FechaRevocacion.HasValue;
            });
    }

    // Property 14: Token revocado nunca puede estar vigente
    [Property(MaxTest = 100)]
    public Property Sesion_Revocada_NuncaEstaVigente()
    {
        return Prop.ForAll<Guid, string>(
            ArbMap.Default.ArbFor<Guid>(),
            ArbMap.Default.ArbFor<string>().Filter(s => s != null),
            (usuarioId, token) =>
            {
                var sesion = new Sesion(usuarioId, token, DateTime.UtcNow.AddDays(7));
                sesion.Revocar();

                // Llamar múltiples veces no cambia el resultado
                return !sesion.EstaVigente() && !sesion.EstaVigente() && !sesion.EstaVigente();
            });
    }

    // Property 15: TokenRecuperacion expira exactamente 1 hora después de creación
    [Property(MaxTest = 100)]
    public Property TokenRecuperacion_ExpiraEnUnaHora()
    {
        return Prop.ForAll<Guid, string>(
            ArbMap.Default.ArbFor<Guid>(),
            ArbMap.Default.ArbFor<string>().Filter(s => s != null),
            (usuarioId, token) =>
            {
                var antes = DateTime.UtcNow;
                var tokenRec = new TokenRecuperacion(usuarioId, token);
                var despues = DateTime.UtcNow;

                var expiracionMinima = antes.AddHours(1);
                var expiracionMaxima = despues.AddHours(1);

                return tokenRec.FechaExpiracion >= expiracionMinima &&
                       tokenRec.FechaExpiracion <= expiracionMaxima;
            });
    }

    // Test adicional: EstaActivo() retorna true solo cuando EstadoId == Activo
    [Fact]
    public void Usuario_EstaActivo_RetornaTrueSoloParaEstadoActivo()
    {
        var usuarioActivo = new Usuario("Juan", "Pérez", "1234567890", "juan@test.com", "hash", EstadosUsuario.Activo);
        var usuarioInactivo = new Usuario("Juan", "Pérez", "1234567890", "juan@test.com", "hash", EstadosUsuario.Inactivo);
        var usuarioSuspendido = new Usuario("Juan", "Pérez", "1234567890", "juan@test.com", "hash", EstadosUsuario.Suspendido);

        Assert.True(usuarioActivo.EstaActivo());
        Assert.False(usuarioInactivo.EstaActivo());
        Assert.False(usuarioSuspendido.EstaActivo());
    }

    // Test: Usuario nuevo tiene EstadoId Activo por defecto
    [Fact]
    public void Usuario_NuevoRegistro_TieneEstadoActivoPorDefecto()
    {
        var usuario = new Usuario("Ana", "García", "0987654321", "ana@test.com", "hash");

        Assert.Equal(EstadosUsuario.Activo, usuario.EstadoId);
        Assert.True(usuario.EstaActivo());
    }

    // Test: ActualizarPassword cambia el hash y establece FechaActualizacion
    [Fact]
    public void Usuario_ActualizarPassword_CambiaHashYFecha()
    {
        var usuario = new Usuario("Ana", "García", "0987654321", "ana@test.com", "hash_original");
        var antes = DateTime.UtcNow;

        usuario.ActualizarPassword("nuevo_hash");

        Assert.Equal("nuevo_hash", usuario.PasswordHash);
        Assert.NotNull(usuario.FechaActualizacion);
        Assert.True(usuario.FechaActualizacion >= antes);
    }

    // Test: TokenRecuperacion.EsValido() retorna false después de MarcarComoUsado
    [Fact]
    public void TokenRecuperacion_DespuesDeMarcarUsado_NoEsValido()
    {
        var token = new TokenRecuperacion(Guid.NewGuid(), "abc123");
        Assert.True(token.EsValido());

        token.MarcarComoUsado();

        Assert.False(token.EsValido());
        Assert.True(token.Usado);
        Assert.NotNull(token.FechaUso);
    }
}
