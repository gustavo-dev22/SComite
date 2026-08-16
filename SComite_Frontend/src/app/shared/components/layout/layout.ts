import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import Swal from 'sweetalert2';

const MAPA_ICONOS: Record<string, string> = {
  'school': 'pi pi-building',
  'user-plus': 'pi pi-user-plus',
  'users': 'pi pi-users',
  'pie-chart': 'pi pi-chart-pie',
  'shield-check': 'pi pi-shield',
  'wallet': 'pi pi-wallet',
  'check-circle': 'pi pi-check-circle',
  'receipt': 'pi pi-receipt',
  'bar-chart': 'pi pi-chart-bar',
  'calendar': 'pi pi-calendar',
  'megaphone': 'pi pi-megaphone',
  'vote': 'pi pi-list-check',
  'file-text': 'pi pi-file',
  'layers': 'pi pi-file',
  'trash': 'pi pi-trash',
  'gift': 'pi pi-gift',
  'message-square': 'pi pi-whatsapp'
};

@Component({
  selector: 'app-layout',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './layout.html',
  styleUrl: './layout.scss',
})
export class LayoutComponent {
  authService = inject(AuthService);

  sidebarAbierto = signal<boolean>(true);
  sidebarMovilAbierto = signal<boolean>(false);
  menuDesplegado = signal<Record<number, boolean>>({});

  // 🚀 Re-inicializa los submenús abiertos cada vez que cambia el árbol de menú (rol/sesión),
  // en lugar del setTimeout(100) sucio que dependía de carreras de detección de cambios.
  private _efectoMenu = effect(() => {
    this.authService.menuJerarquico();
    this.inicializarSubmenusAbiertos();
  });

  // 🚀 Evento para alternar de rol
  onRolChange(event: Event): void {
    const idRol = Number((event.target as HTMLSelectElement).value);
    this.authService.cambiarRol(idRol);
  }

  private inicializarSubmenusAbiertos(): void {
    const estadoInicial: Record<number, boolean> = {};
    this.authService.menuJerarquico().forEach(item => {
      estadoInicial[item.idObjeto] = true;
    });
    this.menuDesplegado.set(estadoInicial);
  }

  toggleSidebar(): void {
    this.sidebarAbierto.update(v => !v);
  }

  toggleSidebarMovil(): void {
    this.sidebarMovilAbierto.update(v => !v);
  }

  toggleSubmenu(idObjeto: number): void {
    this.menuDesplegado.update(estado => ({
      ...estado,
      [idObjeto]: !estado[idObjeto]
    }));
  }

  cerrarSesion(): void {
    Swal.fire({
      title: '¿Cerrar Sesión?',
      text: 'Tendrás que ingresar tus credenciales nuevamente para acceder.',
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: '#ef4444',
      cancelButtonColor: '#64748b',
      confirmButtonText: 'Sí, salir',
      cancelButtonText: 'Cancelar',
      reverseButtons: true,
      allowOutsideClick: false,
      allowEscapeKey: false
    }).then((result) => {
      if (result.isConfirmed) {
        this.authService.logout();
      }
    });
  }

  getIconClass(icono: string | null, esSubmenu = false): string {
    if (!icono) {
      return esSubmenu ? 'pi pi-angle-right' : 'pi pi-folder';
    }

    // 1. Si ya viene con la nomenclatura completa de Bootstrap Icons o PrimeIcons
    if (icono.startsWith('pi ') || icono.startsWith('bi ')) {
      return icono;
    }

    // 2. Si viene con el prefijo "pi-"
    if (icono.startsWith('pi-')) {
      return `pi ${icono}`;
    }

    // 3. Mapeo de nombres comunes recibidos de SASI a PrimeIcons
    return MAPA_ICONOS[icono] || `pi pi-${icono}`;
  }
}
