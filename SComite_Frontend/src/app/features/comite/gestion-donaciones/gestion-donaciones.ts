import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, takeUntil } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { DonacionService } from '../../../core/services/donacion.service';
import { Aula } from '../../../core/models/aula.model';
import { DonacionComite } from '../../../core/models/donacion.model';
import { manejarErrorHttp } from '../../../core/utils/http-error.util';
import { formatearFechaLocal, hoyLocal } from '../../../core/utils/fecha.util';
import { BasePeriodosComponent } from '../../../core/base/base-periodos.component';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-gestion-donaciones',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule],
  templateUrl: './gestion-donaciones.html',
  styleUrl: './gestion-donaciones.scss',
})
export class GestionDonacionesComponent extends BasePeriodosComponent implements OnInit {
  private reiniciarCarga$ = new Subject<void>();
  private donacionService = inject(DonacionService);

  constructor() {
    super();
    this.destroyRef.onDestroy(() => this.reiniciarCarga$.complete());
  }

  aulas = signal<Aula[]>([]);
  donaciones = signal<DonacionComite[]>([]);

  periodoSeleccionadoId = signal<number | null>(null);
  aulaSeleccionadaId = signal<number | null>(null);
  mesSeleccionado = signal<number>(0);

  cargandoAulas = signal<boolean>(false);
  cargando = signal<boolean>(false);
  guardando = signal<boolean>(false);
  mostrarModal = signal<boolean>(false);

  formDonacion = signal<Partial<DonacionComite>>({
    id: 0,
    aulaId: 0,
    donante: '',
    monto: 0,
    fechaDonacion: hoyLocal(),
    concepto: '',
    observacion: ''
  });

  tienePeriodoSeleccionado = computed(() => this.periodoSeleccionadoId() !== null && this.periodoSeleccionadoId()! > 0);
  tieneAulaSeleccionada = computed(() => this.aulaSeleccionadaId() !== null && this.aulaSeleccionadaId()! > 0);

  totalDonaciones = computed(() => {
    return this.donaciones().reduce((sum, item) => sum + item.monto, 0);
  });

  ngOnInit(): void {
    this.cargarPeriodos();
  }

  onPeriodoChange(event: Event): void {
    this.reiniciarCarga$.next();
    const id = Number((event.target as HTMLSelectElement).value) || null;
    this.periodoSeleccionadoId.set(id);
    this.aulaSeleccionadaId.set(null);
    this.aulas.set([]);
    this.donaciones.set([]);

    if (id && id > 0) this.cargarAulasPorPeriodo(id);
  }

  cargarAulasPorPeriodo(periodoId: number): void {
    this.cargandoAulas.set(true);
    this.aulaService.getMisAulas(periodoId).pipe(takeUntil(this.reiniciarCarga$), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.aulas.set(data);
        this.cargandoAulas.set(false);
      },
      error: (err) => {
        this.cargandoAulas.set(false);
        manejarErrorHttp(err, 'No se pudieron cargar las aulas.');
      }
    });
  }

  onAulaChange(event: Event): void {
    this.reiniciarCarga$.next();
    const aulaId = Number((event.target as HTMLSelectElement).value) || null;
    this.aulaSeleccionadaId.set(aulaId);

    if (aulaId && aulaId > 0) {
      this.cargarDonaciones(aulaId);
    } else {
      this.donaciones.set([]);
    }
  }

  onMesChange(event: Event): void {
    this.reiniciarCarga$.next();
    const mes = Number((event.target as HTMLSelectElement).value);
    this.mesSeleccionado.set(mes);

    const aulaId = this.aulaSeleccionadaId();
    if (aulaId) {
      this.cargarDonaciones(aulaId);
    }
  }

  cargarDonaciones(aulaId: number): void {
    this.cargando.set(true);
    const periodoObj = this.periodos().find(p => p.id === this.periodoSeleccionadoId());
    const anio = periodoObj ? periodoObj.anio : new Date().getFullYear();
    const mes = this.mesSeleccionado();

    this.donacionService.getDonacionesPorAula(aulaId, anio, mes).pipe(takeUntil(this.reiniciarCarga$), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.donaciones.set(data);
        this.cargando.set(false);
      },
      error: (err) => {
        this.cargando.set(false);
        manejarErrorHttp(err, 'No se pudieron cargar las donaciones.');
      }
    });
  }

  abrirModalNuevo(): void {
    this.formDonacion.set({
      id: 0,
      aulaId: this.aulaSeleccionadaId()!,
      donante: '',
      monto: 0,
      fechaDonacion: hoyLocal(),
      concepto: '',
      observacion: ''
    });
    this.mostrarModal.set(true);
  }

  abrirModalEditar(d: DonacionComite): void {
    this.formDonacion.set({
      id: d.id,
      aulaId: d.aulaId,
      donante: d.donante,
      monto: d.monto,
      fechaDonacion: formatearFechaLocal(d.fechaDonacion),
      concepto: d.concepto,
      observacion: d.observacion || ''
    });
    this.mostrarModal.set(true);
  }

  cerrarModal(): void {
    this.mostrarModal.set(false);
  }

  onInputToUppercase(campo: 'donante' | 'concepto'): void {
    const val = this.formDonacion()[campo] || '';
    this.formDonacion.update(f => ({ ...f, [campo]: val.toUpperCase() }));
  }

  actualizarCampo(campo: string, valor: unknown): void {
    this.formDonacion.update(f => ({ ...f, [campo]: valor }) as Partial<DonacionComite>);
  }

  guardarDonacion(): void {
    const dto = this.formDonacion();
    if (!dto.donante?.trim() || !dto.concepto?.trim() || (dto.monto || 0) <= 0) return;

    dto.aulaId = this.aulaSeleccionadaId()!;
    this.guardando.set(true);

    this.donacionService.guardarDonacion(dto).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.guardando.set(false);
        this.cerrarModal();
        this.cargarDonaciones(this.aulaSeleccionadaId()!);
      },
      error: (err) => {
        this.guardando.set(false);
        manejarErrorHttp(err, 'No se pudo guardar la donación.');
      }
    });
  }

  eliminarDonacion(id: number): void {
    Swal.fire({
      title: '¿Eliminar donación?',
      text: 'Esta acción no se puede deshacer y afectará los saldos del aula.',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#0f172a', // slate-900
      cancelButtonColor: '#94a3b8',  // slate-400
      confirmButtonText: 'Sí, eliminar',
      cancelButtonText: 'Cancelar',
      allowOutsideClick: false,
      allowEscapeKey: false,
      customClass: {
        popup: 'rounded-2xl',
        confirmButton: 'rounded-xl font-bold text-xs px-4 py-2.5',
        cancelButton: 'rounded-xl font-semibold text-xs px-4 py-2.5'
      }
    }).then((result) => {
      if (result.isConfirmed) {
        this.donacionService.eliminarDonacion(id, this.aulaSeleccionadaId()!)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              Swal.fire({
                title: '¡Eliminada!',
                text: 'El registro de donación ha sido eliminado correctamente.',
                icon: 'success',
                timer: 1800,
                showConfirmButton: false
              });
              this.cargarDonaciones(this.aulaSeleccionadaId()!);
            },
            error: (err) => manejarErrorHttp(err, 'No se pudo eliminar la donación.')
          });
      }
    });
  }
}
