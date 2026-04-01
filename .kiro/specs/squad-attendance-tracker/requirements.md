# Documento de Requisitos

## Introducción

El **Squad Attendance Tracker** es un sistema de control de asistencia laboral diseñado para un equipo de 3 freelancers. El sistema reutiliza la autenticación JWT existente (.NET + Angular) y la tabla `Users` ya poblada con las 3 cuentas. Permite registrar las 4 acciones del ciclo de trabajo diario (entrada, inicio de almuerzo, fin de almuerzo y salida), visualizar el estado en tiempo real del equipo y consultar el historial semanal de cumplimiento de horas.

---

## Glosario

- **Attendance_API**: Backend .NET que expone los endpoints de asistencia.
- **Attendance_Dashboard**: Frontend Angular que muestra el panel principal de asistencia.
- **AttendanceRecord**: Registro individual de una acción de asistencia (entrada, almuerzo, salida).
- **ActionType**: Tipo de acción registrada. Valores posibles: `ClockIn`, `LunchStart`, `LunchEnd`, `ClockOut`.
- **Estado_Actual**: El último `ActionType` registrado por un usuario en el día en curso.
- **Ciclo_Diario**: Secuencia ordenada de 4 acciones: `ClockIn` → `LunchStart` → `LunchEnd` → `ClockOut`.
- **Jornada_Laboral**: Período de trabajo de 8 horas efectivas con 1 hora de almuerzo, aplicable los días lunes y jueves.
- **Equipo**: Conjunto fijo de 3 usuarios registrados en la tabla `Users` existente.
- **Semana_Laboral**: Período de lunes a domingo utilizado como unidad de agrupación en el historial.

---

## Requisitos

### Requisito 1: Registro de Acciones de Asistencia

**User Story:** Como freelancer, quiero registrar mis acciones de entrada, almuerzo y salida, para que el sistema tenga un historial preciso de mi jornada laboral.

#### Criterios de Aceptación

1. WHEN un usuario autenticado envía una solicitud `POST /api/attendance/clock`, THE Attendance_API SHALL crear un `AttendanceRecord` con los campos `UserId`, `ActionType` y `Timestamp` (UTC) en la base de datos.
2. WHEN el `ActionType` recibido no corresponde al siguiente paso esperado en el `Ciclo_Diario` del usuario, THE Attendance_API SHALL rechazar la solicitud con un código HTTP 409 y un mensaje descriptivo del estado actual.
3. WHEN un usuario ya completó el `Ciclo_Diario` en el día en curso, THE Attendance_API SHALL rechazar cualquier nueva solicitud de registro con un código HTTP 409.
4. IF el token JWT de la solicitud es inválido o está ausente, THEN THE Attendance_API SHALL rechazar la solicitud con un código HTTP 401.
5. THE Attendance_API SHALL registrar el `Timestamp` usando la hora UTC del servidor en el momento de recibir la solicitud, sin aceptar timestamps enviados por el cliente.

---

### Requisito 2: Consulta de Estado Actual

**User Story:** Como freelancer, quiero consultar mi estado actual y el de mis compañeros, para que el panel muestre la acción disponible en cada momento.

#### Criterios de Aceptación

1. WHEN un usuario autenticado realiza `GET /api/attendance/status`, THE Attendance_API SHALL retornar el `Estado_Actual` del usuario autenticado para el día en curso.
2. WHEN un usuario no ha registrado ninguna acción en el día en curso, THE Attendance_API SHALL retornar un estado `None` indicando que la próxima acción disponible es `ClockIn`.
3. WHEN un usuario autenticado realiza `GET /api/attendance/team-status`, THE Attendance_API SHALL retornar el `Estado_Actual` de los 3 miembros del `Equipo` para el día en curso.
4. IF el token JWT de la solicitud es inválido o está ausente, THEN THE Attendance_API SHALL rechazar la solicitud con un código HTTP 401.

---

### Requisito 3: Panel Principal con Botón Dinámico

**User Story:** Como freelancer, quiero ver un botón que refleje mi próxima acción disponible, para que no tenga que recordar en qué paso del ciclo me encuentro.

#### Criterios de Aceptación

1. WHEN el `Estado_Actual` del usuario es `None`, THE Attendance_Dashboard SHALL mostrar un botón con la etiqueta "Entrada".
2. WHEN el `Estado_Actual` del usuario es `ClockIn`, THE Attendance_Dashboard SHALL mostrar un botón con la etiqueta "Inicio de Almuerzo".
3. WHEN el `Estado_Actual` del usuario es `LunchStart`, THE Attendance_Dashboard SHALL mostrar un botón con la etiqueta "Fin de Almuerzo".
4. WHEN el `Estado_Actual` del usuario es `LunchEnd`, THE Attendance_Dashboard SHALL mostrar un botón con la etiqueta "Salida".
5. WHEN el `Estado_Actual` del usuario es `ClockOut`, THE Attendance_Dashboard SHALL ocultar el botón de acción y mostrar un mensaje indicando que el ciclo del día fue completado.
6. WHEN el usuario hace clic en el botón de acción, THE Attendance_Dashboard SHALL enviar la solicitud correspondiente a `POST /api/attendance/clock` y actualizar el estado mostrado sin recargar la página.

---

### Requisito 4: Sección de Estado del Equipo

**User Story:** Como freelancer, quiero ver el estado actual de mis 2 compañeros en el panel, para que pueda saber si están disponibles o en almuerzo.

#### Criterios de Aceptación

1. THE Attendance_Dashboard SHALL mostrar una sección "Estado del Equipo" con los 3 miembros del `Equipo`, incluyendo al usuario autenticado.
2. WHEN se carga el panel, THE Attendance_Dashboard SHALL consultar `GET /api/attendance/team-status` y mostrar el `Estado_Actual` de cada miembro con una etiqueta legible en español.
3. THE Attendance_Dashboard SHALL representar cada `ActionType` con las siguientes etiquetas: `None` → "Sin iniciar", `ClockIn` → "Trabajando", `LunchStart` → "En almuerzo", `LunchEnd` → "Trabajando", `ClockOut` → "Jornada completada".
4. WHILE el panel está abierto, THE Attendance_Dashboard SHALL actualizar la sección "Estado del Equipo" cada 60 segundos mediante polling a `GET /api/attendance/team-status`.

---

### Requisito 5: Historial Semanal

**User Story:** Como freelancer, quiero ver un historial semanal de mi asistencia, para que pueda verificar si cumplí las horas requeridas cada semana.

#### Criterios de Aceptación

1. WHEN un usuario autenticado realiza `GET /api/attendance/history?weekOffset={n}`, THE Attendance_API SHALL retornar todos los `AttendanceRecord` del usuario para la `Semana_Laboral` indicada, donde `weekOffset=0` es la semana actual y valores negativos representan semanas anteriores.
2. THE Attendance_API SHALL calcular las horas efectivas trabajadas por día como la diferencia entre `ClockOut` y `ClockIn`, menos la duración del almuerzo (`LunchEnd` - `LunchStart`).
3. WHEN un día de la semana es lunes o jueves y el usuario completó el `Ciclo_Diario`, THE Attendance_API SHALL indicar en la respuesta si las horas efectivas fueron iguales o superiores a 8 horas.
4. WHEN un día de la semana es lunes o jueves y el usuario no completó el `Ciclo_Diario`, THE Attendance_API SHALL indicar ese día como "Incompleto" en la respuesta.
5. THE Attendance_Dashboard SHALL mostrar el historial semanal en una tabla con columnas: Fecha, Entrada, Inicio Almuerzo, Fin Almuerzo, Salida, Horas Efectivas y Cumplimiento.
6. THE Attendance_Dashboard SHALL permitir al usuario navegar entre semanas anteriores usando controles de paginación que invoquen `GET /api/attendance/history` con el `weekOffset` correspondiente.

---

### Requisito 6: Persistencia y Modelo de Datos

**User Story:** Como desarrollador, quiero una tabla de base de datos bien definida para los registros de asistencia, para que los datos sean consistentes y relacionados con los usuarios existentes.

#### Criterios de Aceptación

1. THE Attendance_API SHALL utilizar una tabla `AttendanceRecords` con los campos: `Id` (clave primaria), `UserId` (clave foránea a `Users.Id`), `ActionType` (enumeración), `Timestamp` (datetime UTC).
2. THE Attendance_API SHALL aplicar una restricción de unicidad sobre (`UserId`, `ActionType`, fecha UTC) para evitar registros duplicados del mismo tipo en el mismo día.
3. THE Attendance_API SHALL mantener la integridad referencial entre `AttendanceRecords.UserId` y `Users.Id` mediante una restricción de clave foránea.
