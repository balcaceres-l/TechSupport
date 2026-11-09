using System.ComponentModel.DataAnnotations;

namespace TechSupport.MVC.Models;

public class IncidenteViewModel
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Prioridad { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public int UsuarioId { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public int? TecnicoId { get; set; }
    public string? NombreTecnico { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaResolucion { get; set; }
}

public class CreateIncidenteViewModel
{
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(200, ErrorMessage = "El título no puede superar los 200 caracteres.")]
    [Display(Name = "Título")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(2000, ErrorMessage = "La descripción no puede superar los 2000 caracteres.")]
    [Display(Name = "Descripción")]
    public string Descripcion { get; set; } = string.Empty;

    [Required(ErrorMessage = "La prioridad es obligatoria.")]
    [Display(Name = "Prioridad")]
    public string Prioridad { get; set; } = "Media";
}
