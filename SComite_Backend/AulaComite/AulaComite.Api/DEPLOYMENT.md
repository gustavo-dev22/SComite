# Guía de Despliegue — AulaComite.Api (Backend .NET)

Este documento detalla las variables de entorno necesarias para desplegar el
Backend en Producción. Los valores sensibles **no deben** colocarse en
`appsettings.json` ni `appsettings.Production.json` (ambos versionados en git);
deben inyectarse como variables de entorno o en el secret manager del entorno
de hospedaje.

> Importante: `appsettings.Local.json` está excluido de git (ver `.gitignore`)
> y solo se usa para desarrollo local. **Ningún secreto real debe committearse.**

## Entorno de ejecución

| Variable | Valor |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` (activa `appsettings.Production.json`) |

## Variables de entorno requeridas

### Conexión a Base de Datos (SQL Server)

| Variable | Descripción |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | Cadena de conexión de SQL Server. Ej.: `Server=...;Database=db_ComiteAula;Integrated Security=...;TrustServerCertificate=true` |

### Autenticación JWT (obligatorio)

| Variable | Descripción |
| --- | --- |
| `JwtSettings__SecretKey` | Clave simétrica (mínimo 32 caracteres). **Cambiar en cada entorno.** |
| `JwtSettings__Issuer` | Emisor del token (ej.: `SASI`). |
| `JwtSettings__Audience` | Audiencia del token (ej.: `SASI_CLIENT`). |

Si falta alguna, la aplicación falla al arrancar con un mensaje claro
(`Program.cs` → sección `JwtSettings`).

### Integración SASI (login de usuarios)

| Variable | Descripción |
| --- | --- |
| `SasiSettings__BaseUrl` | URL base del API de SASI (ej.: `https://host-sasi/SASI/api/`). |
| `SasiSettings__SistemaId` | Identificador del sistema en SASI (ej.: `7`). |

### Almacenamiento de comprobantes (Cloudinary)

El servicio `CloudinaryFileStorageService` solo se usa en Producción (en
Desarrollo se usa `LocalFileStorageService`). Requiere las tres credenciales:

| Variable | Descripción |
| --- | --- |
| `Cloudinary__CloudName` | Nombre de la cuenta Cloudinary. |
| `Cloudinary__ApiKey` | API Key de Cloudinary. |
| `Cloudinary__ApiSecret` | API Secret de Cloudinary. |

### CORS (orígenes permitidos)

| Variable | Descripción |
| --- | --- |
| `Cors__AllowedOrigins__0` | Primer origen permitido (ej.: `https://comite.mi-dominio.com`). |
| `Cors__AllowedOrigins__1` | Segundo origen permitido (opcional). Repetir por cada origen. |

> Si no se configuran orígenes, la política CORS niega todas las solicitudes
> de orígenes cruzados (sin comodines), garantizando un comportamiento seguro
> por defecto.

### Hosts permitidos

| Variable | Descripción |
| --- | --- |
| `AllowedHosts` | Hosts que pueden llegar al API (ej.: `comite-api.mi-dominio.com;localhost`). |

## Migraciones de Base de Datos

En Producción las migraciones **no** se aplican automáticamente. Aplícalas vía
CI/CD o herramienta dedicada, por ejemplo:

```bash
dotnet ef database update --project AulaComite.Infrastructure --startup-project AulaComite.Api --configuration Release
```

## Notas del Frontend (Angular)

- `src/environments/environment.prod.ts` usa un placeholder `YOUR_PRODUCTION_API_URL`
  como URL de la API por defecto.
- Sustitúyelo en el pipeline de CI/CD, **o** inyéctalo en runtime definiendo
  `window.__APP_API_URL__` en el `index.html` o un script de despliegue, sin
  necesidad de recompilar:
  ```html
  <script>window.__APP_API_URL__ = 'https://comite-api.mi-dominio.com/api';</script>
  ```

## Archivos de configuración versionados

| Archivo | Contenido | ¿Secretos? |
| --- | --- | --- |
| `appsettings.json` | Config base (Conexión, SASI, CORS, Serilog, Cloudinary, AllowedHosts). | No (los valores Cloudinary están vacíos; se inyectan por entorno). |
| `appsettings.Production.json` | Overrides para Producción (CORS, AllowedHosts). | No. |
| `appsettings.Local.json` | Overrides locales (JWT, Cloudinary) para desarrollo. | Excluido de git. |
