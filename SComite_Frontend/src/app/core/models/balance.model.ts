export interface BalanceConsolidado {
  saldoAnteriorArrastrado: number;
  ingresosMensuales: number;
  ingresosExtraordinarios: number;
  totalIngresosMes: number;
  totalEgresosMes: number;
  saldoNetoEnCaja: number;
  totalPorCobrar: number;
  porcentajeCumplimiento: number;
}

export interface GastoCategoriaResumen {
  categoria: string;
  totalMonto: number;
  cantidadRegistros: number;
}

export interface BalanceGeneralDTO {
  consolidado: BalanceConsolidado;
  gastosPorCategoria: GastoCategoriaResumen[];
  gastosDetalle: GastoComiteDTO[];
}

export interface GastoComiteDTO {
  id: number;
  fechaGasto: string;
  concepto: string;
  categoria: string;
  monto: number;
  tipoComprobante: string;
  numeroComprobante?: string;
  proveedor?: string;
}

export interface GastoDetalleAgrupado extends GastoComiteDTO {
  rowspan?: number;
  esPrimerItemDelGrupo?: boolean;
}