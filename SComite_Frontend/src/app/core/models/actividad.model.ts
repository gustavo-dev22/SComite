export interface ActividadComite {
  id: number;
  aulaId: number;
  nombreActividad: string;
  descripcion?: string;
  fechaProgramada: string;
  montoPresupuestado: number;
  cuotaSugeridaPorAlumno: number;
  estado: 'PLANIFICADA' | 'EN_PROCESO' | 'FINALIZADA' | 'CANCELADA';
  fechaRegistro?: string;
}