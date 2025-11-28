# Script para iniciar todo el sistema ChatApp

Write-Host "Iniciando ChatApp System..." -ForegroundColor Green

# 0. Compilar Solucion
Write-Host "Compilando solucion..." -ForegroundColor Yellow
dotnet build ChatApp.sln
if ($LASTEXITCODE -ne 0) {
    Write-Error "Error en la compilacion. Deteniendo inicio."
    exit
}

# 1. Iniciar Usuarios.API
Write-Host "Iniciando Usuarios.API..."
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd Usuarios.API; dotnet run"

# 2. Iniciar Grupos.API
Write-Host "Iniciando Grupos.API..."
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd Grupos.API; dotnet run"

# 3. Iniciar Mensajes.API
Write-Host "Iniciando Mensajes.API..."
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd Mensajes.API; dotnet run"

# 4. Iniciar Frontend (UI)
Write-Host "Iniciando Frontend..."
if (Test-Path "chat-app-ui") {
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd chat-app-ui; npm install; npm run dev"
} else {
    Write-Warning "No se encontro la carpeta chat-app-ui"
}

Write-Host "Todo iniciado. Revisa las nuevas ventanas." -ForegroundColor Green
Write-Host '--------------------------------------------------'
Write-Host "URLs de Acceso:" -ForegroundColor Cyan
Write-Host "   Usuarios API: http://localhost:5156/swagger"
Write-Host "   Grupos API:   http://localhost:5022/swagger"
Write-Host "   Mensajes API: http://localhost:5078/swagger"
Write-Host "   Frontend UI:  http://localhost:5173"
Write-Host '--------------------------------------------------'
