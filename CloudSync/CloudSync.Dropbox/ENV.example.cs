// Copy this file, rename it to ENV.cs, remove the preprocessor directive and replace the values with your own.
#if !ENV_EXISTS
namespace CloudSync.Dropbox;

public static class ENV
{
    public const string CLIENT_ID = "YOUR CLIENT ID";
}
#endif