import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { PeriodoLectivo } from '../models/periodoLectivo.model';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class PeriodoService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/Periodos`;

  crearPeriodo(periodo: Partial<PeriodoLectivo>): Observable<any> {
    return this.http.post(`${environment.apiUrl}/Periodos`, periodo);
  }

  actualizarPeriodo(id: number, periodo: Partial<PeriodoLectivo>): Observable<any> {
    return this.http.put(`${environment.apiUrl}/Periodos/${id}`, periodo);
  }
}
