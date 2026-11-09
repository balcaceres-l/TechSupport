using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechSupport.API.Data;
using TechSupport.API.DTOs;
using TechSupport.API.Enums;
using TechSupport.API.Models;

namespace TechSupport.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IncidentesController : ControllerBase
{
    private readonly AppDbContext _context;

    public IncidentesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/incidentes
    [HttpGet]
    public async Task<ActionResult<IEnumerable<IncidenteDto>>> GetAll()
    {
        var incidentes = await _context.Incidentes
            .Include(i => i.Usuario)
            .Include(i => i.Tecnico)
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
        var incidente = await _context.Incidentes
            .Include(i => i.Usuario)
            .Include(i => i.Tecnico)
            .Where(i => i.Id == id)
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
            .FirstOrDefaultAsync();

        if (incidente == null)
        {
            return NotFound(new { message = $"No se encontró el incidente con ID {id}." });
        }

        return Ok(incidente);
    }

    // POST: api/incidentes
    [HttpPost]
    public async Task<ActionResult<IncidenteDto>> Create([FromBody] CreateIncidenteDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Verificar que el usuario reportador exista
        var usuario = await _context.Usuarios.FindAsync(dto.UsuarioId);
        if (usuario == null)
        {
            return BadRequest(new { message = $"El usuario con ID {dto.UsuarioId} no existe." });
        }

        var incidente = new Incidente
        {
            Titulo = dto.Titulo,
            Descripcion = dto.Descripcion,
            Prioridad = dto.Prioridad,
            Estado = Estado.Pendiente,
            UsuarioId = dto.UsuarioId,
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
            NombreUsuario = usuario.NombreUsuario,
            TecnicoId = null,
            NombreTecnico = null,
            FechaCreacion = incidente.FechaCreacion,
            FechaResolucion = null
        };

        return CreatedAtAction(nameof(GetById), new { id = incidente.Id }, resultadoDto);
    }
}
