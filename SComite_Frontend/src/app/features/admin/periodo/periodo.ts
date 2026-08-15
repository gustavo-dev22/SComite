import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { PeriodoLectivo } from '../../../core/models/periodoLectivo.model';
import { PeriodoService } from '../../../core/services/periodo.service';
import { BasePeriodosComponent } from '../../../core/base/base-periodos.component';
import Swal from 'sweetalert2';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-periodo',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './periodo.html',
  styleUrl: './periodo.scss',
})
export class PeriodoComponent extends BasePeriodosComponent implements OnInit {
  private periodoService = inject(PeriodoService);
  private fb = inject(FormBuilder);

  cargando = signal<boolean>(false);
  guardando = signal<boolean>(false);
  modalAbierto = signal<boolean>(false);
  esEdicion = signal<boolean>(false);

  periodoForm: FormGroup = this.fb.group({
    id: [0],
    anio: [new Date().getFullYear(), [Validators.required, Validators.min(2020), Validators.max(2050)]],
    fechaInicio: ['', [Validators.required]],
    fechaFin: ['', [Validators.required]],
    esActivo: [false]
  });

  ngOnInit(): void {
    this.cargando.set(true);
    this.cargarPeriodos();
  }

  protected override onPeriodosCargados(): void {
    this.cargando.set(false);
  }

  protected override onPeriodosError(): void {
    this.cargando.set(false);
  }

  abrirModalCrear(): void {
    this.esEdicion.set(false);
    const anioSugerido = new Date().getFullYear();
    
    this.periodoForm.reset({
      id: 0,
      anio: anioSugerido,
      fechaInicio: `${anioSugerido}-03-01`,
      fechaFin: `${anioSugerido}-12-20`,
      esActivo: false
    });
    this.modalAbierto.set(true);
  }

  abrirModalEditar(p: PeriodoLectivo): void {
    this.esEdicion.set(true);
    
    // Formatear fechas para los inputs tipo date (yyyy-MM-dd)
    const fechaInicioFmt = p.fechaInicio ? p.fechaInicio.split('T')[0] : '';
    const fechaFinFmt = p.fechaFin ? p.fechaFin.split('T')[0] : '';

    this.periodoForm.patchValue({
      id: p.id,
      anio: p.anio,
      fechaInicio: fechaInicioFmt,
      fechaFin: fechaFinFmt,
      esActivo: p.esActivo
    });
    this.modalAbierto.set(true);
  }

  cerrarModal(): void {
    this.modalAbierto.set(false);
  }

  guardar(): void {
    if (this.periodoForm.invalid) {
      this.periodoForm.markAllAsTouched();
      return;
    }
    if (this.guardando()) return;
    this.guardando.set(true);

    const val = this.periodoForm.value;
    const payload = {
      id: val.id,
      anio: Number(val.anio),
      fechaInicio: new Date(val.fechaInicio).toISOString(),
      fechaFin: new Date(val.fechaFin).toISOString(),
      esActivo: Boolean(val.esActivo)
    };

    const request = this.esEdicion()
      ? this.periodoService.actualizarPeriodo(payload.id, payload)
      : this.periodoService.crearPeriodo(payload);

    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.guardando.set(false);
        Swal.fire({
          icon: 'success',
          title: this.esEdicion() ? '¡Actualizado!' : '¡Creado!',
          text: this.esEdicion() ? 'Periodo lectivo modificado.' : 'Periodo lectivo registrado.',
          timer: 1500,
          showConfirmButton: false
        });
        this.cerrarModal();
        this.cargarPeriodos();
      },
      error: (err) => {
        this.guardando.set(false);
        Swal.fire('Error', err.error?.mensaje || (this.esEdicion() ? 'No se pudo actualizar.' : 'No se pudo crear.'), 'error');
      }
    });
  }
}
