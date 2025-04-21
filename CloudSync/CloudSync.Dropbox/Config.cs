namespace CloudSync.Dropbox;

public sealed class Config
{
    public string AppKey { get; set; } = ENV.CLIENT_ID;
    public string RefreshToken { get; set; } = string.Empty;
}