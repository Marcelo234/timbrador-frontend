namespace AuthBackend.Application.DTOs;

public record WeekHistoryResponse
{
    public string WeekStart { get; init; } = string.Empty;
    public string WeekEnd { get; init; } = string.Empty;
    public List<DayHistoryDto> Days { get; init; } = new();
}

public record DayHistoryDto
{
    public string Date { get; init; } = string.Empty;
    public string DayOfWeek { get; init; } = string.Empty;
    public bool IsWorkDay { get; init; }
    public DateTime? ClockIn { get; init; }
    public DateTime? LunchStart { get; init; }
    public DateTime? LunchEnd { get; init; }
    public DateTime? ClockOut { get; init; }
    public double? EffectiveHours { get; init; }
    public string? Compliance { get; init; }
}
