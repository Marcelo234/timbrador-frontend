import { Component, inject, ChangeDetectorRef } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';

import { AuthService } from '../../../../core/services/auth.service';
import { ErrorMessageComponent } from '../../../../shared/components/error-message/error-message.component';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, ErrorMessageComponent],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.scss'
})
export class ForgotPasswordComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);

  errorMessage = '';
  successMessage = '';

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]]
  });

  get email() { return this.form.get('email'); }

  onSubmit(): void {
    if (this.form.invalid) return;

    this.errorMessage = '';
    this.successMessage = '';

    this.authService.forgotPassword(this.form.value.email!).subscribe({
      next: () => {
        this.successMessage = 'Si el correo está registrado, recibirás instrucciones en breve';
        this.cdr.detectChanges();
      },
      error: (err: HttpErrorResponse) => {
        if (err.status === 0) {
          this.errorMessage = 'Error de conexión. Verifica tu conexión a internet';
        } else {
          // Never reveal whether the email exists
          this.successMessage = 'Si el correo está registrado, recibirás instrucciones en breve';
        }
        this.cdr.detectChanges();
      }
    });
  }
}
