using CloudSync.Enums;
using CloudSync.Interfaces;
using CloudSync.Models;
using CloudSync.Mods;
using PropertyChanged.SourceGenerator;
using StardewModdingAPI;
using StardewUI.Framework;
using StardewValley.Menus;

namespace CloudSync.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    [Notify] private bool _autoUpload;
    [Notify] private bool _backupSaves;
    [Notify] private bool _purgeBackups;
    [Notify] private int _backupsToKeep;
    [Notify] private Extension? _selectedExtension;
    [Notify] private bool _isExtensionSettingsVisible;

    [Notify] private bool _overwriteSaveSettings;
    [Notify] private int _uiScale;
    [Notify] private int _zoomLevel;
    [Notify] private bool _useLegacySlingshotFiring;
    [Notify] private bool _showPlacementTileForGamepad;
    [Notify] private bool _rumble;

    public List<Extension> Extensions { get; set; }
    public List<bool> SlingshotFireModes { get; set; } = new()
    {
        false,
        true
    };

    public Func<Extension, string> ExtensionFormat { get; } = ext =>
        !string.IsNullOrEmpty(ext.Name) ? $"{ext.Name} by {ext.Author}"
            : ext.UniqueId;
    public Func<bool, string> SlingshotFireModeFormat { get; } = mode =>
        mode ? I18n.Ui_SettingsView_Setting_SlingshotFireMode_True()
            : I18n.Ui_SettingsView_Setting_SlingshotFireMode_False();

    public SettingsViewModel(List<Extension> extensions)
    {
        AutoUpload = Mod.Config.AutoUpload;
        BackupSaves = Mod.Config.BackupSaves;
        PurgeBackups = Mod.Config.PurgeBackups;
        BackupsToKeep = Mod.Config.BackupsToKeep;

        OverwriteSaveSettings = Mod.Config.OverwriteSaveSettings;
        UiScale = Mod.Config.UiScale;
        ZoomLevel = Mod.Config.ZoomLevel;
        UseLegacySlingshotFiring = Mod.Config.UseLegacySlingshotFiring;
        ShowPlacementTileForGamepad = Mod.Config.ShowPlacementTileForGamepad;
        Rumble = Mod.Config.Rumble;

        Extensions = new()
        {
            new Extension(string.Empty, string.Empty, string.Empty)
        };
        Extensions.AddRange(extensions);

        if (!string.IsNullOrEmpty(Mod.Config.SelectedExtension))
        {
            SelectedExtension = Extensions.FirstOrDefault(ext => ext.UniqueId == Mod.Config.SelectedExtension);
            IsExtensionSettingsVisible = SelectedExtension is not null && !string.IsNullOrEmpty(SelectedExtension.UniqueId);
        }

        PropertyChanged += (_, _) => IsExtensionSettingsVisible = SelectedExtension is not null && !string.IsNullOrEmpty(SelectedExtension.UniqueId);
    }

    public static void Show(List<Extension> extensions, IClickableMenu? parentMenu = null)
    {
        if (Api.StardewUI.ViewEngine is null)
        {
            Mod.Logger.Log("ViewEngine is null.", LogLevel.Warn);
            return;
        }

        SettingsViewModel viewModel = new(extensions);
        IMenuController controller = Api.StardewUI.ViewEngine.CreateMenuControllerFromAsset($"{Api.StardewUI.ViewsPrefix}/SettingsView", viewModel);
        MenusManager.Show(controller, viewModel, parentMenu: parentMenu);
    }

    public void OpenExtensionSettings()
    {
        if (SelectedExtension is null)
        {
            return;
        }

        IExtensionApi? api = Mod.ModHelper.ModRegistry.GetApi<IExtensionApi>(SelectedExtension.UniqueId);
        if (api is null)
        {
            MessageBoxViewModel.Show(I18n.Messages_SettingsViewModel_FailedLoadExtensionApi(), parentMenu: Controller?.Menu);
            return;
        }

        api.ShowSettings(Controller?.Menu);
    }

    public async Task ManualPurge()
    {
        if (SelectedExtension is null)
        {
            return;
        }

        IExtensionApi? api = Mod.ModHelper.ModRegistry.GetApi<IExtensionApi>(SelectedExtension.UniqueId);
        if (api is null)
        {
            MessageBoxViewModel.Show(I18n.Messages_SettingsViewModel_FailedLoadExtensionApi(), parentMenu: Controller?.Menu);
            return;
        }
        ICloudClient client = api.GetClient();
        if (!client.IsAuthenticated())
        {
            MessageBoxViewModel.Show(I18n.Messages_SettingsViewModel_FailedPurgeNotAuthenticated(), parentMenu: Controller?.Menu);
            return;
        }

        MessageBoxViewModel.Show(
            I18n.Messages_SettingsViewModel_PurgingBackups(),
            MessageBoxButtons.None,
            () => false,
            Controller?.Menu);
        try
        {
            await client.PurgeBackups(Mod.Config.BackupsToKeep);
            MessageBoxViewModel.Show(I18n.Messages_SettingsViewModel_PurgedBackups(), parentMenu: Controller?.Menu);
        }
        catch (Exception ex)
        {
            Mod.Logger.Log($"An error occured while purging backups: {ex}", LogLevel.Error);
            MessageBoxViewModel.Show(message: I18n.Messages_SettingsViewModel_FailedPurgeBackups_CheckLogs(), parentMenu: Controller?.Menu);
        }
    }

    public void Save()
    {
        Mod.Config.AutoUpload = AutoUpload;
        Mod.Config.BackupSaves = BackupSaves;
        Mod.Config.PurgeBackups = PurgeBackups;
        Mod.Config.BackupsToKeep = BackupsToKeep;
        Mod.Config.SelectedExtension = SelectedExtension?.UniqueId;

        Mod.Config.OverwriteSaveSettings = OverwriteSaveSettings;
        Mod.Config.UiScale = UiScale;
        Mod.Config.ZoomLevel = ZoomLevel;
        Mod.Config.UseLegacySlingshotFiring = UseLegacySlingshotFiring;
        Mod.Config.ShowPlacementTileForGamepad = ShowPlacementTileForGamepad;
        Mod.Config.Rumble = Rumble;

        Mod.ModHelper.WriteConfig(Mod.Config);
        CloseMenu();
    }

    public void Reset()
    {
        Config newConfig = new();

        AutoUpload = newConfig.AutoUpload;
        BackupSaves = newConfig.BackupSaves;
        PurgeBackups = newConfig.PurgeBackups;
        BackupsToKeep = newConfig.BackupsToKeep;
        SelectedExtension = Extensions[0];

        OverwriteSaveSettings = newConfig.OverwriteSaveSettings;
        UiScale = newConfig.UiScale;
        ZoomLevel = newConfig.ZoomLevel;
        UseLegacySlingshotFiring = newConfig.UseLegacySlingshotFiring;
        ShowPlacementTileForGamepad = newConfig.ShowPlacementTileForGamepad;
        Rumble = newConfig.Rumble;
    }

    public void Cancel()
    {
        CloseMenu();
    }
}