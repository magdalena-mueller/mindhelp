using Microsoft.JSInterop;
using Newtonsoft.Json;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

namespace MindHelp.Wasm.Services;

public class BrowserSessionPersistence : IGotrueSessionPersistence<Session>
{
    private const string Key = "supabase.session";
    private readonly IJSInProcessRuntime _js;

    /// <summary>
    /// Initialisiert eine neue Instanz der BrowserSessionPersistence-Klasse.
    /// </summary>
    
    /// <param name="js">Der JavaScript-Runtime-Service.</param>

    public BrowserSessionPersistence(IJSRuntime js)
    {
        _js = (IJSInProcessRuntime)js;
    }

    public void SaveSession(Session session)
        => _js.InvokeVoid("localStorage.setItem", Key, JsonConvert.SerializeObject(session));

    public void DestroySession()
        => _js.InvokeVoid("localStorage.removeItem", Key);

    public Session? LoadSession()
    {
        var json = _js.Invoke<string?>("localStorage.getItem", Key);
        return json == null ? null : JsonConvert.DeserializeObject<Session>(json);
    }
}