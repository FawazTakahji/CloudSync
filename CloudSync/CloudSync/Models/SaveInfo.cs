using Newtonsoft.Json;

namespace CloudSync.Models;

public class SaveInfo
{
    [JsonProperty("folderName", Required = Required.Always)]
    public readonly string FolderName;
    [JsonProperty("farmerName", Required = Required.Always)]
    public readonly string FarmerName;
    [JsonProperty("farmName", Required = Required.Always)]
    public readonly string FarmName;
    [JsonProperty("daysPlayed", Required = Required.Always)]
    public readonly int DaysPlayed;

    public SaveInfo(string folderName, string farmerName, string farmName, int daysPlayed)
    {
        FolderName = folderName;
        FarmerName = farmerName;
        FarmName = farmName;
        DaysPlayed = daysPlayed;
    }

    public static SaveInfo FromTuple((string folderName, string farmerName, string farmName, int daysPlayed) save)
    {
        return new SaveInfo(save.folderName, save.farmerName, save.farmName, save.daysPlayed);
    }
}