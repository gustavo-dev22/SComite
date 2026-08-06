import { CommonModule } from '@angular/common';
import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { GastoService } from '../../../core/services/gasto.service';
import { AulaService } from '../../../core/services/aula.service';
import { PeriodoLectivo } from '../../../core/models/periodoLectivo.model';
import { Aula } from '../../../core/models/aula.model';
import { GastoComite, ResumenCajaAula } from '../../../core/models/gasto.model';
import Swal from 'sweetalert2';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-registro-gastos',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './registro-gastos.html',
  styleUrl: './registro-gastos.scss',
})
export class RegistroGastosComponent implements OnInit {
  private destroyRef = inject(DestroyRef);
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

  esEdicion = signal<boolean>(false);
  gastoEditarId = signal<number | null>(null);
  subiendoArchivo = signal<boolean>(false);
  archivoSeleccionadoNombre = signal<string>('');

  gastoForm: FormGroup = this.fb.group({
    concepto: ['', [Validators.required, Validators.maxLength(150)]],
    categoria: ['MATERIALES', Validators.required],
    monto: [0, [Validators.required, Validators.min(0.10)]],
    fechaGasto: [new Date().toISOString().substring(0, 10), Validators.required],
    tipoComprobante: ['BOLETA', Validators.required],
    numeroComprobante: [''],
    proveedor: [''],
    observacion: [''],
    urlComprobante: ['']
  });

  ngOnInit(): void {
    this.cargarPeriodos();
  }

  cargarPeriodos(): void {
    this.aulaService.getPeriodos().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => this.periodos.set(data),
      error: (err) => Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar los periodos lectivos.', 'error')
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
    this.aulaService.getAulas(periodoId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.aulas.set(data);
        this.cargandoAulas.set(false);
      },
      error: (err) => {
        this.cargandoAulas.set(false);
        Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar las aulas.', 'error');
      }
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
    this.gastoService.obtenerBalanceMensual(aulaId, anio, mes).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => this.resumenCaja.set(res),
      error: (err) => Swal.fire('Error', err.error?.mensaje || 'No se pudo cargar el balance mensual.', 'error')
    });

    // 2. Cargar Lista de Gastos
    this.gastoService.obtenerPorAula(aulaId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.gastos.set(data);
        this.cargando.set(false);
      },
      error: (err) => {
        this.cargando.set(false);
        Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar los gastos.', 'error');
      }
    });
  }

  guardarGasto(): void {
    const aulaId = this.aulaSeleccionadaId();
    if (this.gastoForm.invalid || !aulaId) return;

    const payload = { ...this.gastoForm.value, aulaId };

    if (this.esEdicion()) {
      const id = this.gastoEditarId()!;
      this.gastoService.actualizar(id, { ...payload, id }).subscribe({
        next: () => {
          this.cerrarModal();
          Swal.fire({ icon: 'success', title: 'Gasto Actualizado', timer: 1500, showConfirmButton: false });
          this.cargarGastosYBalance(aulaId);
        },
        error: (err) => Swal.fire('Error', err.error?.mensaje || 'No se pudo actualizar.', 'error')
      });
    } else {
      this.gastoService.crear(payload).subscribe({
        next: () => {
          this.cerrarModal();
          Swal.fire({ icon: 'success', title: 'Gasto Registrado', timer: 1500, showConfirmButton: false });
          this.cargarGastosYBalance(aulaId);
        },
        error: (err) => Swal.fire('Error', err.error?.mensaje || 'No se pudo registrar.', 'error')
      });
    }
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
        this.gastoService.eliminar(gasto.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
          next: () => {
            Swal.fire({
              icon: 'success',
              title: 'Gasto Eliminado',
              timer: 1500,
              showConfirmButton: false
            });
            this.cargarGastosYBalance(this.aulaSeleccionadaId()!);
          },
          error: (err) => Swal.fire('Error', err.error?.mensaje || 'No se pudo eliminar el gasto.', 'error')
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

  abrirModalCrear(): void {
    if (!this.puedeRegistrarGasto()) return;
    this.esEdicion.set(false);
    this.gastoEditarId.set(null);
    this.archivoSeleccionadoNombre.set('');
    
    this.gastoForm.reset({
      categoria: 'MATERIALES',
      monto: 0,
      fechaGasto: new Date().toISOString().substring(0, 10),
      tipoComprobante: 'BOLETA',
      urlComprobante: ''
    });
    this.mostrarModal.set(true);
  }

  abrirModalEditar(g: GastoComite): void {
    this.esEdicion.set(true);
    this.gastoEditarId.set(g.id);
    this.archivoSeleccionadoNombre.set(g.urlComprobante ? 'Comprobante adjuntado' : '');

    const fechaFormat = new Date(g.fechaGasto).toISOString().substring(0, 10);

    this.gastoForm.patchValue({
      concepto: g.concepto,
      categoria: g.categoria,
      monto: g.monto,
      fechaGasto: fechaFormat,
      tipoComprobante: g.tipoComprobante,
      numeroComprobante: g.numeroComprobante || '',
      proveedor: g.proveedor || '',
      observacion: g.observacion || '',
      urlComprobante: g.urlComprobante || ''
    });

    this.mostrarModal.set(true);
  }

  cerrarModal(): void {
    this.mostrarModal.set(false);
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    this.archivoSeleccionadoNombre.set(file.name);
    this.subiendoArchivo.set(true);

    this.gastoService.subirArchivoComprobante(file).subscribe({
      next: (res) => {
        this.gastoForm.patchValue({ urlComprobante: res.urlComprobante });
        this.subiendoArchivo.set(false);
        Swal.fire({ icon: 'success', title: 'Archivo Subido', text: 'Comprobante cargado exitosamente.', timer: 1200, showConfirmButton: false });
      },
      error: (err) => {
        this.subiendoArchivo.set(false);
        Swal.fire('Error', err.error?.mensaje || 'No se pudo subir el archivo.', 'error');
      }
    });
  }

  abrirComprobante(url: string | undefined): void {
    if (!url) return;

    let fullUrl = url;

    if (!url.startsWith('http://') && !url.startsWith('https://')) {
      const backendBaseUrl = environment.apiUrl.replace(/\/api\/?$/, '');
      const urlLimpia = url.startsWith('/') ? url : `/${url}`;
      fullUrl = `${backendBaseUrl}${urlLimpia}`;
    }

    // 🚀 CACHE-BUSTING: Agrega un timestamp único a la URL
    const timestamp = new Date().getTime();
    const separator = fullUrl.includes('?') ? '&' : '?';
    const urlConCacheBuster = `${fullUrl}${separator}_t=${timestamp}`;

    window.open(urlConCacheBuster, '_blank');
  }
}
