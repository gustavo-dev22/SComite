import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';
import { CreateGastoCommand, GastoComite, ResumenCajaAula } from '../models/gasto.model';

@Injectable({
  providedIn: 'root',
})
export class GastoService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/Gastos`;

  obtenerPorAula(aulaId: number): Observable<GastoComite[]> {
    return this.http.get<GastoComite[]>(`${this.apiUrl}/aula/${aulaId}`);
  }

  obtenerResumenCaja(aulaId: number): Observable<ResumenCajaAula> {
    return this.http.get<ResumenCajaAula>(`${this.apiUrl}/aula/${aulaId}/resumen-caja`);
  }

  crear(command: CreateGastoCommand): Observable<{ id: number; mensaje: string }> {
    return this.http.post<{ id: number; mensaje: string }>(this.apiUrl, command);
  }

  actualizar(id: number, payload: any): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, payload);
  }

  eliminar(id: number): Observable<{ exito: boolean; mensaje: string }> {
    return this.http.delete<{ exito: boolean; mensaje: string }>(`${this.apiUrl}/${id}`);
  }

  subirArchivoComprobante(file: File): Observable<{ urlComprobante: string }> {
    const formData = new FormData();
    formData.append('archivo', file);
    return this.http.post<{ urlComprobante: string }>(`${this.apiUrl}/subir-comprobante`, formData);
  }

  obtenerBalanceMensual(aulaId: number, anioLectivo: number, mes?: number | null): Observable<ResumenCajaAula> {
    let url = `${this.apiUrl}/aula/${aulaId}/balance-mensual?anioLectivo=${anioLectivo}`;
    if (mes && mes > 0) {
      url += `&mes=${mes}`;
    }
    return this.http.get<ResumenCajaAula>(url);
  }
}
