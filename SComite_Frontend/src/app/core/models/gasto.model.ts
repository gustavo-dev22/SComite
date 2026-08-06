export interface GastoComite {
  id: number;
  aulaId: number;
  concepto: string;
  categoria: string;
  monto: number;
  fechaGasto: string;
  tipoComprobante: string;
  numeroComprobante?: string;
  proveedor?: string;
  observacion?: string;
  urlComprobante?: string;
  usuarioRegistro: string;
  fechaRegistro: string;
}

export interface ResumenCajaAula {
  saldoAnteriorArrastrado: number;
  ingresosDelMes: number;
  montoDonacionesMes?: number;
  egresosDelMes: number;
  saldoDisponibleReal: number;
}

export interface CreateGastoCommand {
  aulaId: number;
  concepto: string;
  categoria: string;
  monto: number;
  fechaGasto: string;
  tipoComprobante: string;
  numeroComprobante?: string;
  proveedor?: string;
  observacion?: string;
}