namespace MindHelp.Shared;

public class HelpingPersonal
{
    public int HelpingPersonalId { get; set; }

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsVerified { get; set; }

    public DateTime Birthday { get; set; }

    public DateTime? LastOnlinePersonal { get; set; }
}