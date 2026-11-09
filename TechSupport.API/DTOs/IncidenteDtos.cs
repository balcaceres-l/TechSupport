using System.ComponentModel.DataAnnotations;
using TechSupport.API.Enums;

namespace TechSupport.API.DTOs;

public class IncidenteDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public Prioridad Prioridad { get; set; }
    public Estado Estado { get; set; }
    public int UsuarioId { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public int? TecnicoId { get; set; }
    public string? NombreTecnico { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaResolucion { get; set; }
}

public class CreateIncidenteDto
{
    [Required(ErrorMessage = "El título es obligatorio.")]
    [MaxLength(200, ErrorMessage = "El título no puede superar los 200 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [MaxLength(2000, ErrorMessage = "La descripción no puede superar los 2000 caracteres.")]
    public string Descripcion { get; set; } = string.Empty;

    public Prioridad Prioridad { get; set; } = Prioridad.Media;
}

public class AsignarTecnicoDto
{
    [Required(ErrorMessage = "El ID del técnico es obligatorio.")]
    public int TecnicoId { get; set; }
}

public class CambiarEstadoDto
{
    [Required(ErrorMessage = "El nuevo estado es obligatorio.")]
    public Estado NuevoEstado { get; set; }
}

public class UsuarioDto
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public Rol Rol { get; set; }
}
