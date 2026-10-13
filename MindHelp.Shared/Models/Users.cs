using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MindHelp.Shared.Models;

[Table("users")]
public class User
{
    [Column("userID")]
    public int UserId { get; set; }

    [Column("created_at")]
    public DateTime Created { get; set; } = DateTime.UtcNow;

    [Column("username")]
    [Required(ErrorMessage = "Bitte geben Sie einen Benutzernamen ein.")]
    public string Username { get; set; } = string.Empty;

    [Column("password")]
    [Required(ErrorMessage = "Bitte geben Sie ein Passwort ein.")]
    public string Password { get; set; } = string.Empty;

    [Column("email")]
    [Required(ErrorMessage = "Bitte geben Sie eine E-Mail-Adresse ein.")]
    public string Email { get; set; } = string.Empty;

    [Column("picture")]
    public byte[]? ProfilePicture { get; set; }     //"?" weils optional ist

    [Column("verified")]
    public bool IsVerified { get; set; }

    [Column("lastOnline")]
    public DateTime? LastOnlineUsers { get; set; }

    public ICollection<Chat> Chats { get; set; } = [];
}