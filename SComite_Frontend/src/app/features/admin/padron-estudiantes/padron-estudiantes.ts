import { Component, inject, OnInit, signal } from '@angular/core';
import { EstudianteService } from '../../../core/services/estudiante.service';
import { AulaService } from '../../../core/services/aula.service';
import { ComiteService } from '../../../core/services/comite.service';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { PeriodoLectivo } from '../../../core/models/periodoLectivo.model';
import { Aula } from '../../../core/models/aula.model';
import { Estudiante } from '../../../core/models/estudiante.model';
import { UsuarioSasi } from '../../../core/models/comiteIntegrante.model';
import Swal from 'sweetalert2';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-padron-estudiantes',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './padron-estudiantes.html',
  styleUrl: './padron-estudiantes.scss',
})
export class PadronEstudiantesComponent implements OnInit {
  private estudianteService = inject(EstudianteService);
  private aulaService = inject(AulaService);
  private comiteService = inject(ComiteService);
  private fb = inject(FormBuilder);

  periodos = signal<PeriodoLectivo[]>([]);
  aulas = signal<Aula[]>([]);
  estudiantes = signal<Estudiante[]>([]);
  apoderadosSasi = signal<UsuarioSasi[]>([]);

  periodoSeleccionado = signal<number | null>(null);
  aulaSeleccionada = signal<number | null>(null);

  cargando = signal<boolean>(false);
  modalAbierto = signal<boolean>(false);
  esEdicion = signal<boolean>(false);

  estudianteForm: FormGroup = this.fb.group({
    id: [0],
    aulaId: [0],
    tipoDocumento: ['DNI', [Validators.required]],
    numeroDocumento: ['', [Validators.required, Validators.pattern('^[0-9]{8,12}$')]],
    nombres: ['', [Validators.required]],
    apellidoPaterno: ['', [Validators.required]],
    apellidoMaterno: ['', [Validators.required]],
    usuarioIdApoderadoSasi: [''],
    nombreApoderado: [''],
    telefonoApoderado: ['']
  });

  ngOnInit(): void {
    this.cargarPeriodos();
    this.cargarApoderadosSasi();
  }

  onInputToUppercase(controlName: string): void {
    const control = this.estudianteForm.get(controlName);
    if (control && control.value) {
      control.patchValue(control.value.toUpperCase(), { emitEvent: false });
    }
  }

  cargarPeriodos(): void {
    this.aulaService.getPeriodos().subscribe(data => {
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
    });
  }

  cargarAulasPorPeriodo(periodoId: number): void {
    this.aulaService.getAulas(periodoId).subscribe(data => {
      this.aulas.set(data);
      if (data.length > 0) {
        this.aulaSeleccionada.set(data[0].id);
        this.cargarEstudiantes(data[0].id);
      } else {
        this.aulaSeleccionada.set(null);
        this.estudiantes.set([]);
      }
    });
  }

  cargarApoderadosSasi(): void {
    this.comiteService.getApoderadosSasi().subscribe(data => {
      const ordenados = [...data].sort((a, b) => 
        a.nombreCompleto.localeCompare(b.nombreCompleto, 'es', { sensitivity: 'base' })
      );

      this.apoderadosSasi.set(ordenados);
    });
  }

  cargarEstudiantes(aulaId: number): void {
    this.cargando.set(true);
    this.estudianteService.getEstudiantesPorAula(aulaId).subscribe({
      next: (data) => {
        const ordenados = [...data].sort((a, b) => {
          const apellidoA = `${a.apellidoPaterno} ${a.apellidoMaterno}`.trim();
          const apellidoB = `${b.apellidoPaterno} ${b.apellidoMaterno}`.trim();

          return apellidoA.localeCompare(apellidoB, 'es', { sensitivity: 'base' });
        });

        this.estudiantes.set(ordenados);
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false)
    });
  }

  onPeriodoChange(event: Event): void {
    const periodoId = Number((event.target as HTMLSelectElement).value);
    this.periodoSeleccionado.set(periodoId);
    this.cargarAulasPorPeriodo(periodoId);
  }

  onAulaChange(event: Event): void {
    const aulaId = Number((event.target as HTMLSelectElement).value);
    this.aulaSeleccionada.set(aulaId);
    this.cargarEstudiantes(aulaId);
  }

  onApoderadoSelectChange(event: Event): void {
    const idSasi = (event.target as HTMLSelectElement).value;
    const apoderado = this.apoderadosSasi().find(a => a.usuarioId === idSasi);
    if (apoderado) {
      this.estudianteForm.patchValue({
        nombreApoderado: apoderado.nombreCompleto
      });
    }
  }

  abrirModalCrear(): void {
    if (!this.aulaSeleccionada()) {
      Swal.fire('Atención', 'Debe seleccionar un aula.', 'warning');
      return;
    }
    this.esEdicion.set(false);
    this.estudianteForm.reset({
      id: 0,
      aulaId: this.aulaSeleccionada(),
      tipoDocumento: 'DNI',
      numeroDocumento: '',
      nombres: '',
      apellidoPaterno: '',
      apellidoMaterno: '',
      usuarioIdApoderadoSasi: '',
      nombreApoderado: '',
      telefonoApoderado: ''
    });
    this.modalAbierto.set(true);
  }

  abrirModalEditar(e: Estudiante): void {
    this.esEdicion.set(true);
    this.estudianteForm.patchValue({
      id: e.id,
      aulaId: e.aulaId,
      tipoDocumento: e.tipoDocumento,
      numeroDocumento: e.numeroDocumento,
      nombres: e.nombres,
      apellidoPaterno: e.apellidoPaterno,
      apellidoMaterno: e.apellidoMaterno,
      usuarioIdApoderadoSasi: e.usuarioIdApoderadoSasi || '',
      nombreApoderado: e.nombreApoderado || '',
      telefonoApoderado: e.telefonoApoderado || ''
    });
    this.modalAbierto.set(true);
  }

  cerrarModal(): void {
    this.modalAbierto.set(false);
  }

  guardarEstudiante(): void {
    if (this.estudianteForm.invalid) {
      this.estudianteForm.markAllAsTouched();
      return;
    }

    const payload = {
      ...this.estudianteForm.value,
      aulaId: this.aulaSeleccionada()
    };

    if (this.esEdicion()) {
      this.estudianteService.actualizarEstudiante(payload.id, payload).subscribe({
        next: () => {
          Swal.fire({ icon: 'success', title: '¡Actualizado!', text: 'Estudiante modificado.', timer: 1500, showConfirmButton: false });
          this.cerrarModal();
          this.cargarEstudiantes(this.aulaSeleccionada()!);
        },
        error: (err) => Swal.fire('Error', err.error?.mensaje || 'No se pudo actualizar.', 'error')
      });
    } else {
      this.estudianteService.crearEstudiante(payload).subscribe({
        next: () => {
          Swal.fire({ icon: 'success', title: '¡Registrado!', text: 'Estudiante agregado al padrón.', timer: 1500, showConfirmButton: false });
          this.cerrarModal();
          this.cargarEstudiantes(this.aulaSeleccionada()!);
        },
        error: (err) => Swal.fire('Error', err.error?.mensaje || 'No se pudo registrar.', 'error')
      });
    }
  }

  eliminarEstudiante(e: Estudiante): void {
    Swal.fire({
      title: '¿Desactivar Estudiante?',
      text: `¿Desea cambiar el estado de ${e.nombreCompleto}?`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#ef4444',
      confirmButtonText: 'Sí, desactivar',
      cancelButtonText: 'Cancelar'
    }).then((res) => {
      if (res.isConfirmed) {
        this.estudianteService.eliminarEstudiante(e.id).subscribe({
          next: () => {
            Swal.fire('Inactivo', 'El estudiante fue desactivado.', 'success');
            this.cargarEstudiantes(this.aulaSeleccionada()!);
          },
          error: (err) => Swal.fire('Error', err.error?.mensaje || 'No se pudo desactivar.', 'error')
        });
      }
    });
  }
}
