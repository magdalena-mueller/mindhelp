using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MindHelp.Wasm.Models;

// Diese Klassen entsprechen den Tabellen in Supabase (Schema public).
// BaseModel + die Attribute sagen der Supabase-Bibliothek, welche Tabelle/Spalte gemeint ist.

[Table("profiles")]
public class Profile : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("role")]
    public string Role { get; set; } = "user";   // "user" = hilfesuchend, "helper" = Hilfskraft

    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [Column("birthday")]
    public DateTime? Birthday { get; set; }

    [Column("avatar_url")]
    public string? AvatarUrl { get; set; }

    [Column("last_online")]
    public DateTime? LastOnline { get; set; }

    public bool IstHilfskraft => Role == "helper";

    public string VollerName => $"{FirstName} {LastName}".Trim();
}

[Table("helper_details")]
public class HelperDetails : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("specification")]
    public string? Specification { get; set; }

    [Column("proof_url")]
    public string? ProofUrl { get; set; }

    [Column("verified")]
    public bool Verified { get; set; }
}

[Table("conversations")]
public class Conversation : BaseModel
{
    // false = die Datenbank vergibt die ID selbst
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("helper_id")]
    public Guid HelperId { get; set; }

    [Column("created_at", ignoreOnInsert: true)]
    public DateTime CreatedAt { get; set; }
}

[Table("messages")]
public class ChatMessage : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("conversation_id")]
    public long ConversationId { get; set; }

    [Column("sender_id")]
    public Guid SenderId { get; set; }

    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("created_at", ignoreOnInsert: true)]
    public DateTime CreatedAt { get; set; }

    [Column("seen_at")]
    public DateTime? SeenAt { get; set; }
}