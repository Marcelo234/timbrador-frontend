import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { forkJoin, interval, Subscription, switchMap } from 'rxjs';

import { AuthService } from '../../core/services/auth.service';
import { AttendanceService } from '../../core/services/attendance.service';
import { Usuario } from '../auth/models/usuario.model';
import {
  ActionType,
  StatusResponse,
  TeamMemberStatus,
  WeekHistory,
  ACTION_LABELS,
  NEXT_ACTION_LABELS
} from '../attendance/models/attendance.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit, OnDestroy {
  currentUser: Usuario | null = null;

  // Null = still loading (never show button until server responds)
  status: StatusResponse | null = null;
  statusError = false;

  teamStatus: TeamMemberStatus[] = [];
  clocking = false;
  errorMessage = '';
  today = new Date();

  // History
  weekHistory: WeekHistory | null = null;
  weekOffset = 0;
  loadingHistory = false;

  // Toast
  toastMessage = '';
  toastVisible = false;
  private toastTimer?: ReturnType<typeof setTimeout>;

  private statusSub?: Subscription;
  private pollSub?: Subscription;
  private heartbeatSub?: Subscription;

  // How often to refresh status + team (ms)
  private static readonly POLL_INTERVAL = 60_000;

  constructor(
    private authService: AuthService,
    private attendanceService: AttendanceService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.authService.loadCurrentUser();
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
      this.cdr.detectChanges();
    });

    // Subscribe to the shared status stream — updates whenever fetchStatus() is called
    this.statusSub = this.attendanceService.status$.subscribe(s => {
      this.status = s;
      this.cdr.detectChanges();
    });

    // Single combined poll: status + team every 10 seconds.
    // Initial load fires immediately via the poll, subsequent ticks keep both in sync.
    this.refreshAll();
    this.pollSub = interval(DashboardComponent.POLL_INTERVAL).pipe(
      switchMap(() => forkJoin({
        status: this.attendanceService.fetchStatus(),
        team:   this.attendanceService.getTeamStatus()
      }))
    ).subscribe({
      next: ({ team }) => {
        // status$ is already updated inside fetchStatus() via BehaviorSubject
        this.teamStatus = team;
        this.cdr.detectChanges();
      },
      error: (err) => {
        if (err?.status === 401) this.authService.logout();
      }
    });

    this.loadHistory();

    // Heartbeat every 10 seconds (piggybacks on the same interval cadence)
    this.attendanceService.heartbeat().subscribe();
    this.heartbeatSub = interval(DashboardComponent.POLL_INTERVAL).subscribe(() => {
      this.attendanceService.heartbeat().subscribe();
    });
  }

  ngOnDestroy(): void {
    this.statusSub?.unsubscribe();
    this.pollSub?.unsubscribe();
    this.heartbeatSub?.unsubscribe();
    clearTimeout(this.toastTimer);
  }

  /** Fetches status + team in parallel and updates both in one detectChanges pass. */
  private refreshAll(): void {
    forkJoin({
      status: this.attendanceService.fetchStatus(),
      team:   this.attendanceService.getTeamStatus()
    }).subscribe({
      next: ({ team }) => {
        this.teamStatus = team;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.statusError = true;
        if (err?.status === 401) this.authService.logout();
        this.cdr.detectChanges();
      }
    });
  }

  clock(): void {
    if (!this.status?.nextAction || this.clocking) return;
    this.clocking = true;
    this.errorMessage = '';

    const actionLabel = NEXT_ACTION_LABELS[this.status.currentAction ?? 'None'];

    this.attendanceService.clock(this.status.nextAction).pipe(
      // Chain fetchStatus() so the BehaviorSubject is updated BEFORE next() fires
      switchMap(() => this.attendanceService.fetchStatus())
    ).subscribe({
      next: () => {
        this.showToast(`✅ ${actionLabel} registrada correctamente`);
        this.refreshAll();
        if (this.weekOffset === 0) this.loadHistory();
        this.clocking = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.errorMessage = err?.error?.error ?? 'Error al registrar la acción';
        this.clocking = false;
        this.cdr.detectChanges();
      }
    });
  }

  logout(): void {
    this.authService.logout();
  }

  loadHistory(): void {
    this.loadingHistory = true;
    this.attendanceService.getHistory(this.weekOffset).subscribe({
      next: h => { this.weekHistory = h; this.loadingHistory = false; this.cdr.detectChanges(); },
      error: () => { this.loadingHistory = false; this.cdr.detectChanges(); }
    });
  }

  prevWeek(): void {
    this.weekOffset--;
    this.loadHistory();
  }

  nextWeek(): void {
    if (this.weekOffset < 0) {
      this.weekOffset++;
      this.loadHistory();
    }
  }

  get canGoNext(): boolean {
    return this.weekOffset < 0;
  }

  formatDate(iso: string): string {
    return new Date(iso + 'T00:00:00').toLocaleDateString('es-ES', { day: '2-digit', month: '2-digit' });
  }

  formatHours(hours: number | null): string {
    if (hours === null) return '—';
    const h = Math.floor(hours);
    const m = Math.round((hours - h) * 60);
    return m > 0 ? `${h}h ${m}m` : `${h}h`;
  }

  // ─── UI helpers ──────────────────────────────────────────────────────────────

  get currentLabel(): string {
    return ACTION_LABELS[this.status?.currentAction ?? 'None'];
  }

  get nextLabel(): string {
    return NEXT_ACTION_LABELS[this.status?.currentAction ?? 'None'];
  }

  get buttonColor(): string {
    const map: Record<ActionType, string> = {
      None:       'btn-green',
      ClockIn:    'btn-orange',
      LunchStart: 'btn-blue',
      LunchEnd:   'btn-red',
      ClockOut:   ''
    };
    return map[this.status?.currentAction ?? 'None'];
  }

  get cycleComplete(): boolean {
    return (this.status?.cycleComplete ?? false) || this.status?.currentAction === 'ClockOut';
  }

  teamMemberLabel(action: ActionType): string {
    return ACTION_LABELS[action];
  }

  teamMemberIndicator(action: ActionType): string {
    const map: Record<ActionType, string> = {
      None:       '⚪',
      ClockIn:    '🟢',
      LunchStart: '🟡',
      LunchEnd:   '🟢',
      ClockOut:   '🔴'
    };
    return map[action];
  }

  isCurrentUser(member: TeamMemberStatus): boolean {
    return member.userId === this.currentUser?.id;
  }

  formatTime(iso: string | null): string {
    if (!iso) return '—';
    const utcStr = iso.endsWith('Z') || iso.includes('+') ? iso : iso + 'Z';
    return new Date(utcStr).toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit' });
  }

  showToast(message: string): void {
    clearTimeout(this.toastTimer);
    this.toastMessage = message;
    this.toastVisible = true;
    this.cdr.detectChanges();
    this.toastTimer = setTimeout(() => {
      this.toastVisible = false;
      this.cdr.detectChanges();
    }, 3500);
  }

  isOnline(member: TeamMemberStatus): boolean {
    if (!member.lastSeenUtc) return false;
    const utcStr = member.lastSeenUtc.endsWith('Z') ? member.lastSeenUtc : member.lastSeenUtc + 'Z';
    return (Date.now() - new Date(utcStr).getTime()) < 5 * 60 * 1000;
  }
}
