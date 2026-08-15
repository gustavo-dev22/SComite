import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, takeUntil } from 'rxjs';
import { BalanceService } from '../../../core/services/balance.service';
import { AulaService } from '../../../core/services/aula.service';
import { PeriodoLectivo } from '../../../core/models/periodoLectivo.model';
import { Aula } from '../../../core/models/aula.model';
import { BalanceConsolidado, GastoCategoriaResumen, GastoComiteDTO, GastoDetalleAgrupado } from '../../../core/models/balance.model';
import { ComiteService } from '../../../core/services/comite.service';
import { ComiteIntegrante } from '../../../core/models/comiteIntegrante.model';
import { InstitucionService } from '../../../core/services/institucion.service';
import { InstitucionEducativa } from '../../../core/models/institucion.model';
import { PdfExporterService } from '../../../core/services/pdf-exporter.service';
import { manejarErrorHttp } from '../../../core/utils/http-error.util';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-balance-caja',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  templateUrl: './balance-caja.html',
  styleUrl: './balance-caja.scss',
})
export class BalanceCajaComponent implements OnInit {
  private destroyRef = inject(DestroyRef);
  private reiniciarCarga$ = new Subject<void>();
  private balanceService = inject(BalanceService);
  private aulaService = inject(AulaService);
  private comiteService = inject(ComiteService);
  private institucionService = inject(InstitucionService);
  private pdfExporter = inject(PdfExporterService);

  constructor() {
    this.destroyRef.onDestroy(() => this.reiniciarCarga$.complete());
  }

  // Listas
  periodos = signal<PeriodoLectivo[]>([]);
  aulas = signal<Aula[]>([]);
  gastosCategorias = signal<GastoCategoriaResumen[]>([]);
  gastosDetalles = signal<GastoComiteDTO[]>([]);
  integrantesComiteRaw = signal<ComiteIntegrante[]>([]);
  institucion = signal<InstitucionEducativa | null>(null);

  // Consolidado Financiero
  consolidado = signal<BalanceConsolidado>({
    saldoAnteriorArrastrado: 0,
    ingresosMensuales: 0,
    ingresosExtraordinarios: 0,
    ingresosDonaciones: 0,
    totalIngresosMes: 0,
    totalEgresosMes: 0,
    saldoNetoEnCaja: 0,
    totalPorCobrar: 0,
    porcentajeCumplimiento: 0
  });

  // Selecciones (Secuencia de controles)
  periodoSeleccionadoId = signal<number | null>(null);
  aulaSeleccionadaId = signal<number | null>(null);
  mesSeleccionado = signal<number>(0);

  // Estados UI
  cargando = signal<boolean>(false);
  cargandoAulas = signal<boolean>(false);
  descargandoPdf = signal<boolean>(false);

  tienePeriodoSeleccionado = computed(() => this.periodoSeleccionadoId() !== null && this.periodoSeleccionadoId()! > 0);
  tieneAulaSeleccionada = computed(() => this.aulaSeleccionadaId() !== null && this.aulaSeleccionadaId()! > 0);

  firmasComite = computed(() => {
    const lista = this.integrantesComiteRaw();

    const obtenerNombre = (cargoBusqueda: string) => {
      const miembro = lista.find(m => m.cargo?.toUpperCase().includes(cargoBusqueda));
      return miembro ? (miembro.nombreCompleto) : '________________________';
    };

    return {
      presidente: obtenerNombre('PRESIDENTE'),
      tesorero: obtenerNombre('TESORERO'),
      secretario: obtenerNombre('SECRETARIO'),
      vocal: obtenerNombre('VOCAL')
    };
  });

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

  nombreMesActual = computed(() => {
    const mes = this.mesSeleccionado();
    const meses = ['Todo el Año (Acumulado)', 'Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio', 'Julio', 'Agosto', 'Setiembre', 'Octubre', 'Noviembre', 'Diciembre'];
    return meses[mes] || 'Mes Actual';
  });

  gastosAgrupados = computed<GastoDetalleAgrupado[]>(() => {
    const lista = [...this.gastosDetalles()];
    if (lista.length === 0) return [];

    // 1. Ordenar por categoría para que los ítems del mismo grupo estén contiguos
    lista.sort((a, b) => a.categoria.localeCompare(b.categoria));

    const resultado: GastoDetalleAgrupado[] = [];
    let i = 0;

    while (i < lista.length) {
      const categoriaActual = lista[i].categoria;
      let count = 0;

      // Contar cuántos elementos pertenecen a la misma categoría
      for (let j = i; j < lista.length; j++) {
        if (lista[j].categoria === categoriaActual) {
          count++;
        } else {
          break;
        }
      }

      // El primer elemento del grupo lleva el rowspan
      for (let j = 0; j < count; j++) {
        const item = { ...lista[i + j] } as GastoDetalleAgrupado;
        if (j === 0) {
          item.rowspan = count;
          item.esPrimerItemDelGrupo = true;
        } else {
          item.esPrimerItemDelGrupo = false;
        }
        resultado.push(item);
      }

      i += count;
    }

    return resultado;
  });

  ngOnInit(): void {
    this.cargarPeriodos();
    this.cargarDatosInstitucion();
  }

  cargarPeriodos(): void {
    this.aulaService.getPeriodos().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => this.periodos.set(data),
      error: (err) => manejarErrorHttp(err, 'No se pudieron cargar los periodos lectivos.')
    });
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
    this.resetDatos();

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
      this.cargarBalanceConsolidado(aulaId);
      this.cargarIntegrantesComite(aulaId);
    } else {
      this.resetDatos();
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

  onMesChange(event: Event): void {
    this.reiniciarCarga$.next();
    const mes = Number((event.target as HTMLSelectElement).value);
    this.mesSeleccionado.set(mes);

    const aulaId = this.aulaSeleccionadaId();
    if (aulaId) {
      this.cargarBalanceConsolidado(aulaId);
    }
  }

  cargarBalanceConsolidado(aulaId: number): void {
    this.cargando.set(true);
    const periodoId = this.periodoSeleccionadoId();
    const periodoObj = this.periodos().find(p => p.id === periodoId);
    const anio = periodoObj ? periodoObj.anio : new Date().getFullYear();
    const mes = this.mesSeleccionado();

    this.balanceService.obtenerConsolidado(aulaId, anio, mes).pipe(takeUntil(this.reiniciarCarga$), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        this.consolidado.set(res.consolidado);
        this.gastosCategorias.set(res.gastosPorCategoria);
        this.gastosDetalles.set(res.gastosDetalle || []);
        this.cargando.set(false);
      },
      error: (err) => {
        this.cargando.set(false);
        manejarErrorHttp(err, 'No se pudo cargar el balance de caja.');
      }
    });
  }

  private resetDatos(): void {
    this.consolidado.set({
      saldoAnteriorArrastrado: 0,
      ingresosMensuales: 0,
      ingresosExtraordinarios: 0,
      ingresosDonaciones: 0,
      totalIngresosMes: 0,
      totalEgresosMes: 0,
      saldoNetoEnCaja: 0,
      totalPorCobrar: 0,
      porcentajeCumplimiento: 0
    });
    this.gastosCategorias.set([]);
    this.gastosDetalles.set([]);
    this.integrantesComiteRaw.set([]);
  }

  async descargarPdfDirecto(): Promise<void> {
    const id = this.aulaSeleccionadaId();
    if (!id) return;

    const aula = this.aulas().find(a => a.id === id);
    const mesVal = this.mesSeleccionado();

    let nivelStr = 'NIVEL';
    let gradoStr = 'GRADO';
    let seccionStr = 'SECCION';

    if (aula) {
      nivelStr = aula.nivel.toUpperCase().trim().replace(/\s+/g, '_');
      gradoStr = String(aula.grado)
        .toUpperCase()
        .trim()
        .replace(/[^A-Z0-9Ññ]/g, '_')
        .replace(/_+/g, '_')
        .replace(/^_|_$/g, '');
      seccionStr = aula.seccion.toUpperCase().trim().replace(/[^A-Z0-9]/g, '');
    }

    let nombreArchivo = '';
    if (mesVal === 0) {
      nombreArchivo = `Rendicion_Todo_el_Año_(Acumulado)_${nivelStr}_${gradoStr}_${seccionStr}.pdf`;
    } else {
      const mesNombre = this.nombreMesActual().trim().replace(/\s+/g, '_');
      nombreArchivo = `Rendicion_Mes_${mesNombre}_${nivelStr}_${gradoStr}_${seccionStr}.pdf`;
    }

    this.descargandoPdf.set(true);
    try {
      const c = this.consolidado();

      await this.pdfExporter.exportarRendicionCaja({
        nombreArchivo,
        nombreInstitucion: this.institucion()?.nombreInstitucion,
        urlLogo: this.institucion()?.urlLogo,
        aulaNombre: this.aulaNombreActual(),
        anioLectivo: this.anioActual(),
        periodoTexto: this.nombreMesActual(),
        fechaEmision: new Date(), // Hora exacta de descarga
        cuadro1: {
          saldoAnterior: c.saldoAnteriorArrastrado || 0,
          totalIngresosMes: c.totalIngresosMes || 0,
          totalEgresosMes: c.totalEgresosMes || 0,
          saldoNeto: c.saldoNetoEnCaja || 0
        },
        cuadro2: {
          ingresosMensuales: c.ingresosMensuales || 0,
          ingresosExtraordinarios: c.ingresosExtraordinarios || 0,
          ingresosDonaciones: c.ingresosDonaciones || 0
        },
        cuadro3Gastos: (this.gastosAgrupados() || []).map(g => ({
          categoria: g.categoria,
          concepto: g.concepto,
          proveedor: g.proveedor,
          tipoComprobante: g.tipoComprobante,
          numeroComprobante: g.numeroComprobante,
          monto: g.monto
        }))
      });
    } catch {
      Swal.fire('Error', 'No se pudo generar el PDF de Rendición de Cuentas.', 'error');
    } finally {
      this.descargandoPdf.set(false);
    }
  }
}
