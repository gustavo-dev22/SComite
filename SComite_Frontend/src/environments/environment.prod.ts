// Configuración del entorno de producción.
//
// El dominio real de la API se resuelve con la siguiente prioridad:
//   1. window.__APP_API_URL__ : permite inyectar la URL en runtime desde el
//      index.html o un script de despliegue, sin necesidad de recompilar.
//   2. Marcador YOUR_PRODUCTION_API_URL : sustituir por el dominio definitivo
//      en el pipeline de CI/CD o directamente antes del despliegue.
const apiUrlInyectada =
  (window as unknown as { __APP_API_URL__?: string }).__APP_API_URL__;

export const environment = {
  production: true,
  apiUrl: apiUrlInyectada || 'YOUR_PRODUCTION_API_URL'
};
