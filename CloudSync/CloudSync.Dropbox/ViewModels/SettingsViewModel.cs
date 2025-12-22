using System.Collections.Specialized;
using CloudSync.Dropbox.Mods;
using CloudSync.Extensions;
using CloudSync.Interfaces;
using CloudSync.Models;
using CloudSync.Utilities;
using CloudSync.ViewModels;
using Dropbox.Api;
using Newtonsoft.Json.Linq;
using PropertyChanged.SourceGenerator;
using StardewModdingAPI;
using StardewUI.Framework;
using StardewValley.Menus;
using SystemUtils = CloudSync.Utilities.System;

namespace CloudSync.Dropbox.ViewModels;

public partial class SettingsViewModel : ViewModelBase, IReadyToClose
{
    private bool _isAuthenticating;
    [Notify] private bool _isLoggedIn;

    [Notify] private string _appKey;
    [Notify] private string _refreshToken;
    [Notify] private string _timeoutText;

    public SettingsViewModel()
    {
        AppKey = Mod.Config.AppKey;
        RefreshToken = Mod.Config.RefreshToken;
        TimeoutText = Mod.Config.Timeout.ToString();
        CheckStatus();
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
        MenusManager.Show(controller, viewModel);
    }

    private void CheckStatus()
    {
        if (string.IsNullOrEmpty(Mod.Config.RefreshToken) || string.IsNullOrEmpty(Mod.Config.AppKey))
        {
            IsLoggedIn = false;
        }
        else
        {
            IsLoggedIn = true;
        }
    }

    public async Task Login()
    {
        if (!ReadyToClose())
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
            string redirectUri = $"http://127.0.0.1:{port}/dbx";
            string url =
                $"https://www.dropbox.com/oauth2/authorize" +
                $"?client_id={Mod.Config.AppKey}" +
                "&response_type=code&token_access_type=offline" +
                $"&code_challenge={codeChallenge}" +
                "&code_challenge_method=S256" +
                $"&redirect_uri={redirectUri}";

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
                    "https://api.dropboxapi.com/oauth2/token",
                    new()
                    {
                        { "code", code },
                        { "grant_type", "authorization_code" },
                        { "code_verifier", codeVerifier },
                        { "client_id", Mod.Config.AppKey },
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
            CheckStatus();
            _isAuthenticating = false;
        }
    }

    public void Logout()
    {
        if (!ReadyToClose())
        {
            return;
        }

        RefreshToken = string.Empty;
        CloudClient.DropboxClient = null;
        IsLoggedIn = false;
        SaveSettings();
    }

    private bool SaveSettings()
    {
        if (AppKey != Mod.Config.AppKey || RefreshToken != Mod.Config.RefreshToken &&
            (!string.IsNullOrEmpty(Mod.Config.AppKey) && !string.IsNullOrEmpty(Mod.Config.RefreshToken)))
        {
            DropboxClient dbx = new(Mod.Config.RefreshToken, Mod.Config.AppKey);
            dbx.Auth.TokenRevokeAsync().SafeFireAndForget(ex => Mod.Logger.Log($"An error occured while revoking the token: {ex}", LogLevel.Error));
        }

        Mod.Config.AppKey = AppKey;
        Mod.Config.RefreshToken = RefreshToken;

        if (string.IsNullOrEmpty(Mod.Config.RefreshToken) || string.IsNullOrEmpty(Mod.Config.AppKey))
        {
            CloudClient.DropboxClient = null;
            IsLoggedIn = false;
        }
        if (uint.TryParse(TimeoutText, out uint parsedTimeout) && parsedTimeout > 0)
        {
            if (Mod.Config.Timeout != parsedTimeout)
            {
                CloudClient.DropboxClient = null;
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

    public void Save()
    {
        if (!ReadyToClose())
        {
            return;
        }

        if (SaveSettings())
        {
            CloseMenu();
        }
    }

    public void Cancel()
    {
        if (ReadyToClose())
        {
            CloseMenu();
        }
    }

    public void Reset()
    {
        if (!ReadyToClose())
        {
            return;
        }

        Config newConfig = new();
        AppKey = newConfig.AppKey;
        RefreshToken = newConfig.RefreshToken;
        TimeoutText = newConfig.Timeout.ToString();

        SaveSettings();
    }

    public bool ReadyToClose()
    {
        return !_isAuthenticating;
    }
}