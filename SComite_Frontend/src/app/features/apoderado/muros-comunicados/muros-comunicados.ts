import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApoderadoService } from '../../../core/services/apoderado.service';
import { AulaService } from '../../../core/services/aula.service';
import { AnuncioApoderado, HijoApoderado } from '../../../core/models/apoderado.model';
import Swal from 'sweetalert2';

const BADGES_CATEGORIA_ANUNCIO: Record<string, string> = {
  'URGENTE': 'bg-rose-100 text-rose-700 border-rose-200',
  'CITACION': 'bg-amber-100 text-amber-800 border-amber-200',
  'TESORERIA': 'bg-emerald-100 text-emerald-800 border-emerald-200',
  'EVENTO': 'bg-indigo-100 text-indigo-800 border-indigo-200'
};

@Component({
  selector: 'app-muros-comunicados',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  templateUrl: './muros-comunicados.html',
  styleUrl: './muros-comunicados.scss',
})
export class MurosComunicadosComponent implements OnInit {
  private destroyRef = inject(DestroyRef);
  private apoderadoService = inject(ApoderadoService);
  private aulaService = inject(AulaService);

  cargandoHijos = signal<boolean>(false);
  cargandoAnuncios = signal<boolean>(false);

  hijos = signal<HijoApoderado[]>([]);
  estudianteSeleccionadoId = signal<number | null>(null);

  anuncios = signal<AnuncioApoderado[]>([]);

  private lecturasEnCurso = new Set<number>();

  anioLectivoActual = signal<number>(new Date().getFullYear());

  hijoActual = computed(() => {
    const id = this.estudianteSeleccionadoId();
    return this.hijos().find(h => h.estudianteId === id) || null;
  });

  ngOnInit(): void {
    this.aulaService.getAnioLectivoVigente().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (anio) => {
        this.anioLectivoActual.set(anio);
        this.cargarHijos();
      },
      error: () => this.cargarHijos()
    });
  }

  cargarHijos(): void {
    this.cargandoHijos.set(true);
    this.apoderadoService.getMisHijos(this.anioLectivoActual()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.hijos.set(data);
        this.cargandoHijos.set(false);

        if (data.length > 0) {
          this.estudianteSeleccionadoId.set(data[0].estudianteId);
          this.cargarAnuncios();
        }
      },
      error: (err) => {
        this.cargandoHijos.set(false);
        Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar tus hijos.', 'error');
      }
    });
  }

  cargarAnuncios(): void {
    const estudianteId = this.estudianteSeleccionadoId();
    if (!estudianteId) return;

    this.cargandoAnuncios.set(true);
    this.apoderadoService.getAnunciosMuro(estudianteId, this.anioLectivoActual()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.anuncios.set(data);
        this.cargandoAnuncios.set(false);
      },
      error: (err) => {
        this.cargandoAnuncios.set(false);
        Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar los comunicados.', 'error');
      }
    });
  }

  onSeleccionarHijo(estudianteId: number): void {
    this.estudianteSeleccionadoId.set(estudianteId);
    this.cargarAnuncios();
  }

  // 🚀 Al interactuar con la tarjeta, si no ha sido leído, marca la vista en la BD
  marcarComoLeido(anuncio: AnuncioApoderado): void {
    if (anuncio.leido || this.lecturasEnCurso.has(anuncio.id)) return;

    const estudianteId = this.estudianteSeleccionadoId();
    if (!estudianteId) return;

    this.lecturasEnCurso.add(anuncio.id);

    this.apoderadoService.marcarLecturaAnuncio(anuncio.id, estudianteId, this.anioLectivoActual()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.lecturasEnCurso.delete(anuncio.id);
        this.anuncios.update(lista =>
          lista.map(item =>
            item.id === anuncio.id
              ? { ...item, leido: true, cantidadVistas: (item.cantidadVistas || 0) + 1 }
              : item
          )
        );
      },
      error: (err) => {
        this.lecturasEnCurso.delete(anuncio.id);
        Swal.fire('Error', err.error?.mensaje || 'No se pudo registrar la lectura del comunicado.', 'error');
      }
    });
  }

  getCategoriaBadgeClass(categoria: string): string {
    return BADGES_CATEGORIA_ANUNCIO[categoria] || 'bg-slate-100 text-slate-700 border-slate-200';
  }
}
