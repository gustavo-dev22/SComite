import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, takeUntil } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { ActaService } from '../../../core/services/acta.service';
import { AuthService } from '../../../core/services/auth.service';
import { Aula } from '../../../core/models/aula.model';
import { ActaAsambleaComite } from '../../../core/models/acta.model';
import { ComiteService } from '../../../core/services/comite.service';
import { ComiteIntegrante } from '../../../core/models/comiteIntegrante.model';
import { InstitucionService } from '../../../core/services/institucion.service';
import { InstitucionEducativa } from '../../../core/models/institucion.model';
import { PdfExporterService } from '../../../core/services/pdf-exporter.service';
import { manejarErrorHttp } from '../../../core/utils/http-error.util';
import { formatearFechaLocal, hoyLocal } from '../../../core/utils/fecha.util';
import { BasePeriodosComponent } from '../../../core/base/base-periodos.component';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-actas-asamblea',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule],
  templateUrl: './actas-asamblea.html',
  styleUrl: './actas-asamblea.scss',
})
export class ActasAsambleaComponent extends BasePeriodosComponent implements OnInit {
  private reiniciarCarga$ = new Subject<void>();
  private actaService = inject(ActaService);
  private authService = inject(AuthService);
  private comiteService = inject(ComiteService);
  private institucionService = inject(InstitucionService);
  private pdfExporter = inject(PdfExporterService);

  constructor() {
    super();
    this.destroyRef.onDestroy(() => this.reiniciarCarga$.complete());
  }

  aulas = signal<Aula[]>([]);
  actas = signal<ActaAsambleaComite[]>([]);
  integrantesComiteRaw = signal<ComiteIntegrante[]>([]);
  institucion = signal<InstitucionEducativa | null>(null);

  periodoSeleccionadoId = signal<number | null>(null);
  aulaSeleccionadaId = signal<number | null>(null);

  cargandoAulas = signal<boolean>(false);
  cargando = signal<boolean>(false);
  guardando = signal<boolean>(false);
  mostrarModal = signal<boolean>(false);

  descargandoPdf = signal<boolean>(false);

  formActa = signal<Partial<ActaAsambleaComite>>({
    id: 0,
    aulaId: 0,
    numeroActa: '',
    titulo: '',
    fechaReunion: hoyLocal(),
    agendaAcuerdos: '',
    estadoActa: 'APROBADA',
    urlDocumentoPdf: '',
    usuarioRegistro: ''
  });

  tienePeriodoSeleccionado = computed(() => this.periodoSeleccionadoId() !== null && this.periodoSeleccionadoId()! > 0);
  tieneAulaSeleccionada = computed(() => this.aulaSeleccionadaId() !== null && this.aulaSeleccionadaId()! > 0);

  aulaNombreActual = computed(() => {
    const id = this.aulaSeleccionadaId();
    const aula = this.aulas().find(a => a.id === id);
    return aula ? `${aula.nivel} - ${aula.grado} "${aula.seccion}"` : '';
  });

  anioActual = computed(() => {
    const id = this.periodoSeleccionadoId();
    const p = this.periodos().find(x => x.id === id);
    return p ? p.anio : new Date().getFullYear();
  });

  firmasComite = computed(() => {
    const lista = this.integrantesComiteRaw();
    const obtenerNombre = (cargoBusqueda: string) => {
      const miembro = lista.find(m => m.cargo?.toUpperCase().includes(cargoBusqueda));
      return miembro ? miembro.nombreCompleto : '________________________';
    };

    return {
      presidente: obtenerNombre('PRESIDENTE'),
      tesorero: obtenerNombre('TESORERO'),
      secretario: obtenerNombre('SECRETARIO'),
      vocal: obtenerNombre('VOCAL')
    };
  });

  ngOnInit(): void {
    this.cargarPeriodos();
    this.cargarDatosInstitucion();
  }

  cargarDatosInstitucion(): void {
    this.institucionService.getConfiguracion().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        if (data) this.institucion.set(data);
      },
      error: (err) => manejarErrorHttp(err, 'No se pudieron cargar los datos de la institución educativa.')
    });
  }

  onPeriodoChange(event: Event): void {
    this.reiniciarCarga$.next();
    const id = Number((event.target as HTMLSelectElement).value) || null;
    this.periodoSeleccionadoId.set(id);
    this.aulaSeleccionadaId.set(null);
    this.aulas.set([]);
    this.actas.set([]);

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
      this.cargarActas(aulaId);
      this.cargarIntegrantesComite(aulaId);
    } else {
      this.actas.set([]);
      this.integrantesComiteRaw.set([]);
    }
  }

  cargarIntegrantesComite(aulaId: number): void {
    this.comiteService.getComitePorAula(aulaId).pipe(takeUntil(this.reiniciarCarga$), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => this.integrantesComiteRaw.set(data),
      error: (err) => {
        this.integrantesComiteRaw.set([]);
        manejarErrorHttp(err, 'No se pudieron cargar los integrantes del comité.');
      }
    });
  }

  cargarActas(aulaId: number): void {
    this.cargando.set(true);
    const periodoObj = this.periodos().find(p => p.id === this.periodoSeleccionadoId());
    const anio = periodoObj ? periodoObj.anio : new Date().getFullYear();

    this.actaService.getActasPorAula(aulaId, anio).pipe(takeUntil(this.reiniciarCarga$), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.actas.set(data);
        this.cargando.set(false);
      },
      error: (err) => {
        this.cargando.set(false);
        manejarErrorHttp(err, 'No se pudieron cargar las actas.');
      }
    });
  }

  abrirModalNuevo(): void {
    const aulaId = this.aulaSeleccionadaId();
    if (!aulaId) return;

    const periodoObj = this.periodos().find(p => p.id === this.periodoSeleccionadoId());
    const anio = periodoObj ? periodoObj.anio : new Date().getFullYear();

    this.actaService.getSiguienteNumeroActa(aulaId, anio).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        const usuarioActual = this.authService.usuarioActual();
        const nombreUsuario = usuarioActual ? usuarioActual : 'Comité de Aula';

        this.formActa.set({
          id: 0,
          aulaId: aulaId,
          numeroActa: res.siguienteNumeroActa, 
          titulo: '',
          fechaReunion: hoyLocal(),
          agendaAcuerdos: '',
          estadoActa: 'BORRADOR',
          urlDocumentoPdf: '',
          usuarioRegistro: nombreUsuario
        });
        
        this.mostrarModal.set(true);
      },
      error: (err) => {
        manejarErrorHttp(err, 'No se pudo obtener el número de acta. Intente nuevamente.');
      }
    });
  }

  abrirModalEditar(a: ActaAsambleaComite): void {
    const usuarioActual = this.authService.usuarioActual();
    const nombreUsuario = usuarioActual ? usuarioActual : (a.usuarioRegistro || 'Comité de Aula');

    this.formActa.set({
      id: a.id,
      aulaId: a.aulaId,
      numeroActa: a.numeroActa,
      titulo: a.titulo,
      fechaReunion: formatearFechaLocal(a.fechaReunion),
      agendaAcuerdos: a.agendaAcuerdos,
      estadoActa: a.estadoActa,
      urlDocumentoPdf: a.urlDocumentoPdf || '',
      usuarioRegistro: nombreUsuario
    });
    this.mostrarModal.set(true);
  }

  cerrarModal(): void {
    this.mostrarModal.set(false);
  }

  onInputToUppercase(campo: 'numeroActa' | 'titulo'): void {
    const val = this.formActa()[campo] || '';
    this.formActa.update(f => ({ ...f, [campo]: val.toUpperCase() }));
  }

  actualizarCampo(campo: string, valor: unknown): void {
    this.formActa.update(f => ({ ...f, [campo]: valor }) as Partial<ActaAsambleaComite>);
  }

  guardarActa(): void {
    const dto = this.formActa();
    if (!dto.numeroActa?.trim() || !dto.titulo?.trim() || !dto.agendaAcuerdos?.trim()) return;

    dto.aulaId = this.aulaSeleccionadaId()!;
    this.guardando.set(true);

    this.actaService.guardarActa(dto).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.guardando.set(false);
        this.cerrarModal();
        this.cargarActas(this.aulaSeleccionadaId()!);
      },
      error: (err) => {
        this.guardando.set(false);
        manejarErrorHttp(err, 'No se pudo guardar el acta.');
      }
    });
  }

  eliminarActa(id: number): void {
    Swal.fire({
      title: '¿Eliminar acta de asamblea?',
      text: 'Esta acción eliminará el registro del libro digital de actas del aula.',
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
        this.actaService.eliminarActa(id, this.aulaSeleccionadaId()!)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              Swal.fire({
                title: '¡Eliminada!',
                text: 'El acta de asamblea ha sido eliminada correctamente.',
                icon: 'success',
                timer: 1800,
                showConfirmButton: false
              });
              this.cargarActas(this.aulaSeleccionadaId()!);
            },
            error: (err) => manejarErrorHttp(err, 'No se pudo eliminar el acta.')
          });
      }
    });
  }

  getEstadoClass(estado: string): string {
    switch (estado) {
      case 'APROBADA': return 'bg-emerald-100 text-emerald-800 border-emerald-200';
      case 'BORRADOR': return 'bg-amber-100 text-amber-800 border-amber-200';
      default: return 'bg-slate-100 text-slate-700 border-slate-200';
    }
  }

  async descargarPdfActa(acta: ActaAsambleaComite): Promise<void> {
    const numActaClean = acta.numeroActa.replace(/[^A-Z0-9]/gi, '_');
    const nombreArchivo = `${numActaClean}.pdf`;

    const datePipe = new DatePipe('en-US');
    const fechaReunionStr = datePipe.transform(acta.fechaReunion, 'dd/MM/yyyy') || '';

    this.descargandoPdf.set(true);
    try {
      await this.pdfExporter.exportarActaOficial({
        nombreArchivo,
        nombreInstitucion: this.institucion()?.nombreInstitucion,
        urlLogo: this.institucion()?.urlLogo,
        aulaNombre: this.aulaNombreActual(),
        anioLectivo: this.anioActual(),
        numeroActa: acta.numeroActa,
        estadoActa: acta.estadoActa,
        fechaReunion: fechaReunionStr,
        usuarioRegistro: acta.usuarioRegistro,
        tituloAsamblea: acta.titulo,
        agendaAcuerdos: acta.agendaAcuerdos,
        fechaEmision: new Date() // Fecha y hora exacta de descarga
      });
    } catch {
      Swal.fire('Error', 'No se pudo generar el PDF del acta. Inténtalo de nuevo.', 'error');
    } finally {
      this.descargandoPdf.set(false);
    }
  }
}
