using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MindHelp.Wasm.Services;
using MudBlazor.Services;

namespace MindHelp.Wasm;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services.AddMudServices();

        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

        // SupabaseLogin und Chat
        builder.Services.AddScoped<SupaBaseService>();
        builder.Services.AddScoped<ChatService>();
        builder.Services.AddScoped<ProfilService>();

        await builder.Build().RunAsync();
    }

    
}
