namespace SEToolbox.Models
{
    public class BlueprintDialogModel : BaseModel
    {
        #region Fields

        private string _blueprintName;
        private string _dialogTitle;
        private bool _checkForExisting;
        private string _localBlueprintsFolder;

        #endregion

        #region Properties

        public string BlueprintName
        {
            get => _blueprintName;

            set => SetProperty(ref _blueprintName, value, nameof(BlueprintName));
        }

        public string DialogTitle
        {
            get => _dialogTitle;

            set => SetProperty(ref _dialogTitle, value, nameof(DialogTitle));
        }

        public bool CheckForExisting
        {
            get => _checkForExisting;

            set => SetProperty( ref _checkForExisting, value, nameof(CheckForExisting));
        }

        public string LocalBlueprintsFolder
        {
            get => _localBlueprintsFolder;

            set => SetProperty(ref _localBlueprintsFolder, value, nameof(LocalBlueprintsFolder));
        }

        #endregion

        #region Methods

        public void Load(string dialogText, bool checkForExisting, string localBlueprintsFolder)
        {
            DialogTitle = dialogText;
            CheckForExisting = checkForExisting;
            LocalBlueprintsFolder = localBlueprintsFolder;
        }

        #endregion
    }
}
