using MindHelp.Wasm.Models;
using Supabase.Realtime;
using Supabase.Realtime.PostgresChanges;
using static Supabase.Postgrest.Constants;

namespace MindHelp.Wasm.Services;

// Alles rund um Profile, Hilfskräfte und den Chat.
// Die Sicherheitsregeln (RLS) in Supabase sorgen dafür, dass jede/r nur die eigenen Chats sieht.
public class ChatService
{
    private readonly SupaBaseService _supabase;
    private RealtimeChannel? _nachrichtenKanal;
    private RealtimeChannel? _gelesenKanal;

    public ChatService(SupaBaseService supabase)
    {
        _supabase = supabase;
    }

    // Wird aufgerufen, sobald eine neue Nachricht live ankommt
    public event Action<ChatMessage>? NeueNachricht;

    // Wird aufgerufen, wenn eine Nachricht geändert wurde (z. B. gelesen)
    public event Action<ChatMessage>? NachrichtGeaendert;

    // ID der angemeldeten Person (aus dem Supabase-Login)
    public async Task<Guid?> GetMeineIdAsync()
    {
        var client = await _supabase.InitializeAsync();
        var id = client.Auth.CurrentUser?.Id;
        return Guid.TryParse(id, out var guid) ? guid : null;
    }

    public async Task<Profile?> GetMeinProfilAsync()
    {
        var meineId = await GetMeineIdAsync();
        if (meineId == null) return null;

        var client = await _supabase.InitializeAsync();
        var antwort = await client.From<Profile>()
            .Filter("id", Operator.Equals, meineId.Value.ToString())
            .Get();
        return antwort.Models.FirstOrDefault();
    }

    // Vorname für die Begrüßung: aus dem Profil, sonst aus den Daten der Registrierung
    public async Task<string> GetMeinVornameAsync(Profile? profil)
    {
        if (!string.IsNullOrWhiteSpace(profil?.FirstName))
            return profil.FirstName;

        var client = await _supabase.InitializeAsync();
        var daten = client.Auth.CurrentUser?.UserMetadata;
        if (daten != null && daten.TryGetValue("first_name", out var vorname) && vorname != null)
            return vorname.ToString() ?? "";

        return "";
    }

    // Alle Hilfskräfte mit ihren Zusatzdaten (Fachgebiet, verifiziert)
    public async Task<List<(Profile Profil, HelperDetails? Details)>> GetHilfskraefteAsync()
    {
        var client = await _supabase.InitializeAsync();

        var profile = await client.From<Profile>()
            .Filter("role", Operator.Equals, "helper")
            .Get();
        var details = await client.From<HelperDetails>().Get();

        return profile.Models
            .Select(p => (p, details.Models.FirstOrDefault(d => d.Id == p.Id)))
            .OrderBy(h => h.p.VollerName)
            .ToList();
    }

    // Alle eigenen Unterhaltungen mit dem Profil der jeweils anderen Person
    public async Task<List<(Conversation Unterhaltung, Profile? Gegenueber)>> GetMeineUnterhaltungenAsync()
    {
        var meineId = await GetMeineIdAsync();
        if (meineId == null) return [];

        var client = await _supabase.InitializeAsync();
        var unterhaltungen = (await client.From<Conversation>()
            .Order("created_at", Ordering.Descending)
            .Get()).Models;   // RLS liefert nur die eigenen

        var andereIds = unterhaltungen
            .Select(u => (u.UserId == meineId ? u.HelperId : u.UserId).ToString())
            .Distinct()
            .ToList<object>();

        var profile = andereIds.Count == 0
            ? []
            : (await client.From<Profile>().Filter("id", Operator.In, andereIds).Get()).Models;

        return unterhaltungen
            .Select(u =>
            {
                var andereId = u.UserId == meineId ? u.HelperId : u.UserId;
                return (u, profile.FirstOrDefault(p => p.Id == andereId));
            })
            .ToList();
    }

    // Unterhaltung mit einer Hilfskraft holen oder neu anlegen
    public async Task<Conversation?> GetOderStarteUnterhaltungAsync(Guid hilfskraftId)
    {
        var meineId = await GetMeineIdAsync();
        if (meineId == null) return null;

        var client = await _supabase.InitializeAsync();
        var vorhanden = await client.From<Conversation>()
            .Filter("user_id", Operator.Equals, meineId.Value.ToString())
            .Filter("helper_id", Operator.Equals, hilfskraftId.ToString())
            .Get();

        if (vorhanden.Models.FirstOrDefault() is { } unterhaltung)
            return unterhaltung;

        var neu = await client.From<Conversation>().Insert(new Conversation
        {
            UserId = meineId.Value,
            HelperId = hilfskraftId
        });
        return neu.Models.FirstOrDefault();
    }

    public async Task<List<ChatMessage>> GetNachrichtenAsync(long unterhaltungId)
    {
        var client = await _supabase.InitializeAsync();
        var antwort = await client.From<ChatMessage>()
            .Filter("conversation_id", Operator.Equals, unterhaltungId.ToString())
            .Order("created_at", Ordering.Ascending)
            .Get();
        return antwort.Models;
    }

    public async Task<ChatMessage?> SendeNachrichtAsync(long unterhaltungId, string text)
    {
        var meineId = await GetMeineIdAsync();
        if (meineId == null || string.IsNullOrWhiteSpace(text)) return null;

        var client = await _supabase.InitializeAsync();
        var antwort = await client.From<ChatMessage>().Insert(new ChatMessage
        {
            ConversationId = unterhaltungId,
            SenderId = meineId.Value,
            Content = text.Trim()
        });
        return antwort.Models.FirstOrDefault();
    }

    // Live-Empfang neuer Nachrichten starten (Supabase Realtime)
    public async Task StarteLiveEmpfangAsync()
    {
        if (_nachrichtenKanal != null) return;

        var client = await _supabase.InitializeAsync();
        await _supabase.VerbindeLiveAsync();   // Live-Verbindung (WebSocket) zu Supabase aufbauen

        // Neue Nachrichten
        _nachrichtenKanal = await client.From<ChatMessage>().On(
            PostgresChangesOptions.ListenType.Inserts,
            (sender, aenderung) =>
            {
                var nachricht = aenderung.Model<ChatMessage>();
                if (nachricht != null) NeueNachricht?.Invoke(nachricht);
            });

        // Geänderte Nachrichten (z. B. "gelesen" → zwei Häkchen)
        _gelesenKanal = await client.From<ChatMessage>().On(
            PostgresChangesOptions.ListenType.Updates,
            (sender, aenderung) =>
            {
                var nachricht = aenderung.Model<ChatMessage>();
                if (nachricht != null) NachrichtGeaendert?.Invoke(nachricht);
            });
    }

    public void StoppeLiveEmpfang()
    {
        _nachrichtenKanal?.Unsubscribe();
        _nachrichtenKanal = null;
        _gelesenKanal?.Unsubscribe();
        _gelesenKanal = null;
    }

    // ---------- Für die Chatliste (wie bei WhatsApp) ----------

    // Alle Chats mit letzter Nachricht, Anzahl ungelesener Nachrichten und Notfall-Markierung.
    // Für Hilfesuchende kommen danach alle Hilfskräfte, mit denen es noch keinen Chat gibt.
    public async Task<List<ChatEintrag>> GetChatListeAsync(bool binHilfskraft)
    {
        var meineId = await GetMeineIdAsync();
        if (meineId == null) return [];

        var client = await _supabase.InitializeAsync();
        var unterhaltungen = await GetMeineUnterhaltungenAsync();
        var ids = unterhaltungen.Select(u => (object)u.Unterhaltung.Id.ToString()).ToList();

        var nachrichten = ids.Count == 0
            ? []
            : (await client.From<ChatMessage>()
                .Filter("conversation_id", Operator.In, ids)
                .Order("created_at", Ordering.Ascending)
                .Get()).Models;

        var notfaelle = ids.Count == 0
            ? []
            : (await client.From<Notruf>()
                .Filter("conversation_id", Operator.In, ids)
                .Filter("status", Operator.Equals, "angenommen")
                .Get()).Models;

        var details = (await client.From<HelperDetails>().Get()).Models;

        var liste = unterhaltungen
            .Where(u => u.Gegenueber != null)
            .Select(u =>
            {
                var meine = nachrichten.Where(n => n.ConversationId == u.Unterhaltung.Id).ToList();
                return new ChatEintrag
                {
                    Unterhaltung = u.Unterhaltung,
                    Gegenueber = u.Gegenueber!,
                    Details = details.FirstOrDefault(d => d.Id == u.Gegenueber!.Id),
                    LetzteNachricht = meine.LastOrDefault(),
                    Ungelesen = meine.Count(n => n.SenderId != meineId && n.SeenAt == null),
                    Notfall = notfaelle.Any(n => n.ConversationId == u.Unterhaltung.Id)
                };
            })
            // Notfälle zuerst, dann die neueste Nachricht oben
            .OrderByDescending(e => e.Notfall)
            .ThenByDescending(e => e.LetzteNachricht?.CreatedAt ?? e.Unterhaltung!.CreatedAt)
            .ToList();

        if (!binHilfskraft)
        {
            var schonImChat = liste.Select(e => e.Gegenueber.Id).ToHashSet();
            var hilfskraefte = await GetHilfskraefteAsync();
            liste.AddRange(hilfskraefte
                .Where(h => !schonImChat.Contains(h.Profil.Id))
                .Where(h => h.Details?.Verified == true)   // nur geprüfte Hilfskräfte können angeschrieben werden
                .Select(h => new ChatEintrag { Gegenueber = h.Profil, Details = h.Details }));
        }

        return liste;
    }

    public async Task<Conversation?> GetUnterhaltungAsync(long id)
    {
        var client = await _supabase.InitializeAsync();
        var antwort = await client.From<Conversation>()
            .Filter("id", Operator.Equals, id.ToString())
            .Get();
        return antwort.Models.FirstOrDefault();
    }

    public async Task<Profile?> GetProfilAsync(Guid id)
    {
        var client = await _supabase.InitializeAsync();
        var antwort = await client.From<Profile>()
            .Filter("id", Operator.Equals, id.ToString())
            .Get();
        return antwort.Models.FirstOrDefault();
    }

    public async Task<HelperDetails?> GetHilfskraftDetailsAsync(Guid id)
    {
        var client = await _supabase.InitializeAsync();
        var antwort = await client.From<HelperDetails>()
            .Filter("id", Operator.Equals, id.ToString())
            .Get();
        return antwort.Models.FirstOrDefault();
    }

    // Alle Nachrichten der anderen Person in diesem Chat als gelesen markieren
    public async Task MarkiereAlsGelesenAsync(long unterhaltungId)
    {
        var meineId = await GetMeineIdAsync();
        if (meineId == null) return;

        var client = await _supabase.InitializeAsync();
        await client.From<ChatMessage>()
            .Filter("conversation_id", Operator.Equals, unterhaltungId.ToString())
            .Filter("sender_id", Operator.NotEqual, meineId.Value.ToString())
            .Filter("seen_at", Operator.Is, "null")
            .Set(n => n.SeenAt!, DateTime.UtcNow)
            .Update();
    }
}