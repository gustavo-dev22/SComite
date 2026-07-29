import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, MenuItemNode, MenuObjeto, RolComite } from '../models/sasi.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
    private http = inject(HttpClient);
    private router = inject(Router);
    private apiUrl = `${environment.apiUrl}/Auth`;

    usuarioActual = signal<string | null>(sessionStorage.getItem('usuario_nombre'));
    rolesDisponibles = signal<RolComite[]>(this.obtenerRolesStorage());
    rolActivoId = signal<number | null>(this.obtenerRolActivoInicial());

    // Signal derivada: Objeto del Rol seleccionado actualmente
    rolActivoObj = computed<RolComite | null>(() => {
      const id = this.rolActivoId();
      const roles = this.rolesDisponibles();
      
      // Buscar por ID activo; si no coincide, tomar el principal o el primero como respaldo
      return roles.find(r => r.idRol === id) 
          || roles.find(r => r.esPrincipal) 
          || roles[0] 
          || null;
    });

    // Signal derivada: Nombre del rol activo (reemplaza tu signal antiguo 'rolActual')
  rolActual = computed<string>(() => {
    return this.rolActivoObj()?.nombreRol || 'APODERADO';
  });

  // Signal derivada: Menús pertenecientes ÚNICAMENTE al rol activo
  menuSesion = computed<MenuObjeto[]>(() => {
    return this.rolActivoObj()?.objetos || [];
  });

  // Signal derivada: Árbol de menú jerárquico reactivo
  menuJerarquico = computed<MenuItemNode[]>(() => {
    const lista = this.menuSesion().filter(o => o.activo);
    const padres = lista
      .filter(o => o.tipo === 'Menu' && (o.idPadre === null || o.idPadre === undefined))
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
        if (res.exito && res.sistemaComite?.roles?.length > 0) {
          sessionStorage.setItem('token_aula', res.token);
          sessionStorage.setItem('usuario_nombre', res.nombreUsuario);

          // 1. Guardar la lista completa de roles entregados por SASI
          const roles = res.sistemaComite.roles;
          sessionStorage.setItem('roles_aula', JSON.stringify(roles));

          // 2. Establecer por defecto el rol principal (o el primero)
          const rolPrincipal = roles.find(r => r.esPrincipal === true) || roles[0];
          sessionStorage.setItem('rol_activo_id', rolPrincipal.idRol.toString());

          // 3. Actualizar Signals reactivas
          this.usuarioActual.set(res.nombreUsuario);
          this.rolesDisponibles.set(roles);
          this.rolActivoId.set(rolPrincipal.idRol);
        }
      })
    );
  }

  // 🚀 Cambiar de Rol en tiempo real y redirigir
  cambiarRol(idRol: number): void {
    const nuevoRol = this.rolesDisponibles().find(r => r.idRol === idRol);
    if (!nuevoRol) return;

    sessionStorage.setItem('rol_activo_id', idRol.toString());
    this.rolActivoId.set(idRol);

    // Navegar automáticamente a la primera ruta ejecutable del nuevo rol
    const primeraRuta = this.obtenerPrimeraRutaSubmenu(nuevoRol.objetos);
    this.router.navigate([primeraRuta]);
  }

  logout(): void {
    sessionStorage.removeItem('token_aula');
    sessionStorage.removeItem('usuario_nombre');
    sessionStorage.removeItem('roles_aula');
    sessionStorage.removeItem('rol_activo_id');

    this.usuarioActual.set(null);
    this.rolesDisponibles.set([]);
    this.rolActivoId.set(null);
    this.router.navigate(['/login']);
  }

  isAuthenticated(): boolean {
    return !!sessionStorage.getItem('token_aula');
  }

  private obtenerRolesStorage(): RolComite[] {
    const raw = sessionStorage.getItem('roles_aula');
    return raw ? JSON.parse(raw) : [];
  }

  private obtenerRolActivoInicial(): number | null {
    const rawId = sessionStorage.getItem('rol_activo_id');
    const roles = this.obtenerRolesStorage();

    if (roles.length === 0) return null;

    // Si ya hay un ID seleccionado guardado y existe en los roles
    if (rawId) {
      const idExistente = Number(rawId);
      const existe = roles.some(r => r.idRol === idExistente);
      if (existe) return idExistente;
    }

    // Si no hay o fue reconfigurado, busca el que tenga esPrincipal = true
    const principal = roles.find(r => r.esPrincipal === true) || roles[0];
    return principal ? principal.idRol : null;
  }

  private obtenerPrimeraRutaSubmenu(objetos: MenuObjeto[]): string {
    const submenus = objetos.filter(o => o.activo && o.tipo === 'Submenu' && o.url && o.url !== '#');
    if (submenus.length > 0 && submenus[0].url) {
      const url = submenus[0].url;
      return url.startsWith('/') ? url.substring(1) : url;
    }
    return 'admin/periodos';
  }
}