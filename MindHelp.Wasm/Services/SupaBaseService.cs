namespace MindHelp.Wasm.Services;

public class SupaBaseService
{
    string url = "https://btluoessffljxbznjabe.supabase.co";
    string key = "sb_publishable_0EFTKf2BiAp8_YNgVuBnyg_iWe2WgcN";

    private Supabase.Client _client;

    public async Task InitializeAsync()
    {
        var options = new Supabase.SupabaseOptions
        {
            AutoConnectRealtime = true
        };

        _client = new Supabase.Client(url, key, options);
        await _client.InitializeAsync();
    }

    public async Task RegisterUserAsync(string email, string password)
    {
        if (_client == null)
        {
            await InitializeAsync();
        }

        var response = await _client.Auth.SignUp(email, password); // Legt einen neuen User an
        if (response.User != null)
        {
            Console.WriteLine($"User registered: {response.User.Email}");
        }
        else
        {
            //Console.WriteLine($"Error registering user: {response.Error.Message}");
        }
    }

}