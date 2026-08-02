export interface HijoApoderado {
  estudianteId: number;
  nombreEstudiante: string;
  aulaId: number;
  nombreAula: string;
  nivel: string;
  grado: string;
  seccion: string;
  tesoreroNombre: string;
  tesoreroTelefono: string;
  numeroYapePlin: string;
}

export interface CuotaApoderado {
  cuotaId: number;
  concepto: string;
  tipoCuota: string;
  montoTotalCuota: number;
  fechaVencimiento: string;
  montoPagado: number;
  montoPendiente: number;
  estadoPago: string;
  estadoVisual: 'PAGADO' | 'VENCIDO' | 'PENDIENTE';
  fechaPago?: string;
}

export interface ResumenPagosApoderado {
  estudianteId: number;
  totalPendiente: number;
  totalPagado: number;
  cantidadVencidas: number;
  cuotas: CuotaApoderado[];
}

export interface AnuncioApoderado {
  id: number;
  aulaId: number;
  titulo: string;
  contenido: string;
  categoria: string;
  esFijado: boolean;
  urlAdjunto?: string;
  usuarioRegistro: string;
  fechaPublicacion: string;
  cantidadVistas: number;
  leido: boolean;
}

export interface EventoCronogramaApoderado {
  id: number;
  aulaId: number;
  nombreActividad: string;
  descripcion?: string;
  fechaProgramada: string;
  montoPresupuestado: number;
  cuotaSugeridaPorAlumno: number;
  estado: string; // PLANIFICADA, EN_PROCESO, FINALIZADA, CANCELADA
}