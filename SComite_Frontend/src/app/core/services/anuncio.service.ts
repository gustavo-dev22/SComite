import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';
import { AnuncioComite, ResumenAuditoriaAnuncio } from '../models/anuncio.model';

@Injectable({
  providedIn: 'root',
})
export class AnuncioService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/anuncios`;

  getAnunciosPorAula(aulaId: number, anio: number): Observable<AnuncioComite[]> {
    return this.http.get<AnuncioComite[]>(`${this.apiUrl}/aula/${aulaId}?anio=${anio}`);
  }

  guardarAnuncio(anuncio: Partial<AnuncioComite>): Observable<{ id: number; mensaje: string }> {
    return this.http.post<{ id: number; mensaje: string }>(this.apiUrl, anuncio);
  }

  eliminarAnuncio(id: number, aulaId: number): Observable<{ mensaje: string }> {
    return this.http.delete<{ mensaje: string }>(`${this.apiUrl}/${id}/aula/${aulaId}`);
  }

  getAuditoriaVistas(anuncioId: number): Observable<ResumenAuditoriaAnuncio> {
    return this.http.get<ResumenAuditoriaAnuncio>(`${this.apiUrl}/auditoria-vistas/${anuncioId}`);
  }
}
