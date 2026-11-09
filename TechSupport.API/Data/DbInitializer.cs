using TechSupport.API.Enums;
using TechSupport.API.Models;

namespace TechSupport.API.Data;

public static class DbInitializer
{
    public static void Initialize(AppDbContext context)
    {
        context.Database.EnsureCreated();

        if (context.Usuarios.Any())
        {
            return; // Ya fue sembrada la base de datos
        }

        var usuarios = new List<Usuario>
        {
            new Usuario
            {
                NombreUsuario = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123*"),
                Rol = Rol.Administrador
            },
            new Usuario
            {
                NombreUsuario = "tecnico",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Tecnico123*"),
                Rol = Rol.Tecnico
            },
            new Usuario
            {
                NombreUsuario = "usuario",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Usuario123*"),
                Rol = Rol.Usuario
            }
        };

        context.Usuarios.AddRange(usuarios);
        context.SaveChanges();
    }
}
