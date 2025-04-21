using StardewModdingAPI;

namespace ExampleExtension;

public class Mod : StardewModdingAPI.Mod
{
    public override void Entry(IModHelper helper)
    {

    }

    public override object? GetApi()
    {
        return new ExtensionApi();
    }
}