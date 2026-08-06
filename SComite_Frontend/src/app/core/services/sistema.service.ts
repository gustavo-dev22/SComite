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

  resetBaseDeDatos(confirmacionTexto: string): Observable<{ exito: boolean; mensaje: string }> {
    return this.http.post<{ exito: boolean; mensaje: string }>(`${this.apiUrl}/reset-database`, { confirmacionTexto });
  }

  descargarBackupManual(): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/descargar-backup`, { responseType: 'blob' });
  }
}
