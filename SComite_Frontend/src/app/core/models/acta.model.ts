export interface ActaAsambleaComite {
  id: number;
  aulaId: number;
  numeroActa: string;
  titulo: string;
  fechaReunion: string;
  agendaAcuerdos: string;
  estadoActa: 'BORRADOR' | 'APROBADA';
  urlDocumentoPdf?: string;
  usuarioRegistro: string;
  fechaRegistro: string;
  usuarioActualizacion?: string; 
  fechaActualizacion?: string;
  estado: boolean;
}