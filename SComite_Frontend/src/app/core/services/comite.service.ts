import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';
import { ComiteIntegrante, UsuarioSasi } from '../models/comiteIntegrante.model';

@Injectable({
  providedIn: 'root',
})
export class ComiteService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/Comite`;

  getComitePorAula(aulaId: number): Observable<ComiteIntegrante[]> {
    return this.http.get<ComiteIntegrante[]>(`${this.apiUrl}/aula/${aulaId}`);
  }

  getApoderadosSasi(): Observable<UsuarioSasi[]> {
    return this.http.get<UsuarioSasi[]>(`${this.apiUrl}/apoderados-sasi`);
  }

  asignarIntegrante(data: { aulaId: number; usuarioIdSasi: string; nombreCompleto: string; email: string; cargo: string }): Observable<any> {
    return this.http.post(this.apiUrl, data);
  }

  eliminarIntegrante(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}
