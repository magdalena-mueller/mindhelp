namespace MindHelp.Shared;

public class User
{
    public int UserId { get; set; }

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Specification { get; set; } = string.Empty;

    public byte[]? ProfilePicture { get; set; }     //"?" weils optional ist

    public DateTime? LastOnlineUsers { get; set; }

    public bool IsVerified { get; set; }
}