export type EstadoPago = 'PENDIENTE' | 'PARCIAL' | 'COMPLETO';

export interface Cuota {
  id: number;
  aulaId: number;
  concepto: string;
  montoIndividual: number;
  fechaVencimiento: string;
  estado: 'EN COBRO' | 'CERRADA' | 'ANULADA';
  tipoCuota: 'EXTRAORDINARIA' | 'RECURRENTE_MENSUAL';
  observacion?: string;
  fechaCreacion: string;
  totalEstudiantesAsignados: number;
  totalMontoEsperado: number;
  totalMontoRecaudado: number;
  estudiantesAlDia: number;
  estudiantesPendientes: number;
  estudiantesExonerados?: number;
}

export interface CreateCuotaCommand {
  aulaId: number;
  concepto: string;
  montoIndividual: number;
  fechaVencimiento: string;
  observacion?: string;
}

export interface GenerarCuotasMensualesCommand {
  aulaId: number;
  conceptoBase: string;
  montoMensual: number;
  mesInicio: number;
  diaVencimiento: number;
  anioLectivo: number;
}

export interface CuotaEstudianteCobro {
  cuotaDetalleId: number;
  cuotaId: number;
  estudianteId: number;
  estudianteNombreCompleto: string;
  estudianteDocumento: string;
  nombreApoderado: string;
  telefonoApoderado: string;
  montoAsignado: number;
  montoPagado: number;
  estadoPago: EstadoPago;
  fechaUltimoPago?: string;
}

export interface RegistrarPagoManualCommand {
  cuotaDetalleId: number;
  montoAbonado: number;
  formaPago: string;
}

export interface EstudiantePendienteCuota {
  cuotaDetalleId: number;
  estudianteId: number;
  tipoDocumento: string;
  numeroDocumento: string;
  nombreEstudiante: string;
  nombreApoderado: string;
  telefonoApoderado: string;
  montoAsignado: number;
  montoPagado: number;
  montoPendiente: number;
  estadoPago: EstadoPago;
}

export interface EstudianteExoneradoCuota {
  cuotaDetalleId: number;
  estudianteId: number;
  tipoDocumento: string;
  numeroDocumento: string;
  nombreEstudiante: string;
  nombreApoderado: string;
  telefonoApoderado: string;
  montoAsignado: number;
  motivoExoneracion: string;
  fechaExoneracion?: string;
}