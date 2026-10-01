import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  protected readonly title = signal('SComite_Frontend');

  private router = inject(Router);
  private authService = inject(AuthService);

  ngOnInit(): void {
    this.evitarRetrocesoTrasLogout();
  }

  // Evita que el botón "atrás" del navegador muestre páginas protegidas restauradas
  // desde la caché de navegación (bfcache) después de cerrar sesión.
  private evitarRetrocesoTrasLogout(): void {
    window.addEventListener('pageshow', (event) => {
      if (!event.persisted || this.authService.tieneSesionValida()) return;

      const url = this.router.url;
      if (url.startsWith('/login') || url.startsWith('/sso-callback')) return;

      this.router.navigateByUrl('/login', { replaceUrl: true });
    });
  }
}
