using System.ComponentModel.DataAnnotations;

namespace TechSupport.MVC.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [Display(Name = "Usuario")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseModel
{
    public bool Success { get; set; }
    public string Token { get; set; } = string.Empty;
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
