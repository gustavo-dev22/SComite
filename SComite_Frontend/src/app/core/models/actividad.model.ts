export type EstadoActividad = 'PLANIFICADA' | 'EN_PROCESO' | 'FINALIZADA' | 'CANCELADA';

export interface ActividadComite {
  id: number;
  aulaId: number;
  nombreActividad: string;
  descripcion?: string;
  fechaProgramada: string;
  montoPresupuestado: number;
  cuotaSugeridaPorAlumno: number;
  estado: EstadoActividad;
  fechaRegistro?: string;
}