import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';
import { DonacionComite } from '../models/donacion.model';

@Injectable({
  providedIn: 'root',
})
export class DonacionService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/donaciones`;

  getDonacionesPorAula(aulaId: number, anio: number, mes: number = 0): Observable<DonacionComite[]> {
    return this.http.get<DonacionComite[]>(`${this.apiUrl}/aula/${aulaId}?anio=${anio}&mes=${mes}`);
  }

  guardarDonacion(donacion: Partial<DonacionComite>): Observable<{ id: number; mensaje: string }> {
    return this.http.post<{ id: number; mensaje: string }>(this.apiUrl, donacion);
  }

  eliminarDonacion(id: number, aulaId: number): Observable<{ mensaje: string }> {
    return this.http.delete<{ mensaje: string }>(`${this.apiUrl}/${id}/aula/${aulaId}`);
  }
}
