using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using CloudSync.Interfaces;
using CloudSync.Models;
using CloudSync.Utilities;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Newtonsoft.Json;
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
        FileList folders = await rootRequest.ExecuteAsync();

        CloudSyncId = folders.Files.FirstOrDefault(f => f.Name == "CloudSync")?.Id;
        if (CloudSyncId is null)
        {
            DriveFile file = await drive.Files.Create(new DriveFile
            {
                Name = "CloudSync",
                MimeType = "application/vnd.google-apps.folder"
            }).ExecuteAsync();

            CloudSyncId = file.Id;

            DriveFile saves = await drive.Files.Create(new DriveFile
            {
                Name = "Saves",
                MimeType = "application/vnd.google-apps.folder",
                Parents = new List<string> { CloudSyncId }
            }).ExecuteAsync();
            SavesId = saves.Id;

            DriveFile backups = await drive.Files.Create(new DriveFile
            {
                Name = "Backups",
                MimeType = "application/vnd.google-apps.folder",
                Parents = new List<string> { CloudSyncId }
            }).ExecuteAsync();
            BackupsId = backups.Id;

            return;
        }

        FilesResource.ListRequest csRequest = drive.Files.List();
        csRequest.Q = $"mimeType='application/vnd.google-apps.folder' and '{CloudSyncId}' in parents and trashed = false";
        FileList csFolders = await csRequest.ExecuteAsync();

        SavesId = csFolders.Files.FirstOrDefault(f => f.Name == "Saves")?.Id;
        BackupsId = csFolders.Files.FirstOrDefault(f => f.Name == "Backups")?.Id;

        if (SavesId is null)
        {
            DriveFile saves = await drive.Files.Create(new DriveFile
            {
                Name = "Saves",
                MimeType = "application/vnd.google-apps.folder",
                Parents = new List<string> { CloudSyncId }
            }).ExecuteAsync();
            SavesId = saves.Id;
        }

        if (BackupsId is null)
        {
            DriveFile backups = await drive.Files.Create(new DriveFile
            {
                Name = "Backups",
                MimeType = "application/vnd.google-apps.folder",
                Parents = new List<string> { CloudSyncId }
            }).ExecuteAsync();
            BackupsId = backups.Id;
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

        foreach (DriveFile file in files)
        {
            await Drive.Files.Delete(file.Id).ExecuteAsync();
        }
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

        foreach (DriveFile file in allFiles)
        {
            await Drive.Files.Delete(file.Id).ExecuteAsync();
        }
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

        DriveFile destination = await Drive.Files.Create(new DriveFile
        {
            Name = $"{saveFolder.Name}_[{DateTimeOffset.Now.ToString(DateFormat).Replace(":", "")}]",
            MimeType = "application/vnd.google-apps.folder",
            Parents = new List<string> { BackupsId },
            Description = DateTimeOffset.Now.ToString(DateFormat)
        }).ExecuteAsync();

        DriveFile[] files = allFiles.Where(f => f.MimeType != "application/vnd.google-apps.folder" && f.Parents.Contains(saveFolder.Id)).ToArray();
        foreach (DriveFile file in files)
        {
            await Drive.Files.Copy(new DriveFile
            {
                Name = file.Name,
                Parents = new List<string> { destination.Id }
            }, file.Id).ExecuteAsync();
        }

        DriveFile[] folders = allFiles.Where(f => f.MimeType == "application/vnd.google-apps.folder" && f.Parents.Contains(saveFolder.Id)).ToArray();
        foreach (DriveFile folder in folders)
        {
            await CopyFolder(Drive, allFiles, folder, destination.Id);
        }
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
                await Drive.Files.Delete(backup.id).ExecuteAsync();
            }
        }
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

            FileList files = await savesRequest.ExecuteAsync();
            allFiles.AddRange(files.Files);
            nextPageToken = files.NextPageToken;
        } while (!string.IsNullOrEmpty(nextPageToken));

        return allFiles.ToArray();
    }

    private static async Task UploadDirectory(DriveService drive, string dir, string parentId, string? description = null, bool root = false)
    {
        DriveFile parent = await drive.Files.Create(new DriveFile
        {
            Name = Path.GetFileName(dir),
            MimeType = "application/vnd.google-apps.folder",
            Parents = new List<string> { parentId },
            Description = description
        }).ExecuteAsync();

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
            await using FileStream stream = File.OpenRead(file);
            await drive.Files.Create(fileToUpload, stream, "application/octet-stream").UploadAsync();
        }

        string[] dirs = Directory.GetDirectories(dir);
        foreach (string subDir in dirs)
        {
            await UploadDirectory(drive, subDir, parent.Id);
        }
    }

    private static async Task DownloadDirectory(DriveService drive, DriveFile[] allFiles, string parentId, string dir)
    {
        List<DriveFile> files = allFiles.Where(f => f.MimeType != "application/vnd.google-apps.folder" && f.Parents.Contains(parentId)).ToList();
        foreach (DriveFile file in files)
        {
            string filePath = Path.Combine(dir, file.Name);
            Directory.CreateDirectory(dir);
            await using FileStream stream = File.OpenWrite(filePath);
            await drive.Files.Get(file.Id).DownloadAsync(stream);
        }

        List<DriveFile> folders = allFiles.Where(f => f.MimeType == "application/vnd.google-apps.folder" && f.Parents.Contains(parentId)).ToList();
        foreach (DriveFile folder in folders)
        {
            await DownloadDirectory(drive, allFiles, folder.Id, Path.Combine(dir, folder.Name));
        }
    }

    private static async Task CopyFolder(DriveService drive, DriveFile[] allFiles, DriveFile source, string parentId)
    {
        DriveFile destination = await drive.Files.Create(new DriveFile
        {
            Name = source.Name,
            MimeType = "application/vnd.google-apps.folder",
            Parents = new List<string> { parentId }
        }).ExecuteAsync();

        DriveFile[] files = allFiles.Where(f => f.MimeType != "application/vnd.google-apps.folder" && f.Parents.Contains(source.Id)).ToArray();
        foreach (DriveFile file in files)
        {
            await drive.Files.Copy(new DriveFile
            {
                Name = file.Name,
                Parents = new List<string> { destination.Id }
            }, file.Id).ExecuteAsync();
        }

        DriveFile[] folders = allFiles.Where(f => f.MimeType == "application/vnd.google-apps.folder" && f.Parents.Contains(source.Id)).ToArray();
        foreach (DriveFile folder in folders)
        {
            await CopyFolder(drive, allFiles, folder, destination.Id);
        }
    }
}