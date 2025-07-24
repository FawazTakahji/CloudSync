namespace CloudSync.GoogleDrive;

public class Config
{
    public string ClientId { get; set; } = ENV.CLIENT_ID;
    public string ClientSecret { get; set; } = ENV.CLIENT_SECRET;
    public string RefreshToken { get; set; } = string.Empty;
    public uint Timeout { get; set; } = 5;
}