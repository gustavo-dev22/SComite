export interface ResumenCajaAula {
  aulaId: number;
  nivel: string;
  grado: string;
  seccion: string;
  nombreAula: string;
  totalIngresos: number;
  totalEgresos: number;
  saldoNeto: number;
  estadoFinanciero: 'AL_DIA' | 'SIN_MOVIMIENTO' | 'ALERTA_ROJO';
}

export interface ResumenGeneralCajasConsolidadas {
  totalIngresosInstitucional: number;
  totalEgresosInstitucional: number;
  saldoNetoInstitucional: number;
  totalAulas: number;
  aulasAlDia: number;
  aulasSinMovimiento: number;
  aulasEnAlerta: number;
  detalleAulas: ResumenCajaAula[];
}