using CloudSync.Enums;
using CloudSync.Interfaces;
using CloudSync.Models;
using CloudSync.UI;
using CloudSync.ViewModels;
using StardewModdingAPI;
using StardewValley;
using StardewValley.SaveSerialization;


namespace CloudSync.Utilities;

public static class Saves
{
    public static (SaveInfo[] saves, bool loadFailed) GetLocalSaves()
    {
        if (!Directory.Exists(Constants.SavesPath))
        {
            return (Array.Empty<SaveInfo>(), false);
        }

        string[] subDirectories = Directory.GetDirectories(Constants.SavesPath);

        string tempDir = Path.Combine(Constants.SavesPath, "cstemp");
        List<SaveInfo> saves = new();
        bool loadFailed = false;
        foreach (string directory in subDirectories)
        {
            if (directory.Equals(tempDir, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            string saveGameInfoPath = Path.Combine(directory, "SaveGameInfo");
            if (!File.Exists(saveGameInfoPath))
            {
                loadFailed = true;
                Mod.Logger.Log($"Couldn't find the SaveGameInfo file in the directory \"{directory}\".", LogLevel.Warn);
                continue;
            }

            SaveInfo saveInfo;
            try
            {
                saveInfo = GetSaveInfo(savePath: directory, checkExistence: false);
            }
            catch (Exception ex)
            {
                Mod.Logger.Log($"An error occured while getting the info from \"{saveGameInfoPath}\": {ex}", LogLevel.Error);
                loadFailed = true;
                continue;
            }
            saves.Add(saveInfo);
        }

        return (saves.ToArray(), loadFailed);
    }

    public static SaveInfo GetSaveInfo(string? savePath = null, string? saveName = null, bool checkExistence = true)
    {
        if (string.IsNullOrEmpty(savePath) && string.IsNullOrEmpty(saveName))
        {
            throw new ArgumentException("Either savePath or saveName must be provided.");
        }

        string saveGameInfoPath = savePath is not null ? Path.Combine(savePath, "SaveGameInfo") : Path.Combine(Constants.SavesPath, saveName!, "SaveGameInfo");
        if (checkExistence && !File.Exists(saveGameInfoPath))
        {
            throw new FileNotFoundException($"The file \"{saveGameInfoPath}\" doesn't exist.");
        }

        using FileStream fileStream = File.OpenRead(saveGameInfoPath);
        Farmer farmer = SaveSerializer.Deserialize<Farmer>(fileStream);

        return new SaveInfo(
            folderName: saveName ?? Path.GetFileName(savePath!),
            farmerName: farmer.Name,
            farmName: farmer.farmName.Value,
            daysPlayed: (int)farmer.stats.DaysPlayed);
    }

    public static bool IsExcludedName(string fileName, string saveName)
    {
        return fileName.Equals("BACKUP_SAVE", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("SaveGameInfo_old", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals($"{saveName}_old", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals($"{saveName}_SVBAK", StringComparison.OrdinalIgnoreCase);
    }

    public static async Task Upload()
    {
        if (Mod.Config.SelectedExtension is null || Mod.Extensions.All(e => e.UniqueId != Mod.Config.SelectedExtension))
        {
            return;
        }
        IExtensionApi? api = Mod.ModHelper.ModRegistry.GetApi<IExtensionApi>(Mod.Config.SelectedExtension);
        if (api is null)
        {
            Mod.Logger.Log($"Couldn't load the api for the extension \"{Mod.Config.SelectedExtension}\".", LogLevel.Error);
            MessageBoxViewModel.Show(I18n.CouldNotUploadSave_CheckLogs());
            return;
        }
        ICloudClient client = api.GetClient();
        if (!client.IsAuthenticated())
        {
            Mod.Logger.Log("Couldn't upload the save to the cloud because the client is not authenticated.", LogLevel.Info);
            return;
        }
        if (Constants.CurrentSavePath is null || Constants.SaveFolderName is null)
        {
            Mod.Logger.Log("Couldn't get the current save path.", LogLevel.Error);
            MessageBoxViewModel.Show(I18n.CouldNotUploadSave_CheckLogs());
            return;
        }

        string currentUploadingSave = Constants.SaveFolderName;
        try
        {
            Mod.UploadingSaves.Add(currentUploadingSave);
            UploadBanner.Check();

            try
            {
                await client.BackupSave(currentUploadingSave);
            }
            catch (Exception ex)
            {
                Mod.Logger.Log($"An error occured while backing up the save \"{currentUploadingSave}\": {ex}", LogLevel.Error);
                MessageBoxResult? result = await MessageBoxViewModel.ShowAsync(
                    message: I18n.FailedBackupSave_Overwrite(),
                    buttons: MessageBoxButtons.YesNo);

                if (result is not MessageBoxResult.Yes)
                {
                    return;
                }
            }

            try
            {
                await client.DeleteSave(currentUploadingSave);
            }
            catch (Exception ex)
            {
                Mod.Logger.Log($"An error occured while deleting the old save \"{currentUploadingSave}\": {ex}", LogLevel.Error);
                MessageBoxViewModel.Show(I18n.CouldNotUploadSave_CheckLogs());
                return;
            }

            try
            {
                await client.UploadSave(currentUploadingSave);
            }
            catch (Exception ex)
            {
                Mod.Logger.Log($"An error occured while uploading the save \"{currentUploadingSave}\": {ex}", LogLevel.Error);
                MessageBoxViewModel.Show(I18n.CouldNotUploadSave_CheckLogs());
            }
        }
        finally
        {
            Mod.UploadingSaves.Remove(currentUploadingSave);
            UploadBanner.Check();
        }
    }

    public static async Task Purge()
    {
        if (Mod.Config.PurgeBackups == false || Mod.Config.SelectedExtension is null || Mod.Extensions.All(e => e.UniqueId != Mod.Config.SelectedExtension))
        {
            return;
        }
        Mod.Logger.Log($"Purging backups and keeping {Mod.Config.BackupsToKeep} " +
                       (Mod.Config.BackupsToKeep > 1 ? "backups" : "backup") + " for each save.", LogLevel.Info);
        IExtensionApi? api = Mod.ModHelper.ModRegistry.GetApi<IExtensionApi>(Mod.Config.SelectedExtension);
        if (api is null)
        {
            Mod.Logger.Log($"Couldn't load the api for the extension \"{Mod.Config.SelectedExtension}\".", LogLevel.Error);
            MessageBoxViewModel.Show(I18n.CouldNotBackupPurge_CheckLogs());
            return;
        }
        ICloudClient client = api.GetClient();
        if (!client.IsAuthenticated())
        {
            Mod.Logger.Log("Client is not authenticated.", LogLevel.Info);
            return;
        }

        try
        {
            await client.PurgeBackups(Mod.Config.BackupsToKeep);
        }
        catch (Exception ex)
        {
            Mod.Logger.Log($"An error occured while purging the backups: {ex}", LogLevel.Error);
            MessageBoxViewModel.Show(I18n.CouldNotBackupPurge_CheckLogs());
            return;
        }

        Mod.Logger.Log("Backups purged.", LogLevel.Info);
    }
}