import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class SistemaService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/sistema`;

  resetBaseDeDatos(confirmacionTexto: string): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(`${this.apiUrl}/reset-database`, { confirmacionTexto });
  }
}
