@page "/login"
@inject SupaBaseService SupabaseService
@inject NavigationManager Nav

<MudTextField @bind-Value="_email" Label="E-Mail" />
<MudTextField @bind-Value="_password" Label="Passwort" InputType="InputType.Password" />
<MudButton OnClick="DoLogin">Anmelden</MudButton>
<MudText Color="Color.Error">@_error</MudText>

@code {
    private string _email = "";
    private string _password = "";
    private string _error = "";

    private async Task DoLogin()
    {
        var ok = await SupabaseService.LoginAsync(_email, _password);

        if (ok)
            Nav.NavigateTo("/");
        else
            _error = "E-Mail oder Passwort falsch.";
    }
}
