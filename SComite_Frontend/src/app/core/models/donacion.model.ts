export interface DonacionComite {
  id: number;
  aulaId: number;
  donante: string;
  monto: number;
  fechaDonacion: string;
  concepto: string;
  observacion?: string;
  fechaRegistro?: string;
}