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

    
    // Wird nur gelesen (für "Du bist seit … Tagen dabei"), nie mitgeschickt
    [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTime CreatedAt { get; set; }

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

    [Column("erreichbar")]
    public bool Erreichbar { get; set; } = true;
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


// Diese Tabellen sind für die Seiten in der Leiste und Funktionen während der Benutzung gedacht.

[Table("notrufe")]
public class Notruf : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("nachricht")]
    public string? Nachricht { get; set; }

    [Column("status")]
    public string Status { get; set; } = "offen";   // offen → angenommen → erledigt (oder zurueckgezogen)

    [Column("helper_id")]
    public Guid? HelperId { get; set; }

    [Column("conversation_id")]
    public long? ConversationId { get; set; }

    [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTime CreatedAt { get; set; }

    [Column("angenommen_at")]
    public DateTime? AngenommenAt { get; set; }

    [Column("erledigt_at")]
    public DateTime? ErledigtAt { get; set; }
}

[Table("stimmungen")]
public class Stimmung : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("wert")]
    public int Wert { get; set; }   // 1 = schlecht … 5 = super

    [Column("notiz")]
    public string? Notiz { get; set; }

    [Column("geteilt")]
    public bool Geteilt { get; set; }

    [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTime CreatedAt { get; set; }
}

[Table("ziele")]
public class Ziel : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("titel")]
    public string Titel { get; set; } = string.Empty;

    [Column("erledigt")]
    public bool Erledigt { get; set; }

    [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTime CreatedAt { get; set; }
}

[Table("kurz_checks")]
public class CheckErgebnis : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("prozent")]
    public int Prozent { get; set; }

    [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTime CreatedAt { get; set; }
}

[Table("verfuegbarkeit")]
public class Verfuegbarkeit : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("helper_id")]
    public Guid HelperId { get; set; }

    [Column("wochentag")]
    public int Wochentag { get; set; }   // 1 = Montag … 7 = Sonntag

    [Column("block")]
    public int Block { get; set; }       // 0 = 8–12, 1 = 12–16, 2 = 16–20, 3 = 20–24
}

// Eine Zeile in der Chatliste (wird im Programm zusammengebaut, keine eigene Tabelle)
public class ChatEintrag
{
    public Conversation? Unterhaltung { get; set; }   // null = Hilfskraft, mit der noch kein Chat besteht
    public Profile Gegenueber { get; set; } = new();
    public HelperDetails? Details { get; set; }
    public ChatMessage? LetzteNachricht { get; set; }
    public int Ungelesen { get; set; }
    public bool Notfall { get; set; }
}