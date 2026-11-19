using MindHelp.Wasm.Models;
using Supabase.Realtime;
using Supabase.Realtime.PostgresChanges;
using static Supabase.Postgrest.Constants;

namespace MindHelp.Wasm.Services;

// Notruf: Hilfesuchende senden ihn, alle Hilfskräfte bekommen ihn live, EINE nimmt an.
public class NotrufService
{
    private readonly SupaBaseService _supabase;
    private RealtimeChannel? _kanal;

    public NotrufService(SupaBaseService supabase)
    {
        _supabase = supabase;
    }

    // Wird aufgerufen, wenn ein Notruf neu ist oder sich ändert (angenommen, erledigt …)
    public event Action<Notruf>? NotrufGeaendert;

    // Der eigene Notruf, der gerade läuft (für Hilfesuchende)
    public Notruf? MeinNotruf { get; private set; }

    private async Task<Guid?> GetMeineIdAsync()
    {
        var client = await _supabase.InitializeAsync();
        return Guid.TryParse(client.Auth.CurrentUser?.Id, out var id) ? id : null;
    }

    // ---------- Hilfesuchende ----------

    public async Task<Notruf?> SendeNotrufAsync(string? text)
    {
        var meineId = await GetMeineIdAsync();
        if (meineId == null) return null;

        var client = await _supabase.InitializeAsync();
        var antwort = await client.From<Notruf>().Insert(new Notruf
        {
            UserId = meineId.Value,
            Nachricht = string.IsNullOrWhiteSpace(text) ? null : text.Trim()
        });
        MeinNotruf = antwort.Models.FirstOrDefault();
        return MeinNotruf;
    }

    public async Task ZieheNotrufZurueckAsync()
    {
        if (MeinNotruf == null) return;

        var client = await _supabase.InitializeAsync();
        await client.From<Notruf>()
            .Filter("id", Operator.Equals, MeinNotruf.Id.ToString())
            .Set(n => n.Status, "zurueckgezogen")
            .Update();
        MeinNotruf = null;
    }

    // Beim Start: läuft noch ein offener Notruf von mir? (z. B. nach dem Neuladen der Seite)
    public async Task LadeMeinenOffenenNotrufAsync()
    {
        var meineId = await GetMeineIdAsync();
        if (meineId == null) return;

        var client = await _supabase.InitializeAsync();
        var antwort = await client.From<Notruf>()
            .Filter("user_id", Operator.Equals, meineId.Value.ToString())
            .Filter("status", Operator.Equals, "offen")
            .Get();
        MeinNotruf = antwort.Models.FirstOrDefault();
    }

    // Nach "Zum Chat": der Notruf ist erledigt, das Fenster soll beim nächsten Mal wieder von vorne beginnen
    public void VergissMeinenNotruf()
    {
        MeinNotruf = null;
    }

    // ---------- Hilfskräfte ----------

    public async Task<List<Notruf>> GetOffeneNotrufeAsync()
    {
        var client = await _supabase.InitializeAsync();
        var antwort = await client.From<Notruf>()
            .Filter("status", Operator.Equals, "offen")
            .Order("created_at", Ordering.Ascending)
            .Get();
        return antwort.Models;
    }

    // Gibt die ID des Chats zurück – oder null, wenn jemand anderes schneller war
    public async Task<long?> NimmNotrufAnAsync(long notrufId)
    {
        var client = await _supabase.InitializeAsync();
        var antwort = await client.Rpc("notruf_annehmen", new Dictionary<string, object> { { "p_notruf_id", notrufId } });
        var inhalt = antwort.Content?.Trim();
        return long.TryParse(inhalt, out var unterhaltungId) ? unterhaltungId : null;
    }

    public async Task<Notruf?> GetAngenommenenNotrufAsync(long unterhaltungId)
    {
        var client = await _supabase.InitializeAsync();
        var antwort = await client.From<Notruf>()
            .Filter("conversation_id", Operator.Equals, unterhaltungId.ToString())
            .Filter("status", Operator.Equals, "angenommen")
            .Get();
        return antwort.Models.FirstOrDefault();
    }

    public async Task SchliesseFallAbAsync(long notrufId)
    {
        var client = await _supabase.InitializeAsync();
        await client.From<Notruf>()
            .Filter("id", Operator.Equals, notrufId.ToString())
            .Set(n => n.Status, "erledigt")
            .Set(n => n.ErledigtAt!, DateTime.UtcNow)
            .Update();
    }

    // Alle Notrufe, die ich übernommen habe (für das Board "Gespräche")
    public async Task<List<Notruf>> GetMeineFaelleAsync()
    {
        var meineId = await GetMeineIdAsync();
        if (meineId == null) return [];

        var client = await _supabase.InitializeAsync();
        var antwort = await client.From<Notruf>()
            .Filter("helper_id", Operator.Equals, meineId.Value.ToString())
            .Order("created_at", Ordering.Descending)
            .Get();
        return antwort.Models;
    }

    // ---------- Live-Empfang ----------

    public async Task StarteLiveEmpfangAsync()
    {
        if (_kanal != null) return;

        var client = await _supabase.InitializeAsync();
        await _supabase.VerbindeLiveAsync();
        _kanal = await client.From<Notruf>().On(
            PostgresChangesOptions.ListenType.All,
            (sender, aenderung) =>
            {
                var notruf = aenderung.Model<Notruf>();
                if (notruf == null) return;

                if (MeinNotruf != null && notruf.Id == MeinNotruf.Id)
                    MeinNotruf = notruf;

                NotrufGeaendert?.Invoke(notruf);
            });
    }

    public void StoppeLiveEmpfang()
    {
        _kanal?.Unsubscribe();
        _kanal = null;
        MeinNotruf = null;
    }
}