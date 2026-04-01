using System.Security.Claims;
using AuthBackend.Application.DTOs;
using AuthBackend.Domain.Entities;
using AuthBackend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthBackend.Infrastructure.Controllers;

[ApiController]
[Route("api/attendance")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly AuthDbContext _db;

    public AttendanceController(AuthDbContext db)
    {
        _db = db;
    }

    // ─── POST /api/attendance/clock ──────────────────────────────────────────────
    [HttpPost("clock")]
    public async Task<IActionResult> Clock([FromBody] ClockRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new { error = "Token inválido: claim 'sub' no encontrado" });

        var (today, tomorrow) = GetLocalDayBoundsUtc();

        // Load today's records for this user, ordered by timestamp
        var todayRecords = await _db.AttendanceRecords
            .Where(r => r.UserId == userId.Value && r.Timestamp >= today && r.Timestamp < tomorrow)
            .OrderBy(r => r.Timestamp)
            .ToListAsync();

        // Determine current state (last action today)
        var lastAction = todayRecords.LastOrDefault()?.ActionType;

        // Determine what the next valid action is
        var expectedNext = AttendanceRecord.NextAction(lastAction);

        if (expectedNext == null)
            return Conflict(new { error = "Ya completaste el ciclo de trabajo de hoy." });

        if (request.ActionType != expectedNext)
        {
            var currentLabel = lastAction == null ? "Sin iniciar" : lastAction.ToString();
            return Conflict(new
            {
                error = $"Acción inválida. Estado actual: '{currentLabel}'. La próxima acción esperada es: '{expectedNext}'."
            });
        }

        var record = new AttendanceRecord(userId.Value, request.ActionType);
        _db.AttendanceRecords.Add(record);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            id = record.Id,
            userId = record.UserId,
            actionType = record.ActionType.ToString(),
            timestamp = DateTime.SpecifyKind(record.Timestamp, DateTimeKind.Utc),
            nextAction = AttendanceRecord.NextAction(record.ActionType)?.ToString()
        });
    }

    // ─── GET /api/attendance/status ──────────────────────────────────────────────
    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        var (today, tomorrow) = GetLocalDayBoundsUtc();
        var last = await _db.AttendanceRecords
            .Where(r => r.UserId == userId.Value && r.Timestamp >= today && r.Timestamp < tomorrow)
            .OrderByDescending(r => r.Timestamp)
            .FirstOrDefaultAsync();

        var current = last?.ActionType;
        var next = AttendanceRecord.NextAction(current);

        return Ok(new
        {
            currentAction = current?.ToString() ?? "None",
            nextAction = next?.ToString(),
            cycleComplete = next == null && current != null
        });
    }

    // ─── POST /api/attendance/heartbeat ─────────────────────────────────────────
    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var presence = await _db.UserPresence.FindAsync(userId.Value);
        if (presence == null)
        {
            presence = new UserPresence(userId.Value);
            _db.UserPresence.Add(presence);
        }
        else
        {
            presence.Touch();
            _db.UserPresence.Update(presence);
        }

        await _db.SaveChangesAsync();
        return Ok(new { lastSeenUtc = DateTime.SpecifyKind(presence.LastSeenUtc, DateTimeKind.Utc) });
    }

    // ─── GET /api/attendance/team-status ─────────────────────────────────────────
    [HttpGet("team-status")]
    public async Task<IActionResult> TeamStatus()
    {
        var (today, tomorrow) = GetLocalDayBoundsUtc();

        var latestPerUser = await _db.AttendanceRecords
            .Where(r => r.Timestamp >= today && r.Timestamp < tomorrow)
            .GroupBy(r => r.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                LastAction = g.OrderByDescending(r => r.Timestamp).First().ActionType,
                LastTimestamp = g.OrderByDescending(r => r.Timestamp).First().Timestamp
            })
            .ToListAsync();

        var presenceMap = await _db.UserPresence
            .ToDictionaryAsync(p => p.UserId, p => p.LastSeenUtc);

        var users = await _db.Usuarios
            .Select(u => new { u.Id, u.Nombres, u.Apellidos, u.Email })
            .ToListAsync();

        var result = users.Select(u =>
        {
            var status = latestPerUser.FirstOrDefault(s => s.UserId == u.Id);
            var current = status?.LastAction;
            var lastTs = status?.LastTimestamp is DateTime ts
                ? DateTime.SpecifyKind(ts, DateTimeKind.Utc)
                : (DateTime?)null;
            var lastSeen = presenceMap.TryGetValue(u.Id, out var ls)
                ? DateTime.SpecifyKind(ls, DateTimeKind.Utc)
                : (DateTime?)null;
            return new
            {
                userId = u.Id,
                nombre = $"{u.Nombres} {u.Apellidos}",
                email = u.Email,
                currentAction = current?.ToString() ?? "None",
                lastTimestamp = lastTs,
                lastSeenUtc = lastSeen,
                cycleComplete = current == ActionType.ClockOut
            };
        });

        return Ok(result);
    }

    // ─── GET /api/attendance/history ─────────────────────────────────────────────
    [HttpGet("history")]
    public async Task<IActionResult> History([FromQuery] int weekOffset = 0)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        // Calculate Monday of the target week in local (Ecuador) time — fixed UTC-5 offset, no DST
        var ecuadorOffset = TimeSpan.FromHours(-5);
        var localToday = (DateTime.UtcNow + ecuadorOffset).Date;
        var currentMonday = localToday.AddDays(-(((int)localToday.DayOfWeek + 6) % 7));
        var weekStartLocal = currentMonday.AddDays(weekOffset * 7);
        var weekEndLocal   = weekStartLocal.AddDays(6); // Sunday

        // Convert week boundaries to UTC for DB queries
        var weekStartUtc = weekStartLocal - ecuadorOffset;
        var weekEndUtc   = weekStartUtc.AddDays(7); // exclusive upper bound (Mon 05:00 UTC next week)

        // Query all records for the user in this week range
        var records = await _db.AttendanceRecords
            .Where(r => r.UserId == userId.Value
                     && r.Timestamp >= weekStartUtc
                     && r.Timestamp < weekEndUtc)
            .OrderBy(r => r.Timestamp)
            .ToListAsync();

        // Group by local date using the fixed offset
        var days = new List<DayHistoryDto>();
        for (int i = 0; i < 7; i++)
        {
            var localDate   = weekStartLocal.AddDays(i);
            var dayStartUtc = localDate - ecuadorOffset;
            var dayEndUtc   = dayStartUtc.AddDays(1);
            var dayRecords  = records.Where(r => r.Timestamp >= dayStartUtc && r.Timestamp < dayEndUtc).ToList();

            var clockIn    = dayRecords.FirstOrDefault(r => r.ActionType == ActionType.ClockIn)?.Timestamp;
            var lunchStart = dayRecords.FirstOrDefault(r => r.ActionType == ActionType.LunchStart)?.Timestamp;
            var lunchEnd   = dayRecords.FirstOrDefault(r => r.ActionType == ActionType.LunchEnd)?.Timestamp;
            var clockOut   = dayRecords.FirstOrDefault(r => r.ActionType == ActionType.ClockOut)?.Timestamp;

            // isWorkDay = Monday (1) or Thursday (4) in DayOfWeek
            var isWorkDay = localDate.DayOfWeek == System.DayOfWeek.Monday
                         || localDate.DayOfWeek == System.DayOfWeek.Thursday;

            // Calculate effective hours only when cycle is complete
            double? effectiveHours = null;
            string? compliance = null;

            bool cycleComplete = clockIn.HasValue && lunchStart.HasValue
                              && lunchEnd.HasValue && clockOut.HasValue;

            if (cycleComplete)
            {
                var lunchDuration = lunchEnd!.Value - lunchStart!.Value;
                var totalWorked   = clockOut!.Value - clockIn!.Value;
                effectiveHours    = Math.Round((totalWorked - lunchDuration).TotalHours, 2);

                if (isWorkDay)
                    compliance = effectiveHours >= 8.0 ? "Cumplido" : "Incompleto";
            }
            else if (isWorkDay && dayRecords.Count > 0)
            {
                // Started but didn't complete the cycle on a work day
                compliance = "Incompleto";
            }

            days.Add(new DayHistoryDto
            {
                Date        = localDate.ToString("yyyy-MM-dd"),
                DayOfWeek   = GetSpanishDayName(localDate.DayOfWeek),
                IsWorkDay   = isWorkDay,
                ClockIn     = clockIn.HasValue    ? DateTime.SpecifyKind(clockIn.Value,    DateTimeKind.Utc) : null,
                LunchStart  = lunchStart.HasValue ? DateTime.SpecifyKind(lunchStart.Value, DateTimeKind.Utc) : null,
                LunchEnd    = lunchEnd.HasValue   ? DateTime.SpecifyKind(lunchEnd.Value,   DateTimeKind.Utc) : null,
                ClockOut    = clockOut.HasValue   ? DateTime.SpecifyKind(clockOut.Value,   DateTimeKind.Utc) : null,
                EffectiveHours = effectiveHours,
                Compliance  = compliance
            });
        }

        var response = new WeekHistoryResponse
        {
            WeekStart = weekStartLocal.ToString("yyyy-MM-dd"),
            WeekEnd   = weekEndLocal.ToString("yyyy-MM-dd"),
            Days      = days
        };

        return Ok(response);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────────
    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    /// <summary>
    /// Returns the UTC boundaries [start, end) that correspond to "today" in
    /// Ecuador time (UTC-5, no DST). Uses a fixed offset to avoid platform
    /// differences between Windows ("SA Pacific Standard Time") and Linux
    /// ("America/Guayaquil") timezone IDs.
    /// </summary>
    private static (DateTime start, DateTime end) GetLocalDayBoundsUtc()
    {
        var ecuadorOffset = TimeSpan.FromHours(-5);
        var localNow = DateTime.UtcNow + ecuadorOffset;
        var localToday = localNow.Date;                          // midnight ECT today
        var startUtc = localToday - ecuadorOffset;               // convert back to UTC
        return (startUtc, startUtc.AddDays(1));
    }

    private static string GetSpanishDayName(System.DayOfWeek day) => day switch
    {
        System.DayOfWeek.Monday    => "Lunes",
        System.DayOfWeek.Tuesday   => "Martes",
        System.DayOfWeek.Wednesday => "Miércoles",
        System.DayOfWeek.Thursday  => "Jueves",
        System.DayOfWeek.Friday    => "Viernes",
        System.DayOfWeek.Saturday  => "Sábado",
        System.DayOfWeek.Sunday    => "Domingo",
        _                          => day.ToString()
    };
}

public record ClockRequest(ActionType ActionType);
