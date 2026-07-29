export interface ComiteIntegrante {
  id: number;
  aulaId: number;
  usuarioIdSasi: string;
  nombreCompleto: string;
  email: string;
  cargo: string;
  estado: boolean;
  fechaAsignacion: string;
}

export interface UsuarioSasi {
  usuarioId: string;
  email: string;
  nombreCompleto: string;
  userName: string;
  rol: string;
}