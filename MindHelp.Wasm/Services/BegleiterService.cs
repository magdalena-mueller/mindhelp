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

    // Wird ausgelöst, wenn sich etwas ändert (z. B. Punkte von einer anderen Seite)
    public event Action<Begleiter>? Geaendert;

    private async Task<Guid> GetMeineIdAsync()
    {
        var client = await _supabase.InitializeAsync();
        return Guid.Parse(client.Auth.CurrentUser!.Id!);
    }

    // Eigenen Begleiter holen – beim ersten Mal wird er angelegt (als Ei)
    public async Task<Begleiter> GetAsync()
    {
        var client = await _supabase.InitializeAsync();
        var meineId = await GetMeineIdAsync();

        var vorhanden = await client.From<Begleiter>()
            .Filter("user_id", Operator.Equals, meineId.ToString())
            .Get();
        if (vorhanden.Models.FirstOrDefault() is { } begleiter)
            return begleiter;

        var neu = await client.From<Begleiter>().Insert(new Begleiter { UserId = meineId });
        return neu.Models.FirstOrDefault() ?? new Begleiter { UserId = meineId };
    }

    // Punkte dazugeben, z. B. fürs Abhaken eines Ziels. Fehler werden still ignoriert –
    // die eigentliche Aktion (Ziel, Stimmung …) soll nie daran scheitern.
    public async Task PunkteDazuAsync(int punkte)
    {
        try
        {
            var begleiter = await GetAsync();
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
