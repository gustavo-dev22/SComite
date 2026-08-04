import { CommonModule } from '@angular/common';
import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ApoderadoService } from '../../../core/services/apoderado.service';
import { InstitucionService } from '../../../core/services/institucion.service';
import { ActaApoderado, HijoApoderado } from '../../../core/models/apoderado.model';
import { InstitucionEducativa } from '../../../core/models/institucion.model';
import { PdfExporterService } from '../../../core/services/pdf-exporter.service';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-actas-documentos',
  imports: [CommonModule],
  templateUrl: './actas-documentos.html',
  styleUrl: './actas-documentos.scss',
})
export class ActasDocumentosComponent implements OnInit {
  private destroyRef = inject(DestroyRef);
  private apoderadoService = inject(ApoderadoService);
  private institucionService = inject(InstitucionService);
  private pdfExporter = inject(PdfExporterService);

  cargandoHijos = signal<boolean>(false);
  cargandoActas = signal<boolean>(false);

  hijos = signal<HijoApoderado[]>([]);
  estudianteSeleccionadoId = signal<number | null>(null);

  actas = signal<ActaApoderado[]>([]);
  institucion = signal<InstitucionEducativa | null>(null);

  descargandoPdf = signal<boolean>(false);
  actaParaPdf = signal<ActaApoderado | null>(null);
  fechaEmision = new Date();

  anioLectivoActual = new Date().getFullYear();

  hijoActual = computed(() => {
    const id = this.estudianteSeleccionadoId();
    return this.hijos().find(h => h.estudianteId === id) || null;
  });

  ngOnInit(): void {
    this.cargarHijos();
    this.cargarDatosInstitucion();
  }

  cargarHijos(): void {
    this.cargandoHijos.set(true);
    this.apoderadoService.getMisHijos(this.anioLectivoActual).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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
    const estudianteId = this.estudianteSeleccionadoId();
    if (!estudianteId) return;

    this.cargandoActas.set(true);
    this.apoderadoService.getActasAprobadas(estudianteId, this.anioLectivoActual).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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
    this.actaParaPdf.set(acta);

    const element = document.getElementById('acta-imprimible-pdf');
    if (!element) {
      Swal.fire('Error', 'No se encontró el contenedor del acta.', 'error');
      this.descargandoPdf.set(false);
      return;
    }

    const numActaClean = acta.numeroActa.replace(/[^A-Z0-9]/gi, '_');
    const nombreArchivo = `${numActaClean}.pdf`;

    this.descargandoPdf.set(true);
    try {
      await this.pdfExporter.exportarElemento(element, nombreArchivo);
    } catch {
      Swal.fire('Error', 'No se pudo generar el PDF del acta. Inténtalo de nuevo.', 'error');
    } finally {
      this.descargandoPdf.set(false);
    }
  }
}
