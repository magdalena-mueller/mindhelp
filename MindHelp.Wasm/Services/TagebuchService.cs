using MindHelp.Wasm.Models;
using static Supabase.Postgrest.Constants;

namespace MindHelp.Wasm.Services;

// Für Hilfesuchende: Stimmungen ("Heute"), Ziele ("Mein Weg") und Kurz-Checks.
// Jede Person sieht nur ihre eigenen Einträge (dafür sorgen die Regeln in Supabase).
public class TagebuchService
{
    private readonly SupaBaseService _supabase;

    public TagebuchService(SupaBaseService supabase)
    {
        _supabase = supabase;
    }

    private async Task<Guid> GetMeineIdAsync()
    {
        var client = await _supabase.InitializeAsync();
        return Guid.Parse(client.Auth.CurrentUser!.Id!);
    }

    // ---------- Stimmungen ----------

    public async Task SpeichereStimmungAsync(int wert, string? notiz, bool geteilt)
    {
        var client = await _supabase.InitializeAsync();
        await client.From<Stimmung>().Insert(new Stimmung
        {
            UserId = await GetMeineIdAsync(),
            Wert = wert,
            Notiz = string.IsNullOrWhiteSpace(notiz) ? null : notiz.Trim(),
            Geteilt = geteilt
        });
    }

    public async Task<List<Stimmung>> GetStimmungenAsync(int tage)
    {
        var client = await _supabase.InitializeAsync();
        var ab = DateTime.UtcNow.Date.AddDays(-(tage - 1));
        var antwort = await client.From<Stimmung>()
            .Filter("user_id", Operator.Equals, (await GetMeineIdAsync()).ToString())
            .Filter("created_at", Operator.GreaterThanOrEqual, ab.ToString("o"))
            .Order("created_at", Ordering.Ascending)
            .Get();
        return antwort.Models;
    }

    // Für Hilfskräfte: der letzte geteilte Eintrag einer Person (im Chat angezeigt)
    public async Task<Stimmung?> GetLetzteGeteilteStimmungAsync(Guid personId)
    {
        var client = await _supabase.InitializeAsync();
        var antwort = await client.From<Stimmung>()
            .Filter("user_id", Operator.Equals, personId.ToString())
            .Filter("geteilt", Operator.Equals, "true")
            .Order("created_at", Ordering.Descending)
            .Limit(1)
            .Get();
        return antwort.Models.FirstOrDefault();
    }

    // ---------- Ziele ----------

    public async Task<List<Ziel>> GetZieleAsync()
    {
        var client = await _supabase.InitializeAsync();
        var antwort = await client.From<Ziel>()
            .Filter("user_id", Operator.Equals, (await GetMeineIdAsync()).ToString())
            .Order("created_at", Ordering.Ascending)
            .Get();
        return antwort.Models;
    }

    public async Task<Ziel?> NeuesZielAsync(string titel)
    {
        var client = await _supabase.InitializeAsync();
        var antwort = await client.From<Ziel>().Insert(new Ziel
        {
            UserId = await GetMeineIdAsync(),
            Titel = titel.Trim()
        });
        return antwort.Models.FirstOrDefault();
    }

    public async Task SetzeErledigtAsync(Ziel ziel, bool erledigt)
    {
        var client = await _supabase.InitializeAsync();
        await client.From<Ziel>()
            .Filter("id", Operator.Equals, ziel.Id.ToString())
            .Set(z => z.Erledigt, erledigt)
            .Update();
        ziel.Erledigt = erledigt;
    }

    public async Task LoescheZielAsync(Ziel ziel)
    {
        var client = await _supabase.InitializeAsync();
        await client.From<Ziel>()
            .Filter("id", Operator.Equals, ziel.Id.ToString())
            .Delete();
    }

    // ---------- Kurz-Check ----------

    public async Task SpeichereCheckAsync(int prozent)
    {
        var client = await _supabase.InitializeAsync();
        await client.From<CheckErgebnis>().Insert(new CheckErgebnis
        {
            UserId = await GetMeineIdAsync(),
            Prozent = prozent
        });
    }

    public async Task<List<CheckErgebnis>> GetChecksAsync()
    {
        var client = await _supabase.InitializeAsync();
        var antwort = await client.From<CheckErgebnis>()
            .Filter("user_id", Operator.Equals, (await GetMeineIdAsync()).ToString())
            .Order("created_at", Ordering.Descending)
            .Limit(5)
            .Get();
        return antwort.Models;
    }
}