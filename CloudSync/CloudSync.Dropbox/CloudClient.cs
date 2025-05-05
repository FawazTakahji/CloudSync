using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using CloudSync.Dropbox.Models;
using CloudSync.Extensions;
using CloudSync.Interfaces;
using CloudSync.Models;
using CloudSync.Utilities;
using Dropbox.Api;
using Dropbox.Api.Files;
using Dropbox.Api.Stone;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using StardewModdingAPI;

namespace CloudSync.Dropbox;

public class CloudClient : ICloudClient
{
    private const string DateFormat = "yyyy-MM-ddTHH.mm.sszzz";
    private const string BackupRegex = @"^.+_\d+_\[\d{4}-\d{2}-\d{2}T\d{2}\.\d{2}\.\d{2}\+\d{4}\]$";
    private const string DateRegex = @"_\[(\d{4}-\d{2}-\d{2}T\d{2}\.\d{2}\.\d{2}\+\d{4})\]$";
    public static DropboxClient? DropboxClient;
    private static readonly ResiliencePipeline Pipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = new PredicateBuilder().Handle<RateLimitException>(),
            DelayGenerator = static args => new ValueTask<TimeSpan?>(args.AttemptNumber switch
            {
                0 => TimeSpan.Zero,
                1 => TimeSpan.FromSeconds(1),
                _ => TimeSpan.FromSeconds(5)
            })
        })
        .AddConcurrencyLimiter(9, int.MaxValue)
        .Build();

    [MemberNotNull(nameof(DropboxClient))]
    private static void CheckClient()
    {
        if (string.IsNullOrEmpty(Mod.Config.AppKey) || string.IsNullOrEmpty(Mod.Config.RefreshToken))
        {
            throw new Exception("The Dropbox client is null.");
        }

        DropboxClient ??= new DropboxClient(Mod.Config.RefreshToken, Mod.Config.AppKey);
    }

    /// <inheritdoc />
    public bool IsAuthenticated()
    {
        if (string.IsNullOrEmpty(Mod.Config.AppKey) || string.IsNullOrEmpty(Mod.Config.RefreshToken))
        {
            return false;
        }

        DropboxClient ??= new DropboxClient(Mod.Config.RefreshToken, Mod.Config.AppKey);
        return true;
    }

    /// <inheritdoc />
    public async Task<((string folderName, string farmerName, string farmName, int daysPlayed)[] saves, bool loadFailed)> GetSaves()
    {
        CheckClient();

        IDownloadResponse<DownloadZipResult> response;
        try
        {
            response = await Pipeline.ExecuteAsync(async _ => await DropboxClient.Files.DownloadZipAsync("/Info"));
        }
        catch (ApiException<DownloadZipError> ex)
        {
            if (ex.ErrorResponse.AsPath?.Value.IsNotFound ?? false)
            {
                return (Array.Empty<(string folderName, string farmerName, string farmName, int daysPlayed)>(), false);
            }

            throw;
        }

        await using Stream stream = await response.GetContentAsStreamAsync();
        using ZipArchive zip = new(stream, ZipArchiveMode.Read);

        List<SaveInfo> saves = new();
        bool loadFailed = false;
        foreach (ZipArchiveEntry entry in zip.Entries)
        {
            if (entry.IsDirectory())
            {
                continue;
            }

            try
            {
                await using Stream entryStream = entry.Open();
                using StreamReader reader = new(entryStream);
                string json = await reader.ReadToEndAsync();
                var save = JsonConvert.DeserializeObject<SaveInfo>(json);
                if (save is not null)
                {
                    saves.Add(save);
                }
                else
                {
                    loadFailed = true;
                    Mod.Logger.Log($"Couldn't parse the file \"{entry.FullName}\" as a save", LogLevel.Warn);
                }
            }
            catch (Exception ex)
            {
                loadFailed = true;
                Mod.Logger.Log($"An error occured while parsing the file \"{entry.FullName}\": {ex}", LogLevel.Error);
            }
        }

        return (saves.Select(save => (save.FolderName, save.FarmerName, save.FarmName, save.DaysPlayed)).ToArray(), loadFailed);
    }

    /// <inheritdoc />
    public async Task DeleteSave(string saveName)
    {
        CheckClient();

        bool failed = false;
        await Task.WhenAll(TryDeleteInfo(DropboxClient), TryDeleteFolder(DropboxClient));
        return;

        async Task TryDeleteInfo(DropboxClient client)
        {
            try
            {
                if (failed)
                {
                    return;
                }

                await DeleteSaveInfo(client, saveName);
            }
            catch (Exception)
            {
                failed = true;
                throw;
            }
        }

        async Task TryDeleteFolder(DropboxClient client)
        {
            try
            {
                if (failed)
                {
                    return;
                }

                await DeleteSaveFolder(client, saveName);
            }
            catch (Exception)
            {
                failed = true;
                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task UploadSave(string saveName)
    {
        CheckClient();

        SaveInfo info = Saves.GetSaveInfo(saveName: saveName);
        string json = JsonConvert.SerializeObject(info);
        await using Stream body = new MemoryStream(Encoding.UTF8.GetBytes(json));
        await Pipeline.ExecuteAsync(async _ => await DropboxClient.Files.UploadAsync(
            path: $"/Info/{saveName}.json",
            body: body,
            mute: true));

        string savePath = Path.Combine(Constants.SavesPath, saveName);
        string targetPath = Path.Combine("/Saves", saveName);
        UploadFileArg[] files = GetFiles(savePath, targetPath, true, saveName);

        bool failed = false;
        await Task.WhenAll(files.Select(arg => TryUpload(DropboxClient, arg)));
        return;

        async Task TryUpload(DropboxClient client, UploadFileArg arg)
        {
            try
            {
                if (failed)
                {
                    return;
                }

                await UploadFile(client, arg.LocalPath, arg.TargetPath);
            }
            catch (Exception)
            {
                failed = true;
                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task DownloadSave(string saveName, string parentPath)
    {
        CheckClient();

        IDownloadResponse<DownloadZipResult> response = await Pipeline.ExecuteAsync(async _ =>
            await DropboxClient.Files.DownloadZipAsync($"/Saves/{saveName}"));

        await using Stream stream = await response.GetContentAsStreamAsync();
        using ZipArchive zip = new(stream, ZipArchiveMode.Read);
        zip.ExtractToDirectory(parentPath);
    }

    /// <inheritdoc />
    public async Task<(string folderName, string cloudFolderName, DateTimeOffset date)[]> GetBackups()
    {
        CheckClient();

        List<Metadata> entries;
        try
        {
            entries = await ListFolderAll(DropboxClient, "/Backups");
        }
        catch (ApiException<ListFolderError> ex)
        {
            // Don't throw if there is no backups folder
            if (ex.ErrorResponse.AsPath?.Value.IsNotFound ?? false)
            {
                return Array.Empty<(string folderName, string cloudFolderName, DateTimeOffset date)>();
            }

            throw;
        }

        IEnumerable<Metadata> backups = entries.Where(e => e.IsFolder && Regex.IsMatch(e.Name, BackupRegex));

        return backups.Select(backup =>
        {
            DateTimeOffset date = DateTimeOffset.MinValue;
            Match match = Regex.Match(backup.Name, DateRegex);
            if (match.Success
                && DateTimeOffset.TryParseExact(
                    match.Groups[1].Value,
                    DateFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTimeOffset parsedDate))
            {
                date = parsedDate;
            }

            string[] split = backup.Name.Split('_');
            return (string.Join("_", split.Take(split.Length - 1)), backup.Name, date);
        }).ToArray();
    }

    /// <inheritdoc />
    public async Task DeleteBackup(string folderName)
    {
        CheckClient();

        await Pipeline.ExecuteAsync(async _ => await DropboxClient.Files.DeleteV2Async($"/Backups/{folderName}"));
    }

    /// <inheritdoc />
    public async Task BackupSave(string saveName)
    {
        CheckClient();

        try
        {
            await Pipeline.ExecuteAsync(async _ => await DropboxClient.Files.CopyV2Async(
                fromPath: $"/Saves/{saveName}",
                toPath: $"/Backups/{saveName}_[{DateTimeOffset.Now.ToString(DateFormat).Replace(":", "")}]"));
        }
        catch (ApiException<RelocationError> ex)
        {
            // Don't throw if an old save folder doesn't exist
            if (!ex.ErrorResponse.AsFromLookup?.Value.IsNotFound ?? true)
            {
                throw;
            }
        }
    }

    /// <inheritdoc/>
    public async Task DownloadBackup(string folderName, string parentPath)
    {
        CheckClient();

        IDownloadResponse<DownloadZipResult> response = await Pipeline.ExecuteAsync(async _ =>
            await DropboxClient.Files.DownloadZipAsync($"/Backups/{folderName}"));

        await using Stream stream = await response.GetContentAsStreamAsync();
        using ZipArchive zip = new(stream, ZipArchiveMode.Read);
        zip.ExtractToDirectory(parentPath);
    }

    /// <inheritdoc />
    public async Task PurgeBackups(int backupsToKeep)
    {
        CheckClient();

        List<Metadata> entries;
        try
        {
            entries = await ListFolderAll(DropboxClient, "/Backups");
        }
        catch (ApiException<ListFolderError> ex)
        {
            // Don't throw if there is no backups folder
            if (ex.ErrorResponse.AsPath?.Value.IsNotFound ?? false)
            {
                return;
            }

            throw;
        }

        IEnumerable<Metadata> backups = entries.Where(e => e.IsFolder && Regex.IsMatch(e.Name, BackupRegex));
        IEnumerable<IGrouping<string, Metadata>> groupedBackups = backups.GroupBy(backup =>
        {
            string[] split = backup.Name.Split('_');
            return $"{split[0]}_{split[1]}";
        });

        List<Task> tasks = new();
        foreach (IGrouping<string,Metadata> group in groupedBackups)
        {
            if (group.Count() <= backupsToKeep)
            {
                continue;
            }

            IOrderedEnumerable<Metadata> sortedBackups = group.OrderByDescending(backup =>
            {
                Match match = Regex.Match(backup.Name, DateRegex);
                if (!match.Success)
                {
                    return DateTimeOffset.MinValue;
                }

                if (DateTimeOffset.TryParseExact(
                        match.Groups[1].Value,
                        DateFormat,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTimeOffset parsedDate))
                {
                    return parsedDate;
                }

                return DateTimeOffset.MinValue;
            });

            IEnumerable<Metadata> backupsToDelete = sortedBackups.Skip(backupsToKeep);

            foreach (Metadata backup in backupsToDelete)
            {
                tasks.Add(TryDeleteBackup(DropboxClient, backup));
            }
        }

        await Task.WhenAll(tasks);
        return;

        async Task TryDeleteBackup(DropboxClient client, Metadata backup)
        {
            try
            {
                await Pipeline.ExecuteAsync(async _ => await client.Files.DeleteV2Async(backup.PathLower));
            }
            catch (Exception ex)
            {
                if (ex is not ApiException<DeleteError> deleteErrorEx || (!deleteErrorEx.ErrorResponse.AsPathLookup?.Value.IsNotFound ?? true))
                {
                    Mod.Logger.Log($"An error occured while deleting the backup \"{backup.Name}\": {ex}", LogLevel.Error);
                }
            }
        }
    }

    private static async Task DeleteSaveInfo(DropboxClient client, string saveName)
    {
        try
        {
            await Pipeline.ExecuteAsync(async _ => await client.Files.DeleteV2Async($"/Info/{saveName}.json"));
        }
        catch (ApiException<DeleteError> ex)
        {
            if (!ex.ErrorResponse.AsPathLookup?.Value.IsNotFound ?? true)
            {
                throw;
            }
        }
    }

    private static async Task DeleteSaveFolder(DropboxClient client, string saveName)
    {
        try
        {
            await Pipeline.ExecuteAsync(async _ => await client.Files.DeleteV2Async($"/Saves/{saveName}"));
        }
        catch (ApiException<DeleteError> ex)
        {
            if (!ex.ErrorResponse.AsPathLookup?.Value.IsNotFound ?? true)
            {
                throw;
            }
        }
    }

    private static UploadFileArg[] GetFiles(string folderPath, string targetPath, bool root = false, string? saveName = null)
    {
        List<UploadFileArg> files = new();

        string[] filePaths = Directory.GetFiles(folderPath);
        foreach (string filePath in filePaths)
        {
            string fileName = Path.GetFileName(filePath);

            if (root && saveName is not null && Saves.IsExcludedName(fileName, saveName))
            {
                continue;
            }

            files.Add(new UploadFileArg(filePath, Path.Combine(targetPath, fileName)));
        }

        string[] dirPaths = Directory.GetDirectories(folderPath);
        foreach (string dirPath in dirPaths)
        {
            string dirName = Path.GetFileName(dirPath);
            UploadFileArg[] subFiles = GetFiles(dirPath, Path.Combine(targetPath, dirName));
            files.AddRange(subFiles);
        }

        return files.ToArray();
    }

    private static async Task UploadFile(DropboxClient client, string localPath, string targetPath)
    {
        await using FileStream stream = File.OpenRead(localPath);
        await Pipeline.ExecuteAsync(async _ => await client.Files.UploadAsync(
            targetPath.Replace('\\', '/'),
            body: stream,
            mute: true));
    }

    private static async Task<List<Metadata>> ListFolderAll(DropboxClient client, string path)
    {
        List<Metadata> entries = new();

        ListFolderResult result = await Pipeline.ExecuteAsync(async _ => await client.Files.ListFolderAsync(path));
        entries.AddRange(result.Entries);

        while (result.HasMore)
        {
            result = await Pipeline.ExecuteAsync(async _ => await client.Files.ListFolderContinueAsync(result.Cursor));
            entries.AddRange(result.Entries);
        }

        return entries;
    }
}