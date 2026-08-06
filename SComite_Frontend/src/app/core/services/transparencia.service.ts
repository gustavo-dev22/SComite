import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BalanceAula } from '../models/gastoTransparencia.model';

@Injectable({
  providedIn: 'root',
})
export class TransparenciaService {
  private apiUrl = `${environment.apiUrl}/transparencia`;

  constructor(private http: HttpClient) {}

  getBalanceAula(aulaId: number, anio: number): Observable<BalanceAula> {
    return this.http.get<BalanceAula>(`${this.apiUrl}/aula/${aulaId}/balance?anio=${anio}`);
  }
}
