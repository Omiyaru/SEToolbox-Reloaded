namespace SEToolbox.Models
{
    public class LanguageModel : BaseModel
    {
        #region Fields

        private string _ietfLanguageTag;
        private string _imageName;
        private string _languageName;
        private string _nativeName;

        #endregion

        #region Properties

        public string IetfLanguageTag
        {
            get => _ietfLanguageTag;
            set => SetProperty(ref _ietfLanguageTag,value, nameof(IetfLanguageTag));
        }

        public string ImageName
        {
            get => _imageName;
            set => SetProperty(ref _imageName, value, nameof(ImageName));

        }

        public string Name
        {
            get => NativeName == LanguageName ? NativeName : string.Format($"{NativeName} / {LanguageName}"); 
        }

        /// <summary>
        /// Localized language name.
        /// </summary>
        public string LanguageName
        {
            get => _languageName;
            set => SetProperty(ref _languageName, value, nameof(LanguageName), nameof(Name));
        }

        public string NativeName
        {
            get => _nativeName;
            set => SetProperty(ref _nativeName, value, nameof(NativeName), nameof(Name));
        }

        #endregion
    }
}
