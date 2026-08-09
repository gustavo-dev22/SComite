export type EstadoActa = 'BORRADOR' | 'APROBADA';

export interface ActaAsambleaComite {
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
  usuarioActualizacion?: string; 
  fechaActualizacion?: string;
  estado: boolean;
}