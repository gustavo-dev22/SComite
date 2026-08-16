import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { NavigationStart, Router } from '@angular/router';
import { catchError, filter, throwError } from 'rxjs';
import Swal from 'sweetalert2';
import { AuthService } from '../services/auth.service';

let sesionExpiradaEnCurso = false;
let permisosAlertaEnCurso = false;
let demasiadasPeticionesAlertaEnCurso = false;
let servicioNoDisponibleAlertaEnCurso = false;
let sinConexionAlertaEnCurso = false;
let suscripcionNavegacionRegistrada = false;

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Si el usuario navega a otra ruta antes de que se resuelvan las alertas,
  // re-limpiar las banderas globales evita que queden bloqueadas indefinidamente.
  if (!suscripcionNavegacionRegistrada) {
    suscripcionNavegacionRegistrada = true;
    router.events
      .pipe(filter((e) => e instanceof NavigationStart))
      .subscribe(() => {
        sesionExpiradaEnCurso = false;
        permisosAlertaEnCurso = false;
        demasiadasPeticionesAlertaEnCurso = false;
        servicioNoDisponibleAlertaEnCurso = false;
        sinConexionAlertaEnCurso = false;
      });
  }

  const esSolicitudLogin = req.url.toLowerCase().includes('/auth/login');

  return next(req).pipe(
    catchError((error: unknown) => {
      const httpError =
        error instanceof HttpErrorResponse
          ? error
          : new HttpErrorResponse({ error, status: 0, statusText: 'Error' });

      // Status 0 = error de red / backend caído / sin conexión con el servidor.
      // Se informa al usuario con una alerta global única (el sistema está caído),
      // en lugar de mostrar mensajes por recurso que inducen a error.
      if (httpError.status === 0) {
        manejarSinConexion();
      } else if (!esSolicitudLogin && httpError.status === 401) {
        manejarSesionExpirada(authService, router);
      } else if (!esSolicitudLogin && httpError.status === 403) {
        manejarPermisosInsuficientes();
      } else if (!esSolicitudLogin && httpError.status === 429) {
        manejarDemasiadasPeticiones();
      } else if (!esSolicitudLogin && httpError.status >= 500 && httpError.status <= 504) {
        // Se prioriza el mensaje del backend (p. ej. SASI no disponible con 503)
        // para que el usuario reciba el detalle específico en lugar de un texto genérico.
        const cuerpo = httpError.error as { mensaje?: string; detail?: string } | null;
        const mensajeBackend = cuerpo?.mensaje ?? cuerpo?.detail;
        manejarServicioNoDisponible(mensajeBackend);
      }

      return throwError(() => error);
    })
  );
};

/**
 *  Alerta global cuando NO se puede alcanzar el servidor (status 0):
 * backend detenido, red caída, CORS bloqueado o servidor apagado. Se muestra una
 * sola vez (bandera) para no acumular múltiples alertas por cada petición fallida.
 */
function manejarSinConexion(): void {
  if (sinConexionAlertaEnCurso) return;
  sinConexionAlertaEnCurso = true;

  void Swal.fire({
    icon: 'error',
    title: 'Sistema no disponible',
    text: 'No se pudo conectar con el servidor. Verifique su conexión o intente nuevamente en unos minutos.',
    confirmButtonColor: '#2563eb',
    confirmButtonText: 'Entendido'
  }).then(() => {
    sinConexionAlertaEnCurso = false;
  });
}

function manejarSesionExpirada(authService: AuthService, router: Router): void {
  if (sesionExpiradaEnCurso) return;
  sesionExpiradaEnCurso = true;

  authService.limpiarSesion();

  void Swal.fire({
    icon: 'warning',
    title: 'Sesión expirada',
    text: 'Tu sesión ha caducado. Ingresa nuevamente para continuar.',
    confirmButtonColor: '#2563eb',
    confirmButtonText: 'Aceptar',
    allowOutsideClick: false,
    allowEscapeKey: false
  }).then(() => {
    sesionExpiradaEnCurso = false;
    router.navigate(['/login']);
  });
}

function manejarPermisosInsuficientes(): void {
  if (permisosAlertaEnCurso) return;
  permisosAlertaEnCurso = true;

  void Swal.fire({
    icon: 'error',
    title: 'Acceso denegado',
    text: 'No tienes permisos suficientes para realizar esta acción.',
    confirmButtonColor: '#2563eb',
    confirmButtonText: 'Entendido'
  }).then(() => {
    permisosAlertaEnCurso = false;
  });
}

function manejarDemasiadasPeticiones(): void {
  if (demasiadasPeticionesAlertaEnCurso) return;
  demasiadasPeticionesAlertaEnCurso = true;

  void Swal.fire({
    icon: 'warning',
    title: 'Demasiadas peticiones',
    text: 'Demasiadas peticiones. Intente nuevamente en unos segundos.',
    confirmButtonColor: '#2563eb',
    confirmButtonText: 'Entendido'
  }).then(() => {
    demasiadasPeticionesAlertaEnCurso = false;
  });
}

function manejarServicioNoDisponible(mensajeBackend?: string): void {
  if (servicioNoDisponibleAlertaEnCurso) return;
  servicioNoDisponibleAlertaEnCurso = true;

  void Swal.fire({
    icon: 'error',
    title: 'Servicio no disponible',
    text: mensajeBackend ?? 'Servicio no disponible temporalmente. Intente nuevamente en unos minutos.',
    confirmButtonColor: '#2563eb',
    confirmButtonText: 'Entendido'
  }).then(() => {
    servicioNoDisponibleAlertaEnCurso = false;
  });
}