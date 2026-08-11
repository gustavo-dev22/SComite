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
      });
  }

  const esSolicitudLogin = req.url.toLowerCase().includes('/auth/login');

  return next(req).pipe(
    catchError((error: unknown) => {
      const httpError =
        error instanceof HttpErrorResponse
          ? error
          : new HttpErrorResponse({ error, status: 0, statusText: 'Error' });

      if (!esSolicitudLogin && httpError.status === 401) {
        manejarSesionExpirada(authService, router);
      } else if (!esSolicitudLogin && httpError.status === 403) {
        manejarPermisosInsuficientes();
      } else if (!esSolicitudLogin && httpError.status === 429) {
        manejarDemasiadasPeticiones();
      } else if (!esSolicitudLogin && httpError.status >= 500 && httpError.status <= 504) {
        manejarServicioNoDisponible();
      }

      return throwError(() => error);
    })
  );
};

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

function manejarServicioNoDisponible(): void {
  if (servicioNoDisponibleAlertaEnCurso) return;
  servicioNoDisponibleAlertaEnCurso = true;

  void Swal.fire({
    icon: 'error',
    title: 'Servicio no disponible',
    text: 'Servicio no disponible temporalmente. Intente nuevamente en unos minutos.',
    confirmButtonColor: '#2563eb',
    confirmButtonText: 'Entendido'
  }).then(() => {
    servicioNoDisponibleAlertaEnCurso = false;
  });
}