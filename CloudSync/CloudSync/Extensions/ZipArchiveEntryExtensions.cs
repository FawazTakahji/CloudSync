using System.IO.Compression;

namespace CloudSync.Extensions;

public static class ZipArchiveEntryExtensions
{
    public static bool IsDirectory(this ZipArchiveEntry entry)
    {
        if (entry.FullName[^1] == '/' || entry.FullName[^1] == '\\')
        {
            return true;
        }

        byte lowerByte = (byte)(entry.ExternalAttributes & 0x00FF);
        FileAttributes attributes = (FileAttributes)lowerByte;
        return attributes.HasFlag(FileAttributes.Directory);
    }
}