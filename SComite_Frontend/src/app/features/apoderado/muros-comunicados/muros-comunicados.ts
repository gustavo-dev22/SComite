import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ApoderadoService } from '../../../core/services/apoderado.service';
import { AnuncioApoderado, HijoApoderado } from '../../../core/models/apoderado.model';

@Component({
  selector: 'app-muros-comunicados',
  imports: [CommonModule],
  templateUrl: './muros-comunicados.html',
  styleUrl: './muros-comunicados.scss',
})
export class MurosComunicadosComponent implements OnInit {
  private apoderadoService = inject(ApoderadoService);

  cargandoHijos = signal<boolean>(false);
  cargandoAnuncios = signal<boolean>(false);

  hijos = signal<HijoApoderado[]>([]);
  estudianteSeleccionadoId = signal<number | null>(null);

  anuncios = signal<AnuncioApoderado[]>([]);

  anioLectivoActual = new Date().getFullYear();

  hijoActual = computed(() => {
    const id = this.estudianteSeleccionadoId();
    return this.hijos().find(h => h.estudianteId === id) || null;
  });

  ngOnInit(): void {
    this.cargarHijos();
  }

  cargarHijos(): void {
    this.cargandoHijos.set(true);
    this.apoderadoService.getMisHijos(this.anioLectivoActual).subscribe({
      next: (data) => {
        this.hijos.set(data);
        this.cargandoHijos.set(false);

        if (data.length > 0) {
          this.estudianteSeleccionadoId.set(data[0].estudianteId);
          this.cargarAnuncios();
        }
      },
      error: () => this.cargandoHijos.set(false)
    });
  }

  cargarAnuncios(): void {
    const estudianteId = this.estudianteSeleccionadoId();
    if (!estudianteId) return;

    this.cargandoAnuncios.set(true);
    this.apoderadoService.getAnunciosMuro(estudianteId, this.anioLectivoActual).subscribe({
      next: (data) => {
        this.anuncios.set(data);
        this.cargandoAnuncios.set(false);

        // 🚀 Marcar como leídos automáticamente los comunicados visibles
        data.forEach(anuncio => {
          if (!anuncio.leido) {
            this.marcarComoLeido(anuncio);
          }
        });
      },
      error: () => this.cargandoAnuncios.set(false)
    });
  }

  onSeleccionarHijo(estudianteId: number): void {
    this.estudianteSeleccionadoId.set(estudianteId);
    this.cargarAnuncios();
  }

  // 🚀 Al hacer hover/interactuar con la tarjeta, si no ha sido leído, marca la vista en la BD
  marcarComoLeido(anuncio: AnuncioApoderado): void {
    if (anuncio.leido) return;

    const estudianteId = this.estudianteSeleccionadoId();
    if (!estudianteId) return;

    this.apoderadoService.marcarLecturaAnuncio(anuncio.id, estudianteId).subscribe({
      next: () => {
        anuncio.leido = true;
        anuncio.cantidadVistas = (anuncio.cantidadVistas || 0) + 1;
      },
      error: (err) => console.error('Error al registrar lectura:', err)
    });
  }

  getCategoriaBadgeClass(categoria: string): string {
    switch (categoria) {
      case 'URGENTE': return 'bg-rose-100 text-rose-700 border-rose-200';
      case 'CITACION': return 'bg-amber-100 text-amber-800 border-amber-200';
      case 'TESORERIA': return 'bg-emerald-100 text-emerald-800 border-emerald-200';
      case 'EVENTO': return 'bg-indigo-100 text-indigo-800 border-indigo-200';
      default: return 'bg-slate-100 text-slate-700 border-slate-200';
    }
  }
}
