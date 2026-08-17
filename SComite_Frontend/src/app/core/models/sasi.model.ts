export interface MenuObjeto {
  idObjeto: number;
  nombre: string;
  tipo: 'Menu' | 'Submenu';
  url: string;
  titulo: string;
  icono: string;
  activo: boolean;
  orden: number;
  idPadre: number | null;
  posicion?: number;
  subObjetos?: MenuObjeto[];
  hijos?: MenuObjeto[];
}

export interface RolComite {
  idRol: number;
  nombreRol: string;
  activo?: boolean;
  esPrincipal: boolean;
  objetos: MenuObjeto[];
}

export interface SistemaComite {
  id: number;
  nombre: string;
  activo: boolean;
  roles: RolComite[];
}

export interface AuthResponse {
  exito: boolean;
  mensaje?: string;
  token: string;
  nombreUsuario: string;
  email: string;
  sistemaComite: SistemaComite;
}

export interface MenuItemNode extends MenuObjeto {
  submenus: MenuObjeto[];
}