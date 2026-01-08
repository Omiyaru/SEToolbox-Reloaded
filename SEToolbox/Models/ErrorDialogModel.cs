namespace SEToolbox.Models
{
    public class ErrorDialogModel : BaseModel
    {
        #region Fields

        private string _errorDescription;
        private string _errorText;
        private bool _canContinue;

        #endregion

        #region Properties

        public string ErrorDescription
        {
            get => _errorDescription;
            set => SetProperty(ref _errorDescription, value, nameof(ErrorDescription));
        }

        public string ErrorText
        {
            get => _errorText;
            set => SetProperty(ref _errorText, value, nameof(ErrorText));
        }

        public bool CanContinue
        {
            get => _canContinue;
            set => SetProperty(ref _canContinue, value, nameof(CanContinue));
        }

        #endregion

        #region Methods

        public void Load(string errorDescription, string errorText, bool canContinue)
        {
            ErrorDescription = errorDescription ?? string.Empty;
            ErrorText = errorText ?? string.Empty;
            CanContinue = canContinue;
        }

        #endregion
    }
}
