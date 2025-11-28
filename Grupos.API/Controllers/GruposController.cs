using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Grupos.API.Data;
using Grupos.API.DTOs;
using Grupos.API.Models;
using System.Security.Claims;

namespace Grupos.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // ⚠️ TODOS los endpoints protegidos
public class GruposController : ControllerBase
{
    private readonly GruposDbContext _context;

    private readonly IHttpClientFactory _httpClientFactory;

    public GruposController(GruposDbContext context, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
    }

    // Obtener el ID del usuario del token JWT
    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                       ?? User.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim))
        {
            Console.WriteLine("❌ [Grupos.API] GetUserId: No se encontró claim 'sub' o NameIdentifier en el token.");
            foreach (var claim in User.Claims)
            {
                Console.WriteLine($"   - Claim: {claim.Type} = {claim.Value}");
            }
            throw new UnauthorizedAccessException("Token inválido: No contiene ID de usuario.");
        }

        if (!Guid.TryParse(userIdClaim, out var guid))
        {
             Console.WriteLine($"❌ [Grupos.API] GetUserId: El ID '{userIdClaim}' no es un GUID válido.");
             throw new UnauthorizedAccessException($"Token inválido: ID de usuario '{userIdClaim}' no es un GUID.");
        }

        return guid;
    }

    // POST /api/grupos - Crear grupo
    [HttpPost]
    public async Task<IActionResult> CrearGrupo([FromBody] CrearGrupoDto dto)
    {
        try 
        {
            var userId = GetUserId();

            var grupo = new Grupo
            {
                Nombre = dto.Nombre,
                CreadoPor = userId
            };

            _context.Grupos.Add(grupo);
            await _context.SaveChangesAsync();

            // Agregar al creador como miembro
            _context.MiembrosGrupo.Add(new MiembroGrupo
            {
                GrupoId = grupo.Id,
                UsuarioId = userId
            });

            // Agregar otros miembros
            foreach (var miembroId in dto.MiembrosIds)
            {
                if (miembroId != userId) // No duplicar al creador
                {
                    _context.MiembrosGrupo.Add(new MiembroGrupo
                    {
                        GrupoId = grupo.Id,
                        UsuarioId = miembroId
                    });
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { id = grupo.Id, nombre = grupo.Nombre });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [Grupos.API] Error en CrearGrupo: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    // GET /api/grupos/mis-grupos - Listar grupos del usuario
    [HttpGet("mis-grupos")]
    public async Task<IActionResult> MisGrupos()
    {
        try
        {
            var userId = GetUserId();
            Console.WriteLine($"✅ [Grupos.API] MisGrupos: Buscando grupos para usuario {userId}");

            var gruposIds = await _context.MiembrosGrupo
                .Where(m => m.UsuarioId == userId)
                .Select(m => m.GrupoId)
                .ToListAsync();

            var grupos = await _context.Grupos
                .Where(g => gruposIds.Contains(g.Id))
                .Include(g => g.Miembros)
                .ToListAsync();

            // Obtener información de usuarios
            var usuariosIds = grupos.SelectMany(g => g.Miembros.Select(m => m.UsuarioId)).Distinct().ToList();
            var usuariosInfo = new Dictionary<Guid, UserInfoDto>();

            foreach (var id in usuariosIds)
            {
                var info = await GetUserInfo(id);
                if (info != null)
                {
                    usuariosInfo[id] = info;
                }
            }

            // Mapear a DTO
            var gruposDto = grupos.Select(g => new GrupoResponseDto
            {
                Id = g.Id,
                Nombre = g.Nombre,
                CreadoPor = g.CreadoPor,
                FechaCreacion = g.FechaCreacion,
                Miembros = g.Miembros.Select(m => new MiembroGrupoResponseDto
                {
                    Id = m.Id,
                    GrupoId = m.GrupoId,
                    UsuarioId = m.UsuarioId,
                    FechaIngreso = m.FechaIngreso,
                    NombreUsuario = usuariosInfo.ContainsKey(m.UsuarioId) ? usuariosInfo[m.UsuarioId].Nombre : "Usuario",
                    EmailUsuario = usuariosInfo.ContainsKey(m.UsuarioId) ? usuariosInfo[m.UsuarioId].Email : ""
                }).ToList()
            }).ToList();

            Console.WriteLine($"✅ [Grupos.API] MisGrupos: Encontrados {grupos.Count} grupos.");
            return Ok(gruposDto);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [Grupos.API] Error en MisGrupos: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    private async Task<UserInfoDto?> GetUserInfo(Guid userId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var apiUrl = Environment.GetEnvironmentVariable("USUARIOS_API_URL") ?? "http://localhost:5156";
            var response = await client.GetAsync($"{apiUrl}/api/users/{userId}");
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserInfoDto>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error al obtener info del usuario {userId}: {ex.Message}");
        }

        return null;
    }

    // GET /api/grupos/{id}/miembros - Listar miembros de un grupo
    [HttpGet("{id}/miembros")]
    public async Task<IActionResult> ListarMiembros(Guid id)
    {
        try
        {
            var miembros = await _context.MiembrosGrupo
                .Where(m => m.GrupoId == id)
                .ToListAsync();

            return Ok(miembros);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [Grupos.API] Error en ListarMiembros: {ex.Message}");
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    // POST /api/grupos/{id}/miembros - Agregar miembro
    [HttpPost("{id}/miembros")]
    public async Task<IActionResult> AgregarMiembro(Guid id, [FromBody] AgregarMiembroDto dto)
    {
        try
        {
            // Verificar que el grupo existe
            var grupoExiste = await _context.Grupos.AnyAsync(g => g.Id == id);
            if (!grupoExiste)
                return NotFound("Grupo no encontrado");

            // Verificar que no está ya en el grupo
            var yaEsMiembro = await _context.MiembrosGrupo
                .AnyAsync(m => m.GrupoId == id && m.UsuarioId == dto.UserId);
            
            if (yaEsMiembro)
                return BadRequest("El usuario ya es miembro del grupo");

            var miembro = new MiembroGrupo
            {
                GrupoId = id,
                UsuarioId = dto.UserId
            };

            _context.MiembrosGrupo.Add(miembro);
            await _context.SaveChangesAsync();

            return Ok(miembro);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [Grupos.API] Error en AgregarMiembro: {ex.Message}");
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    // DELETE /api/grupos/{id}/miembros/{userId} - Eliminar miembro
    [HttpDelete("{id}/miembros/{userId}")]
    public async Task<IActionResult> EliminarMiembro(Guid id, Guid userId)
    {
        try
        {
            var miembro = await _context.MiembrosGrupo
                .FirstOrDefaultAsync(m => m.GrupoId == id && m.UsuarioId == userId);

            if (miembro == null)
                return NotFound("Miembro no encontrado");

            _context.MiembrosGrupo.Remove(miembro);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [Grupos.API] Error en EliminarMiembro: {ex.Message}");
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }

    // DELETE /api/grupos/{id} - Eliminar grupo
    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarGrupo(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var grupo = await _context.Grupos.FindAsync(id);

            if (grupo == null)
                return NotFound("Grupo no encontrado");

            // Solo el creador puede eliminar
            if (grupo.CreadoPor != userId)
                return Forbid();

            _context.Grupos.Remove(grupo);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [Grupos.API] Error en EliminarGrupo: {ex.Message}");
            return StatusCode(500, $"Error interno: {ex.Message}");
        }
    }
}
