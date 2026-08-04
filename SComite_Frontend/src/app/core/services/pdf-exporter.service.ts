import { Injectable } from '@angular/core';
import html2pdf from 'html2pdf.js';

@Injectable({
  providedIn: 'root'
})
export class PdfExporterService {
  async exportarElemento(element: HTMLElement, filename: string): Promise<void> {
    const displayOriginal = element.style.display;
    element.style.display = 'block';

    await new Promise((resolve) => setTimeout(resolve, 200));

    try {
      await html2pdf()
        .set({
          margin: 10,
          filename,
          image: { type: 'jpeg', quality: 0.98 },
          html2canvas: {
            scale: 2,
            useCORS: true,
            logging: false,
            backgroundColor: '#ffffff',
            onclone: (clonedDoc: Document) => {
              this.sanitizarEstilosOklch(clonedDoc, element.id);
            }
          },
          jsPDF: { unit: 'mm', format: 'a4', orientation: 'portrait' }
        })
        .from(element)
        .save();
    } finally {
      element.style.display = displayOriginal;
    }
  }

  private sanitizarEstilosOklch(clonedDoc: Document, idElemento: string): void {
    const elemento = clonedDoc.getElementById(idElemento);
    if (!elemento) return;

    elemento.style.display = 'block';
    elemento.style.backgroundColor = '#ffffff';
    elemento.style.color = '#0f172a';

    const elementos = elemento.querySelectorAll('*');
    elementos.forEach((el) => {
      const htmlEl = el as HTMLElement;
      htmlEl.style.boxShadow = 'none';
      htmlEl.style.textShadow = 'none';
      const style = window.getComputedStyle(htmlEl);
      if (style.color.includes('oklch')) htmlEl.style.color = '#0f172a';
      if (style.backgroundColor.includes('oklch')) htmlEl.style.backgroundColor = '#ffffff';
      if (style.borderColor.includes('oklch')) htmlEl.style.borderColor = '#cbd5e1';
    });
  }
}
