using System.Net;
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
                var errorMsg = ParseErrorMessage(response, responseBody, "Usuario o contraseña incorrectos.");
                return (false, null, errorMsg);
            }

            var result = JsonSerializer.Deserialize<LoginResponseModel>(responseBody, _jsonOptions);
            return (true, result, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al conectar con el servicio de autenticación.");
            return (false, null, "No se pudo establecer conexión con el servidor de la API.");
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

            _logger.LogWarning("Error al consultar incidentes: HTTP {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al consultar lista de incidentes.");
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

            _logger.LogWarning("Error al consultar incidente #{Id}: HTTP {StatusCode}", id, response.StatusCode);
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
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var error = ParseErrorMessage(response, responseBody, $"Error al crear incidente (HTTP {response.StatusCode}).");
            return (false, error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al crear incidente.");
            return (false, "Error de comunicación con el servicio de la API.");
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
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            return (false, ParseErrorMessage(response, responseBody, "Error al asignar técnico."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al asignar técnico.");
            return (false, "Error de comunicación al contactar con la API.");
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
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            return (false, ParseErrorMessage(response, responseBody, "Error al cambiar estado."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al cambiar estado.");
            return (false, "Error de comunicación con la API al actualizar estado.");
        }
    }

    public async Task<(bool Success, string? Error)> EliminarIncidenteAsync(int incidenteId)
    {
        await AttachBearerTokenAsync();
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/incidentes/{incidenteId}");
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            return (false, ParseErrorMessage(response, responseBody, "Error al eliminar incidente."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al eliminar incidente.");
            return (false, "Error de comunicación con la API al eliminar incidente.");
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
            _logger.LogError(ex, "Excepción al consultar lista de técnicos.");
        }
        return new List<UsuarioSimpleViewModel>();
    }

    private static string ParseErrorMessage(HttpResponseMessage response, string responseBody, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.TryGetProperty("message", out var msgElement) && !string.IsNullOrWhiteSpace(msgElement.GetString()))
                {
                    return msgElement.GetString()!;
                }
                if (doc.RootElement.TryGetProperty("title", out var titleElement) && !string.IsNullOrWhiteSpace(titleElement.GetString()))
                {
                    return titleElement.GetString()!;
                }
            }
            catch { }
        }

        return response.StatusCode switch
        {
            HttpStatusCode.BadRequest => "Solicitud inválida. Verifique que cumple con las reglas de negocio.",
            HttpStatusCode.Unauthorized => "Sesión inválida o expirada. Por favor, vuelva a iniciar sesión.",
            HttpStatusCode.Forbidden => "Acceso denegado: no cuenta con los permisos necesarios para realizar esta acción.",
            HttpStatusCode.NotFound => "El elemento solicitado no fue encontrado en el sistema.",
            HttpStatusCode.InternalServerError => "Error interno en el servidor. Por favor comuníquese con el administrador.",
            _ => fallback
        };
    }
}
