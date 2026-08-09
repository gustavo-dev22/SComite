import { CommonModule } from '@angular/common';
import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { AnuncioService } from '../../../core/services/anuncio.service';
import { AulaService } from '../../../core/services/aula.service';
import { PeriodoLectivo } from '../../../core/models/periodoLectivo.model';
import { AnuncioComite, ResumenAuditoriaAnuncio } from '../../../core/models/anuncio.model';
import { Aula } from '../../../core/models/aula.model';
import { AuthService } from '../../../core/services/auth.service';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-muro-anuncios',
  imports: [CommonModule, FormsModule],
  templateUrl: './muro-anuncios.html',
  styleUrl: './muro-anuncios.scss',
})
export class MuroAnunciosComponent implements OnInit {
  private destroyRef = inject(DestroyRef);
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

  mostrarModalAuditoria = signal<boolean>(false);
  cargandoAuditoria = signal<boolean>(false);
  resumenAuditoria = signal<ResumenAuditoriaAnuncio | null>(null);
  anuncioAuditoriaTitulo = signal<string>('');
  filtroAuditoria = signal<'TODOS' | 'LEIDOS' | 'PENDIENTES'>('TODOS');

  lecturasFiltradas = computed(() => {
    const lista = this.resumenAuditoria()?.lecturas || [];
    const filtro = this.filtroAuditoria();

    if (filtro === 'LEIDOS') return lista.filter(x => x.leido);
    if (filtro === 'PENDIENTES') return lista.filter(x => !x.leido);
    return lista;
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
    this.anuncios.set([]);

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
      this.cargarAnuncios(aulaId);
    } else {
      this.anuncios.set([]);
    }
  }

  cargarAnuncios(aulaId: number): void {
    this.cargando.set(true);
    const periodoObj = this.periodos().find(p => p.id === this.periodoSeleccionadoId());
    const anio = periodoObj ? periodoObj.anio : new Date().getFullYear();

    this.anuncioService.getAnunciosPorAula(aulaId, anio).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.anuncios.set(data);
        this.cargando.set(false);
      },
      error: (err) => {
        this.cargando.set(false);
        Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar los comunicados.', 'error');
      }
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

    this.anuncioService.guardarAnuncio(dto).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.guardando.set(false);
        this.cerrarModal();
        this.cargarAnuncios(this.aulaSeleccionadaId()!);
      },
      error: (err) => {
        this.guardando.set(false);
        Swal.fire('Error', err.error?.mensaje || 'No se pudo guardar el comunicado.', 'error');
      }
    });
  }

  eliminarAnuncio(id: number): void {
    Swal.fire({
      title: '¿Eliminar comunicado?',
      text: 'Esta acción removerá el comunicado de la cartelera digital del aula.',
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
        this.anuncioService.eliminarAnuncio(id, this.aulaSeleccionadaId()!)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              Swal.fire({
                title: '¡Eliminado!',
                text: 'El comunicado oficial ha sido eliminado.',
                icon: 'success',
                timer: 1800,
                showConfirmButton: false
              });
              this.cargarAnuncios(this.aulaSeleccionadaId()!);
            },
            error: (err) => Swal.fire('Error', err.error?.mensaje || 'No se pudo eliminar el comunicado.', 'error')
          });
      }
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

  abrirModalVistas(anuncio: AnuncioComite): void {
    this.anuncioAuditoriaTitulo.set(anuncio.titulo);
    this.mostrarModalAuditoria.set(true);
    this.cargandoAuditoria.set(true);
    this.filtroAuditoria.set('TODOS');

    this.anuncioService.getAuditoriaVistas(anuncio.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.resumenAuditoria.set(data);
        this.cargandoAuditoria.set(false);
      },
      error: (err) => {
        this.cargandoAuditoria.set(false);
        Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar las vistas del comunicado.', 'error');
      }
    });
  }

  cerrarModalAuditoria(): void {
    this.mostrarModalAuditoria.set(false);
    this.resumenAuditoria.set(null);
  }
}
