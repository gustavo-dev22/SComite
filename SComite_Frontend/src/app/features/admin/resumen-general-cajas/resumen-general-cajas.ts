import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, takeUntil } from 'rxjs';

const BADGES_ESTADO_CAJA: Record<string, string> = {
  'AL_DIA': 'bg-emerald-100 text-emerald-800 border-emerald-200',
  'SIN_MOVIMIENTO': 'bg-slate-100 text-slate-700 border-slate-200',
  'ALERTA_ROJO': 'bg-rose-100 text-rose-800 border-rose-200'
};
import { FormsModule } from '@angular/forms';
import { AuditoriaService } from '../../../core/services/auditoria.service';
import { InstitucionService } from '../../../core/services/institucion.service';
import { PeriodoLectivo } from '../../../core/models/periodoLectivo.model';
import { ResumenGeneralCajasConsolidadas } from '../../../core/models/auditoria.model';
import { InstitucionEducativa } from '../../../core/models/institucion.model';
import { PdfExporterService } from '../../../core/services/pdf-exporter.service';
import { BasePeriodosComponent } from '../../../core/base/base-periodos.component';
import { manejarErrorHttp } from '../../../core/utils/http-error.util';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-resumen-general-cajas',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule],
  templateUrl: './resumen-general-cajas.html',
  styleUrl: './resumen-general-cajas.scss',
})
export class ResumenGeneralCajasComponent extends BasePeriodosComponent implements OnInit {
  private reiniciarCarga$ = new Subject<void>();
  private auditoriaService = inject(AuditoriaService);
  private institucionService = inject(InstitucionService);
  private pdfExporter = inject(PdfExporterService);

  constructor() {
    super();
    this.destroyRef.onDestroy(() => this.reiniciarCarga$.complete());
  }

  cargando = signal<boolean>(false);
  periodoSeleccionadoId = signal<number | null>(null);
  nivelFiltro = signal<string>('');

  dataConsolidada = signal<ResumenGeneralCajasConsolidadas | null>(null);
  institucion = signal<InstitucionEducativa | null>(null);

  descargandoPdf = signal<boolean>(false);

  anioActual = computed(() => {
    const id = this.periodoSeleccionadoId();
    const p = this.periodos().find(x => x.id === id);
    return p ? p.anio : new Date().getFullYear();
  });

  ngOnInit(): void {
    this.cargarPeriodos();
    this.cargarDatosInstitucion();
  }

  protected override onPeriodosCargados(data: PeriodoLectivo[]): void {
    const activo = data.find(p => p.esActivo);
    if (activo) {
      this.periodoSeleccionadoId.set(activo.id);
      this.cargarResumen();
    }
  }

  cargarDatosInstitucion(): void {
    this.institucionService.getConfiguracion().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        if (data) this.institucion.set(data);
      },
      error: (err) => manejarErrorHttp(err, 'No se pudieron cargar los datos de la institución educativa.')
    });
  }

  cargarResumen(): void {
    this.reiniciarCarga$.next();
    const anio = this.anioActual();
    const nivel = this.nivelFiltro();

    this.cargando.set(true);
    this.auditoriaService.getResumenGeneralCajas(anio, nivel).pipe(takeUntil(this.reiniciarCarga$), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        this.dataConsolidada.set(res);
        this.cargando.set(false);
      },
      error: (err) => {
        this.cargando.set(false);
        manejarErrorHttp(err, 'No se pudo cargar el resumen general de cajas.');
      }
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
    return BADGES_ESTADO_CAJA[estado] || 'bg-slate-100 text-slate-700';
  }

  // Exportar Consolidado Global a PDF
  async exportarPdf(): Promise<void> {
    const data = this.dataConsolidada();
    if (!data) {
      Swal.fire('Error', 'No hay datos cargados para exportar.', 'error');
      return;
    }

    const nombreArchivo = `Resumen_General_Cajas_${this.anioActual()}.pdf`;

    this.descargandoPdf.set(true);
    try {
      await this.pdfExporter.exportarResumenGeneralCajas({
        nombreArchivo,
        nombreInstitucion: this.institucion()?.nombreInstitucion,
        urlLogo: this.institucion()?.urlLogo,
        anioLectivo: this.anioActual(),
        totalIngresos: data.totalIngresosInstitucional || 0,
        totalEgresos: data.totalEgresosInstitucional || 0,
        saldoNeto: data.saldoNetoInstitucional || 0,
        aulas: (data.detalleAulas || []).map(a => ({
          nombreAula: a.nombreAula,
          nivel: a.nivel,
          totalIngresos: a.totalIngresos,
          totalEgresos: a.totalEgresos,
          saldoNeto: a.saldoNeto
        })),
        fechaEmision: new Date() // Fecha y hora exacta actual de la descarga
      });
    } catch {
      Swal.fire('Error', 'No se pudo generar el PDF. Inténtalo de nuevo.', 'error');
    } finally {
      this.descargandoPdf.set(false);
    }
  }
}
