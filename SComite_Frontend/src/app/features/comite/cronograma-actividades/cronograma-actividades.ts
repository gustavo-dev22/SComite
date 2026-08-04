import { CommonModule } from '@angular/common';
import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActividadService } from '../../../core/services/actividad.service';
import { AulaService } from '../../../core/services/aula.service';
import { PeriodoLectivo } from '../../../core/models/periodoLectivo.model';
import { Aula } from '../../../core/models/aula.model';
import { ActividadComite } from '../../../core/models/actividad.model';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-cronograma-actividades',
  imports: [CommonModule, FormsModule],
  templateUrl: './cronograma-actividades.html',
  styleUrl: './cronograma-actividades.scss',
})
export class CronogramaActividadesComponent implements OnInit {
  private destroyRef = inject(DestroyRef);
  private actividadService = inject(ActividadService);
  private aulaService = inject(AulaService);

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
    fechaProgramada: new Date().toISOString().split('T')[0],
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
      error: (err) => Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar los periodos lectivos.', 'error')
    });
  }

  onPeriodoChange(event: Event): void {
    const id = Number((event.target as HTMLSelectElement).value) || null;
    this.periodoSeleccionadoId.set(id);
    this.aulaSeleccionadaId.set(null);
    this.aulas.set([]);
    this.actividades.set([]);

    if (id && id > 0) this.cargarAulasPorPeriodo(id);
  }

  cargarAulasPorPeriodo(periodoId: number): void {
    this.cargandoAulas.set(true);
    this.aulaService.getAulas(periodoId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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

    this.actividadService.getActividadesPorAula(aulaId, anio).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.actividades.set(data);
        this.cargandoActividades.set(false);
      },
      error: (err) => {
        this.cargandoActividades.set(false);
        Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar las actividades.', 'error');
      }
    });
  }

  abrirModalNuevo(): void {
    this.formActividad.set({
      id: 0,
      aulaId: this.aulaSeleccionadaId()!,
      nombreActividad: '',
      descripcion: '',
      fechaProgramada: new Date().toISOString().split('T')[0],
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
      fechaProgramada: new Date(act.fechaProgramada).toISOString().split('T')[0],
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
        Swal.fire('Error', err.error?.mensaje || 'No se pudo guardar la actividad.', 'error');
      }
    });
  }

  eliminarActividad(id: number): void {
    if (!confirm('¿Está seguro de eliminar esta actividad del cronograma?')) return;

    this.actividadService.eliminarActividad(id, this.aulaSeleccionadaId()!).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.cargarActividades(this.aulaSeleccionadaId()!),
      error: (err) => Swal.fire('Error', err.error?.mensaje || 'No se pudo eliminar la actividad.', 'error')
    });
  }

  getBadgeColor(estado: string): string {
    switch (estado) {
      case 'PLANIFICADA': return 'bg-amber-100 text-amber-800 border-amber-200';
      case 'EN_PROCESO': return 'bg-blue-100 text-blue-800 border-blue-200';
      case 'FINALIZADA': return 'bg-emerald-100 text-emerald-800 border-emerald-200';
      case 'CANCELADA': return 'bg-rose-100 text-rose-800 border-rose-200';
      default: return 'bg-slate-100 text-slate-800 border-slate-200';
    }
  }
}
