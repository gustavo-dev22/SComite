import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, takeUntil } from 'rxjs';

const BADGES_CARGO: Record<string, string> = {
  'PRESIDENTE': 'bg-purple-100 text-purple-800 border-purple-200',
  'TESORERO': 'bg-emerald-100 text-emerald-800 border-emerald-200',
  'SECRETARIO': 'bg-blue-100 text-blue-800 border-blue-200',
  'VOCAL': 'bg-amber-100 text-amber-800 border-amber-200'
};
import { ComiteService } from '../../../core/services/comite.service';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { PeriodoLectivo } from '../../../core/models/periodoLectivo.model';
import { Aula } from '../../../core/models/aula.model';
import { ComiteIntegrante, UsuarioSasi } from '../../../core/models/comiteIntegrante.model';
import { BasePeriodosComponent } from '../../../core/base/base-periodos.component';
import { manejarErrorHttp } from '../../../core/utils/http-error.util';
import Swal from 'sweetalert2';
import { CommonModule } from '@angular/common';
import { ModalA11yDirective } from '../../../shared/directives/modal-a11y.directive';

@Component({
  selector: 'app-asignacion-comite',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ReactiveFormsModule, ModalA11yDirective],
  templateUrl: './asignacion-comite.html',
  styleUrl: './asignacion-comite.scss',
})
export class AsignacionComiteComponent extends BasePeriodosComponent implements OnInit {
  private reiniciarCarga$ = new Subject<void>();
  private comiteService = inject(ComiteService);
  private fb = inject(FormBuilder);

  constructor() {
    super();
    this.destroyRef.onDestroy(() => this.reiniciarCarga$.complete());
  }

  aulas = signal<Aula[]>([]);
  integrantes = signal<ComiteIntegrante[]>([]);
  apoderadosSasi = signal<UsuarioSasi[]>([]);
  // SASI-DOWN: indica si el catálogo de apoderados de SASI está disponible.
  sasiDisponible = signal<boolean>(true);

  periodoSeleccionado = signal<number | null>(null);
  aulaSeleccionada = signal<number | null>(null);

  cargando = signal<boolean>(false);
  guardando = signal<boolean>(false);
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

  protected override onPeriodosCargados(data: PeriodoLectivo[]): void {
    const periodoActual = this.buscarPeriodoVigente(data);
    if (periodoActual) {
      this.periodoSeleccionado.set(periodoActual.id);
      this.cargarAulasPorPeriodo(periodoActual.id);
    }
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
    }, (err) => manejarErrorHttp(err, 'No se pudieron cargar las aulas.'));
  }

  cargarApoderadosSasi(): void {
    this.comiteService.getApoderadosSasi().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        const ordenados = [...data].sort((a, b) =>
          a.nombreCompleto.localeCompare(b.nombreCompleto, 'es', { sensitivity: 'base' })
        );

        this.apoderadosSasi.set(ordenados);
        this.sasiDisponible.set(true);
      },
      error: (err) => {
        // SASI-DOWN: si SASI no está disponible (503) o hubo error de conexión (0),
        // se avisa de forma amigable y se bloquea la asignación de integrantes.
        const esSasiNoDisponible =
          (err as { status?: number } | null)?.status === 503 ||
          (err as { status?: number } | null)?.status === 0;

        this.sasiDisponible.set(!esSasiNoDisponible);

        manejarErrorHttp(err, 'No se pudieron cargar los apoderados de SASI.');
      }
    });
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
        manejarErrorHttp(err, 'No se pudieron cargar los integrantes del comité.');
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

    // SASI-DOWN: se re-consulta el catálogo de apoderados en el momento de asignar
    // (no solo al cargar la página). Así, si SASI se cayó después de entrar al sistema,
    // el usuario recibe el aviso automáticamente al abrir el modal, sin recargar.
    this.cargarApoderadosSasi();

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
    if (this.guardando()) return;
    this.guardando.set(true);

    // SASI-DOWN (CRÍTICO): se re-consulta el catálogo de SASI en el MOMENTO de guardar,
    // en lugar de confiar en la señal cacheada de cuando se abrió el modal. Así, si SASI se
    // cayó mientras el modal estaba abierto, la asignación se bloquea con mensaje amigable.
    this.comiteService.getApoderadosSasi().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (apoderados) => {
        this.sasiDisponible.set(true);
        this.apoderadosSasi.set([...apoderados].sort((a, b) =>
          a.nombreCompleto.localeCompare(b.nombreCompleto, 'es', { sensitivity: 'base' })
        ));

        const apoderado = apoderados.find(a => a.usuarioId === this.comiteForm.value.usuarioIdSasi);
        if (!apoderado) {
          this.guardando.set(false);
          Swal.fire({
            icon: 'warning',
            title: 'Apoderado no disponible',
            text: 'El apoderado seleccionado ya no está disponible en el servicio SASI. Vuelva a seleccionarlo e intente nuevamente.',
            confirmButtonColor: '#2563eb',
            confirmButtonText: 'Entendido'
          });
          return;
        }

        this.ejecutarAsignacion(apoderado);
      },
      error: (err) => {
        this.guardando.set(false);

        const esSasiNoDisponible =
          (err as { status?: number } | null)?.status === 503 ||
          (err as { status?: number } | null)?.status === 0;

        this.sasiDisponible.set(!esSasiNoDisponible);

        if (esSasiNoDisponible) {
          Swal.fire({
            icon: 'warning',
            title: 'SASI no disponible',
            text: 'El servicio de autenticación (SASI) no está disponible. No se pudo asignar el cargo. Intente nuevamente en unos minutos.',
            confirmButtonColor: '#2563eb',
            confirmButtonText: 'Entendido'
          });
          return;
        }

        manejarErrorHttp(err, 'Error al asignar.');
      }
    });
  }

  private ejecutarAsignacion(apoderado: UsuarioSasi): void {
    const payload = {
      aulaId: this.aulaSeleccionada()!,
      usuarioIdSasi: apoderado.usuarioId,
      nombreCompleto: apoderado.nombreCompleto,
      email: apoderado.email,
      cargo: this.comiteForm.value.cargo
    };

    this.comiteService.asignarIntegrante(payload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.guardando.set(false);
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
      error: (err) => {
        this.guardando.set(false);
        manejarErrorHttp(err, 'Error al asignar.');
      }
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
          error: (err) => manejarErrorHttp(err, 'No se pudo remover.')
        });
      }
    });
  }

  getCargoBadgeClass(cargo: string): string {
    return BADGES_CARGO[cargo.toUpperCase()] || 'bg-amber-100 text-amber-800 border-amber-200';
  }
}
