export interface GastoTransparencia {
  id: number;
  fechaGasto: string;
  concepto: string;
  categoria: string;
  monto: number;
  proveedor?: string;
  tipoComprobante?: string;
  numeroComprobante?: string;
  urlComprobante?: string;
}

export interface BalanceMensual {
  anio: number;
  mesNum: number;
  nombreMes: string;
  totalIngresosMes: number;
  totalEgresosMes: number;
  saldoMes: number;
}

export interface BalanceAula {
  totalIngresos: number;
  totalEgresos: number;
  saldoDisponible: number;
  desgloseMensual: BalanceMensual[];
  egresos: GastoTransparencia[];
}