import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, defer, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, MenuItemNode, MenuObjeto, RolComite } from '../models/sasi.model';

// Cliente SSO registrado en SASI para el sistema Comité de Aula.
const SSO_CLIENT_ID = 'comite';
const SSO_REDIRECT_PATH = '/sso-callback';
const SSO_VERIFIER_KEY = 'sso_code_verifier';
const SSO_STATE_KEY = 'sso_state';
const SSO_RETURN_URL_KEY = 'sso_return_url';

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
      tap(res => this.establecerSesion(res))
    );
  }

  // ====== SSO provisto por SASI (authorization code + PKCE) ======

  // Inicia el flujo SSO: verifica disponibilidad, genera PKCE/state y redirige al login de SASI.
  async iniciarSsoSesion(returnUrl?: string): Promise<void> {
    const disponible = await this.verificarSasiDisponibleAsync();
    if (!disponible) {
      throw new Error('No se pudo conectar con el servicio de autenticación (SASI). Verifique que esté disponible e intente nuevamente.');
    }

    const verifier = this.generarCodeVerifier();
    const challenge = await this.generarCodeChallenge(verifier);
    const state = this.generarState();

    sessionStorage.setItem(SSO_VERIFIER_KEY, verifier);
    sessionStorage.setItem(SSO_STATE_KEY, state);
    sessionStorage.setItem(SSO_RETURN_URL_KEY, returnUrl ?? '');

    const params = new URLSearchParams({
      client_id: SSO_CLIENT_ID,
      returnUrl: this.redirectUriSso(),
      state,
      code_challenge: challenge,
      code_challenge_method: 'S256'
    });

    window.location.href = `${environment.sasiSsoLoginUrl}?${params.toString()}`;
  }

  // Canjea el authorization code devuelto por SASI y establece la sesión local.
  completarSsoSesion(code: string, state: string): Observable<AuthResponse> {
    return defer(() => {
      const savedState = sessionStorage.getItem(SSO_STATE_KEY);
      if (!savedState || savedState !== state) {
        throw new Error('El estado de la solicitud no coincide. Inicie sesión nuevamente.');
      }

      const verifier = sessionStorage.getItem(SSO_VERIFIER_KEY);
      if (!verifier) {
        throw new Error('No se encontró el verificador PKCE. Inicie sesión nuevamente.');
      }

      const body = { code, codeVerifier: verifier, redirectUri: this.redirectUriSso() };

      return this.http.post<AuthResponse>(`${this.apiUrl}/sso`, body).pipe(
        tap(res => this.establecerSesion(res))
      );
    });
  }

  obtenerReturnUrlSso(): string | null {
    const returnUrl = sessionStorage.getItem(SSO_RETURN_URL_KEY);
    return returnUrl && returnUrl.trim() ? returnUrl : null;
  }

  // Establece la sesión local a partir de la respuesta del backend (login directo o SSO).
  private establecerSesion(res: AuthResponse): void {
    // 1. Guardar solo los roles ACTIVOS entregados por SASI (los desactivados en el
    //    toggle de SASI no deben aparecer en el selector de rol).
    const roles = AuthService.filtrarRolesActivos(res.sistemaComite?.roles ?? []);

    if (!res.exito || roles.length === 0) {
      throw new Error(res.mensaje || "Tu usuario no tiene un rol activo en el sistema 'Comité de Aula'.");
    }

    sessionStorage.setItem('token_aula', res.token);
    sessionStorage.setItem('usuario_nombre', res.nombreUsuario);
    sessionStorage.setItem('roles_aula', JSON.stringify(roles));

    // 2. Establecer por defecto el rol principal (o el primero)
    const rolPrincipal = roles.find(r => r.esPrincipal === true) || roles[0];
    sessionStorage.setItem('rol_activo_id', rolPrincipal.idRol.toString());

    // 3. Actualizar Signals reactivas
    this.usuarioActual.set(res.nombreUsuario);
    this.rolesDisponibles.set(roles);
    this.rolActivoId.set(rolPrincipal.idRol);

    this.limpiarTemporalesSso();
  }

  private limpiarTemporalesSso(): void {
    sessionStorage.removeItem(SSO_VERIFIER_KEY);
    sessionStorage.removeItem(SSO_STATE_KEY);
    sessionStorage.removeItem(SSO_RETURN_URL_KEY);
  }

  private redirectUriSso(): string {
    return `${window.location.origin}${SSO_REDIRECT_PATH}`;
  }

  // Consulta al backend (que a su vez hace ping a SASI) para no redirigir si el SSO
  // está caído. Se usa fetch para no disparar las alertas globales del interceptor.
  private async verificarSasiDisponibleAsync(): Promise<boolean> {
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 5000);
    try {
      const res = await fetch(`${this.apiUrl}/sso/ping`, {
        method: 'GET',
        cache: 'no-store',
        signal: controller.signal
      });
      if (!res.ok) return false;
      const data = (await res.json()) as { disponible?: boolean };
      return data?.disponible === true;
    } catch {
      return false;
    } finally {
      clearTimeout(timeout);
    }
  }

  private generarCodeVerifier(): string {
    const bytes = new Uint8Array(32);
    crypto.getRandomValues(bytes);
    return this.base64Url(bytes);
  }

  private async generarCodeChallenge(verifier: string): Promise<string> {
    const digest = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(verifier));
    return this.base64Url(new Uint8Array(digest));
  }

  private generarState(): string {
    const bytes = new Uint8Array(16);
    crypto.getRandomValues(bytes);
    return this.base64Url(bytes);
  }

  private base64Url(bytes: Uint8Array): string {
    let binario = '';
    bytes.forEach(b => (binario += String.fromCharCode(b)));
    return btoa(binario).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  }

  // Cambiar de Rol en tiempo real y redirigir
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
    this.limpiarSesion();
    this.router.navigate(['/login']);
  }

  // Cierre de sesión único (SSO): limpia la sesión local y cierra también la de SASI.
  logoutSso(): void {
    this.limpiarSesion();
    const returnUrl = `${window.location.origin}/login`;
    window.location.href = `${environment.sasiLogoutUrl}?returnUrl=${encodeURIComponent(returnUrl)}`;
  }

  limpiarSesion(): void {
    sessionStorage.removeItem('token_aula');
    sessionStorage.removeItem('usuario_nombre');
    sessionStorage.removeItem('roles_aula');
    sessionStorage.removeItem('rol_activo_id');

    this.usuarioActual.set(null);
    this.rolesDisponibles.set([]);
    this.rolActivoId.set(null);
  }

  obtenerToken(): string | null {
    return sessionStorage.getItem('token_aula');
  }

  tieneSesionValida(): boolean {
    const token = this.obtenerToken();
    if (!token) return false;

    const payload = AuthService.decodificarJwt(token);
    if (payload?.exp) {
      const expiraEnMs = payload.exp * 1000;
      if (Date.now() >= expiraEnMs) {
        this.limpiarSesion();
        return false;
      }
    }
    return true;
  }

  isAuthenticated(): boolean {
    return this.tieneSesionValida();
  }

  private static decodificarJwt(token: string): { exp?: number } | null {
    try {
      const payload = token.split('.')[1];
      if (!payload) return null;

      const base64 = payload.replace(/-/g, '+').replace(/_/g, '/');
      const json = decodeURIComponent(
        atob(base64)
          .split('')
          .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
          .join('')
      );
      return JSON.parse(json);
    } catch {
      return null;
    }
  }

  private obtenerRolesStorage(): RolComite[] {
    const raw = sessionStorage.getItem('roles_aula');
    if (!raw) return [];
    try {
      const parsed = JSON.parse(raw);
      const roles = Array.isArray(parsed) ? parsed : [];
      return AuthService.filtrarRolesActivos(roles);
    } catch {
      sessionStorage.removeItem('roles_aula');
      sessionStorage.removeItem('rol_activo_id');
      return [];
    }
  }

  // 🛡️ Los roles desactivados en SASI (activo=false) no deben mostrarse en el
  // selector de rol. Filtra también roles que vengan sin el campo (compatibilidad).
  private static filtrarRolesActivos(roles: RolComite[]): RolComite[] {
    return roles.filter(r => r.activo !== false);
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

  obtenerRutaInicial(): string {
    const roles = this.rolesDisponibles();
    const rolInicial = roles.find(r => r.esPrincipal) || roles[0];
    if (!rolInicial) return 'login';

    const urlsEnOrdenLectura = this.obtenerUrlsEnOrdenLectura(rolInicial.objetos);

    // Listado de rutas declaradas en el Router
    const rutasExistentes = this.router.config
      .flatMap(r => r.children || [])
      .map(c => c.path);

    // Ningún usuario debe aterrizar en mantenimiento, logs o auditorías al iniciar sesión.
    const esRutaUtilitariaSistema = (url: string): boolean => {
      const urlLower = url.toLowerCase();
      return urlLower.includes('mantenimiento') ||
        urlLower.includes('logs') ||
        urlLower.includes('auditoria') ||
        urlLower.includes('seguridad/');
    };

    // 1. Primera ruta operativa válida que coincida en Angular y SASI (excluyendo utilitarios)
    const primeraRutaOperativa = urlsEnOrdenLectura.find(url =>
      rutasExistentes.includes(url) && !esRutaUtilitariaSistema(url)
    );
    if (primeraRutaOperativa) return primeraRutaOperativa;

    // 2. Si solo tuviera permisos a herramientas de sistema, tomar la primera disponible
    return urlsEnOrdenLectura.find(url => rutasExistentes.includes(url)) || 'login';
  }

  urlPermitidaEnMenu(url: string): boolean {
    const objetivo = url.startsWith('/') ? url.substring(1) : url;
    return this.obtenerUrlsEnOrdenLectura(this.menuSesion()).includes(objetivo);
  }

  private obtenerUrlsEnOrdenLectura(objetos: MenuObjeto[]): string[] {
    const urls: string[] = [];

    const obtenerNumeroOrden = (obj: MenuObjeto): number => {
      if (obj.orden !== undefined && obj.orden !== null) return Number(obj.orden);
      if (obj.posicion !== undefined && obj.posicion !== null) return Number(obj.posicion);
      if (obj.idObjeto !== undefined && obj.idObjeto !== null) return Number(obj.idObjeto);
      return 0;
    };

    const recorrer = (nodos: MenuObjeto[]): void => {
      const ordenados = [...nodos].sort((a, b) => obtenerNumeroOrden(a) - obtenerNumeroOrden(b));

      for (const nodo of ordenados) {
        if (!nodo || nodo.activo === false) continue;

        if (nodo.url && nodo.url !== '#' && nodo.url !== '/' && nodo.url !== 'javascript:void(0);') {
          urls.push(nodo.url.startsWith('/') ? nodo.url.substring(1) : nodo.url);
        }

        const hijos = nodo.subObjetos || nodo.hijos || [];
        if (hijos.length > 0) recorrer(hijos);
      }
    };

    recorrer(objetos);
    return urls;
  }
}