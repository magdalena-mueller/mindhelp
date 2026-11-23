using MindHelp.Wasm.Models;
using static Supabase.Postgrest.Constants;

namespace MindHelp.Wasm.Services;

// Begleiter "Mindi": wächst, wenn man sich um sich selbst kümmert.
// Er verliert nie Punkte und kann nicht sterben – wer länger weg war, findet ihn nur schlafend.
public class BegleiterService
{
    private readonly SupaBaseService _supabase;

    public BegleiterService(SupaBaseService supabase)
    {
        _supabase = supabase;
    }

    // Ab wie vielen Punkten welche Stufe erreicht ist
    public static readonly (int Ab, string Name)[] Stufen =
    [
        (0, "Ei"),
        (20, "Baby"),
        (80, "Klein"),
        (200, "Groß"),
        (400, "Strahlend")
    ];

    public static int Stufe(int punkte) => Stufen.Count(s => punkte >= s.Ab) - 1;

    // Die Arten, die man großziehen kann: Schlüssel, Anzeigename, Standardname, Beschreibung, Icon für die Sammlung
    public static readonly (string Art, string Titel, string Standardname, string Beschreibung, string Icon)[] Arten =
    [
        ("mindi", "Mindi", "Mindi", "der Kuschelige", MudBlazor.Icons.Material.Filled.Spa),
        ("fee", "Fee", "Feli", "die Zauberhafte", MudBlazor.Icons.Material.Filled.AutoAwesome),
        ("drache", "Drache", "Draki", "der Mutige", MudBlazor.Icons.Material.Filled.LocalFireDepartment)
    ];

    public static (string Art, string Titel, string Standardname, string Beschreibung, string Icon) ArtInfo(string art) =>
        Arten.FirstOrDefault(a => a.Art == art, Arten[0]);

    // Wird ausgelöst, wenn sich etwas ändert (z. B. Punkte von einer anderen Seite)
    public event Action<Begleiter>? Geaendert;

    private async Task<Guid> GetMeineIdAsync()
    {
        var client = await _supabase.InitializeAsync();
        return Guid.Parse(client.Auth.CurrentUser!.Id!);
    }

    // Eigenen Begleiter holen – null heißt: man hat sich noch nicht entschieden (dann kommt die Begrüßung)
    public async Task<Begleiter?> GetAsync()
    {
        var client = await _supabase.InitializeAsync();
        var antwort = await client.From<Begleiter>()
            .Filter("user_id", Operator.Equals, (await GetMeineIdAsync()).ToString())
            .Get();
        return antwort.Models.FirstOrDefault();
    }

    // Erste Entscheidung: Ei der gewählten Art – oder "Nein danke" (aktiv = false)
    public async Task<Begleiter> ErstelleAsync(string art, bool aktiv)
    {
        var client = await _supabase.InitializeAsync();
        var neu = new Begleiter
        {
            UserId = await GetMeineIdAsync(),
            Art = art,
            Name = ArtInfo(art).Standardname,
            Aktiv = aktiv
        };
        var antwort = await client.From<Begleiter>().Insert(neu);
        var begleiter = antwort.Models.FirstOrDefault() ?? neu;
        Geaendert?.Invoke(begleiter);
        return begleiter;
    }

    // Im Profil ein- oder ausschalten (Punkte bleiben erhalten)
    public async Task SetzeAktivAsync(Begleiter begleiter, bool aktiv)
    {
        begleiter.Aktiv = aktiv;
        var client = await _supabase.InitializeAsync();
        await client.From<Begleiter>()
            .Filter("user_id", Operator.Equals, begleiter.UserId.ToString())
            .Set(b => b.Aktiv, aktiv)
            .Update();
        Geaendert?.Invoke(begleiter);
    }

    // Solange es noch ein Ei ist, darf man die Art tauschen
    public async Task TauscheEiAsync(Begleiter begleiter, string art)
    {
        if (Stufe(begleiter.Punkte) > 0) return;   // schon geschlüpft → erst nach dem Großziehen

        var alterStandardname = ArtInfo(begleiter.Art).Standardname;
        begleiter.Art = art;
        if (begleiter.Name == alterStandardname)
            begleiter.Name = ArtInfo(art).Standardname;   // eigenen Namen behalten, Standardnamen anpassen

        var client = await _supabase.InitializeAsync();
        await client.From<Begleiter>()
            .Filter("user_id", Operator.Equals, begleiter.UserId.ToString())
            .Set(b => b.Art, begleiter.Art)
            .Set(b => b.Name, begleiter.Name)
            .Update();
        Geaendert?.Invoke(begleiter);
    }

    // Punkte dazugeben, z. B. fürs Abhaken eines Ziels. Fehler werden still ignoriert –
    // die eigentliche Aktion (Ziel, Stimmung …) soll nie daran scheitern.
    public async Task PunkteDazuAsync(int punkte)
    {
        try
        {
            var begleiter = await GetAsync();
            if (begleiter == null || !begleiter.Aktiv) return;   // kein Begleiter gewünscht → keine Punkte
            begleiter.Punkte += punkte;
            begleiter.LetztePflege = DateTime.UtcNow;

            var client = await _supabase.InitializeAsync();
            await client.From<Begleiter>()
                .Filter("user_id", Operator.Equals, begleiter.UserId.ToString())
                .Set(b => b.Punkte, begleiter.Punkte)
                .Set(b => b.LetztePflege, begleiter.LetztePflege)
                .Update();

            Geaendert?.Invoke(begleiter);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Begleiter-Punkte konnten nicht gespeichert werden: " + ex.Message);
        }
    }

    // Streicheln weckt ihn auf (keine Punkte, nur "zuletzt gekümmert")
    public async Task StreichelnAsync(Begleiter begleiter)
    {
        begleiter.LetztePflege = DateTime.UtcNow;
        var client = await _supabase.InitializeAsync();
        await client.From<Begleiter>()
            .Filter("user_id", Operator.Equals, begleiter.UserId.ToString())
            .Set(b => b.LetztePflege, begleiter.LetztePflege)
            .Update();
    }

    // Ganz gewachsener Begleiter kommt in die Sammlung, danach gibt es ein neues Ei der gewählten Art
    public async Task InSammlungUndNeuesEiAsync(Begleiter begleiter, string neueArt)
    {
        var client = await _supabase.InitializeAsync();

        await client.From<SammlungEintrag>().Insert(new SammlungEintrag
        {
            UserId = begleiter.UserId,
            Art = begleiter.Art,
            Name = begleiter.Name
        });

        begleiter.Art = neueArt;
        begleiter.Name = ArtInfo(neueArt).Standardname;
        begleiter.Punkte = 0;
        begleiter.LetztePflege = DateTime.UtcNow;

        await client.From<Begleiter>()
            .Filter("user_id", Operator.Equals, begleiter.UserId.ToString())
            .Set(b => b.Art, begleiter.Art)
            .Set(b => b.Name, begleiter.Name)
            .Set(b => b.Punkte, begleiter.Punkte)
            .Set(b => b.LetztePflege, begleiter.LetztePflege)
            .Update();

        Geaendert?.Invoke(begleiter);
    }

    public async Task<List<SammlungEintrag>> GetSammlungAsync()
    {
        var client = await _supabase.InitializeAsync();
        var antwort = await client.From<SammlungEintrag>()
            .Filter("user_id", Operator.Equals, (await GetMeineIdAsync()).ToString())
            .Order("abgeschlossen_at", Ordering.Ascending)
            .Get();
        return antwort.Models;
    }

    public async Task UmbenennenAsync(Begleiter begleiter, string name)
    {
        begleiter.Name = name.Trim();
        var client = await _supabase.InitializeAsync();
        await client.From<Begleiter>()
            .Filter("user_id", Operator.Equals, begleiter.UserId.ToString())
            .Set(b => b.Name, begleiter.Name)
            .Update();
    }
}

