namespace AuthBackend.Domain.Entities;

public enum ActionType
{
    ClockIn = 1,
    LunchStart = 2,
    LunchEnd = 3,
    ClockOut = 4
}

public class AttendanceRecord
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public ActionType ActionType { get; private set; }
    public DateTime Timestamp { get; private set; }

    // Navigation
    public Usuario? Usuario { get; private set; }

    private AttendanceRecord() { } // EF Core

    public AttendanceRecord(Guid userId, ActionType actionType)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        ActionType = actionType;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Returns the next expected ActionType given the current one, or null if cycle is complete.
    /// </summary>
    public static ActionType? NextAction(ActionType? current) => current switch
    {
        null          => ActionType.ClockIn,
        ActionType.ClockIn    => ActionType.LunchStart,
        ActionType.LunchStart => ActionType.LunchEnd,
        ActionType.LunchEnd   => ActionType.ClockOut,
        ActionType.ClockOut   => null, // cycle complete
        _                     => null
    };
}
