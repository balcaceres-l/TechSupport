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
            TempData["ErrorMessage"] = "No se encontró el incidente solicitado.";
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
}
