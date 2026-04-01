import { Component, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs/operators';

import { AuthService } from '../../../../core/services/auth.service';
import { PasswordValidators } from '../../../../shared/validators/password.validators';
import { LoadingSpinnerComponent } from '../../../../shared/components/loading-spinner/loading-spinner.component';
import { ErrorMessageComponent } from '../../../../shared/components/error-message/error-message.component';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, LoadingSpinnerComponent, ErrorMessageComponent],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);

  loading = false;
  errorMessage = '';

  form = this.fb.group(
    {
      nombres: ['', [Validators.required]],
      apellidos: ['', [Validators.required]],
      cedula: ['', [Validators.required, Validators.pattern(/^\d{10}$/)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, PasswordValidators.strong()]],
      confirmarPassword: ['', [Validators.required]]
    },
    { validators: PasswordValidators.match('password', 'confirmarPassword') }
  );

  get nombres() { return this.form.get('nombres'); }
  get apellidos() { return this.form.get('apellidos'); }
  get cedula() { return this.form.get('cedula'); }
  get email() { return this.form.get('email'); }
  get password() { return this.form.get('password'); }
  get confirmarPassword() { return this.form.get('confirmarPassword'); }

  get passwordStrength() {
    return this.password?.errors?.['passwordStrength'] as
      | { minLength: boolean; hasUppercase: boolean; hasLowercase: boolean; hasNumber: boolean }
      | undefined;
  }

  onSubmit(): void {
    if (this.form.invalid || this.loading) return;

    this.loading = true;
    this.errorMessage = '';

    const { nombres, apellidos, cedula, email, password, confirmarPassword } = this.form.value;

    this.authService
      .register({
        nombres: nombres!,
        apellidos: apellidos!,
        cedula: cedula!,
        email: email!,
        password: password!,
        confirmarPassword: confirmarPassword!
      })
      .pipe(finalize(() => { this.loading = false; }))
      .subscribe({
        next: () => {
          this.router.navigate(['/login'], { queryParams: { registered: 'true' } });
        },
        error: (err: HttpErrorResponse) => {
          if (err.status === 400) {
            this.errorMessage = 'El email o cédula ya están registrados';
          } else if (err.status === 0) {
            this.errorMessage = 'Error de conexión. Verifica tu conexión a internet';
          } else {
            this.errorMessage = 'Ocurrió un error inesperado. Intenta nuevamente';
          }
        }
      });
  }
}
