namespace CloudSync.Models;

public class Extension
{
    public readonly string? Name;
    public readonly string Author;
    public readonly string UniqueId;

    public Extension(string? name, string author, string uniqueId)
    {
        Name = name;
        Author = author;
        UniqueId = uniqueId;
    }
}