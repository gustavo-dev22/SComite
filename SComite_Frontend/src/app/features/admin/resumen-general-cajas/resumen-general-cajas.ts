import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuditoriaService } from '../../../core/services/auditoria.service';
import { AulaService } from '../../../core/services/aula.service';
import { InstitucionService } from '../../../core/services/institucion.service';
import { PeriodoLectivo } from '../../../core/models/periodoLectivo.model';
import { ResumenGeneralCajasConsolidadas } from '../../../core/models/auditoria.model';
import { InstitucionEducativa } from '../../../core/models/institucion.model';

declare var html2canvas: any;
declare var jspdf: any;

@Component({
  selector: 'app-resumen-general-cajas',
  imports: [CommonModule, FormsModule],
  templateUrl: './resumen-general-cajas.html',
  styleUrl: './resumen-general-cajas.scss',
})
export class ResumenGeneralCajasComponent implements OnInit {
  private auditoriaService = inject(AuditoriaService);
  private aulaService = inject(AulaService);
  private institucionService = inject(InstitucionService);

  cargando = signal<boolean>(false);
  periodos = signal<PeriodoLectivo[]>([]);
  periodoSeleccionadoId = signal<number | null>(null);
  nivelFiltro = signal<string>('');

  dataConsolidada = signal<ResumenGeneralCajasConsolidadas | null>(null);
  institucion = signal<InstitucionEducativa | null>(null);

  descargandoPdf = signal<boolean>(false);
  fechaEmision = new Date();

  anioActual = computed(() => {
    const id = this.periodoSeleccionadoId();
    const p = this.periodos().find(x => x.id === id);
    return p ? p.anio : new Date().getFullYear();
  });

  ngOnInit(): void {
    this.cargarPeriodos();
    this.cargarDatosInstitucion();
  }

  cargarPeriodos(): void {
    this.aulaService.getPeriodos().subscribe({
      next: (data) => {
        this.periodos.set(data);
        const activo = data.find(p => p.esActivo);
        if (activo) {
          this.periodoSeleccionadoId.set(activo.id);
          this.cargarResumen();
        }
      }
    });
  }

  cargarDatosInstitucion(): void {
    this.institucionService.getConfiguracion().subscribe({
      next: (data) => {
        if (data) this.institucion.set(data);
      }
    });
  }

  cargarResumen(): void {
    const anio = this.anioActual();
    const nivel = this.nivelFiltro();

    this.cargando.set(true);
    this.auditoriaService.getResumenGeneralCajas(anio, nivel).subscribe({
      next: (res) => {
        this.dataConsolidada.set(res);
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false)
    });
  }

  onPeriodoChange(event: Event): void {
    const id = Number((event.target as HTMLSelectElement).value);
    this.periodoSeleccionadoId.set(id);
    this.cargarResumen();
  }

  onNivelChange(event: Event): void {
    const nivel = (event.target as HTMLSelectElement).value;
    this.nivelFiltro.set(nivel);
    this.cargarResumen();
  }

  getEstadoBadgeClass(estado: string): string {
    switch (estado) {
      case 'AL_DIA': return 'bg-emerald-100 text-emerald-800 border-emerald-200';
      case 'SIN_MOVIMIENTO': return 'bg-slate-100 text-slate-700 border-slate-200';
      case 'ALERTA_ROJO': return 'bg-rose-100 text-rose-800 border-rose-200';
      default: return 'bg-slate-100 text-slate-700';
    }
  }

  // Exportar Consolidado Global a PDF
  exportarPdf(): void {
    const element = document.getElementById('reporte-resumen-general-pdf');
    if (!element) return;

    this.descargandoPdf.set(true);
    element.style.display = 'block';

    setTimeout(() => {
      const nombreArchivo = `Resumen_General_Cajas_${this.anioActual()}.pdf`;

      html2canvas(element, {
        scale: 2,
        useCORS: true,
        logging: false,
        backgroundColor: '#ffffff'
      }).then((canvas: HTMLCanvasElement) => {
        const imgData = canvas.toDataURL('image/jpeg', 0.98);
        const { jsPDF } = jspdf;
        const pdf = new jsPDF('p', 'mm', 'a4');

        const imgWidth = 190;
        const imgHeight = (canvas.height * imgWidth) / canvas.width;

        pdf.addImage(imgData, 'JPEG', 10, 10, imgWidth, imgHeight);
        pdf.save(nombreArchivo);

        element.style.display = 'none';
        this.descargandoPdf.set(false);
      }).catch((err: any) => {
        console.error('Error generando PDF:', err);
        element.style.display = 'none';
        this.descargandoPdf.set(false);
      });
    }, 200);
  }
}
