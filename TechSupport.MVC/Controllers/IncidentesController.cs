using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechSupport.MVC.Models;
using TechSupport.MVC.Services;

namespace TechSupport.MVC.Controllers;

[Authorize]
public class IncidentesController : Controller
{
    private readonly ITechSupportApiClient _apiClient;
    private readonly ILogger<IncidentesController> _logger;

    public IncidentesController(ITechSupportApiClient apiClient, ILogger<IncidentesController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    // GET: /Incidentes
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var incidentes = await _apiClient.GetIncidentesAsync();
        return View(incidentes);
    }

    // GET: /Incidentes/Details/5
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var incidente = await _apiClient.GetIncidenteByIdAsync(id);
        if (incidente == null)
        {
            TempData["ErrorMessage"] = "No se encontró el incidente solicitado o no tienes permiso para visualizarlo.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Tecnicos = await _apiClient.GetTecnicosAsync();
        return View(incidente);
    }

    // GET: /Incidentes/Create
    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateIncidenteViewModel());
    }

    // POST: /Incidentes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateIncidenteViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (success, error) = await _apiClient.CreateIncidenteAsync(model);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Error al registrar el incidente.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Incidente registrado exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Incidentes/AsignarTecnico
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AsignarTecnico(int incidenteId, int tecnicoId)
    {
        var (success, error) = await _apiClient.AsignarTecnicoAsync(incidenteId, tecnicoId);
        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "No se pudo asignar el técnico.";
        }
        else
        {
            TempData["SuccessMessage"] = "Técnico asignado correctamente. El estado pasó a 'Asignado'.";
        }

        return RedirectToAction(nameof(Details), new { id = incidenteId });
    }

    // POST: /Incidentes/CambiarEstado
    [HttpPost]
    [Authorize(Roles = "Administrador,Tecnico")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int incidenteId, string nuevoEstado)
    {
        var (success, error) = await _apiClient.CambiarEstadoAsync(incidenteId, nuevoEstado);
        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "No se pudo cambiar el estado.";
        }
        else
        {
            TempData["SuccessMessage"] = $"El estado del incidente fue actualizado a '{nuevoEstado}' con éxito.";
        }

        return RedirectToAction(nameof(Details), new { id = incidenteId });
    }

    // POST: /Incidentes/Eliminar/5
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var (success, error) = await _apiClient.EliminarIncidenteAsync(id);
        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "No se pudo eliminar el incidente.";
        }
        else
        {
            TempData["SuccessMessage"] = $"Incidente #{id} eliminado correctamente.";
        }

        return RedirectToAction(nameof(Index));
    }
}
