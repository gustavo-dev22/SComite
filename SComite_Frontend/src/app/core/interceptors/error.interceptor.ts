import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import Swal from 'sweetalert2';
import { AuthService } from '../services/auth.service';

let sesionExpiradaEnCurso = false;

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const esSolicitudLogin = req.url.toLowerCase().includes('/auth/login');

  return next(req).pipe(
    catchError((error: unknown) => {
      const httpError =
        error instanceof HttpErrorResponse
          ? error
          : new HttpErrorResponse({ error, status: 0, statusText: 'Error' });

      if (!esSolicitudLogin && httpError.status === 401) {
        manejarSesionExpirada(authService, router);
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
