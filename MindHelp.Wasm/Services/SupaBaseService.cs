using Microsoft.JSInterop;

namespace MindHelp.Wasm.Services;

public class SupaBaseService
{
    private string SupabaseUrl = "https://btluoessffljxbznjabe.supabase.co";
    private string SupabaseKey = "sb_publishable_0EFTKf2BiAp8_YNgVuBnyg_iWe2WgcN";

    private readonly IJSRuntime _js;

    public SupaBaseService(IJSRuntime js)
    {
        _js = js;
    }

    private Supabase.Client? _client;

    // Fehlermeldung des letzten fehlgeschlagenen Logins bzw. der letzten Registrierung
    public string? LetzterFehler { get; private set; }

    // Wird ausgelöst, wenn sich jemand an- oder abmeldet (z. B. damit das Menü sich anpasst)
    public event Action? AnmeldungGeaendert;


    /// <summary>
    /// Erstellt den Client beim ersten Aufruf und verbindet mit Supabase.
    /// Weitere Aufrufe liefern denselben Client zurück.
    /// </summary>
    public async Task<Supabase.Client> InitializeAsync()
    {
        if (_client == null)
        {
            var options = new Supabase.SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = true //Live-Verbindung für den Chat
            };

            var client = new Supabase.Client(SupabaseUrl, SupabaseKey, options);
            client.Auth.SetPersistence(new BrowserSessionPersistence(_js));
            await client.InitializeAsync();
            
            // Eine gespeicherte Anmeldung (vom letzten Besuch) kann abgelaufen sein – nach ca. 1 Stunde.
            // Deshalb beim Start erneuern, sonst würde Supabase die Daten (z. B. das Profil) nicht herausgeben.
            if (client.Auth.CurrentSession != null)
            {
                try
                {
                    await client.Auth.RefreshSession();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Anmeldung konnte nicht erneuert werden: " + ex.Message);
                    try { await client.Auth.SignOut(); } catch { /* dann einfach neu anmelden */ }
                }
            }

            _client = client;
        }
        return _client;
    }

    // Methode zum Registrieren eines neuen Benutzers
    public Task<bool> RegisterUserAsync(string email, string password)
    {
        return RegisterUserAsyncCore(email, password, null);
    }

    public Task<bool> RegisterUserAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        DateTime birthday,
        string specification,
        string role = "user")   // "user" = hilfesuchend, "helper" = Hilfskraft
    {
        // Zusätzliche Daten, aus denen Supabase automatisch das Profil anlegt (mathias)
        var data = new Dictionary<string, object>
        {
            { "first_name", firstName },
            { "last_name", lastName },
            { "birthday", birthday.ToString("yyyy-MM-dd") },
            { "specification", specification },
            { "role", role }   // damit die Datenbank Hilfskräfte erkennt
        };
        return RegisterUserAsyncCore(email, password, data);
    }

    private async Task<bool> RegisterUserAsyncCore(string email, string password, Dictionary<string, object>? data)
    {
        LetzterFehler = null; // Zurücksetzen der Fehlermeldung
       

        try
        {
             var client = await InitializeAsync(); // Stellt sicher, dass der Client initialisiert ist (Mathias)
            var options = new Supabase.Gotrue.SignUpOptions
            {
                Data = data // Hier werden die zusätzlichen Daten übergeben (Mathias)
            };
            var response = await client.Auth.SignUp(email, password, options); // Legt einen neuen User an

            if (response?.User != null)
            {
                Console.WriteLine($"User registered: {response.User.Email}");
                return true;
            }
            else
            {
                Console.WriteLine("Signup lieferte keinen User zurück.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error registering user: {ex.Message}");
        }

        return false;
    }

    // Meldet einen User an. Supabase liefert dabei das JWT (AccessToken).
    public async Task<bool> LoginAsync(string email, string password)
    {
        
        LetzterFehler = null; // Zurücksetzen der Fehlermeldung
        try
        {
            var client = await InitializeAsync();
            var session = await client.Auth.SignIn(email, password);
            var ok = session?.AccessToken != null;
            if (ok) AnmeldungGeaendert?.Invoke();
            return ok;
        }
        catch (Exception ex)
        {
            LetzterFehler = Uebersetze(ex.Message); // Übersetze die Fehlermeldung ins Deutsche
            Console.WriteLine($"Login fehlgeschlagen: {ex.Message}");
            return false;
        }
    }

    // Häufige Supabase-Fehlermeldungen verständlich auf Deutsch
    private static string Uebersetze(string meldung)
    {
        if (meldung.Contains("Email not confirmed", StringComparison.OrdinalIgnoreCase))
            return "Die E-Mail-Adresse ist noch nicht bestätigt. Bitte klicke auf den Link in der Bestätigungs-Mail (auch im Spam-Ordner nachsehen).";
        if (meldung.Contains("Invalid login credentials", StringComparison.OrdinalIgnoreCase))
            return "E-Mail oder Passwort falsch.";
        if (meldung.Contains("already registered", StringComparison.OrdinalIgnoreCase))
            return "Diese E-Mail-Adresse ist bereits registriert.";
        if (meldung.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
            return "Zu viele Versuche. Bitte warte ein paar Minuten und versuche es erneut.";
        if (meldung.Contains("Database error", StringComparison.OrdinalIgnoreCase))
            return "Fehler in der Datenbank beim Anlegen des Profils.";
        return "Fehler: " + meldung;
    }


    /// <summary>
    /// Meldet den aktuellen Benutzer ab.
    /// </summary>
    public async Task LogoutAsync()
    {
        var client = await InitializeAsync();
        await client.Auth.SignOut();
        AnmeldungGeaendert?.Invoke(); // Invoke heißt "aufrufen" – also das Event auslösen, dass sich die Anmeldung geändert hat
    }

    /// <summary>
    /// Prüft, ob ein Benutzer eingeloggt ist.
    /// </summary>
    public bool IsLoggedIn => _client?.Auth.CurrentSession != null;

        /// <summary>
    /// Baut die Live-Verbindung (Realtime) zu Supabase auf – nur einmal, egal wie oft es aufgerufen wird.
    /// Chat und Notruf brauchen sie beide.
    /// </summary>
    public Task VerbindeLiveAsync()
    {
        return _liveVerbindung ??= BaueLiveVerbindungAsync();
    }

    private Task? _liveVerbindung;

    private async Task BaueLiveVerbindungAsync()
    {
        var client = await InitializeAsync();
        try
        {
            await client.Realtime.ConnectAsync();
            // Damit Supabase weiß, wer zuhört (sonst greifen die Sicherheitsregeln nicht)
            if (client.Auth.CurrentSession?.AccessToken is { } token)
                client.Realtime.SetAuth(token);
        }
        catch
        {
            _liveVerbindung = null;   // beim nächsten Mal neu versuchen
            throw;
        }
    }
}