using System.IO;
using SEToolbox.Support;


namespace SEToolbox.Models
{
    public class FindApplicationModel : BaseModel
    {
        #region Fields

        private string _gameApplicationPath;
        private string _gameBinPath;
        private bool _isValidApplication;
        private bool _isWrongApplication;

        #endregion

        #region Properties

        public string GameApplicationPath
        {
            get => _gameApplicationPath;
            set => SetProperty(ref _gameApplicationPath, value, nameof(GameApplicationPath), () => { Validate();});
           
        }

        public string GameBinPath
        {
            get => _gameBinPath;
            set => SetProperty(ref _gameBinPath, value, nameof(GameBinPath));
        }

        public bool IsValidApplication
        {
            get => _isValidApplication;
            set => SetProperty(ref _isValidApplication, value, nameof(IsValidApplication));
        }

        public bool IsWrongApplication
        {
            get => _isWrongApplication;
            set => SetProperty(ref _isWrongApplication, value, nameof(IsWrongApplication));
        }

        #endregion

        #region Methods

        public void Validate()
        {
            GameBinPath = null;
            IsValidApplication = false;
            IsWrongApplication = false;

            if (string.IsNullOrEmpty(GameApplicationPath))
                return;

            try
            {
                var fullPath = Path.GetFullPath(GameApplicationPath);
                if (File.Exists(fullPath))
                {
                    GameBinPath = Path.GetDirectoryName(fullPath);
                    IsValidApplication = ToolboxUpdater.ValidateSpaceEngineersInstall(GameBinPath);
                    IsWrongApplication = !IsValidApplication;
                }
            }
            catch { }
        }

        #endregion
    }
}