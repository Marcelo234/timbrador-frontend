# Plan de Implementación — Squad Attendance Tracker

## Tasks

- [x] 1. Backend — Entidad y base de datos
  - [x] 1.1 Crear entidad `AttendanceRecord` con enum `ActionType` y lógica `NextAction()`
  - [x] 1.2 Crear `AttendanceRecordConfiguration` (EF Core, índice único, FK a Usuarios)
  - [x] 1.3 Registrar `DbSet<AttendanceRecord>` en `AuthDbContext`
  - [x] 1.4 Generar y aplicar migración `AddAttendanceRecords`

- [x] 2. Backend — API endpoints
  - [x] 2.1 Implementar `POST /api/attendance/clock` con validación de máquina de estados
  - [x] 2.2 Implementar `GET /api/attendance/status` (estado del usuario autenticado)
  - [x] 2.3 Implementar `GET /api/attendance/team-status` (estado de todos los usuarios)

- [x] 3. Documentación
  - [x] 3.1 Completar `design.md` con esquema de BD, endpoints y plan de componentes Angular

- [-] 4. Frontend — Servicio y modelos
  - [x] 4.1 Crear modelos TypeScript (`ActionType`, `StatusResponse`, `TeamMemberStatus`, `ClockResponse`)
  - [x] 4.2 Crear `AttendanceService` con métodos `clock()`, `getStatus()`, `getTeamStatus()`

- [x] 5. Frontend — Dashboard
  - [x] 5.1 Modificar `DashboardComponent` con lógica de estado y polling del equipo
  - [x] 5.2 Crear template HTML con diseño de tarjetas (Mi Jornada + Estado del Equipo)
  - [x] 5.3 Crear estilos CSS con botón dinámico por color de estado

- [x] 6. Backend — Historial semanal (fase 2)
  - [x] 6.1 Implementar `GET /api/attendance/history?weekOffset={n}`
  - [x] 6.2 Calcular horas efectivas y cumplimiento por día

- [x] 7. Frontend — Historial semanal (fase 2)
  - [x] 7.1 Agregar `getHistory()` al `AttendanceService`
  - [x] 7.2 Agregar sección de historial con tabla y navegación semanal al Dashboard
