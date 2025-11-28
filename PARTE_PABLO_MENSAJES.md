# 🎯 PARTE DE PABLO - API DE MENSAJES Y SIGNALR

## Tu Responsabilidad
Sos el **dueño absoluto** de `Mensajes.API`, el **SignalR Hub** y del schema `mensajeria` en la base de datos.

⚠️ **Esta es la parte más compleja del proyecto**. Tenés el core del tiempo real.

## ⚠️ ANTES DE EMPEZAR
1. Esperar a que **Ulises complete la FASE 1** (setup inicial)
2. Hacer `git clone` del repositorio
3. Hacer `git checkout develop`
4. Abrir la solución `ChatApp.sln`

---

## 📦 Tu Proyecto: `Mensajes.API`

### Estructura de Carpetas
```
Mensajes.API/
├── Controllers/
│   └── MensajesController.cs
├── Data/
│   ├── MensajesDbContext.cs
│   └── Migrations/
├── Models/
│   ├── Mensaje.cs
│   └── LecturaMensaje.cs
├── DTOs/
│   ├── EnviarMensajeDto.cs
│   └── MensajeDto.cs
├── Hubs/
│   └── ChatHub.cs
├── Program.cs
└── appsettings.json
```

---

## 🗄️ Base de Datos - Schema `mensajeria`

### Tabla: `Mensajes`
```sql
CREATE TABLE mensajeria.Mensajes (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    RemitenteId UUID NOT NULL,
    DestinatarioId UUID NULL,
    GrupoId UUID NULL,
    Contenido TEXT NOT NULL,
    FechaEnvio TIMESTAMP DEFAULT NOW(),
    CHECK (
        (DestinatarioId IS NOT NULL AND GrupoId IS NULL) OR
        (DestinatarioId IS NULL AND GrupoId IS NOT NULL)
    )
);
```

### Tabla: `LecturasMensaje`
```sql
CREATE TABLE mensajeria.LecturasMensaje (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    MensajeId UUID NOT NULL REFERENCES mensajeria.Mensajes(Id) ON DELETE CASCADE,
    UsuarioId UUID NOT NULL,
    FechaLectura TIMESTAMP DEFAULT NOW(),
    UNIQUE(MensajeId, UsuarioId)
);
```

---

## 💻 PASO 1: Configurar tu DbContext

### `Data/MensajesDbContext.cs`
```csharp
using Microsoft.EntityFrameworkCore;
using Mensajes.API.Models;

namespace Mensajes.API.Data;

public class MensajesDbContext : DbContext
{
    public MensajesDbContext(DbContextOptions<MensajesDbContext> options) : base(options) { }

    public DbSet<Mensaje> Mensajes { get; set; }
    public DbSet<LecturaMensaje> LecturasMensaje { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ⚠️ CRÍTICO: Usar el schema "mensajeria"
        modelBuilder.HasDefaultSchema("mensajeria");

        modelBuilder.Entity<LecturaMensaje>()
            .HasIndex(l => new { l.MensajeId, l.UsuarioId })
            .IsUnique();
    }
}
```

### Registrar en `Program.cs`
```csharp
// Agregar después de var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<MensajesDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ⚠️ CRÍTICO: Agregar SignalR
builder.Services.AddSignalR();
```

---

## 📝 PASO 2: Crear los Modelos

### `Models/Mensaje.cs`
```csharp
namespace Mensajes.API.Models;

public class Mensaje
{
    public Guid Id { get; set; }
    public Guid RemitenteId { get; set; }
    public Guid? DestinatarioId { get; set; }
    public Guid? GrupoId { get; set; }
    public string Contenido { get; set; } = string.Empty;
    public DateTime FechaEnvio { get; set; } = DateTime.UtcNow;
}
```

### `Models/LecturaMensaje.cs`
```csharp
namespace Mensajes.API.Models;

public class LecturaMensaje
{
    public Guid Id { get; set; }
    public Guid MensajeId { get; set; }
    public Guid UsuarioId { get; set; }
    public DateTime FechaLectura { get; set; } = DateTime.UtcNow;
}
```

---

## 🎨 PASO 3: Crear los DTOs

### `DTOs/EnviarMensajeDirectoDto.cs`
```csharp
namespace Mensajes.API.DTOs;

public class EnviarMensajeDirectoDto
{
    public Guid DestinatarioId { get; set; }
    public string Contenido { get; set; } = string.Empty;
}
```

### `DTOs/EnviarMensajeGrupoDto.cs`
```csharp
namespace Mensajes.API.DTOs;

public class EnviarMensajeGrupoDto
{
    public Guid GrupoId { get; set; }
    public string Contenido { get; set; } = string.Empty;
}
```

### `DTOs/MensajeDto.cs`
```csharp
namespace Mensajes.API.DTOs;

public class MensajeDto
{
    public Guid Id { get; set; }
    public Guid RemitenteId { get; set; }
    public Guid? DestinatarioId { get; set; }
    public Guid? GrupoId { get; set; }
    public string Contenido { get; set; } = string.Empty;
    public DateTime FechaEnvio { get; set; }
}
```

---

## 🔌 PASO 4: Crear el SignalR Hub (LO MÁS IMPORTANTE)

### `Hubs/ChatHub.cs`
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Mensajes.API.Hubs;

[Authorize] // ⚠️ Hub protegido con JWT
public class ChatHub : Hub
{
    private Guid GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                       ?? Context.User?.FindFirst("sub")?.Value;
        return Guid.Parse(userIdClaim!);
    }

    // Cliente invoca: NotificarEscribiendo
    public async Task NotificarEscribiendo(string conversacionId)
    {
        var userId = GetUserId();
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Usuario";
        
        // Emitir a todos excepto al que escribe
        await Clients.OthersInGroup(conversacionId)
            .SendAsync("UsuarioEscribiendo", conversacionId, userName);
    }

    // Método para que el cliente se una a una "sala" de conversación
    public async Task JoinConversation(string conversacionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, conversacionId);
    }

    // Método para que el cliente salga de una "sala"
    public async Task LeaveConversation(string conversacionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversacionId);
    }

    // Método para notificar que un mensaje fue enviado (llamado desde el Controller)
    public async Task NotificarMensajeEnviado(string conversacionId, MensajeDto mensaje)
    {
        await Clients.Group(conversacionId)
            .SendAsync("MensajeRecibido", mensaje);
    }

    // Método para notificar que un mensaje fue leído
    public async Task NotificarMensajeLeido(string conversacionId, Guid mensajeId, Guid userId)
    {
        await Clients.Group(conversacionId)
            .SendAsync("MensajeLeido", mensajeId, userId);
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        Console.WriteLine($"Usuario {userId} conectado al Hub");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        Console.WriteLine($"Usuario {userId} desconectado del Hub");
        await base.OnDisconnectedAsync(exception);
    }
}
```

### Registrar el Hub en `Program.cs` (después de `app.UseAuthorization()`):
```csharp
// ⚠️ CRÍTICO: Mapear el Hub
app.MapHub<ChatHub>("/chathub").RequireAuthorization();
```

---

## 🚀 PASO 5: Crear el Controlador

### `Controllers/MensajesController.cs`
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Mensajes.API.Data;
using Mensajes.API.DTOs;
using Mensajes.API.Models;
using Mensajes.API.Hubs;
using System.Security.Claims;

namespace Mensajes.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MensajesController : ControllerBase
{
    private readonly MensajesDbContext _context;
    private readonly IHubContext<ChatHub> _hubContext;

    public MensajesController(MensajesDbContext context, IHubContext<ChatHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                       ?? User.FindFirst("sub")?.Value;
        return Guid.Parse(userIdClaim!);
    }

    // POST /api/mensajes/directo - Enviar mensaje directo
    [HttpPost("directo")]
    public async Task<IActionResult> EnviarMensajeDirecto([FromBody] EnviarMensajeDirectoDto dto)
    {
        var remitenteId = GetUserId();

        var mensaje = new Mensaje
        {
            RemitenteId = remitenteId,
            DestinatarioId = dto.DestinatarioId,
            Contenido = dto.Contenido
        };

        _context.Mensajes.Add(mensaje);
        await _context.SaveChangesAsync();

        var mensajeDto = new MensajeDto
        {
            Id = mensaje.Id,
            RemitenteId = mensaje.RemitenteId,
            DestinatarioId = mensaje.DestinatarioId,
            Contenido = mensaje.Contenido,
            FechaEnvio = mensaje.FechaEnvio
        };

        // Notificar vía SignalR
        var conversacionId = GetConversacionId(remitenteId, dto.DestinatarioId);
        await _hubContext.Clients.Group(conversacionId)
            .SendAsync("MensajeRecibido", mensajeDto);

        return Ok(mensajeDto);
    }

    // POST /api/mensajes/grupo - Enviar mensaje grupal
    [HttpPost("grupo")]
    public async Task<IActionResult> EnviarMensajeGrupo([FromBody] EnviarMensajeGrupoDto dto)
    {
        var remitenteId = GetUserId();

        var mensaje = new Mensaje
        {
            RemitenteId = remitenteId,
            GrupoId = dto.GrupoId,
            Contenido = dto.Contenido
        };

        _context.Mensajes.Add(mensaje);
        await _context.SaveChangesAsync();

        var mensajeDto = new MensajeDto
        {
            Id = mensaje.Id,
            RemitenteId = mensaje.RemitenteId,
            GrupoId = mensaje.GrupoId,
            Contenido = mensaje.Contenido,
            FechaEnvio = mensaje.FechaEnvio
        };

        // Notificar vía SignalR al grupo
        await _hubContext.Clients.Group($"grupo-{dto.GrupoId}")
            .SendAsync("MensajeRecibido", mensajeDto);

        return Ok(mensajeDto);
    }

    // GET /api/mensajes/conversacion/{userId} - Historial con paginación
    [HttpGet("conversacion/{userId}")]
    public async Task<IActionResult> ObtenerConversacion(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var currentUserId = GetUserId();

        var mensajes = await _context.Mensajes
            .Where(m =>
                (m.RemitenteId == currentUserId && m.DestinatarioId == userId) ||
                (m.RemitenteId == userId && m.DestinatarioId == currentUserId))
            .OrderByDescending(m => m.FechaEnvio)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MensajeDto
            {
                Id = m.Id,
                RemitenteId = m.RemitenteId,
                DestinatarioId = m.DestinatarioId,
                Contenido = m.Contenido,
                FechaEnvio = m.FechaEnvio
            })
            .ToListAsync();

        return Ok(mensajes);
    }

    // GET /api/mensajes/grupo/{grupoId} - Historial grupal con paginación
    [HttpGet("grupo/{grupoId}")]
    public async Task<IActionResult> ObtenerMensajesGrupo(
        Guid grupoId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var mensajes = await _context.Mensajes
            .Where(m => m.GrupoId == grupoId)
            .OrderByDescending(m => m.FechaEnvio)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new MensajeDto
            {
                Id = m.Id,
                RemitenteId = m.RemitenteId,
                GrupoId = m.GrupoId,
                Contenido = m.Contenido,
                FechaEnvio = m.FechaEnvio
            })
            .ToListAsync();

        return Ok(mensajes);
    }

    // POST /api/mensajes/{mensajeId}/visto - Marcar como visto
    [HttpPost("{mensajeId}/visto")]
    public async Task<IActionResult> MarcarComoVisto(Guid mensajeId)
    {
        var userId = GetUserId();

        var yaLeido = await _context.LecturasMensaje
            .AnyAsync(l => l.MensajeId == mensajeId && l.UsuarioId == userId);

        if (yaLeido)
            return Ok(new { message = "Ya marcado como leído" });

        var lectura = new LecturaMensaje
        {
            MensajeId = mensajeId,
            UsuarioId = userId
        };

        _context.LecturasMensaje.Add(lectura);
        await _context.SaveChangesAsync();

        // Notificar vía SignalR
        var mensaje = await _context.Mensajes.FindAsync(mensajeId);
        if (mensaje != null)
        {
            string conversacionId;
            if (mensaje.GrupoId.HasValue)
                conversacionId = $"grupo-{mensaje.GrupoId}";
            else
                conversacionId = GetConversacionId(mensaje.RemitenteId, mensaje.DestinatarioId!.Value);

            await _hubContext.Clients.Group(conversacionId)
                .SendAsync("MensajeLeido", mensajeId, userId);
        }

        return Ok(lectura);
    }

    // GET /api/mensajes/{mensajeId}/lecturas - Quién leyó el mensaje
    [HttpGet("{mensajeId}/lecturas")]
    public async Task<IActionResult> ObtenerLecturas(Guid mensajeId)
    {
        var lecturas = await _context.LecturasMensaje
            .Where(l => l.MensajeId == mensajeId)
            .Select(l => new
            {
                l.UsuarioId,
                l.FechaLectura
            })
            .ToListAsync();

        return Ok(lecturas);
    }

    // Helper: Generar ID único de conversación entre 2 usuarios
    private string GetConversacionId(Guid userId1, Guid userId2)
    {
        var ids = new[] { userId1, userId2 }.OrderBy(id => id).ToArray();
        return $"conv-{ids[0]}-{ids[1]}";
    }
}
```

---

## 🔄 PASO 6: Migraciones

```bash
# Crear la migración inicial
dotnet ef migrations add InitialCreate --project Mensajes.API

# Aplicar la migración a la base de datos
dotnet ef database update --project Mensajes.API
```

---

## ✅ PASO 7: Probar tu API y Hub

### Probar el Hub con JavaScript (desde el navegador):
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("https://mensajes-api-tup.azurewebsites.net/chathub", {
        accessTokenFactory: () => "TU_JWT_TOKEN"
    })
    .build();

// Escuchar eventos
connection.on("MensajeRecibido", (mensaje) => {
    console.log("Nuevo mensaje:", mensaje);
});

connection.on("UsuarioEscribiendo", (conversacionId, userName) => {
    console.log(`${userName} está escribiendo...`);
});

// Conectar
connection.start()
    .then(() => {
        console.log("Conectado al Hub!");
        // Unirse a una conversación
        connection.invoke("JoinConversation", "conv-guid1-guid2");
    });

// Notificar que estoy escribiendo
connection.invoke("NotificarEscribiendo", "conv-guid1-guid2");
```

---

## 📤 PASO 8: Git Workflow

### Antes de empezar:
```bash
git pull origin develop --rebase
```

### Cuando termines:
```bash
git add .
git commit -m "feat(mensajes): implementar API completa con SignalR"
git pull origin develop --rebase
git push origin develop
```

### ⚠️ Avisar al equipo:
> "Muchachos, subí la API de Mensajes y el Hub de SignalR. Está todo testeado. 🔥"

---

## 🎯 Endpoints y Métodos del Hub (Contrato)

### REST Endpoints:
| Método | Endpoint | Descripción |
|--------|----------|-------------|
| POST | `/api/mensajes/directo` | Enviar mensaje 1 a 1 |
| POST | `/api/mensajes/grupo` | Enviar mensaje grupal |
| GET | `/api/mensajes/conversacion/{userId}` | Historial (paginado) |
| GET | `/api/mensajes/grupo/{grupoId}` | Historial grupal (paginado) |
| POST | `/api/mensajes/{mensajeId}/visto` | Marcar como leído |
| GET | `/api/mensajes/{mensajeId}/lecturas` | Quién leyó |

### SignalR Hub (`/chathub`):
| Cliente Invoca | Servidor Emite |
|----------------|----------------|
| `NotificarEscribiendo(conversacionId)` | `UsuarioEscribiendo(conversacionId, userName)` |
| `JoinConversation(conversacionId)` | - |
| - | `MensajeRecibido(mensaje)` |
| - | `MensajeLeido(mensajeId, userId)` |

---

## 🚨 Reglas de Oro

1. **NUNCA** toques el schema `usuarios` o `grupos`
2. **SIEMPRE** usa `[Authorize]` en todos los endpoints y el Hub
3. **PULL ANTES DE PUSH**: `git pull origin develop --rebase`
4. **NO SUBAS CÓDIGO ROTO**: Si no compila, no lo subas
5. **COMUNICA**: Tu parte es crítica, avisa si algo no funciona

---

## 🤝 Coordinación con el Equipo

- **Ulises** te consumirá desde la UI (JavaScript/React)
- **Nicolás** te dará los `grupoId` para mensajes grupales
- Tu API **NO** necesita saber quiénes son los usuarios, solo sus IDs

---

## 📞 Dudas Frecuentes

**P: ¿Cómo pruebo el Hub sin frontend?**
R: Usa una página HTML simple con SignalR JS o usa Postman (soporte de WebSockets).

**P: ¿Por qué usar `IHubContext` en el Controller?**
R: Para emitir eventos de SignalR desde fuera del Hub (cuando guardas un mensaje en la DB).

**P: ¿Cómo manejo las "salas" de conversación?**
R: Usa `Groups.AddToGroupAsync()` con un ID único (ejemplo: `conv-guid1-guid2`).

---

¡Éxito, Pablo! Tu parte es el corazón del tiempo real. 💪🔥
