import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateFn, Router, UrlTree } from '@angular/router';
import Swal from 'sweetalert2';
import { AuthService } from '../services/auth.service';

let alertaEnCurso = false;

export const roleGuard: CanActivateFn = (route: ActivatedRouteSnapshot): boolean | UrlTree => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.tieneSesionValida()) return true;

  const rolActivo = authService.rolActivoObj();
  if (!rolActivo) return true;

  const rolesPermitidos = (route.data?.['roles'] as string[] | undefined) ?? [];

  const url = route.pathFromRoot
    .flatMap(segmento => segmento.url.map(segmentoUrl => segmentoUrl.path))
    .join('/');

  const rolPermitido = rolesPermitidos.some(
    rol => rol.toLowerCase() === rolActivo.nombreRol.toLowerCase()
  );
  const urlPermitidaPorMenu = authService.urlPermitidaEnMenu(url);

  if (rolPermitido || urlPermitidaPorMenu) return true;

  if (!alertaEnCurso) {
    alertaEnCurso = true;
    void Swal.fire({
      icon: 'error',
      title: 'Acceso denegado',
      text: 'No tienes permisos para acceder a esta sección.',
      confirmButtonColor: '#2563eb',
      confirmButtonText: 'Entendido'
    }).then(() => {
      alertaEnCurso = false;
    });
  }

  return router.createUrlTree([authService.obtenerRutaInicial()]);
};
