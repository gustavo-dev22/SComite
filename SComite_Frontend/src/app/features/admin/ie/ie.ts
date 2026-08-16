import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { InstitucionService } from '../../../core/services/institucion.service';
import { AuthService } from '../../../core/services/auth.service';
import { InstitucionEducativa } from '../../../core/models/institucion.model';
import { manejarErrorHttp } from '../../../core/utils/http-error.util';
import Swal from 'sweetalert2';

const MAX_LOGO_MB = 1;
const TIPOS_LOGO_PERMITIDOS = ['image/png', 'image/jpeg', 'image/webp'];

@Component({
  selector: 'app-ie',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule],
  templateUrl: './ie.html',
  styleUrl: './ie.scss',
})
export class IeComponent implements OnInit {
  private destroyRef = inject(DestroyRef);
  private institucionService = inject(InstitucionService);
  private authService = inject(AuthService);
  private timerMensaje: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    this.destroyRef.onDestroy(() => this.limpiarTimerMensaje());
  }

  cargando = signal<boolean>(false);
  guardando = signal<boolean>(false);
  mensajeExito = signal<string | null>(null);

  formDatos = signal<Partial<InstitucionEducativa>>({
    nombreInstitucion: '',
    direccion: '',
    urlLogo: '',
    usuarioActualizacion: ''
  });

  ngOnInit(): void {
    this.cargarDatos();
  }

  cargarDatos(): void {
    this.cargando.set(true);
    this.institucionService.getConfiguracion().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        if (data) {
          this.formDatos.set(data);
        }
        this.cargando.set(false);
      },
      error: (err) => {
        this.cargando.set(false);
        manejarErrorHttp(err, 'No se pudieron cargar los datos de la institución.');
      }
    });
  }

  // Cargar imagen de logo local en formato Base64 para guardado seguro en la BD
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];

    // Validar tipo MIME permitido
    if (!TIPOS_LOGO_PERMITIDOS.includes(file.type)) {
      input.value = '';
      Swal.fire('Formato no válido', 'Solo se permiten imágenes PNG, JPG o WEBP.', 'warning');
      return;
    }

    // Validar tamaño máximo (evita strings base64 gigantes y errores HTTP 413)
    const maxBytes = MAX_LOGO_MB * 1024 * 1024;
    if (file.size > maxBytes) {
      input.value = '';
      Swal.fire('Imagen demasiado grande', `El logo no puede superar ${MAX_LOGO_MB} MB.`, 'warning');
      return;
    }

    const reader = new FileReader();

    reader.onload = (e: ProgressEvent<FileReader>) => {
      const result = e.target?.result;
      if (typeof result === 'string') {
        this.formDatos.update(f => ({ ...f, urlLogo: result }));
      }
    };

    reader.readAsDataURL(file);
  }

  removerLogo(): void {
    this.formDatos.update(f => ({ ...f, urlLogo: '' }));
  }

  actualizarCampo(campo: string, valor: unknown): void {
    this.formDatos.update(f => ({ ...f, [campo]: valor }) as Partial<InstitucionEducativa>);
  }

  guardar(): void {
    const datos = this.formDatos();
    if (!datos.nombreInstitucion?.trim()) return;

    const usuario = this.authService.usuarioActual();
    const nombreUsuario = usuario ? usuario : 'ADMINISTRADOR';

    this.guardando.set(true);
    this.mensajeExito.set(null);
    this.limpiarTimerMensaje();

    const payload = { ...datos, usuarioActualizacion: nombreUsuario };

    this.institucionService.guardarConfiguracion(payload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        this.guardando.set(false);

        // Actualizar la fecha y usuario de la UI inmediatamente
        this.formDatos.update(f => ({
          ...f,
          usuarioActualizacion: nombreUsuario,
          fechaActualizacion: res.fechaActualizacion || new Date().toISOString()
        }));

        this.mensajeExito.set('Los datos de la Institución Educativa se actualizaron correctamente.');
        this.timerMensaje = setTimeout(() => this.mensajeExito.set(null), 4000);
      },
      error: (err) => {
        this.guardando.set(false);
        manejarErrorHttp(err, 'No se pudo guardar la configuración.');
      }
    });
  }

  private limpiarTimerMensaje(): void {
    if (this.timerMensaje !== null) {
      clearTimeout(this.timerMensaje);
      this.timerMensaje = null;
    }
  }
}
