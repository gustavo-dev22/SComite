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