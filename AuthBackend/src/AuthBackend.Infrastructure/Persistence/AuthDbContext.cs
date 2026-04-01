using AuthBackend.Domain.Entities;
using AuthBackend.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace AuthBackend.Infrastructure.Persistence;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    public DbSet<EstadoUsuario> EstadosUsuario => Set<EstadoUsuario>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Sesion> Sesiones => Set<Sesion>();
    public DbSet<TokenRecuperacion> TokensRecuperacion => Set<TokenRecuperacion>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<UserPresence> UserPresence => Set<UserPresence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new EstadoUsuarioConfiguration());
        modelBuilder.ApplyConfiguration(new UsuarioConfiguration());
        modelBuilder.ApplyConfiguration(new SesionConfiguration());
        modelBuilder.ApplyConfiguration(new TokenRecuperacionConfiguration());
        modelBuilder.ApplyConfiguration(new AttendanceRecordConfiguration());
        modelBuilder.ApplyConfiguration(new UserPresenceConfiguration());
    }
}
