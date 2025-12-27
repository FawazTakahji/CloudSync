using System.Collections.Specialized;
using CloudSync.Extensions;
using CloudSync.GoogleDrive.Mods;
using CloudSync.Interfaces;
using CloudSync.Models;
using CloudSync.Utilities;
using CloudSync.ViewModels;
using Google.Apis.Drive.v3;
using Newtonsoft.Json.Linq;
using PropertyChanged.SourceGenerator;
using StardewUI.Framework;
using StardewValley.Menus;
using StardewModdingAPI;
using SystemUtils = CloudSync.Utilities.System;

namespace CloudSync.GoogleDrive.ViewModels;

public partial class SettingsViewModel : ViewModelBase, IReadyToClose
{
    private bool _isAuthenticating;
    [Notify] private bool _isLoggedIn;
    [Notify] private string _clientId;
    [Notify] private string _clientSecret;
    [Notify] private string _refreshToken;
    [Notify] private string _timeoutText;

    public SettingsViewModel()
    {
        ClientId = Mod.Config.ClientId;
        ClientSecret = Mod.Config.ClientSecret;
        RefreshToken = Mod.Config.RefreshToken;
        TimeoutText = Mod.Config.Timeout.ToString();

        IsLoggedIn = !string.IsNullOrEmpty(ClientId) && !string.IsNullOrEmpty(RefreshToken);
    }

    public static void Show(IClickableMenu? parentMenu = null)
    {
        if (Api.ViewEngine is null)
        {
            Mod.Logger.Log("ViewEngine is null.", LogLevel.Warn);
            return;
        }

        SettingsViewModel viewModel = new();
        IMenuController controller = Api.ViewEngine.CreateMenuControllerFromAsset($"{Api.ViewsPrefix}/SettingsView", viewModel);
        viewModel.Controller = controller;
        MenusManager.Show(controller, viewModel);
    }

    private bool SaveSettings()
    {
        if (ClientId != Mod.Config.ClientId
            || ClientSecret != Mod.Config.ClientSecret
            || RefreshToken != Mod.Config.RefreshToken
            && !string.IsNullOrEmpty(Mod.Config.ClientId)
            && !string.IsNullOrEmpty(Mod.Config.RefreshToken))
        {
            CloudClient.Drive = null;
            Revoke().SafeFireAndForget(ex => Mod.Logger.Log($"An error occured while revoking the token: {ex}", LogLevel.Error));
        }

        Mod.Config.ClientId = ClientId;
        Mod.Config.ClientSecret = ClientSecret;
        Mod.Config.RefreshToken = RefreshToken;

        IsLoggedIn = !string.IsNullOrEmpty(ClientId) && !string.IsNullOrEmpty(RefreshToken);

        if (uint.TryParse(TimeoutText, out uint parsedTimeout) && parsedTimeout > 0)
        {
            if (Mod.Config.Timeout != parsedTimeout)
            {
                CloudClient.Drive = null;
            }
            Mod.Config.Timeout = parsedTimeout;
        }
        else
        {
            MessageBoxViewModel.Show(
                message: I18n.Messages_SettingsViewModel_InvalidTimeout(),
                parentMenu: Controller?.Menu);
            TimeoutText = Mod.Config.Timeout.ToString();
            return false;
        }

        try
        {
            Mod.ModHelper.WriteConfig(Mod.Config);
        }
        catch (Exception ex)
        {
            Mod.Logger.Log($"An error occured while saving settings: {ex}", LogLevel.Error);
        }

        return true;
    }

    public async Task Login()
    {
        if (_isAuthenticating)
        {
            return;
        }

        if (Constants.TargetPlatform == GamePlatform.Android)
        {
            MessageBoxViewModel.Show(
                message: I18n.Messages_SettingsViewModel_PlatformNotSupported(),
                parentMenu: Controller?.Menu);
            return;
        }

        try
        {
            _isAuthenticating = true;
            (string codeVerifier, string codeChallenge) = Auth.GeneratePkce();
            int port = Auth.GetRandomUnusedPort();
            string redirectUri = $"http://127.0.0.1:{port}/googledrive";
            string url = "https://accounts.google.com/o/oauth2/v2/auth" +
                         $"?client_id={ClientId}" +
                         "&response_type=code" +
                         $"&code_challenge={codeChallenge}" +
                         "&code_challenge_method=S256" +
                         $"&redirect_uri={redirectUri}" +
                         $"&scope={DriveService.Scope.DriveFile}";

            Listener listener = new($"{redirectUri}/");
            var browserViewModel = ButtonsBoxViewModel.Show(
                I18n.Messages_SettingsViewModel_CompleteAuthenticationInBrowser(),
                new[]
                {
                    new BoxButton(I18n.Ui_Buttons_Cancel(), () =>
                    {
                        listener.Stop();
                    })
                },
                () => false,
                Controller?.Menu);

            if (browserViewModel is null)
            {
                listener.Stop();
                return;
            }

            try
            {
                SystemUtils.OpenUri(url);
            }
            catch (Exception ex)
            {
                Mod.Logger.Log($"An error occured while trying to open the url: {ex}", LogLevel.Error);
                Mod.Logger.Log($"\"{url}\"", LogLevel.Info);
                StardewValley.DesktopClipboard.SetText(url);
                MessageBoxViewModel.Show(
                    message: I18n.Messages_SettingsViewModel_FailedOpenUrl_CopiedToClipboard(),
                    parentMenu: browserViewModel.Controller?.Menu);
            }

            NameValueCollection? query;
            try
            {
                query = await listener.ListenAsync();
                listener.Stop();
            }
            catch (Exception ex)
            {
                Mod.Logger.Log($"An error occured while listening to the browser: {ex}", LogLevel.Error);
                MessageBoxViewModel.Show(
                    I18n.Messages_SettingsViewModel_FailedRetrieveCode_CheckLogs(),
                    parentMenu: Controller?.Menu);
                return;
            }

            browserViewModel.Controller?.Close();
            if (query is null)
            {
                return;
            }

            string? code = query["code"];
            if (string.IsNullOrEmpty(code))
            {
                MessageBoxViewModel.Show(
                    message: I18n.Messages_SettingsViewModel_QueryNoCode(),
                    parentMenu: Controller?.Menu);
                return;
            }
            var tokenViewModel = MessageBoxViewModel.Show(
                I18n.Messages_SettingsViewModel_RetrievingToken(),
                parentMenu: Controller?.Menu);

            JObject? json;
            try
            {
                json = await Auth.GetTokenResponse(
                    "https://oauth2.googleapis.com/token",
                    new()
                    {
                        { "code", code },
                        { "grant_type", "authorization_code" },
                        { "code_verifier", codeVerifier },
                        { "client_id", ClientId },
                        { "client_secret", ClientSecret },
                        { "redirect_uri", redirectUri }
                    });
            }
            catch (Exception ex)
            {
                Mod.Logger.Log($"An error occured while getting the token response: {ex}", LogLevel.Error);
                MessageBoxViewModel.Show(
                    message: I18n.Messages_SettingsViewModel_FailedGetTokenResponse_CheckLogs(),
                    parentMenu: Controller?.Menu);
                return;
            }

            if (json?["refresh_token"]?.ToString() is not { } refreshToken)
            {
                MessageBoxViewModel.Show(
                    message: I18n.Messages_SettingsViewModel_FailedAuth_NoToken(),
                    parentMenu: Controller?.Menu);
                return;
            }
            tokenViewModel?.Controller?.Close();
            RefreshToken = refreshToken;
            SaveSettings();
        }
        finally
        {
            _isAuthenticating = false;
            IsLoggedIn = !string.IsNullOrEmpty(ClientId) && !string.IsNullOrEmpty(ClientSecret) && !string.IsNullOrEmpty(RefreshToken);
        }
    }

    public void Logout()
    {
        if (_isAuthenticating)
        {
            return;
        }

        RefreshToken = string.Empty;
        IsLoggedIn = false;
        SaveSettings();
    }

    public void Save()
    {
        if (!ReadyToClose())
        {
            return;
        }

        if (SaveSettings())
        {
            Controller?.Close();
        }
    }

    public void Cancel()
    {
        if (ReadyToClose())
        {
            Controller?.Close();
        }
    }

    public void Reset()
    {
        if (_isAuthenticating)
        {
            return;
        }

        Config config = new();
        ClientId = config.ClientId;
        ClientSecret = config.ClientSecret;
        RefreshToken = config.RefreshToken;
        TimeoutText = config.Timeout.ToString();

        IsLoggedIn = false;
    }

    public bool ReadyToClose()
    {
        return !_isAuthenticating;
    }

    private async Task Revoke()
    {
        HttpClient client = new();
        await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, $"https://oauth2.googleapis.com/revoke?token={Mod.Config.RefreshToken}"));
    }
}