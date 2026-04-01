import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap, BehaviorSubject, Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ClockResponse,
  StatusResponse,
  TeamMemberStatus,
  WeekHistory,
  ActionType,
  ACTION_NUMERIC
} from '../../features/attendance/models/attendance.models';

@Injectable({ providedIn: 'root' })
export class AttendanceService {
  private readonly api = `${environment.apiUrl}/api/attendance`;

  // Single source of truth for the current user's status
  private readonly _status$ = new BehaviorSubject<StatusResponse | null>(null);
  readonly status$ = this._status$.asObservable();

  constructor(private http: HttpClient) {}

  fetchStatus(): Observable<StatusResponse> {
    return this.http.get<StatusResponse>(`${this.api}/status`).pipe(
      tap(s => this._status$.next(s))
    );
  }

  clock(actionType: ActionType): Observable<ClockResponse> {
    return this.http.post<ClockResponse>(`${this.api}/clock`, {
      actionType: ACTION_NUMERIC[actionType]
    });
  }

  getTeamStatus(): Observable<TeamMemberStatus[]> {
    return this.http.get<TeamMemberStatus[]>(`${this.api}/team-status`);
  }

  getHistory(weekOffset: number): Observable<WeekHistory> {
    return this.http.get<WeekHistory>(`${this.api}/history`, {
      params: { weekOffset: weekOffset.toString() }
    });
  }

  heartbeat(): Observable<void> {
    return this.http.post<void>(`${this.api}/heartbeat`, {});
  }
}
