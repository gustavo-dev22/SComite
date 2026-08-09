import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-login',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class LoginComponent {
  private destroyRef = inject(DestroyRef);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private authService = inject(AuthService);

  cargando = signal<boolean>(false);
  mostrarPassword = signal<boolean>(false);
  errorMensaje = signal<string | null>(null);

  loginForm: FormGroup = this.fb.group({
    userName: ['', [Validators.required]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  toggleMostrarPassword(): void {
    this.mostrarPassword.update(value => !value);
  }

  onSubmit(): void {
    if (this.cargando()) return; // Evita doble envío por clics repetidos
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.errorMensaje.set(null);
    this.cargando.set(true);

    Swal.fire({
      title: 'Autenticando...',
      text: 'Validando credenciales, por favor espere...',
      allowOutsideClick: false,
      allowEscapeKey: false,
      didOpen: () => {
        Swal.showLoading();
      }
    });

    this.authService.login(this.loginForm.value).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        this.cargando.set(false);
        
        Swal.fire({
          icon: 'success',
          title: `¡Bienvenido, ${res.nombreUsuario}!`,
          text: 'Acceso verificado correctamente.',
          toast: true,
          position: 'top-end',
          showConfirmButton: false,
          timer: 2000,
          timerProgressBar: true
        });

        const rutaInicial = this.authService.obtenerRutaInicial();
        this.router.navigate([rutaInicial]);
      },
      error: (err) => {
        this.cargando.set(false);
        const mensajeError = err.error?.mensaje || 'Ocurrió un error al intentar iniciar sesión.';
        this.errorMensaje.set(mensajeError);
        Swal.fire({
          icon: 'error',
          title: 'Acceso Denegado',
          text: mensajeError,
          confirmButtonColor: '#2563eb',
          confirmButtonText: 'Entendido'
        });
      }
    });
  }
}
