import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, MenuItemNode, MenuObjeto } from '../models/sasi.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
    private http = inject(HttpClient);
    private router = inject(Router);
    private apiUrl = `${environment.apiUrl}/Auth`;

    usuarioActual = signal<string | null>(sessionStorage.getItem('usuario_nombre'));
    rolActual = signal<string | null>(sessionStorage.getItem('usuario_rol'));
    menuSesion = signal<MenuObjeto[]>(this.obtenerMenuStorage());

    menuJerarquico = computed<MenuItemNode[]>(() => {
        const lista = this.menuSesion().filter(o => o.activo);
        const padres = lista
        .filter(o => o.tipo === 'Menu' && o.idPadre === null)
        .sort((a, b) => a.orden - b.orden);

        return padres.map(padre => {
        const hijos = lista
            .filter(o => o.idPadre === padre.idObjeto)
            .sort((a, b) => a.orden - b.orden);

        return {
            ...padre,
            submenus: hijos
        };
        });
    });

    login(credentials: { userName: string; password: string }): Observable<AuthResponse> {
        return this.http.post<AuthResponse>(`${this.apiUrl}/login`, credentials).pipe(
            tap(res => {
                if (res.exito) {
                    sessionStorage.setItem('token_aula', res.token);
                    sessionStorage.setItem('usuario_nombre', res.nombreUsuario);

                    const nombreRol = res.sistemaComite.roles[0]?.nombreRol || 'APODERADO';
                    const rolEnMayusculas = nombreRol.toUpperCase();
                    sessionStorage.setItem('usuario_rol', rolEnMayusculas);
                    
                    // Extraer y armar menú del rol asignado
                    const objetosMenu = res.sistemaComite.roles[0]?.objetos || [];
                    sessionStorage.setItem('menu_aula', JSON.stringify(objetosMenu));
                    
                    // Actualizar Signals
                    this.usuarioActual.set(res.nombreUsuario);
                    this.rolActual.set(rolEnMayusculas);
                    this.menuSesion.set(objetosMenu);
                }
            })
        );
    }

    logout(): void {
        sessionStorage.removeItem('token_aula');
        sessionStorage.removeItem('usuario_nombre');
        sessionStorage.removeItem('usuario_rol');
        sessionStorage.removeItem('menu_aula');
        this.usuarioActual.set(null);
        this.menuSesion.set([]);
        this.router.navigate(['/login']);
    }

    isAuthenticated(): boolean {
        return !!sessionStorage.getItem('token_aula');
    }

    private obtenerMenuStorage(): MenuObjeto[] {
        const raw = sessionStorage.getItem('menu_aula');
        return raw ? JSON.parse(raw) : [];
    }
}