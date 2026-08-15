import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, takeUntil } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { ActividadService } from '../../../core/services/actividad.service';
import { AulaService } from '../../../core/services/aula.service';
import { PeriodoLectivo } from '../../../core/models/periodoLectivo.model';
import { Aula } from '../../../core/models/aula.model';
import { ActividadComite } from '../../../core/models/actividad.model';
import { manejarErrorHttp } from '../../../core/utils/http-error.util';
import { formatearFechaLocal, hoyLocal } from '../../../core/utils/fecha.util';
import Swal from 'sweetalert2';

const BADGES_ESTADO_ACTIVIDAD: Record<string, string> = {
  'PLANIFICADA': 'bg-amber-100 text-amber-800 border-amber-200',
  'EN_PROCESO': 'bg-blue-100 text-blue-800 border-blue-200',
  'EN PROCESO': 'bg-blue-100 text-blue-800 border-blue-200',
  'FINALIZADA': 'bg-emerald-100 text-emerald-800 border-emerald-200',
  'CANCELADA': 'bg-rose-100 text-rose-800 border-rose-200'
};

@Component({
  selector: 'app-cronograma-actividades',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule],
  templateUrl: './cronograma-actividades.html',
  styleUrl: './cronograma-actividades.scss',
})
export class CronogramaActividadesComponent implements OnInit {
  private destroyRef = inject(DestroyRef);
  private reiniciarCarga$ = new Subject<void>();
  private actividadService = inject(ActividadService);
  private aulaService = inject(AulaService);

  constructor() {
    this.destroyRef.onDestroy(() => this.reiniciarCarga$.complete());
  }

  // Signals para listas
  periodos = signal<PeriodoLectivo[]>([]);
  aulas = signal<Aula[]>([]);
  actividades = signal<ActividadComite[]>([]);

  // Filtros
  periodoSeleccionadoId = signal<number | null>(null);
  aulaSeleccionadaId = signal<number | null>(null);

  // Estados UI
  cargandoAulas = signal<boolean>(false);
  cargandoActividades = signal<boolean>(false);
  guardando = signal<boolean>(false);
  mostrarModal = signal<boolean>(false);

  // Formulario
  formActividad = signal<Partial<ActividadComite>>({
    id: 0,
    aulaId: 0,
    nombreActividad: '',
    descripcion: '',
    fechaProgramada: hoyLocal(),
    montoPresupuestado: 0,
    cuotaSugeridaPorAlumno: 0,
    estado: 'PLANIFICADA'
  });

  // Signals computados
  tienePeriodoSeleccionado = computed(() => this.periodoSeleccionadoId() !== null && this.periodoSeleccionadoId()! > 0);
  tieneAulaSeleccionada = computed(() => this.aulaSeleccionadaId() !== null && this.aulaSeleccionadaId()! > 0);

  totalPresupuestado = computed(() => {
    return this.actividades().reduce((sum, item) => sum + item.montoPresupuestado, 0);
  });

  actividadesFinalizadasCount = computed(() => {
    return this.actividades().filter(a => a.estado === 'FINALIZADA').length;
  });

  ngOnInit(): void {
    this.cargarPeriodos();
  }

  cargarPeriodos(): void {
    this.aulaService.getPeriodos().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => this.periodos.set(data),
      error: (err) => manejarErrorHttp(err, 'No se pudieron cargar los periodos lectivos.')
    });
  }

  onPeriodoChange(event: Event): void {
    this.reiniciarCarga$.next();
    const id = Number((event.target as HTMLSelectElement).value) || null;
    this.periodoSeleccionadoId.set(id);
    this.aulaSeleccionadaId.set(null);
    this.aulas.set([]);
    this.actividades.set([]);

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
      this.cargarActividades(aulaId);
    } else {
      this.actividades.set([]);
    }
  }

  cargarActividades(aulaId: number): void {
    this.cargandoActividades.set(true);
    const periodoId = this.periodoSeleccionadoId();
    const p = this.periodos().find(x => x.id === periodoId);
    const anio = p ? p.anio : new Date().getFullYear();

    this.actividadService.getActividadesPorAula(aulaId, anio).pipe(takeUntil(this.reiniciarCarga$), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.actividades.set(data);
        this.cargandoActividades.set(false);
      },
      error: (err) => {
        this.cargandoActividades.set(false);
        manejarErrorHttp(err, 'No se pudieron cargar las actividades.');
      }
    });
  }

  abrirModalNuevo(): void {
    this.formActividad.set({
      id: 0,
      aulaId: this.aulaSeleccionadaId()!,
      nombreActividad: '',
      descripcion: '',
      fechaProgramada: hoyLocal(),
      montoPresupuestado: 0,
      cuotaSugeridaPorAlumno: 0,
      estado: 'PLANIFICADA'
    });
    this.mostrarModal.set(true);
  }

  abrirModalEditar(act: ActividadComite): void {
    this.formActividad.set({
      id: act.id,
      aulaId: act.aulaId,
      nombreActividad: act.nombreActividad,
      descripcion: act.descripcion || '',
      fechaProgramada: formatearFechaLocal(act.fechaProgramada),
      montoPresupuestado: act.montoPresupuestado,
      cuotaSugeridaPorAlumno: act.cuotaSugeridaPorAlumno,
      estado: act.estado
    });
    this.mostrarModal.set(true);
  }

  cerrarModal(): void {
    this.mostrarModal.set(false);
  }

  guardarActividad(): void {
    const dto = this.formActividad();
    if (!dto.nombreActividad?.trim()) return;

    dto.aulaId = this.aulaSeleccionadaId()!;
    this.guardando.set(true);

    this.actividadService.guardarActividad(dto).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.guardando.set(false);
        this.cerrarModal();
        this.cargarActividades(this.aulaSeleccionadaId()!);
      },
      error: (err) => {
        this.guardando.set(false);
        manejarErrorHttp(err, 'No se pudo guardar la actividad.');
      }
    });
  }

  eliminarActividad(id: number): void {
    Swal.fire({
      title: '¿Eliminar actividad?',
      text: 'Esta acción eliminará el evento del cronograma de actividades del aula.',
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
        this.actividadService.eliminarActividad(id, this.aulaSeleccionadaId()!)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              Swal.fire({
                title: '¡Eliminada!',
                text: 'La actividad ha sido eliminada del cronograma.',
                icon: 'success',
                timer: 1800,
                showConfirmButton: false
              });
              this.cargarActividades(this.aulaSeleccionadaId()!);
            },
            error: (err) => manejarErrorHttp(err, 'No se pudo eliminar la actividad.')
          });
      }
    });
  }

  actualizarCampo(campo: string, valor: unknown): void {
    this.formActividad.update(f => ({ ...f, [campo]: valor }) as Partial<ActividadComite>);
  }

  getBadgeColor(estado: string): string {
    return BADGES_ESTADO_ACTIVIDAD[estado] || 'bg-slate-100 text-slate-800 border-slate-200';
  }
}
