import { Injectable } from '@angular/core';

export interface ColumnasPdf {
  header: string;
  dataKey: string;
  align?: 'left' | 'center' | 'right';
}

export interface FichaCampoPdf {
  etiqueta: string;
  valor: string;
}

interface JsPDFConAutoTable {
  lastAutoTable?: { finalY: number } | null;
}

@Injectable({
  providedIn: 'root'
})
export class PdfExporterService {
  private cargarJsPDF(): Promise<typeof import('jspdf')['default']> {
    return import('jspdf').then((m) => m.default);
  }

  private cargarAutoTable(): Promise<typeof import('jspdf-autotable')['default']> {
    return import('jspdf-autotable').then((m) => m.default);
  }

  async exportarActaOficial(config: {
    nombreArchivo: string;
    nombreInstitucion?: string;
    urlLogo?: string;
    aulaNombre: string;
    anioLectivo: number | string;
    numeroActa: string;
    estadoActa: string;
    fechaReunion: string;
    usuarioRegistro: string;
    tituloAsamblea: string;
    agendaAcuerdos: string;
    fechaEmision: Date;
  }): Promise<void> {
    const jsPDF = await this.cargarJsPDF();
    const doc = new jsPDF({
      orientation: 'portrait',
      unit: 'mm',
      format: 'a4'
    });

    const pageWidth = doc.internal.pageSize.getWidth(); // 210mm
    let startY = 15;

    // 1. Carga opcional de Logo en Base64 para jsPDF
    if (config.urlLogo) {
      try {
        const logoBase64 = await this.obtenerImagenBase64(config.urlLogo);
        if (logoBase64) {
          doc.addImage(logoBase64, 'PNG', 14, startY, 20, 20);
        }
      } catch {
        // Si falla la descarga del logo por red/CORS, continúa sin romper la generación
      }
    }

    const posXTexto = config.urlLogo ? 38 : 14;

    // 2. Cabecera Institucional
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(13);
    doc.setTextColor(15, 23, 42); // slate-900 (#0f172a)
    doc.text((config.nombreInstitucion || 'INSTITUCIÓN EDUCATIVA').toUpperCase(), posXTexto, startY + 5);

    // Subtítulo
    doc.setFontSize(10);
    doc.setFont('helvetica', 'bold');
    doc.setTextColor(30, 41, 59); // slate-800 (#1e293b)
    doc.text('ACTA OFICIAL DE ASAMBLEA DE PADRES DE FAMILIA', posXTexto, startY + 11);

    // Contexto Aula / Año
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(9);
    doc.setTextColor(51, 65, 85); // slate-700 (#334155)
    doc.text(`Comité de Aula - ${config.aulaNombre} | Año Lectivo ${config.anioLectivo}`, posXTexto, startY + 16);

    // Línea divisora superior
    startY += 22;
    doc.setDrawColor(15, 23, 42);
    doc.setLineWidth(0.6);
    doc.line(14, startY, pageWidth - 14, startY);
    startY += 6;

    // 3. Ficha de Datos del Acta (Recuadro gris)
    doc.setFillColor(248, 250, 252); // slate-50 (#f8fafc)
    doc.setDrawColor(203, 213, 225); // slate-300 (#cbd5e1)
    doc.roundedRect(14, startY, pageWidth - 28, 24, 2, 2, 'FD');

    // Fila 1 de la ficha
    const col1X = 18;
    const col2X = 110;

    // Código / N° de Acta
    doc.setFontSize(7.5);
    doc.setFont('helvetica', 'bold');
    doc.setTextColor(100, 116, 139); // slate-500
    doc.text('CÓDIGO / N° DE ACTA:', col1X, startY + 6);
    doc.setFontSize(10);
    doc.setFont('courier', 'bold');
    doc.setTextColor(15, 23, 42);
    doc.text(config.numeroActa, col1X, startY + 11);

    // Estado del Documento
    doc.setFontSize(7.5);
    doc.setFont('helvetica', 'bold');
    doc.setTextColor(100, 116, 139);
    doc.text('ESTADO DEL DOCUMENTO:', col2X, startY + 6);
    doc.setFontSize(9.5);
    doc.setFont('helvetica', 'bold');
    doc.setTextColor(4, 120, 87); // emerald-700 (#047857)
    doc.text(config.estadoActa.toUpperCase(), col2X, startY + 11);

    // Fila 2 de la ficha
    // Fecha de Celebración
    doc.setFontSize(7.5);
    doc.setFont('helvetica', 'bold');
    doc.setTextColor(100, 116, 139);
    doc.text('FECHA DE CELEBRACIÓN:', col1X, startY + 17);
    doc.setFontSize(9);
    doc.setFont('helvetica', 'normal');
    doc.setTextColor(30, 41, 59);
    doc.text(config.fechaReunion, col1X, startY + 21);

    // Registrado Por
    doc.setFontSize(7.5);
    doc.setFont('helvetica', 'bold');
    doc.setTextColor(100, 116, 139);
    doc.text('REGISTRADO POR:', col2X, startY + 17);
    doc.setFontSize(9);
    doc.setFont('helvetica', 'normal');
    doc.setTextColor(30, 41, 59);
    doc.text(config.usuarioRegistro, col2X, startY + 21);

    startY += 30;

    // 4. Asunto / Agenda Principal
    doc.setFontSize(8);
    doc.setFont('helvetica', 'bold');
    doc.setTextColor(100, 116, 139);
    doc.text('AGENDA / ASUNTO PRINCIPAL:', 14, startY);

    startY += 5;
    doc.setFontSize(11);
    doc.setFont('helvetica', 'bold');
    doc.setTextColor(15, 23, 42);
    const tituloLineas = doc.splitTextToSize(config.tituloAsamblea.toUpperCase(), pageWidth - 28);
    doc.text(tituloLineas, 14, startY);

    startY += (tituloLineas.length * 5) + 6;

    // 5. Contenedor de Puntos Tratados y Acuerdos
    doc.setFillColor(255, 255, 255);
    doc.setDrawColor(203, 213, 225);

    // Título de la caja de acuerdos
    doc.setFontSize(8.5);
    doc.setFont('helvetica', 'bold');
    doc.setTextColor(15, 23, 42);
    doc.text('PUNTOS TRATADOS Y ACUERDOS APROBADOS POR LA ASAMBLEA:', 14, startY);

    startY += 3;
    doc.setDrawColor(226, 232, 240); // slate-200
    doc.line(14, startY, pageWidth - 14, startY);
    startY += 5;

    // Texto de la agenda / acuerdos
    doc.setFontSize(9);
    doc.setFont('helvetica', 'normal');
    doc.setTextColor(30, 41, 59);

    const acuerdosLineas = doc.splitTextToSize(config.agendaAcuerdos, pageWidth - 32);
    const altoCajaTextos = Math.max((acuerdosLineas.length * 4.5) + 10, 50);

    // Dibujamos el recuadro que engloba los acuerdos
    doc.roundedRect(14, startY - 5, pageWidth - 28, altoCajaTextos, 2, 2, 'S');
    doc.text(acuerdosLineas, 18, startY + 2);

    // 6. Pie de página oficial
    doc.setFontSize(7.5);
    doc.setFont('helvetica', 'normal');
    doc.setTextColor(148, 163, 184); // slate-400

    const fechaFormateada = config.fechaEmision.toLocaleString('es-PE', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });

    const pieTexto = `Documento emitido desde el Sistema de Comité de Aula (SCA) - Fecha de impresión: ${fechaFormateada}`;
    doc.text(pieTexto, pageWidth / 2, doc.internal.pageSize.getHeight() - 8, { align: 'center' });

    // Guardado del archivo PDF
    doc.save(config.nombreArchivo);
  }

  async exportarReporteMorosos(config: {
    nombreArchivo: string;
    nombreInstitucion?: string;
    urlLogo?: string;
    conceptoCuota: string;
    montoCuota: number;
    totalPendientes: number;
    estudiantes: {
      nombreEstudiante: string;
      documentoEstudiante: string;
      nombreApoderado: string;
      telefonoApoderado: string;
      montoPendiente: number;
    }[];
    fechaEmision: Date;
  }): Promise<void> {
    const jsPDF = await this.cargarJsPDF();
    const autoTable = await this.cargarAutoTable();
    const doc = new jsPDF({
      orientation: 'portrait',
      unit: 'mm',
      format: 'a4'
    });

    const pageWidth = doc.internal.pageSize.getWidth(); // 210mm
    let startY = 15;

    // 1. Carga opcional de Logo en Base64
    if (config.urlLogo) {
      try {
        const logoBase64 = await this.obtenerImagenBase64(config.urlLogo);
        if (logoBase64) {
          doc.addImage(logoBase64, 'PNG', 14, startY, 18, 18);
        }
      } catch {
        // Omite el logo en caso de error de red
      }
    }

    const posXTexto = config.urlLogo ? 36 : 14;

    // 2. Cabecera Institucional
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.setTextColor(15, 23, 42); // slate-900 (#0f172a)
    doc.text((config.nombreInstitucion || 'INSTITUCIÓN EDUCATIVA').toUpperCase(), posXTexto, startY + 4);

    // Subtítulo del Reporte (Color rojo/rose-700)
    doc.setFontSize(9.5);
    doc.setFont('helvetica', 'bold');
    doc.setTextColor(190, 18, 60); // rose-700 (#be123c)
    doc.text('REPORTE OFICIAL DE MOROSIDAD / ESTUDIANTES PENDIENTES', posXTexto, startY + 9.5);

    // MetaInfo
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(8);
    doc.setTextColor(100, 116, 139); // slate-500 (#64748b)

    const fechaFormateada = config.fechaEmision.toLocaleString('es-PE', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });

    doc.text(`Cuota: ${config.conceptoCuota} | Fecha de Emisión: ${fechaFormateada}`, posXTexto, startY + 14.5);

    // Línea divisora
    startY += 19;
    doc.setDrawColor(15, 23, 42);
    doc.setLineWidth(0.5);
    doc.line(14, startY, pageWidth - 14, startY);
    startY += 5;

    // 3. Tarjeta de Totales (Recuadro de métricas)
    doc.setFillColor(248, 250, 252); // slate-50
    doc.setDrawColor(203, 213, 225); // slate-300
    doc.roundedRect(14, startY, pageWidth - 28, 14, 2, 2, 'FD');

    const colWidth = (pageWidth - 28) / 2;

    // Total Apoderados Pendientes
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(7.5);
    doc.setTextColor(100, 116, 139);
    doc.text('TOTAL APODERADOS PENDIENTES:', 18, startY + 5);
    doc.setFontSize(9.5);
    doc.setTextColor(190, 18, 60); // rose-700
    doc.text(`${config.totalPendientes} Estudiantes`, 18, startY + 10.5);

    // Monto Individual de Cuota
    doc.setFontSize(7.5);
    doc.setTextColor(100, 116, 139);
    doc.text('MONTO INDIVIDUAL DE CUOTA:', 18 + colWidth, startY + 5);
    doc.setFontSize(9.5);
    doc.setTextColor(15, 23, 42); // slate-900
    doc.text(`S/. ${config.montoCuota.toFixed(2)}`, 18 + colWidth, startY + 10.5);

    startY += 18;

    // 4. Tabla de Estudiantes Morosos mediante AutoTable
    const filasTabla = config.estudiantes.map(e => [
      `${e.nombreEstudiante}\nDoc: ${e.documentoEstudiante}`,
      e.nombreApoderado,
      e.telefonoApoderado,
      `S/. ${e.montoPendiente.toFixed(2)}`
    ]);

    autoTable(doc, {
      startY: startY,
      margin: { left: 14, right: 14, bottom: 15 },
      head: [['Estudiante', 'Apoderado Responsable', 'Teléfono', 'Pendiente (S/.)']],
      body: filasTabla,
      styles: {
        font: 'helvetica',
        fontSize: 8,
        cellPadding: 2.5,
        textColor: [51, 65, 85], // slate-700
        lineColor: [226, 232, 240], // slate-200
        lineWidth: 0.15
      },
      headStyles: {
        fillColor: [241, 245, 249], // slate-100
        textColor: [15, 23, 42], // slate-900
        fontStyle: 'bold',
        lineWidth: 0.25,
        lineColor: [203, 213, 225]
      },
      columnStyles: {
        0: { cellWidth: 65, halign: 'left', fontStyle: 'bold' },
        1: { halign: 'left' },
        2: { cellWidth: 30, halign: 'center' },
        3: { cellWidth: 32, halign: 'right', fontStyle: 'bold', textColor: [190, 18, 60] }
      },
      didDrawPage: (data) => {
        // Pie de página
        const pageCount = doc.internal.pages.length - 1;
        doc.setFontSize(7.5);
        doc.setTextColor(148, 163, 184); // slate-400
        const pieTexto = `Documento Oficial de Tesorería emitido desde el Sistema de Comité de Aula - Página ${data.pageNumber} de ${pageCount}`;
        doc.text(pieTexto, pageWidth / 2, doc.internal.pageSize.getHeight() - 7, { align: 'center' });
      }
    });

    // 5. Guardar documento
    doc.save(config.nombreArchivo);
  }

  async exportarRendicionCaja(config: {
    nombreArchivo: string;
    nombreInstitucion?: string;
    urlLogo?: string;
    aulaNombre: string;
    anioLectivo: number | string;
    periodoTexto: string; // Ej: "Marzo" o "Todo el Año (Acumulado)"
    fechaEmision: Date;
    cuadro1: {
      saldoAnterior: number;
      totalIngresosMes: number;
      totalEgresosMes: number;
      saldoNeto: number;
    };
    cuadro2: {
      ingresosMensuales: number;
      ingresosExtraordinarios: number;
      ingresosDonaciones: number;
    };
    cuadro3Gastos: {
      categoria: string;
      concepto: string;
      proveedor?: string;
      tipoComprobante: string;
      numeroComprobante?: string;
      monto: number;
    }[];
  }): Promise<void> {
    const jsPDF = await this.cargarJsPDF();
    const autoTable = await this.cargarAutoTable();
    const doc = new jsPDF({
      orientation: 'portrait',
      unit: 'mm',
      format: 'a4'
    });

    const pageWidth = doc.internal.pageSize.getWidth(); // 210mm
    let startY = 15;

    // 1. Logo opcional
    if (config.urlLogo) {
      try {
        const logoBase64 = await this.obtenerImagenBase64(config.urlLogo);
        if (logoBase64) {
          doc.addImage(logoBase64, 'PNG', 14, startY, 18, 18);
        }
      } catch {
        // En caso de fallo de carga de logo continúa sin interrumpir
      }
    }

    const posXTexto = config.urlLogo ? 36 : 14;

    // 2. Cabecera Institucional
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.setTextColor(15, 23, 42); // #0f172a
    doc.text((config.nombreInstitucion || 'INSTITUCIÓN EDUCATIVA').toUpperCase(), posXTexto, startY + 4);

    // Subtítulo del Reporte
    doc.setFontSize(9);
    doc.setFont('helvetica', 'bold');
    doc.setTextColor(30, 41, 59); // #1e293b
    const subtitulo = `INFORME DE RENDICIÓN DE CUENTAS DE CAJA - COMITÉ DE AULA ${config.aulaNombre.toUpperCase()}`;
    doc.text(subtitulo, posXTexto, startY + 9.5);

    // MetaInfo
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(8);
    doc.setTextColor(100, 116, 139); // #64748b

    const fechaFormateada = config.fechaEmision.toLocaleString('es-PE', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });

    doc.text(`Año Lectivo ${config.anioLectivo} | Periodo: ${config.periodoTexto} | Fecha de Emisión: ${fechaFormateada}`, posXTexto, startY + 14.5);

    // Línea divisora
    startY += 19;
    doc.setDrawColor(15, 23, 42);
    doc.setLineWidth(0.5);
    doc.line(14, startY, pageWidth - 14, startY);
    startY += 6;

    // 3. CUADRO I: CUADRO GENERAL DE CAJA (RESUMEN EJECUTIVO)
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(8.5);
    doc.setTextColor(15, 23, 42);
    doc.text('I. CUADRO GENERAL DE CAJA (RESUMEN EJECUTIVO)', 14, startY);
    startY += 2;

    autoTable(doc, {
      startY: startY,
      margin: { left: 14, right: 14 },
      head: [['Concepto / Movimiento', 'Monto (S/.)']],
      body: [
        ['1. Saldo Inicial Arrastrado del Mes Anterior', `S/. ${config.cuadro1.saldoAnterior.toFixed(2)}`],
        ['(+) Total Ingresos Recaudados en el Mes', `+ S/. ${config.cuadro1.totalIngresosMes.toFixed(2)}`],
        ['(-) Total Egresos y Gastos del Mes', `- S/. ${config.cuadro1.totalEgresosMes.toFixed(2)}`],
        ['(=) SALDO NETO DISPONIBLE EN CAJA AL CIERRE', `S/. ${config.cuadro1.saldoNeto.toFixed(2)}`]
      ],
      styles: {
        font: 'helvetica',
        fontSize: 8,
        cellPadding: 2,
        textColor: [51, 65, 85],
        lineColor: [226, 232, 240],
        lineWidth: 0.15
      },
      headStyles: {
        fillColor: [241, 245, 249],
        textColor: [15, 23, 42],
        fontStyle: 'bold',
        lineWidth: 0.2
      },
      columnStyles: {
        0: { halign: 'left' },
        1: { cellWidth: 45, halign: 'right', fontStyle: 'bold' }
      },
      didParseCell: (data) => {
        // Estilos específicos de colores por fila
        if (data.section === 'body') {
          if (data.row.index === 1) {
            data.cell.styles.textColor = [4, 120, 87]; // emerald-700
          } else if (data.row.index === 2) {
            data.cell.styles.textColor = [190, 18, 60]; // rose-700
          } else if (data.row.index === 3) {
            data.cell.styles.fillColor = [248, 250, 252];
            data.cell.styles.fontStyle = 'bold';
            data.cell.styles.textColor = [49, 46, 129]; // indigo-900
          }
        }
      }
    });

    startY = ((doc as JsPDFConAutoTable).lastAutoTable?.finalY ?? startY) + 7;

    // 4. CUADRO II: DESGLOSE DE INGRESOS RECAUDADOS
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(8.5);
    doc.setTextColor(15, 23, 42);
    doc.text('II. DESGLOSE DE INGRESOS RECAUDADOS', 14, startY);
    startY += 2;

    autoTable(doc, {
      startY: startY,
      margin: { left: 14, right: 14 },
      head: [['Tipo de Aporte', 'Monto Recaudado (S/.)']],
      body: [
        ['Caja Chica Mensual (Aportes Ordinarios)', `S/. ${config.cuadro2.ingresosMensuales.toFixed(2)}`],
        ['Cuotas Extraordinarias / Actividades', `S/. ${config.cuadro2.ingresosExtraordinarios.toFixed(2)}`],
        ['Donaciones y Aportes Voluntarios', `S/. ${config.cuadro2.ingresosDonaciones.toFixed(2)}`]
      ],
      styles: {
        font: 'helvetica',
        fontSize: 8,
        cellPadding: 2,
        textColor: [51, 65, 85],
        lineColor: [226, 232, 240],
        lineWidth: 0.15
      },
      headStyles: {
        fillColor: [241, 245, 249],
        textColor: [15, 23, 42],
        fontStyle: 'bold',
        lineWidth: 0.2
      },
      columnStyles: {
        0: { halign: 'left' },
        1: { cellWidth: 45, halign: 'right', fontStyle: 'bold' }
      }
    });

    startY = ((doc as JsPDFConAutoTable).lastAutoTable?.finalY ?? startY) + 7;

    // 5. CUADRO III: DESGLOSE DETALLADO DE EGRESOS POR CATEGORÍA Y CONCEPTO
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(8.5);
    doc.setTextColor(15, 23, 42);
    doc.text('III. DESGLOSE DETALLADO DE EGRESOS POR CATEGORÍA Y CONCEPTO', 14, startY);
    startY += 2;

    const filasGastos = config.cuadro3Gastos.map(g => [
      g.categoria.toUpperCase(),
      `${g.concepto}${g.proveedor ? ' (' + g.proveedor + ')' : ''}`,
      `${g.tipoComprobante}${g.numeroComprobante ? ' N° ' + g.numeroComprobante : ''}`,
      `S/. ${g.monto.toFixed(2)}`
    ]);

    autoTable(doc, {
      startY: startY,
      margin: { left: 14, right: 14, bottom: 15 },
      head: [['Categoría', 'Concepto / Detalle de Gasto', 'Comprobante / N°', 'Monto (S/.)']],
      body: filasGastos.length > 0 ? filasGastos : [['No se registraron egresos en este periodo.', '', '', '']],
      foot: filasGastos.length > 0 ? [[
        { content: 'TOTAL EGRESOS EJECUTADOS:', colSpan: 3, styles: { halign: 'right', fontStyle: 'bold' } },
        { content: `S/. ${config.cuadro1.totalEgresosMes.toFixed(2)}`, styles: { halign: 'center', fontStyle: 'bold', textColor: [190, 18, 60] } }
      ]] : undefined,
      styles: {
        font: 'helvetica',
        fontSize: 7.5,
        cellPadding: 2,
        textColor: [51, 65, 85],
        lineColor: [203, 213, 225],
        lineWidth: 0.15,
        valign: 'middle'
      },
      headStyles: {
        fillColor: [241, 245, 249],
        textColor: [15, 23, 42],
        fontStyle: 'bold',
        halign: 'center',
        lineWidth: 0.25,
        lineColor: [203, 213, 225]
      },
      footStyles: {
        fillColor: [248, 250, 252],
        textColor: [15, 23, 42],
        lineWidth: 0.25,
        lineColor: [203, 213, 225]
      },
      columnStyles: {
        0: { cellWidth: 38, halign: 'center', fontStyle: 'bold' },
        1: { halign: 'left' },
        2: { cellWidth: 40, halign: 'center', textColor: [100, 116, 139] },
        3: { cellWidth: 28, halign: 'center', fontStyle: 'bold', textColor: [190, 18, 60] }
      },
      didDrawPage: (data) => {
        // Pie de página oficial en todas las hojas
        const pageCount = doc.internal.pages.length - 1;
        doc.setFontSize(7.5);
        doc.setTextColor(148, 163, 184);
        const pieTexto = `Informe de Rendición de Cuentas de Caja emitido desde el Sistema de Comité de Aula - Página ${data.pageNumber} de ${pageCount}`;
        doc.text(pieTexto, pageWidth / 2, doc.internal.pageSize.getHeight() - 7, { align: 'center' });
      }
    });

    // 6. Guardar archivo
    doc.save(config.nombreArchivo);
  }

  async exportarResumenGeneralCajas(config: {
    nombreArchivo: string;
    nombreInstitucion?: string;
    urlLogo?: string;
    anioLectivo: number | string;
    totalIngresos: number;
    totalEgresos: number;
    saldoNeto: number;
    aulas: {
      nombreAula: string;
      nivel: string;
      totalIngresos: number;
      totalEgresos: number;
      saldoNeto: number;
    }[];
    fechaEmision: Date;
  }): Promise<void> {
    const jsPDF = await this.cargarJsPDF();
    const autoTable = await this.cargarAutoTable();
    const doc = new jsPDF({
      orientation: 'portrait',
      unit: 'mm',
      format: 'a4'
    });

    const pageWidth = doc.internal.pageSize.getWidth(); // 210mm
    let startY = 15;

    // 1. Carga opcional de Logo en Base64
    if (config.urlLogo) {
      try {
        const logoBase64 = await this.obtenerImagenBase64(config.urlLogo);
        if (logoBase64) {
          doc.addImage(logoBase64, 'PNG', 14, startY, 18, 18);
        }
      } catch {
        // En caso de fallo de red en la imagen, continúa sin interrumpir
      }
    }

    const posXTexto = config.urlLogo ? 36 : 14;

    // 2. Cabecera Institucional
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.setTextColor(15, 23, 42); // slate-900 (#0f172a)
    doc.text((config.nombreInstitucion || 'INSTITUCIÓN EDUCATIVA').toUpperCase(), posXTexto, startY + 4);

    // Subtítulo del Reporte
    doc.setFontSize(9.5);
    doc.setFont('helvetica', 'bold');
    doc.setTextColor(71, 85, 105); // slate-600 (#475569)
    doc.text('CONSOLIDADO GENERAL DE AUDITORÍA FINANCIERA DE CAJAS', posXTexto, startY + 9.5);

    // MetaInfo
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(8);
    doc.setTextColor(100, 116, 139); // slate-500 (#64748b)

    const fechaFormateada = config.fechaEmision.toLocaleString('es-PE', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });

    doc.text(`Año Lectivo ${config.anioLectivo} | Fecha de Emisión: ${fechaFormateada}`, posXTexto, startY + 14.5);

    // Línea divisora
    startY += 19;
    doc.setDrawColor(15, 23, 42);
    doc.setLineWidth(0.5);
    doc.line(14, startY, pageWidth - 14, startY);
    startY += 5;

    // 3. Tarjeta de Totales Institucionales (3 Columnas)
    doc.setFillColor(248, 250, 252); // slate-50
    doc.setDrawColor(203, 213, 225); // slate-300
    doc.roundedRect(14, startY, pageWidth - 28, 14, 2, 2, 'FD');

    const colWidth = (pageWidth - 28) / 3;

    // Total Recaudado Institucional
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(7);
    doc.setTextColor(100, 116, 139);
    doc.text('TOTAL RECAUDADO INSTITUCIONAL:', 17, startY + 4.5);
    doc.setFontSize(9.5);
    doc.setTextColor(4, 120, 87); // emerald-700
    doc.text(`S/. ${config.totalIngresos.toFixed(2)}`, 17, startY + 10);

    // Total Egresos Ejecutados
    doc.setFontSize(7);
    doc.setTextColor(100, 116, 139);
    doc.text('TOTAL EGRESOS EJECUTADOS:', 17 + colWidth, startY + 4.5);
    doc.setFontSize(9.5);
    doc.setTextColor(190, 18, 60); // rose-700
    doc.text(`S/. ${config.totalEgresos.toFixed(2)}`, 17 + colWidth, startY + 10);

    // Saldo Neto Disponible
    doc.setFontSize(7);
    doc.setTextColor(100, 116, 139);
    doc.text('SALDO NETO DISPONIBLE:', 17 + (colWidth * 2), startY + 4.5);
    doc.setFontSize(9.5);
    doc.setTextColor(30, 27, 75); // indigo-950
    doc.text(`S/. ${config.saldoNeto.toFixed(2)}`, 17 + (colWidth * 2), startY + 10);

    startY += 18;

    // 4. Tabla de Detalle por Aula mediante AutoTable
    const filasTabla = config.aulas.map(a => [
      a.nombreAula,
      a.nivel.toUpperCase(),
      `S/. ${a.totalIngresos.toFixed(2)}`,
      `S/. ${a.totalEgresos.toFixed(2)}`,
      `S/. ${a.saldoNeto.toFixed(2)}`
    ]);

    autoTable(doc, {
      startY: startY,
      margin: { left: 14, right: 14, bottom: 15 },
      head: [['Aula / Grado', 'Nivel', 'Ingresos (S/.)', 'Egresos (S/.)', 'Saldo Neto (S/.)']],
      body: filasTabla,
      styles: {
        font: 'helvetica',
        fontSize: 8,
        cellPadding: 2.5,
        textColor: [51, 65, 85], // slate-700
        lineColor: [226, 232, 240], // slate-200
        lineWidth: 0.15
      },
      headStyles: {
        fillColor: [241, 245, 249], // slate-100
        textColor: [15, 23, 42], // slate-900
        fontStyle: 'bold',
        lineWidth: 0.25,
        lineColor: [203, 213, 225]
      },
      columnStyles: {
        0: { halign: 'left', fontStyle: 'bold' },
        1: { cellWidth: 32, halign: 'left' },
        2: { cellWidth: 30, halign: 'right', fontStyle: 'bold', textColor: [4, 120, 87] },
        3: { cellWidth: 30, halign: 'right', fontStyle: 'bold', textColor: [190, 18, 60] },
        4: { cellWidth: 32, halign: 'right', fontStyle: 'bold', textColor: [15, 23, 42] }
      },
      didDrawPage: (data) => {
        // Pie de página oficial
        const pageCount = doc.internal.pages.length - 1;
        doc.setFontSize(7.5);
        doc.setTextColor(148, 163, 184); // slate-400
        const pieTexto = `Documento Oficial de Auditoría emitido desde el Sistema de Comité de Aula - Página ${data.pageNumber} de ${pageCount}`;
        doc.text(pieTexto, pageWidth / 2, doc.internal.pageSize.getHeight() - 7, { align: 'center' });
      }
    });

    // 5. Guardar archivo PDF
    doc.save(config.nombreArchivo);
  }

  /**
   * Convierte una URL de imagen a Base64 para ser embebida vectorialmente
   */
  private obtenerImagenBase64(url: string): Promise<string | null> {
    return new Promise((resolve) => {
      const img = new Image();
      img.crossOrigin = 'Anonymous';
      img.onload = () => {
        const canvas = document.createElement('canvas');
        canvas.width = img.width;
        canvas.height = img.height;
        const ctx = canvas.getContext('2d');
        if (ctx) {
          ctx.drawImage(img, 0, 0);
          resolve(canvas.toDataURL('image/png'));
        } else {
          resolve(null);
        }
      };
      img.onerror = () => resolve(null);
      img.src = url;
    });
  }
}
