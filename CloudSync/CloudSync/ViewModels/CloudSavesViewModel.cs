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
using SaveUtils = CloudSync.Utilities.Saves;

namespace CloudSync.ViewModels;

public partial class CloudSavesViewModel : SavesViewModelBase
{
    private readonly ICloudClient _client;
    [Notify] private List<SaveData> _saves = new();

    public CloudSavesViewModel(ICloudClient client)
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

        CloudSavesViewModel viewModel = new(client);
        IMenuController controller = Api.StardewUI.ViewEngine.CreateMenuControllerFromAsset($"{Api.StardewUI.ViewsPrefix}/CloudSavesView", viewModel);
        MenusManager.Show(controller, viewModel, parentMenu, enableCloseButton: true);
        viewModel.LoadSaves()
            .SafeFireAndForget(ex => Mod.Logger.Log($"An error occured while retrieving the saves from the cloud: {ex}", LogLevel.Error));
    }

    public async Task LoadSaves()
    {
        MessageBoxViewModel.Show(
            message: I18n.Messages_CloudSavesViewModel_RetrievingSaves(),
            buttons: MessageBoxButtons.None,
            readyToClose: () => false,
            Controller?.Menu);

        try
        {
            var info = await _client.GetSaves();
            SaveInfo[] saves = info.saves.Select(SaveInfo.FromTuple).ToArray();
            Controller?.Menu.GetChildMenu()?.exitThisMenu();

            if (saves.Length == 0)
            {
                MessageBoxViewModel.Show(I18n.Messages_CloudSavesViewModel_NoSaves(), parentMenu: Game1.activeClickableMenu);
                return;
            }

            Saves = saves.Select(save => new SaveData(save)).ToList();

            if (info.loadFailed)
            {
                MessageBoxViewModel.Show(I18n.Messages_CloudSavesViewModel_CouldNotRetrieveSomeSaves_CheckLogs(), parentMenu: Controller?.Menu);
            }
        }
        catch (Exception ex)
        {
            Mod.Logger.Log($"An error occured while retrieving the saves from the cloud: {ex}", LogLevel.Error);
            MessageBoxViewModel.Show(I18n.Messages_CloudSavesViewModel_FailedRetrieveSavesCloud_CheckLogs(), parentMenu: Game1.activeClickableMenu);
        }
    }

    public async Task DownloadSave(SaveInfo info)
    {
        if (Constants.SaveFolderName is not null && Constants.SaveFolderName.EqualsIgnoreCase(info.FolderName))
        {
            MessageBoxViewModel.Show(I18n.Messages_CloudSavesViewModel_ExitCurrentSave(), parentMenu: Controller?.Menu);
            return;
        }
        if (Mod.UploadingSaves.Contains(info.FolderName, StringComparer.OrdinalIgnoreCase))
        {
            MessageBoxViewModel.Show(I18n.Messages_CloudSavesViewModel_SaveUploadingWait(), parentMenu: Controller?.Menu);
            return;
        }

        MessageBoxViewModel.Show(
            message: I18n.Messages_CloudSavesViewModel_Downloading(),
            buttons: MessageBoxButtons.None,
            readyToClose: () => false,
            Controller?.Menu);

        bool shouldDownload = await ShouldDownload(info);
        if (!shouldDownload)
        {
            return;
        }

        string tempFolder = Path.Combine(Constants.SavesPath, "cstemp");
        string tempSavePath = Path.Combine(tempFolder, info.FolderName);
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
                I18n.Messages_CloudSavesViewModel_FailedDownloadSave_CheckLogs(),
                parentMenu: Controller?.Menu);
            return;
        }

        try
        {
            try
            {
                await _client.DownloadSave(info.FolderName, tempFolder);
            }
            catch (Exception ex)
            {
                Mod.Logger.Log($"An error occured while downloading the save \"{info.FolderName}\": {ex}", LogLevel.Error);
                MessageBoxViewModel.Show(
                    I18n.Messages_CloudSavesViewModel_FailedDownloadSave_CheckLogs(),
                    parentMenu: Controller?.Menu);
                return;
            }

            string savePath = Path.Combine(Constants.SavesPath, info.FolderName);
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
                    I18n.Messages_CloudSavesViewModel_CouldNotDeleteSave_CheckLogs(),
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
                    I18n.Messages_CloudSavesViewModel_FailedDownloadSave_CheckLogs(),
                    parentMenu: Controller?.Menu);
                return;
            }

            if (!Mod.Config.OverwriteSaveSettings)
            {
                MessageBoxViewModel.Show(I18n.Messages_CloudSavesViewModel_SaveDownloaded(), parentMenu: Controller?.Menu);
                return;
            }

            string saveFilePath = Path.Combine(savePath, info.FolderName);
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
                    I18n.Messages_CloudSavesViewModel_SaveDownloaded_SettingsOverwriteFailed_CheckLogs(),
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
                    I18n.Messages_CloudSavesViewModel_SettingsOverwriteFailed_SaveDeleted(),
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

            MessageBoxViewModel.Show(I18n.Messages_CloudSavesViewModel_SaveDownloaded(), parentMenu: Controller?.Menu);
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
    }

    public async Task DeleteSave(SaveInfo info)
    {
        if (Mod.UploadingSaves.Contains(info.FolderName, StringComparer.OrdinalIgnoreCase))
        {
            MessageBoxViewModel.Show(I18n.Messages_CloudSavesViewModel_SaveUploadingWait(), parentMenu: Controller?.Menu);
            return;
        }

        string displayName = $"{info.FarmerName} | {info.FarmName}";
        MessageBoxResult? result = await MessageBoxViewModel.ShowAsync(
            message: I18n.Messages_CloudSavesViewModel_SaveDeleteConfirm(displayName),
            buttons: MessageBoxButtons.YesNo,
            parentMenu: Controller?.Menu);
        if (result is not MessageBoxResult.Yes)
        {
            return;
        }

        MessageBoxViewModel.Show(
            message: I18n.Messages_CloudSavesViewModel_DeletingSave(),
            buttons: MessageBoxButtons.None,
            readyToClose: () => false,
            Controller?.Menu);
        try
        {
            await _client.DeleteSave(info.FolderName);

            MessageBoxViewModel.Show(I18n.Messages_CloudSavesViewModel_SaveDeleted(), parentMenu: Controller?.Menu);
            Saves = Saves.Where(save => save.Info.FolderName != info.FolderName).ToList();
        }
        catch (Exception ex)
        {
            Mod.Logger.Log($"An error occured while deleting the save \"{info.FolderName}\": {ex}", LogLevel.Error);
            MessageBoxViewModel.Show(I18n.Messages_CloudSavesViewModel_FailedDeleteSave_CheckLogs(displayName), parentMenu: Controller?.Menu);
        }
    }

    private async Task<bool> ShouldDownload(SaveInfo info)
    {
        SaveInfo? localInfo = null;
        try
        {
            SaveInfo[] localSaves = SaveUtils.GetLocalSaves().saves;
            localInfo = localSaves.FirstOrDefault(save => save.FolderName == info.FolderName);
        }
        catch (Exception ex)
        {
            Mod.Logger.Log($"An error occured while retrieving the local saves: {ex}", LogLevel.Error);
            MessageBoxResult? result1 = await MessageBoxViewModel.ShowAsync(
                message: I18n.Messages_CloudSavesViewModel_CouldNotCompare_OverwriteLocal(),
                buttons: MessageBoxButtons.YesNo,
                parentMenu: Controller?.Menu.GetChildMenu());

            if (result1 is not MessageBoxResult.Yes)
            {
                Controller?.Menu.GetChildMenu().exitThisMenu();
                return false;
            }
        }

        if (localInfo is null || info.DaysPlayed >= localInfo.DaysPlayed)
        {
            return true;
        }

        MessageBoxResult? result2 = await MessageBoxViewModel.ShowAsync(
            message: I18n.Messages_CloudSavesViewModel_LocalSaveNewer_OverwriteLocal(localInfo.DaysPlayed, info.DaysPlayed),
            buttons: MessageBoxButtons.YesNo,
            parentMenu: Controller?.Menu.GetChildMenu());

        if (result2 is not MessageBoxResult.Yes)
        {
            Controller?.Menu.GetChildMenu().exitThisMenu();
            return false;
        }

        return true;
    }
}