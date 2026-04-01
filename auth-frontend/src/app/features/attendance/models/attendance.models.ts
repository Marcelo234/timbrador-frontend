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
  lastSeenUtc: string | null;
  cycleComplete: boolean;
}

export interface ClockResponse {
  id: string;
  userId: string;
  actionType: ActionType;
  timestamp: string;
  nextAction: ActionType | null;
}

export const ACTION_LABELS: Record<ActionType, string> = {
  None:       'Sin iniciar',
  ClockIn:    'Trabajando',
  LunchStart: 'En almuerzo',
  LunchEnd:   'Trabajando',
  ClockOut:   'Jornada completada'
};

export const NEXT_ACTION_LABELS: Record<ActionType, string> = {
  None:       'Registrar Entrada',
  ClockIn:    'Iniciar Almuerzo',
  LunchStart: 'Terminar Almuerzo',
  LunchEnd:   'Registrar Salida',
  ClockOut:   ''
};

export const ACTION_NUMERIC: Record<ActionType, number> = {
  None: 0, ClockIn: 1, LunchStart: 2, LunchEnd: 3, ClockOut: 4
};

export interface DayRecord {
  date: string;
  dayOfWeek: string;
  isWorkDay: boolean;
  clockIn: string | null;
  lunchStart: string | null;
  lunchEnd: string | null;
  clockOut: string | null;
  effectiveHours: number | null;
  compliance: string | null;
}

export interface WeekHistory {
  weekStart: string;
  weekEnd: string;
  days: DayRecord[];
}
