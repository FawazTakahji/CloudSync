using CloudSync.Enums;
using CloudSync.Extensions;
using CloudSync.Interfaces;
using CloudSync.Models;
using CloudSync.Mods;
using PropertyChanged.SourceGenerator;
using StardewModdingAPI;
using StardewUI.Framework;
using StardewValley.Menus;

namespace CloudSync.ViewModels;

public partial class BackupsViewModel : ViewModelBase
{
    private readonly ICloudClient _client;
    [Notify] private List<Backup> _backups = new();
    [Notify] private bool _loaded;

    public BackupsViewModel(ICloudClient client)
    {
        _client = client;
    }

    public static void Show(ICloudClient client, IClickableMenu? parentMenu = null)
    {
        if (Api.StardewUI.ViewEngine is null)
        {
            Mod.Logger.Log("ViewEngine is null.", LogLevel.Warn);
            return;
        }

        BackupsViewModel viewModel = new(client);
        IMenuController controller = Api.StardewUI.ViewEngine.CreateMenuControllerFromAsset($"{Api.StardewUI.ViewsPrefix}/BackupsView", viewModel);
        MenusManager.Show(controller, viewModel, parentMenu, enableCloseButton: true);
        viewModel.LoadBackups()
            .SafeFireAndForget(ex => Mod.Logger.Log($"An error occured while retrieving the backups from the cloud: {ex}", LogLevel.Error));
    }

    public async Task LoadBackups()
    {
        var viewmodel = MessageBoxViewModel.Show(
            message: I18n.Messages_BackupsViewModel_RetrievingBackups(),
            buttons: MessageBoxButtons.None,
            readyToClose: () => false,
            Controller?.Menu);

        try
        {
            var backups = await _client.GetBackups();
            Backups = backups.Select(Backup.FromTuple)
                .OrderBy(o => o.CloudFolderName)
                .ToList();
            viewmodel?.Controller?.Menu.exitThisMenu();

            if (Backups.Count == 0)
            {
                MessageBoxViewModel.Show(I18n.Messages_BackupsViewModel_NoBackups(), parentMenu: Controller?.Menu.GetParentMenu());
                return;
            }

            Loaded = true;
        }
        catch (Exception ex)
        {
            Mod.Logger.Log($"An error occured while retrieving the backups from the cloud: {ex}", LogLevel.Error);
            MessageBoxViewModel.Show(I18n.Messages_BackupsViewModel_FailedRetrieveBackups_CheckLogs(), parentMenu: Controller?.Menu.GetParentMenu());
        }
    }

    public async Task PurgeBackups()
    {
        MessageBoxViewModel.Show(
            message: I18n.Messages_BackupsViewModel_PurgingBackups(),
            buttons: MessageBoxButtons.None,
            readyToClose: () => false,
            Controller?.Menu);
        try
        {
            await _client.PurgeBackups(Mod.Config.BackupsToKeep);
            MessageBoxViewModel.Show(I18n.Messages_BackupsViewModel_PurgedBackups(), parentMenu: Controller?.Menu);
        }
        catch (Exception ex)
        {
            Mod.Logger.Log($"An error occured while purging the backups: {ex}", LogLevel.Error);
            MessageBoxViewModel.Show(message: I18n.Messages_BackupsViewModel_FailedPurgeBackups_CheckLogs(), parentMenu: Controller?.Menu);
        }
    }

    public async Task DeleteBackup(Backup backup)
    {
        MessageBoxResult? result = await MessageBoxViewModel.ShowAsync(
            message: I18n.Messages_BackupsViewModel_DeleteBackupConfirm(backup.CloudFolderName),
            buttons: MessageBoxButtons.YesNo,
            parentMenu: Controller?.Menu);

        if (result is not MessageBoxResult.Yes)
        {
            return;
        }

        MessageBoxViewModel.Show(
            message: I18n.Messages_BackupsViewModel_DeletingBackup(),
            buttons: MessageBoxButtons.None,
            readyToClose: () => false,
            Controller?.Menu);

        try
        {
            await _client.DeleteBackup(backup.CloudFolderName);
            Backups = Backups.Where(b => b.CloudFolderName != backup.CloudFolderName).ToList();
            MessageBoxViewModel.Show(I18n.Messages_BackupsViewModel_BackupDeleted(), parentMenu: Controller?.Menu);
        }
        catch (Exception ex)
        {
            Mod.Logger.Log($"An error occured while deleting the backup \"{backup.CloudFolderName}\": {ex}", LogLevel.Error);
            MessageBoxViewModel.Show(message: I18n.Messages_BackupsViewModel_FailedDeleteBackup_CheckLogs(), parentMenu: Controller?.Menu);
        }
    }
}