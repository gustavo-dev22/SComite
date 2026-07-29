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