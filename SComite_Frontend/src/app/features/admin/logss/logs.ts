import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { LogService } from '../../../core/services/log.service';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { LogSistema, PagedResult } from '../../../core/models/log.model';
import { CommonModule } from '@angular/common';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-logs',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './logs.html',
  styleUrl: './logs.scss',
})
export class LogsComponent implements OnInit {
  private destroyRef = inject(DestroyRef);
  private logService = inject(LogService);
  private fb = inject(FormBuilder);

  logs = signal<LogSistema[]>([]);
  totalRegistros = signal<number>(0);
  paginaActual = signal<number>(1);
  totalPaginas = signal<number>(1);
  cargando = signal<boolean>(false);

  logSeleccionado = signal<LogSistema | null>(null);
  modalDetalleAbierto = signal<boolean>(false);

  modulosDisponibles = ['AUTH', 'AULAS', 'ESTUDIANTES', 'COMITE', 'PERIODOS', 'TESORERIA'];
  nivelesDisponibles = ['INFO', 'WARN', 'ERROR', 'CRITICAL'];

  filtrosForm: FormGroup = this.fb.group({
    fechaInicio: [''],
    fechaFin: [''],
    nivel: [''],
    modulo: [''],
    busqueda: ['']
  });

  ngOnInit(): void {
    const hoyLocal = this.obtenerFechaLocalFormateada(new Date());
    
    const hace7dias = new Date();
    hace7dias.setDate(hace7dias.getDate() - 7);
    const hace7DiasLocal = this.obtenerFechaLocalFormateada(hace7dias);

    this.filtrosForm.patchValue({
      fechaInicio: hace7DiasLocal, // Ej: 2026-07-21
      fechaFin: hoyLocal          // Ej: 2026-07-28
    });

    this.cargarLogs();
  }

  cargarLogs(pagina: number = 1): void {
    this.cargando.set(true);
    this.paginaActual.set(pagina);

    const formValues = this.filtrosForm.value;
    const filtros = {
      ...formValues,
      pagina: this.paginaActual(),
      tamanoPagina: 15
    };

    this.logService.getLogs(filtros).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res: PagedResult<LogSistema>) => {
        this.logs.set(res.items);
        this.totalRegistros.set(res.totalRegistros);
        this.totalPaginas.set(res.totalPaginas);
        this.cargando.set(false);
      },
      error: (err) => {
        this.cargando.set(false);
        Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar los registros.', 'error');
      }
    });
  }

  limpiarFiltros(): void {
    const hoyLocal = this.obtenerFechaLocalFormateada(new Date());
    
    const hace7dias = new Date();
    hace7dias.setDate(hace7dias.getDate() - 7);
    const hace7DiasLocal = this.obtenerFechaLocalFormateada(hace7dias);

    this.filtrosForm.reset({
      fechaInicio: hace7DiasLocal,
      fechaFin: hoyLocal,
      nivel: '',
      modulo: '',
      busqueda: ''
    });

    this.cargarLogs(1);
  }

  private obtenerFechaLocalFormateada(fecha: Date): string {
    const anio = fecha.getFullYear();
    const mes = String(fecha.getMonth() + 1).padStart(2, '0');
    const dia = String(fecha.getDate()).padStart(2, '0');
    return `${anio}-${mes}-${dia}`;
  }

  verDetalle(log: LogSistema): void {
    this.logSeleccionado.set(log);
    this.modalDetalleAbierto.set(true);
  }

  cerrarModalDetalle(): void {
    this.modalDetalleAbierto.set(false);
    this.logSeleccionado.set(null);
  }

  getNivelBadgeClass(nivel: string): string {
    switch (nivel.toUpperCase()) {
      case 'INFO': return 'bg-blue-100 text-blue-800 border-blue-200';
      case 'WARN': return 'bg-amber-100 text-amber-800 border-amber-200';
      case 'ERROR': return 'bg-rose-100 text-rose-800 border-rose-200';
      case 'CRITICAL': return 'bg-purple-100 text-purple-900 border-purple-300 font-bold';
      default: return 'bg-slate-100 text-slate-700 border-slate-200';
    }
  }
}
