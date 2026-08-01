import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SistemaService } from '../../../core/services/sistema.service';

@Component({
  selector: 'app-mantenimiento-sistema',
  imports: [CommonModule, FormsModule],
  templateUrl: './mantenimiento-sistema.html',
  styleUrl: './mantenimiento-sistema.scss',
})
export class MantenimientoSistemaComponent {
  private sistemaService = inject(SistemaService);

  mostrarModalConfirmacion = signal<boolean>(false);
  textoConfirmacion = signal<string>('');
  procesandoReset = signal<boolean>(false);
  mensajeResultado = signal<string | null>(null);

  abrirModalReset(): void {
    this.textoConfirmacion.set('');
    this.mensajeResultado.set(null);
    this.mostrarModalConfirmacion.set(true);
  }

  cerrarModal(): void {
    if (this.procesandoReset()) return;
    this.mostrarModalConfirmacion.set(false);
  }

  ejecutarPurgaBaseDeDatos(): void {
    if (this.textoConfirmacion().trim() !== 'ELIMINAR TODO') return;

    this.procesandoReset.set(true);
    this.sistemaService.resetBaseDeDatos(this.textoConfirmacion().trim()).subscribe({
      next: (res) => {
        this.procesandoReset.set(false);
        this.cerrarModal();
        alert('✅ ' + res.message);
        window.location.reload(); // Recargar la aplicación limpia
      },
      error: (err) => {
        this.procesandoReset.set(false);
        this.mensajeResultado.set(err.error?.message || 'Error al intentar reiniciar la base de datos.');
      }
    });
  }
}
