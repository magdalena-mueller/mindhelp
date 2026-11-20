using Microsoft.JSInterop;

namespace MindHelp.Wasm.Services;

// Merkt sich, ob der dunkle Modus an ist – auch nach dem Schließen der App (im Browser gespeichert).
// Umschalten geht nur in den Profileinstellungen.
public class DesignService
{
    private const string Schluessel = "mh-dunkler-modus";
    private readonly IJSRuntime _js;
    private bool _geladen;

    public DesignService(IJSRuntime js)
    {
        _js = js;
    }

    public bool Dunkel { get; private set; }

    // Das Layout hört darauf und färbt die App sofort um
    public event Action? Geaendert;

    // Gespeicherte Wahl beim Start einmal lesen
    public async Task LadeAsync()
    {
        if (_geladen) return;
        _geladen = true;
        try
        {
            Dunkel = await _js.InvokeAsync<string?>("localStorage.getItem", Schluessel) == "ja";
        }
        catch
        {
            Dunkel = false;   // z. B. privates Fenster ohne Speicher → einfach hell
        }
    }

    public async Task SetzeDunkelAsync(bool dunkel)
    {
        Dunkel = dunkel;
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", Schluessel, dunkel ? "ja" : "nein");
        }
        catch { /* Speichern nicht möglich – gilt dann nur bis zum Neuladen */ }
        Geaendert?.Invoke();
    }
}