import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, takeUntil } from 'rxjs';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import Swal from 'sweetalert2';
import { Aula } from '../../../core/models/aula.model';
import { BasePeriodosComponent } from '../../../core/base/base-periodos.component';
import { CommonModule } from '@angular/common';
import { ModalA11yDirective } from '../../../shared/directives/modal-a11y.directive';

@Component({
  selector: 'app-aula',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ReactiveFormsModule, ModalA11yDirective],
  templateUrl: './aula.html',
  styleUrl: './aula.scss',
})
export class AulaComponent extends BasePeriodosComponent implements OnInit {
  private reiniciarCarga$ = new Subject<void>();
  private fb = inject(FormBuilder);

  constructor() {
    super();
    this.destroyRef.onDestroy(() => this.reiniciarCarga$.complete());
  }

  aulas = signal<Aula[]>([]);
  cargando = signal<boolean>(false);
  guardando = signal<boolean>(false);
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

  cargarAulas(): void {
    this.reiniciarCarga$.next();
    this.cargando.set(true);
    this.aulaService.getAulas(this.periodoFiltroSelected() || undefined).pipe(takeUntil(this.reiniciarCarga$), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.aulas.set(data);
        this.cargando.set(false);
      },
      error: (err) => {
        this.cargando.set(false);
        Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar las aulas.', 'error');
      }
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
    if (this.guardando()) return;
    this.guardando.set(true);

    const formValues = this.aulaForm.value;
    const request = this.esEdicion()
      ? this.aulaService.actualizarAula(formValues.id, formValues)
      : this.aulaService.crearAula(formValues);

    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.guardando.set(false);
        Swal.fire({
          icon: 'success',
          title: this.esEdicion() ? '¡Actualizado!' : '¡Registrado!',
          text: this.esEdicion() ? 'Aula modificada correctamente.' : 'Aula creada exitosamente.',
          timer: 1500,
          showConfirmButton: false
        });
        this.cerrarModal();
        this.cargarAulas();
      },
      error: (err) => {
        this.guardando.set(false);
        Swal.fire('Error', err.error?.mensaje || 'Error al guardar.', 'error');
      }
    });
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
        this.aulaService.eliminarAula(aula.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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
