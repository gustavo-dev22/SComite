import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { InstitucionService } from '../../../core/services/institucion.service';
import { AuthService } from '../../../core/services/auth.service';
import { InstitucionEducativa } from '../../../core/models/institucion.model';

@Component({
  selector: 'app-ie',
  imports: [CommonModule, FormsModule],
  templateUrl: './ie.html',
  styleUrl: './ie.scss',
})
export class IeComponent implements OnInit {
  private institucionService = inject(InstitucionService);
  private authService = inject(AuthService);

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
    this.institucionService.getConfiguracion().subscribe({
      next: (data) => {
        if (data) {
          this.formDatos.set(data);
        }
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false)
    });
  }

  // Cargar imagen de logo local en formato Base64 para guardado seguro en la BD
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      const file = input.files[0];
      const reader = new FileReader();

      reader.onload = (e: any) => {
        this.formDatos.update(f => ({ ...f, urlLogo: e.target.result }));
      };

      reader.readAsDataURL(file);
    }
  }

  removerLogo(): void {
    this.formDatos.update(f => ({ ...f, urlLogo: '' }));
  }

  guardar(): void {
    const datos = this.formDatos();
    if (!datos.nombreInstitucion?.trim()) return;

    const usuario = this.authService.usuarioActual();
    const nombreUsuario = usuario ? usuario : 'ADMINISTRADOR';
    datos.usuarioActualizacion = nombreUsuario;

    this.guardando.set(true);
    this.mensajeExito.set(null);

    this.institucionService.guardarConfiguracion(datos).subscribe({
      next: (res: any) => {
        this.guardando.set(true);
        
        // 🚀 Actualizar la fecha y usuario de la UI inmediatamente
        this.formDatos.update(f => ({
          ...f,
          usuarioActualizacion: nombreUsuario,
          fechaActualizacion: res.fechaActualizacion || new Date().toISOString()
        }));

        this.guardando.set(false);
        this.mensajeExito.set('Los datos de la Institución Educativa se actualizaron correctamente.');
        setTimeout(() => this.mensajeExito.set(null), 4000);
      },
      error: () => this.guardando.set(false)
    });
  }
}
