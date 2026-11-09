using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechSupport.API.Data;
using TechSupport.API.DTOs;
using TechSupport.API.Enums;

namespace TechSupport.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsuariosController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/usuarios/tecnicos
    [HttpGet("tecnicos")]
    public async Task<ActionResult<IEnumerable<UsuarioDto>>> GetTecnicos()
    {
        var tecnicos = await _context.Usuarios
            .Where(u => u.Rol == Rol.Tecnico)
            .Select(u => new UsuarioDto
            {
                Id = u.Id,
                NombreUsuario = u.NombreUsuario,
                Rol = u.Rol
            })
            .ToListAsync();

        return Ok(tecnicos);
    }
}
