using System;
using SEToolbox.Support;

namespace SEToolbox.Models
{
    public class SettingsModel : BaseModel
    {
        #region Fields

        private string _seBinPath;
        private string _customVoxelPath;
        private bool? _alwaysCheckForUpdates;
        private bool? _useCustomResource;
        private bool _isValid;

        #endregion

        #region Properties

        public string SEBinPath
        {
        get => _seBinPath;

            set
            {
                SetProperty(ref _seBinPath, nameof(SEBinPath));
                Validate();
            }
        }

        public string CustomVoxelPath
        {
            get => _customVoxelPath ?? string.Empty;
            set => SetProperty(ref _customVoxelPath, nameof(CustomVoxelPath), () => 
            	   Validate());

        }

        public bool? AlwaysCheckForUpdates
        {
            get => _alwaysCheckForUpdates;
            set => SetProperty(ref _alwaysCheckForUpdates, nameof(AlwaysCheckForUpdates), () => 
            	   Validate());


        }
        

        public bool? UseCustomResource
        {
            get => _useCustomResource;
            set => SetProperty(ref _useCustomResource, nameof(UseCustomResource), () => 
            	   Validate());
               
        }


        public bool IsValid
        {
            get => _isValid;

            private set => SetProperty(ref _isValid, nameof(IsValid));
        }

        #endregion

        #region Methods

        public void Load(string seBinPath, string customVoxelPath, bool? alwaysCheckForUpdates, bool? useCustomResource)
        {
            SEBinPath = seBinPath;
            CustomVoxelPath = customVoxelPath;
            AlwaysCheckForUpdates = alwaysCheckForUpdates;
            UseCustomResource = useCustomResource;
        }

        private void Validate()
        {
            IsValid = ToolboxUpdater.ValidateSpaceEngineersInstall(SEBinPath);
            // no need to check CustomVoxelPath, AlwaysCheckForUpdates, or UseCustomResource.
        }

        #endregion
    }
}
