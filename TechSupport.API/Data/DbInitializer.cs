using TechSupport.API.Enums;
using TechSupport.API.Models;

namespace TechSupport.API.Data;

public static class DbInitializer
{
    public static void Initialize(AppDbContext context)
    {
        context.Database.EnsureCreated();

        var usuariosIniciales = new List<Usuario>
        {
            // Administradores
            new Usuario
            {
                NombreUsuario = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123*"),
                Rol = Rol.Administrador
            },
            new Usuario
            {
                NombreUsuario = "admin2",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123*"),
                Rol = Rol.Administrador
            },

            // Técnicos (múltiples para probar asignación en dropdowns)
            new Usuario
            {
                NombreUsuario = "tecnico",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Tecnico123*"),
                Rol = Rol.Tecnico
            },
            new Usuario
            {
                NombreUsuario = "tecnico2",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Tecnico123*"),
                Rol = Rol.Tecnico
            },
            new Usuario
            {
                NombreUsuario = "tecnico3",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Tecnico123*"),
                Rol = Rol.Tecnico
            },

            // Usuarios finales (para probar aislamiento de tickets creados)
            new Usuario
            {
                NombreUsuario = "usuario",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Usuario123*"),
                Rol = Rol.Usuario
            },
            new Usuario
            {
                NombreUsuario = "usuario2",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Usuario123*"),
                Rol = Rol.Usuario
            },
            new Usuario
            {
                NombreUsuario = "usuario3",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Usuario123*"),
                Rol = Rol.Usuario
            }
        };

        bool huboCambios = false;
        foreach (var nuevoUsuario in usuariosIniciales)
        {
            if (!context.Usuarios.Any(u => u.NombreUsuario.ToLower() == nuevoUsuario.NombreUsuario.ToLower()))
            {
                context.Usuarios.Add(nuevoUsuario);
                huboCambios = true;
            }
        }

        if (huboCambios)
        {
            context.SaveChanges();
        }
    }
}
