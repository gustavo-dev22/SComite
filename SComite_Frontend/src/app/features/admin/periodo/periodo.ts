import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AulaService } from '../../../core/services/aula.service';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { PeriodoLectivo } from '../../../core/models/periodoLectivo.model';
import { PeriodoService } from '../../../core/services/periodo.service';
import Swal from 'sweetalert2';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-periodo',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './periodo.html',
  styleUrl: './periodo.scss',
})
export class PeriodoComponent implements OnInit {
  private destroyRef = inject(DestroyRef);
  private aulaService = inject(AulaService);
  private periodoService = inject(PeriodoService);
  private fb = inject(FormBuilder);

  periodos = signal<PeriodoLectivo[]>([]);
  cargando = signal<boolean>(false);
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
    this.cargarPeriodos();
  }

  cargarPeriodos(): void {
    this.cargando.set(true);
    this.aulaService.getPeriodos().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.periodos.set(data);
        this.cargando.set(false);
      },
      error: (err) => {
        this.cargando.set(false);
        Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar los periodos lectivos.', 'error');
      }
    });
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

    const val = this.periodoForm.value;
    const payload = {
      id: val.id,
      anio: Number(val.anio),
      fechaInicio: new Date(val.fechaInicio).toISOString(),
      fechaFin: new Date(val.fechaFin).toISOString(),
      esActivo: Boolean(val.esActivo)
    };

    if (this.esEdicion()) {
      this.periodoService.actualizarPeriodo(payload.id, payload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => {
          Swal.fire({ icon: 'success', title: '¡Actualizado!', text: 'Periodo lectivo modificado.', timer: 1500, showConfirmButton: false });
          this.cerrarModal();
          this.cargarPeriodos();
        },
        error: (err) => Swal.fire('Error', err.error?.mensaje || 'No se pudo actualizar.', 'error')
      });
    } else {
      this.periodoService.crearPeriodo(payload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => {
          Swal.fire({ icon: 'success', title: '¡Creado!', text: 'Periodo lectivo registrado.', timer: 1500, showConfirmButton: false });
          this.cerrarModal();
          this.cargarPeriodos();
        },
        error: (err) => Swal.fire('Error', err.error?.mensaje || 'No se pudo crear.', 'error')
      });
    }
  }
}
