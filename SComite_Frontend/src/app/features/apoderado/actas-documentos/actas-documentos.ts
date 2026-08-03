import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ApoderadoService } from '../../../core/services/apoderado.service';
import { InstitucionService } from '../../../core/services/institucion.service';
import { ActaApoderado, HijoApoderado } from '../../../core/models/apoderado.model';
import { InstitucionEducativa } from '../../../core/models/institucion.model';

declare var html2canvas: any;
declare var jspdf: any;

@Component({
  selector: 'app-actas-documentos',
  imports: [CommonModule],
  templateUrl: './actas-documentos.html',
  styleUrl: './actas-documentos.scss',
})
export class ActasDocumentosComponent implements OnInit {
  private apoderadoService = inject(ApoderadoService);
  private institucionService = inject(InstitucionService);

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
    this.apoderadoService.getMisHijos(this.anioLectivoActual).subscribe({
      next: (data) => {
        this.hijos.set(data);
        this.cargandoHijos.set(false);

        if (data.length > 0) {
          this.estudianteSeleccionadoId.set(data[0].estudianteId);
          this.cargarActas();
        }
      },
      error: () => this.cargandoHijos.set(false)
    });
  }

  cargarDatosInstitucion(): void {
    this.institucionService.getConfiguracion().subscribe({
      next: (data) => {
        if (data) this.institucion.set(data);
      }
    });
  }

  cargarActas(): void {
    const estudianteId = this.estudianteSeleccionadoId();
    if (!estudianteId) return;

    this.cargandoActas.set(true);
    this.apoderadoService.getActasAprobadas(estudianteId, this.anioLectivoActual).subscribe({
      next: (data) => {
        this.actas.set(data);
        this.cargandoActas.set(false);
      },
      error: () => this.cargandoActas.set(false)
    });
  }

  onSeleccionarHijo(estudianteId: number): void {
    this.estudianteSeleccionadoId.set(estudianteId);
    this.cargarActas();
  }

  descargarPdfActa(acta: ActaApoderado): void {
    this.actaParaPdf.set(acta);
    this.descargandoPdf.set(true);

    const element = document.getElementById('acta-imprimible-pdf');
    if (!element) {
      this.descargandoPdf.set(false);
      return;
    }

    element.style.display = 'block';

    setTimeout(() => {
      const numActaClean = acta.numeroActa.replace(/[^A-Z0-9]/gi, '_');
      const nombreArchivo = `${numActaClean}.pdf`;

      html2canvas(element, {
        scale: 2,
        useCORS: true,
        logging: false,
        backgroundColor: '#ffffff',
        onclone: (clonedDoc: Document) => {
          const pdfEl = clonedDoc.getElementById('acta-imprimible-pdf');
          if (pdfEl) {
            pdfEl.style.display = 'block';
            pdfEl.style.backgroundColor = '#ffffff';
            pdfEl.style.color = '#0f172a';

            const elements = pdfEl.querySelectorAll('*');
            elements.forEach((el: any) => {
              el.style.boxShadow = 'none';
              el.style.textShadow = 'none';
              const style = window.getComputedStyle(el);

              if (style.color && style.color.includes('oklch')) el.style.color = '#0f172a';
              if (style.backgroundColor && style.backgroundColor.includes('oklch')) el.style.backgroundColor = '#ffffff';
              if (style.borderColor && style.borderColor.includes('oklch')) el.style.borderColor = '#cbd5e1';
            });
          }
        }
      }).then((canvas: HTMLCanvasElement) => {
        const imgData = canvas.toDataURL('image/jpeg', 0.98);
        const { jsPDF } = jspdf;
        const pdf = new jsPDF('p', 'mm', 'a4');

        const imgWidth = 210 - 20;
        const imgHeight = (canvas.height * imgWidth) / canvas.width;

        pdf.addImage(imgData, 'JPEG', 10, 10, imgWidth, imgHeight);
        pdf.save(nombreArchivo);

        element.style.display = 'none';
        this.descargandoPdf.set(false);
      }).catch((err: any) => {
        console.error('Error al generar PDF:', err);
        element.style.display = 'none';
        this.descargandoPdf.set(false);
      });
    }, 200);
  }
}
