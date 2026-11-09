using Microsoft.EntityFrameworkCore;
using TechSupport.API.Enums;
using TechSupport.API.Models;

namespace TechSupport.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Incidente> Incidentes => Set<Incidente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.NombreUsuario).IsRequired().HasMaxLength(100);
            entity.HasIndex(u => u.NombreUsuario).IsUnique();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Rol).HasConversion<string>();
        });

        modelBuilder.Entity<Incidente>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Titulo).IsRequired().HasMaxLength(200);
            entity.Property(i => i.Descripcion).IsRequired().HasMaxLength(2000);
            entity.Property(i => i.Prioridad).HasConversion<string>();
            entity.Property(i => i.Estado).HasConversion<string>();

            entity.HasOne(i => i.Usuario)
                  .WithMany(u => u.IncidentesReportados)
                  .HasForeignKey(i => i.UsuarioId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(i => i.Tecnico)
                  .WithMany(u => u.IncidentesAsignados)
                  .HasForeignKey(i => i.TecnicoId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
