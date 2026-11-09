using System.ComponentModel.DataAnnotations;

namespace TechSupport.API.DTOs;

public class LoginRequestDto
{
    [Required]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public bool Success { get; set; } = true;
    public string Token { get; set; } = string.Empty;
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
