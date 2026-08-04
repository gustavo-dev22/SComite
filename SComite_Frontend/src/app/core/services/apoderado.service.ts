import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { ActaApoderado, AnuncioApoderado, EventoCronogramaApoderado, HijoApoderado, ResumenPagosApoderado } from '../models/apoderado.model';
import { ApiResponse } from '../models/api-response.model';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class ApoderadoService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/apoderado`;

  getMisHijos(anio: number): Observable<HijoApoderado[]> {
    return this.http.get<HijoApoderado[]>(`${this.apiUrl}/mis-hijos?anio=${anio}`);
  }

  getCuotasPendientes(estudianteId: number, anio: number): Observable<ResumenPagosApoderado> {
    return this.http.get<ResumenPagosApoderado>(`${this.apiUrl}/cuotas-pendientes?estudianteId=${estudianteId}&anio=${anio}`);
  }

  getAnunciosMuro(estudianteId: number, anio: number): Observable<AnuncioApoderado[]> {
    return this.http.get<AnuncioApoderado[]>(`${this.apiUrl}/anuncios-muro?estudianteId=${estudianteId}&anio=${anio}`);
  }

  marcarLecturaAnuncio(anuncioId: number, estudianteId: number): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.apiUrl}/marcar-lectura-anuncio`, { anuncioId, estudianteId });
  }

  getCronogramaEventos(estudianteId: number, anio: number): Observable<EventoCronogramaApoderado[]> {
    return this.http.get<EventoCronogramaApoderado[]>(`${this.apiUrl}/cronograma-eventos?estudianteId=${estudianteId}&anio=${anio}`);
  }

  getActasAprobadas(estudianteId: number, anio: number): Observable<ActaApoderado[]> {
    return this.http.get<ActaApoderado[]>(`${this.apiUrl}/actas-aprobadas?estudianteId=${estudianteId}&anio=${anio}`);
  }
}
