using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Usuarios.API.Data;
using Usuarios.API.DTOs;
using Usuarios.API.Models;

namespace Usuarios.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UsuariosDbContext _context;
    private readonly ILogger<UsersController> _logger;

    public UsersController(UsuariosDbContext context, ILogger<UsersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Sincroniza usuario desde Supabase Auth a la base de datos local
    /// </summary>
    [HttpPost("sync")]
    public async Task<ActionResult<UserDto>> SyncUser([FromBody] SyncUserRequest request)
    {
        try
        {
            var authId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                        ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(authId))
            {
                return Unauthorized("No se pudo obtener el ID de autenticación");
            }

            // Buscar usuario existente
            var perfil = await _context.Perfiles
                .FirstOrDefaultAsync(p => p.AuthId == authId);

            if (perfil == null)
            {
                // Crear nuevo perfil
                perfil = new Perfil
                {
                    Id = Guid.NewGuid(),
                    AuthId = authId,
                    Email = request.Email,
                    Nombre = request.Nombre,
                    AvatarUrl = request.AvatarUrl,
                    Estado = "online",
                    FechaCreacion = DateTime.UtcNow,
                    UltimaConexion = DateTime.UtcNow
                };

                _context.Perfiles.Add(perfil);
                _logger.LogInformation("Nuevo perfil creado para usuario {AuthId}", authId);
            }
            else
            {
                // Actualizar perfil existente
                perfil.Email = request.Email;
                perfil.Nombre = request.Nombre;
                perfil.AvatarUrl = request.AvatarUrl ?? perfil.AvatarUrl;
                perfil.Estado = "online";
                perfil.UltimaConexion = DateTime.UtcNow;

                _logger.LogInformation("Perfil actualizado para usuario {AuthId}", authId);
            }

            await _context.SaveChangesAsync();

            return Ok(new UserDto
            {
                Id = perfil.Id,
                AuthId = perfil.AuthId,
                Email = perfil.Email,
                Nombre = perfil.Nombre,
                AvatarUrl = perfil.AvatarUrl,
                Estado = perfil.Estado,
                FechaCreacion = perfil.FechaCreacion,
                UltimaConexion = perfil.UltimaConexion
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sincronizando usuario");
            return StatusCode(500, "Error interno del servidor");
        }
    }

    /// <summary>
    /// Obtiene el perfil del usuario autenticado
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetMe()
    {
        try
        {
            var authId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                        ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(authId))
            {
                return Unauthorized("No se pudo obtener el ID de autenticación");
            }

            var perfil = await _context.Perfiles
                .FirstOrDefaultAsync(p => p.AuthId == authId);

            if (perfil == null)
            {
                return NotFound("Perfil no encontrado. Debe sincronizar primero.");
            }

            return Ok(new UserDto
            {
                Id = perfil.Id,
                AuthId = perfil.AuthId,
                Email = perfil.Email,
                Nombre = perfil.Nombre,
                AvatarUrl = perfil.AvatarUrl,
                Estado = perfil.Estado,
                FechaCreacion = perfil.FechaCreacion,
                UltimaConexion = perfil.UltimaConexion
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo perfil del usuario");
            return StatusCode(500, "Error interno del servidor");
        }
    }

    /// <summary>
    /// Obtiene todos los usuarios (para búsqueda de contactos)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAllUsers([FromQuery] string? search = null)
    {
        try
        {
            var query = _context.Perfiles.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => 
                    p.Nombre.Contains(search) || 
                    p.Email.Contains(search));
            }

            var perfiles = await query
                .OrderBy(p => p.Nombre)
                .Take(50) // Limitar a 50 resultados
                .ToListAsync();

            var userDtos = perfiles.Select(p => new UserDto
            {
                Id = p.Id,
                AuthId = p.AuthId,
                Email = p.Email,
                Nombre = p.Nombre,
                AvatarUrl = p.AvatarUrl,
                Estado = p.Estado,
                FechaCreacion = p.FechaCreacion,
                UltimaConexion = p.UltimaConexion
            }).ToList();

            return Ok(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo lista de usuarios");
            return StatusCode(500, "Error interno del servidor");
        }
    }

    /// <summary>
    /// Actualiza el estado de conexión del usuario
    /// </summary>
    [HttpPatch("status")]
    public async Task<IActionResult> UpdateStatus([FromBody] string estado)
    {
        try
        {
            var authId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                        ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(authId))
            {
                return Unauthorized();
            }

            var perfil = await _context.Perfiles
                .FirstOrDefaultAsync(p => p.AuthId == authId);

            if (perfil == null)
            {
                return NotFound();
            }

            perfil.Estado = estado;
            perfil.UltimaConexion = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando estado");
            return StatusCode(500, "Error interno del servidor");
        }
    }
}
