namespace MindHelp.Shared;

[Table("chat")]
public class Chat
{
    [PrimaryKey("chatID", false)]
    public int ChatId { get; set; }

    [Column("created_at")]
    public DateTime Created { get; set; } = DateTime.UtcNow;

    [Column("FK_HilfeID")]
    public int HelpingPersonalId { get; set; }

    [Column("FK_UserID")]
    public int UserId { get; set; }

    [Column("textMessage")]
    public string TextMessage { get; set; } = string.Empty;

    [Column("timeSent")]
    public DateTime TimeSent { get; set; }

    [Column("messageSeen")]
    public DateTime? MessageSeen { get; set; }
}