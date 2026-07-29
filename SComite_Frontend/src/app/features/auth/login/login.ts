import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import Swal from 'sweetalert2';
import { AuthResponse } from '../../../core/models/sasi.model';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private authService = inject(AuthService);

  cargando = signal<boolean>(false);
  mostrarPassword = signal<boolean>(false);
  errorMensaje = signal<string | null>(null);

  loginForm: FormGroup = this.fb.group({
    userName: ['', [Validators.required]],
    password: ['', [Validators.required, Validators.minLength(4)]]
  });

  toggleMostrarPassword(): void {
    this.mostrarPassword.update(value => !value);
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

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

    this.authService.login(this.loginForm.value).subscribe({
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

        const rutaInicial = this.obtenerRutaInicial(res);
        this.router.navigate([rutaInicial]);
      },
      error: (err) => {
        this.cargando.set(false);
        const mensajeError = err.error?.mensaje || 'Ocurrió un error al intentar iniciar sesión.';
        
        // 🚀 Alerta de Error/Rechazo de Acceso con SweetAlert2
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

  private obtenerRutaInicial(res: AuthResponse): string {
    const roles = res.sistemaComite?.roles || [];
    const rolInicial = roles.find(r => r.esPrincipal) || roles[0];

    if (!rolInicial) return 'admin/periodos';

    const objetos = rolInicial.objetos || [];
    const urlsSasi: string[] = [];

    const aplanarObjetos = (lista: any[]) => {
      for (const obj of lista) {
        if (obj.activo && obj.url && obj.url !== '#' && obj.url !== '/') {
          const urlLimpia = obj.url.startsWith('/') ? obj.url.substring(1) : obj.url;
          urlsSasi.push(urlLimpia);
        }
        if (obj.subObjetos) aplanarObjetos(obj.subObjetos);
        if (obj.hijos) aplanarObjetos(obj.hijos);
      }
    };

    aplanarObjetos(objetos);

    // Verificamos si la ruta existe en la tabla de rutas del Router de Angular
    const rutasExistentes = this.router.config
      .flatMap(r => r.children || [])
      .map(c => c.path);

    const rutaValida = urlsSasi.find(u => rutasExistentes.includes(u));

    return rutaValida || 'admin/periodos';
  }
}
