using Res = SEToolbox.Properties.Resources;

namespace SEToolbox.Models
{
    public class OwnerModel : BaseModel
    {
        #region Fields

        private string _name;

        private string _model;

        private long _playerId;

        private bool _isPlayer;

        #endregion

        #region Properties

        public string Name
        {
            get => _name;
            
            set => SetProperty(ref _name, nameof(Name), nameof(DisplayName));
        }


        public string Model
        {
            get => _model;
            set => SetProperty(ref _model, nameof(Model));
        }

        public long PlayerId
        {
            get => _playerId;
            
            set => SetProperty(ref _playerId, nameof(PlayerId));
        }

        public bool IsPlayer
        {
            get => _isPlayer;
            
            set => SetProperty(ref _isPlayer, nameof(IsPlayer), nameof(DisplayName));
        }

        public string DisplayName
        {
            get => _isPlayer || _playerId == 0 ? _name : $"{_name} ({Res.ClsCharacterDead})";
        }

        #endregion
    }
}
