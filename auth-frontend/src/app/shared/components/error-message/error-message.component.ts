import {
  Component,
  Input,
  Output,
  EventEmitter,
  OnChanges,
  OnDestroy,
  SimpleChanges
} from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-error-message',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (message) {
      <div class="error-container" role="alert">
        <span class="error-text">{{ message }}</span>
        <button class="close-btn" (click)="dismiss()" aria-label="Cerrar mensaje de error">
          &times;
        </button>
      </div>
    }
  `,
  styles: [`
    .error-container {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0.75rem 1rem;
      background-color: #fee2e2;
      border: 1px solid #fca5a5;
      border-radius: 0.375rem;
      color: #b91c1c;
      margin-bottom: 1rem;
    }

    .error-text {
      flex: 1;
      font-size: 0.875rem;
    }

    .close-btn {
      background: none;
      border: none;
      color: #b91c1c;
      cursor: pointer;
      font-size: 1.25rem;
      line-height: 1;
      padding: 0 0 0 0.75rem;
      opacity: 0.7;
    }

    .close-btn:hover {
      opacity: 1;
    }
  `]
})
export class ErrorMessageComponent implements OnChanges, OnDestroy {
  @Input() message = '';
  @Output() dismissed = new EventEmitter<void>();

  private dismissTimer: ReturnType<typeof setTimeout> | null = null;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['message']) {
      this.clearTimer();
      if (this.message) {
        this.dismissTimer = setTimeout(() => this.dismiss(), 5000);
      }
    }
  }

  ngOnDestroy(): void {
    this.clearTimer();
  }

  dismiss(): void {
    this.clearTimer();
    this.dismissed.emit();
  }

  private clearTimer(): void {
    if (this.dismissTimer !== null) {
      clearTimeout(this.dismissTimer);
      this.dismissTimer = null;
    }
  }
}
