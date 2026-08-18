import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';
import { Estudiante, RespuestaMigracionApi } from '../models/estudiante.model';
import { ApiResponse, ResultadoCargaMasivaEstudiantes } from '../models/api-response.model';

@Injectable({
  providedIn: 'root',
})
export class EstudianteService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/Estudiantes`;

  getEstudiantesPorAula(aulaId: number): Observable<Estudiante[]> {
    return this.http.get<Estudiante[]>(`${this.apiUrl}/aula/${aulaId}`);
  }

  getEstudiante(id: number): Observable<Estudiante> {
    return this.http.get<Estudiante>(`${this.apiUrl}/${id}`);
  }

  crearEstudiante(estudiante: Partial<Estudiante>): Observable<ApiResponse> {
    return this.http.post<ApiResponse>(this.apiUrl, estudiante);
  }

  actualizarEstudiante(id: number, estudiante: Partial<Estudiante>): Observable<ApiResponse> {
    return this.http.put<ApiResponse>(`${this.apiUrl}/${id}`, estudiante);
  }

  eliminarEstudiante(id: number): Observable<ApiResponse> {
    return this.http.delete<ApiResponse>(`${this.apiUrl}/${id}`);
  }

  cargaMasiva(aulaId: number, estudiantes: Partial<Estudiante>[]): Observable<ResultadoCargaMasivaEstudiantes> {
    return this.http.post<ResultadoCargaMasivaEstudiantes>(`${this.apiUrl}/carga-masiva`, { aulaId, estudiantes });
  }

  migrarEstudiantes(payload: { aulaDestinoId: number; estudianteIds: number[] }): Observable<RespuestaMigracionApi> {
    return this.http.post<RespuestaMigracionApi>(`${this.apiUrl}/migrar`, payload);
  }
}
