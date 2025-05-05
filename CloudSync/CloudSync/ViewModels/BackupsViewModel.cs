using System.Xml;
using CloudSync.Enums;
using CloudSync.Extensions;
using CloudSync.Interfaces;
using CloudSync.Models;
using CloudSync.Mods;
using PropertyChanged.SourceGenerator;
using StardewModdingAPI;
using StardewUI.Framework;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.Menus;
using StardewValley.SaveSerialization;

namespace CloudSync.ViewModels;

public partial class BackupsViewModel : ViewModelBase
{
    private readonly ICloudClient _client;
    [Notify] private List<Backup> _backups = new();
    [Notify] private bool _loaded;

    public BackupsViewModel(ICloudClient client)
    {
        _client = client;
    }

    public static void Show(ICloudClient client, IClickableMenu? parentMenu = null)
    {
        if (Api.StardewUI.ViewEngine is null)
        {
            Mod.Logger.Log("ViewEngine is null.", LogLevel.Warn);
            return;
        }

        BackupsViewModel viewModel = new(client);
        IMenuController controller = Api.StardewUI.ViewEngine.CreateMenuControllerFromAsset($"{Api.StardewUI.ViewsPrefix}/BackupsView", viewModel);
        MenusManager.Show(controller, viewModel, parentMenu, enableCloseButton: true);
        viewModel.LoadBackups()
            .SafeFireAndForget(ex => Mod.Logger.Log($"An error occured while retrieving the backups from the cloud: {ex}", LogLevel.Error));
    }

    public async Task LoadBackups()
    {
        var viewmodel = MessageBoxViewModel.Show(
            message: I18n.Messages_BackupsViewModel_RetrievingBackups(),
            buttons: MessageBoxButtons.None,
            readyToClose: () => false,
            Controller?.Menu);

        try
        {
            var backups = await _client.GetBackups();
            Backups = backups.Select(Backup.FromTuple)
                .OrderBy(o => o.CloudFolderName)
                .ToList();
            viewmodel?.Controller?.Menu.exitThisMenu();

            if (Backups.Count == 0)
            {
                MessageBoxViewModel.Show(I18n.Messages_BackupsViewModel_NoBackups(), parentMenu: Controller?.Menu.GetParentMenu());
                return;
            }

            Loaded = true;
        }
        catch (Exception ex)
        {
            Mod.Logger.Log($"An error occured while retrieving the backups from the cloud: {ex}", LogLevel.Error);
            MessageBoxViewModel.Show(I18n.Messages_BackupsViewModel_FailedRetrieveBackups_CheckLogs(), parentMenu: Controller?.Menu.GetParentMenu());
        }
    }

    public async Task PurgeBackups()
    {
        MessageBoxViewModel.Show(
            message: I18n.Messages_BackupsViewModel_PurgingBackups(),
            buttons: MessageBoxButtons.None,
            readyToClose: () => false,
            Controller?.Menu);
        try
        {
            await _client.PurgeBackups(Mod.Config.BackupsToKeep);
            await MessageBoxViewModel.ShowAsync(I18n.Messages_BackupsViewModel_PurgedBackups(), parentMenu: Controller?.Menu);
        }
        catch (Exception ex)
        {
            Mod.Logger.Log($"An error occured while purging the backups: {ex}", LogLevel.Error);
            MessageBoxViewModel.Show(message: I18n.Messages_BackupsViewModel_FailedPurgeBackups_CheckLogs(), parentMenu: Controller?.Menu);
            return;
        }

        try
        {
            await LoadBackups();
        }
        catch (Exception ex)
        {
            Mod.Logger.Log($"An error occured while loading the backups: {ex}", LogLevel.Error);
        }
    }

    public async Task DeleteBackup(Backup backup)
    {
        MessageBoxResult? result = await MessageBoxViewModel.ShowAsync(
            message: I18n.Messages_BackupsViewModel_DeleteBackupConfirm(backup.CloudFolderName),
            buttons: MessageBoxButtons.YesNo,
            parentMenu: Controller?.Menu);

        if (result is not MessageBoxResult.Yes)
        {
            return;
        }

        MessageBoxViewModel.Show(
            message: I18n.Messages_BackupsViewModel_DeletingBackup(),
            buttons: MessageBoxButtons.None,
            readyToClose: () => false,
            Controller?.Menu);

        try
        {
            await _client.DeleteBackup(backup.CloudFolderName);
            Backups = Backups.Where(b => b.CloudFolderName != backup.CloudFolderName).ToList();
            MessageBoxViewModel.Show(I18n.Messages_BackupsViewModel_BackupDeleted(), parentMenu: Controller?.Menu);
        }
        catch (Exception ex)
        {
            Mod.Logger.Log($"An error occured while deleting the backup \"{backup.CloudFolderName}\": {ex}", LogLevel.Error);
            MessageBoxViewModel.Show(message: I18n.Messages_BackupsViewModel_FailedDeleteBackup_CheckLogs(), parentMenu: Controller?.Menu);
        }
    }

    public async Task RestoreBackup(Backup backup)
    {
        if (Constants.SaveFolderName is not null && Constants.SaveFolderName.EqualsIgnoreCase(backup.FolderName))
        {
            MessageBoxViewModel.Show(I18n.Messages_Other_ExitCurrentSave(), parentMenu: Controller?.Menu);
            return;
        }
        if (Mod.UploadingSaves.Contains(backup.FolderName, StringComparer.OrdinalIgnoreCase))
        {
            MessageBoxViewModel.Show(I18n.Messages_Other_SaveUploadingWait(), parentMenu: Controller?.Menu);
            return;
        }

        var viewModel = MessageBoxViewModel.Show(
            message: I18n.Messages_BackupsViewModel_RestoringBackup(),
            buttons: MessageBoxButtons.None,
            readyToClose: () => false,
            Controller?.Menu);

        string savePath = Path.Combine(Constants.SavesPath, backup.FolderName);
        try
        {
            if (Directory.Exists(savePath))
            {
                MessageBoxResult? result = await MessageBoxViewModel.ShowAsync(
                    message: I18n.Messages_BackupsViewModel_OverwriteSave(backup.FolderName),
                    buttons: MessageBoxButtons.YesNo,
                    parentMenu: viewModel?.Controller?.Menu);

                if (result is not MessageBoxResult.Yes)
                {
                    viewModel?.Controller?.Menu.exitThisMenu();
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Mod.Logger.Log($"An error occured while checking if the save \"{backup.FolderName}\" exists: {ex}", LogLevel.Error);
            MessageBoxViewModel.Show(
                I18n.Messages_BackupsViewModel_FailedRestoreBackup_CheckLogs(),
                parentMenu: Controller?.Menu);
            return;
        }

        string tempFolder = Path.Combine(Constants.SavesPath, "cstemp");
        string tempSavePath = Path.Combine(tempFolder, backup.CloudFolderName);
        try
        {
            if (Directory.Exists(tempSavePath))
            {
                Directory.Delete(tempSavePath, true);
            }
        }
        catch (Exception ex)
        {
            Mod.Logger.Log($"An error occured while deleting the temp save folder \"{tempSavePath}\": {ex}", LogLevel.Error);
            MessageBoxViewModel.Show(
                I18n.Messages_BackupsViewModel_FailedRestoreBackup_CheckLogs(),
                parentMenu: Controller?.Menu);
            return;
        }

        try
        {
            try
            {
                await _client.DownloadBackup(backup.CloudFolderName, tempFolder);
            }
            catch (Exception ex)
            {
                Mod.Logger.Log($"An error occured while downloading the backup \"{backup.CloudFolderName}\": {ex}", LogLevel.Error);
                MessageBoxViewModel.Show(
                    I18n.Messages_BackupsViewModel_FailedRestoreBackup_CheckLogs(),
                    parentMenu: Controller?.Menu);
                return;
            }

            try
            {
                if (Directory.Exists(savePath))
                {
                    Directory.Delete(savePath, true);
                }
            }
            catch (Exception ex)
            {
                Mod.Logger.Log($"An error occured while deleting the save folder \"{savePath}\": {ex}", LogLevel.Error);
                MessageBoxViewModel.Show(
                    I18n.Messages_BackupsViewModel_FailedRestoreBackup_CheckLogs(),
                    parentMenu: Controller?.Menu);
                return;
            }

            try
            {
                Directory.Move(tempSavePath, savePath);
            }
            catch (Exception ex)
            {
                Mod.Logger.Log($"An error occured while moving the temp save folder \"{tempSavePath}\" to \"{savePath}\": {ex}", LogLevel.Error);
                MessageBoxViewModel.Show(
                    I18n.Messages_BackupsViewModel_FailedRestoreBackup_CheckLogs(),
                    parentMenu: Controller?.Menu);
                return;
            }
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempSavePath))
                {
                    Directory.Delete(tempSavePath, true);
                }
            }
            catch (Exception ex)
            {
                Mod.Logger.Log($"An error occured while deleting the temp save folder \"{tempSavePath}\": {ex}", LogLevel.Error);
            }
        }

        if (!Mod.Config.OverwriteSaveSettings || Mod.GCSInstalled)
        {
            MessageBoxViewModel.Show(I18n.Messages_BackupsViewModel_BackupRestored(), parentMenu: Controller?.Menu);
            return;
        }

        string saveFilePath = Path.Combine(savePath, backup.FolderName);
        FileStream stream;
        SaveGame saveGame;
        try
        {
            stream = File.Open(saveFilePath, FileMode.Open, FileAccess.ReadWrite);
            saveGame = SaveSerializer.Deserialize<SaveGame>(stream);
        }
        catch (Exception ex)
        {
            Mod.Logger.Log($"An error occured while opening the save file \"{saveFilePath}\": {ex}", LogLevel.Error);
            MessageBoxViewModel.Show(
                I18n.Messages_BackupsViewModel_BackupRestored_SettingsOverwriteFailed_CheckLogs(),
                parentMenu: Controller?.Menu);
            return;
        }

        saveGame.options.singlePlayerDesiredUIScale = Mod.Config.UiScale / 100.0f;
        saveGame.options.localCoopDesiredUIScale = Mod.Config.UiScale / 100.0f;
        saveGame.options.singlePlayerBaseZoomLevel = Mod.Config.ZoomLevel / 100.0f;
        saveGame.options.localCoopBaseZoomLevel = Mod.Config.ZoomLevel / 100.0f;
        saveGame.options.useLegacySlingshotFiring = Mod.Config.UseLegacySlingshotFiring;
        saveGame.options.showPlacementTileForGamepad = Mod.Config.ShowPlacementTileForGamepad;
        saveGame.options.rumble = Mod.Config.Rumble;


        XmlWriter? writer = null;
        try
        {
            stream.SetLength(0);
            writer = XmlWriter.Create(stream);
            SaveSerializer.Serialize(writer, saveGame);

            writer.Close();
            stream.Close();
        }
        catch (Exception ex)
        {
            Mod.Logger.Log($"An error occured while saving the save file \"{saveFilePath}\": {ex}", LogLevel.Error);
            MessageBoxViewModel.Show(
                I18n.Messages_Other_SettingsOverwriteFailed_SaveDeleted(),
                parentMenu: Controller?.Menu);

            writer?.Close();
            stream.Close();

            try
            {
                Directory.Delete(savePath, true);
            }
            catch (Exception ex2)
            {
                Mod.Logger.Log($"An error occured while deleting the save folder \"{savePath}\": {ex2}",
                    LogLevel.Error);
            }

            return;
        }

        MessageBoxViewModel.Show(I18n.Messages_BackupsViewModel_BackupRestored(), parentMenu: Controller?.Menu);
    }
}