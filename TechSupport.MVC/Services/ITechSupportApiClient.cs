using TechSupport.MVC.Models;

namespace TechSupport.MVC.Services;

public interface ITechSupportApiClient
{
    Task<(bool Success, LoginResponseModel? Data, string? Error)> LoginAsync(string username, string password);
    Task<List<IncidenteViewModel>> GetIncidentesAsync();
    Task<IncidenteViewModel?> GetIncidenteByIdAsync(int id);
    Task<(bool Success, string? Error)> CreateIncidenteAsync(CreateIncidenteViewModel model);
    Task<(bool Success, string? Error)> AsignarTecnicoAsync(int incidenteId, int tecnicoId);
    Task<(bool Success, string? Error)> CambiarEstadoAsync(int incidenteId, string nuevoEstado);
    Task<(bool Success, string? Error)> EliminarIncidenteAsync(int incidenteId);
    Task<List<UsuarioSimpleViewModel>> GetTecnicosAsync();
}

public class UsuarioSimpleViewModel
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
}
