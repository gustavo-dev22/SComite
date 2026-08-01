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
        console.log('Ruta inicial determinada:', rutaInicial);
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

    if (!rolInicial) return 'login';

    const objetos = rolInicial.objetos || [];

    const obtenerNumeroOrden = (obj: any): number => {
      if (obj.orden !== undefined && obj.orden !== null) return Number(obj.orden);
      if (obj.posicion !== undefined && obj.posicion !== null) return Number(obj.posicion);
      if (obj.id !== undefined && obj.id !== null) return Number(obj.id);
      return 0;
    };

    const ordenarLista = (lista: any[]) => {
      return [...lista].sort((a, b) => obtenerNumeroOrden(a) - obtenerNumeroOrden(b));
    };

    const urlsEnOrdenLectura: string[] = [];

    const recorrerMenu = (nodos: any[]) => {
      const nodosOrdenados = ordenarLista(nodos);

      for (const nodo of nodosOrdenados) {
        if (!nodo || nodo.activo === false) continue;

        if (nodo.url && nodo.url !== '#' && nodo.url !== '/' && nodo.url !== 'javascript:void(0);') {
          const urlLimpia = nodo.url.startsWith('/') ? nodo.url.substring(1) : nodo.url;
          urlsEnOrdenLectura.push(urlLimpia);
        }

        const hijos = nodo.subObjetos || nodo.hijos || [];
        if (hijos.length > 0) {
          recorrerMenu(hijos);
        }
      }
    };

    recorrerMenu(objetos);

    // Obtener todas las rutas declaradas en Angular
    const rutasExistentes = this.router.config
      .flatMap(r => r.children || [])
      .map(c => c.path);

    // 🚀 FILTRO DINÁMICO DE PÁGINAS DE ATERRIZAJE (LANDING)
    // Ningún usuario debe aterrizar en mantenimiento, logs o auditorías al iniciar sesión.
    const esRutaUtilitariaSistema = (url: string): boolean => {
      const urlLower = url.toLowerCase();
      return urlLower.includes('mantenimiento') || 
            urlLower.includes('logs') || 
            urlLower.includes('auditoria') || 
            urlLower.includes('seguridad/');
    };

    // 1. Buscar la primera ruta operativa válida que coincida en Angular y SASI (excluyendo utilitarios)
    const primeraRutaOperativa = urlsEnOrdenLectura.find(url => 
      rutasExistentes.includes(url) && !esRutaUtilitariaSistema(url)
    );

    if (primeraRutaOperativa) {
      return primeraRutaOperativa;
    }

    // 2. Si solo tuviera permisos a herramientas de sistema, tomar la primera disponible
    const primeraRutaCualquiera = urlsEnOrdenLectura.find(url => rutasExistentes.includes(url));

    return primeraRutaCualquiera || 'login';
  }
}
