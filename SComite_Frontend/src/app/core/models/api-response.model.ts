export interface ApiResponse<T = unknown> {
  data?: T;
  exito?: boolean;
  mensaje?: string;
}

export interface ResultadoCargaMasivaEstudiantes {
  registrosProcesados: number;
  registrosInsertados: number;
  registrosOmitidos: number;
  detallesObservaciones: string[];
}