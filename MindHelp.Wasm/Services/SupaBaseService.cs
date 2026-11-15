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
        var client = await InitializeAsync(); // Stellt sicher, dass der Client initialisiert ist (Mathias)

        try
        {
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
        var client = await InitializeAsync();

        try
        {
            var session = await client.Auth.SignIn(email, password);
            return session?.AccessToken != null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login fehlgeschlagen: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Meldet den aktuellen Benutzer ab.
    /// </summary>
    public async Task LogoutAsync()
    {
        var client = await InitializeAsync();
        await client.Auth.SignOut();
    }

    /// <summary>
    /// Prüft, ob ein Benutzer eingeloggt ist.
    /// </summary>
    public bool IsLoggedIn => _client?.Auth.CurrentSession != null;
}