import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, takeUntil } from 'rxjs';
import { ApoderadoService } from '../../../core/services/apoderado.service';
import { AulaService } from '../../../core/services/aula.service';
import { InstitucionService } from '../../../core/services/institucion.service';
import { ActaApoderado, HijoApoderado } from '../../../core/models/apoderado.model';
import { InstitucionEducativa } from '../../../core/models/institucion.model';
import { PdfExporterService } from '../../../core/services/pdf-exporter.service';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-actas-documentos',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  templateUrl: './actas-documentos.html',
  styleUrl: './actas-documentos.scss',
})
export class ActasDocumentosComponent implements OnInit {
  private destroyRef = inject(DestroyRef);
  private reiniciarCarga$ = new Subject<void>();
  private apoderadoService = inject(ApoderadoService);
  private aulaService = inject(AulaService);
  private institucionService = inject(InstitucionService);
  private pdfExporter = inject(PdfExporterService);

  constructor() {
    this.destroyRef.onDestroy(() => this.reiniciarCarga$.complete());
  }

  cargandoHijos = signal<boolean>(false);
  cargandoActas = signal<boolean>(false);

  hijos = signal<HijoApoderado[]>([]);
  estudianteSeleccionadoId = signal<number | null>(null);

  actas = signal<ActaApoderado[]>([]);
  institucion = signal<InstitucionEducativa | null>(null);

  descargandoPdf = signal<boolean>(false);
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
    this.cargarDatosInstitucion();
  }

  cargarHijos(): void {
    this.cargandoHijos.set(true);
    this.apoderadoService.getMisHijos(this.anioLectivoActual()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.hijos.set(data);
        this.cargandoHijos.set(false);

        if (data.length > 0) {
          this.estudianteSeleccionadoId.set(data[0].estudianteId);
          this.cargarActas();
        }
      },
      error: (err) => {
        this.cargandoHijos.set(false);
        Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar tus hijos.', 'error');
      }
    });
  }

  cargarDatosInstitucion(): void {
    this.institucionService.getConfiguracion().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        if (data) this.institucion.set(data);
      },
      error: (err) => Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar los datos de la institución educativa.', 'error')
    });
  }

  cargarActas(): void {
    this.reiniciarCarga$.next();
    const estudianteId = this.estudianteSeleccionadoId();
    if (!estudianteId) return;

    this.cargandoActas.set(true);
    this.apoderadoService.getActasAprobadas(estudianteId, this.anioLectivoActual()).pipe(takeUntil(this.reiniciarCarga$), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.actas.set(data);
        this.cargandoActas.set(false);
      },
      error: (err) => {
        this.cargandoActas.set(false);
        Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar las actas.', 'error');
      }
    });
  }

  onSeleccionarHijo(estudianteId: number): void {
    this.estudianteSeleccionadoId.set(estudianteId);
    this.cargarActas();
  }

  async descargarPdfActa(acta: ActaApoderado): Promise<void> {
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
        aulaNombre: this.hijoActual()?.nombreAula || '',
        anioLectivo: this.anioLectivoActual(),
        numeroActa: acta.numeroActa,
        estadoActa: acta.estadoActa,
        fechaReunion: fechaReunionStr,
        usuarioRegistro: acta.usuarioRegistro,
        tituloAsamblea: acta.titulo,
        agendaAcuerdos: acta.agendaAcuerdos,
        fechaEmision: new Date() // Hora actual exacta del clic
      });
    } catch {
      Swal.fire('Error', 'No se pudo generar el PDF del acta. Inténtalo de nuevo.', 'error');
    } finally {
      this.descargandoPdf.set(false);
    }
  }
}
