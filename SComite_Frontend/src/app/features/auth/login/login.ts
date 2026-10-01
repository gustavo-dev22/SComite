import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-login',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class LoginComponent {
  private route = inject(ActivatedRoute);
  private authService = inject(AuthService);

  cargando = signal<boolean>(false);
  errorMensaje = signal<string | null>(null);

  // Acceso único mediante el SSO institucional (SASI): el usuario ingresa sus
  // credenciales en la pantalla de SASI y vuelve a /sso-callback.
  async iniciarSso(): Promise<void> {
    if (this.cargando()) return; // Evita doble redirección por clics repetidos

    this.errorMensaje.set(null);
    this.cargando.set(true);

    Swal.fire({
      title: 'Conectando con SASI...',
      text: 'Redirigiendo al inicio de sesión institucional.',
      allowOutsideClick: false,
      allowEscapeKey: false,
      didOpen: () => {
        Swal.showLoading();
      }
    });

    try {
      const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? undefined;
      await this.authService.iniciarSsoSesion(returnUrl);
      // Si la redirección es exitosa el navegador sale de la aplicación.
    } catch (err) {
      this.cargando.set(false);

      const mensaje = err instanceof Error && err.message
        ? err.message
        : 'No se pudo conectar con el servicio de autenticación (SASI).';

      this.errorMensaje.set(mensaje);

      void Swal.fire({
        icon: 'error',
        title: 'Servicio no disponible',
        text: mensaje,
        confirmButtonColor: '#2563eb',
        confirmButtonText: 'Entendido'
      });
    }
  }
}
