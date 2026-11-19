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

        // SupabaseLogin und Chat und ProfilService registrieren, damit sie in den Komponenten per [Inject] verwendet werden können
        builder.Services.AddScoped<SupaBaseService>();
        builder.Services.AddScoped<ChatService>();
        builder.Services.AddScoped<ProfilService>();
        builder.Services.AddScoped<NotrufService>();
        builder.Services.AddScoped<TagebuchService>();
        builder.Services.AddScoped<HilfskraftService>();

        await builder.Build().RunAsync();
    }

    
}
