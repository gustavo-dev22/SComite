import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { PeriodoLectivo } from '../models/periodoLectivo.model';
import { ApiResponse } from '../models/api-response.model';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class PeriodoService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/Periodos`;

  crearPeriodo(periodo: Partial<PeriodoLectivo>): Observable<ApiResponse> {
    return this.http.post<ApiResponse>(`${environment.apiUrl}/Periodos`, periodo);
  }

  actualizarPeriodo(id: number, periodo: Partial<PeriodoLectivo>): Observable<ApiResponse> {
    return this.http.put<ApiResponse>(`${environment.apiUrl}/Periodos/${id}`, periodo);
  }
}
