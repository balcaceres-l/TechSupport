using System.Text.Json.Serialization;
using TechSupport.API.Enums;

namespace TechSupport.API.Models;

public class Usuario
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Rol Rol { get; set; }

    [JsonIgnore]
    public ICollection<Incidente> IncidentesReportados { get; set; } = new List<Incidente>();

    [JsonIgnore]
    public ICollection<Incidente> IncidentesAsignados { get; set; } = new List<Incidente>();
}
