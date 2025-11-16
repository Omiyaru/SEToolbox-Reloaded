using SEToolbox.Interfaces;
using SEToolbox.Models;
using SEToolbox.ViewModels;
using SEToolbox.Views;

namespace SEToolbox.Services
{
    public static class DialogExtensions
    {
        public static bool? ShowErrorDialog(this IDialogService dialogService, BaseViewModel parentViewModel, string errorTitle, string errorInformation, bool canContinue)
        {
            ErrorDialogModel model = new();
            model.Load(errorTitle, errorInformation, canContinue);
            ErrorDialogViewModel loadVm = new(parentViewModel, model);
            return dialogService.ShowDialog<WindowErrorDialog>(parentViewModel, loadVm);
        }
    }
}
