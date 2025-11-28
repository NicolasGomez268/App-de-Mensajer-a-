# 🎯 PARTE DE ULISES - SETUP INICIAL + UI + USUARIOS.API

## Tu Responsabilidad
Sos el **arquitecto e integrador**. Tenés 3 misiones:
1. **FASE 1**: Setup completo del proyecto (GitHub, Supabase, estructura .NET)
2. **FASE 2**: Desarrollar `Usuarios.API` y el schema `usuarios`
3. **FASE 3**: Crear la UI y conectar todo el sistema

⚠️ **Nicolás y Pablo NO empiezan hasta que termines la FASE 1**.

---

# 🏁 FASE 1: SETUP INICIAL (Misión Crítica)

## PASO 1: GitHub - Configuración del Repositorio

### 1.1. Crear el repositorio:
```bash
# En GitHub.com
- Crear repo: tup-progi-v-integrador
- Visibilidad: Privado
- NO inicializar con README (lo haremos local)
```

### 1.2. Clonar e inicializar localmente:
```bash
git clone https://github.com/TU_USUARIO/tup-progi-v-integrador.git
cd tup-progi-v-integrador

# Crear README inicial
echo "# TUP - Integrador - Chat RealTime" > README.md
git add README.md
git commit -m "Initial commit"
git push -u origin main

# Crear rama develop
git checkout -b develop
git push -u origin develop
```

### 1.3. Proteger rama main:
```
1. Ir a Settings > Branches
2. Add branch protection rule
3. Branch name pattern: main
4. Marcar: "Require pull request before merging"
5. Save
```

### 1.4. Invitar colaboradores:
```
Settings > Collaborators > Add people
- Agregar a Nicolás
- Agregar a Pablo
```

---

## PASO 2: Supabase - El Corazón del Sistema

### 2.1. Crear cuenta y proyecto:
```
1. Ir a https://supabase.com
2. Sign Up (gratis)
3. New Project:
   - Name: tup-chat-app
   - Database Password: [GUARDAR EN LUGAR SEGURO]
   - Region: South America (más cercana)
4. Esperar 2 minutos a que se cree
```

### 2.2. Obtener credenciales (GUARDAR TODO):

#### Connection String (para las APIs):
```
Project Settings > Database > Connection String > URI
Ejemplo:
postgresql://postgres.xxxxx:PASSWORD@aws-0-sa-east-1.pooler.supabase.com:6543/postgres
```

#### URL del Proyecto:
```
Project Settings > API > Project URL
Ejemplo:
https://xxxxx.supabase.co
```

#### API Keys:
```
Project Settings > API
- anon/public key: (para la UI)
- service_role key: (NO USAR en frontend)
```

#### JWT Secret:
```
Project Settings > API > JWT Settings > JWT Secret
Ejemplo: un string largo de caracteres aleatorios
```

### 2.3. Crear los 3 schemas:
```
1. Ir a SQL Editor
2. Copiar y ejecutar este script:
```

```sql
-- Crear los 3 schemas (cajas separadas)
CREATE SCHEMA IF NOT EXISTS usuarios;
CREATE SCHEMA IF NOT EXISTS grupos;
CREATE SCHEMA IF NOT EXISTS mensajeria;

-- Dar permisos
GRANT USAGE ON SCHEMA usuarios TO postgres, anon, authenticated, service_role;
GRANT USAGE ON SCHEMA grupos TO postgres, anon, authenticated, service_role;
GRANT USAGE ON SCHEMA mensajeria TO postgres, anon, authenticated, service_role;

GRANT ALL ON ALL TABLES IN SCHEMA usuarios TO postgres, anon, authenticated, service_role;
GRANT ALL ON ALL TABLES IN SCHEMA grupos TO postgres, anon, authenticated, service_role;
GRANT ALL ON ALL TABLES IN SCHEMA mensajeria TO postgres, anon, authenticated, service_role;

-- Verificar
SELECT schema_name FROM information_schema.schemata WHERE schema_name IN ('usuarios', 'grupos', 'mensajeria');
```

✅ Deberías ver los 3 schemas listados.

---

## PASO 3: Estructura Local .NET

### 3.1. Crear la solución y proyectos:
```bash
# Asegurarte de estar en la carpeta del repo
cd tup-progi-v-integrador

# Crear solución
dotnet new sln -n ChatApp

# Crear los 4 proyectos
dotnet new webapi -n Usuarios.API --framework net9.0
dotnet new webapi -n Grupos.API --framework net9.0
dotnet new webapi -n Mensajes.API --framework net9.0
dotnet new classlib -n Shared.Kernel --framework net9.0

# Agregar proyectos a la solución
dotnet sln add Usuarios.API/Usuarios.API.csproj
dotnet sln add Grupos.API/Grupos.API.csproj
dotnet sln add Mensajes.API/Mensajes.API.csproj
dotnet sln add Shared.Kernel/Shared.Kernel.csproj
```

### 3.2. Establecer referencias:
```bash
# Cada API referencia a Shared.Kernel
dotnet add Usuarios.API reference Shared.Kernel
dotnet add Grupos.API reference Shared.Kernel
dotnet add Mensajes.API reference Shared.Kernel
```

---

## PASO 4: Instalar Dependencias

### 4.1. En Shared.Kernel (autenticación JWT):
```bash
cd Shared.Kernel
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
cd ..
```

### 4.2. En las 3 APIs (PostgreSQL + EF Core):
```bash
# Usuarios.API
cd Usuarios.API
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
cd ..

# Grupos.API
cd Grupos.API
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
cd ..

# Mensajes.API
cd Mensajes.API
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.AspNetCore.SignalR
cd ..
```

---

## PASO 5: Configurar appsettings.json (LAS 3 APIS)

### Archivo: `Usuarios.API/appsettings.json`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "postgresql://postgres.xxxxx:TU_PASSWORD@aws-0-sa-east-1.pooler.supabase.com:6543/postgres"
  },
  "Jwt": {
    "Issuer": "https://xxxxx.supabase.co/auth/v1",
    "Audience": "authenticated",
    "Key": "TU_JWT_SECRET_DE_SUPABASE"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

⚠️ **COPIAR EL MISMO** `appsettings.json` a `Grupos.API` y `Mensajes.API` (reemplazar con TUS datos de Supabase).

---

## PASO 6: Configurar Seguridad en Program.cs (LAS 3 APIS)

### Archivo: `Usuarios.API/Program.cs` (Grupos y Mensajes igual)
```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// ========== 1. CORS ==========
var allowedOrigins = new[] {
    "http://localhost:3000",
    "http://localhost:5173",
    "https://chat-app-tup.azurewebsites.net" // Reemplazar con tu URL de Azure
};

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ========== 2. RATE LIMITING ==========
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", limiter =>
    {
        limiter.PermitLimit = 100;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 0;
    });
});

// ========== 3. AUTENTICACIÓN JWT ==========
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = config["Jwt:Issuer"],
            ValidAudience = config["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config["Jwt:Key"]!))
        };

        // ⚠️ IMPORTANTE para SignalR (solo en Mensajes.API)
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && 
                    path.StartsWithSegments("/chathub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ========== 4. CONTROLADORES ==========
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ========== BUILD ==========
var app = builder.Build();

// ========== MIDDLEWARE ==========
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowSpecificOrigins");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireRateLimiting("fixed");

// ⚠️ Solo en Mensajes.API:
// app.MapHub<ChatHub>("/chathub").RequireAuthorization();

app.Run();
```

⚠️ **COPIAR** este `Program.cs` a las 3 APIs (Usuarios, Grupos, Mensajes).

---

## PASO 7: Primer Push a GitHub

```bash
# Verificar que estás en develop
git branch

# Si no estás en develop:
git checkout develop

# Agregar todo
git add .
git commit -m "feat: setup inicial v4 - Supabase + 3 APIs + seguridad JWT"
git push -u origin develop
```

---

## PASO 8: Avisar al Equipo

### Mensaje para Nicolás y Pablo:
```
🚀 FASE 1 COMPLETA 🚀

El esqueleto está listo en la rama `develop`.

📌 Pasos para empezar:
1. git clone https://github.com/TU_USUARIO/tup-progi-v-integrador.git
2. git checkout develop
3. Abrir ChatApp.sln en Visual Studio
4. Revisar vuestros archivos:
   - Nicolás: PARTE_NICOLAS_GRUPOS.md
   - Pablo: PARTE_PABLO_MENSAJES.md

📦 Credenciales de Supabase:
- Connection String: [ENVIAR POR PRIVADO]
- JWT Secret: [ENVIAR POR PRIVADO]
- Project URL: [ENVIAR POR PRIVADO]

⚠️ REGLAS:
- SIEMPRE hacer `git pull origin develop --rebase` antes de empezar
- NO subir código que no compile
- AVISAR cuando suban cambios

¡A laburar! 💪
```

---

# 🏁 FASE 2: TU DESARROLLO (Usuarios.API)

## Tu Proyecto: `Usuarios.API`

### Estructura:
```
Usuarios.API/
├── Controllers/
│   └── UsersController.cs
├── Data/
│   ├── UsuariosDbContext.cs
│   └── Migrations/
├── Models/
│   └── Perfil.cs
├── DTOs/
│   ├── SyncUserDto.cs
│   └── UserDto.cs
├── Program.cs
└── appsettings.json
```

---

## Base de Datos - Schema `usuarios`

### Tabla: `Perfiles`
```sql
CREATE TABLE usuarios.Perfiles (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    AuthId VARCHAR(255) UNIQUE NOT NULL,
    Email VARCHAR(255) NOT NULL,
    Nombre VARCHAR(100),
    FechaCreacion TIMESTAMP DEFAULT NOW()
);
```

---

## Código: DbContext

### `Data/UsuariosDbContext.cs`
```csharp
using Microsoft.EntityFrameworkCore;
using Usuarios.API.Models;

namespace Usuarios.API.Data;

public class UsuariosDbContext : DbContext
{
    public UsuariosDbContext(DbContextOptions<UsuariosDbContext> options) : base(options) { }

    public DbSet<Perfil> Perfiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ⚠️ CRÍTICO: Schema "usuarios"
        modelBuilder.HasDefaultSchema("usuarios");

        modelBuilder.Entity<Perfil>()
            .HasIndex(p => p.AuthId)
            .IsUnique();
    }
}
```

### Registrar en `Program.cs` (después de var builder):
```csharp
using Usuarios.API.Data;
using Microsoft.EntityFrameworkCore;

// ... después de var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<UsuariosDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
```

---

## Código: Modelo

### `Models/Perfil.cs`
```csharp
namespace Usuarios.API.Models;

public class Perfil
{
    public Guid Id { get; set; }
    public string AuthId { get; set; } = string.Empty; // El "sub" del JWT de Supabase
    public string Email { get; set; } = string.Empty;
    public string? Nombre { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
```

---

## Código: DTOs

### `DTOs/UserDto.cs`
```csharp
namespace Usuarios.API.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public string AuthId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Nombre { get; set; }
}
```

---

## Código: Controlador

### `Controllers/UsersController.cs`
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Usuarios.API.Data;
using Usuarios.API.DTOs;
using Usuarios.API.Models;
using System.Security.Claims;

namespace Usuarios.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UsuariosDbContext _context;

    public UsersController(UsuariosDbContext context)
    {
        _context = context;
    }

    // POST /api/users/sync - Sincronizar usuario de Supabase
    [Authorize]
    [HttpPost("sync")]
    public async Task<IActionResult> SyncUser()
    {
        // Extraer datos del JWT
        var authId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                  ?? User.FindFirst("sub")?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value 
                 ?? User.FindFirst("email")?.Value;

        if (string.IsNullOrEmpty(authId) || string.IsNullOrEmpty(email))
            return Unauthorized("Token inválido");

        // Buscar si ya existe
        var perfilExistente = await _context.Perfiles
            .FirstOrDefaultAsync(p => p.AuthId == authId);

        if (perfilExistente != null)
        {
            return Ok(new UserDto
            {
                Id = perfilExistente.Id,
                AuthId = perfilExistente.AuthId,
                Email = perfilExistente.Email,
                Nombre = perfilExistente.Nombre
            });
        }

        // Crear nuevo perfil
        var nuevoPerfil = new Perfil
        {
            AuthId = authId,
            Email = email,
            Nombre = email.Split('@')[0] // Nombre por defecto
        };

        _context.Perfiles.Add(nuevoPerfil);
        await _context.SaveChangesAsync();

        return Ok(new UserDto
        {
            Id = nuevoPerfil.Id,
            AuthId = nuevoPerfil.AuthId,
            Email = nuevoPerfil.Email,
            Nombre = nuevoPerfil.Nombre
        });
    }

    // GET /api/users/me - Obtener mi perfil
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var authId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                  ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(authId))
            return Unauthorized();

        var perfil = await _context.Perfiles
            .FirstOrDefaultAsync(p => p.AuthId == authId);

        if (perfil == null)
            return NotFound("Usuario no encontrado");

        return Ok(new UserDto
        {
            Id = perfil.Id,
            AuthId = perfil.AuthId,
            Email = perfil.Email,
            Nombre = perfil.Nombre
        });
    }

    // GET /api/users - Listar todos (para que Nico y Pablo puedan buscar)
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var usuarios = await _context.Perfiles
            .Select(p => new UserDto
            {
                Id = p.Id,
                AuthId = p.AuthId,
                Email = p.Email,
                Nombre = p.Nombre
            })
            .ToListAsync();

        return Ok(usuarios);
    }
}
```

---

## Migraciones y Deploy

```bash
# Crear migración
cd Usuarios.API
dotnet ef migrations add InitialCreate
dotnet ef database update

# Probar localmente
dotnet run

# Debería correr en: https://localhost:7XXX
```

---

# 🏁 FASE 3: FRONTEND (UI)

## Opción: React con Vite

### Crear proyecto:
```bash
npm create vite@latest chat-ui -- --template react
cd chat-ui
npm install
npm install @supabase/supabase-js
npm install @microsoft/signalr
npm install axios
```

### Configurar Supabase: `src/supabaseClient.js`
```javascript
import { createClient } from '@supabase/supabase-js'

const supabaseUrl = 'https://xxxxx.supabase.co'
const supabaseAnonKey = 'tu_anon_key'

export const supabase = createClient(supabaseUrl, supabaseAnonKey)
```

### Login: `src/pages/Login.jsx`
```jsx
import { useState } from 'react'
import { supabase } from '../supabaseClient'

export default function Login() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')

  const handleLogin = async () => {
    const { data, error } = await supabase.auth.signInWithPassword({
      email,
      password
    })
    
    if (error) {
      alert(error.message)
    } else {
      // Llamar a tu API para sincronizar
      const token = data.session.access_token
      await fetch('https://usuarios-api-tup.azurewebsites.net/api/users/sync', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`
        }
      })
      
      // Redirigir al chat
      window.location.href = '/chat'
    }
  }

  return (
    <div>
      <h1>Login</h1>
      <input value={email} onChange={e => setEmail(e.target.value)} placeholder="Email" />
      <input value={password} onChange={e => setPassword(e.target.value)} placeholder="Password" type="password" />
      <button onClick={handleLogin}>Iniciar Sesión</button>
    </div>
  )
}
```

### Conectar SignalR: `src/signalrService.js`
```javascript
import * as signalR from '@microsoft/signalr'

let connection = null

export const connectToHub = async (token) => {
  connection = new signalR.HubConnectionBuilder()
    .withUrl('https://mensajes-api-tup.azurewebsites.net/chathub', {
      accessTokenFactory: () => token
    })
    .withAutomaticReconnect()
    .build()

  connection.on('MensajeRecibido', (mensaje) => {
    console.log('Nuevo mensaje:', mensaje)
    // Actualizar UI
  })

  await connection.start()
  console.log('Conectado a SignalR')
}

export const joinConversation = async (conversacionId) => {
  await connection.invoke('JoinConversation', conversacionId)
}

export const notificarEscribiendo = async (conversacionId) => {
  await connection.invoke('NotificarEscribiendo', conversacionId)
}
```

---

## Git Workflow (TODO EL PROYECTO)

```bash
# SIEMPRE antes de empezar
git pull origin develop --rebase

# Cuando termines algo
git add .
git commit -m "feat(ui): login y conexión con Supabase"
git pull origin develop --rebase
git push origin develop
```

---

## 🎯 Checklist Final (Antes de Entregar)

- [ ] Las 3 APIs compilan sin errores
- [ ] Las 3 APIs están desplegadas en Azure
- [ ] La UI se conecta a Supabase Auth
- [ ] La UI llama a `/api/users/sync` después del login
- [ ] La UI se conecta al Hub de SignalR
- [ ] Se pueden enviar mensajes directos
- [ ] Se pueden enviar mensajes grupales
- [ ] Se ve el "escribiendo..."
- [ ] Se marcan mensajes como leídos
- [ ] Todo funciona en producción (URLs de Azure)

---

¡Éxito, Ulises! Vos sos el que junta todo. 💪🚀
