# TechSupport Solutions (.NET 10)

Sistema integral para la gestión y seguimiento de incidentes de soporte técnico, desarrollado con arquitectura desacoplada utilizando **ASP.NET Core Web API** y un frontend **ASP.NET Core MVC** sobre **.NET 10**.

---

## 🏛️ Arquitectura del Sistema

- **`TechSupport.API`**: Servicio RESTful con Entity Framework Core SQLite, autenticación JWT Bearer, hashing de contraseñas con BCrypt, middleware centralizado de manejo de errores y validación estricta de matriz de permisos por rol.
- **`TechSupport.MVC`**: Aplicación web frontend que implementa autenticación basada en Cookies, consumo de la API mediante `HttpClient` con propagación del token JWT, y renderizado condicional de componentes UI según el rol del usuario autenticado.

---

## 👥 Usuarios Preconfigurados

El sistema inicializa automáticamente la base de datos SQLite (`techsupport.db`) y siembra los siguientes usuarios con contraseñas cifradas en BCrypt:

| Usuario | Contraseña | Rol | Permisos Principales |
|---|---|---|---|
| `admin` | `Admin123*` | `Administrador` | Ver todos los incidentes, asignar técnicos, avanzar estados y eliminar incidentes (no cerrados). |
| `tecnico` | `Tecnico123*` | `Tecnico` | Ver únicamente incidentes asignados a él, y avanzar estado de sus asignados (`Asignado` → `EnProceso` → `Resuelto`). |
| `usuario` | `Usuario123*` | `Usuario` | Crear incidentes y consultar únicamente los incidentes reportados por él. |

---

## 🔄 Ciclo de Vida del Incidente

El estado de los incidentes avanza de forma estrictamente secuencial:

```text
[Pendiente] ──(Admin asigna técnico)──> [Asignado] ──(Técnico/Admin)──> [En Proceso] ──(Técnico/Admin)──> [Resuelto] ──(Admin/Técnico)──> [Cerrado]
```

- Al pasar a **Resuelto**, se captura automáticamente la `FechaResolucion` (UTC).
- **No se puede cerrar** un incidente sin antes haber sido resuelto.
- **No se puede eliminar** un incidente si se encuentra en estado **Cerrado**.
- El `UsuarioId` del reportador se asigna de forma inviolable desde los claims del token JWT, nunca desde el cuerpo de la solicitud.

---

## 🚀 Instrucciones de Ejecución

### 1. Iniciar la Web API
```powershell
dotnet run --project TechSupport.API/TechSupport.API.csproj --launch-profile http
```
- Endpoint base: `http://localhost:5018`
- Documentación OpenAPI: `http://localhost:5018/openapi/v1.json`

### 2. Iniciar el Frontend MVC (en otra terminal)
```powershell
dotnet run --project TechSupport.MVC/TechSupport.MVC.csproj --launch-profile http
```
- URL web: `http://localhost:5079`

---

## 🛡️ Manejo Centralizado de Excepciones

La API cuenta con `ExceptionHandlingMiddleware` que intercepta cualquier error no controlado, lo registra en logs mediante `ILogger` y responde un JSON limpio con código HTTP 500:

```json
{
  "success": false,
  "message": "Ha ocurrido un error interno en el servidor. Por favor, comuníquese con el administrador del sistema.",
  "requestId": "0HN7...",
  "timestamp": "2026-11-09T..."
}
```
