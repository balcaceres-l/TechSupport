using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechSupport.API.Data;
using TechSupport.API.DTOs;
using TechSupport.API.Enums;
using TechSupport.API.Models;

namespace TechSupport.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class IncidentesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<IncidentesController> _logger;

    public IncidentesController(AppDbContext context, ILogger<IncidentesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private (int UserId, string Role, string Username)? GetCurrentUser()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("id")?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("rol")?.Value;
        var name = User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("nombre")?.Value;

        if (int.TryParse(idStr, out int userId) && !string.IsNullOrEmpty(role))
        {
            return (userId, role, name ?? "");
        }
        return null;
    }

    // GET: api/incidentes
    // Administrador: ve todos los incidentes
    // Tecnico: ve solo los asignados a él
    // Usuario: ve solo los que él creó
    [HttpGet]
    public async Task<ActionResult<IEnumerable<IncidenteDto>>> GetAll()
    {
        var current = GetCurrentUser();
        if (current == null)
        {
            return Unauthorized(new { message = "Token de autenticación inválido o sin claims requeridos." });
        }

        var query = _context.Incidentes
            .Include(i => i.Usuario)
            .Include(i => i.Tecnico)
            .AsQueryable();

        if (current.Value.Role == Rol.Administrador.ToString())
        {
            // Admin ve todos
        }
        else if (current.Value.Role == Rol.Tecnico.ToString())
        {
            // Técnico solo asignados a él
            query = query.Where(i => i.TecnicoId == current.Value.UserId);
        }
        else
        {
            // Usuario solo los creados por él
            query = query.Where(i => i.UsuarioId == current.Value.UserId);
        }

        var incidentes = await query
            .OrderByDescending(i => i.FechaCreacion)
            .Select(i => new IncidenteDto
            {
                Id = i.Id,
                Titulo = i.Titulo,
                Descripcion = i.Descripcion,
                Prioridad = i.Prioridad,
                Estado = i.Estado,
                UsuarioId = i.UsuarioId,
                NombreUsuario = i.Usuario != null ? i.Usuario.NombreUsuario : string.Empty,
                TecnicoId = i.TecnicoId,
                NombreTecnico = i.Tecnico != null ? i.Tecnico.NombreUsuario : null,
                FechaCreacion = i.FechaCreacion,
                FechaResolucion = i.FechaResolucion
            })
            .ToListAsync();

        return Ok(incidentes);
    }

    // GET: api/incidentes/5
    [HttpGet("{id}")]
    public async Task<ActionResult<IncidenteDto>> GetById(int id)
    {
        var current = GetCurrentUser();
        if (current == null)
        {
            return Unauthorized(new { message = "Token de autenticación inválido." });
        }

        var incidente = await _context.Incidentes
            .Include(i => i.Usuario)
            .Include(i => i.Tecnico)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (incidente == null)
        {
            return NotFound(new { message = $"No se encontró el incidente con ID {id}." });
        }

        // Validación de permisos de lectura por rol
        if (current.Value.Role == Rol.Tecnico.ToString() && incidente.TecnicoId != current.Value.UserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "No tienes permiso para ver este incidente (no te está asignado)." });
        }

        if (current.Value.Role == Rol.Usuario.ToString() && incidente.UsuarioId != current.Value.UserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "No tienes permiso para ver este incidente (no lo creaste tú)." });
        }

        var dto = new IncidenteDto
        {
            Id = incidente.Id,
            Titulo = incidente.Titulo,
            Descripcion = incidente.Descripcion,
            Prioridad = incidente.Prioridad,
            Estado = incidente.Estado,
            UsuarioId = incidente.UsuarioId,
            NombreUsuario = incidente.Usuario != null ? incidente.Usuario.NombreUsuario : string.Empty,
            TecnicoId = incidente.TecnicoId,
            NombreTecnico = incidente.Tecnico != null ? incidente.Tecnico.NombreUsuario : null,
            FechaCreacion = incidente.FechaCreacion,
            FechaResolucion = incidente.FechaResolucion
        };

        return Ok(dto);
    }

    // POST: api/incidentes
    // Crear incidente: Todos los usuarios autenticados.
    // UsuarioId se toma SIEMPRE del claim del JWT, nunca del cuerpo de la petición.
    [HttpPost]
    public async Task<ActionResult<IncidenteDto>> Create([FromBody] CreateIncidenteDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var current = GetCurrentUser();
        if (current == null)
        {
            return Unauthorized(new { message = "Token de autenticación inválido." });
        }

        var incidente = new Incidente
        {
            Titulo = dto.Titulo.Trim(),
            Descripcion = dto.Descripcion.Trim(),
            Prioridad = dto.Prioridad,
            Estado = Estado.Pendiente,
            UsuarioId = current.Value.UserId, // Asignado exclusivamente desde el claim JWT
            FechaCreacion = DateTime.UtcNow
        };

        _context.Incidentes.Add(incidente);
        await _context.SaveChangesAsync();

        var resultadoDto = new IncidenteDto
        {
            Id = incidente.Id,
            Titulo = incidente.Titulo,
            Descripcion = incidente.Descripcion,
            Prioridad = incidente.Prioridad,
            Estado = incidente.Estado,
            UsuarioId = incidente.UsuarioId,
            NombreUsuario = current.Value.Username,
            TecnicoId = null,
            NombreTecnico = null,
            FechaCreacion = incidente.FechaCreacion,
            FechaResolucion = null
        };

        return CreatedAtAction(nameof(GetById), new { id = incidente.Id }, resultadoDto);
    }
}
