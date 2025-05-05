namespace CloudSync.Models;

public class Backup
{
    public readonly string FolderName;
    public readonly string CloudFolderName;
    public readonly  DateTimeOffset Date;
    public string DisplayName => $"{FolderName}{Environment.NewLine}{Date:d} {Date:hh:mm:ss t z}";

    public Backup(string folderName, string cloudFolderName, DateTimeOffset date)
    {
        FolderName = folderName;
        CloudFolderName = cloudFolderName;
        Date = date;
    }

    public static Backup FromTuple((string folderName, string cloudFolderName, DateTimeOffset date) backup)
    {
        return new Backup(backup.folderName, backup.cloudFolderName, backup.date);
    }
}