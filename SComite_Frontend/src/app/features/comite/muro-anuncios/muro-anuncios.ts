import { CommonModule } from '@angular/common';
import { ModalA11yDirective } from '../../../shared/directives/modal-a11y.directive';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, takeUntil } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { AnuncioService } from '../../../core/services/anuncio.service';
import { AnuncioComite, ResumenAuditoriaAnuncio } from '../../../core/models/anuncio.model';
import { Aula } from '../../../core/models/aula.model';
import { AuthService } from '../../../core/services/auth.service';
import { manejarErrorHttp } from '../../../core/utils/http-error.util';
import { BasePeriodosComponent } from '../../../core/base/base-periodos.component';
import Swal from 'sweetalert2';

const BADGES_CATEGORIA_ANUNCIO: Record<string, string> = {
  'URGENTE': 'bg-rose-100 text-rose-700 border-rose-200',
  'CITACION': 'bg-amber-100 text-amber-800 border-amber-200',
  'TESORERIA': 'bg-emerald-100 text-emerald-800 border-emerald-200',
  'EVENTO': 'bg-indigo-100 text-indigo-800 border-indigo-200'
};

@Component({
  selector: 'app-muro-anuncios',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule, ModalA11yDirective],
  templateUrl: './muro-anuncios.html',
  styleUrl: './muro-anuncios.scss',
})
export class MuroAnunciosComponent extends BasePeriodosComponent implements OnInit {
  private reiniciarCarga$ = new Subject<void>();
  private anuncioService = inject(AnuncioService);
  private authService = inject(AuthService);

  constructor() {
    super();
    this.destroyRef.onDestroy(() => this.reiniciarCarga$.complete());
  }

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

  onPeriodoChange(event: Event): void {
    this.reiniciarCarga$.next();
    const id = Number((event.target as HTMLSelectElement).value) || null;
    this.periodoSeleccionadoId.set(id);
    this.aulaSeleccionadaId.set(null);
    this.aulas.set([]);
    this.anuncios.set([]);

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
      this.cargarAnuncios(aulaId);
    } else {
      this.anuncios.set([]);
    }
  }

  cargarAnuncios(aulaId: number): void {
    this.cargando.set(true);
    const periodoObj = this.periodos().find(p => p.id === this.periodoSeleccionadoId());
    const anio = periodoObj ? periodoObj.anio : new Date().getFullYear();

    this.anuncioService.getAnunciosPorAula(aulaId, anio).pipe(takeUntil(this.reiniciarCarga$), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.anuncios.set(data);
        this.cargando.set(false);
      },
      error: (err) => {
        this.cargando.set(false);
        manejarErrorHttp(err, 'No se pudieron cargar los comunicados.');
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
        manejarErrorHttp(err, 'No se pudo guardar el comunicado.');
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
            error: (err) => manejarErrorHttp(err, 'No se pudo eliminar el comunicado.')
          });
      }
    });
  }

  getCategoriaBadgeClass(categoria: string): string {
    return BADGES_CATEGORIA_ANUNCIO[categoria] || 'bg-slate-100 text-slate-700 border-slate-200';
  }

  actualizarCampo(campo: string, valor: unknown): void {
    this.formAnuncio.update(f => ({ ...f, [campo]: valor }) as Partial<AnuncioComite>);
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
        manejarErrorHttp(err, 'No se pudieron cargar las vistas del comunicado.');
      }
    });
  }

  cerrarModalAuditoria(): void {
    this.mostrarModalAuditoria.set(false);
    this.resumenAuditoria.set(null);
  }
}
