import { HttpErrorResponse } from '@angular/common/http';
import Swal from 'sweetalert2';
import { manejarErrorHttp } from './http-error.util';

describe('manejarErrorHttp', () => {
  const swalSpy = vi.spyOn(Swal, 'fire').mockResolvedValue({ isConfirmed: true } as never);

  beforeEach(() => {
    swalSpy.mockClear();
  });

  it('NO muestra alerta local cuando el backend está caído (status 0) porque el interceptor lo maneja globalmente', () => {
    const err = new HttpErrorResponse({ status: 0, statusText: 'Unknown Error' });

    manejarErrorHttp(err, 'No se pudieron cargar los periodos lectivos.');

    expect(swalSpy).not.toHaveBeenCalled();
  });

  it('NO muestra alerta local para status 401 (sesión expirada manejada por el interceptor)', () => {
    const err = new HttpErrorResponse({ status: 401, statusText: 'Unauthorized' });

    manejarErrorHttp(err, 'No se pudieron cargar los periodos lectivos.');

    expect(swalSpy).not.toHaveBeenCalled();
  });

  it('NO muestra alerta local para status 5xx (servicio no disponible manejado por el interceptor)', () => {
    const err = new HttpErrorResponse({ status: 500, statusText: 'Internal Server Error' });

    manejarErrorHttp(err, 'No se pudieron cargar los periodos lectivos.');

    expect(swalSpy).not.toHaveBeenCalled();
  });

  it('SÍ muestra el mensaje del backend cuando existe (p. ej. 404 con mensaje)', () => {
    const err = new HttpErrorResponse({
      status: 404,
      statusText: 'Not Found',
      error: { mensaje: 'No se encontró el recurso.' }
    });

    manejarErrorHttp(err, 'Mensaje de respaldo.');

    expect(swalSpy).toHaveBeenCalledWith('Error', 'No se encontró el recurso.', 'error');
  });

  it('lee el campo detail de ProblemDetails RFC 7807 cuando no hay mensaje propio', () => {
    const err = new HttpErrorResponse({
      status: 400,
      statusText: 'Bad Request',
      error: { detail: 'El monto abonado excede la deuda pendiente.' }
    });

    manejarErrorHttp(err, 'Mensaje de respaldo.');

    expect(swalSpy).toHaveBeenCalledWith('Error', 'El monto abonado excede la deuda pendiente.', 'error');
  });

  it('usa el mensaje de respaldo cuando no hay mensaje del backend (p. ej. 400 sin cuerpo)', () => {
    const err = new HttpErrorResponse({ status: 400, statusText: 'Bad Request' });

    manejarErrorHttp(err, 'No se pudieron cargar los gastos.');

    expect(swalSpy).toHaveBeenCalledWith('Error', 'No se pudieron cargar los gastos.', 'error');
  });
});
