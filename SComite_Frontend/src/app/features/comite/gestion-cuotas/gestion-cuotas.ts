import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CuotaService } from '../../../core/services/cuota.service';
import { Cuota } from '../../../core/models/cuota.model';
import { AulaService } from '../../../core/services/aula.service';
import { PeriodoLectivo } from '../../../core/models/periodoLectivo.model';
import { Aula } from '../../../core/models/aula.model';

@Component({
  selector: 'app-gestion-cuotas',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './gestion-cuotas.html',
  styleUrl: './gestion-cuotas.scss',
})
export class GestionCuotasComponent implements OnInit {
  private fb = inject(FormBuilder);
  private cuotaService = inject(CuotaService);
  private aulaService = inject(AulaService);

  periodos = signal<PeriodoLectivo[]>([]);
  aulas = signal<Aula[]>([]);
  cuotas = signal<Cuota[]>([]);

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

  ngOnInit(): void {
    this.cargarPeriodos();
  }

  cargarPeriodos(): void {
    this.aulaService.getPeriodos().subscribe({
      next: (data) => this.periodos.set(data)
    });
  }

  onPeriodoChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    const id = value ? Number(value) : null;
    
    this.periodoSeleccionadoId.set(id);
    this.aulaSeleccionadaId.set(null);
    this.aulas.set([]);
    this.cuotas.set([]);

    if (id && id > 0) {
      this.cargarAulasPorPeriodo(id);
    }
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
    const value = (event.target as HTMLSelectElement).value;
    const aulaId = value ? Number(value) : null;

    this.aulaSeleccionadaId.set(aulaId);
    this.cuotas.set([]);

    if (aulaId && aulaId > 0) {
      this.cargarCuotas(aulaId);
    }
  }

  cargarCuotas(aulaId: number): void {
    this.cargando.set(true);
    this.cuotaService.obtenerPorAula(aulaId).subscribe({
      next: (data) => {
        this.cuotas.set(data);
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false)
    });
  }

  guardarCuota(): void {
    const aulaId = this.aulaSeleccionadaId();
    if (this.cuotaForm.invalid || !aulaId) return;

    const payload = {
      ...this.cuotaForm.value,
      aulaId: aulaId
    };

    this.cuotaService.crear(payload).subscribe({
      next: () => {
        this.cerrarModal();
        this.cargarCuotas(aulaId);
      }
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

    this.cuotaService.programarMensual(payload).subscribe({
      next: () => {
        this.cerrarModal();
        this.cargarCuotas(aulaId);
      }
    });
  }

  abrirModal(): void {
    if (!this.puedeCrearCuota()) return;
    this.cuotaForm.reset({ montoIndividual: 0 });
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
}
