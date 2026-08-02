import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';
import { ResumenGeneralCajasConsolidadas } from '../models/auditoria.model';

@Injectable({
  providedIn: 'root',
})
export class AuditoriaService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/auditoria`;

  getResumenGeneralCajas(anio: number, nivel?: string): Observable<ResumenGeneralCajasConsolidadas> {
    let url = `${this.apiUrl}/resumen-cajas?anio=${anio}`;
    if (nivel) url += `&nivel=${encodeURIComponent(nivel)}`;
    return this.http.get<ResumenGeneralCajasConsolidadas>(url);
  }
}
