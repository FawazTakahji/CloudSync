using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.RateLimiting;
using CloudSync.Interfaces;
using CloudSync.Models;
using CloudSync.Utilities;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using StardewModdingAPI;
using StardewValley.Extensions;
using DriveFile = Google.Apis.Drive.v3.Data.File;
using File = System.IO.File;

namespace CloudSync.GoogleDrive;

public class CloudClient : ICloudClient
{
    public static DriveService? Drive;
    public static string? CloudSyncId;
    public static string? SavesId;
    public static string? BackupsId;

    private const string DateFormat = "yyyy-MM-ddTHH.mm.sszzz";
    private const string BackupRegex = @"^(.+_\d+)_\[\d{4}-\d{2}-\d{2}T\d{2}\.\d{2}\.\d{2}\+\d{4}\]$";

    private static readonly ResiliencePipeline Pipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = new PredicateBuilder().Handle<GoogleApiException>(ex => ex.HttpStatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests),
            DelayGenerator = static args => new ValueTask<TimeSpan?>(args.AttemptNumber switch
            {
                0 => TimeSpan.Zero,
                1 => TimeSpan.FromSeconds(1),
                _ => TimeSpan.FromSeconds(5)
            })
        })
        .AddRateLimiter(new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 12000,
            QueueLimit = int.MaxValue,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 4
        }))
        .Build();

    [MemberNotNull(nameof(Drive))]
    private static void CheckClient()
    {
        if (string.IsNullOrEmpty(Mod.Config.ClientId) ||
            string.IsNullOrEmpty(Mod.Config.ClientSecret) ||
            string.IsNullOrEmpty(Mod.Config.RefreshToken))
        {
            throw new Exception("Drive Service is null.");
        }

        if (Drive is not null)
        {
            return;
        }

        GoogleAuthorizationCodeFlow flow = new(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = Mod.Config.ClientId,
                ClientSecret = Mod.Config.ClientSecret
            },
            Scopes = new[] { DriveService.Scope.DriveFile }
        });

        TokenResponse response = new()
        {
            AccessToken = "no",
            RefreshToken = Mod.Config.RefreshToken
        };

        Drive = new DriveService(new BaseClientService.Initializer
        {
            ApplicationName = "CloudSync",
            HttpClientInitializer = new UserCredential(flow, "user", response)
        });
    }

    [MemberNotNull(nameof(CloudSyncId), nameof(SavesId), nameof(BackupsId))]
    private static async Task CheckFolders(DriveService drive)
    {
        if (CloudSyncId is not null && SavesId is not null && BackupsId is not null)
        {
            return;
        }

        FilesResource.ListRequest rootRequest = drive.Files.List();
        rootRequest.Q = "name='CloudSync' and mimeType='application/vnd.google-apps.folder' and 'root' in parents and trashed = false";
        FileList folders = await Pipeline.ExecuteAsync(async _ => await rootRequest.ExecuteAsync(CancellationToken.None));

        CloudSyncId = folders.Files.FirstOrDefault(f => f.Name == "CloudSync")?.Id;
        if (CloudSyncId is null)
        {
            DriveFile file = await Pipeline.ExecuteAsync(async _ => await drive.Files.Create(new DriveFile
            {
                Name = "CloudSync",
                MimeType = "application/vnd.google-apps.folder"
            }).ExecuteAsync(CancellationToken.None));

            CloudSyncId = file.Id;

            List<Task> tasks = new()
            {
                CreateSaves(drive, CloudSyncId),
                CreateBackups(drive, CloudSyncId)
            };
            await Task.WhenAll(tasks);
            if (SavesId is null)
            {
                throw new Exception("Failed to create the Saves folder.");
            }
            if (BackupsId is null)
            {
                throw new Exception("Failed to create the Backups folder.");
            }

            return;
        }

        FilesResource.ListRequest csRequest = drive.Files.List();
        csRequest.Q = $"mimeType='application/vnd.google-apps.folder' and '{CloudSyncId}' in parents and trashed = false";
        FileList csFolders = await csRequest.ExecuteAsync();

        SavesId = csFolders.Files.FirstOrDefault(f => f.Name == "Saves")?.Id;
        BackupsId = csFolders.Files.FirstOrDefault(f => f.Name == "Backups")?.Id;

        List<Task> createTasks = new();
        if (SavesId is null)
        {
            createTasks.Add(CreateSaves(drive, CloudSyncId));
        }
        if (BackupsId is null)
        {
            createTasks.Add(CreateBackups(drive, CloudSyncId));
        }

        if (createTasks.Count > 0)
        {
            await Task.WhenAll(createTasks);
        }

        if (SavesId is null)
        {
            throw new Exception("Failed to create the Saves folder.");
        }
        if (BackupsId is null)
        {
            throw new Exception("Failed to create the Backups folder.");
        }
    }

    public bool IsAuthenticated()
    {
        return !string.IsNullOrEmpty(Mod.Config.ClientId) && !string.IsNullOrEmpty(Mod.Config.ClientSecret) && !string.IsNullOrEmpty(Mod.Config.RefreshToken);
    }

    public async Task<((string folderName, string farmerName, string farmName, int daysPlayed)[] saves, bool loadFailed)> GetSaves()
    {
        CheckClient();
        await CheckFolders(Drive);

        DriveFile[] files = await GetAllFiles(Drive,
            fields: "name, description",
            q: $"mimeType='application/vnd.google-apps.folder' and '{SavesId}' in parents and trashed = false");

        List<SaveInfo> saveInfos = new();
        bool loadFailed = false;
        foreach (DriveFile file in files)
        {
            try
            {
                var info = JsonConvert.DeserializeObject<SaveInfo>(file.Description);
                if (info is not null)
                {
                    saveInfos.Add(info);
                }
                else
                {
                    loadFailed = true;
                    Mod.Logger.Log($"Couldn't deserialize the save info for \"{file.Name}\".", LogLevel.Warn);
                }
            }
            catch (Exception ex)
            {
                loadFailed = true;
                Mod.Logger.Log($"An error occured while deserializing the save info for \"{file.Name}\": {ex}", LogLevel.Error);
            }
        }

        return (saveInfos.Select(info => (info.FolderName, info.FarmerName, info.FarmName, info.DaysPlayed)).ToArray(), loadFailed);
    }

    public async Task DeleteSave(string saveName)
    {
        CheckClient();
        await CheckFolders(Drive);

        DriveFile[] files = await GetAllFiles(Drive,
            fields: "id",
            q: $"name='{saveName}' and mimeType='application/vnd.google-apps.folder' and '{SavesId}' in parents and trashed = false");

        List<Task> tasks = new();
        foreach (DriveFile file in files)
        {
            tasks.Add(Pipeline.ExecuteAsync(async _ => await Drive.Files.Delete(file.Id).ExecuteAsync(CancellationToken.None)).AsTask());
        }
        await Task.WhenAll(tasks);
    }

    public async Task UploadSave(string saveName)
    {
        CheckClient();

        SaveInfo info = Saves.GetSaveInfo(saveName: saveName);
        string json = JsonConvert.SerializeObject(info);
        await CheckFolders(Drive);

        string savePath = Path.Combine(Constants.SavesPath, saveName);
        await UploadDirectory(Drive, savePath, SavesId, json, true);
    }

    public async Task DownloadSave(string saveName, string parentPath)
    {
        CheckClient();
        await CheckFolders(Drive);

        DriveFile[] allFiles = await GetAllFiles(Drive,
            fields: "name, id, mimeType, parents",
            q: "trashed = false");
        DriveFile? saveFolder = allFiles.FirstOrDefault(f => f.Name.EqualsIgnoreCase(saveName)
                                                             && f.MimeType == "application/vnd.google-apps.folder"
                                                             && f.Parents.Contains(SavesId));
        if (saveFolder is null)
        {
            throw new Exception($"Couldn't find the save \"{saveName}\".");
        }

        await DownloadDirectory(Drive, allFiles, saveFolder.Id, Path.Combine(parentPath, saveName));
    }

    public async Task<(string folderName, string cloudFolderName, DateTimeOffset date)[]> GetBackups()
    {
        CheckClient();
        await CheckFolders(Drive);

        DriveFile[] allFiles = await GetAllFiles(Drive,
            fields: "name, description",
            q: $"mimeType='application/vnd.google-apps.folder' and '{BackupsId}' in parents and trashed = false");
        List<Backup> backups = new();

        foreach (DriveFile file in allFiles)
        {
            if (Regex.Match(file.Name, BackupRegex) is not { Success: true } match)
            {
                Mod.Logger.Log($"Couldn't parse the backup name for backup \"{file.Name}\".", LogLevel.Warn);
                continue;
            }
            if (string.IsNullOrEmpty(file.Description))
            {
                Mod.Logger.Log($"Couldn't parse the backup description for backup \"{file.Name}\".", LogLevel.Warn);
                continue;
            }

            DateTimeOffset date = !DateTimeOffset.TryParseExact(file.Description, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
                ? DateTimeOffset.MinValue
                : date;

            backups.Add(new Backup(match.Groups[1].Value, file.Name, date));
        }

        return backups.Select(backup => (backup.FolderName, backup.CloudFolderName, backup.Date)).ToArray();
    }

    public async Task DeleteBackup(string folderName)
    {
        CheckClient();
        await CheckFolders(Drive);

        DriveFile[] allFiles = await GetAllFiles(Drive,
            q: $"name='{folderName}' and mimeType='application/vnd.google-apps.folder' and '{BackupsId}' in parents and trashed = false");

        List<Task> tasks = new();
        foreach (DriveFile file in allFiles)
        {
            tasks.Add(Pipeline.ExecuteAsync(async _ => await Drive.Files.Delete(file.Id).ExecuteAsync(CancellationToken.None)).AsTask());
        }
        await Task.WhenAll(tasks);
    }

    public async Task BackupSave(string saveName)
    {
        CheckClient();
        await CheckFolders(Drive);

        DriveFile[] allFiles = await GetAllFiles(Drive,
            fields: "id, name, mimeType, parents",
            q: "trashed = false");

        DriveFile? saveFolder = allFiles.FirstOrDefault(f => f.Name.EqualsIgnoreCase(saveName)
                                                            && f.MimeType == "application/vnd.google-apps.folder"
                                                            && f.Parents.Contains(SavesId));
        if (saveFolder is null)
        {
            return;
        }

        DriveFile destination = await Pipeline.ExecuteAsync(async _ => await Drive.Files.Create(new DriveFile
        {
            Name = $"{saveFolder.Name}_[{DateTimeOffset.Now.ToString(DateFormat).Replace(":", "")}]",
            MimeType = "application/vnd.google-apps.folder",
            Parents = new List<string> { BackupsId },
            Description = DateTimeOffset.Now.ToString(DateFormat)
        }).ExecuteAsync(CancellationToken.None));

        List<Task> tasks = new();

        DriveFile[] files = allFiles.Where(f => f.MimeType != "application/vnd.google-apps.folder" && f.Parents.Contains(saveFolder.Id)).ToArray();
        foreach (DriveFile file in files)
        {
            tasks.Add(Pipeline.ExecuteAsync(async _ => await Drive.Files.Copy(new DriveFile
            {
                Name = file.Name,
                Parents = new List<string> { destination.Id },
            }, file.Id).ExecuteAsync(CancellationToken.None)).AsTask());
        }

        DriveFile[] folders = allFiles.Where(f => f.MimeType == "application/vnd.google-apps.folder" && f.Parents.Contains(saveFolder.Id)).ToArray();
        foreach (DriveFile folder in folders)
        {
            tasks.Add(CopyFolder(Drive, allFiles, folder, destination.Id));
        }

        await Task.WhenAll(tasks);
    }

    public async Task DownloadBackup(string folderName, string parentPath)
    {
        CheckClient();
        await CheckFolders(Drive);

        DriveFile[] allFiles = await GetAllFiles(Drive,
            fields: "id, name, mimeType, parents",
            q: "trashed = false");

        DriveFile? backupFolder = allFiles.FirstOrDefault(f => f.Name.EqualsIgnoreCase(folderName)
                                                               && f.MimeType == "application/vnd.google-apps.folder"
                                                               && f.Parents.Contains(BackupsId));
        if (backupFolder is null)
        {
            throw new Exception($"Couldn't find the backup \"{folderName}\".");
        }

        await DownloadDirectory(Drive, allFiles, backupFolder.Id, Path.Combine(parentPath, folderName));
    }

    public async Task PurgeBackups(int backupsToKeep)
    {
        CheckClient();
        await CheckFolders(Drive);

        DriveFile[] allFiles = await GetAllFiles(Drive,
            fields: "name, id, description",
            q: $"mimeType='application/vnd.google-apps.folder' and '{BackupsId}' in parents and trashed = false");

        List<(string folderName, string id, DateTimeOffset date)> backups = new();
        foreach (DriveFile file in allFiles)
        {
            if (Regex.Match(file.Name, BackupRegex) is not { Success: true } match)
            {
                Mod.Logger.Log($"Couldn't parse the backup name for backup \"{file.Name}\".", LogLevel.Warn);
                continue;
            }
            if (string.IsNullOrEmpty(file.Description))
            {
                Mod.Logger.Log($"Couldn't parse the backup date description for backup \"{file.Name}\".", LogLevel.Warn);
                continue;
            }
            if (!DateTimeOffset.TryParseExact(file.Description, DateFormat, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateTimeOffset date))
            {
                Mod.Logger.Log($"Couldn't parse the backup date for backup \"{file.Name}\".", LogLevel.Warn);
                continue;
            }

            backups.Add((match.Groups[1].Value, file.Id, date));
        }


        IEnumerable<IGrouping<string, (string folderName, string id, DateTimeOffset date)>> groupedBackups = backups.GroupBy(backup => backup.folderName);

        List<Task> tasks = new();
        foreach (IGrouping<string,(string folderName, string id, DateTimeOffset date)> group in groupedBackups)
        {
            if (group.Count() <= backupsToKeep)
            {
                continue;
            }

            IEnumerable<(string folderName, string id, DateTimeOffset date)> backupsToDelete = group
                .OrderByDescending(backup => backup.date)
                .Skip(backupsToKeep);

            foreach ((string folderName, string id, DateTimeOffset date) backup in backupsToDelete)
            {
                tasks.Add(TryDeleteBackup(Drive, backup.folderName, backup.id));
            }
        }

        await Task.WhenAll(tasks);
        return;

        async Task TryDeleteBackup(DriveService drive, string folderName, string id)
        {
            try
            {
                await Pipeline.ExecuteAsync(async _ => await drive.Files.Delete(id).ExecuteAsync(CancellationToken.None));
            }
            catch (Exception ex)
            {
                Mod.Logger.Log($"An error occured while deleting the backup \"{folderName}\" with id \"{id}\": {ex}", LogLevel.Error);
            }
        }
    }

    private static async Task CreateSaves(DriveService drive, string id)
    {
        DriveFile saves = await Pipeline.ExecuteAsync(async _ => await drive.Files.Create(new DriveFile
        {
            Name = "Saves",
            MimeType = "application/vnd.google-apps.folder",
            Parents = new List<string> { id }
        }).ExecuteAsync(CancellationToken.None));
        SavesId = saves.Id;
    }

    private static async Task CreateBackups(DriveService drive, string id)
    {
        DriveFile backups = await Pipeline.ExecuteAsync(async _ => await drive.Files.Create(new DriveFile
        {
            Name = "Backups",
            MimeType = "application/vnd.google-apps.folder",
            Parents = new List<string> { id }
        }).ExecuteAsync(CancellationToken.None));
        BackupsId = backups.Id;
    }

    private static async Task<DriveFile[]> GetAllFiles(DriveService drive, string? fields = null, string? q = null)
    {
        List<DriveFile> allFiles = new();
        string nextPageToken = string.Empty;
        do
        {
            FilesResource.ListRequest savesRequest = drive.Files.List();
            savesRequest.PageToken = nextPageToken;
            if (q is not null)
            {
                savesRequest.Q = q;
            }
            if (fields is not null)
            {
                savesRequest.Fields = $"nextPageToken, files({fields})";
            }

            FileList files = await Pipeline.ExecuteAsync(async _ => await savesRequest.ExecuteAsync(CancellationToken.None));
            allFiles.AddRange(files.Files);
            nextPageToken = files.NextPageToken;
        } while (!string.IsNullOrEmpty(nextPageToken));

        return allFiles.ToArray();
    }

    private static async Task UploadDirectory(DriveService drive, string dir, string parentId, string? description = null, bool root = false)
    {
        DriveFile parent = await Pipeline.ExecuteAsync(async _ => await drive.Files.Create(new DriveFile
        {
            Name = Path.GetFileName(dir),
            MimeType = "application/vnd.google-apps.folder",
            Parents = new List<string> { parentId },
            Description = description
        }).ExecuteAsync(CancellationToken.None));

        List<Task> tasks = new();

        string[] files = Directory.GetFiles(dir);
        foreach (string file in files)
        {
            string name = Path.GetFileName(file);
            if (root && Saves.IsExcludedName(name, Path.GetFileName(dir)))
            {
                continue;
            }

            DriveFile fileToUpload = new DriveFile
            {
                Name = name,
                Parents = new List<string> { parent.Id }
            };
            tasks.Add(UploadFile(drive, fileToUpload, file, "application/octet-stream"));
        }

        string[] dirs = Directory.GetDirectories(dir);
        foreach (string subDir in dirs)
        {
            tasks.Add(UploadDirectory(drive, subDir, parent.Id));
        }

        await Task.WhenAll(tasks);
    }

    private static async Task UploadFile(DriveService drive, DriveFile file, string path, string mimeType)
    {
        await using FileStream stream = File.OpenRead(path);
        await Pipeline.ExecuteAsync(async _ => await drive.Files.Create(file, stream, mimeType).UploadAsync(CancellationToken.None));
    }

    private static async Task DownloadDirectory(DriveService drive, DriveFile[] allFiles, string parentId, string dir)
    {
        List<Task> tasks = new();

        List<DriveFile> files = allFiles.Where(f => f.MimeType != "application/vnd.google-apps.folder" && f.Parents.Contains(parentId)).ToList();
        foreach (DriveFile file in files)
        {
            tasks.Add(DownloadFile(drive, dir, file));
        }

        List<DriveFile> folders = allFiles.Where(f => f.MimeType == "application/vnd.google-apps.folder" && f.Parents.Contains(parentId)).ToList();
        foreach (DriveFile folder in folders)
        {
            tasks.Add(DownloadDirectory(drive, allFiles, folder.Id, Path.Combine(dir, folder.Name)));
        }

        await Task.WhenAll(tasks);
    }

    private static async Task DownloadFile(DriveService drive, string parentPath, DriveFile file)
    {
        string filePath = Path.Combine(parentPath, file.Name);
        Directory.CreateDirectory(parentPath);
        await using FileStream stream = File.OpenWrite(filePath);
        await Pipeline.ExecuteAsync(async _ => await drive.Files.Get(file.Id).DownloadAsync(stream, CancellationToken.None));
    }

    private static async Task CopyFolder(DriveService drive, DriveFile[] allFiles, DriveFile source, string parentId)
    {
        DriveFile destination = await Pipeline.ExecuteAsync(async _ => await drive.Files.Create(new DriveFile
        {
            Name = source.Name,
            MimeType = "application/vnd.google-apps.folder",
            Parents = new List<string> { parentId }
        }).ExecuteAsync(CancellationToken.None));

        List<Task> tasks = new();

        DriveFile[] files = allFiles.Where(f => f.MimeType != "application/vnd.google-apps.folder" && f.Parents.Contains(source.Id)).ToArray();
        foreach (DriveFile file in files)
        {
            tasks.Add(Pipeline.ExecuteAsync(async _ => await drive.Files.Copy(new DriveFile
            {
                Name = file.Name,
                Parents = new List<string> { destination.Id }
            }, file.Id).ExecuteAsync(CancellationToken.None)).AsTask());
        }

        DriveFile[] folders = allFiles.Where(f => f.MimeType == "application/vnd.google-apps.folder" && f.Parents.Contains(source.Id)).ToArray();
        foreach (DriveFile folder in folders)
        {
            tasks.Add(CopyFolder(drive, allFiles, folder, destination.Id));
        }

        await Task.WhenAll(tasks);
    }
}