using CloudSync.ViewModels;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace CloudSync.Patches;

public static class GameLocationPatches
{
    // prevent overwriting save file while uploading
    public static bool answerDialogueAction_Prefix(string questionAndAnswer)
    {
        if (Mod.UploadingSaves.Count == 0 || Constants.SaveFolderName is null)
        {
            return true;
        }

        if (questionAndAnswer is { Length: 9 } and "Sleep_Yes")
        {
            if (Mod.UploadingSaves.Contains(Constants.SaveFolderName, StringComparer.OrdinalIgnoreCase))
            {
                if (Game1.activeClickableMenu is DialogueBox dialogueBox)
                {
                    dialogueBox.closeDialogue();
                }

                MessageBoxViewModel.Show(I18n.Messages_Other_WaitUploadFinish());

                return false;
            }
        }

        return true;
    }
}