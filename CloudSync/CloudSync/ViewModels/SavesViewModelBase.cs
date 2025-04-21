using CloudSync.Models;

namespace CloudSync.ViewModels;

public class SavesViewModelBase : ViewModelBase
{
    public void ShowInfo(SaveInfo info)
    {
        MessageBoxViewModel.Show(I18n.Messages_SavesViewModel_SaveInfo(info.FarmName, info.FarmerName, info.DaysPlayed, info.FolderName),
            parentMenu: Controller?.Menu);
    }
}