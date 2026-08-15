import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, takeUntil } from 'rxjs';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CuotaService } from '../../../core/services/cuota.service';
import { manejarErrorHttp } from '../../../core/utils/http-error.util';
import { normalizarTelefonoPeru } from '../../../core/utils/whatsapp.util';
import { Aula } from '../../../core/models/aula.model';
import { Cuota, CuotaEstudianteCobro, EstadoPago } from '../../../core/models/cuota.model';
import { BasePeriodosComponent } from '../../../core/base/base-periodos.component';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-validar-comprobantes',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './validar-comprobantes.html',
  styleUrl: './validar-comprobantes.scss',
})
export class ValidarComprobantesComponent extends BasePeriodosComponent implements OnInit {
  private reiniciarCarga$ = new Subject<void>();
  private fb = inject(FormBuilder);
  private cuotaService = inject(CuotaService);

  constructor() {
    super();
    this.destroyRef.onDestroy(() => this.reiniciarCarga$.complete());
  }

  // Listas
  aulas = signal<Aula[]>([]);
  cuotas = signal<Cuota[]>([]);
  cobrosEstudiantes = signal<CuotaEstudianteCobro[]>([]);

  // Filtros
  periodoSeleccionadoId = signal<number | null>(null);
  aulaSeleccionadaId = signal<number | null>(null);
  cuotaSeleccionadaId = signal<number | null>(null);
  filtroEstado = signal<'TODOS' | EstadoPago>('TODOS');

  cargando = signal<boolean>(false);
  cargandoAulas = signal<boolean>(false);
  cargandoCuotas = signal<boolean>(false);
  registrandoPago = signal<boolean>(false);
  anulandoPago = signal<boolean>(false);
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
    if (filtro === 'PENDIENTE') return lista.filter(c => c.estadoPago === 'PENDIENTE' || c.estadoPago === 'PARCIAL');
    return lista.filter(c => c.estadoPago === filtro);
  });

  cantidadPendientes = computed(() => this.cobrosEstudiantes().filter(c => c.estadoPago === 'PENDIENTE' || c.estadoPago === 'PARCIAL').length);
  cantidadPagados = computed(() => this.cobrosEstudiantes().filter(c => c.estadoPago === 'COMPLETO').length);
  cantidadExonerados = computed(() => this.cobrosEstudiantes().filter(c => c.estadoPago === 'EXONERADO').length);

  pagoForm: FormGroup = this.fb.group({
    montoAbonado: [0, [Validators.required, Validators.min(0.10)]],
    formaPago: ['YAPE', Validators.required]
  });

  // 🚀 Obtener la cuota seleccionada actualmente para conocer su estado
  cuotaActual = computed(() => {
    const id = this.cuotaSeleccionadaId();
    return this.cuotas().find(c => c.id === id) || null;
  });

  // 🚀 Saber si la cuota seleccionada está CERRADA / Saneada
  cuotaEstaCerrada = computed(() => {
    return this.cuotaActual()?.estado === 'CERRADA';
  });

  ngOnInit(): void {
    this.cargarPeriodos();
  }

  onPeriodoChange(event: Event): void {
    this.reiniciarCarga$.next();
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
    this.aulaService.getMisAulas(periodoId).pipe(takeUntil(this.reiniciarCarga$), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.aulas.set(data);
        this.cargandoAulas.set(false);
      },
      error: (err) => {
        this.cargandoAulas.set(false);
        manejarErrorHttp(err, 'No se pudieron cargar las aulas.');
      }
    });
  }

  onAulaChange(event: Event): void {
    this.reiniciarCarga$.next();
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
    this.cuotaService.obtenerPorAula(aulaId).pipe(takeUntil(this.reiniciarCarga$), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.cuotas.set(data);
        this.cargandoCuotas.set(false);
      },
      error: (err) => {
        this.cargandoCuotas.set(false);
        manejarErrorHttp(err, 'No se pudieron cargar las cuotas.');
      }
    });
  }

  onCuotaChange(event: Event): void {
    this.reiniciarCarga$.next();
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
    this.cuotaService.obtenerCobrosPorCuota(cuotaId).pipe(takeUntil(this.reiniciarCarga$), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.cobrosEstudiantes.set(data);
        this.cargando.set(false);
      },
      error: (err) => {
        this.cargando.set(false);
        manejarErrorHttp(err, 'No se pudieron cargar los cobros de la cuota.');
      }
    });
  }

  // Marcar como pago completo rápido con un solo clic (Ej. Pago total con Yape)
  pagoRapidoCompleto(item: CuotaEstudianteCobro): void {
    if (this.registrandoPago()) return;
    this.registrandoPago.set(true);
    const montoFaltante = item.montoAsignado - item.montoPagado;
    this.cuotaService.registrarPagoManual({
      cuotaDetalleId: item.cuotaDetalleId,
      montoAbonado: montoFaltante,
      formaPago: 'YAPE'
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.registrandoPago.set(false);
        Swal.fire({
          icon: 'success',
          title: '¡Pago Registrado!',
          text: `Se canceló la cuota de ${item.estudianteNombreCompleto}.`,
          timer: 1500,
          showConfirmButton: false
        });
        this.recargarCobros();
      },
      error: (err) => {
        this.registrandoPago.set(false);
        manejarErrorHttp(err, 'No se pudo registrar el pago.');
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
      Validators.min(Math.min(0.10, saldoPendiente)),
      Validators.max(saldoPendiente) // 👈 No permite superar el saldo restante
    ]);
    this.pagoForm.controls['montoAbonado'].updateValueAndValidity();

    this.mostrarModalPago.set(true);
  }

  confirmarPagoModal(): void {
    const comp = this.estudianteCobroModal();
    if (this.pagoForm.invalid || !comp) return;
    if (this.registrandoPago()) return;
    this.registrandoPago.set(true);

    this.cuotaService.registrarPagoManual({
      cuotaDetalleId: comp.cuotaDetalleId,
      montoAbonado: this.pagoForm.value.montoAbonado,
      formaPago: this.pagoForm.value.formaPago
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.registrandoPago.set(false);
        this.cerrarModal();
        Swal.fire({
          icon: 'success',
          title: 'Abono Guardado',
          text: 'El abono fue registrado exitosamente.',
          timer: 1500,
          showConfirmButton: false
        });
        this.recargarCobros();
      },
      error: (err) => {
        this.registrandoPago.set(false);
        manejarErrorHttp(err, 'No se pudo guardar el abono.');
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
        if (this.anulandoPago()) return;
        this.anulandoPago.set(true);
        this.cuotaService.anularPago(item.cuotaDetalleId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
          next: () => {
            this.anulandoPago.set(false);
            Swal.fire({
              icon: 'success',
              title: 'Pago Revertido',
              text: 'El estado volvió a pendiente.',
              timer: 1500,
              showConfirmButton: false
            });
            this.recargarCobros();
          },
          error: (err) => {
            this.anulandoPago.set(false);
            manejarErrorHttp(err, 'No se pudo revertir el pago.');
          }
        });
      }
    });
  }

  recargarCobros(): void {
    const cuotaId = this.cuotaSeleccionadaId();
    if (cuotaId && cuotaId > 0) {
      this.reiniciarCarga$.next();
      this.cargarCobros(cuotaId);
    }
  }

  cerrarModal(): void {
    this.mostrarModalPago.set(false);
    this.estudianteCobroModal.set(null);
  }

  obtenerLinkWhatsApp(telefono: string | null | undefined): string | null {
    const normalizado = normalizarTelefonoPeru(telefono);
    return normalizado ? `https://wa.me/${normalizado}` : null;
  }
}
