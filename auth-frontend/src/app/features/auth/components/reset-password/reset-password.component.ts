import { Component, OnInit, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs/operators';

import { AuthService } from '../../../../core/services/auth.service';
import { PasswordValidators } from '../../../../shared/validators/password.validators';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { ErrorMessageComponent } from '../../../../shared/components/error-message/error-message.component';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, LoadingSpinnerComponent, ErrorMessageComponent],
  templateUrl: './reset-password.component.html',
  styleUrl: './reset-password.component.scss'
})
export class ResetPasswordComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  loading = false;
  errorMessage = '';

  token = '';
  email = '';

  form = this.fb.group(
    {
      nuevaPassword: ['', [Validators.required, PasswordValidators.strong()]],
      confirmarPassword: ['', [Validators.required]]
    },
    { validators: PasswordValidators.match('nuevaPassword', 'confirmarPassword') }
  );

  get nuevaPassword() { return this.form.get('nuevaPassword'); }
  get confirmarPassword() { return this.form.get('confirmarPassword'); }

  get passwordStrength() {
    return this.nuevaPassword?.errors?.['passwordStrength'] as
      | { minLength: boolean; hasUppercase: boolean; hasLowercase: boolean; hasNumber: boolean }
      | undefined;
  }

  ngOnInit(): void {
    const params = this.route.snapshot.queryParams;
    this.token = params['token'] ?? '';
    this.email = params['email'] ?? '';

    if (!this.token) {
      this.errorMessage = 'Enlace de recuperación inválido';
      this.form.disable();
    }
  }

  onSubmit(): void {
    if (this.form.invalid || this.loading) return;

    this.loading = true;
    this.errorMessage = '';

    const { nuevaPassword, confirmarPassword } = this.form.value;

    this.authService
      .resetPassword(this.token, this.email, nuevaPassword!, confirmarPassword!)
      .pipe(finalize(() => { this.loading = false; }))
      .subscribe({
        next: () => {
          this.router.navigate(['/login'], { queryParams: { passwordReset: 'true' } });
        },
        error: (err: HttpErrorResponse) => {
          if (err.status === 401) {
            this.errorMessage = 'El enlace de recuperación ha expirado o es inválido';
          } else if (err.status === 0) {
            this.errorMessage = 'Error de conexión. Verifica tu conexión a internet';
          } else {
            this.errorMessage = 'Ocurrió un error inesperado. Intenta nuevamente';
          }
        }
      });
  }
}
