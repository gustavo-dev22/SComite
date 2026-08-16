import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, Subject, takeUntil } from 'rxjs';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { GastoService } from '../../../core/services/gasto.service';
import { Aula } from '../../../core/models/aula.model';
import { GastoComite, ResumenCajaAula } from '../../../core/models/gasto.model';
import { manejarErrorHttp } from '../../../core/utils/http-error.util';
import { formatearFechaLocal, hoyLocal } from '../../../core/utils/fecha.util';
import { BasePeriodosComponent } from '../../../core/base/base-periodos.component';
import Swal from 'sweetalert2';
import { environment } from '../../../../environments/environment';
import { ModalA11yDirective } from '../../../shared/directives/modal-a11y.directive';

const MAX_COMPROBANTE_MB = 5;
const TIPOS_COMPROBANTE_MIME_PERMITIDOS = ['image/jpeg', 'image/png', 'image/webp', 'application/pdf'];
const EXTENSIONES_COMPROBANTE_PERMITIDAS = ['jpg', 'jpeg', 'png', 'webp', 'pdf'];

interface GastoForm {
  concepto: FormControl<string>;
  categoria: FormControl<string>;
  monto: FormControl<number>;
  fechaGasto: FormControl<string>;
  tipoComprobante: FormControl<string>;
  numeroComprobante: FormControl<string>;
  proveedor: FormControl<string>;
  observacion: FormControl<string>;
  urlComprobante: FormControl<string>;
}

@Component({
  selector: 'app-registro-gastos',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ReactiveFormsModule, ModalA11yDirective],
  templateUrl: './registro-gastos.html',
  styleUrl: './registro-gastos.scss',
})
export class RegistroGastosComponent extends BasePeriodosComponent implements OnInit {
  private reiniciarCarga$ = new Subject<void>();
  private fb = inject(FormBuilder).nonNullable;
  private gastoService = inject(GastoService);

  constructor() {
    super();
    this.destroyRef.onDestroy(() => this.reiniciarCarga$.complete());
  }

  // Listas
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
      if (!g.fechaGasto) return false;
      const fecha = new Date(g.fechaGasto);
      return (fecha.getMonth() + 1) === mes;
    });
  });

  esEdicion = signal<boolean>(false);
  gastoEditarId = signal<number | null>(null);
  subiendoArchivo = signal<boolean>(false);
  guardando = signal<boolean>(false);
  archivoSeleccionadoNombre = signal<string>('');

  gastoForm: FormGroup<GastoForm> = this.fb.group({
    concepto: this.fb.control('', [Validators.required, Validators.maxLength(150)]),
    categoria: this.fb.control('MATERIALES', [Validators.required]),
    monto: this.fb.control(0, [Validators.required, Validators.min(0.10)]),
    fechaGasto: this.fb.control(hoyLocal(), [Validators.required]),
    tipoComprobante: this.fb.control('BOLETA', [Validators.required]),
    numeroComprobante: this.fb.control(''),
    proveedor: this.fb.control(''),
    observacion: this.fb.control(''),
    urlComprobante: this.fb.control('')
  });

  ngOnInit(): void {
    this.cargarPeriodos();
  }

  onPeriodoChange(event: Event): void {
    this.reiniciarCarga$.next();
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

    if (aulaId && aulaId > 0) {
      this.cargarGastosYBalance(aulaId);
    } else {
      this.gastos.set([]);
      this.resetsResumen();
    }
  }

  onMesChange(event: Event): void {
    this.reiniciarCarga$.next();
    const mes = Number((event.target as HTMLSelectElement).value);
    this.mesSeleccionado.set(mes);

    const aulaId = this.aulaSeleccionadaId();
    if (aulaId) {
      this.cargarGastosYBalance(aulaId);
    }
  }

  cargarGastosYBalance(aulaId: number): void {
    this.reiniciarCarga$.next();
    this.cargando.set(true);

    const periodoId = this.periodoSeleccionadoId();
    const periodoObj = this.periodos().find(p => p.id === periodoId);
    const anio = periodoObj ? periodoObj.anio : new Date().getFullYear();
    const mes = this.mesSeleccionado();

    // 1. Cargar Balance Mensual con Arrastre
    this.gastoService.obtenerBalanceMensual(aulaId, anio, mes).pipe(takeUntil(this.reiniciarCarga$), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => this.resumenCaja.set(res),
      error: (err) => manejarErrorHttp(err, 'No se pudo cargar el balance mensual.')
    });

    // 2. Cargar Lista de Gastos
    this.gastoService.obtenerPorAula(aulaId).pipe(takeUntil(this.reiniciarCarga$), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.gastos.set(data);
        this.cargando.set(false);
      },
      error: (err) => {
        this.cargando.set(false);
        manejarErrorHttp(err, 'No se pudieron cargar los gastos.');
      }
    });
  }

  guardarGasto(): void {
    const aulaId = this.aulaSeleccionadaId();
    if (this.gastoForm.invalid || !aulaId) return;
    if (this.guardando()) return;
    this.guardando.set(true);

    const payload = { ...this.gastoForm.getRawValue(), aulaId };

    const request: Observable<unknown> = this.esEdicion()
      ? this.gastoService.actualizar(this.gastoEditarId()!, { ...payload, id: this.gastoEditarId()! })
      : this.gastoService.crear(payload);

    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.guardando.set(false);
        this.cerrarModal();
        Swal.fire({ icon: 'success', title: this.esEdicion() ? 'Gasto Actualizado' : 'Gasto Registrado', timer: 1500, showConfirmButton: false });
        this.cargarGastosYBalance(aulaId);
      },
      error: (err) => {
        this.guardando.set(false);
        manejarErrorHttp(err, this.esEdicion() ? 'No se pudo actualizar.' : 'No se pudo registrar.');
      }
    });
  }

  eliminarGasto(gasto: GastoComite): void {
    Swal.fire({
      title: '¿Eliminar Gasto?',
      text: `Se reincorporarán S/. ${(Number(gasto.monto) || 0).toFixed(2)} al saldo de caja.`,
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
          error: (err) => manejarErrorHttp(err, 'No se pudo eliminar el gasto.')
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
      fechaGasto: hoyLocal(),
      tipoComprobante: 'BOLETA',
      urlComprobante: ''
    });
    this.mostrarModal.set(true);
  }

  abrirModalEditar(g: GastoComite): void {
    this.esEdicion.set(true);
    this.gastoEditarId.set(g.id);
    this.archivoSeleccionadoNombre.set(g.urlComprobante ? 'Comprobante adjuntado' : '');

    const fechaFormat = formatearFechaLocal(g.fechaGasto);

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

    // 🚀 Validar tipo MIME / extensión permitida (JPG, PNG, WEBP, PDF)
    const extension = file.name.split('.').pop()?.toLowerCase() || '';
    const esFormatoValido =
      TIPOS_COMPROBANTE_MIME_PERMITIDOS.includes(file.type) ||
      EXTENSIONES_COMPROBANTE_PERMITIDAS.includes(extension);

    if (!esFormatoValido) {
      input.value = '';
      this.archivoSeleccionadoNombre.set('');
      Swal.fire('Formato no válido', 'Solo se permiten archivos JPG, PNG, WEBP o PDF.', 'warning');
      return;
    }

    // 🚀 Validar tamaño máximo (evita archivos gigantes y errores HTTP 413)
    const maxBytes = MAX_COMPROBANTE_MB * 1024 * 1024;
    if (file.size > maxBytes) {
      input.value = '';
      this.archivoSeleccionadoNombre.set('');
      Swal.fire('Archivo demasiado grande', `El comprobante no puede superar ${MAX_COMPROBANTE_MB} MB.`, 'warning');
      return;
    }

    this.archivoSeleccionadoNombre.set(file.name);
    this.subiendoArchivo.set(true);

    this.gastoService.subirArchivoComprobante(file).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        this.gastoForm.patchValue({ urlComprobante: res.urlComprobante });
        this.subiendoArchivo.set(false);
        Swal.fire({ icon: 'success', title: 'Archivo Subido', text: 'Comprobante cargado exitosamente.', timer: 1200, showConfirmButton: false });
      },
      error: (err) => {
        this.subiendoArchivo.set(false);
        manejarErrorHttp(err, 'No se pudo subir el archivo.');
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

    window.open(urlConCacheBuster, '_blank', 'noopener,noreferrer');
  }
}
