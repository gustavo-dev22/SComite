import { HttpErrorResponse } from '@angular/common/http';
import Swal from 'sweetalert2';

const STATUS_MANEJADOS_GLOBALMENTE = [401, 403, 429];

export function manejarErrorHttp(err: unknown, mensajeFallback: string): void {
  const status = err instanceof HttpErrorResponse ? err.status : 0;

  const esErrorGlobal =
    STATUS_MANEJADOS_GLOBALMENTE.includes(status) || (status >= 500 && status <= 504);

  if (esErrorGlobal) {
    // El errorInterceptor ya mostró la alerta global (sesión expirada, permisos, etc.)
    return;
  }

  const mensaje =
    err instanceof HttpErrorResponse ? (err.error?.mensaje ?? mensajeFallback) : mensajeFallback;

  void Swal.fire('Error', mensaje, 'error');
}