import { DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PeriodoLectivo } from '../models/periodoLectivo.model';
import { AulaService } from '../services/aula.service';
import { manejarErrorHttp } from '../utils/http-error.util';

export abstract class BasePeriodosComponent {
  protected readonly destroyRef = inject(DestroyRef);
  protected readonly aulaService = inject(AulaService);

  periodos = signal<PeriodoLectivo[]>([]);

  cargarPeriodos(): void {
    this.aulaService.getPeriodos().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.periodos.set(data);
        this.onPeriodosCargados(data);
      },
      error: (err) => {
        this.onPeriodosError(err);
        manejarErrorHttp(err, 'No se pudieron cargar los periodos lectivos.');
      }
    });
  }

  protected onPeriodosCargados(_data: PeriodoLectivo[]): void {}

  protected onPeriodosError(_err: unknown): void {}

  protected buscarPeriodoVigente(data: PeriodoLectivo[]): PeriodoLectivo | null {
    const anioSistema = new Date().getFullYear();
    return data.find(p => p.anio === anioSistema) || data.find(p => p.esActivo) || data[0] || null;
  }
}