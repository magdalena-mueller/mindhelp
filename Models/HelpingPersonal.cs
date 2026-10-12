using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MindHelp.Shared;

[Table("hilfe")]
public class HelpingPersonal
{
    [Column("helpID")]
    public int HelpingPersonalId { get; set; }

    [Column("created_at")]
    public DateTime Created { get; set; } = DateTime.UtcNow;

    [Column("firstname")]
    [Required(ErrorMessage = "Bitte geben Sie einen Vornamen ein.")]
    public string FirstName { get; set; } = string.Empty;

    [Column("lastname")]
    [Required(ErrorMessage = "Bitte geben Sie einen Nachnamen ein.")]
    public string LastName { get; set; } = string.Empty;

    [Column("email")]
    [Required(ErrorMessage = "Bitte geben Sie eine E-Mail-Adresse ein.")]
    public string Email { get; set; } = string.Empty;

    [Column("password")]
    [Required(ErrorMessage = "Bitte geben Sie ein Passwort ein.")]
    public string Password { get; set; } = string.Empty;

    [Column("profilePicture")]
    [Required(ErrorMessage = "Bitte fügen Sie ein Profilbild hinzu.")]
    public string ProfilePicture { get; set; } = string.Empty;

    [Column("proofOfProfession")]
    [Required(ErrorMessage = "Bitte fügen Sie einen Nachweis der Berufstätigkeit an.")]
    public string ProofOfProfession { get; set; } = string.Empty;

    [Column("verified")]
    public bool IsVerified { get; set; }

    [Column("birthday")]
    [Required(ErrorMessage = "Bitte geben Sie Ihr Geburtsdatum an.")]
    public DateTime Birthday { get; set; }

    [Column("specification")]
    public string? Specification { get; set; } = string.Empty;

    [Column("lastOnline")]
    public DateTime? LastOnlinePersonal { get; set; }

    public ICollection<Chat> Chats { get; set; } = [];
}