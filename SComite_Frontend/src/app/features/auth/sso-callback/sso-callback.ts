import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import Swal from 'sweetalert2';

import { AuthService } from '../../../core/services/auth.service';
import { extraerMensajeError } from '../../../core/utils/http-error.util';

@Component({
  selector: 'app-sso-callback',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule],
  templateUrl: './sso-callback.html',
  styleUrl: './sso-callback.scss'
})
export class SsoCallbackComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private authService = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  ngOnInit(): void {
    const params = this.route.snapshot.queryParamMap;
    const code = params.get('code');
    const state = params.get('state');
    const error = params.get('error') ?? params.get('error_description');

    if (error) {
      this.fallar('No se pudo completar el inicio de sesión con SASI.');
      return;
    }

    if (!code || !state) {
      this.fallar('La respuesta de autenticación está incompleta.');
      return;
    }

    Swal.fire({
      title: 'Validando acceso...',
      text: 'Estamos completando el inicio de sesión seguro con SASI.',
      allowOutsideClick: false,
      allowEscapeKey: false,
      didOpen: () => {
        Swal.showLoading();
      }
    });

    this.authService.completarSsoSesion(code, state)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          void Swal.fire({
            icon: 'success',
            title: `¡Bienvenido, ${res.nombreUsuario}!`,
            text: 'Acceso verificado correctamente.',
            toast: true,
            position: 'top-end',
            showConfirmButton: false,
            timer: 2000,
            timerProgressBar: true
          });

          const returnUrl = this.authService.obtenerReturnUrlSso();
          if (returnUrl) {
            this.router.navigateByUrl(returnUrl);
          } else {
            this.router.navigate([this.authService.obtenerRutaInicial()]);
          }
        },
        error: (err: unknown) => {
          this.fallar(this.extraerMensaje(err));
        }
      });
  }

  private fallar(mensaje: string): void {
    void Swal.fire({
      icon: 'error',
      title: 'Error de acceso',
      text: mensaje,
      confirmButtonColor: '#2563eb',
      confirmButtonText: 'Entendido'
    }).then(() => this.router.navigate(['/login']));
  }

  private extraerMensaje(err: unknown): string {
    if (err instanceof HttpErrorResponse) {
      if (err.status === 0) {
        return 'No se pudo conectar con el servicio de autenticación (SASI). Verifique su conexión e intente nuevamente.';
      }

      const cuerpo = err.error as { error_description?: string; mensaje?: string; message?: string } | null;
      const descripcion = cuerpo?.error_description ?? cuerpo?.mensaje ?? cuerpo?.message;
      if (typeof descripcion === 'string' && descripcion.trim()) return descripcion;

      return extraerMensajeError(err, 'No se pudo validar el acceso.');
    }

    if (err instanceof Error && err.message) return err.message;
    return 'No se pudo validar el acceso.';
  }
}
