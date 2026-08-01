import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AulaService } from '../../../core/services/aula.service';
import { DonacionService } from '../../../core/services/donacion.service';
import { PeriodoLectivo } from '../../../core/models/periodoLectivo.model';
import { Aula } from '../../../core/models/aula.model';
import { DonacionComite } from '../../../core/models/donacion.model';

@Component({
  selector: 'app-gestion-donaciones',
  imports: [CommonModule, FormsModule],
  templateUrl: './gestion-donaciones.html',
  styleUrl: './gestion-donaciones.scss',
})
export class GestionDonacionesComponent implements OnInit {
  private donacionService = inject(DonacionService);
  private aulaService = inject(AulaService);

  periodos = signal<PeriodoLectivo[]>([]);
  aulas = signal<Aula[]>([]);
  donaciones = signal<DonacionComite[]>([]);

  periodoSeleccionadoId = signal<number | null>(null);
  aulaSeleccionadaId = signal<number | null>(null);
  mesSeleccionado = signal<number>(0);

  cargandoAulas = signal<boolean>(false);
  cargando = signal<boolean>(false);
  guardando = signal<boolean>(false);
  mostrarModal = signal<boolean>(false);

  formDonacion = signal<Partial<DonacionComite>>({
    id: 0,
    aulaId: 0,
    donante: '',
    monto: 0,
    fechaDonacion: new Date().toISOString().split('T')[0],
    concepto: '',
    observacion: ''
  });

  tienePeriodoSeleccionado = computed(() => this.periodoSeleccionadoId() !== null && this.periodoSeleccionadoId()! > 0);
  tieneAulaSeleccionada = computed(() => this.aulaSeleccionadaId() !== null && this.aulaSeleccionadaId()! > 0);

  totalDonaciones = computed(() => {
    return this.donaciones().reduce((sum, item) => sum + item.monto, 0);
  });

  ngOnInit(): void {
    this.cargarPeriodos();
  }

  cargarPeriodos(): void {
    this.aulaService.getPeriodos().subscribe({
      next: (data) => this.periodos.set(data)
    });
  }

  onPeriodoChange(event: Event): void {
    const id = Number((event.target as HTMLSelectElement).value) || null;
    this.periodoSeleccionadoId.set(id);
    this.aulaSeleccionadaId.set(null);
    this.aulas.set([]);
    this.donaciones.set([]);

    if (id && id > 0) this.cargarAulasPorPeriodo(id);
  }

  cargarAulasPorPeriodo(periodoId: number): void {
    this.cargandoAulas.set(true);
    this.aulaService.getAulas(periodoId).subscribe({
      next: (data) => {
        this.aulas.set(data);
        this.cargandoAulas.set(false);
      },
      error: () => this.cargandoAulas.set(false)
    });
  }

  onAulaChange(event: Event): void {
    const aulaId = Number((event.target as HTMLSelectElement).value) || null;
    this.aulaSeleccionadaId.set(aulaId);

    if (aulaId && aulaId > 0) {
      this.cargarDonaciones(aulaId);
    } else {
      this.donaciones.set([]);
    }
  }

  onMesChange(event: Event): void {
    const mes = Number((event.target as HTMLSelectElement).value);
    this.mesSeleccionado.set(mes);

    const aulaId = this.aulaSeleccionadaId();
    if (aulaId) {
      this.cargarDonaciones(aulaId);
    }
  }

  cargarDonaciones(aulaId: number): void {
    this.cargando.set(true);
    const periodoObj = this.periodos().find(p => p.id === this.periodoSeleccionadoId());
    const anio = periodoObj ? periodoObj.anio : new Date().getFullYear();
    const mes = this.mesSeleccionado();

    this.donacionService.getDonacionesPorAula(aulaId, anio, mes).subscribe({
      next: (data) => {
        this.donaciones.set(data);
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false)
    });
  }

  abrirModalNuevo(): void {
    this.formDonacion.set({
      id: 0,
      aulaId: this.aulaSeleccionadaId()!,
      donante: '',
      monto: 0,
      fechaDonacion: new Date().toISOString().split('T')[0],
      concepto: '',
      observacion: ''
    });
    this.mostrarModal.set(true);
  }

  abrirModalEditar(d: DonacionComite): void {
    this.formDonacion.set({
      id: d.id,
      aulaId: d.aulaId,
      donante: d.donante,
      monto: d.monto,
      fechaDonacion: new Date(d.fechaDonacion).toISOString().split('T')[0],
      concepto: d.concepto,
      observacion: d.observacion || ''
    });
    this.mostrarModal.set(true);
  }

  cerrarModal(): void {
    this.mostrarModal.set(false);
  }

  onInputToUppercase(campo: 'donante' | 'concepto'): void {
    const val = this.formDonacion()[campo] || '';
    this.formDonacion.update(f => ({ ...f, [campo]: val.toUpperCase() }));
  }

  guardarDonacion(): void {
    const dto = this.formDonacion();
    if (!dto.donante?.trim() || !dto.concepto?.trim() || (dto.monto || 0) <= 0) return;

    dto.aulaId = this.aulaSeleccionadaId()!;
    this.guardando.set(true);

    this.donacionService.guardarDonacion(dto).subscribe({
      next: () => {
        this.guardando.set(false);
        this.cerrarModal();
        this.cargarDonaciones(this.aulaSeleccionadaId()!);
      },
      error: () => this.guardando.set(false)
    });
  }

  eliminarDonacion(id: number): void {
    if (!confirm('¿Está seguro de eliminar este registro de donación?')) return;

    this.donacionService.eliminarDonacion(id, this.aulaSeleccionadaId()!).subscribe({
      next: () => this.cargarDonaciones(this.aulaSeleccionadaId()!)
    });
  }
}
