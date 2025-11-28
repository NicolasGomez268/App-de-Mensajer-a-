# TUP - Integrador - Chat RealTime

Sistema de mensajería en tiempo real con .NET 9, SignalR, PostgreSQL y Supabase Auth.

## 🏗️ Arquitectura

- **Usuarios.API**: Gestión de perfiles y sincronización con Supabase Auth
- **Grupos.API**: Creación y administración de grupos de chat
- **Mensajes.API**: Envío de mensajes y comunicación en tiempo real con SignalR
- **Shared.Kernel**: Configuración compartida de autenticación JWT

## 🚀 Tecnologías

- .NET 9
- ASP.NET Core Web API
- SignalR (WebSockets)
- PostgreSQL (Supabase)
- Entity Framework Core
- JWT Authentication

## 👥 Equipo

- **Ulises**: Arquitecto, Usuarios.API y Frontend
- **Nicolás**: Grupos.API
- **Pablo**: Mensajes.API y SignalR Hub

## 📦 Estructura del Proyecto

```
ChatApp/
├── Usuarios.API/
├── Grupos.API/
├── Mensajes.API/
└── Shared.Kernel/
```

## 🔧 Configuración

Ver las guías individuales:
- `PARTE_ULISES_SETUP_Y_UI.md`
- `PARTE_NICOLAS_GRUPOS.md`
- `PARTE_PABLO_MENSAJES.md`
