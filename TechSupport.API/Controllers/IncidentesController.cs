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

        return Ok(MapToDto(incidente));
    }

    // POST: api/incidentes
    // Crear incidente: Todos los roles
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
            UsuarioId = current.Value.UserId, // Tomado siempre del claim JWT
            FechaCreacion = DateTime.UtcNow
        };

        _context.Incidentes.Add(incidente);
        await _context.SaveChangesAsync();

        incidente.Usuario = await _context.Usuarios.FindAsync(current.Value.UserId);

        return CreatedAtAction(nameof(GetById), new { id = incidente.Id }, MapToDto(incidente));
    }

    // PATCH: api/incidentes/{id}/asignar
    // Asignar técnico: Solo Administrador. Al asignar, el estado pasa a "Asignado". Solo se puede asignar a alguien con rol Técnico.
    [HttpPatch("{id}/asignar")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<IncidenteDto>> AsignarTecnico(int id, [FromBody] AsignarTecnicoDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var incidente = await _context.Incidentes
            .Include(i => i.Usuario)
            .Include(i => i.Tecnico)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (incidente == null)
        {
            return NotFound(new { message = $"No se encontró el incidente con ID {id}." });
        }

        if (incidente.Estado == Estado.Cerrado)
        {
            return BadRequest(new { message = "No se puede reasignar un incidente que ya se encuentra Cerrado." });
        }

        var tecnico = await _context.Usuarios.FindAsync(dto.TecnicoId);
        if (tecnico == null || tecnico.Rol != Rol.Tecnico)
        {
            return BadRequest(new { message = "El usuario seleccionado no existe o no tiene el rol de Técnico." });
        }

        incidente.TecnicoId = tecnico.Id;
        incidente.Tecnico = tecnico;
        incidente.Estado = Estado.Asignado;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Incidente #{Id} asignado al técnico {Tecnico}.", incidente.Id, tecnico.NombreUsuario);

        return Ok(MapToDto(incidente));
    }

    // PATCH: api/incidentes/{id}/estado
    // Cambiar estado: Admin, y Técnico (solo en sus asignados).
    // El estado solo puede avanzar al siguiente paso lógico. No se puede cerrar sin resolver. Llenar FechaResolucion al pasar a Resuelto.
    [HttpPatch("{id}/estado")]
    public async Task<ActionResult<IncidenteDto>> CambiarEstado(int id, [FromBody] CambiarEstadoDto dto)
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

        var incidente = await _context.Incidentes
            .Include(i => i.Usuario)
            .Include(i => i.Tecnico)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (incidente == null)
        {
            return NotFound(new { message = $"No se encontró el incidente con ID {id}." });
        }

        // Permisos de cambio de estado: Admin, y Técnico solo en sus asignados
        bool esAdmin = current.Value.Role == Rol.Administrador.ToString();
        bool esTecnicoAsignado = current.Value.Role == Rol.Tecnico.ToString() && incidente.TecnicoId == current.Value.UserId;

        if (!esAdmin && !esTecnicoAsignado)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "No tienes permiso para modificar el estado de este incidente." });
        }

        var estadoActual = incidente.Estado;
        var nuevoEstado = dto.NuevoEstado;

        // Validar avance estricto al siguiente paso lógico
        bool transicionValida = (estadoActual, nuevoEstado) switch
        {
            (Estado.Pendiente, Estado.Asignado) => true,
            (Estado.Asignado, Estado.EnProceso) => true,
            (Estado.EnProceso, Estado.Resuelto) => true,
            (Estado.Resuelto, Estado.Cerrado) => true,
            _ => false
        };

        if (!transicionValida)
        {
            return BadRequest(new
            {
                message = $"Transición no permitida: No se puede cambiar de '{estadoActual}' a '{nuevoEstado}'. El estado solo puede avanzar al siguiente paso lógico (Pendiente -> Asignado -> En proceso -> Resuelto -> Cerrado) y no se puede cerrar sin haberlo resuelto."
            });
        }

        incidente.Estado = nuevoEstado;

        // Llenar FechaResolucion al pasar a Resuelto
        if (nuevoEstado == Estado.Resuelto)
        {
            incidente.FechaResolucion = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Incidente #{Id} cambió de estado a {NuevoEstado}.", incidente.Id, nuevoEstado);

        return Ok(MapToDto(incidente));
    }

    // DELETE: api/incidentes/{id}
    // Eliminar incidente: Solo Admin (y prohíbe eliminar si ya está Cerrado).
    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Delete(int id)
    {
        var incidente = await _context.Incidentes.FindAsync(id);

        if (incidente == null)
        {
            return NotFound(new { message = $"No se encontró el incidente con ID {id}." });
        }

        if (incidente.Estado == Estado.Cerrado)
        {
            return BadRequest(new { message = "Operación denegada: No se puede eliminar un incidente que ya se encuentra en estado 'Cerrado'." });
        }

        _context.Incidentes.Remove(incidente);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Incidente #{Id} eliminado por el Administrador.", id);

        return Ok(new { success = true, message = $"El incidente #{id} fue eliminado exitosamente." });
    }

    private static IncidenteDto MapToDto(Incidente incidente)
    {
        return new IncidenteDto
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
    }
}
