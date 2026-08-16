import { HttpErrorResponse } from '@angular/common/http';
import Swal from 'sweetalert2';

const STATUS_MANEJADOS_GLOBALMENTE = [0, 401, 403, 429];

/**
 *  Extrae el mensaje legible de una respuesta de error HTTP. Compatible con el
 * contrato propio (campo `mensaje`) y con ProblemDetails RFC 7807 (campo `detail`)
 * emitido por el ExceptionHandlingMiddleware del backend.
 */
export function extraerMensajeError(err: unknown, mensajeFallback: string): string {
  if (!(err instanceof HttpErrorResponse)) return mensajeFallback;

  const cuerpo = err.error as { mensaje?: string; detail?: string } | null;
  return cuerpo?.mensaje ?? cuerpo?.detail ?? mensajeFallback;
}

export function manejarErrorHttp(err: unknown, mensajeFallback: string): void {
  const status = err instanceof HttpErrorResponse ? err.status : 0;

  const esErrorGlobal =
    STATUS_MANEJADOS_GLOBALMENTE.includes(status) || (status >= 500 && status <= 504);

  if (esErrorGlobal) {
    // El errorInterceptor ya mostró la alerta global (sesión expirada, permisos,
    // demasiadas peticiones, sistema caído sin conexión, servicio no disponible, etc.)
    return;
  }

  const mensaje = extraerMensajeError(err, mensajeFallback);

  void Swal.fire('Error', mensaje, 'error');
}