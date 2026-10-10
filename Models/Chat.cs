namespace MindHelp.Shared;

public class Chat
{
    public int ChatId { get; set; }

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }

    public int HelpingPersonalId { get; set; }

    public string TextMessage { get; set; } = string.Empty;

    public DateTime? LastMessageDate { get; set; }

    public bool IsRead { get; set; }
}