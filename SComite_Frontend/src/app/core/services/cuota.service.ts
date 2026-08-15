import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateCuotaCommand, Cuota, CuotaEstudianteCobro, EstudianteExoneradoCuota, EstudiantePendienteCuota, GenerarCuotasMensualesCommand, RegistrarPagoManualCommand } from '../models/cuota.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class CuotaService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/Cuotas`;

  obtenerPorAula(aulaId: number): Observable<Cuota[]> {
    return this.http.get<Cuota[]>(`${this.apiUrl}/aula/${aulaId}`);
  }

  crear(command: CreateCuotaCommand): Observable<{ id: number; mensaje: string }> {
    return this.http.post<{ id: number; mensaje: string }>(this.apiUrl, command);
  }

  programarMensual(command: GenerarCuotasMensualesCommand): Observable<{ exito: boolean; mensaje: string }> {
    return this.http.post<{ exito: boolean; mensaje: string }>(`${this.apiUrl}/programacion-mensual`, command);
  }

  obtenerCobrosPorCuota(cuotaId: number): Observable<CuotaEstudianteCobro[]> {
    return this.http.get<CuotaEstudianteCobro[]>(`${this.apiUrl}/${cuotaId}/cobros`);
  }

  registrarPagoManual(command: RegistrarPagoManualCommand): Observable<{ exito: boolean; mensaje: string }> {
    return this.http.post<{ exito: boolean; mensaje: string }>(`${this.apiUrl}/registrar-pago-manual`, command);
  }

  anularPago(cuotaDetalleId: number): Observable<{ exito: boolean; mensaje: string }> {
    return this.http.post<{ exito: boolean; mensaje: string }>(`${this.apiUrl}/anular-pago`, { cuotaDetalleId });
  }

  obtenerPendientesPorCuota(cuotaId: number): Observable<EstudiantePendienteCuota[]> {
    return this.http.get<EstudiantePendienteCuota[]>(`${this.apiUrl}/${cuotaId}/pendientes`);
  }

  exonerarEstudiante(payload: { cuotaDetalleId: number; nuevoEstado: string; motivoExoneracion?: string }): Observable<{ exito: boolean; mensaje: string }> {
    return this.http.post<{ exito: boolean; mensaje: string }>(`${this.apiUrl}/exonerar-estudiante`, payload);
  }

  obtenerExoneradosPorCuota(cuotaId: number): Observable<EstudianteExoneradoCuota[]> {
    return this.http.get<EstudianteExoneradoCuota[]>(`${this.apiUrl}/${cuotaId}/exonerados`);
  }

  cambiarEstadoCuota(payload: { cuotaId: number; nuevoEstado: 'CERRADA' | 'EN COBRO' }): Observable<{ exito: boolean; mensaje: string }> {
    return this.http.post<{ exito: boolean; mensaje: string }>(`${this.apiUrl}/cambiar-estado`, payload);
  }
}
