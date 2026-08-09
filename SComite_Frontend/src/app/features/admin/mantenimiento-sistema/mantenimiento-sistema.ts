import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { SistemaService } from '../../../core/services/sistema.service';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-mantenimiento-sistema',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule],
  templateUrl: './mantenimiento-sistema.html',
  styleUrl: './mantenimiento-sistema.scss',
})
export class MantenimientoSistemaComponent {
  private destroyRef = inject(DestroyRef);
  private sistemaService = inject(SistemaService);

  mostrarModalConfirmacion = signal<boolean>(false);
  textoConfirmacion = signal<string>('');
  procesandoReset = signal<boolean>(false);
  descargandoBackup = signal<boolean>(false);
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

  generarBackupManual(): void {
    this.descargandoBackup.set(true);
    this.sistemaService.descargarBackupManual().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `Backup_Seguridad_AulaComite_${new Date().toISOString().substring(0, 10)}.sql`;
        a.click();
        window.URL.revokeObjectURL(url);
        this.descargandoBackup.set(false);
      },
      error: (err) => {
        this.descargandoBackup.set(false);
        Swal.fire('Error', err.error?.mensaje || 'No se pudo generar el backup.', 'error');
      }
    });
  }

  ejecutarPurgaBaseDeDatos(): void {
    if (this.textoConfirmacion().trim() !== 'ELIMINAR TODO') return;

    this.procesandoReset.set(true);
    this.sistemaService.resetBaseDeDatos(this.textoConfirmacion().trim()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        this.procesandoReset.set(false);
        this.cerrarModal();
        alert('✅ ' + res.mensaje);
        window.location.reload(); // Recargar la aplicación limpia
      },
      error: (err) => {
        this.procesandoReset.set(false);
        this.mensajeResultado.set(err.error?.mensaje || 'Error al intentar reiniciar la base de datos.');
      }
    });
  }
}
