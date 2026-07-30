import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';
import { BalanceGeneralDTO } from '../models/balance.model';

@Injectable({
  providedIn: 'root',
})
export class BalanceService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/Balance`;

  obtenerConsolidado(aulaId: number, anioLectivo: number, mes?: number | null): Observable<BalanceGeneralDTO> {
    let url = `${this.apiUrl}/aula/${aulaId}?anioLectivo=${anioLectivo}`;
    if (mes !== undefined && mes !== null) {
      url += `&mes=${mes}`;
    }
    return this.http.get<BalanceGeneralDTO>(url);
  }
}
