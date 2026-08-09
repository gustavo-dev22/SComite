import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { BalanceAula } from '../../../core/models/gastoTransparencia.model';
import { TransparenciaService } from '../../../core/services/transparencia.service';
import { ApoderadoService } from '../../../core/services/apoderado.service';
import { HijoApoderado } from '../../../core/models/apoderado.model';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, takeUntil } from 'rxjs';
import Swal from 'sweetalert2';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-transparencia-balance',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  templateUrl: './transparencia-balance.html',
  styleUrl: './transparencia-balance.scss',
})
export class TransparenciaBalanceComponent implements OnInit {
  private destroyRef = inject(DestroyRef);
  private reiniciarCarga$ = new Subject<void>();
  private apoderadoService = inject(ApoderadoService);
  private transparenciaService = inject(TransparenciaService);

  constructor() {
    this.destroyRef.onDestroy(() => this.reiniciarCarga$.complete());
  }

  cargandoHijos = signal<boolean>(false);
  cargandoBalance = signal<boolean>(false);

  hijos = signal<HijoApoderado[]>([]);
  estudianteSeleccionadoId = signal<number | null>(null);

  balance = signal<BalanceAula | null>(null);
  mesFiltroSeleccionado = signal<number | null>(null);
  anioLectivoActual = new Date().getFullYear();

  hijoActual = computed(() => {
    const id = this.estudianteSeleccionadoId();
    return this.hijos().find(h => h.estudianteId === id) || null;
  });

  // Métricas computadas del balance
  // 🚀 Obtiene los datos del mes seleccionado si el filtro está activo
  mesSeleccionadoData = computed(() => {
    const mesNum = this.mesFiltroSeleccionado();
    if (!mesNum || !this.balance()) return null;
    return this.balance()?.desgloseMensual.find(m => m.mesNum === mesNum) || null;
  });

  // 🚀 Métricas computadas dinámicas (Reaccionan al filtro de mes o muestran el acumulado anual)
  totalIngresos = computed(() => {
    const mes = this.mesSeleccionadoData();
    return mes ? mes.totalIngresosMes : (this.balance()?.totalIngresos || 0);
  });

  totalEgresos = computed(() => {
    const mes = this.mesSeleccionadoData();
    return mes ? mes.totalEgresosMes : (this.balance()?.totalEgresos || 0);
  });

  saldoDisponible = computed(() => {
    const mes = this.mesSeleccionadoData();
    return mes ? mes.saldoMes : (this.balance()?.saldoDisponible || 0);
  });

  desgloseMensual = computed(() => this.balance()?.desgloseMensual || []);

  // Filtrado de la lista de egresos por mes
  egresosFiltrados = computed(() => {
    const lista = this.balance()?.egresos || [];
    const mes = this.mesFiltroSeleccionado();
    if (!mes) return lista;

    return lista.filter(g => {
      const fecha = new Date(g.fechaGasto);
      return (fecha.getMonth() + 1) === mes;
    });
  });

  ngOnInit(): void {
    this.cargarHijos();
  }

  cargarHijos(): void {
    this.cargandoHijos.set(true);
    this.apoderadoService.getMisHijos(this.anioLectivoActual)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.hijos.set(data);
          this.cargandoHijos.set(false);

          if (data.length > 0) {
            this.estudianteSeleccionadoId.set(data[0].estudianteId);
            this.cargarBalance();
          }
        },
        error: (err) => {
          this.cargandoHijos.set(false);
          Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar tus hijos.', 'error');
        }
      });
  }

  cargarBalance(): void {
    this.reiniciarCarga$.next();
    const hijo = this.hijoActual();
    if (!hijo || !hijo.aulaId) return;

    this.cargandoBalance.set(true);
    this.transparenciaService.getBalanceAula(hijo.aulaId, this.anioLectivoActual)
      .pipe(takeUntil(this.reiniciarCarga$), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.balance.set(data);
          this.cargandoBalance.set(false);
        },
        error: (err) => {
          this.cargandoBalance.set(false);
          Swal.fire('Error', err.error?.mensaje || 'No se pudo cargar el balance del aula.', 'error');
        }
      });
  }

  onSeleccionarHijo(estudianteId: number): void {
    this.estudianteSeleccionadoId.set(estudianteId);
    this.mesFiltroSeleccionado.set(null);
    this.cargarBalance();
  }

  onFiltrarPorMes(mesNum: number | null): void {
    this.mesFiltroSeleccionado.set(this.mesFiltroSeleccionado() === mesNum ? null : mesNum);
  }

  abrirComprobante(url?: string): void {
    if (!url) return;
    if (!/^https?:\/\//i.test(url)) return;
    const timestamp = new Date().getTime();
    const separator = url.includes('?') ? '&' : '?';
    window.open(`${url}${separator}_t=${timestamp}`, '_blank', 'noopener,noreferrer');
  }
}
