import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, takeUntil } from 'rxjs';
import { EstudianteService } from '../../../core/services/estudiante.service';
import { ComiteService } from '../../../core/services/comite.service';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { PeriodoLectivo } from '../../../core/models/periodoLectivo.model';
import { Aula } from '../../../core/models/aula.model';
import { Estudiante } from '../../../core/models/estudiante.model';
import { UsuarioSasi } from '../../../core/models/comiteIntegrante.model';
import { BasePeriodosComponent } from '../../../core/base/base-periodos.component';
import Swal from 'sweetalert2';
import { CommonModule } from '@angular/common';
import * as XLSX from 'xlsx';

const MAX_ARCHIVO_MB = 5;
const MAX_FILAS_CARGA = 1000;
const FILAS_POR_PAGINA_PREVIEW = 20;
const EXTENSIONES_EXCEL_PERMITIDAS = ['xls', 'xlsx'];
const TIPOS_MIME_EXCEL_PERMITIDOS = [
  'application/vnd.ms-excel',
  'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
];

interface FilaEstudianteExcel {
  TipoDocumento?: string;
  NumeroDocumento?: string;
  Nombres?: string;
  ApellidoPaterno?: string;
  ApellidoMaterno?: string;
  NombreApoderado?: string;
  TelefonoApoderado?: string;
  tipoDocumento?: string;
  numeroDocumento?: string;
  nombres?: string;
  apellidoPaterno?: string;
  apellidoMaterno?: string;
  nombreApoderado?: string;
  telefonoApoderado?: string;
}

function escaparHtml(valor: string): string {
  return valor
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

interface RegistroPrevioEstudiante {
  tipoDocumento: string;
  numeroDocumento: string;
  nombres: string;
  apellidoPaterno: string;
  apellidoMaterno: string;
  nombreApoderado: string;
  telefonoApoderado: string;
  tieneApoderadoExcel: boolean;
  existeEnSasi: boolean;
  nombreSasiNormalizado: string;
}

@Component({
  selector: 'app-padron-estudiantes',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './padron-estudiantes.html',
  styleUrl: './padron-estudiantes.scss',
})
export class PadronEstudiantesComponent extends BasePeriodosComponent implements OnInit {
  private reiniciarCarga$ = new Subject<void>();
  private estudianteService = inject(EstudianteService);
  private comiteService = inject(ComiteService);
  private fb = inject(FormBuilder);

  aulas = signal<Aula[]>([]);
  estudiantes = signal<Estudiante[]>([]);
  apoderadosSasi = signal<UsuarioSasi[]>([]);

  periodoSeleccionado = signal<number | null>(null);
  aulaSeleccionada = signal<number | null>(null);

  cargando = signal<boolean>(false);
  cargandoDetalle = signal<boolean>(false);
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

  modalCargaMasivaAbierto = signal<boolean>(false);
  procesandoArchivo = signal<boolean>(false);
  registrosPrevios = signal<RegistroPrevioEstudiante[]>([]);
  nombreArchivoCargado = signal<string>('');

  previewPagina = signal<number>(1);
  registrosPaginados = computed<RegistroPrevioEstudiante[]>(() => {
    const lista = this.registrosPrevios();
    const inicio = (this.previewPagina() - 1) * FILAS_POR_PAGINA_PREVIEW;
    return lista.slice(inicio, inicio + FILAS_POR_PAGINA_PREVIEW);
  });
  totalPaginasPreview = computed<number>(() =>
    Math.max(1, Math.ceil(this.registrosPrevios().length / FILAS_POR_PAGINA_PREVIEW))
  );

  ngOnInit(): void {
    this.cargarPeriodos();
    this.cargarApoderadosSasi();
  }

  constructor() {
    super();
    this.destroyRef.onDestroy(() => this.reiniciarCarga$.complete());
  }

  onInputToUppercase(controlName: string): void {
    const control = this.estudianteForm.get(controlName);
    if (control && control.value) {
      control.patchValue(control.value.toUpperCase(), { emitEvent: false });
    }
  }

  protected override onPeriodosCargados(data: PeriodoLectivo[]): void {
    const periodoActual = this.buscarPeriodoVigente(data);
    if (periodoActual) {
      this.periodoSeleccionado.set(periodoActual.id);
      this.cargarAulasPorPeriodo(periodoActual.id);
    }
  }

  cargarAulasPorPeriodo(periodoId: number): void {
    this.reiniciarCarga$.next();
    this.aulaService.getAulas(periodoId).pipe(takeUntil(this.reiniciarCarga$), takeUntilDestroyed(this.destroyRef)).subscribe(data => {
      this.aulas.set(data);
      if (data.length > 0) {
        this.aulaSeleccionada.set(data[0].id);
        this.cargarEstudiantes(data[0].id);
      } else {
        this.aulaSeleccionada.set(null);
        this.estudiantes.set([]);
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

  cargarEstudiantes(aulaId: number): void {
    this.reiniciarCarga$.next();
    this.cargando.set(true);
    this.estudianteService.getEstudiantesPorAula(aulaId).pipe(takeUntil(this.reiniciarCarga$), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (data) => {
        const ordenados = [...data].sort((a, b) => {
          const apellidoA = `${a.apellidoPaterno} ${a.apellidoMaterno}`.trim();
          const apellidoB = `${b.apellidoPaterno} ${b.apellidoMaterno}`.trim();

          return apellidoA.localeCompare(apellidoB, 'es', { sensitivity: 'base' });
        });

        this.estudiantes.set(ordenados);
        this.cargando.set(false);
      },
      error: (err) => {
        this.cargando.set(false);
        Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar los estudiantes.', 'error');
      }
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
    this.cargandoDetalle.set(true);

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

    // 🛡️ La fila de la tabla trae el documento/teléfono ENMASCARADOS (***).
    // Consultamos el detalle para traer los datos reales y recién ahí
    // rellenamos el formulario de edición.
    this.estudianteService.getEstudiante(e.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (detalle) => {
        this.estudianteForm.patchValue({
          numeroDocumento: detalle.numeroDocumento,
          telefonoApoderado: detalle.telefonoApoderado || ''
        });
        this.cargandoDetalle.set(false);
      },
      error: (err) => {
        this.cargandoDetalle.set(false);
        Swal.fire('Error', err.error?.mensaje || 'No se pudieron cargar los datos reales del estudiante.', 'error');
      }
    });
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
      this.estudianteService.actualizarEstudiante(payload.id, payload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => {
          Swal.fire({ icon: 'success', title: '¡Actualizado!', text: 'Estudiante modificado.', timer: 1500, showConfirmButton: false });
          this.cerrarModal();
          this.cargarEstudiantes(this.aulaSeleccionada()!);
        },
        error: (err) => Swal.fire('Error', err.error?.mensaje || 'No se pudo actualizar.', 'error')
      });
    } else {
      this.estudianteService.crearEstudiante(payload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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
      cancelButtonText: 'Cancelar',
      allowEscapeKey: false,
      allowOutsideClick: false
    }).then((res) => {
      if (res.isConfirmed) {
        this.estudianteService.eliminarEstudiante(e.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
          next: () => {
            Swal.fire('Inactivo', 'El estudiante fue desactivado.', 'success');
            this.cargarEstudiantes(this.aulaSeleccionada()!);
          },
          error: (err) => Swal.fire('Error', err.error?.mensaje || 'No se pudo desactivar.', 'error')
        });
      }
    });
  }

  /* CARGA MASIVA */

  abrirModalCargaMasiva(): void {
    if (!this.aulaSeleccionada()) {
      Swal.fire('Atención', 'Debe seleccionar un aula antes de realizar la carga masiva.', 'warning');
      return;
    }
    this.registrosPrevios.set([]);
    this.previewPagina.set(1);
    this.nombreArchivoCargado.set('');
    this.modalCargaMasivaAbierto.set(true);
  }

  cerrarModalCargaMasiva(): void {
    this.modalCargaMasivaAbierto.set(false);
    this.registrosPrevios.set([]);
  }

  // 🚀 1. Generar y Descargar Plantilla Excel (.xlsx) Nativa
  descargarPlantillaExcel(): void {
    const dataPlantilla = [
      {
        TipoDocumento: 'DNI',
        NumeroDocumento: '00000000',
        Nombres: 'NOMBRES DEL ESTUDIANTE',
        ApellidoPaterno: 'APELLIDO PATERNO',
        ApellidoMaterno: 'APELLIDO MATERNO',
        NombreApoderado: 'NOMBRE DEL APODERADO',
        TelefonoApoderado: 'SIN TELEFONO'
      }
    ];

    const worksheet = XLSX.utils.json_to_sheet(dataPlantilla);
    const workbook = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(workbook, worksheet, 'Estudiantes');

    worksheet['!cols'] = [
      { wch: 15 }, { wch: 18 }, { wch: 22 }, { wch: 20 }, { wch: 20 }, { wch: 30 }, { wch: 18 }
    ];

    XLSX.writeFile(workbook, 'Plantilla_Importacion_Estudiantes.xlsx');
  }

  // 🚀 2. Lectura y procesamiento de archivos Excel (.xlsx / .xls) y CSV
  onArchivoSeleccionado(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    this.nombreArchivoCargado.set(file.name);

    // 🚀 Validar tipo MIME / extensión permitida (Excel .xls / .xlsx)
    const extension = file.name.split('.').pop()?.toLowerCase() || '';
    const esExcelValido =
      TIPOS_MIME_EXCEL_PERMITIDOS.includes(file.type) ||
      EXTENSIONES_EXCEL_PERMITIDAS.includes(extension);

    if (!esExcelValido) {
      input.value = '';
      this.nombreArchivoCargado.set('');
      this.registrosPrevios.set([]);
      Swal.fire('Formato no válido', 'Solo se permiten archivos Excel (.xls o .xlsx).', 'warning');
      return;
    }

    // 🚀 Validar tamaño máximo del archivo (evita strings/base64 gigantes y congelamientos)
    const maxBytes = MAX_ARCHIVO_MB * 1024 * 1024;
    if (file.size > maxBytes) {
      input.value = '';
      this.nombreArchivoCargado.set('');
      this.registrosPrevios.set([]);
      Swal.fire('Archivo demasiado grande', `El archivo excede el límite de ${MAX_ARCHIVO_MB} MB.`, 'warning');
      return;
    }

    const reader = new FileReader();
    reader.onload = (e: ProgressEvent<FileReader>) => {
      const result = e.target?.result;
      if (typeof result === 'string' || !result) return;
      const data = new Uint8Array(result);
      const workbook = XLSX.read(data, { type: 'array' });

      const firstSheetName = workbook.SheetNames[0];
      const worksheet = workbook.Sheets[firstSheetName];
      if (!worksheet || !worksheet['!ref']) {
        this.registrosPrevios.set([]);
        Swal.fire('Archivo sin datos', 'No se encontró contenido válido en la hoja del Excel.', 'warning');
        return;
      }

      // 🚀 Lectura limitada por rango para no congelar el hilo principal
      const rango = XLSX.utils.decode_range(worksheet['!ref']);
      const filaMaxLectura = Math.min(rango.e.r, MAX_FILAS_CARGA);
      const jsonResult = XLSX.utils.sheet_to_json<FilaEstudianteExcel>(worksheet, {
        defval: '',
        range: { s: { r: 0, c: rango.s.c }, e: { r: filaMaxLectura, c: rango.e.c } }
      });

      const apoderadosCatalogo = this.apoderadosSasi();

      const estudiantesParsed: RegistroPrevioEstudiante[] = jsonResult.map((row: FilaEstudianteExcel) => {
        const nombreApoderadoExcel = String(row.NombreApoderado || row.nombreApoderado || '').trim();
        let existeSasi = false;
        let nombreEncontrado = '';

        if (nombreApoderadoExcel) {
          const apMatch = apoderadosCatalogo.find(a => 
            a.nombreCompleto.toLowerCase().includes(nombreApoderadoExcel.toLowerCase()) ||
            nombreApoderadoExcel.toLowerCase().includes(a.nombreCompleto.toLowerCase())
          );

          if (apMatch) {
            existeSasi = true;
            nombreEncontrado = apMatch.nombreCompleto;
          }
        }

        return {
          tipoDocumento: String(row.TipoDocumento || row.tipoDocumento || 'DNI').trim(),
          numeroDocumento: String(row.NumeroDocumento || row.numeroDocumento || '').trim(),
          nombres: String(row.Nombres || row.nombres || '').trim().toUpperCase(),
          apellidoPaterno: String(row.ApellidoPaterno || row.apellidoPaterno || '').trim().toUpperCase(),
          apellidoMaterno: String(row.ApellidoMaterno || row.apellidoMaterno || '').trim().toUpperCase(),
          nombreApoderado: nombreApoderadoExcel,
          telefonoApoderado: String(row.TelefonoApoderado || row.telefonoApoderado || '').trim(),

          // 🚀 Banderas para la vista previa
          tieneApoderadoExcel: !!nombreApoderadoExcel,
          existeEnSasi: existeSasi,
          nombreSasiNormalizado: nombreEncontrado
        };
      });

      this.registrosPrevios.set(estudiantesParsed);
      this.previewPagina.set(1);

      // 🚀 Avisar si el archivo supera el límite de filas por lote
      if (rango.e.r > MAX_FILAS_CARGA) {
        Swal.fire('Límite de filas', `El archivo contiene más de ${MAX_FILAS_CARGA} filas. Solo se procesarán los primeros ${MAX_FILAS_CARGA} registros.`, 'warning');
      }
    };

    reader.readAsArrayBuffer(file);
  }

  cambiarPaginaPreview(delta: number): void {
    const nueva = this.previewPagina() + delta;
    if (nueva >= 1 && nueva <= this.totalPaginasPreview()) {
      this.previewPagina.set(nueva);
    }
  }


  // 🚀 3. Envío al backend y despliegue del Reporte de Resultados SWAL
  procesarCargaMasiva(): void {
    const lista = this.registrosPrevios();
    if (lista.length === 0 || !this.aulaSeleccionada()) return;

    this.procesandoArchivo.set(true);

    this.estudianteService.cargaMasiva(this.aulaSeleccionada()!, lista).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        this.procesandoArchivo.set(false);
        this.cerrarModalCargaMasiva();

        // Construcción del reporte de detalles
        let detallesHtml = `
          <div class="text-left text-xs font-sans mt-3">
            <p class="mb-2"><strong>Procesados:</strong> ${res.registrosProcesados} | <strong>Insertados:</strong> <span class="text-emerald-600 font-bold">${res.registrosInsertados}</span> | <strong>Omitidos:</strong> <span class="text-rose-600 font-bold">${res.registrosOmitidos}</span></p>
        `;

        if (res.detallesObservaciones && res.detallesObservaciones.length > 0) {
          detallesHtml += `
            <div class="max-h-40 overflow-y-auto bg-slate-100 p-2.5 rounded-lg border border-slate-200 font-mono text-[11px] space-y-1 text-slate-700">
              ${res.detallesObservaciones.map((obs: string) => `<div>• ${escaparHtml(obs)}</div>`).join('')}
            </div>
          `;
        }
        detallesHtml += `</div>`;

        Swal.fire({
          icon: res.registrosInsertados > 0 ? 'success' : 'warning',
          title: 'Resultado de la Carga Masiva',
          html: detallesHtml,
          confirmButtonColor: '#2563eb',
          allowEscapeKey: false,
          allowOutsideClick: false
        });

        this.cargarEstudiantes(this.aulaSeleccionada()!);
      },
      error: (err) => {
        this.procesandoArchivo.set(false);
        Swal.fire('Error', err.error?.mensaje || 'No se pudo completar la carga masiva.', 'error');
      }
    });
  }
}
