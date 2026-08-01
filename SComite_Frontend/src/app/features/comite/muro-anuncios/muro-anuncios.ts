import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AnuncioService } from '../../../core/services/anuncio.service';
import { AulaService } from '../../../core/services/aula.service';
import { PeriodoLectivo } from '../../../core/models/periodoLectivo.model';
import { AnuncioComite } from '../../../core/models/anuncio.model';
import { Aula } from '../../../core/models/aula.model';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-muro-anuncios',
  imports: [CommonModule, FormsModule],
  templateUrl: './muro-anuncios.html',
  styleUrl: './muro-anuncios.scss',
})
export class MuroAnunciosComponent implements OnInit {
  private anuncioService = inject(AnuncioService);
  private aulaService = inject(AulaService);
  private authService = inject(AuthService);

  periodos = signal<PeriodoLectivo[]>([]);
  aulas = signal<Aula[]>([]);
  anuncios = signal<AnuncioComite[]>([]);

  periodoSeleccionadoId = signal<number | null>(null);
  aulaSeleccionadaId = signal<number | null>(null);

  cargandoAulas = signal<boolean>(false);
  cargando = signal<boolean>(false);
  guardando = signal<boolean>(false);
  mostrarModal = signal<boolean>(false);

  formAnuncio = signal<Partial<AnuncioComite>>({
    id: 0,
    aulaId: 0,
    titulo: '',
    contenido: '',
    categoria: 'INFORMATIVO',
    esFijado: false,
    urlAdjunto: '',
    usuarioRegistro: 'Comité de Aula'
  });

  tienePeriodoSeleccionado = computed(() => this.periodoSeleccionadoId() !== null && this.periodoSeleccionadoId()! > 0);
  tieneAulaSeleccionada = computed(() => this.aulaSeleccionadaId() !== null && this.aulaSeleccionadaId()! > 0);

  ngOnInit(): void {
    this.cargarPeriodos();
  }

  cargarPeriodos(): void {
    this.aulaService.getPeriodos().subscribe({
      next: (data) => this.periodos.set(data)
    });
  }

  onPeriodoChange(event: Event): void {
    const id = Number((event.target as HTMLSelectElement).value) || null;
    this.periodoSeleccionadoId.set(id);
    this.aulaSeleccionadaId.set(null);
    this.aulas.set([]);
    this.anuncios.set([]);

    if (id && id > 0) this.cargarAulasPorPeriodo(id);
  }

  cargarAulasPorPeriodo(periodoId: number): void {
    this.cargandoAulas.set(true);
    this.aulaService.getAulas(periodoId).subscribe({
      next: (data) => {
        this.aulas.set(data);
        this.cargandoAulas.set(false);
      },
      error: () => this.cargandoAulas.set(false)
    });
  }

  onAulaChange(event: Event): void {
    const aulaId = Number((event.target as HTMLSelectElement).value) || null;
    this.aulaSeleccionadaId.set(aulaId);

    if (aulaId && aulaId > 0) {
      this.cargarAnuncios(aulaId);
    } else {
      this.anuncios.set([]);
    }
  }

  cargarAnuncios(aulaId: number): void {
    this.cargando.set(true);
    const periodoObj = this.periodos().find(p => p.id === this.periodoSeleccionadoId());
    const anio = periodoObj ? periodoObj.anio : new Date().getFullYear();

    this.anuncioService.getAnunciosPorAula(aulaId, anio).subscribe({
      next: (data) => {
        this.anuncios.set(data);
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false)
    });
  }

  abrirModalNuevo(): void {
    const usuarioActual = this.authService.usuarioActual();
    const nombreUsuario = usuarioActual ? usuarioActual : 'Comité de Aula';
    
    this.formAnuncio.set({
      id: 0,
      aulaId: this.aulaSeleccionadaId()!,
      titulo: '',
      contenido: '',
      categoria: 'INFORMATIVO',
      esFijado: false,
      urlAdjunto: '',
      usuarioRegistro: nombreUsuario
    });
    this.mostrarModal.set(true);
  }

  abrirModalEditar(a: AnuncioComite): void {
    const usuarioActual = this.authService.usuarioActual();
    const nombreUsuario = usuarioActual ? usuarioActual : 'Comité de Aula';

    this.formAnuncio.set({
      id: a.id,
      aulaId: a.aulaId,
      titulo: a.titulo,
      contenido: a.contenido,
      categoria: a.categoria,
      esFijado: a.esFijado,
      urlAdjunto: a.urlAdjunto || '',
      usuarioRegistro: nombreUsuario
    });
    this.mostrarModal.set(true);
  }

  cerrarModal(): void {
    this.mostrarModal.set(false);
  }

  guardarAnuncio(): void {
    const dto = this.formAnuncio();
    if (!dto.titulo?.trim() || !dto.contenido?.trim()) return;

    dto.aulaId = this.aulaSeleccionadaId()!;
    this.guardando.set(true);

    this.anuncioService.guardarAnuncio(dto).subscribe({
      next: () => {
        this.guardando.set(false);
        this.cerrarModal();
        this.cargarAnuncios(this.aulaSeleccionadaId()!);
      },
      error: () => this.guardando.set(false)
    });
  }

  eliminarAnuncio(id: number): void {
    if (!confirm('¿Está seguro de eliminar este comunicado oficial?')) return;

    this.anuncioService.eliminarAnuncio(id, this.aulaSeleccionadaId()!).subscribe({
      next: () => this.cargarAnuncios(this.aulaSeleccionadaId()!)
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
