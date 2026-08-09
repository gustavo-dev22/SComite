import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, takeUntil } from 'rxjs';

const BADGES_CARGO: Record<string, string> = {
  'PRESIDENTE': 'bg-purple-100 text-purple-800 border-purple-200',
  'TESORERO': 'bg-emerald-100 text-emerald-800 border-emerald-200',
  'SECRETARIO': 'bg-blue-100 text-blue-800 border-blue-200',
  'VOCAL': 'bg-amber-100 text-amber-800 border-amber-200'
};
import { ComiteService } from '../../../core/services/comite.service';
import { AulaService } from '../../../core/services/aula.service';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { PeriodoLectivo } from '../../../core/models/periodoLectivo.model';
import { Aula } from '../../../core/models/aula.model';
import { ComiteIntegrante, UsuarioSasi } from '../../../core/models/comiteIntegrante.model';
import Swal from 'sweetalert2';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-asignacion-comite',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './asignacion-comite.html',
  styleUrl: './asignacion-comite.scss',
})
export class AsignacionComiteComponent implements OnInit {
  private destroyRef = inject(DestroyRef);
  private reiniciarCarga$ = new Subject<void>();
  private comiteService = inject(ComiteService);
  private aulaService = inject(AulaService);
  private fb = inject(FormBuilder);

  constructor() {
    this.destroyRef.onDestroy(() => this.reiniciarCarga$.complete());
  }

  periodos = signal<PeriodoLectivo[]>([]);
  aulas = signal<Aula[]>([]);
  integrantes = signal<ComiteIntegrante[]>([]);
  apoderadosSasi = signal<UsuarioSasi[]>([]);

  periodoSeleccionado = signal<number | null>(null);
  aulaSeleccionada = signal<number | null>(null);

  cargando = signal<boolean>(false);
  modalAbierto = signal<boolean>(false);

  cargosDisponibles = ['PRESIDENTE', 'TESORERO', 'SECRETARIO', 'VOCAL'];

  comiteForm: FormGroup = this.fb.group({
    cargo: ['', [Validators.required]],
    usuarioIdSasi: ['', [Validators.required]]
  });

  ngOnInit(): void {
    this.cargarPeriodos();
    this.cargarApoderadosSasi();
  }

  cargarPeriodos(): void {
    this.aulaService.getPeriodos().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(data => {
      this.periodos.set(data);

      if (data.length > 0) {
        const anioActualSistema = new Date().getFullYear(); // 2026

        // 1. Buscamos el periodo cuya propiedad 'anio' o número dentro de 'nombre' coincida con el año actual
        const periodoActual = data.find(p => {
          const anioExtraido = p.anio || Number(p.nombre.replace(/\D/g, '')); // Extrae los dígitos (ej: "Año Lectivo 2026" -> 2026)
          return anioExtraido === anioActualSistema;
        }) || data.find(p => p.esActivo) || data[0];

        // 2. Asignamos el ID del periodo 2026
        this.periodoSeleccionado.set(periodoActual.id);

        // 3. Cargamos de inmediato las aulas de ese periodo
        this.cargarAulasPorPeriodo(periodoActual.id);
      }
    }, (err) => Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar los periodos lectivos.', 'error'));
  }

  cargarAulasPorPeriodo(periodoId: number): void {
    this.aulaService.getAulas(periodoId).pipe(takeUntil(this.reiniciarCarga$), takeUntilDestroyed(this.destroyRef)).subscribe(data => {
      this.aulas.set(data);
      if (data.length > 0) {
        this.aulaSeleccionada.set(data[0].id);
        this.cargarComiteAula(data[0].id);
      } else {
        this.aulaSeleccionada.set(null);
        this.integrantes.set([]);
      }
    }, (err) => Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar las aulas.', 'error'));
  }

  cargarApoderadosSasi(): void {
    this.comiteService.getApoderadosSasi().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(data => {
      const ordenados = [...data].sort((a, b) => 
        a.nombreCompleto.localeCompare(b.nombreCompleto, 'es', { sensitivity: 'base' })
      );

      this.apoderadosSasi.set(ordenados);
    }, (err) => Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar los apoderados de SASI.', 'error'));
  }

  cargarComiteAula(aulaId: number): void {
    this.cargando.set(true);
    this.comiteService.getComitePorAula(aulaId).pipe(takeUntil(this.reiniciarCarga$), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        this.integrantes.set(data);
        this.cargando.set(false);
      },
      error: (err) => {
        this.cargando.set(false);
        Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar los integrantes del comité.', 'error');
      }
    });
  }

  onPeriodoChange(event: Event): void {
    this.reiniciarCarga$.next();
    const periodoId = Number((event.target as HTMLSelectElement).value);
    this.periodoSeleccionado.set(periodoId);
    this.cargarAulasPorPeriodo(periodoId);
  }

  onAulaChange(event: Event): void {
    this.reiniciarCarga$.next();
    const aulaId = Number((event.target as HTMLSelectElement).value);
    this.aulaSeleccionada.set(aulaId);
    this.cargarComiteAula(aulaId);
  }

  abrirModal(): void {
    if (!this.aulaSeleccionada()) {
      Swal.fire('Atención', 'Debe seleccionar un aula para asignar integrantes.', 'warning');
      return;
    }
    this.comiteForm.reset({ cargo: '', usuarioIdSasi: '' });
    this.modalAbierto.set(true);
  }

  cerrarModal(): void {
    this.modalAbierto.set(false);
  }

  guardarAsignacion(): void {
    if (this.comiteForm.invalid || !this.aulaSeleccionada()) {
      this.comiteForm.markAllAsTouched();
      return;
    }

    const apoderado = this.apoderadosSasi().find(a => a.usuarioId === this.comiteForm.value.usuarioIdSasi);
    if (!apoderado) return;

    const payload = {
      aulaId: this.aulaSeleccionada()!,
      usuarioIdSasi: apoderado.usuarioId,
      nombreCompleto: apoderado.nombreCompleto,
      email: apoderado.email,
      cargo: this.comiteForm.value.cargo
    };

    this.comiteService.asignarIntegrante(payload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        Swal.fire({
          icon: 'success',
          title: '¡Asignado!',
          text: `Cargo de ${payload.cargo} asignado con éxito.`,
          timer: 1500,
          showConfirmButton: false
        });
        this.cerrarModal();
        this.cargarComiteAula(this.aulaSeleccionada()!);
      },
      error: (err) => Swal.fire('Error', err.error?.mensaje || 'Error al asignar.', 'error')
    });
  }

  eliminarIntegrante(integrante: ComiteIntegrante): void {
    Swal.fire({
      title: '¿Remover Integrante?',
      text: `¿Estás seguro de remover a ${integrante.nombreCompleto} del cargo ${integrante.cargo}?`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#ef4444',
      cancelButtonColor: '#64748b',
      confirmButtonText: 'Sí, remover',
      cancelButtonText: 'Cancelar',
      allowOutsideClick: false,
      allowEscapeKey: false
    }).then((result) => {
      if (result.isConfirmed) {
        this.comiteService.eliminarIntegrante(integrante.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
          next: () => {
            Swal.fire('Removido', 'El integrante ha sido removido.', 'success');
            this.cargarComiteAula(this.aulaSeleccionada()!);
          },
          error: (err) => Swal.fire('Error', err.error?.mensaje || 'No se pudo remover.', 'error')
        });
      }
    });
  }

  getCargoBadgeClass(cargo: string): string {
    return BADGES_CARGO[cargo.toUpperCase()] || 'bg-amber-100 text-amber-800 border-amber-200';
  }
}
