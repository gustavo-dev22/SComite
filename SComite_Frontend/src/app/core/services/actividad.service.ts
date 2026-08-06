import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';
import { ActividadComite } from '../models/actividad.model';

@Injectable({
  providedIn: 'root',
})
export class ActividadService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/Actividades`;

  getActividadesPorAula(aulaId: number, anio: number): Observable<ActividadComite[]> {
    return this.http.get<ActividadComite[]>(`${this.apiUrl}/aula/${aulaId}?anio=${anio}`);
  }

  guardarActividad(actividad: Partial<ActividadComite>): Observable<{ id: number; mensaje: string }> {
    return this.http.post<{ id: number; mensaje: string }>(this.apiUrl, actividad);
  }

  eliminarActividad(id: number, aulaId: number): Observable<{ mensaje: string }> {
    return this.http.delete<{ mensaje: string }>(`${this.apiUrl}/${id}/aula/${aulaId}`);
  }
}
