using CloudSync.Interfaces;

namespace ExampleExtension;

public class CloudClient : ICloudClient
{
    /// <inheritdoc />
    public bool IsAuthenticated()
    {
        return true;
    }

    /// <inheritdoc />
    public async Task<((string folderName, string farmerName, string farmName, int daysPlayed)[] saves, bool loadFailed)> GetSaves()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async Task DeleteSave(string saveName)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async Task UploadSave(string saveName)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async Task DownloadSave(string saveName, string parentPath)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async Task BackupSave(string saveName)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async Task PurgeBackups(int backupsToKeep)
    {
        throw new NotImplementedException();
    }
}