import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CuotaService } from '../../../core/services/cuota.service';
import { AulaService } from '../../../core/services/aula.service';
import { PeriodoLectivo } from '../../../core/models/periodoLectivo.model';
import { Aula } from '../../../core/models/aula.model';
import { Cuota, CuotaEstudianteCobro } from '../../../core/models/cuota.model';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-validar-comprobantes',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './validar-comprobantes.html',
  styleUrl: './validar-comprobantes.scss',
})
export class ValidarComprobantesComponent implements OnInit {
  private fb = inject(FormBuilder);
  private cuotaService = inject(CuotaService);
  private aulaService = inject(AulaService);

  // Listas
  periodos = signal<PeriodoLectivo[]>([]);
  aulas = signal<Aula[]>([]);
  cuotas = signal<Cuota[]>([]);
  cobrosEstudiantes = signal<CuotaEstudianteCobro[]>([]);

  // Filtros
  periodoSeleccionadoId = signal<number | null>(null);
  aulaSeleccionadaId = signal<number | null>(null);
  cuotaSeleccionadaId = signal<number | null>(null);
  filtroEstado = signal<string>('TODOS'); // 'TODOS' | 'PENDIENTE' | 'COMPLETO'

  cargando = signal<boolean>(false);
  cargandoAulas = signal<boolean>(false);
  cargandoCuotas = signal<boolean>(false);
  mostrarModalPago = signal<boolean>(false);
  estudianteCobroModal = signal<CuotaEstudianteCobro | null>(null);
  montoMaximoAbono = signal<number>(0);

  tienePeriodoSeleccionado = computed(() => this.periodoSeleccionadoId() !== null && this.periodoSeleccionadoId()! > 0);
  tieneAulaSeleccionada = computed(() => this.aulaSeleccionadaId() !== null && this.aulaSeleccionadaId()! > 0);
  tieneCuotaSeleccionada = computed(() => this.cuotaSeleccionadaId() !== null && this.cuotaSeleccionadaId()! > 0);

  // Cobros filtrados por PENDIENTE/COMPLETO
  cobrosFiltrados = computed(() => {
    const lista = this.cobrosEstudiantes();
    const filtro = this.filtroEstado();

    if (filtro === 'TODOS') return lista;
    return lista.filter(c => c.estadoPago === filtro);
  });

  pagoForm: FormGroup = this.fb.group({
    montoAbonado: [0, [Validators.required, Validators.min(0.10)]],
    formaPago: ['YAPE', Validators.required]
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
    this.cuotaSeleccionadaId.set(null);
    this.aulas.set([]);
    this.cuotas.set([]);
    this.cobrosEstudiantes.set([]);

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
    this.cuotaSeleccionadaId.set(null);
    this.cuotas.set([]);
    this.cobrosEstudiantes.set([]);

    if (aulaId && aulaId > 0) {
      this.cargarCuotasPorAula(aulaId);
    }
  }

  cargarCuotasPorAula(aulaId: number): void {
    this.cargandoCuotas.set(true);
    this.cuotaService.obtenerPorAula(aulaId).subscribe({
      next: (data) => {
        this.cuotas.set(data);
        this.cargandoCuotas.set(false);
      },
      error: () => this.cargandoCuotas.set(false)
    });
  }

  onCuotaChange(event: Event): void {
    const cuotaId = Number((event.target as HTMLSelectElement).value) || null;
    this.cuotaSeleccionadaId.set(cuotaId);

    if (cuotaId && cuotaId > 0) {
      this.cargarCobros(cuotaId);
    } else {
      this.cobrosEstudiantes.set([]);
    }
  }

  cargarCobros(cuotaId: number): void {
    this.cargando.set(true);
    this.cuotaService.obtenerCobrosPorCuota(cuotaId).subscribe({
      next: (data) => {
        this.cobrosEstudiantes.set(data);
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false)
    });
  }

  // Marcar como pago completo rápido con un solo clic (Ej. Pago total con Yape)
  pagoRapidoCompleto(item: CuotaEstudianteCobro): void {
    const montoFaltante = item.montoAsignado - item.montoPagado;
    this.cuotaService.registrarPagoManual({
      cuotaDetalleId: item.cuotaDetalleId,
      montoAbonado: montoFaltante,
      formaPago: 'YAPE'
    }).subscribe({
      next: () => {
        Swal.fire({
          icon: 'success',
          title: '¡Pago Registrado!',
          text: `Se canceló la cuota de ${item.estudianteNombreCompleto}.`,
          timer: 1500,
          showConfirmButton: false
        });
        this.cargarCobros(this.cuotaSeleccionadaId()!);
      }
    });
  }

  abrirModalPagoParcial(item: CuotaEstudianteCobro): void {
    this.estudianteCobroModal.set(item);
    
    // Calculamos el saldo máximo exacto que le falta pagar
    const saldoPendiente = item.montoAsignado - item.montoPagado;
    this.montoMaximoAbono.set(saldoPendiente);

    // Asignamos validación dinámica de max(saldoPendiente)
    this.pagoForm.reset({ 
      montoAbonado: saldoPendiente, 
      formaPago: 'YAPE' 
    });

    this.pagoForm.controls['montoAbonado'].setValidators([
      Validators.required,
      Validators.min(0.10),
      Validators.max(saldoPendiente) // 👈 No permite superar el saldo restante
    ]);
    this.pagoForm.controls['montoAbonado'].updateValueAndValidity();

    this.mostrarModalPago.set(true);
  }

  confirmarPagoModal(): void {
    const comp = this.estudianteCobroModal();
    if (this.pagoForm.invalid || !comp) return;

    this.cuotaService.registrarPagoManual({
      cuotaDetalleId: comp.cuotaDetalleId,
      montoAbonado: this.pagoForm.value.montoAbonado,
      formaPago: this.pagoForm.value.formaPago
    }).subscribe({
      next: () => {
        this.cerrarModal();
        Swal.fire({
          icon: 'success',
          title: 'Abono Guardado',
          text: 'El abono fue registrado exitosamente.',
          timer: 1500,
          showConfirmButton: false
        });
        this.cargarCobros(this.cuotaSeleccionadaId()!);
      }
    });
  }

  anularPago(item: CuotaEstudianteCobro): void {
    Swal.fire({
      title: '¿Revertir Pago?',
      text: `El estado de ${item.estudianteNombreCompleto} volverá a PENDIENTE.`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#e11d48',
      cancelButtonColor: '#64748b',
      confirmButtonText: 'Sí, Revertir',
      cancelButtonText: 'Cancelar',
      allowOutsideClick: false,
      allowEscapeKey: false
    }).then((result) => {
      if (result.isConfirmed) {
        this.cuotaService.anularPago(item.cuotaDetalleId).subscribe({
          next: () => {
            Swal.fire({
              icon: 'success',
              title: 'Pago Revertido',
              text: 'El estado volvió a pendiente.',
              timer: 1500,
              showConfirmButton: false
            });
            this.cargarCobros(this.cuotaSeleccionadaId()!);
          }
        });
      }
    });
  }

  cerrarModal(): void {
    this.mostrarModalPago.set(false);
    this.estudianteCobroModal.set(null);
  }
}
