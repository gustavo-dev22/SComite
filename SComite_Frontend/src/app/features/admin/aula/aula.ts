import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import Swal from 'sweetalert2';
import { AulaService } from '../../../core/services/aula.service';
import { PeriodoLectivo } from '../../../core/models/periodoLectivo.model';
import { Aula } from '../../../core/models/aula.model';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-aula',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './aula.html',
  styleUrl: './aula.scss',
})
export class AulaComponent implements OnInit {
  private aulaService = inject(AulaService);
  private fb = inject(FormBuilder);

  aulas = signal<Aula[]>([]);
  periodos = signal<PeriodoLectivo[]>([]);
  cargando = signal<boolean>(false);
  modalAbierto = signal<boolean>(false);
  esEdicion = signal<boolean>(false);

  periodoFiltroSelected = signal<number | null>(null);

  aulaForm: FormGroup = this.fb.group({
    id: [0],
    periodoId: ['', [Validators.required]],
    nivel: ['PRIMARIA', [Validators.required]],
    grado: ['', [Validators.required]],
    seccion: ['', [Validators.required]]
  });

  onInputToUppercase(controlName: string): void {
    const control = this.aulaForm.get(controlName);
    if (control && control.value) {
      control.patchValue(control.value.toUpperCase(), { emitEvent: false });
    }
  }

  ngOnInit(): void {
    this.cargarPeriodos();
    this.cargarAulas();
  }

  cargarPeriodos(): void {
    this.aulaService.getPeriodos().subscribe(data => {
      this.periodos.set(data);
    });
  }

  cargarAulas(): void {
    this.cargando.set(true);
    this.aulaService.getAulas(this.periodoFiltroSelected() || undefined).subscribe({
      next: (data) => {
        this.aulas.set(data);
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false)
    });
  }

  filtrarPorPeriodo(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.periodoFiltroSelected.set(value ? Number(value) : null);
    this.cargarAulas();
  }

  abrirModalCrear(): void {
    this.esEdicion.set(false);
    this.aulaForm.reset({
      id: 0,
      periodoId: '',
      nivel: 'PRIMARIA',
      grado: '',
      seccion: ''
    });
    this.modalAbierto.set(true);
  }

  abrirModalEditar(aula: Aula): void {
    this.esEdicion.set(true);
    this.aulaForm.patchValue({
      id: aula.id,
      periodoId: aula.periodoId,
      nivel: aula.nivel,
      grado: aula.grado,
      seccion: aula.seccion
    });
    this.modalAbierto.set(true);
  }

  cerrarModal(): void {
    this.modalAbierto.set(false);
  }

  guardarAula(): void {
    if (this.aulaForm.invalid) {
      this.aulaForm.markAllAsTouched();
      return;
    }

    const formValues = this.aulaForm.value;

    if (this.esEdicion()) {
      this.aulaService.actualizarAula(formValues.id, formValues).subscribe({
        next: () => {
          Swal.fire({ icon: 'success', title: '¡Actualizado!', text: 'Aula modificada correctamente.', timer: 1500, showConfirmButton: false });
          this.cerrarModal();
          this.cargarAulas();
        },
        error: (err) => Swal.fire('Error', err.error?.mensaje || 'Error al actualizar.', 'error')
      });
    } else {
      this.aulaService.crearAula(formValues).subscribe({
        next: () => {
          Swal.fire({ icon: 'success', title: '¡Registrado!', text: 'Aula creada exitosamente.', timer: 1500, showConfirmButton: false });
          this.cerrarModal();
          this.cargarAulas();
        },
        error: (err) => Swal.fire('Error', err.error?.mensaje || 'Error al guardar.', 'error')
      });
    }
  }

  eliminarAula(aula: Aula): void {
    Swal.fire({
      title: '¿Desactivar Aula?',
      text: `¿Estás seguro de desactivar "${aula.nombreDisplay}"?`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#ef4444',
      cancelButtonColor: '#64748b',
      confirmButtonText: 'Sí, desactivar',
      cancelButtonText: 'Cancelar',
      allowOutsideClick: false,
      allowEscapeKey: false
    }).then((result) => {
      if (result.isConfirmed) {
        this.aulaService.eliminarAula(aula.id).subscribe({
          next: () => {
            Swal.fire('¡Desactivada!', 'El aula ha sido desactivada.', 'success');
            this.cargarAulas();
          },
          error: (err) => Swal.fire('Error', err.error?.mensaje || 'No se pudo desactivar.', 'error')
        });
      }
    });
  }
}
