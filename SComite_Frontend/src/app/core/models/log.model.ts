export interface LogSistema {
  id: number;
  fecha: string;
  nivel: 'INFO' | 'WARNING' | 'ERROR' | 'CRITICAL';
  modulo: string;
  accion: string;
  usuario?: string;
  ip?: string;
  mensaje: string;
  detalleException?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalRegistros: number;
  paginaActual: number;
  totalPaginas: number;
}

export interface LogFiltros {
  fechaInicio?: string;
  fechaFin?: string;
  nivel?: string;
  modulo?: string;
  busqueda?: string;
  pagina: number;
  tamanoPagina: number;
}