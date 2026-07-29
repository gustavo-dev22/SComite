import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';
import { Estudiante } from '../models/estudiante.model';

@Injectable({
  providedIn: 'root',
})
export class EstudianteService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/Estudiantes`;

  getEstudiantesPorAula(aulaId: number): Observable<Estudiante[]> {
    return this.http.get<Estudiante[]>(`${this.apiUrl}/aula/${aulaId}`);
  }

  crearEstudiante(estudiante: Partial<Estudiante>): Observable<any> {
    return this.http.post(this.apiUrl, estudiante);
  }

  actualizarEstudiante(id: number, estudiante: Partial<Estudiante>): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, estudiante);
  }

  eliminarEstudiante(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}
