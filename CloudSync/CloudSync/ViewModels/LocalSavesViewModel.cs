using CloudSync.Enums;
using CloudSync.Interfaces;
using CloudSync.Models;
using CloudSync.Mods;
using SaveUtils = CloudSync.Utilities.Saves;
using PropertyChanged.SourceGenerator;
using StardewModdingAPI;
using StardewUI.Framework;
using StardewValley.Menus;

namespace CloudSync.ViewModels;

public partial class LocalSavesViewModel : SavesViewModelBase
{
    private readonly ICloudClient _client;
    [Notify] private List<SaveData> _saves = new();

    public LocalSavesViewModel(List<SaveData> saves, ICloudClient client)
    {
        Saves = saves;
        _client = client;
    }

    public static void Show(ICloudClient client, IClickableMenu? parentMenu = null)
    {
        if (Api.StardewUI.ViewEngine is null)
        {
            Mod.Logger.Log("ViewEngine is null.", LogLevel.Warn);
            return;
        }

        (SaveInfo[] saves, bool loadFailed) info;
        try
        {
            info = SaveUtils.GetLocalSaves();
        }
        catch (Exception ex)
        {
            Mod.Logger.Log($"An error occured while retrieving the local saves: {ex}", LogLevel.Error);
            MessageBoxViewModel.Show(I18n.Messages_LocalSavesViewModel_FailedRetrieveSaves_CheckLogs(), parentMenu: parentMenu);
            return;
        }

        List<SaveData> saveData = info.saves.Select(save => new SaveData(save)).ToList();
        LocalSavesViewModel viewModel = new(saveData, client);
        IMenuController controller = Api.StardewUI.ViewEngine.CreateMenuControllerFromAsset($"{Api.StardewUI.ViewsPrefix}/LocalSavesView", viewModel);
        MenusManager.Show(controller, viewModel, parentMenu, enableCloseButton: true);

        if (info.loadFailed)
        {
            MessageBoxViewModel.Show(I18n.Messages_LocalSavesViewModel_CouldNotRetrieveSomeSaves_CheckLogs(), parentMenu: controller.Menu);
        }
    }

    public async Task UploadSave(SaveInfo info)
    {
        if (Mod.UploadingSaves.Contains(info.FolderName, StringComparer.OrdinalIgnoreCase))
        {
            MessageBoxViewModel.Show(I18n.Messages_LocalSavesViewModel_SaveAlreadyUploading(), parentMenu: Controller?.Menu);
            return;
        }

        MessageBoxViewModel.Show(
            message: I18n.Messages_LocalSavesViewModel_Uploading(),
            buttons: MessageBoxButtons.None,
            readyToClose: () => false,
            Controller?.Menu);

        bool shouldUpload = await ShouldUpload(info);
        if (!shouldUpload)
        {
            return;
        }
        bool shouldContinue = await TryBackup(info.FolderName);
        if (!shouldContinue)
        {
            return;
        }

        try
        {
            await _client.DeleteSave(info.FolderName);
        }
        catch (Exception ex)
        {
            Mod.Logger.Log($"An error occured while deleting the old save \"{info.FolderName}\": {ex}", LogLevel.Error);
            MessageBoxViewModel.Show(I18n.Messages_LocalSavesViewModel_FailedUploadSave(), parentMenu: Controller?.Menu);
            return;
        }
        try
        {
            await _client.UploadSave(info.FolderName);
        }
        catch (Exception ex)
        {
            Mod.Logger.Log($"An error occured while uploading the save \"{info.FolderName}\": {ex}", LogLevel.Error);
            MessageBoxViewModel.Show(I18n.Messages_LocalSavesViewModel_FailedUploadSave(), parentMenu: Controller?.Menu);
            return;
        }

        MessageBoxViewModel.Show(I18n.Messages_LocalSavesViewModel_SaveUploaded(), parentMenu: Controller?.Menu);
    }

    private async Task<bool> ShouldUpload(SaveInfo info)
    {
        SaveInfo? cloudInfo = null;
        try
        {
            SaveInfo[] cloudSaves = (await _client.GetSaves()).saves.Select(SaveInfo.FromTuple).ToArray();
            cloudInfo = cloudSaves.FirstOrDefault(save => save.FolderName == info.FolderName);
        }
        catch (Exception ex)
        {
            Mod.Logger.Log($"An error occured while retrieving the cloud saves: {ex}", LogLevel.Error);
            MessageBoxResult? result1 = await MessageBoxViewModel.ShowAsync(
                message: I18n.Messages_LocalSavesViewModel_CouldNotCompare_OverwriteCloud(),
                buttons: MessageBoxButtons.YesNo,
                parentMenu: Controller?.Menu.GetChildMenu());

            if (result1 is not MessageBoxResult.Yes)
            {
                Controller?.Menu.GetChildMenu().exitThisMenu();
                return false;
            }
        }

        if (cloudInfo is null || info.DaysPlayed >= cloudInfo.DaysPlayed)
        {
            return true;
        }

        MessageBoxResult? result2 = await MessageBoxViewModel.ShowAsync(
            message: I18n.Messages_LocalSavesViewModel_CloudSaveNewer_OverwriteCloud(cloudInfo.DaysPlayed, info.DaysPlayed),
            buttons: MessageBoxButtons.YesNo,
            parentMenu: Controller?.Menu.GetChildMenu());

        if (result2 is not MessageBoxResult.Yes)
        {
            Controller?.Menu.GetChildMenu().exitThisMenu();
            return false;
        }

        return true;
    }

    private async Task<bool> TryBackup(string folderName)
    {
        if (!Mod.Config.BackupSaves)
        {
            return true;
        }
        try
        {
            await _client.BackupSave(folderName);
        }
        catch (Exception ex)
        {
            Mod.Logger.Log($"An error occured while backing up the save \"{folderName}\": {ex}", LogLevel.Error);
            MessageBoxResult? result = await MessageBoxViewModel.ShowAsync(
                message: I18n.Messages_LocalSavesViewModel_FailedBackupExisting_Overwrite(),
                buttons: MessageBoxButtons.YesNo,
                parentMenu: Controller?.Menu.GetChildMenu());

            if (result is not MessageBoxResult.Yes)
            {
                Controller?.Menu.GetChildMenu().exitThisMenu();
                return false;
            }
        }

        return true;
    }
}