# 🎯 PARTE DE NICOLÁS - API DE GRUPOS

## Tu Responsabilidad
Sos el **dueño absoluto** de `Grupos.API` y del schema `grupos` en la base de datos.

## ⚠️ ANTES DE EMPEZAR
1. Esperar a que **Ulises complete la FASE 1** (setup inicial)
2. Hacer `git clone` del repositorio
3. Hacer `git checkout develop`
4. Abrir la solución `ChatApp.sln`

---

## 📦 Tu Proyecto: `Grupos.API`

### Estructura de Carpetas
```
Grupos.API/
├── Controllers/
│   └── GruposController.cs
├── Data/
│   ├── GruposDbContext.cs
│   └── Migrations/
├── Models/
│   ├── Grupo.cs
│   └── MiembroGrupo.cs
├── DTOs/
│   ├── CrearGrupoDto.cs
│   └── GrupoDto.cs
├── Program.cs
└── appsettings.json
```

---

## 🗄️ Base de Datos - Schema `grupos`

### Tabla: `Grupos`
```sql
CREATE TABLE grupos.Grupos (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Nombre VARCHAR(100) NOT NULL,
    CreadoPor UUID NOT NULL,
    FechaCreacion TIMESTAMP DEFAULT NOW()
);
```

### Tabla: `MiembrosGrupo`
```sql
CREATE TABLE grupos.MiembrosGrupo (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    GrupoId UUID NOT NULL REFERENCES grupos.Grupos(Id) ON DELETE CASCADE,
    UsuarioId UUID NOT NULL,
    FechaIngreso TIMESTAMP DEFAULT NOW(),
    UNIQUE(GrupoId, UsuarioId)
);
```

---

## 💻 PASO 1: Configurar tu DbContext

### `Data/GruposDbContext.cs`
```csharp
using Microsoft.EntityFrameworkCore;
using Grupos.API.Models;

namespace Grupos.API.Data;

public class GruposDbContext : DbContext
{
    public GruposDbContext(DbContextOptions<GruposDbContext> options) : base(options) { }

    public DbSet<Grupo> Grupos { get; set; }
    public DbSet<MiembroGrupo> MiembrosGrupo { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ⚠️ CRÍTICO: Usar el schema "grupos"
        modelBuilder.HasDefaultSchema("grupos");

        modelBuilder.Entity<MiembroGrupo>()
            .HasIndex(m => new { m.GrupoId, m.UsuarioId })
            .IsUnique();
    }
}
```

### Registrar en `Program.cs`
```csharp
// Agregar después de var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<GruposDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
```

---

## 📝 PASO 2: Crear los Modelos

### `Models/Grupo.cs`
```csharp
namespace Grupos.API.Models;

public class Grupo
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public Guid CreadoPor { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
```

### `Models/MiembroGrupo.cs`
```csharp
namespace Grupos.API.Models;

public class MiembroGrupo
{
    public Guid Id { get; set; }
    public Guid GrupoId { get; set; }
    public Guid UsuarioId { get; set; }
    public DateTime FechaIngreso { get; set; } = DateTime.UtcNow;
}
```

---

## 🎨 PASO 3: Crear los DTOs

### `DTOs/CrearGrupoDto.cs`
```csharp
namespace Grupos.API.DTOs;

public class CrearGrupoDto
{
    public string Nombre { get; set; } = string.Empty;
    public List<Guid> MiembrosIds { get; set; } = new();
}
```

### `DTOs/AgregarMiembroDto.cs`
```csharp
namespace Grupos.API.DTOs;

public class AgregarMiembroDto
{
    public Guid UserId { get; set; }
}
```

---

## 🚀 PASO 4: Crear el Controlador

### `Controllers/GruposController.cs`
```csharp
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
```

---

## 🔄 PASO 5: Migraciones

```bash
# Crear la migración inicial
dotnet ef migrations add InitialCreate --project Grupos.API

# Aplicar la migración a la base de datos
dotnet ef database update --project Grupos.API
```

---

## ✅ PASO 6: Probar tu API

### Con cURL (necesitas un JWT válido):
```bash
# Crear grupo
curl -X POST https://grupos-api-tup.azurewebsites.net/api/grupos \
  -H "Authorization: Bearer TU_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "nombre": "Grupo de Prueba",
    "miembrosIds": ["guid-del-usuario-2"]
  }'

# Listar mis grupos
curl https://grupos-api-tup.azurewebsites.net/api/grupos/mis-grupos \
  -H "Authorization: Bearer TU_JWT_TOKEN"
```

---

## 📤 PASO 7: Git Workflow

### Antes de empezar a trabajar:
```bash
git pull origin develop --rebase
```

### Cuando termines:
```bash
git add .
git commit -m "feat(grupos): implementar API completa de grupos"
git pull origin develop --rebase
git push origin develop
```

### ⚠️ Avisar al equipo:
> "Muchachos, subí la API de Grupos completa a `develop`. Está testeada y funcionando. 🚀"

---

## 🎯 Endpoints que DEBES Implementar (Contrato)

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| POST | `/api/grupos` | Crear grupo |
| GET | `/api/grupos/mis-grupos` | Listar grupos del usuario |
| GET | `/api/grupos/{id}/miembros` | Listar miembros |
| POST | `/api/grupos/{id}/miembros` | Agregar miembro |
| DELETE | `/api/grupos/{id}/miembros/{userId}` | Eliminar miembro |
| DELETE | `/api/grupos/{id}` | Eliminar grupo |

---

## 🚨 Reglas de Oro

1. **NUNCA** toques el schema `usuarios` o `mensajeria`
2. **SIEMPRE** usa `[Authorize]` en todos los endpoints
3. **PULL ANTES DE PUSH**: `git pull origin develop --rebase`
4. **NO SUBAS CÓDIGO ROTO**: Si no compila, no lo subas
5. **COMUNICA**: Avisa cuando termines y subas tu código

---

## 🤝 Coordinación con el Equipo

- **Ulises** te dará el `userId` (GUID) que necesitas para relacionar con usuarios
- **Pablo** consumirá tus grupos para enviar mensajes grupales
- Tu API **NO** necesita saber nada de mensajes, solo de grupos y miembros

---

## 📞 Dudas Frecuentes

**P: ¿Cómo obtengo el userId del token JWT?**
R: Usa `GetUserId()` en el controlador (ya está en el código).

**P: ¿Cómo pruebo sin tener el frontend?**
R: Usa Postman/Insomnia y pídele a Ulises un JWT válido.

**P: ¿Qué hago si hay conflictos en Git?**
R: Avisa en el grupo. Resuelvan juntos.

---

¡Éxito, Nicolás! Tu parte es clave para que el chat grupal funcione. 💪
