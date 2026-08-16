import { AfterViewInit, Directive, ElementRef, EventEmitter, HostListener, inject, OnDestroy, Output } from '@angular/core';

@Directive({
  selector: '[appModalA11y]',
  standalone: true,
})
export class ModalA11yDirective implements AfterViewInit, OnDestroy {
  private el = inject<ElementRef<HTMLElement>>(ElementRef);
  private elementoPrevio: HTMLElement | null = null;

  @Output() appModalClose = new EventEmitter<void>();

  ngAfterViewInit(): void {
    const elemento = this.el.nativeElement;

    if (!elemento.hasAttribute('role')) {
      elemento.setAttribute('role', 'dialog');
    }
    if (!elemento.hasAttribute('aria-modal')) {
      elemento.setAttribute('aria-modal', 'true');
    }
    if (!elemento.hasAttribute('tabindex')) {
      elemento.setAttribute('tabindex', '-1');
    }

    this.elementoPrevio = document.activeElement as HTMLElement | null;
    (elemento as HTMLElement).focus();
  }

  @HostListener('keydown.escape')
  onEscape(): void {
    this.appModalClose.emit();
  }

  ngOnDestroy(): void {
    if (this.elementoPrevio && this.elementoPrevio.focus) {
      this.elementoPrevio.focus();
    }
  }
}