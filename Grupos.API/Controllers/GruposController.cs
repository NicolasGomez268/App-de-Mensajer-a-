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

    public GruposController(GruposDbContext context)
    {
        _context = context;
    }

    // Obtener el ID del usuario del token JWT
    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                       ?? User.FindFirst("sub")?.Value;
        return Guid.Parse(userIdClaim!);
    }

    // POST /api/grupos - Crear grupo
    [HttpPost]
    public async Task<IActionResult> CrearGrupo([FromBody] CrearGrupoDto dto)
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

    // GET /api/grupos/mis-grupos - Listar grupos del usuario
    [HttpGet("mis-grupos")]
    public async Task<IActionResult> MisGrupos()
    {
        var userId = GetUserId();

        var grupos = await _context.MiembrosGrupo
            .Where(m => m.UsuarioId == userId)
            .Join(_context.Grupos,
                  miembro => miembro.GrupoId,
                  grupo => grupo.Id,
                  (miembro, grupo) => grupo)
            .ToListAsync();

        return Ok(grupos);
    }

    // GET /api/grupos/{id}/miembros - Listar miembros de un grupo
    [HttpGet("{id}/miembros")]
    public async Task<IActionResult> ListarMiembros(Guid id)
    {
        var miembros = await _context.MiembrosGrupo
            .Where(m => m.GrupoId == id)
            .ToListAsync();

        return Ok(miembros);
    }

    // POST /api/grupos/{id}/miembros - Agregar miembro
    [HttpPost("{id}/miembros")]
    public async Task<IActionResult> AgregarMiembro(Guid id, [FromBody] AgregarMiembroDto dto)
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

    // DELETE /api/grupos/{id}/miembros/{userId} - Eliminar miembro
    [HttpDelete("{id}/miembros/{userId}")]
    public async Task<IActionResult> EliminarMiembro(Guid id, Guid userId)
    {
        var miembro = await _context.MiembrosGrupo
            .FirstOrDefaultAsync(m => m.GrupoId == id && m.UsuarioId == userId);

        if (miembro == null)
            return NotFound("Miembro no encontrado");

        _context.MiembrosGrupo.Remove(miembro);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE /api/grupos/{id} - Eliminar grupo
    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarGrupo(Guid id)
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
}
