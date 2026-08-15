import { EstadoPago } from './cuota.model';
import { EstadoActa } from './acta.model';
import { EstadoActividad } from './actividad.model';

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
}

export interface CuotaApoderado {
  cuotaId: number;
  cuotaDetalleId?: number;
  concepto: string;
  tipoCuota: string;
  montoTotalCuota: number;
  fechaVencimiento: string;
  montoPagado: number;
  montoPendiente: number;
  estadoPago: EstadoPago;
  estadoVisual: 'PAGADO' | 'VENCIDO' | 'PENDIENTE' | 'EXONERADO';
  fechaPago?: string;
  motivoExoneracion?: string;
  fechaUltimoPago?: string;
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
  estado: EstadoActividad;
}

export interface ActaApoderado {
  id: number;
  aulaId: number;
  numeroActa: string;
  titulo: string;
  fechaReunion: string;
  agendaAcuerdos: string;
  estadoActa: EstadoActa;
  urlDocumentoPdf?: string;
  usuarioRegistro: string;
  fechaRegistro: string;
}