import { Component, inject, ChangeDetectorRef, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';

import { AuthService } from '../../../../core/services/auth.service';
import { TokenService } from '../../../../core/services/token.service';
import { ErrorMessageComponent } from '../../../../shared/components/error-message/error-message.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, ErrorMessageComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private tokenService = inject(TokenService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  errorMessage = '';

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]]
  });

  get email() { return this.form.get('email'); }
  get password() { return this.form.get('password'); }

  ngOnInit(): void {
    // Wipe any stale/corrupt tokens so the interceptor starts clean
    this.tokenService.clearTokens();
  }

  onSubmit(): void {
    if (this.form.invalid) return;

    this.errorMessage = '';

    const { email, password } = this.form.value;

    this.authService.login({ email: email!, password: password! }).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: (err: HttpErrorResponse) => {
        if (err.status === 401) {
          this.errorMessage = 'Credenciales incorrectas. Por favor, intenta de nuevo';
        } else if (err.status === 429) {
          const retryAfter = err.headers.get('Retry-After') ?? '?';
          this.errorMessage = `Demasiados intentos. Intenta en ${retryAfter} segundos`;
        } else if (err.status === 0) {
          this.errorMessage = 'Error de conexión. Verifica tu conexión a internet';
        } else {
          this.errorMessage = 'Ocurrió un error inesperado. Intenta nuevamente';
        }
        this.cdr.detectChanges();
      }
    });
  }
}
