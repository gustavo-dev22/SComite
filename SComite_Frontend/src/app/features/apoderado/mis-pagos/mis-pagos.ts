import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, takeUntil } from 'rxjs';

import { FormsModule } from '@angular/forms';
import { ApoderadoService } from '../../../core/services/apoderado.service';
import { CuotaApoderado, HijoApoderado, ResumenPagosApoderado } from '../../../core/models/apoderado.model';
import { manejarErrorHttp } from '../../../core/utils/http-error.util';
import { normalizarTelefonoPeru } from '../../../core/utils/whatsapp.util';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-mis-pagos',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule],
  templateUrl: './mis-pagos.html',
  styleUrl: './mis-pagos.scss',
})
export class MisPagosComponent implements OnInit {
  private destroyRef = inject(DestroyRef);
  private reiniciarCarga$ = new Subject<void>();
  private apoderadoService = inject(ApoderadoService);

  constructor() {
    this.destroyRef.onDestroy(() => this.reiniciarCarga$.complete());
  }

  cargandoHijos = signal<boolean>(false);
  cargandoCuotas = signal<boolean>(false);

  hijos = signal<HijoApoderado[]>([]);
  estudianteSeleccionadoId = signal<number | null>(null);

  resumen = signal<ResumenPagosApoderado | null>(null);

  anioLectivoActual = new Date().getFullYear();

  // 🚀 Hijo actualmente activo
  hijoActual = computed(() => {
    const id = this.estudianteSeleccionadoId();
    return this.hijos().find(h => h.estudianteId === id) || null;
  });

  ngOnInit(): void {
    this.cargarHijos();
  }

  cargarHijos(): void {
    this.cargandoHijos.set(true);
    this.apoderadoService.getMisHijos(this.anioLectivoActual).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.hijos.set(data);
        this.cargandoHijos.set(false);

        // Seleccionar automáticamente al primer hijo por defecto
        if (data.length > 0) {
          this.estudianteSeleccionadoId.set(data[0].estudianteId);
          this.cargarCuotasEstudiante();
        }
      },
      error: (err) => {
        this.cargandoHijos.set(false);
        manejarErrorHttp(err, 'No se pudieron cargar tus hijos.');
      }
    });
  }

  cargarCuotasEstudiante(): void {
    this.reiniciarCarga$.next();
    const estudianteId = this.estudianteSeleccionadoId();
    if (!estudianteId) return;

    this.cargandoCuotas.set(true);
    this.resumen.set(null);
    this.apoderadoService.getCuotasPendientes(estudianteId, this.anioLectivoActual).pipe(takeUntil(this.reiniciarCarga$), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.resumen.set(data);
        this.cargandoCuotas.set(false);
      },
      error: (err) => {
        this.cargandoCuotas.set(false);
        manejarErrorHttp(err, 'No se pudieron cargar las cuotas pendientes.');
      }
    });
  }

  onSeleccionarHijo(estudianteId: number): void {
    this.estudianteSeleccionadoId.set(estudianteId);
    this.cargarCuotasEstudiante();
  }

  notificarPorWhatsApp(cuota: CuotaApoderado): void {
    const hijo = this.hijoActual();
    if (!hijo) return;

    const tesoreroTel = normalizarTelefonoPeru(hijo.tesoreroTelefono);
    const tesoreroNom = hijo.tesoreroNombre || 'Tesorero(a)';

    if (!tesoreroTel) return;

    const montoCuota = Number(cuota.montoTotalCuota) || 0;
    const mensaje = `Hola ${tesoreroNom}, le escribo para notificarle que acabo de realizar el pago de S/. ${montoCuota.toFixed(2)} por concepto de "${cuota.concepto}" para el alumno ${hijo.nombreEstudiante} del aula ${hijo.nombreAula}. Adjunto mi comprobante.`;

    const url = `https://wa.me/${tesoreroTel}?text=${encodeURIComponent(mensaje)}`;
    window.open(url, '_blank', 'noopener,noreferrer');
  }
}
