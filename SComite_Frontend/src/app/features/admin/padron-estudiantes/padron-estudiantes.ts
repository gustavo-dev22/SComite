import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, takeUntil } from 'rxjs';
import { EstudianteService } from '../../../core/services/estudiante.service';
import { ComiteService } from '../../../core/services/comite.service';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { PeriodoLectivo } from '../../../core/models/periodoLectivo.model';
import { Aula } from '../../../core/models/aula.model';
import { Estudiante } from '../../../core/models/estudiante.model';
import { UsuarioSasi } from '../../../core/models/comiteIntegrante.model';
import { BasePeriodosComponent } from '../../../core/base/base-periodos.component';
import { manejarErrorHttp } from '../../../core/utils/http-error.util';
import Swal from 'sweetalert2';
import { CommonModule } from '@angular/common';
import type { Cell } from 'exceljs';
import { ModalA11yDirective } from '../../../shared/directives/modal-a11y.directive';
const MAX_ARCHIVO_MB = 5;
const MAX_FILAS_CARGA = 1000;
const FILAS_POR_PAGINA_PREVIEW = 20;
const EXTENSIONES_EXCEL_PERMITIDAS = ['xlsx'];
const TIPOS_MIME_EXCEL_PERMITIDOS = [
  'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
];

interface EstudianteForm {
  id: FormControl<number>;
  aulaId: FormControl<number>;
  tipoDocumento: FormControl<string>;
  numeroDocumento: FormControl<string>;
  nombres: FormControl<string>;
  apellidoPaterno: FormControl<string>;
  apellidoMaterno: FormControl<string>;
  usuarioIdApoderadoSasi: FormControl<string>;
  nombreApoderado: FormControl<string>;
  telefonoApoderado: FormControl<string>;
}

interface FilaEstudianteExcel {
  tipoDocumento: string;
  numeroDocumento: string;
  nombres: string;
  apellidoPaterno: string;
  apellidoMaterno: string;
  nombreApoderado: string;
  telefonoApoderado: string;
}

const CAMPOS_PLANTILLA: { campo: keyof FilaEstudianteExcel; encabezado: string }[] = [
  { campo: 'tipoDocumento', encabezado: 'TipoDocumento' },
  { campo: 'numeroDocumento', encabezado: 'NumeroDocumento' },
  { campo: 'nombres', encabezado: 'Nombres' },
  { campo: 'apellidoPaterno', encabezado: 'ApellidoPaterno' },
  { campo: 'apellidoMaterno', encabezado: 'ApellidoMaterno' },
  { campo: 'nombreApoderado', encabezado: 'NombreApoderado' },
  { campo: 'telefonoApoderado', encabezado: 'TelefonoApoderado' }
];

const MAPA_CAMPOS_COLUMNA: Record<string, keyof FilaEstudianteExcel> = {
  tipodocumento: 'tipoDocumento',
  numerodocumento: 'numeroDocumento',
  nrodocumento: 'numeroDocumento',
  numerodedocumento: 'numeroDocumento',
  dni: 'numeroDocumento',
  nombres: 'nombres',
  nombre: 'nombres',
  apellidopaterno: 'apellidoPaterno',
  apellidomaterno: 'apellidoMaterno',
  nombreapoderado: 'nombreApoderado',
  apoderado: 'nombreApoderado',
  telefonoapoderado: 'telefonoApoderado',
  telefonomovil: 'telefonoApoderado'
};

function normalizarEncabezado(texto: string): string {
  return texto
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]/g, '');
}

function valorCeldaComoTexto(celda: Cell): string {
  const valor = celda.value;
  if (valor === null || valor === undefined) return '';
  if (typeof valor === 'string') return valor.trim();
  if (typeof valor === 'number' || typeof valor === 'boolean') return String(valor);
  if (valor instanceof Date) return valor.toLocaleDateString('es-PE');
  if (typeof valor === 'object') {
    const objeto = valor as { text?: unknown; richText?: { text?: unknown }[] };
    if (Array.isArray(objeto.richText)) {
      return objeto.richText.map((fragmento) => String(fragmento.text ?? '')).join('').trim();
    }
    if (objeto.text !== undefined && objeto.text !== null) {
      return String(objeto.text).trim();
    }
  }
  return '';
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
  imports: [CommonModule, ReactiveFormsModule, ModalA11yDirective],
  templateUrl: './padron-estudiantes.html',
  styleUrl: './padron-estudiantes.scss',
})
export class PadronEstudiantesComponent extends BasePeriodosComponent implements OnInit {
  private reiniciarCarga$ = new Subject<void>();
  private estudianteService = inject(EstudianteService);
  private comiteService = inject(ComiteService);
  private fb = inject(FormBuilder).nonNullable;

  aulas = signal<Aula[]>([]);
  estudiantes = signal<Estudiante[]>([]);
  apoderadosSasi = signal<UsuarioSasi[]>([]);
  // SASI-DOWN: indica si el catálogo de apoderados de SASI está disponible.
  // Permite avisar al usuario y bloquear el guardado cuando el vínculo con un
  // apoderado es obligatorio y SASI no responde.
  sasiDisponible = signal<boolean>(true);

  periodoSeleccionado = signal<number | null>(null);
  aulaSeleccionada = signal<number | null>(null);

  cargando = signal<boolean>(false);
  cargandoDetalle = signal<boolean>(false);
  modalAbierto = signal<boolean>(false);
  esEdicion = signal<boolean>(false);
  guardando = signal<boolean>(false);

  estudianteForm: FormGroup<EstudianteForm> = this.fb.group({
    id: this.fb.control(0),
    aulaId: this.fb.control(0),
    tipoDocumento: this.fb.control('DNI', [Validators.required]),
    numeroDocumento: this.fb.control('', [Validators.required, Validators.pattern('^[0-9]{8,12}$')]),
    nombres: this.fb.control('', [Validators.required]),
    apellidoPaterno: this.fb.control('', [Validators.required]),
    apellidoMaterno: this.fb.control('', [Validators.required]),
    usuarioIdApoderadoSasi: this.fb.control(''),
    nombreApoderado: this.fb.control(''),
    telefonoApoderado: this.fb.control('')
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
        // SASI-DOWN: si el servicio SASI no está disponible (503) o hubo un error de
        // conexión (status 0), se informa de forma amigable y se desactiva el vínculo de
        // apoderados para que el usuario no registre alumnos con datos incompletos.
        const esSasiNoDisponible =
          (err as { status?: number } | null)?.status === 503 ||
          (err as { status?: number } | null)?.status === 0;

        this.sasiDisponible.set(!esSasiNoDisponible);

        manejarErrorHttp(err, 'No se pudieron cargar los apoderados de SASI.');
      }
    });
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
        manejarErrorHttp(err, 'No se pudieron cargar los estudiantes.');
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
      aulaId: this.aulaSeleccionada() ?? 0,
      tipoDocumento: 'DNI',
      numeroDocumento: '',
      nombres: '',
      apellidoPaterno: '',
      apellidoMaterno: '',
      usuarioIdApoderadoSasi: '',
      nombreApoderado: '',
      telefonoApoderado: ''
    });

    // SASI-DOWN: se re-consulta el catálogo de apoderados al abrir el modal (no solo
    // al cargar la página), de modo que si SASI cayó después, el aviso aparece al instante.
    this.cargarApoderadosSasi();

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

    // SASI-DOWN: se re-consulta el catálogo de apoderados al abrir el modal para
    // detectar una caída de SASI ocurrida después de cargar la página.
    this.cargarApoderadosSasi();

    this.modalAbierto.set(true);

    // La fila de la tabla trae el documento/teléfono ENMASCARADOS (***).
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
        manejarErrorHttp(err, 'No se pudieron cargar los datos reales del estudiante.');
      }
    });
  }

  cerrarModal(): void {
    this.modalAbierto.set(false);
  }

  guardarEstudiante(): void {
    if (this.guardando()) return;
    if (this.estudianteForm.invalid) {
      this.estudianteForm.markAllAsTouched();
      return;
    }

    const aulaId = this.aulaSeleccionada();
    if (!aulaId) {
      Swal.fire('Atención', 'Debe seleccionar un aula.', 'warning');
      return;
    }

    // SASI-DOWN (CRÍTICO): solo se re-consulta SASI si el usuario está vinculando un
    // apoderado. Si SASI se cayó mientras el modal estaba abierto, la señal cacheada ya no
    // es fiable, por lo que se valida en el MOMENTO de guardar contra el catálogo real.
    const vinculandoApoderado =
      !!this.estudianteForm.value.usuarioIdApoderadoSasi ||
      !!this.estudianteForm.value.nombreApoderado?.trim();

    if (!vinculandoApoderado) {
      this.ejecutarGuardadoEstudiante(aulaId);
      return;
    }

    this.guardando.set(true);

    this.comiteService.getApoderadosSasi().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (apoderados) => {
        this.sasiDisponible.set(true);
        this.apoderadosSasi.set([...apoderados].sort((a, b) =>
          a.nombreCompleto.localeCompare(b.nombreCompleto, 'es', { sensitivity: 'base' })
        ));

        const apoderado = apoderados.find(a => a.usuarioId === this.estudianteForm.value.usuarioIdApoderadoSasi);
        if (!apoderado) {
          this.guardando.set(false);
          Swal.fire({
            icon: 'warning',
            title: 'Apoderado no disponible',
            text: 'El apoderado seleccionado ya no está disponible en el servicio SASI. Vuelva a seleccionarlo, guarde sin apoderado o intente nuevamente.',
            confirmButtonColor: '#2563eb',
            confirmButtonText: 'Entendido'
          });
          return;
        }

        // Se normaliza el nombre desde el catálogo REAL de SASI antes de guardar.
        this.estudianteForm.patchValue({
          usuarioIdApoderadoSasi: apoderado.usuarioId,
          nombreApoderado: apoderado.nombreCompleto
        });

        this.ejecutarGuardadoEstudiante(aulaId);
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
            text: 'El servicio de autenticación (SASI) no está disponible. No se pudo vincular el apoderado. Puede guardar el estudiante sin apoderado o intentar nuevamente en unos minutos.',
            confirmButtonColor: '#2563eb',
            confirmButtonText: 'Entendido'
          });
          return;
        }

        manejarErrorHttp(err, 'No se pudo registrar.');
      }
    });
  }

  private ejecutarGuardadoEstudiante(aulaId: number): void {
    const payload = {
      ...this.estudianteForm.getRawValue(),
      aulaId
    };

    if (this.esEdicion()) {
      this.estudianteService.actualizarEstudiante(payload.id, payload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => {
          this.guardando.set(false);
          Swal.fire({ icon: 'success', title: '¡Actualizado!', text: 'Estudiante modificado.', timer: 1500, showConfirmButton: false });
          this.cerrarModal();
          this.cargarEstudiantes(this.aulaSeleccionada()!);
        },
        error: (err) => {
          this.guardando.set(false);
          manejarErrorHttp(err, 'No se pudo actualizar.');
        }
      });
    } else {
      this.estudianteService.crearEstudiante(payload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => {
          this.guardando.set(false);
          Swal.fire({ icon: 'success', title: '¡Registrado!', text: 'Estudiante agregado al padrón.', timer: 1500, showConfirmButton: false });
          this.cerrarModal();
          this.cargarEstudiantes(this.aulaSeleccionada()!);
        },
        error: (err) => {
          this.guardando.set(false);
          manejarErrorHttp(err, 'No se pudo registrar.');
        }
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
          error: (err) => manejarErrorHttp(err, 'No se pudo desactivar.')
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

  // 1. Generar y Descargar Plantilla Excel (.xlsx) Nativa
  async descargarPlantillaExcel(): Promise<void> {
    const { Workbook } = await import('exceljs');
    const workbook = new Workbook();
    const worksheet = workbook.addWorksheet('Estudiantes');

    worksheet.addRow(CAMPOS_PLANTILLA.map((col) => col.encabezado));
    worksheet.addRow([
      'DNI',
      '00000000',
      'NOMBRES DEL ESTUDIANTE',
      'APELLIDO PATERNO',
      'APELLIDO MATERNO',
      'NOMBRE DEL APODERADO',
      'SIN TELEFONO'
    ]);

    worksheet.columns = [
      { width: 15 }, { width: 18 }, { width: 22 }, { width: 20 }, { width: 20 }, { width: 30 }, { width: 18 }
    ];
    worksheet.getRow(1).font = { bold: true };

    workbook.xlsx.writeBuffer().then((buffer) => {
      const blob = new Blob([buffer], {
        type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
      });
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = 'Plantilla_Importacion_Estudiantes.xlsx';
      link.click();
      URL.revokeObjectURL(url);
    });
  }

  // 2. Lectura y procesamiento de archivos Excel (.xlsx) y CSV
  onArchivoSeleccionado(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    this.nombreArchivoCargado.set(file.name);

    // Validar tipo MIME / extensión permitida (Excel .xlsx)
    const extension = file.name.split('.').pop()?.toLowerCase() || '';
    const esExcelValido =
      TIPOS_MIME_EXCEL_PERMITIDOS.includes(file.type) ||
      EXTENSIONES_EXCEL_PERMITIDAS.includes(extension);

    if (!esExcelValido) {
      input.value = '';
      this.nombreArchivoCargado.set('');
      this.registrosPrevios.set([]);
      Swal.fire('Formato no válido', 'Solo se permiten archivos Excel (.xlsx).', 'warning');
      return;
    }

    // Validar tamaño máximo del archivo (evita strings/base64 gigantes y congelamientos)
    const maxBytes = MAX_ARCHIVO_MB * 1024 * 1024;
    if (file.size > maxBytes) {
      input.value = '';
      this.nombreArchivoCargado.set('');
      this.registrosPrevios.set([]);
      Swal.fire('Archivo demasiado grande', `El archivo excede el límite de ${MAX_ARCHIVO_MB} MB.`, 'warning');
      return;
    }

    void this.procesarArchivoExcel(file, input);
  }

  private async procesarArchivoExcel(file: File, input: HTMLInputElement): Promise<void> {
    try {
      const arrayBuffer = await file.arrayBuffer();
      const { Workbook } = await import('exceljs');
      const workbook = new Workbook();
      await workbook.xlsx.load(arrayBuffer);

      const worksheet = workbook.worksheets[0];
      if (!worksheet || worksheet.actualRowCount === 0) {
        this.registrosPrevios.set([]);
        Swal.fire('Archivo sin datos', 'No se encontró contenido válido en la hoja del Excel.', 'warning');
        return;
      }

      // Mapear encabezados (fila 1) a los campos esperados de la plantilla
      const campoPorColumna = new Map<number, keyof FilaEstudianteExcel>();
      worksheet.getRow(1).eachCell({ includeEmpty: true }, (celda, numeroColumna) => {
        const campo = MAPA_CAMPOS_COLUMNA[normalizarEncabezado(valorCeldaComoTexto(celda))];
        if (campo) campoPorColumna.set(numeroColumna, campo);
      });

      if (campoPorColumna.size === 0) {
        this.registrosPrevios.set([]);
        Swal.fire('Plantilla no válida', 'No se reconocieron los encabezados. Descargue la plantilla oficial e intente nuevamente.', 'warning');
        return;
      }

      // Lectura limitada por filas para no congelar el hilo principal
      const filaMaxLectura = Math.min(worksheet.actualRowCount, MAX_FILAS_CARGA + 1);
      const apoderadosCatalogo = this.apoderadosSasi();
      const estudiantesParsed: RegistroPrevioEstudiante[] = [];

      for (let numeroFila = 2; numeroFila <= filaMaxLectura; numeroFila++) {
        const fila = worksheet.getRow(numeroFila);
        const datos: FilaEstudianteExcel = {
          tipoDocumento: '',
          numeroDocumento: '',
          nombres: '',
          apellidoPaterno: '',
          apellidoMaterno: '',
          nombreApoderado: '',
          telefonoApoderado: ''
        };

        fila.eachCell({ includeEmpty: false }, (celda, numeroColumna) => {
          const campo = campoPorColumna.get(numeroColumna);
          if (campo) datos[campo] = valorCeldaComoTexto(celda);
        });

        // Saltar filas completamente vacías
        if (!datos.numeroDocumento && !datos.nombres) continue;

        const nombreApoderadoExcel = datos.nombreApoderado.trim();
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

        estudiantesParsed.push({
          tipoDocumento: datos.tipoDocumento || 'DNI',
          numeroDocumento: datos.numeroDocumento,
          nombres: datos.nombres.toUpperCase(),
          apellidoPaterno: datos.apellidoPaterno.toUpperCase(),
          apellidoMaterno: datos.apellidoMaterno.toUpperCase(),
          nombreApoderado: nombreApoderadoExcel,
          telefonoApoderado: datos.telefonoApoderado,

          // Banderas para la vista previa
          tieneApoderadoExcel: !!nombreApoderadoExcel,
          existeEnSasi: existeSasi,
          nombreSasiNormalizado: nombreEncontrado
        });
      }

      this.registrosPrevios.set(estudiantesParsed);
      this.previewPagina.set(1);

      // Avisar si el archivo supera el límite de filas por lote
      if (worksheet.actualRowCount - 1 > MAX_FILAS_CARGA) {
        Swal.fire('Límite de filas', `El archivo contiene más de ${MAX_FILAS_CARGA} filas. Solo se procesarán los primeros ${MAX_FILAS_CARGA} registros.`, 'warning');
      }
    } catch {
      input.value = '';
      this.nombreArchivoCargado.set('');
      this.registrosPrevios.set([]);
      Swal.fire('Error de lectura', 'No se pudo leer el archivo. Asegúrese de que sea un Excel nativo (.xlsx).', 'error');
    }
  }

  cambiarPaginaPreview(delta: number): void {
    const nueva = this.previewPagina() + delta;
    if (nueva >= 1 && nueva <= this.totalPaginasPreview()) {
      this.previewPagina.set(nueva);
    }
  }


  // 3. Envío al backend y despliegue del Reporte de Resultados SWAL
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
        // El interceptor muestra la alerta global (p. ej. SASI no disponible 503)
        // y se reutiliza manejarErrorHttp para no duplicar ni perder el detalle.
        manejarErrorHttp(err, 'No se pudo completar la carga masiva.');
      }
    });
  }
}
