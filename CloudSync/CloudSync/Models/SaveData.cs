namespace CloudSync.Models;

public class SaveData
{
    public SaveInfo Info { get;  }
    public string DisplayName => $"{Info.FarmerName} | {Info.FarmName} Farm";

    public SaveData(SaveInfo info)
    {
        Info = info;
    }
}