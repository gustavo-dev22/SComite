import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActaService } from '../../../core/services/acta.service';
import { AulaService } from '../../../core/services/aula.service';
import { AuthService } from '../../../core/services/auth.service';
import { PeriodoLectivo } from '../../../core/models/periodoLectivo.model';
import { Aula } from '../../../core/models/aula.model';
import { ActaAsambleaComite } from '../../../core/models/acta.model';
import { ComiteService } from '../../../core/services/comite.service';
import { ComiteIntegrante } from '../../../core/models/comiteIntegrante.model';

declare var html2canvas: any;
declare var jspdf: any;

@Component({
  selector: 'app-actas-asamblea',
  imports: [CommonModule, FormsModule],
  templateUrl: './actas-asamblea.html',
  styleUrl: './actas-asamblea.scss',
})
export class ActasAsambleaComponent implements OnInit {
  private actaService = inject(ActaService);
  private aulaService = inject(AulaService);
  private authService = inject(AuthService);
  private comiteService = inject(ComiteService);

  periodos = signal<PeriodoLectivo[]>([]);
  aulas = signal<Aula[]>([]);
  actas = signal<ActaAsambleaComite[]>([]);
  integrantesComiteRaw = signal<ComiteIntegrante[]>([]);

  periodoSeleccionadoId = signal<number | null>(null);
  aulaSeleccionadaId = signal<number | null>(null);

  cargandoAulas = signal<boolean>(false);
  cargando = signal<boolean>(false);
  guardando = signal<boolean>(false);
  mostrarModal = signal<boolean>(false);

  descargandoPdf = signal<boolean>(false);
  actaParaPdf = signal<ActaAsambleaComite | null>(null);
  fechaEmision = new Date();

  formActa = signal<Partial<ActaAsambleaComite>>({
    id: 0,
    aulaId: 0,
    numeroActa: '',
    titulo: '',
    fechaReunion: new Date().toISOString().split('T')[0],
    agendaAcuerdos: '',
    estadoActa: 'APROBADA',
    urlDocumentoPdf: '',
    usuarioRegistro: ''
  });

  tienePeriodoSeleccionado = computed(() => this.periodoSeleccionadoId() !== null && this.periodoSeleccionadoId()! > 0);
  tieneAulaSeleccionada = computed(() => this.aulaSeleccionadaId() !== null && this.aulaSeleccionadaId()! > 0);

  aulaNombreActual = computed(() => {
    const id = this.aulaSeleccionadaId();
    const aula = this.aulas().find(a => a.id === id);
    return aula ? `${aula.nivel} - ${aula.grado}° "${aula.seccion}"` : '';
  });

  anioActual = computed(() => {
    const id = this.periodoSeleccionadoId();
    const p = this.periodos().find(x => x.id === id);
    return p ? p.anio : new Date().getFullYear();
  });

  firmasComite = computed(() => {
    const lista = this.integrantesComiteRaw();
    const obtenerNombre = (cargoBusqueda: string) => {
      const miembro = lista.find(m => m.cargo?.toUpperCase().includes(cargoBusqueda));
      return miembro ? miembro.nombreCompleto : '________________________';
    };

    return {
      presidente: obtenerNombre('PRESIDENTE'),
      tesorero: obtenerNombre('TESORERO'),
      secretario: obtenerNombre('SECRETARIO'),
      vocal: obtenerNombre('VOCAL')
    };
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
    const id = Number((event.target as HTMLSelectElement).value) || null;
    this.periodoSeleccionadoId.set(id);
    this.aulaSeleccionadaId.set(null);
    this.aulas.set([]);
    this.actas.set([]);

    if (id && id > 0) this.cargarAulasPorPeriodo(id);
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
    const aulaId = Number((event.target as HTMLSelectElement).value) || null;
    this.aulaSeleccionadaId.set(aulaId);

    if (aulaId && aulaId > 0) {
      this.cargarActas(aulaId);
      this.cargarIntegrantesComite(aulaId);
    } else {
      this.actas.set([]);
      this.integrantesComiteRaw.set([]);
    }
  }

  cargarIntegrantesComite(aulaId: number): void {
    this.comiteService.getComitePorAula(aulaId).subscribe({
      next: (data) => this.integrantesComiteRaw.set(data),
      error: () => this.integrantesComiteRaw.set([])
    });
  }

  cargarActas(aulaId: number): void {
    this.cargando.set(true);
    const periodoObj = this.periodos().find(p => p.id === this.periodoSeleccionadoId());
    const anio = periodoObj ? periodoObj.anio : new Date().getFullYear();

    this.actaService.getActasPorAula(aulaId, anio).subscribe({
      next: (data) => {
        this.actas.set(data);
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false)
    });
  }

  abrirModalNuevo(): void {
    const aulaId = this.aulaSeleccionadaId();
    if (!aulaId) return;

    const periodoObj = this.periodos().find(p => p.id === this.periodoSeleccionadoId());
    const anio = periodoObj ? periodoObj.anio : new Date().getFullYear();

    this.actaService.getSiguienteNumeroActa(aulaId, anio).subscribe({
      next: (res) => {
        const usuarioActual = this.authService.usuarioActual();
        const nombreUsuario = usuarioActual ? usuarioActual : 'Comité de Aula';

        this.formActa.set({
          id: 0,
          aulaId: aulaId,
          numeroActa: res.siguienteNumeroActa, 
          titulo: '',
          fechaReunion: new Date().toISOString().split('T')[0],
          agendaAcuerdos: '',
          estadoActa: 'BORRADOR',
          urlDocumentoPdf: '',
          usuarioRegistro: nombreUsuario
        });
        
        this.mostrarModal.set(true);
      }
    });
  }

  abrirModalEditar(a: ActaAsambleaComite): void {
    const usuarioActual = this.authService.usuarioActual();
    const nombreUsuario = usuarioActual ? usuarioActual : (a.usuarioRegistro || 'Comité de Aula');

    this.formActa.set({
      id: a.id,
      aulaId: a.aulaId,
      numeroActa: a.numeroActa,
      titulo: a.titulo,
      fechaReunion: new Date(a.fechaReunion).toISOString().split('T')[0],
      agendaAcuerdos: a.agendaAcuerdos,
      estadoActa: a.estadoActa,
      urlDocumentoPdf: a.urlDocumentoPdf || '',
      usuarioRegistro: nombreUsuario
    });
    this.mostrarModal.set(true);
  }

  cerrarModal(): void {
    this.mostrarModal.set(false);
  }

  onInputToUppercase(campo: 'numeroActa' | 'titulo'): void {
    const val = this.formActa()[campo] || '';
    this.formActa.update(f => ({ ...f, [campo]: val.toUpperCase() }));
  }

  guardarActa(): void {
    const dto = this.formActa();
    if (!dto.numeroActa?.trim() || !dto.titulo?.trim() || !dto.agendaAcuerdos?.trim()) return;

    dto.aulaId = this.aulaSeleccionadaId()!;
    this.guardando.set(true);

    this.actaService.guardarActa(dto).subscribe({
      next: () => {
        this.guardando.set(false);
        this.cerrarModal();
        this.cargarActas(this.aulaSeleccionadaId()!);
      },
      error: () => this.guardando.set(false)
    });
  }

  eliminarActa(id: number): void {
    if (!confirm('¿Está seguro de eliminar esta acta de asamblea?')) return;

    this.actaService.eliminarActa(id, this.aulaSeleccionadaId()!).subscribe({
      next: () => this.cargarActas(this.aulaSeleccionadaId()!)
    });
  }

  getEstadoClass(estado: string): string {
    switch (estado) {
      case 'APROBADA': return 'bg-emerald-100 text-emerald-800 border-emerald-200';
      case 'BORRADOR': return 'bg-amber-100 text-amber-800 border-amber-200';
      default: return 'bg-slate-100 text-slate-700 border-slate-200';
    }
  }

  descargarPdfActa(acta: ActaAsambleaComite): void {
    this.actaParaPdf.set(acta);
    this.descargandoPdf.set(true);

    const element = document.getElementById('acta-imprimible-pdf');
    if (!element) {
      this.descargandoPdf.set(false);
      return;
    }

    // 1. Mostrar elemento para captura
    element.style.display = 'block';

    setTimeout(() => {
      const numActaClean = acta.numeroActa.replace(/[^A-Z0-9]/gi, '_');
      const nombreArchivo = `${numActaClean}.pdf`;

      html2canvas(element, {
        scale: 2,
        useCORS: true,
        logging: false,
        backgroundColor: '#ffffff',
        // 🚀 SOLUCIÓN AL ERROR OKLCH: Sanitizado de estilos en el clon del DOM
        onclone: (clonedDoc: Document) => {
          const pdfEl = clonedDoc.getElementById('acta-imprimible-pdf');
          if (pdfEl) {
            pdfEl.style.display = 'block';
            pdfEl.style.backgroundColor = '#ffffff';
            pdfEl.style.color = '#0f172a';

            const elements = pdfEl.querySelectorAll('*');
            elements.forEach((el: any) => {
              el.style.boxShadow = 'none';
              el.style.textShadow = 'none';
              const style = window.getComputedStyle(el);

              // Sobrescribir cualquier propiedad calculada que use oklch
              if (style.color && style.color.includes('oklch')) {
                el.style.color = '#0f172a';
              }
              if (style.backgroundColor && style.backgroundColor.includes('oklch')) {
                el.style.backgroundColor = '#ffffff';
              }
              if (style.borderColor && style.borderColor.includes('oklch')) {
                el.style.borderColor = '#cbd5e1';
              }
            });
          }
        }
      }).then((canvas: HTMLCanvasElement) => {
        const imgData = canvas.toDataURL('image/jpeg', 0.98);
        const { jsPDF } = jspdf;
        const pdf = new jsPDF('p', 'mm', 'a4');

        const imgWidth = 210 - 20; // Margen A4
        const imgHeight = (canvas.height * imgWidth) / canvas.width;

        pdf.addImage(imgData, 'JPEG', 10, 10, imgWidth, imgHeight);
        pdf.save(nombreArchivo);

        // Ocultar de nuevo
        element.style.display = 'none';
        this.descargandoPdf.set(false);
      }).catch((err: any) => {
        console.error('Error al generar PDF:', err);
        element.style.display = 'none';
        this.descargandoPdf.set(false);
      });

    }, 200);
  }
}
