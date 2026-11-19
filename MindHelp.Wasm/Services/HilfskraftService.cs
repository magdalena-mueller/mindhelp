using MindHelp.Wasm.Models;
using static Supabase.Postgrest.Constants;

namespace MindHelp.Wasm.Services;

// Für Hilfskräfte: erreichbar ja/nein, Zeiten und Zahlen für die Übersicht.
public class HilfskraftService
{
    private readonly SupaBaseService _supabase;

    public HilfskraftService(SupaBaseService supabase)
    {
        _supabase = supabase;
    }

    // Die Zeitblöcke, die man antippen kann (Index = Spalte "block" in der Datenbank)
    public static readonly string[] Bloecke = ["8–12", "12–16", "16–20", "20–24"];
    public static readonly string[] Wochentage = ["Mo", "Di", "Mi", "Do", "Fr", "Sa", "So"];

    private async Task<Guid> GetMeineIdAsync()
    {
        var client = await _supabase.InitializeAsync();
        return Guid.Parse(client.Auth.CurrentUser!.Id!);
    }

    // ---------- Erreichbar ----------

    public async Task SetzeErreichbarAsync(bool erreichbar)
    {
        var client = await _supabase.InitializeAsync();
        await client.From<HelperDetails>()
            .Filter("id", Operator.Equals, (await GetMeineIdAsync()).ToString())
            .Set(h => h.Erreichbar, erreichbar)
            .Update();
    }

    // ---------- Zeiten ----------

    public async Task<List<Verfuegbarkeit>> GetZeitenAsync(Guid? hilfskraftId = null)
    {
        var client = await _supabase.InitializeAsync();
        var id = hilfskraftId ?? await GetMeineIdAsync();
        var antwort = await client.From<Verfuegbarkeit>()
            .Filter("helper_id", Operator.Equals, id.ToString())
            .Get();
        return antwort.Models;
    }

    public async Task SetzeZeitAsync(int wochentag, int block, bool aktiv)
    {
        var client = await _supabase.InitializeAsync();
        var meineId = await GetMeineIdAsync();

        if (aktiv)
        {
            await client.From<Verfuegbarkeit>().Insert(new Verfuegbarkeit
            {
                HelperId = meineId,
                Wochentag = wochentag,
                Block = block
            });
        }
        else
        {
            await client.From<Verfuegbarkeit>()
                .Filter("helper_id", Operator.Equals, meineId.ToString())
                .Filter("wochentag", Operator.Equals, wochentag.ToString())
                .Filter("block", Operator.Equals, block.ToString())
                .Delete();
        }
    }

    // Text wie "heute 16–20 Uhr" für die Vorschau
    public static string ZeitenHeute(List<Verfuegbarkeit> zeiten)
    {
        var heute = (int)DateTime.Today.DayOfWeek;
        if (heute == 0) heute = 7;   // Sonntag ist in .NET 0, bei uns 7

        var bloecke = zeiten.Where(z => z.Wochentag == heute).OrderBy(z => z.Block).Select(z => Bloecke[z.Block]).ToList();
        return bloecke.Count == 0 ? "heute nicht eingetragen" : "heute " + string.Join(", ", bloecke) + " Uhr";
    }

    // ---------- Zahlen für die Übersicht ----------

    // Wie viele Nachrichten ich in den letzten 6 Wochen pro Woche geschrieben habe
    public async Task<(string[] Wochen, double[] Anzahl)> GetNachrichtenProWocheAsync()
    {
        var client = await _supabase.InitializeAsync();
        var meineId = await GetMeineIdAsync();
        var start = DateTime.Today.AddDays(-(((int)DateTime.Today.DayOfWeek + 6) % 7)).AddDays(-35);   // Montag vor 5 Wochen

        var nachrichten = (await client.From<ChatMessage>()
            .Filter("sender_id", Operator.Equals, meineId.ToString())
            .Filter("created_at", Operator.GreaterThanOrEqual, start.ToUniversalTime().ToString("o"))
            .Get()).Models;

        var wochen = new string[6];
        var anzahl = new double[6];
        for (var i = 0; i < 6; i++)
        {
            var von = start.AddDays(i * 7);
            var bis = von.AddDays(7);
            wochen[i] = "KW " + System.Globalization.ISOWeek.GetWeekOfYear(von);
            anzahl[i] = nachrichten.Count(n => n.CreatedAt.ToLocalTime() >= von && n.CreatedAt.ToLocalTime() < bis);
        }
        return (wochen, anzahl);
    }
}