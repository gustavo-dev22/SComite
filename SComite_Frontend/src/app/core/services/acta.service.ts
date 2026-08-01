import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';
import { ActaAsambleaComite } from '../models/acta.model';

@Injectable({
  providedIn: 'root',
})
export class ActaService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/actasAsamblea`;

  getActasPorAula(aulaId: number, anio: number): Observable<ActaAsambleaComite[]> {
    return this.http.get<ActaAsambleaComite[]>(`${this.apiUrl}/aula/${aulaId}?anio=${anio}`);   
  }    
  
  guardarActa(acta: Partial<ActaAsambleaComite>): Observable<{ id: number; message: string }> {     
    return this.http.post<{ id: number; message: string }>(this.apiUrl, acta);   
  }
  
  eliminarActa(id: number, aulaId: number): Observable<{ message: string }> {     
    return this.http.delete<{ message: string }>(`${this.apiUrl}/${id}/aula/${aulaId}`);
  }

  getSiguienteNumeroActa(aulaId: number, anio: number): Observable<{ siguienteNumeroActa: string }> {
    return this.http.get<{ siguienteNumeroActa: string }>(`${this.apiUrl}/aula/${aulaId}/siguiente-numero?anio=${anio}`);
  }
}
