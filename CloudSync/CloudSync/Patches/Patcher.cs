using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace CloudSync.Patches;

public static class Patcher
{
    public static void Apply(string uniqueID)
    {
        try
        {
            Harmony harmony = new(uniqueID);

            harmony.Patch(
                original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.answerDialogueAction)),
                prefix: new HarmonyMethod(typeof(GameLocationPatches), nameof(GameLocationPatches.answerDialogueAction_Prefix))
            );

            harmony.Patch(
                original: AccessTools.Method(typeof(LoadGameMenu), nameof(LoadGameMenu.receiveLeftClick)),
                prefix: new HarmonyMethod(typeof(LoadGameMenuPatches), nameof(LoadGameMenuPatches.receiveLeftClick_Prefix))
            );
        }
        catch (Exception ex)
        {
            Mod.Logger.Log($"An error occurred while applying harmony patches: {ex}", LogLevel.Error);
        }
    }
}