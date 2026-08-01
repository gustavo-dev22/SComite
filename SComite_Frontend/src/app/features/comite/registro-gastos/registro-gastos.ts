import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { GastoService } from '../../../core/services/gasto.service';
import { AulaService } from '../../../core/services/aula.service';
import { PeriodoLectivo } from '../../../core/models/periodoLectivo.model';
import { Aula } from '../../../core/models/aula.model';
import { GastoComite, ResumenCajaAula } from '../../../core/models/gasto.model';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-registro-gastos',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './registro-gastos.html',
  styleUrl: './registro-gastos.scss',
})
export class RegistroGastosComponent implements OnInit {
  private fb = inject(FormBuilder);
  private gastoService = inject(GastoService);
  private aulaService = inject(AulaService);

  // Listas
  periodos = signal<PeriodoLectivo[]>([]);
  aulas = signal<Aula[]>([]);
  gastos = signal<GastoComite[]>([]);
  resumenCaja = signal<ResumenCajaAula>({
    saldoAnteriorArrastrado: 0,
    ingresosDelMes: 0,
    egresosDelMes: 0,
    saldoDisponibleReal: 0
  });

  // Selecciones
  periodoSeleccionadoId = signal<number | null>(null);
  aulaSeleccionadaId = signal<number | null>(null);
  mesSeleccionado = signal<number>(new Date().getMonth() + 1);

  // Estados UI
  cargando = signal<boolean>(false);
  cargandoAulas = signal<boolean>(false);
  mostrarModal = signal<boolean>(false);

  tienePeriodoSeleccionado = computed(() => this.periodoSeleccionadoId() !== null && this.periodoSeleccionadoId()! > 0);
  tieneAulaSeleccionada = computed(() => this.aulaSeleccionadaId() !== null && this.aulaSeleccionadaId()! > 0);
  puedeRegistrarGasto = computed(() => this.aulaSeleccionadaId() !== null && this.aulaSeleccionadaId()! > 0);

  gastosFiltradosMes = computed(() => {
    const lista = this.gastos();
    const mes = this.mesSeleccionado();

    if (!mes || mes === 0) return lista;

    return lista.filter(g => {
      const fecha = new Date(g.fechaGasto);
      return (fecha.getMonth() + 1) === mes;
    });
  });

  gastoForm: FormGroup = this.fb.group({
    concepto: ['', [Validators.required, Validators.maxLength(150)]],
    categoria: ['MATERIALES', Validators.required],
    monto: [0, [Validators.required, Validators.min(0.10)]],
    fechaGasto: [new Date().toISOString().substring(0, 10), Validators.required],
    tipoComprobante: ['BOLETA', Validators.required],
    numeroComprobante: [''],
    proveedor: [''],
    observacion: ['']
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
    this.gastos.set([]);
    this.resetsResumen();

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
      this.cargarGastosYBalance(aulaId);
    } else {
      this.gastos.set([]);
      this.resetsResumen();
    }
  }

  onMesChange(event: Event): void {
    const mes = Number((event.target as HTMLSelectElement).value);
    this.mesSeleccionado.set(mes);

    const aulaId = this.aulaSeleccionadaId();
    if (aulaId) {
      this.cargarGastosYBalance(aulaId);
    }
  }

  cargarGastosYBalance(aulaId: number): void {
    this.cargando.set(true);

    const periodoId = this.periodoSeleccionadoId();
    const periodoObj = this.periodos().find(p => p.id === periodoId);
    const anio = periodoObj ? periodoObj.anio : new Date().getFullYear();
    const mes = this.mesSeleccionado();

    // 1. Cargar Balance Mensual con Arrastre
    this.gastoService.obtenerBalanceMensual(aulaId, anio, mes).subscribe({
      next: (res) => this.resumenCaja.set(res)
    });

    // 2. Cargar Lista de Gastos
    this.gastoService.obtenerPorAula(aulaId).subscribe({
      next: (data) => {
        this.gastos.set(data);
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false)
    });
  }

  guardarGasto(): void {
    const aulaId = this.aulaSeleccionadaId();
    if (this.gastoForm.invalid || !aulaId) return;

    const payload = { ...this.gastoForm.value, aulaId };

    this.gastoService.crear(payload).subscribe({
      next: () => {
        this.cerrarModal();
        Swal.fire({
          icon: 'success',
          title: 'Gasto Registrado',
          text: 'Se ha descontado de la caja del aula.',
          timer: 1500,
          showConfirmButton: false
        });
        this.cargarGastosYBalance(aulaId);
      }
    });
  }

  eliminarGasto(gasto: GastoComite): void {
    Swal.fire({
      title: '¿Eliminar Gasto?',
      text: `Se reincorporarán S/. ${gasto.monto.toFixed(2)} al saldo de caja.`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#e11d48',
      cancelButtonColor: '#64748b',
      confirmButtonText: 'Sí, Eliminar',
      cancelButtonText: 'Cancelar',
      allowEscapeKey: false,
      allowOutsideClick: false
    }).then((result) => {
      if (result.isConfirmed) {
        this.gastoService.eliminar(gasto.id).subscribe({
          next: () => {
            Swal.fire({
              icon: 'success',
              title: 'Gasto Eliminado',
              timer: 1500,
              showConfirmButton: false
            });
            this.cargarGastosYBalance(this.aulaSeleccionadaId()!);
          }
        });
      }
    });
  }

  private resetsResumen(): void {
    this.resumenCaja.set({
      saldoAnteriorArrastrado: 0,
      ingresosDelMes: 0,
      montoDonacionesMes: 0,
      egresosDelMes: 0,
      saldoDisponibleReal: 0
    });
  }

  abrirModal(): void {
    if (!this.puedeRegistrarGasto()) return;
    this.gastoForm.reset({
      categoria: 'MATERIALES',
      monto: 0,
      fechaGasto: new Date().toISOString().substring(0, 10),
      tipoComprobante: 'BOLETA'
    });
    this.mostrarModal.set(true);
  }

  cerrarModal(): void {
    this.mostrarModal.set(false);
  }
}
