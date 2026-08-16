import { HttpErrorResponse, HttpResponse, HttpRequest } from '@angular/common/http';
import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { firstValueFrom, of, throwError } from 'rxjs';
import Swal from 'sweetalert2';
import { errorInterceptor } from './error.interceptor';
import { AuthService } from '../services/auth.service';

describe('errorInterceptor', () => {
  const swalSpy = vi.spyOn(Swal, 'fire').mockResolvedValue({ isConfirmed: true } as never);

  const authServiceMock = {
    limpiarSesion: vi.fn()
  };

  beforeEach(async () => {
    swalSpy.mockClear();
    authServiceMock.limpiarSesion.mockClear();

    await TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(),
        { provide: AuthService, useValue: authServiceMock }
      ]
    }).compileComponents();
  });

  it('no duplica la alerta ante múltiples peticiones fallidas en el mismo ciclo', async () => {
    await TestBed.runInInjectionContext(async () => {
      const req = new HttpRequest<unknown>('GET', '/api/Aulas/periodos');
      const fallaRed = () => {
        return throwError(() => new HttpErrorResponse({ status: 0, statusText: 'Unknown Error' }));
      };

      // Se lanzan ambas peticiones de forma síncrona (mismo ciclo) para que la
      // segunda sea absorbida por la bandera anti-duplicación del interceptor.
      const p1 = firstValueFrom(errorInterceptor(req, fallaRed)).catch(() => void 0);
      const p2 = firstValueFrom(errorInterceptor(req, fallaRed)).catch(() => void 0);

      await Promise.all([p1, p2]);

      expect(swalSpy).toHaveBeenCalledTimes(1);
    });
  });

  it('muestra la alerta global "Sistema no disponible" cuando el backend está caído (status 0)', async () => {
    await TestBed.runInInjectionContext(async () => {
      const req = new HttpRequest<unknown>('GET', '/api/Aulas/periodos');
      const errorRed = new HttpErrorResponse({ status: 0, statusText: 'Unknown Error' });

      await firstValueFrom(
        errorInterceptor(req, () => throwError(() => errorRed))
      ).catch(() => void 0);

      expect(swalSpy).toHaveBeenCalledWith(
        expect.objectContaining({
          icon: 'error',
          title: 'Sistema no disponible'
        })
      );
    });
  });

  it('no muestra la alerta global de conexión para respuestas HTTP normales (p. ej. 404)', async () => {
    await TestBed.runInInjectionContext(async () => {
      const req = new HttpRequest<unknown>('GET', '/api/Aulas/periodos');

      await firstValueFrom(
        errorInterceptor(req, () => {
          return throwError(() => new HttpErrorResponse({ status: 404, statusText: 'Not Found' }));
        })
      ).catch(() => void 0);

      expect(swalSpy).not.toHaveBeenCalledWith(
        expect.objectContaining({ title: 'Sistema no disponible' })
      );
    });
  });

  it('propaga el error al suscriptor para que los handlers locales puedan reaccionar', async () => {
    await TestBed.runInInjectionContext(async () => {
      const req = new HttpRequest<unknown>('GET', '/api/Aulas/periodos');
      const errorOriginal = new HttpErrorResponse({ status: 0, statusText: 'Unknown Error' });

      await expect(
        firstValueFrom(
          errorInterceptor(req, () => {
            return throwError(() => errorOriginal);
          })
        )
      ).rejects.toBe(errorOriginal);
    });
  });

  it('no bloquea las respuestas exitosas', async () => {
    await TestBed.runInInjectionContext(async () => {
      const req = new HttpRequest<unknown>('GET', '/api/Aulas/periodos');
      const respuesta = new HttpResponse({ status: 200, body: { exito: true } });

      const resultado = await firstValueFrom(
        errorInterceptor(req, () => of(respuesta))
      );

      expect(resultado).toEqual(respuesta);
      expect(swalSpy).not.toHaveBeenCalled();
    });
  });
});
