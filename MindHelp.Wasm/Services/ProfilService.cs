using Microsoft.AspNetCore.Components.Forms;
using MindHelp.Wasm.Models;
using static Supabase.Postgrest.Constants;

namespace MindHelp.Wasm.Services;

// Profil der angemeldeten Person anzeigen und ändern.
// Die Sicherheitsregeln (RLS) in Supabase erlauben nur Änderungen am eigenen Profil.
public class ProfilService
{
    private const long MaxBildGroesse = 5 * 1024 * 1024;      // 5 MB
    private const long MaxNachweisGroesse = 10 * 1024 * 1024; // 10 MB

    private readonly SupaBaseService _supabase;

    public ProfilService(SupaBaseService supabase)
    {
        _supabase = supabase;
    }

    public async Task<string?> GetMeineEmailAsync()
    {
        var client = await _supabase.InitializeAsync();
        return client.Auth.CurrentUser?.Email;
    }

    public async Task<HelperDetails?> GetMeineHilfskraftDatenAsync(Guid id)
    {
        var client = await _supabase.InitializeAsync();
        var antwort = await client.From<HelperDetails>()
            .Filter("id", Operator.Equals, id.ToString())
            .Get();
        return antwort.Models.FirstOrDefault();
    }

    // Vorname, Nachname, Geburtstag (und Profilbild-Link) speichern
    public async Task SpeichereProfilAsync(Profile profil)
    {
        var client = await _supabase.InitializeAsync();
        await client.From<Profile>().Update(profil);
    }

    // Fachgebiet / Nachweis einer Hilfskraft speichern ("verifiziert" kann nur der Admin ändern)
    public async Task SpeichereHilfskraftDatenAsync(HelperDetails details)
    {
        var client = await _supabase.InitializeAsync();
        await client.From<HelperDetails>().Update(details);
    }

    // Profilbild hochladen (Bucket "avatars", öffentlich) und den Link zurückgeben
    public async Task<string> LadeProfilbildHochAsync(Guid id, IBrowserFile datei)
    {
        var client = await _supabase.InitializeAsync();
        var inhalt = await LeseDateiAsync(datei, MaxBildGroesse);

        // Jedes Mal ein neuer Dateiname, damit der Browser kein altes Bild aus dem Zwischenspeicher zeigt
        var pfad = $"{id}/profilbild-{DateTime.UtcNow.Ticks}{Path.GetExtension(datei.Name)}";
        await client.Storage.From("avatars").Upload(inhalt, pfad, new Supabase.Storage.FileOptions
        {
            ContentType = datei.ContentType,
            Upsert = false
        });

        return client.Storage.From("avatars").GetPublicUrl(pfad);
    }

    // Berufsnachweis hochladen (Bucket "proofs", privat) und den Speicherpfad zurückgeben
    public async Task<string> LadeNachweisHochAsync(Guid id, IBrowserFile datei)
    {
        var client = await _supabase.InitializeAsync();
        var inhalt = await LeseDateiAsync(datei, MaxNachweisGroesse);

        var pfad = $"{id}/nachweis-{DateTime.UtcNow.Ticks}{Path.GetExtension(datei.Name)}";
        await client.Storage.From("proofs").Upload(inhalt, pfad, new Supabase.Storage.FileOptions
        {
            ContentType = datei.ContentType,
            Upsert = false
        });

        return pfad;
    }

    public async Task AenderePasswortAsync(string neuesPasswort)
    {
        var client = await _supabase.InitializeAsync();
        await client.Auth.Update(new Supabase.Gotrue.UserAttributes { Password = neuesPasswort });
    }

    private static async Task<byte[]> LeseDateiAsync(IBrowserFile datei, long maxGroesse)
    {
        if (datei.Size > maxGroesse)
            throw new InvalidOperationException($"Die Datei ist zu groß (maximal {maxGroesse / 1024 / 1024} MB).");

        using var stream = datei.OpenReadStream(maxGroesse);
        using var speicher = new MemoryStream();
        await stream.CopyToAsync(speicher);
        return speicher.ToArray();
    }
}