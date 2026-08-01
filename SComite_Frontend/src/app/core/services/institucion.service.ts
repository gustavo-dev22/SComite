import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';
import { InstitucionEducativa } from '../models/institucion.model';

@Injectable({
  providedIn: 'root',
})
export class InstitucionService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/institucion`;

  getConfiguracion(): Observable<InstitucionEducativa> {
    return this.http.get<InstitucionEducativa>(this.apiUrl);
  }

  guardarConfiguracion(datos: Partial<InstitucionEducativa>): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(this.apiUrl, datos);
  }
}
