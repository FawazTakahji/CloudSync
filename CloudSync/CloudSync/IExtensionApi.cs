using StardewValley.Menus;

// ReSharper disable once CheckNamespace
namespace CloudSync.Interfaces;

public interface IExtensionApi
{
    /// <summary>
    /// Gets the cloud client for the current extension.
    /// </summary>
    public ICloudClient GetClient();

    /// <summary>
    /// Shows the settings menu for the current extension.
    /// </summary>
    /// <param name="parentMenu">The menu that the settings menu should be a child of.</param>
    public void ShowSettings(IClickableMenu? parentMenu = null);
}

public interface ICloudClient
{
    /// <summary>
    /// Checks if the cloud client is authenticated.
    /// </summary>
    public bool IsAuthenticated();

    /// <summary>
    /// Get a list of saves on the cloud.
    /// </summary>
    /// <returns>A tuple containing the list of saves and a boolean indicating if there was a problem loading one of the saves.</returns>
    public Task<((string folderName, string farmerName, string farmName, int daysPlayed)[] saves, bool loadFailed)> GetSaves();

    /// <summary>
    /// Deletes a save from the cloud.
    /// </summary>
    /// <param name="saveName">The name of the save folder</param>
    public Task DeleteSave(string saveName);

    /// <summary>
    /// Uploads a save to the cloud.
    /// </summary>
    /// <param name="saveName">The name of the save folder</param>
    public Task UploadSave(string saveName);

    /// <summary>
    /// Downloads a save from the cloud.
    /// </summary>
    /// <param name="saveName">The name of the save folder</param>
    /// <param name="parentPath">The folder where the save will be downloaded to, this doesn't include the save name itself</param>
    public Task DownloadSave(string saveName, string parentPath);

    /// <summary>
    /// Get a list of backups on the cloud.
    /// </summary>
    public Task<(string folderName, string cloudFolderName, DateTimeOffset date)[]> GetBackups();

    /// <summary>
    /// Deletes a backup from the cloud.
    /// </summary>
    public Task DeleteBackup(string folderName);

    /// <summary>
    /// Backups an existing save on the cloud, this won't do anything if there is no existing save.
    /// </summary>
    /// <param name="saveName">The name of the save folder</param>
    public Task BackupSave(string saveName);

    /// <summary>
    /// Downloads a backup from the cloud.
    /// </summary>
    /// <param name="folderName">The name of the backup folder</param>
    /// <param name="parentPath">The folder where the backup will be downloaded to, this doesn't include the backup name itself</param>
    public Task DownloadBackup(string folderName, string parentPath);

    /// <summary>
    /// Deletes older backups of all saves.
    /// </summary>
    /// <param name="backupsToKeep">The number of backups to keep</param>
    public Task PurgeBackups(int backupsToKeep);
}