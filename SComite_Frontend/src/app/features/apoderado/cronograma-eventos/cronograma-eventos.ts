import { CommonModule } from '@angular/common';
import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApoderadoService } from '../../../core/services/apoderado.service';
import { EventoCronogramaApoderado, HijoApoderado } from '../../../core/models/apoderado.model';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-cronograma-eventos',
  imports: [CommonModule],
  templateUrl: './cronograma-eventos.html',
  styleUrl: './cronograma-eventos.scss',
})
export class CronogramaEventosComponent implements OnInit {
  private destroyRef = inject(DestroyRef);
  private apoderadoService = inject(ApoderadoService);

  cargandoHijos = signal<boolean>(false);
  cargandoEventos = signal<boolean>(false);

  hijos = signal<HijoApoderado[]>([]);
  estudianteSeleccionadoId = signal<number | null>(null);

  eventos = signal<EventoCronogramaApoderado[]>([]);

  anioLectivoActual = new Date().getFullYear();

  hijoActual = computed(() => {
    const id = this.estudianteSeleccionadoId();
    return this.hijos().find(h => h.estudianteId === id) || null;
  });

  // Métricas computadas
  totalEventos = computed(() => this.eventos().length);
  eventosConcluidos = computed(() => this.eventos().filter(e => e.estado === 'FINALIZADA').length);
  proximoEvento = computed(() => {
    const hoy = new Date().toISOString().substring(0, 10);
    return this.eventos().find(e => e.fechaProgramada >= hoy && e.estado !== 'CANCELADA') || null;
  });

  ngOnInit(): void {
    this.cargarHijos();
  }

  cargarHijos(): void {
    this.cargandoHijos.set(true);
    this.apoderadoService.getMisHijos(this.anioLectivoActual).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.hijos.set(data);
        this.cargandoHijos.set(false);

        if (data.length > 0) {
          this.estudianteSeleccionadoId.set(data[0].estudianteId);
          this.cargarEventos();
        }
      },
      error: (err) => {
        this.cargandoHijos.set(false);
        Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar tus hijos.', 'error');
      }
    });
  }

  cargarEventos(): void {
    const estudianteId = this.estudianteSeleccionadoId();
    if (!estudianteId) return;

    this.cargandoEventos.set(true);
    this.apoderadoService.getCronogramaEventos(estudianteId, this.anioLectivoActual).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.eventos.set(data);
        this.cargandoEventos.set(false);
      },
      error: (err) => {
        this.cargandoEventos.set(false);
        Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar los eventos.', 'error');
      }
    });
  }

  onSeleccionarHijo(estudianteId: number): void {
    this.estudianteSeleccionadoId.set(estudianteId);
    this.cargarEventos();
  }

  getBadgeColor(estado: string): string {
    switch (estado?.toUpperCase()) {
      case 'FINALIZADA': return 'bg-emerald-100 text-emerald-800 border-emerald-200';
      case 'EN_PROCESO':
      case 'EN PROCESO': return 'bg-indigo-100 text-indigo-800 border-indigo-200';
      case 'CANCELADA': return 'bg-rose-100 text-rose-800 border-rose-200';
      default: return 'bg-amber-100 text-amber-800 border-amber-200'; // PLANIFICADA
    }
  }
}
