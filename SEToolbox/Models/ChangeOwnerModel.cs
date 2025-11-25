using System.Collections.ObjectModel;
using System.Linq;

using SEToolbox.Interop;
using Generic = System.Collections.Generic;

namespace SEToolbox.Models
{
    public class ChangeOwnerModel : BaseModel
    {
        #region Fields

        private ObservableCollection<OwnerModel> _playerList;
        private OwnerModel _selectedPlayer;
        private string _title;

        #endregion

        #region Ctor

        public ChangeOwnerModel()
        {
            _playerList = [];
        }

        #endregion

        #region Properties

        public ObservableCollection<OwnerModel> PlayerList
        {
            get => _playerList;
            set => SetProperty(ref _playerList, value, nameof(PlayerList));
        }

        public OwnerModel SelectedPlayer
        {
            get => _selectedPlayer;
            set => SetProperty(ref _selectedPlayer, value, nameof(SelectedPlayer));
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value, nameof(Title));
        }

        #endregion

        #region Methods

        public void Load(long initalOwner)
        {
            PlayerList.Clear();
            PlayerList.Add(new OwnerModel() { Name = "{None}", PlayerId = 0 });

            foreach (var identity in SpaceEngineersCore.WorldResource.Checkpoint.Identities.OrderBy(p => p.DisplayName))
            {
                if (SpaceEngineersCore.WorldResource.Checkpoint.AllPlayersData != null)
                {
                    var player = SpaceEngineersCore.WorldResource.Checkpoint.AllPlayersData.Dictionary.FirstOrDefault(kvp => kvp.Value.IdentityId == identity.PlayerId);
                    PlayerList.Add(new OwnerModel() { Name = identity.DisplayName, PlayerId = identity.PlayerId, Model = identity.Model, IsPlayer = player.Value != null });
                }
            }

            SelectedPlayer = PlayerList.FirstOrDefault(p => p.PlayerId == initalOwner);
        }

        #endregion
    }
}
