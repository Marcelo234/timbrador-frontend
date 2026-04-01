export interface Usuario {
  id: string;
  nombres: string;
  apellidos: string;
  cedula: string;
  email: string;
}

export interface RegistroUsuario {
  nombres: string;
  apellidos: string;
  cedula: string;
  email: string;
  password: string;
  confirmarPassword: string;
}
