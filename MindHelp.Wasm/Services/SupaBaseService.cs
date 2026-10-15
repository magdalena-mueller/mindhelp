using Microsoft.JSInterop;

namespace MindHelp.Wasm.Services;

public class SupaBaseService
{
    private string SupabaseUrl = "https://btluoessffljxbznjabe.supabase.co";
    private string SupabaseKey = "sb_publishable_0EFTKf2BiAp8_YNgVuBnyg_iWe2WgcN";

    //Wieder einkommentiert
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
    /*public async Task<Supabase.Client> InitializeAsync()
    {
        if (_client != null)
        {
            return _client; // Client ist bereits initialisiert
        }
        var options = new Supabase.SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = true
        };

        var client = new Supabase.Client(SupabaseUrl, SupabaseKey, options);
        client.Auth.SetPersistence(new BrowserSessionPersistence(_js)); // Setzt die Session Persistence auf die BrowserSessionPersistence //Davor SetSessionPersistence
        await _client.InitializeAsync();

        _client = client; // Speichert den initialisierten Client für zukünftige Aufrufe (Mathias)
        return _client;    //(Mathias)
    } */


    public async Task<Supabase.Client> InitializeAsync()
    {
        if (_client == null)
        {
            var options = new Supabase.SupabaseOptions
            {
            AutoRefreshToken = true,
            AutoConnectRealtime = true
            };


            var client = new Supabase.Client(SupabaseUrl, SupabaseKey, options);
            client.Auth.SetPersistence(new BrowserSessionPersistence(_js));
            await client.InitializeAsync();
            _client = client;
        }
        return _client;
    }
   
    // Methode zum Registrieren eines neuen Benutzers
    public Task<bool> RegisterUserAsync(string email, string password)
    {
        return RegisterUserAsyncCore(email, password);
    }

    public Task<bool> RegisterUserAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        DateTime birthday,
        string specification)
    {
        return RegisterUserAsyncCore(email, password);
    }

    private async Task<bool> RegisterUserAsyncCore(string email, string password)
    {
        var client = await InitializeAsync(); // Stellt sicher, dass der Client initialisiert ist (Mathias)
        
        /*if (_client == null)
        {
            await InitializeAsync();
        }*/
        try
        {
            var response = await _client.Auth.SignUp(email, password); // Legt einen neuen User an
            
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

    //Wieder einkommentiert
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
    /// wieder einkommentiert
    public async Task LogoutAsync()
    {
        var client = await InitializeAsync();
        await client.Auth.SignOut();
    }

    /// <summary>
    /// Prüft, ob ein Benutzer eingeloggt ist.
    /// </summary>
    //Wieder einkommentiert 
    public bool IsLoggedIn => _client?.Auth.CurrentSession != null;

    
}