export interface Estudiante {
  id: number;
  aulaId: number;
  tipoDocumento: string;
  numeroDocumento: string;
  nombres: string;
  apellidoPaterno: string;
  apellidoMaterno: string;
  nombreCompleto?: string;
  usuarioIdApoderadoSasi?: string;
  nombreApoderado?: string;
  telefonoApoderado?: string;
  estado: boolean;
  fechaRegistro: string;
}

export interface ResultadoMigracion {
  solicitados: number;
  migrados: number;
  omitidos: number;
  detalles: { nombreCompleto: string; motivo: string }[];
}

export interface RespuestaMigracionApi {
  exito: boolean;
  mensaje: string;
  datos: ResultadoMigracion;
}