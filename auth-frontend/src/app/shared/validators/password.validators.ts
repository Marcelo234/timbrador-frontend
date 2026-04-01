import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export class PasswordValidators {
  /**
   * Validates password strength: min 8 chars, uppercase, lowercase, number.
   * Returns null if all requirements are met.
   * Returns { passwordStrength: { minLength, hasUppercase, hasLowercase, hasNumber } }
   * where each boolean is true if the requirement IS met.
   */
  static strong(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const value: string = control.value ?? '';

      const minLength = value.length >= 8;
      const hasUppercase = /[A-Z]/.test(value);
      const hasLowercase = /[a-z]/.test(value);
      const hasNumber = /[0-9]/.test(value);

      if (minLength && hasUppercase && hasLowercase && hasNumber) {
        return null;
      }

      return {
        passwordStrength: { minLength, hasUppercase, hasLowercase, hasNumber }
      };
    };
  }

  /**
   * Validates that two form controls in a group have matching values.
   * Sets { passwordMismatch: true } on the matchingControl if values differ.
   * Always returns null (error is set on the child control, not the group).
   */
  static match(controlName: string, matchingControlName: string): ValidatorFn {
    return (group: AbstractControl): ValidationErrors | null => {
      const control = group.get(controlName);
      const matchingControl = group.get(matchingControlName);

      if (!control || !matchingControl) {
        return null;
      }

      if (control.value !== matchingControl.value) {
        matchingControl.setErrors({ passwordMismatch: true });
      } else {
        // Clear only the passwordMismatch error, preserving other errors
        const errors = matchingControl.errors;
        if (errors) {
          const { passwordMismatch, ...remaining } = errors;
          matchingControl.setErrors(Object.keys(remaining).length ? remaining : null);
        }
      }

      return null;
    };
  }
}
