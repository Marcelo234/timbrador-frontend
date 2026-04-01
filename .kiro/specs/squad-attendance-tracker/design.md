# Documento de Diseño Técnico — Squad Attendance Tracker

## Visión General

El sistema extiende el backend .NET y el frontend Angular existentes del proyecto de autenticación. No se crean proyectos nuevos: se agregan una entidad de dominio, una tabla en la base de datos SQLite existente, un controlador nuevo y componentes Angular dentro de la misma aplicación.

---

## Base de Datos

### Tabla: `AttendanceRecords`

| Campo        | Tipo         | Restricciones                              | Descripción                              |
|--------------|--------------|--------------------------------------------|------------------------------------------|
| `Id`         | `TEXT` (GUID)| PK, NOT NULL                               | Identificador único del registro         |
| `UserId`     | `TEXT` (GUID)| FK → `Usuarios.Id`, NOT NULL               | Usuario que realizó la acción            |
| `ActionType` | `INTEGER`    | NOT NULL, CHECK (1–4)                      | Tipo de acción (ver enum abajo)          |
| `Timestamp`  | `TEXT`       | NOT NULL                                   | Fecha y hora UTC del registro            |

### Enum `ActionType`

| Valor | Nombre       | Etiqueta en UI        |
|-------|--------------|-----------------------|
| `1`   | `ClockIn`    | Entrada               |
| `2`   | `LunchStart` | Inicio de Almuerzo    |
| `3`   | `LunchEnd`   | Fin de Almuerzo       |
| `4`   | `ClockOut`   | Salida                |

### Índices

- `UNIQUE (UserId, ActionType)` — impide que un usuario registre el mismo tipo de acción más de una vez por día (la unicidad por día se refuerza a nivel de aplicación).
- `INDEX (UserId)` — optimiza las consultas de historial por usuario.

### Relación con tabla existente

```
Usuarios (tabla existente)
  Id (PK, GUID)
  Nombres
  Apellidos
  Email
  ...
    │
    │ 1 : N
    ▼
AttendanceRecords (tabla nueva)
  Id (PK, GUID)
  UserId (FK → Usuarios.Id)
  ActionType (INTEGER 1-4)
  Timestamp (DATETIME UTC)
```

---

## Máquina de Estados del Ciclo Diario

```
  [Sin iniciar]
       │
       │ ClockIn (1)
       ▼
  [Trabajando]
       │
       │ LunchStart (2)
       ▼
  [En almuerzo]
       │
       │ LunchEnd (3)
       ▼
  [Trabajando]
       │
       │ ClockOut (4)
       ▼
  [Jornada completada] ── ciclo cerrado, no acepta más acciones hoy
```

La lógica de transición está centralizada en `AttendanceRecord.NextAction(ActionType? current)` en el dominio, garantizando que ningún endpoint pueda saltarse un paso.

---

## API — Endpoints

Todos los endpoints requieren `Authorization: Bearer {jwt}`. El `UserId` se extrae del claim `sub` del token, nunca del body.

### `POST /api/attendance/clock`

Registra la siguiente acción del ciclo para el usuario autenticado.

**Request body:**
```json
{ "actionType": 1 }
```

**Respuesta exitosa `200 OK`:**
```json
{
  "id": "uuid",
  "userId": "uuid",
  "actionType": "ClockIn",
  "timestamp": "2025-01-06T14:00:00Z",
  "nextAction": "LunchStart"
}
```

**Errores:**

| Código | Causa                                              |
|--------|----------------------------------------------------|
| `401`  | Token ausente o inválido                           |
| `409`  | Acción fuera de secuencia o ciclo ya completado    |

---

### `GET /api/attendance/status`

Retorna el estado actual del usuario autenticado para el día en curso.

**Respuesta `200 OK`:**
```json
{
  "currentAction": "ClockIn",
  "nextAction": "LunchStart",
  "cycleComplete": false
}
```

Cuando no hay registros hoy: `"currentAction": "None"`, `"nextAction": "ClockIn"`.

---

### `GET /api/attendance/team-status`

Retorna el estado de todos los usuarios registrados en el sistema para el día en curso.

**Respuesta `200 OK`:**
```json
[
  {
    "userId": "uuid",
    "nombre": "Marcelo García",
    "email": "marcelo@email.com",
    "currentAction": "ClockIn",
    "lastTimestamp": "2025-01-06T14:00:00Z",
    "cycleComplete": false
  },
  ...
]
```

---

### `GET /api/attendance/history?weekOffset={n}`

*(Pendiente de implementar — Requisito 5)*

Retorna el historial semanal del usuario autenticado. `weekOffset=0` es la semana actual, `-1` es la semana anterior.

**Respuesta `200 OK` (estructura planificada):**
```json
{
  "weekStart": "2025-01-06",
  "weekEnd": "2025-01-12",
  "days": [
    {
      "date": "2025-01-06",
      "dayOfWeek": "Lunes",
      "isWorkDay": true,
      "clockIn": "2025-01-06T14:00:00Z",
      "lunchStart": "2025-01-06T18:00:00Z",
      "lunchEnd": "2025-01-06T19:00:00Z",
      "clockOut": "2025-01-06T22:00:00Z",
      "effectiveHours": 7.0,
      "compliance": "Incompleto"
    }
  ]
}
```

---

## Frontend Angular — Componentes

### Componente existente a modificar: `DashboardComponent`

El `DashboardComponent` actual solo muestra el email del usuario. Se extenderá para incluir las tres secciones de asistencia.

**Estructura de secciones:**

```
DashboardComponent
├── Sección: Mi Jornada
│   ├── Nombre del usuario y fecha actual
│   ├── Botón dinámico de acción (cambia según estado)
│   └── Indicador de estado actual con etiqueta en español
│
├── Sección: Estado del Equipo
│   └── Tarjeta por cada miembro (nombre, estado, último timestamp)
│       Polling cada 60 segundos
│
└── Sección: Historial Semanal (fase 2)
    ├── Controles de navegación (semana anterior / siguiente)
    └── Tabla: Fecha | Entrada | Ini. Almuerzo | Fin Almuerzo | Salida | Horas | Cumplimiento
```

### Nuevo servicio: `AttendanceService`

Ubicación: `auth-frontend/src/app/core/services/attendance.service.ts`

Métodos:
- `clock(actionType: number): Observable<ClockResponse>`
- `getStatus(): Observable<StatusResponse>`
- `getTeamStatus(): Observable<TeamMemberStatus[]>`
- `getHistory(weekOffset: number): Observable<WeekHistory>` *(fase 2)*

### Modelos TypeScript

Ubicación: `auth-frontend/src/app/features/attendance/models/`

```typescript
export type ActionType = 'None' | 'ClockIn' | 'LunchStart' | 'LunchEnd' | 'ClockOut';

export interface StatusResponse {
  currentAction: ActionType;
  nextAction: ActionType | null;
  cycleComplete: boolean;
}

export interface TeamMemberStatus {
  userId: string;
  nombre: string;
  email: string;
  currentAction: ActionType;
  lastTimestamp: string | null;
  cycleComplete: boolean;
}

export interface ClockResponse {
  id: string;
  userId: string;
  actionType: ActionType;
  timestamp: string;
  nextAction: ActionType | null;
}
```

### Mapeo de etiquetas UI

| `ActionType`  | Etiqueta estado   | Etiqueta botón (próxima acción) |
|---------------|-------------------|---------------------------------|
| `None`        | Sin iniciar       | Entrada                         |
| `ClockIn`     | Trabajando        | Inicio de Almuerzo              |
| `LunchStart`  | En almuerzo       | Fin de Almuerzo                 |
| `LunchEnd`    | Trabajando        | Salida                          |
| `ClockOut`    | Jornada completada| — (botón oculto)                |

---

## Arquitectura de Capas (resumen)

```
Angular Frontend
  DashboardComponent
    └── AttendanceService (HTTP)
          │
          │ JWT en header Authorization
          ▼
.NET Backend
  AttendanceController
    └── AuthDbContext (EF Core / SQLite)
          └── AttendanceRecords ──FK──► Usuarios
```

---

## Fases de Implementación

| Fase | Alcance                                                        | Estado     |
|------|----------------------------------------------------------------|------------|
| 1    | Entidad, migración, `POST /clock`, `GET /status`, `GET /team-status` | ✅ Completo |
| 2    | `GET /history`, historial semanal en Angular                   | Pendiente  |
| 3    | Dashboard Angular completo con botón dinámico y estado equipo  | Pendiente  |
