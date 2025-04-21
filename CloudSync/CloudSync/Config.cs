namespace CloudSync;

public sealed class Config
{
    public bool AutoUpload { get; set; } = true;
    public bool BackupSaves { get; set; } = true;
    public bool PurgeBackups { get; set; } = true;
    public int BackupsToKeep { get; set; } = 2;
    public string? SelectedExtension { get; set; }

    public bool OverwriteSaveSettings { get; set; } = true;
    public int UiScale { get; set; } = 100;
    public int ZoomLevel { get; set; } = 100;
    public bool UseLegacySlingshotFiring { get; set; } = false;
    public bool ShowPlacementTileForGamepad { get; set; } = true;
    public bool Rumble { get; set; } = true;
}