import { CommonModule } from '@angular/common';
import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ApoderadoService } from '../../../core/services/apoderado.service';
import { CuotaApoderado, HijoApoderado, ResumenPagosApoderado } from '../../../core/models/apoderado.model';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-mis-pagos',
  imports: [CommonModule, FormsModule],
  templateUrl: './mis-pagos.html',
  styleUrl: './mis-pagos.scss',
})
export class MisPagosComponent implements OnInit {
  private destroyRef = inject(DestroyRef);
  private apoderadoService = inject(ApoderadoService);

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
        Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar tus hijos.', 'error');
      }
    });
  }

  cargarCuotasEstudiante(): void {
    const estudianteId = this.estudianteSeleccionadoId();
    if (!estudianteId) return;

    this.cargandoCuotas.set(true);
    this.apoderadoService.getCuotasPendientes(estudianteId, this.anioLectivoActual).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.resumen.set(data);
        this.cargandoCuotas.set(false);
      },
      error: (err) => {
        this.cargandoCuotas.set(false);
        Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar las cuotas pendientes.', 'error');
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

    const tesoreroTel = hijo.tesoreroTelefono.replace(/[^0-9]/g, '');
    const tesoreroNom = hijo.tesoreroNombre || 'Tesorero(a)';

    const mensaje = `Hola ${tesoreroNom}, le escribo para notificarle que acabo de realizar el pago de S/. ${cuota.montoTotalCuota.toFixed(2)} por concepto de "${cuota.concepto}" para el alumno ${hijo.nombreEstudiante} del aula ${hijo.nombreAula}. Adjunto mi comprobante.`;

    const url = `https://wa.me/51${tesoreroTel}?text=${encodeURIComponent(mensaje)}`;
    window.open(url, '_blank');
  }

  getBadgeClass(estado: string): string {
    switch (estado) {
      case 'PAGADO': return 'bg-emerald-100 text-emerald-800 border-emerald-200';
      case 'VENCIDO': return 'bg-rose-100 text-rose-800 border-rose-200';
      default: return 'bg-amber-100 text-amber-800 border-amber-200';
    }
  }
}
