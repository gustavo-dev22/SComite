export interface ApiResponse {
  id?: number;
  exito?: boolean;
  mensaje?: string;
  fechaActualizacion?: string;
  usuarioActualizacion?: string;
}

export interface ResultadoCargaMasivaEstudiantes {
  registrosProcesados: number;
  registrosInsertados: number;
  registrosOmitidos: number;
  detallesObservaciones: string[];
}
