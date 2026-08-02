export interface AnuncioComite {
  id: number;
  aulaId: number;
  titulo: string;
  contenido: string;
  categoria: 'URGENTE' | 'CITACION' | 'TESORERIA' | 'EVENTO' | 'INFORMATIVO';
  esFijado: boolean;
  urlAdjunto?: string;
  usuarioRegistro: string;
  fechaPublicacion: string;
  cantidadVistas: number;
  estado: boolean;
}

export interface AuditoriaLectura {
  estudianteId: number;
  nombreEstudiante: string;
  nombreApoderado: string;
  telefonoApoderado: string;
  leido: boolean;
  fechaLectura?: string;
}

export interface ResumenAuditoriaAnuncio {
  anuncioId: number;
  totalEstudiantesAula: number;
  totalLeidos: number;
  totalPendientes: number;
  lecturas: AuditoriaLectura[];
}