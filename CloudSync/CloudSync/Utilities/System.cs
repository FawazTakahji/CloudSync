using System.Diagnostics;
using StardewModdingAPI;

namespace CloudSync.Utilities;

public static class System
{
    public static void OpenUri(string uri)
    {
        switch (Constants.TargetPlatform)
        {
            case GamePlatform.Windows:
                Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
                break;
            case GamePlatform.Linux:
                Process.Start("xdg-open", $"\"{uri}\"");
                break;
            case GamePlatform.Mac:
                Process.Start("open", uri);
                break;
            default:
                throw new NotSupportedException($"Unsupported platform: {Constants.TargetPlatform}");
        }
    }
}