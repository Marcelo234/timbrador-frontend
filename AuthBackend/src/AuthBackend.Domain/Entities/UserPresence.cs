namespace AuthBackend.Domain.Entities;

public class UserPresence
{
    public Guid UserId { get; private set; }
    public DateTime LastSeenUtc { get; private set; }

    // Navigation
    public Usuario? Usuario { get; private set; }

    private UserPresence() { } // EF Core

    public UserPresence(Guid userId)
    {
        UserId = userId;
        LastSeenUtc = DateTime.UtcNow;
    }

    public void Touch() => LastSeenUtc = DateTime.UtcNow;
}
