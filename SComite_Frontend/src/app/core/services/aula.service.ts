import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';
import { PeriodoLectivo } from '../models/periodoLectivo.model';
import { Aula } from '../models/aula.model';
import { ApiResponse } from '../models/api-response.model';

@Injectable({
  providedIn: 'root',
})
export class AulaService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/Aulas`;

  getAulas(periodoId?: number): Observable<Aula[]> {
    const url = periodoId ? `${this.apiUrl}?periodoId=${periodoId}` : this.apiUrl;
    return this.http.get<Aula[]>(url);
  }

  getPeriodos(): Observable<PeriodoLectivo[]> {
    return this.http.get<PeriodoLectivo[]>(`${this.apiUrl}/periodos`);
  }

  crearAula(aula: { periodoId: number; nivel: string; grado: string; seccion: string }): Observable<ApiResponse<Aula>> {
    return this.http.post<ApiResponse<Aula>>(this.apiUrl, aula);
  }

  actualizarAula(id: number, aula: { id: number; periodoId: number; nivel: string; grado: string; seccion: string }): Observable<ApiResponse<void>> {
    return this.http.put<ApiResponse<void>>(`${this.apiUrl}/${id}`, aula);
  }

  eliminarAula(id: number): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(`${this.apiUrl}/${id}`);
  }
}
