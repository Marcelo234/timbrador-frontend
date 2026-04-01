# Plan de Implementación: Sistema de Autenticación

## Overview

Implementación del sistema de autenticación completo con backend C#/.NET (Clean Architecture) y frontend Angular 17+. Las tareas siguen el orden de dependencias: Domain → Application → Infrastructure → Frontend, asegurando que cada paso integre con el anterior.

## Tasks

- [x] 1. Configurar estructura de solución y proyectos
  - Crear solución `AuthBackend.sln` con tres proyectos: `AuthBackend.Domain`, `AuthBackend.Application`, `AuthBackend.Infrastructure`
  - Agregar referencias entre proyectos: Application → Domain, Infrastructure → Application + Domain
  - Instalar paquetes NuGet: `BCrypt.Net-Next`, `FluentValidation`, `Microsoft.EntityFrameworkCore`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `FsCheck.Xunit`, `xunit`, `Moq`, `coverlet.collector`
  - Crear proyectos de test: `AuthBackend.Domain.Tests`, `AuthBackend.Application.Tests`, `AuthBackend.Infrastructure.Tests`
  - _Requirements: 1.1, 2.1, 10.1_

- [x] 2. Implementar Domain Layer — Entidades y Excepciones
  - [x] 2.1 Crear entidades de dominio
    - Implementar `EstadoUsuario.cs` con clase `EstadosUsuario` (constantes Activo=1, Inactivo=2, Suspendido=3)
    - Implementar `Usuario.cs` con constructor, `ActualizarPassword()` y `EstaActivo()`
    - Implementar `Sesion.cs` con constructor, `Revocar()` y `EstaVigente()`
    - Implementar `TokenRecuperacion.cs` con constructor, `MarcarComoUsado()` y `EsValido()`
    - _Requirements: 1.1, 2.1, 4.1, 5.1_

  - [x] 2.2 Escribir property tests para entidades de dominio
    - **Property 8: Configuración de expiración de tokens** — `Sesion` creada con `DateTime.UtcNow.AddDays(7)` debe tener `FechaExpiracion` exactamente 7 días después
    - **Property 12: Logout revoca token** — Después de `Revocar()`, `EstaVigente()` debe retornar `false` y `FechaRevocacion` debe estar establecida
    - **Property 14: Token revocado no puede usarse** — Para cualquier `Sesion` revocada, `EstaVigente()` siempre retorna `false`
    - **Property 15: Token de recuperación expira en 1 hora** — `TokenRecuperacion` creado debe tener `FechaExpiracion` exactamente 1 hora después de `FechaCreacion`
    - **Validates: Requirements 2.3, 2.4, 4.1, 4.3, 5.1**

  - [x] 2.3 Crear excepciones de dominio
    - Implementar `UsuarioYaExisteException`, `CredencialesInvalidasException`, `TokenInvalidoException`, `UsuarioInactivoException`
    - _Requirements: 1.2, 2.2, 4.3, 5.4_

  - [x] 2.4 Crear interfaces de repositorios y servicios
    - Implementar `IUsuarioRepository`, `ISesionRepository`, `ITokenRepository`, `IPasswordEncoder`, `IEmailService`
    - _Requirements: 1.1, 2.1, 4.1, 5.1_

- [x] 3. Implementar Application Layer — DTOs, Validators y Use Cases
  - [x] 3.1 Crear DTOs
    - Implementar `RegistroUsuarioDto`, `LoginDto`, `TokenResponseDto`, `RecuperacionPasswordDto`, `RestablecerPasswordDto`, `RefreshTokenDto`, `LogoutDto`, `UsuarioResponseDto`
    - _Requirements: 1.1, 2.1, 3.1, 4.1, 5.1_

  - [x] 3.2 Implementar validadores FluentValidation
    - Implementar `RegistroUsuarioValidator` con reglas para Nombres, Apellidos, Cédula (10 dígitos), Email, Password (8+ chars, mayúscula, minúscula, número) y ConfirmarPassword
    - Implementar `PasswordValidator` reutilizable para validación de contraseña en restablecimiento
    - _Requirements: 1.3, 1.4, 5.6_

  - [x] 3.3 Escribir property tests para validadores
    - **Property 3: Validación de formato de email** — Para cualquier string sin `@`, el validator debe retornar error en el campo Email
    - **Property 4: Validación de complejidad de contraseña** — Para cualquier contraseña que no cumpla los 4 requisitos (longitud, mayúscula, minúscula, número), el validator debe retornar error
    - **Validates: Requirements 1.3, 1.4, 5.6**

  - [x] 3.4 Implementar `RegistrarUsuarioUseCase`
    - Verificar email duplicado con `IUsuarioRepository.ExisteUsuario()`, lanzar `UsuarioYaExisteException` si existe
    - Hashear contraseña con `IPasswordEncoder.HashPassword()`
    - Crear `Usuario` con `EstadosUsuario.Activo` por defecto y guardar con `IUsuarioRepository.GuardarUsuario()`
    - _Requirements: 1.1, 1.2, 1.5_

  - [x]* 3.5 Escribir property tests para `RegistrarUsuarioUseCase`
    - **Property 1: Registro exitoso crea usuario** — Para cualquier DTO válido, el use case debe crear un usuario con ID único y llamar `GuardarUsuario` exactamente una vez
    - **Property 1b: Estado inicial Activo al registrarse** — Para cualquier registro exitoso, `usuarioGuardado.EstadoId` debe ser `EstadosUsuario.Activo`
    - **Property 2: Email duplicado rechazado** — Para cualquier email existente, debe lanzar `UsuarioYaExisteException`
    - **Property 5: Contraseñas hasheadas** — Para cualquier contraseña, el hash almacenado debe ser diferente al original y verificable con `IPasswordEncoder.CompararPassword()`
    - **Validates: Requirements 1.1, 1.2, 1.5**

  - [x] 3.6 Implementar `IniciarSesionUseCase`
    - Buscar usuario por email; si no existe o contraseña incorrecta, lanzar `CredencialesInvalidasException`
    - Verificar `usuario.EstaActivo()`, lanzar `UsuarioInactivoException` si no está activo
    - Generar `accessToken` y `refreshToken` con `JwtTokenService`, crear `Sesion` y guardar con `ISesionRepository.GuardarSesion()`
    - Retornar `TokenResponseDto` con `ExpiresIn = 900`
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

  - [x]* 3.7 Escribir property tests para `IniciarSesionUseCase`
    - **Property 6: Login exitoso genera tokens** — Para cualquier usuario activo con credenciales válidas, debe retornar `TokenResponseDto` con ambos tokens y llamar `GuardarSesion` una vez
    - **Property 7: Credenciales inválidas rechazadas uniformemente** — Para email inexistente o contraseña incorrecta, debe lanzar `CredencialesInvalidasException` con el mismo mensaje genérico
    - **Property 7b: Usuario inactivo no puede iniciar sesión** — Para cualquier usuario con `EstadoId` Inactivo o Suspendido, debe lanzar `UsuarioInactivoException` y nunca llamar `GuardarSesion`
    - **Validates: Requirements 2.1, 2.2**

  - [x] 3.8 Implementar `CerrarSesionUseCase`
    - Llamar `ISesionRepository.FinalizarSesion(refreshToken)` para revocar la sesión
    - _Requirements: 4.1, 4.4_

  - [x]* 3.9 Escribir unit tests para `CerrarSesionUseCase`
    - Verificar que `FinalizarSesion` es llamado con el token correcto
    - _Requirements: 4.1_

  - [x] 3.10 Implementar `SolicitarRecuperacionPasswordUseCase`
    - Buscar usuario por email; si no existe, retornar sin lanzar excepción ni llamar `IEmailService`
    - Si existe, generar token con `Guid.NewGuid().ToString("N")`, crear `TokenRecuperacion`, guardar y llamar `IEmailService.EnviarRecuperacionPassword()`
    - _Requirements: 5.1, 5.2, 5.3_

  - [x]* 3.11 Escribir property tests para `SolicitarRecuperacionPasswordUseCase`
    - **Property 15: Solicitud genera token con expiración de 1 hora** — Para cualquier email existente, el token creado debe tener `FechaExpiracion` = `FechaCreacion + 1 hora`
    - **Property 16: Email enviado para usuarios existentes** — Para cualquier email existente, `EnviarRecuperacionPassword` debe ser invocado exactamente una vez
    - **Property 17: Respuesta uniforme para emails no registrados** — Para cualquier email no registrado, no debe lanzar excepción y no debe invocar `IEmailService`
    - **Validates: Requirements 5.1, 5.2, 5.3**

  - [x] 3.12 Implementar `RestablecerPasswordUseCase`
    - Buscar token de recuperación; si no existe o `!EsValido()`, lanzar `TokenInvalidoException`
    - Verificar que el usuario corresponde al token; actualizar contraseña con `ActualizarPassword()` y `IPasswordEncoder.HashPassword()`
    - Marcar token como usado con `MarcarComoUsado()` e invalidar con `ITokenRepository.InvalidarToken()`
    - Revocar todas las sesiones con `ISesionRepository.RevocarTodasLasSesionesDelUsuario()`
    - _Requirements: 5.4, 5.5, 5.6_

  - [x]* 3.13 Escribir property tests para `RestablecerPasswordUseCase`
    - **Property 18: Restablecimiento actualiza contraseña** — Para cualquier token válido y contraseña válida, debe llamar `ActualizarUsuario` y `InvalidarToken` exactamente una vez
    - **Property 19: Restablecimiento revoca todas las sesiones** — Para cualquier restablecimiento exitoso, debe llamar `RevocarTodasLasSesionesDelUsuario` exactamente una vez
    - **Validates: Requirements 5.4, 5.5**

- [x] 4. Checkpoint — Verificar capa de dominio y aplicación
  - Asegurar que todos los tests pasen, preguntar al usuario si hay dudas antes de continuar.

- [x] 5. Implementar Infrastructure Layer — Persistencia
  - [x] 5.1 Crear `AuthDbContext` y configuraciones EF Core
    - Implementar `AuthDbContext` con `DbSet<Usuario>`, `DbSet<EstadoUsuario>`, `DbSet<Sesion>`, `DbSet<TokenRecuperacion>`
    - Implementar `EstadoUsuarioConfiguration` con seed data (Activo=1, Inactivo=2, Suspendido=3) y `ValueGeneratedNever()`
    - Implementar `UsuarioConfiguration` con índices únicos en Email y Cedula, FK a EstadosUsuario con `DeleteBehavior.Restrict`, `HasDefaultValue(EstadosUsuario.Activo)`
    - Implementar `SesionConfiguration` y `TokenRecuperacionConfiguration` con índices en tokens
    - _Requirements: 1.1, 2.1, 4.2_

  - [x] 5.2 Implementar repositorios
    - Implementar `UsuarioRepository` con `BuscarPorEmail`, `ExisteUsuario`, `GuardarUsuario`, `ActualizarUsuario`
    - Implementar `SesionRepository` con `GuardarSesion`, `BuscarPorToken`, `FinalizarSesion` (marca `Revocado=true`), `RevocarTodasLasSesionesDelUsuario`
    - Implementar `TokenRepository` con `GuardarToken`, `BuscarToken`, `InvalidarToken` (marca `Usado=true`)
    - _Requirements: 1.1, 2.1, 3.1, 4.1, 5.1_

  - [x]* 5.3 Escribir integration tests para repositorios con base de datos en memoria
    - Verificar que `GuardarSesion` + `BuscarPorToken` retorna la sesión correcta
    - Verificar que `FinalizarSesion` establece `Revocado=true` y `FechaRevocacion`
    - Verificar que `RevocarTodasLasSesionesDelUsuario` revoca solo las sesiones del usuario indicado
    - **Property 13: Tokens revocados persisten hasta expiración** — Verificar que sesiones revocadas permanecen en BD
    - **Validates: Requirements 4.1, 4.2, 4.3**

- [x] 6. Implementar Infrastructure Layer — Servicios
  - [x] 6.1 Implementar `PasswordEncoder`
    - Implementar `HashPassword()` con `BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12)`
    - Implementar `CompararPassword()` con `BCrypt.Net.BCrypt.Verify()`
    - _Requirements: 1.5_

  - [x]* 6.2 Escribir property test para `PasswordEncoder`
    - **Property 5: Contraseñas hasheadas (implementación real)** — Para cualquier string no vacío, `HashPassword` debe retornar un valor diferente al original y `CompararPassword(original, hash)` debe retornar `true`
    - **Validates: Requirements 1.5**

  - [x] 6.3 Implementar `JwtTokenService`
    - Implementar `GenerarAccessToken()` con claims: `sub` (userId), `email`, `jti`, `nombres`, `apellidos`; expiración 15 minutos; firma HMAC-SHA256
    - Implementar `GenerarRefreshToken()` con 64 bytes aleatorios en Base64
    - _Requirements: 2.3, 2.4, 2.5_

  - [x]* 6.4 Escribir property tests para `JwtTokenService`
    - **Property 8: Configuración de expiración de tokens (JwtTokenService)** — Para cualquier usuario válido, el AccessToken generado debe expirar entre `now+15min` y `now+15min+1seg`
    - **Property 9: Claims en AccessToken** — Para cualquier usuario, el token decodificado debe contener `sub`, `email`, `nombres` y `apellidos` con los valores correctos
    - **Validates: Requirements 2.3, 2.4, 2.5**

  - [x] 6.5 Implementar `EmailService`
    - Implementar `EnviarRecuperacionPassword()` con `SmtpClient`, construyendo URL con `{Frontend:BaseUrl}/reset-password?token={token}`
    - _Requirements: 5.2_

- [x] 7. Implementar Infrastructure Layer — Middleware y Controller
  - [x] 7.1 Implementar `ExceptionHandlingMiddleware`
    - Mapear excepciones a códigos HTTP: `UsuarioYaExisteException`→400, `CredencialesInvalidasException`→401, `TokenInvalidoException`→401, `UsuarioInactivoException`→403, `ValidationException`→400, genérica→500
    - Retornar JSON con `{ error, timestamp }` y loguear con `ILogger`
    - _Requirements: 1.2, 2.2, 9.3_

  - [x]* 7.2 Escribir unit tests para `ExceptionHandlingMiddleware`
    - Verificar mapeo correcto de cada tipo de excepción a su código HTTP
    - Verificar formato de respuesta JSON con campos `error` y `timestamp`
    - _Requirements: 1.2, 2.2_

  - [x] 7.3 Implementar `RateLimitingMiddleware`
    - Aplicar rate limiting solo a `/api/auth/login` y `/api/auth/register`
    - Usar `ConcurrentDictionary<string, (int Count, DateTime ResetTime)>` con clave `{ip}:{endpoint}`
    - Retornar 429 con header `Retry-After` cuando se excedan 5 requests/minuto por IP
    - _Requirements: 10.1, 10.2_

  - [x]* 7.4 Escribir property tests para `RateLimitingMiddleware`
    - **Property 32: Rate limiting por IP** — Para cualquier IP, después de 5 requests en menos de 1 minuto, el siguiente debe retornar 429
    - **Property 33: Respuesta 429 con Retry-After** — Para cualquier request que exceda el límite, la respuesta debe incluir header `Retry-After` con valor > 0
    - **Validates: Requirements 10.1, 10.2**

  - [x] 7.5 Implementar `AuthController` con todos los endpoints
    - `POST /api/auth/register` → 201 con `UsuarioResponseDto`
    - `POST /api/auth/login` → 200 con `TokenResponseDto`
    - `POST /api/auth/refresh` → buscar sesión por token, validar `EstaVigente()`, generar nuevo `AccessToken`, retornar 200
    - `POST /api/auth/logout` → `[Authorize]`, 200 con mensaje
    - `POST /api/auth/forgot-password` → 200 con mensaje genérico
    - `POST /api/auth/reset-password` → 200 con mensaje
    - _Requirements: 1.6, 2.6, 3.1, 3.2, 4.4, 5.1, 5.3_

  - [x]* 7.6 Escribir unit tests para `AuthController`
    - Verificar que cada endpoint retorna el código HTTP correcto en el caso exitoso
    - **Property 20: Token válido permite acceso** — Verificar que endpoint con `[Authorize]` permite request con token válido
    - **Property 21: Ausencia de token rechazada** — Verificar 401 en endpoint protegido sin header Authorization
    - **Property 22: Token expirado rechazado** — Verificar 401 con token expirado
    - **Property 23: Token inválido rechazado** — Verificar 401 con token de firma inválida
    - **Validates: Requirements 1.6, 2.6, 3.1, 3.2, 4.4, 6.1, 6.2, 6.3, 6.4, 6.5**

  - [x] 7.7 Configurar `Program.cs`
    - Registrar todos los use cases, repositorios y servicios en DI
    - Configurar JWT Bearer Authentication con parámetros de validación
    - Registrar `ExceptionHandlingMiddleware` y `RateLimitingMiddleware`
    - Configurar EF Core con connection string, agregar migraciones iniciales
    - Configurar CORS para el frontend Angular
    - Agregar headers de seguridad (`X-Content-Type-Options`, `X-Frame-Options`, `X-XSS-Protection`)
    - _Requirements: 6.1, 10.3, 10.4_

  - [x]* 7.8 Escribir property test para headers de seguridad
    - **Property 34: Headers de seguridad incluidos** — Para cualquier respuesta HTTP del backend, verificar presencia de `X-Content-Type-Options` y `X-Frame-Options`
    - **Validates: Requirements 10.4**

- [x] 8. Checkpoint — Verificar backend completo
  - Asegurar que todos los tests pasen, ejecutar `dotnet test`, preguntar al usuario si hay dudas antes de continuar.

- [x] 9. Implementar Frontend Angular — Estructura base y modelos
  - [x] 9.1 Crear proyecto Angular y estructura de carpetas
    - Crear módulos: `CoreModule`, `AuthModule`, `SharedModule`
    - Crear estructura de carpetas: `core/guards`, `core/interceptors`, `core/services`, `features/auth/components`, `features/auth/models`, `shared/components`, `shared/validators`
    - Configurar `environments/environment.ts` con `apiUrl`
    - Instalar `fast-check` para property-based testing
    - _Requirements: 7.1, 8.1_

  - [x] 9.2 Crear modelos TypeScript
    - Implementar `usuario.model.ts` con interfaces `Usuario` y `RegistroUsuario`
    - Implementar `login.model.ts` con interface `LoginCredentials`
    - Implementar `token-response.model.ts` con interface `TokenResponse`
    - _Requirements: 7.1, 7.2, 8.1_

- [x] 10. Implementar Frontend Angular — Core Services
  - [x] 10.1 Implementar `TokenService`
    - Implementar `saveTokens()`, `saveAccessToken()`, `getAccessToken()`, `getRefreshToken()`, `clearTokens()`
    - Implementar `hasValidToken()` decodificando el JWT y verificando `exp`
    - Implementar `decodeToken()` con `atob()` sobre el payload Base64
    - _Requirements: 8.1, 8.2, 8.3, 8.5_

  - [ ]* 10.2 Escribir property tests para `TokenService`
    - **Property 27: Tokens almacenados tras login exitoso** — Para cualquier par de strings como tokens, `saveTokens` + `getAccessToken`/`getRefreshToken` debe retornar los mismos valores
    - **Property 31: Tokens eliminados tras logout** — Después de `clearTokens()`, `getAccessToken()` y `getRefreshToken()` deben retornar `null`
    - **Validates: Requirements 8.1, 8.5**

  - [x] 10.3 Implementar `AuthService`
    - Implementar `register()`, `login()` (guarda tokens con `TokenService` en `tap`), `logout()` (limpia tokens en `finalize`), `refreshToken()`, `forgotPassword()`, `resetPassword()`
    - Implementar `isAuthenticated()` delegando a `TokenService.hasValidToken()`
    - Implementar `loadCurrentUser()` decodificando el access token y actualizando `currentUserSubject`
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5_

  - [ ]* 10.4 Escribir unit tests para `AuthService`
    - Verificar que `login()` llama `TokenService.saveTokens()` con los tokens de la respuesta
    - Verificar que `logout()` llama `TokenService.clearTokens()` y navega a `/login`
    - _Requirements: 8.1, 8.5_

  - [x] 10.5 Implementar `AuthInterceptor`
    - Agregar header `Authorization: Bearer {token}` a todos los requests si existe access token
    - En error 401: si no está renovando, llamar `authService.refreshToken()`, reintentar request original con nuevo token
    - Si la renovación falla: limpiar tokens y redirigir a `/login`
    - Manejar requests concurrentes durante renovación con `BehaviorSubject` y `filter`/`take(1)`
    - _Requirements: 8.2, 8.3, 8.4_

  - [ ]* 10.6 Escribir property tests para `AuthInterceptor`
    - **Property 28: Renovación automática de token expirado** — Para cualquier request que reciba 401, el interceptor debe intentar renovar antes de reintentar
    - **Property 29: Redirección tras fallo de renovación** — Para cualquier fallo en `refreshToken()`, debe llamar `clearTokens()` y navegar a `/login`
    - **Property 30: Header Authorization incluido** — Para cualquier request con token almacenado, el header `Authorization` debe tener formato `Bearer {token}`
    - **Validates: Requirements 8.2, 8.3, 8.4**

  - [x] 10.7 Implementar `AuthGuard`
    - Verificar `authService.isAuthenticated()`; si es `false`, navegar a `/login` con `queryParams: { returnUrl: state.url }`
    - _Requirements: 8.3_

- [x] 11. Implementar Frontend Angular — Shared
  - [x] 11.1 Implementar `PasswordValidators`
    - Implementar `strong()`: validar longitud ≥ 8, mayúscula, minúscula y número; retornar `{ passwordStrength: {...} }` si falla
    - Implementar `match(controlName, matchingControlName)`: validar que dos campos coincidan; retornar `{ passwordMismatch: true }` si no coinciden
    - _Requirements: 1.4, 7.4_

  - [ ]* 11.2 Escribir property tests para `PasswordValidators`
    - **Property 4: Validación de complejidad de contraseña (frontend)** — Para cualquier contraseña que no cumpla los 4 requisitos, `strong()` debe retornar error `passwordStrength`
    - **Property 24: Validación de email en tiempo real** — Para cualquier email válido (con `@` y dominio), `Validators.email` debe retornar `null`; para cualquier string sin `@`, debe retornar error
    - **Validates: Requirements 1.4, 7.3, 7.4**

  - [x] 11.3 Crear componentes compartidos
    - Implementar `LoadingSpinnerComponent` con `@Input() loading: boolean`
    - Implementar `ErrorMessageComponent` con `@Input() message: string` y auto-dismiss a los 5 segundos
    - _Requirements: 9.1, 9.4_

- [x] 12. Implementar Frontend Angular — Componentes de autenticación
  - [x] 12.1 Implementar `LoginComponent`
    - Crear `ReactiveForm` con campos `email` (Validators.required, Validators.email) y `password` (Validators.required)
    - Deshabilitar botón submit mientras `loading = true`
    - En submit: llamar `authService.login()`, navegar a `/dashboard` en éxito, mostrar error en fallo (401→"Credenciales inválidas", 429→mensaje con Retry-After, 0→"Error de conexión")
    - _Requirements: 7.2, 7.3, 7.5, 7.6, 9.1, 9.2, 9.3, 9.5_

  - [ ]* 12.2 Escribir unit tests para `LoginComponent`
    - **Property 26: Botón deshabilitado durante petición** — Verificar que el botón submit está deshabilitado cuando `loading = true`
    - Verificar que errores del backend se muestran en el componente
    - _Requirements: 7.5, 7.6_

  - [x] 12.3 Implementar `RegisterComponent`
    - Crear `ReactiveForm` con campos: `nombres`, `apellidos`, `cedula`, `email`, `password` (con `PasswordValidators.strong()`), `confirmarPassword`
    - Agregar validador de grupo `PasswordValidators.match('password', 'confirmarPassword')`
    - Mostrar indicador de fortaleza de contraseña basado en errores de `passwordStrength`
    - Deshabilitar botón submit mientras `loading = true`
    - En submit: llamar `authService.register()`, navegar a `/login` en éxito con mensaje de confirmación
    - _Requirements: 7.1, 7.3, 7.4, 7.5, 7.6, 9.1, 9.2, 9.3_

  - [ ]* 12.4 Escribir unit tests para `RegisterComponent`
    - Verificar que el indicador de fortaleza se muestra cuando la contraseña no cumple requisitos
    - Verificar que el error de contraseñas no coincidentes se muestra correctamente
    - _Requirements: 7.4, 7.5_

  - [x] 12.5 Implementar `ForgotPasswordComponent`
    - Crear `ReactiveForm` con campo `email` (Validators.required, Validators.email)
    - En submit: llamar `authService.forgotPassword()`, mostrar mensaje de confirmación genérico en éxito
    - _Requirements: 5.3, 7.3, 9.2_

  - [x] 12.6 Implementar `ResetPasswordComponent`
    - Leer `token` y `email` de `ActivatedRoute.queryParams`
    - Crear `ReactiveForm` con campos `nuevaPassword` y `confirmarPassword` con validadores de fortaleza y coincidencia
    - En submit: llamar `authService.resetPassword()`, navegar a `/login` en éxito
    - _Requirements: 5.4, 5.6, 7.3, 7.5, 9.2_

- [x] 13. Configurar routing y módulos Angular
  - Configurar `AuthRoutingModule` con rutas: `/login`, `/register`, `/forgot-password`, `/reset-password`
  - Registrar `AuthInterceptor` en `CoreModule` con `HTTP_INTERCEPTORS`
  - Aplicar `AuthGuard` a rutas protegidas (ej: `/dashboard`)
  - _Requirements: 8.2, 8.3, 8.4_

- [x] 14. Checkpoint final — Verificar sistema completo
  - Asegurar que todos los tests del backend (`dotnet test`) y frontend (`ng test --watch=false`) pasen, preguntar al usuario si hay dudas.

## Notes

- Las tareas marcadas con `*` son opcionales y pueden omitirse para un MVP más rápido
- Cada tarea referencia requisitos específicos para trazabilidad completa
- Los property tests usan FsCheck (backend) y fast-check (frontend) con mínimo 100 iteraciones
- Los unit tests cubren edge cases y flujos de error específicos
- Las propiedades 1b y 7b son críticas para la lógica de estados de usuario
- El seed data de `EstadosUsuario` debe aplicarse en la migración inicial de EF Core
