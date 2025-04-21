using CloudSync.ViewModels;
using StardewValley.Menus;

namespace CloudSync.Patches;

public static class LoadGameMenuPatches
{
    // prevent deleting saves while uploading
    public static bool receiveLeftClick_Prefix(int x, int y, LoadGameMenu __instance)
    {
        if (Mod.UploadingSaves.Count == 0 || __instance.deleteConfirmationScreen)
        {
            return true;
        }

        if (__instance.selected == -1)
        {
            for (int index = 0; index < __instance.deleteButtons.Count; ++index)
            {
                if (__instance.deleteButtons[index].containsPoint(x, y) && index < __instance.MenuSlots.Count)
                {
                    if (__instance.MenuSlots[index] is LoadGameMenu.SaveFileSlot menuSlot && Mod.UploadingSaves.Contains(menuSlot.Farmer.slotName))
                    {
                        MessageBoxViewModel.Show(I18n.Messages_Other_WaitUploadFinishBeforeDelete());
                        return false;
                    }
                }
            }
        }

        return true;
    }
}