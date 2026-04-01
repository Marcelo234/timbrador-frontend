# Requirements Document

## Introduction

Este documento define los requisitos para un sistema de autenticación completo que incluye un frontend en Angular y un backend en C#/.NET. El sistema permitirá a los usuarios registrarse, iniciar sesión, gestionar sesiones de forma segura y recuperar contraseñas olvidadas.

## Glossary

- **Auth_System**: El sistema completo de autenticación que incluye frontend y backend
- **Auth_Backend**: El servicio backend en C#/.NET que gestiona la lógica de autenticación
- **Auth_Frontend**: La aplicación Angular que proporciona la interfaz de usuario
- **User**: Una persona que interactúa con el sistema de autenticación
- **Session**: Un período de tiempo durante el cual un usuario está autenticado
- **Access_Token**: Token JWT que identifica y autoriza al usuario autenticado
- **Refresh_Token**: Token de larga duración usado para obtener nuevos access tokens
- **Credentials**: Combinación de email y contraseña del usuario

## Requirements

### Requirement 1: Registro de Usuarios

**User Story:** Como un usuario nuevo, quiero registrarme en el sistema, para que pueda acceder a la aplicación.

#### Acceptance Criteria

1. WHEN un usuario proporciona email y contraseña válidos, THE Auth_Backend SHALL crear una nueva cuenta de usuario
2. WHEN un usuario intenta registrarse con un email ya existente, THE Auth_Backend SHALL retornar un error descriptivo
3. THE Auth_Backend SHALL validar que el email tenga formato válido antes de crear la cuenta
4. THE Auth_Backend SHALL validar que la contraseña tenga al menos 8 caracteres, una mayúscula, una minúscula y un número
5. THE Auth_Backend SHALL almacenar las contraseñas usando hash con bcrypt o algoritmo equivalente
6. WHEN el registro es exitoso, THE Auth_Backend SHALL retornar un código de estado 201 con los datos del usuario creado

### Requirement 2: Inicio de Sesión

**User Story:** Como un usuario registrado, quiero iniciar sesión con mis credenciales, para que pueda acceder a mi cuenta.

#### Acceptance Criteria

1. WHEN un usuario proporciona credenciales válidas, THE Auth_Backend SHALL generar un Access_Token y un Refresh_Token
2. WHEN un usuario proporciona credenciales inválidas, THE Auth_Backend SHALL retornar un error 401 sin revelar si el email o la contraseña son incorrectos
3. THE Auth_Backend SHALL generar Access_Tokens con expiración de 15 minutos
4. THE Auth_Backend SHALL generar Refresh_Tokens con expiración de 7 días
5. THE Auth_Backend SHALL incluir en el Access_Token el ID del usuario y roles asignados
6. WHEN el inicio de sesión es exitoso, THE Auth_Backend SHALL retornar ambos tokens en la respuesta

### Requirement 3: Renovación de Tokens

**User Story:** Como un usuario autenticado, quiero que mi sesión se renueve automáticamente, para que no tenga que iniciar sesión constantemente.

#### Acceptance Criteria

1. WHEN un Refresh_Token válido es proporcionado, THE Auth_Backend SHALL generar un nuevo Access_Token
2. WHEN un Refresh_Token expirado o inválido es proporcionado, THE Auth_Backend SHALL retornar un error 401
3. THE Auth_Backend SHALL validar que el Refresh_Token no haya sido revocado antes de generar un nuevo Access_Token
4. WHEN se genera un nuevo Access_Token, THE Auth_Backend SHALL mantener el mismo Refresh_Token si aún es válido

### Requirement 4: Cierre de Sesión

**User Story:** Como un usuario autenticado, quiero cerrar sesión, para que mi cuenta quede protegida cuando no esté usando la aplicación.

#### Acceptance Criteria

1. WHEN un usuario cierra sesión, THE Auth_Backend SHALL revocar el Refresh_Token asociado
2. THE Auth_Backend SHALL mantener una lista de tokens revocados hasta su fecha de expiración original
3. WHEN un token revocado es usado, THE Auth_Backend SHALL retornar un error 401
4. WHEN el cierre de sesión es exitoso, THE Auth_Backend SHALL retornar un código de estado 200

### Requirement 5: Recuperación de Contraseña

**User Story:** Como un usuario que olvidó su contraseña, quiero poder recuperar el acceso a mi cuenta, para que pueda volver a usar la aplicación.

#### Acceptance Criteria

1. WHEN un usuario solicita recuperación de contraseña con un email válido, THE Auth_Backend SHALL generar un token de recuperación con expiración de 1 hora
2. WHEN un usuario solicita recuperación de contraseña, THE Auth_Backend SHALL enviar un email con el enlace de recuperación
3. WHEN un usuario solicita recuperación para un email no registrado, THE Auth_Backend SHALL retornar éxito sin revelar que el email no existe
4. WHEN un usuario proporciona un token de recuperación válido y una nueva contraseña, THE Auth_Backend SHALL actualizar la contraseña
5. WHEN la contraseña es actualizada, THE Auth_Backend SHALL revocar todos los Refresh_Tokens existentes del usuario
6. THE Auth_Backend SHALL validar que la nueva contraseña cumpla los requisitos de complejidad

### Requirement 6: Protección de Endpoints

**User Story:** Como desarrollador, quiero que los endpoints protegidos validen tokens, para que solo usuarios autenticados puedan acceder.

#### Acceptance Criteria

1. WHEN un request incluye un Access_Token válido, THE Auth_Backend SHALL permitir el acceso al endpoint protegido
2. WHEN un request no incluye Access_Token, THE Auth_Backend SHALL retornar un error 401
3. WHEN un request incluye un Access_Token expirado, THE Auth_Backend SHALL retornar un error 401 con mensaje indicando expiración
4. WHEN un request incluye un Access_Token inválido, THE Auth_Backend SHALL retornar un error 401
5. THE Auth_Backend SHALL extraer la información del usuario del Access_Token y hacerla disponible al endpoint

### Requirement 7: Interfaz de Usuario - Formularios

**User Story:** Como usuario, quiero formularios intuitivos para registro e inicio de sesión, para que pueda autenticarme fácilmente.

#### Acceptance Criteria

1. THE Auth_Frontend SHALL proporcionar un formulario de registro con campos para nombres,apellidos, cedula, email, contraseña y confimar contraseña
2. THE Auth_Frontend SHALL proporcionar un formulario de inicio de sesión con campos para email y contraseña
3. WHEN un usuario ingresa datos en los formularios, THE Auth_Frontend SHALL validar el formato del email en tiempo real
4. WHEN un usuario ingresa una contraseña en el registro, THE Auth_Frontend SHALL mostrar indicadores de fortaleza de contraseña
5. WHEN el backend retorna errores de validación, THE Auth_Frontend SHALL mostrar mensajes de error claros junto a los campos correspondientes
6. THE Auth_Frontend SHALL deshabilitar el botón de envío mientras una petición está en proceso

### Requirement 8: Gestión de Tokens en Frontend

**User Story:** Como desarrollador frontend, quiero que los tokens se gestionen automáticamente, para que las peticiones autenticadas funcionen sin intervención manual.

#### Acceptance Criteria

1. WHEN el usuario inicia sesión exitosamente, THE Auth_Frontend SHALL almacenar el Access_Token y Refresh_Token de forma segura
2. WHEN el Access_Token expira, THE Auth_Frontend SHALL usar el Refresh_Token para obtener uno nuevo automáticamente
3. WHEN el Refresh_Token expira o es inválido, THE Auth_Frontend SHALL redirigir al usuario a la página de inicio de sesión
4. THE Auth_Frontend SHALL incluir el Access_Token en el header Authorization de todas las peticiones autenticadas
5. WHEN el usuario cierra sesión, THE Auth_Frontend SHALL eliminar todos los tokens almacenados

### Requirement 9: Manejo de Errores y Feedback

**User Story:** Como usuario, quiero recibir feedback claro sobre el estado de mis acciones, para que entienda qué está sucediendo.

#### Acceptance Criteria

1. WHEN una operación está en proceso, THE Auth_Frontend SHALL mostrar un indicador de carga
2. WHEN una operación es exitosa, THE Auth_Frontend SHALL mostrar un mensaje de confirmación
3. WHEN una operación falla, THE Auth_Frontend SHALL mostrar un mensaje de error descriptivo
4. THE Auth_Frontend SHALL mostrar mensajes de error durante máximo 5 segundos o hasta que el usuario los cierre
5. WHEN hay un error de red, THE Auth_Frontend SHALL mostrar un mensaje indicando problemas de conexión

### Requirement 10: Seguridad y Mejores Prácticas

**User Story:** Como administrador del sistema, quiero que el sistema siga mejores prácticas de seguridad, para que los datos de los usuarios estén protegidos.

#### Acceptance Criteria

1. THE Auth_Backend SHALL implementar rate limiting de 5 intentos de inicio de sesión por minuto por dirección IP
2. WHEN se excede el rate limit, THE Auth_Backend SHALL retornar un error 429 con tiempo de espera
3. THE Auth_Backend SHALL usar HTTPS para todas las comunicaciones
4. THE Auth_Backend SHALL incluir headers de seguridad apropiados en todas las respuestas
5. THE Auth_Frontend SHALL almacenar tokens usando httpOnly cookies o almacenamiento seguro del navegador
6. THE Auth_Backend SHALL registrar todos los intentos de autenticación fallidos con timestamp e IP

