import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-layout',
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './layout.html',
  styleUrl: './layout.scss',
})
export class LayoutComponent implements OnInit {
  authService = inject(AuthService);

  sidebarAbierto = signal<boolean>(true);
  sidebarMovilAbierto = signal<boolean>(false);
  menuDesplegado = signal<{ [key: number]: boolean }>({});

  ngOnInit(): void {
    this.inicializarSubmenusAbiertos();
  }

  // 🚀 Evento para alternar de rol
  onRolChange(event: Event): void {
    const idRol = Number((event.target as HTMLSelectElement).value);
    this.authService.cambiarRol(idRol);
    // Re-inicializamos los submenús abiertos para el nuevo árbol del menú
    setTimeout(() => this.inicializarSubmenusAbiertos(), 100);
  }

  private inicializarSubmenusAbiertos(): void {
    const estadoInicial: { [key: number]: boolean } = {};
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

  getIconClass(icono: string | null, esSubmenu: boolean = false): string {
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
    const mapaIconos: { [key: string]: string } = {
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
      'layers': 'pi pi-file'
    };

    return mapaIconos[icono] || `pi pi-${icono}`;
  }
}
