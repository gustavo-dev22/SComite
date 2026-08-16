import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Observable, map } from 'rxjs';
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

  // Aulas del usuario logueado (comité/apoderado solo ven sus aulas; admin ve todas)
  getMisAulas(periodoId?: number): Observable<Aula[]> {
    const url = periodoId ? `${this.apiUrl}/mis-aulas?periodoId=${periodoId}` : `${this.apiUrl}/mis-aulas`;
    return this.http.get<Aula[]>(url);
  }

  getPeriodos(): Observable<PeriodoLectivo[]> {
    return this.http.get<PeriodoLectivo[]>(`${this.apiUrl}/periodos`);
  }

  getAnioLectivoVigente(): Observable<number> {
    return this.http.get<PeriodoLectivo[]>(`${this.apiUrl}/periodos`).pipe(
      map((periodos) => {
        const vigente = periodos.find(p => p.esActivo) ?? periodos[0];
        return vigente?.anio ?? new Date().getFullYear();
      })
    );
  }

  crearAula(aula: { periodoId: number; nivel: string; grado: string; seccion: string }): Observable<ApiResponse> {
    return this.http.post<ApiResponse>(this.apiUrl, aula);
  }

  actualizarAula(id: number, aula: { id: number; periodoId: number; nivel: string; grado: string; seccion: string }): Observable<ApiResponse> {
    return this.http.put<ApiResponse>(`${this.apiUrl}/${id}`, aula);
  }

  eliminarAula(id: number): Observable<ApiResponse> {
    return this.http.delete<ApiResponse>(`${this.apiUrl}/${id}`);
  }
}
