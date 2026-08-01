import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { BalanceService } from '../../../core/services/balance.service';
import { AulaService } from '../../../core/services/aula.service';
import { PeriodoLectivo } from '../../../core/models/periodoLectivo.model';
import { Aula } from '../../../core/models/aula.model';
import { BalanceConsolidado, GastoCategoriaResumen, GastoComiteDTO, GastoDetalleAgrupado } from '../../../core/models/balance.model';
import { ComiteService } from '../../../core/services/comite.service';
import { ComiteIntegrante } from '../../../core/models/comiteIntegrante.model';

declare var html2canvas: any;
declare var jspdf: any;

@Component({
  selector: 'app-balance-caja',
  imports: [CommonModule],
  templateUrl: './balance-caja.html',
  styleUrl: './balance-caja.scss',
})
export class BalanceCajaComponent implements OnInit {
  private balanceService = inject(BalanceService);
  private aulaService = inject(AulaService);
  private comiteService = inject(ComiteService);

  // Listas
  periodos = signal<PeriodoLectivo[]>([]);
  aulas = signal<Aula[]>([]);
  gastosCategorias = signal<GastoCategoriaResumen[]>([]);
  gastosDetalles = signal<GastoComiteDTO[]>([]);
  integrantesComiteRaw = signal<ComiteIntegrante[]>([]);

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
  fechaEmision = new Date();

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
    return aula ? `${aula.nivel} - ${aula.grado}° "${aula.seccion}"` : '';
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
    this.resetDatos();

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
      this.cargarBalanceConsolidado(aulaId);
      this.cargarIntegrantesComite(aulaId);
    } else {
      this.resetDatos();
    }
  }

  cargarIntegrantesComite(aulaId: number): void {
    this.comiteService.getComitePorAula(aulaId).subscribe({
      next: (data) => this.integrantesComiteRaw.set(data),
      error: () => this.integrantesComiteRaw.set([])
    });
  }

  onMesChange(event: Event): void {
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

    this.balanceService.obtenerConsolidado(aulaId, anio, mes).subscribe({
      next: (res) => {
        this.consolidado.set(res.consolidado);
        this.gastosCategorias.set(res.gastosPorCategoria);
        this.gastosDetalles.set(res.gastosDetalle || []);
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false)
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

  descargarPdfDirecto(): void {
    const element = document.getElementById('reporte-imprimible-pdf');
    if (!element) {
      console.error('No se encontró el contenedor #reporte-imprimible-pdf');
      return;
    }

    // 1. Mostrar modal de carga
    this.descargandoPdf.set(true);

    // 2. Visibilizar temporalmente el elemento para que html2canvas lo capture
    element.style.display = 'block';

    setTimeout(() => {
      const id = this.aulaSeleccionadaId();
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

      html2canvas(element, {
        scale: 2,
        useCORS: true,
        logging: false,
        backgroundColor: '#ffffff',
        onclone: (clonedDoc: Document) => {
          const pdfEl = clonedDoc.getElementById('reporte-imprimible-pdf');
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

        // Ocultar de nuevo
        element.style.display = 'none';
        this.descargandoPdf.set(false);
      }).catch((err: any) => {
        console.error('Error al generar PDF:', err);
        element.style.display = 'none';
        this.descargandoPdf.set(false);
      });

    }, 150);
  }
}
