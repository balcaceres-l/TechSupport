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
    [Required]
    [MaxLength(200)]
    public string Titulo { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Descripcion { get; set; } = string.Empty;

    public Prioridad Prioridad { get; set; } = Prioridad.Media;

    // En Fase 1 (sin login activo), permitimos enviar UsuarioId opcional o requerido; por defecto 3 (usuario estandar)
    public int UsuarioId { get; set; } = 3;
}
