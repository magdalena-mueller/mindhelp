//using Micosoft.JSInterop;

namespace MindHelp.Wasm.Services;

public class SupaBaseService
{
    private string SupabaseUrl = "https://btluoessffljxbznjabe.supabase.co";
    private string SupabaseKey = "sb_publishable_0EFTKf2BiAp8_YNgVuBnyg_iWe2WgcN";

    /*private readonly IJSRuntime _js;

    public SupaBaseService(IJSRuntime js)
    {
        _js = js;
    }*/

    private Supabase.Client _client;

    /// <summary>
    /// Erstellt den Client beim ersten Aufruf und verbindet mit Supabase.
    /// Weitere Aufrufe liefern denselben Client zurück.
    /// </summary>
    public async Task<Supabase.Client> InitializeAsync()
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
        //clint.Auth.SetSessionPersistence(new BrowserSessionPersistence(_js)); // Setzt die Session Persistence auf die BrowserSessionPersistence
        await _client.InitializeAsync();

        _client = client; // Speichert den initialisierten Client für zukünftige Aufrufe (Mathias)
        return _client;    //(Mathias)
    } 
   
    // Methode zum Registrieren eines neuen Benutzers
    public async Task RegisterUserAsync(string email, string password)
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

        
        /*var response = await _client.Auth.SignUp(email, password); // Legt einen neuen User an
        if (response?.User != null)
        {
            Console.WriteLine($"User registered: {response.User.Email}");
        }
        else
        {
            Console.WriteLine("Signup lieferte keinen User zurück.");
            //Console.WriteLine($"Error registering user: {response.Error.Message}");
        }*/
    }

    // Meldet einen User an. Supabase liefert dabei das JWT (AccessToken).
    /*public async Task<bool> LoginAsync(string email, string password)
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
    }*/

    /// <summary>
    /// Meldet den aktuellen Benutzer ab.
    /// </summary>
    /*public async Task LogoutAsync()
    {
        var client = await InitializeAsync();
        await client.Auth.SignOut();
    }*/

    /// <summary>
    /// Prüft, ob ein Benutzer eingeloggt ist.
    /// </summary>
    //public bool IsLoggedIn => _client?.Auth.CurrentSession != null;

    
}