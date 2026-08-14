import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { AulaService } from '../../../core/services/aula.service';
import { DonacionService } from '../../../core/services/donacion.service';
import { PeriodoLectivo } from '../../../core/models/periodoLectivo.model';
import { Aula } from '../../../core/models/aula.model';
import { DonacionComite } from '../../../core/models/donacion.model';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-gestion-donaciones',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule],
  templateUrl: './gestion-donaciones.html',
  styleUrl: './gestion-donaciones.scss',
})
export class GestionDonacionesComponent implements OnInit {
  private destroyRef = inject(DestroyRef);
  private donacionService = inject(DonacionService);
  private aulaService = inject(AulaService);

  periodos = signal<PeriodoLectivo[]>([]);
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
    fechaDonacion: new Date().toISOString().split('T')[0],
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

  cargarPeriodos(): void {
    this.aulaService.getPeriodos().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => this.periodos.set(data),
      error: (err) => Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar los periodos lectivos.', 'error')
    });
  }

  onPeriodoChange(event: Event): void {
    const id = Number((event.target as HTMLSelectElement).value) || null;
    this.periodoSeleccionadoId.set(id);
    this.aulaSeleccionadaId.set(null);
    this.aulas.set([]);
    this.donaciones.set([]);

    if (id && id > 0) this.cargarAulasPorPeriodo(id);
  }

  cargarAulasPorPeriodo(periodoId: number): void {
    this.cargandoAulas.set(true);
    this.aulaService.getMisAulas(periodoId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.aulas.set(data);
        this.cargandoAulas.set(false);
      },
      error: (err) => {
        this.cargandoAulas.set(false);
        Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar las aulas.', 'error');
      }
    });
  }

  onAulaChange(event: Event): void {
    const aulaId = Number((event.target as HTMLSelectElement).value) || null;
    this.aulaSeleccionadaId.set(aulaId);

    if (aulaId && aulaId > 0) {
      this.cargarDonaciones(aulaId);
    } else {
      this.donaciones.set([]);
    }
  }

  onMesChange(event: Event): void {
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

    this.donacionService.getDonacionesPorAula(aulaId, anio, mes).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.donaciones.set(data);
        this.cargando.set(false);
      },
      error: (err) => {
        this.cargando.set(false);
        Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar las donaciones.', 'error');
      }
    });
  }

  abrirModalNuevo(): void {
    this.formDonacion.set({
      id: 0,
      aulaId: this.aulaSeleccionadaId()!,
      donante: '',
      monto: 0,
      fechaDonacion: new Date().toISOString().split('T')[0],
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
      fechaDonacion: new Date(d.fechaDonacion).toISOString().split('T')[0],
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
        Swal.fire('Error', err.error?.mensaje || 'No se pudo guardar la donación.', 'error');
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
            error: (err) => Swal.fire('Error', err.error?.mensaje || 'No se pudo eliminar la donación.', 'error')
          });
      }
    });
  }
}
