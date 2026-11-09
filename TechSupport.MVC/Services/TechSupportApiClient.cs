using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using TechSupport.MVC.Models;

namespace TechSupport.MVC.Services;

public class TechSupportApiClient : ITechSupportApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TechSupportApiClient> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public TechSupportApiClient(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ILogger<TechSupportApiClient> logger)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;

        var baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5018";
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    private async Task AttachBearerTokenAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var token = await httpContext.GetTokenAsync("access_token")
                        ?? httpContext.User.FindFirst("access_token")?.Value;

            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
    }

    public async Task<(bool Success, LoginResponseModel? Data, string? Error)> LoginAsync(string username, string password)
    {
        try
        {
            var payload = new { nombreUsuario = username, password };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/auth/login", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                try
                {
                    using var doc = JsonDocument.Parse(responseBody);
                    if (doc.RootElement.TryGetProperty("message", out var msgElement))
                    {
                        return (false, null, msgElement.GetString());
                    }
                }
                catch { }

                return (false, null, "Credenciales incorrectas o servidor no disponible.");
            }

            var result = JsonSerializer.Deserialize<LoginResponseModel>(responseBody, _jsonOptions);
            return (true, result, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al conectar con el servicio de autenticación.");
            return (false, null, "No se pudo conectar con el servidor de la API.");
        }
    }

    public async Task<List<IncidenteViewModel>> GetIncidentesAsync()
    {
        await AttachBearerTokenAsync();
        try
        {
            var response = await _httpClient.GetAsync("/api/incidentes");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<IncidenteViewModel>>(content, _jsonOptions) ?? new List<IncidenteViewModel>();
            }
            _logger.LogWarning("Error al obtener incidentes: Status {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al consultar incidentes.");
        }
        return new List<IncidenteViewModel>();
    }

    public async Task<IncidenteViewModel?> GetIncidenteByIdAsync(int id)
    {
        await AttachBearerTokenAsync();
        try
        {
            var response = await _httpClient.GetAsync($"/api/incidentes/{id}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IncidenteViewModel>(content, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al consultar incidente con ID {Id}", id);
        }
        return null;
    }

    public async Task<(bool Success, string? Error)> CreateIncidenteAsync(CreateIncidenteViewModel model)
    {
        await AttachBearerTokenAsync();
        try
        {
            var payload = new
            {
                titulo = model.Titulo,
                descripcion = model.Descripcion,
                prioridad = model.Prioridad
            };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/incidentes", content);
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            try
            {
                using var doc = JsonDocument.Parse(errorBody);
                if (doc.RootElement.TryGetProperty("message", out var msgElement))
                {
                    return (false, msgElement.GetString());
                }
            }
            catch { }

            return (false, $"Error al crear incidente (HTTP {response.StatusCode}).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al crear incidente.");
            return (false, "Error de comunicación con el servicio.");
        }
    }

    public async Task<(bool Success, string? Error)> AsignarTecnicoAsync(int incidenteId, int tecnicoId)
    {
        await AttachBearerTokenAsync();
        try
        {
            var payload = new { tecnicoId };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PatchAsync($"/api/incidentes/{incidenteId}/asignar", content);

            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            return (false, ExtraerMensajeError(errorBody, "Error al asignar técnico."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al asignar técnico.");
            return (false, "Error de comunicación al asignar técnico.");
        }
    }

    public async Task<(bool Success, string? Error)> CambiarEstadoAsync(int incidenteId, string nuevoEstado)
    {
        await AttachBearerTokenAsync();
        try
        {
            var payload = new { nuevoEstado };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PatchAsync($"/api/incidentes/{incidenteId}/estado", content);

            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            return (false, ExtraerMensajeError(errorBody, "Error al cambiar estado."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al cambiar estado.");
            return (false, "Error de comunicación al actualizar estado.");
        }
    }

    public async Task<(bool Success, string? Error)> EliminarIncidenteAsync(int incidenteId)
    {
        await AttachBearerTokenAsync();
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/incidentes/{incidenteId}");
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            return (false, ExtraerMensajeError(errorBody, "Error al eliminar incidente."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al eliminar incidente.");
            return (false, "Error de comunicación al eliminar incidente.");
        }
    }

    public async Task<List<UsuarioSimpleViewModel>> GetTecnicosAsync()
    {
        await AttachBearerTokenAsync();
        try
        {
            var response = await _httpClient.GetAsync("/api/usuarios/tecnicos");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<UsuarioSimpleViewModel>>(content, _jsonOptions) ?? new List<UsuarioSimpleViewModel>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al obtener lista de técnicos.");
        }
        return new List<UsuarioSimpleViewModel>();
    }

    private static string ExtraerMensajeError(string json, string fallback)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("message", out var msgElement))
            {
                return msgElement.GetString() ?? fallback;
            }
        }
        catch { }
        return fallback;
    }
}
