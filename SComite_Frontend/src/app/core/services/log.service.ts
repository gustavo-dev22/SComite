import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { LogFiltros, LogSistema, PagedResult } from '../models/log.model';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class LogService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/Logs`;

  getLogs(filtros: LogFiltros): Observable<PagedResult<LogSistema>> {
    let params = new HttpParams()
      .set('pagina', filtros.pagina.toString())
      .set('tamanoPagina', filtros.tamanoPagina.toString());

    if (filtros.fechaInicio) params = params.set('fechaInicio', filtros.fechaInicio);
    if (filtros.fechaFin) params = params.set('fechaFin', filtros.fechaFin);
    if (filtros.nivel) params = params.set('nivel', filtros.nivel);
    if (filtros.modulo) params = params.set('modulo', filtros.modulo);
    if (filtros.busqueda) params = params.set('busqueda', filtros.busqueda);

    return this.http.get<PagedResult<LogSistema>>(this.apiUrl, { params });
  }
}
