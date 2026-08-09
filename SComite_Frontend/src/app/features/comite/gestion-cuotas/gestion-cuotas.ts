import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CuotaService } from '../../../core/services/cuota.service';
import { Cuota, EstudiantePendienteCuota } from '../../../core/models/cuota.model';
import { AulaService } from '../../../core/services/aula.service';
import { PeriodoLectivo } from '../../../core/models/periodoLectivo.model';
import { Aula } from '../../../core/models/aula.model';
import { ActividadService } from '../../../core/services/actividad.service';
import { ActividadComite } from '../../../core/models/actividad.model';
import Swal from 'sweetalert2';
import { PdfExporterService } from '../../../core/services/pdf-exporter.service';
import { InstitucionEducativa } from '../../../core/models/institucion.model';
import { InstitucionService } from '../../../core/services/institucion.service';
import { Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'app-gestion-cuotas',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './gestion-cuotas.html',
  styleUrl: './gestion-cuotas.scss',
})
export class GestionCuotasComponent implements OnInit {
  private destroyRef = inject(DestroyRef);
  private reiniciarCarga$ = new Subject<void>();
  private fb = inject(FormBuilder);
  private cuotaService = inject(CuotaService);
  private aulaService = inject(AulaService);
  private actividadService = inject(ActividadService);
  private pdfExporter = inject(PdfExporterService);
  private institucionService = inject(InstitucionService);

  constructor() {
    this.destroyRef.onDestroy(() => this.reiniciarCarga$.complete());
  }

  periodos = signal<PeriodoLectivo[]>([]);
  aulas = signal<Aula[]>([]);
  cuotas = signal<Cuota[]>([]);
  actividades = signal<ActividadComite[]>([]);

  periodoSeleccionadoId = signal<number | null>(null);
  aulaSeleccionadaId = signal<number | null>(null);
  filtroTipoCuota = signal<string>('TODOS');

  cargando = signal<boolean>(false);
  cargandoAulas = signal<boolean>(false);
  mostrarModal = signal<boolean>(false);
  tipoModal = signal<'EVENTUAL' | 'MENSUAL'>('EVENTUAL');

  tienePeriodoSeleccionado = computed(() => this.periodoSeleccionadoId() !== null && this.periodoSeleccionadoId()! > 0);
  tieneAulaSeleccionada = computed(() => this.aulaSeleccionadaId() !== null && this.aulaSeleccionadaId()! > 0);
  puedeCrearCuota = computed(() => this.aulaSeleccionadaId() !== null);

  cuotasFiltradas = computed(() => {
    const lista = this.cuotas();
    const filtro = this.filtroTipoCuota();

    if (filtro === 'TODOS') return lista;
    return lista.filter(c => c.tipoCuota === filtro);
  });

  cuotaForm: FormGroup = this.fb.group({
    actividadId: [null],
    concepto: ['', [Validators.required, Validators.maxLength(150)]],
    montoIndividual: [0, [Validators.required, Validators.min(1)]],
    fechaVencimiento: ['', Validators.required],
    observacion: ['']
  });

  cuotaMensualForm: FormGroup = this.fb.group({
    conceptoBase: ['Aporte Fondo de Caja Chica', [Validators.required, Validators.maxLength(100)]],
    montoMensual: [10, [Validators.required, Validators.min(1)]],
    mesInicio: [3, [Validators.required, Validators.min(3), Validators.max(12)]],
    diaVencimiento: [10, [Validators.required, Validators.min(1), Validators.max(28)]]
  });

  modalMorososAbierto = signal<boolean>(false);
  cargandoMorosos = signal<boolean>(false);
  cuotaSeleccionadaMorosos = signal<Cuota | null>(null);
  estudiantesMorosos = signal<EstudiantePendienteCuota[]>([]);

  descargandoPdfMorosos = signal<boolean>(false);
  institucion = signal<InstitucionEducativa | null>(null);

  ngOnInit(): void {
    this.cargarPeriodos();
    this.cargarDatosInstitucion();
  }

  cargarDatosInstitucion(): void {
      this.institucionService.getConfiguracion().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (data) => {
          if (data) this.institucion.set(data);
        },
        error: (err) => Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar los datos de la institución educativa.', 'error')
      });
    }

  async exportarPdfMorosos(): Promise<void> {
    const cuota = this.cuotaSeleccionadaMorosos();
    const morosos = this.estudiantesMorosos();

    if (!cuota || morosos.length === 0) return;

    const conceptoLimpio = cuota.concepto.replace(/[^a-zA-Z0-9]/g, '_') || 'Cuota';
    const nombreArchivo = `Pendientes_${conceptoLimpio}.pdf`;

    this.descargandoPdfMorosos.set(true);
    try {
      await this.pdfExporter.exportarReporteMorosos({
        nombreArchivo,
        nombreInstitucion: this.institucion()?.nombreInstitucion,
        urlLogo: this.institucion()?.urlLogo,
        conceptoCuota: cuota.concepto,
        montoCuota: cuota.montoIndividual,
        totalPendientes: morosos.length,
        estudiantes: morosos.map(item => ({
          nombreEstudiante: item.nombreEstudiante,
          documentoEstudiante: `${item.tipoDocumento}: ${item.numeroDocumento}`,
          nombreApoderado: item.nombreApoderado,
          telefonoApoderado: item.telefonoApoderado,
          montoPendiente: item.montoPendiente
        })),
        fechaEmision: new Date() // Fecha y hora exacta actual
      });
    } catch {
      Swal.fire('Error', 'No se pudo generar el reporte en PDF.', 'error');
    } finally {
      this.descargandoPdfMorosos.set(false);
    }
  }

  cargarPeriodos(): void {
    this.aulaService.getPeriodos().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => this.periodos.set(data),
      error: (err) => Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar los periodos lectivos.', 'error')
    });
  }

  onPeriodoChange(event: Event): void {
    this.reiniciarCarga$.next();
    const value = (event.target as HTMLSelectElement).value;
    const id = value ? Number(value) : null;
    
    this.periodoSeleccionadoId.set(id);
    this.aulaSeleccionadaId.set(null);
    this.aulas.set([]);
    this.cuotas.set([]);
    this.actividades.set([]);

    if (id && id > 0) {
      this.cargarAulasPorPeriodo(id);
    }
  }

  cargarAulasPorPeriodo(periodoId: number): void {
    this.cargandoAulas.set(true);
    this.aulaService.getAulas(periodoId).pipe(takeUntil(this.reiniciarCarga$), takeUntilDestroyed(this.destroyRef)).subscribe({
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
    this.reiniciarCarga$.next();
    const value = (event.target as HTMLSelectElement).value;
    const aulaId = value ? Number(value) : null;

    this.aulaSeleccionadaId.set(aulaId);
    this.cuotas.set([]);
    this.actividades.set([]);

    if (aulaId && aulaId > 0) {
      this.cargarCuotas(aulaId);
      this.cargarActividadesDelAula(aulaId);
    }
  }

  cargarCuotas(aulaId: number): void {
    this.cargando.set(true);
    this.cuotaService.obtenerPorAula(aulaId).pipe(takeUntil(this.reiniciarCarga$), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.cuotas.set(data);
        this.cargando.set(false);
      },
      error: (err) => {
        this.cargando.set(false);
        Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar las cuotas.', 'error');
      }
    });
  }

  cargarActividadesDelAula(aulaId: number): void {
    const periodoObj = this.periodos().find(p => p.id === this.periodoSeleccionadoId());
    const anio = periodoObj ? periodoObj.anio : new Date().getFullYear();

    this.actividadService.getActividadesPorAula(aulaId, anio).pipe(takeUntil(this.reiniciarCarga$), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => this.actividades.set(data),
      error: (err) => Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar las actividades.', 'error')
    });
  }

  onActividadChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    const actividadId = value ? Number(value) : null;

    if (actividadId) {
      const act = this.actividades().find(a => a.id === actividadId);
      if (act) {
        // Formatear fecha y autocompletar campos
        const fechaFormatted = new Date(act.fechaProgramada).toISOString().split('T')[0];

        this.cuotaForm.patchValue({
          concepto: `Cuota: ${act.nombreActividad}`,
          montoIndividual: act.cuotaSugeridaPorAlumno,
          fechaVencimiento: fechaFormatted,
          observacion: `VINCULADA A ACTIVIDAD: ${act.nombreActividad}. ${act.descripcion || ''}`
        });

        // Deshabilitar edición directa de los datos vinculados
        this.cuotaForm.get('concepto')?.disable();
        this.cuotaForm.get('montoIndividual')?.disable();
        this.cuotaForm.get('fechaVencimiento')?.disable();
      }
    } else {
      // Si se desmarca, habilitar edición libre
      this.cuotaForm.get('concepto')?.enable();
      this.cuotaForm.get('montoIndividual')?.enable();
      this.cuotaForm.get('fechaVencimiento')?.enable();

      this.cuotaForm.patchValue({
        concepto: '',
        montoIndividual: 0,
        fechaVencimiento: '',
        observacion: ''
      });
    }
  }

  guardarCuota(): void {
    const aulaId = this.aulaSeleccionadaId();
    if (this.cuotaForm.invalid || !aulaId) return;

    const rawValue = this.cuotaForm.getRawValue();

    const payload = {
      ...rawValue,
      aulaId: aulaId
    };

    this.cuotaService.crear(payload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.cerrarModal();
        this.cargarCuotas(aulaId);
      },
      error: (err) => Swal.fire('Error', err.error?.mensaje || 'No se pudo crear la cuota.', 'error')
    });
  }

  guardarCuotaMensual(): void {
    const aulaId = this.aulaSeleccionadaId();
    const periodoId = this.periodoSeleccionadoId();
    if (this.cuotaMensualForm.invalid || !aulaId || !periodoId) return;

    const periodoObj = this.periodos().find(p => p.id === periodoId);
    const anio = periodoObj ? periodoObj.anio : new Date().getFullYear();

    const payload = {
      ...this.cuotaMensualForm.value,
      aulaId: aulaId,
      anioLectivo: anio
    };

    this.cuotaService.programarMensual(payload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.cerrarModal();
        this.cargarCuotas(aulaId);
      },
      error: (err) => Swal.fire('Error', err.error?.mensaje || 'No se pudo programar la cuota mensual.', 'error')
    });
  }

  abrirModal(): void {
    if (!this.puedeCrearCuota()) return;

    this.cuotaForm.get('concepto')?.enable();
    this.cuotaForm.get('montoIndividual')?.enable();
    this.cuotaForm.get('fechaVencimiento')?.enable();

    this.cuotaForm.reset({ actividadId: null, montoIndividual: 0 });
    
    this.cuotaMensualForm.reset({
      conceptoBase: 'Aporte Fondo de Caja Chica',
      montoMensual: 10,
      mesInicio: 3,
      diaVencimiento: 10
    });

    this.tipoModal.set('EVENTUAL');
    this.mostrarModal.set(true);
  }

  cerrarModal(): void {
    this.mostrarModal.set(false);
  }

  abrirModalMorosos(cuota: Cuota): void {
    this.cuotaSeleccionadaMorosos.set(cuota);
    this.estudiantesMorosos.set([]);
    this.cargandoMorosos.set(true);
    this.modalMorososAbierto.set(true);

    this.cuotaService.obtenerPendientesPorCuota(cuota.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.estudiantesMorosos.set(data);
          this.cargandoMorosos.set(false);
        },
        error: (err) => {
          this.cargandoMorosos.set(false);
          Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar los estudiantes pendientes.', 'error');
        }
      });
  }

  cerrarModalMorosos(): void {
    this.modalMorososAbierto.set(false);
    this.cuotaSeleccionadaMorosos.set(null);
    this.estudiantesMorosos.set([]);
  }
}
