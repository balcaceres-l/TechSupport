using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechSupport.API.Data;
using TechSupport.API.DTOs;
using TechSupport.API.Services;

namespace TechSupport.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AppDbContext context, JwtService jwtService, ILogger<AuthController> logger)
    {
        _context = context;
        _jwtService = jwtService;
        _logger = logger;
    }

    // POST: api/auth/login
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.NombreUsuario.ToLower() == dto.NombreUsuario.ToLower());

        if (usuario == null)
        {
            _logger.LogWarning("Intento de login fallido: usuario '{Usuario}' no encontrado.", dto.NombreUsuario);
            return Unauthorized(new { success = false, message = "Usuario o contraseña incorrectos." });
        }

        bool passwordValida = BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash);
        if (!passwordValida)
        {
            _logger.LogWarning("Intento de login fallido: contraseña incorrecta para '{Usuario}'.", dto.NombreUsuario);
            return Unauthorized(new { success = false, message = "Usuario o contraseña incorrectos." });
        }

        var token = _jwtService.GenerateToken(usuario);

        return Ok(new LoginResponseDto
        {
            Success = true,
            Token = token,
            Id = usuario.Id,
            NombreUsuario = usuario.NombreUsuario,
            Rol = usuario.Rol.ToString(),
            Message = "Autenticación exitosa."
        });
    }
}
