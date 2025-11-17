
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using SEToolbox.Interfaces;
using SEToolbox.Models;
using SEToolbox.Services;
using System;
using System.Linq;
using System.Windows.Input;
using VRage.Game;
using VRage.Utils;
using VRageMath;
using System.Collections.Generic;
using VRage.Game.Factions.Definitions;
using SEToolbox.Support;
using System.Security.Cryptography.X509Certificates;



namespace SEToolbox.ViewModels
{
    public class FactionViewModel : BaseViewModel
    {
        #region Fields

        public bool IsFactionCreated = false;
        private readonly FactionModel _factionModel;




        #endregion


        public ICommand[] Commands { get; }



        #region Commands

        public ICommand LoadFactionsCommand { get; set; }
        public ICommand SetAsLeaderCommand { get; set; }
        public ICommand SetFounderCommand { get; set; }
        public ICommand GetFactionCountCommand { get; set; }
        public ICommand GetIconsListCommand { get; set; }
        public ICommand CreateFactionCommand { get; }
        public ICommand DeleteFactionCommand { get; set; }
        public ICommand RenameFactionCommand { get; set; }
        public ICommand SetTagCommand { get; set; }
        public ICommand SetDescriptionCommand { get; set; }
        public ICommand SetIconCommand { get; set; }
        public ICommand SetFactionControlCommand { get; set; }
        // public ICommand SetReputationCommand { get; }
        // public ICommand SetBalanceCommand { get; }
        public ICommand GetBalanceCommand { get; set; }
        public ICommand GetPlayersCommand { get; }


        public Dictionary<long, MyFaction> Factions
        {
            get => _factionModel.Factions;
            set => SetProperty(_factionModel.Factions, value, nameof(Factions));
        }

        public MyFaction SelectedFaction
        {
            get => _factionModel.SelectedFaction;
            set => SetProperty(_factionModel.SelectedFaction, value, nameof(SelectedFaction));
        }

        public MyFactionMember SelectedMember
        {
            get => _factionModel.SelectedMember;
            set => SetProperty(_factionModel.SelectedMember, value, nameof(SelectedMember));
        }

        //public ICommand GetBalanceCommand { get; private set; }

        public DelegateCommand AddPlayerToFactionCommand { get; private set; }

        public FactionViewModel(FactionModel factionModel, BaseViewModel ownerViewModel)
            : base(ownerViewModel)
        {
            _factionModel = factionModel ?? throw new ArgumentNullException(nameof(factionModel));
            Factions = [];
            _ = new MyPlayerCollection();
            Commands =
            [

                LoadFactionsCommand = new DelegateCommand(LoadFactionsExecuted, LoadFactionsCanExecute),
                GetFactionCountCommand = new DelegateCommand(GetFactionCountExecuted, GetFactionCountCanExecute),
                SetFounderCommand = new DelegateCommand(SetFounderExecuted, SetFounderCanExecute),
                SetAsLeaderCommand = new DelegateCommand(SetAsLeaderExecuted, SetAsLeaderCanExecute),
                SetTagCommand = new DelegateCommand(SetTagExecuted, SetTagCanExecute),
                SetDescriptionCommand = new DelegateCommand(SetDescriptionExecuted, SetDescriptionCanExecute),
                SetIconCommand = new DelegateCommand(SetIconExecuted, SetIconCanExecute),
                SetFactionControlCommand = new DelegateCommand(SetFactionControl, SetFactionControlCanExecute),
                AddPlayerToFactionCommand = new DelegateCommand(AddPlayerToFactionExecuted, AddPlayerToFactionCanExecute),
                GetIconsListCommand = new DelegateCommand(GetIconsListExecuted, GetIconsListCanExecute),
                CreateFactionCommand = new DelegateCommand(CreateFactionExecuted, CreateFactionCanExecute),
                DeleteFactionCommand = new DelegateCommand(DeleteFactionExecuted, DeleteFactionCanExecute),
                RenameFactionCommand = new DelegateCommand(RenameFactionExecuted, RenameFactionCanExecute),
                //GetFactionInfoCommand = new DelegateCommand(GetFactionInfoExecuted, GetFactionInfoCanExecute),
                GetBalanceCommand = new DelegateCommand(GetBalanceExecuted, GetBalanceCanExecute),

            ];
        }

        private readonly MyFaction _faction;

        public FactionViewModel(MyFaction faction, BaseViewModel ownerViewModel)
            : base(ownerViewModel)
        {
            _faction = faction ?? throw new ArgumentNullException(nameof(faction));
        }

        public string Tag => _faction.Tag;
        public string Name => _faction.Name;
        public long FactionId => _faction.FactionId;
        public long FounderId => _faction.FounderId;

        public string Description => _faction.Description;
        public MyStringId? FactionIcon => _faction.FactionIcon;
        public int Score => _faction.Score;
        public float ObjectivePercentageCompleted => _faction.ObjectivePercentageCompleted;
        public string FactionTypeString => _faction.FactionTypeString;
        public Vector3 CustomColor => _faction.CustomColor;
        public Vector3 IconColor => _faction.IconColor;
        public int MemberCount => _faction.Members.Count;



        // If you want to allow editing, implement setters and raise OnPropertyChanged.

        private bool SetFounderCanExecute() => _factionModel.SelectedFaction != null;

        private void SetFounderExecuted()
        {
            if (_factionModel.SelectedFaction is { FounderId: >= 0 })
            {
                _factionModel.SetFounder(_factionModel.SelectedFaction.FactionId, _factionModel.SelectedFaction.FounderId, _factionModel.SelectedMember);
            }
        }



        private bool SetTagCanExecute() => _factionModel.SelectedFaction != null;

        private void SetTagExecuted()
        {
            long factionId = _factionModel.SelectedFaction.FactionId;
            string tag = "";

            if (_factionModel != null)
            {
                _factionModel.SetTag(factionId, tag);
                if (_factionModel.SelectedFaction != null)
                {
                    _factionModel.SelectedFaction.Tag = tag;
                    OnPropertyChanged(nameof(_factionModel.SelectedFaction.Tag));
                }
            }
        }

        private bool CreateFactionCanExecute() => true;

        private bool SetAsLeaderCanExecute() => _factionModel.SelectedFaction != null;

        private bool AddPlayerToFactionCanExecute() => _factionModel.SelectedFaction != null;

        private void SetAsLeaderExecuted()
        {
            if (_factionModel.SelectedFaction != null)
            {
                MyFactionMember? member = _factionModel.SelectedMember;
                if (member == null)
                {
                    throw new InvalidOperationException("No member selected to set as leader.");
                }

                if (member.Value.IsLeader == false)
                {
                    _factionModel.SetAsLeader(_factionModel.SelectedFaction.FactionId, member.Value.PlayerId);
                    OnPropertyChanged(nameof(_factionModel.Members));
                }
            }
        }

        private bool LoadFactionsCanExecute() => true;

        private void LoadFactionsExecuted()
        {
            _factionModel.LoadFactions();
            OnPropertyChanged(nameof(Factions));
        }

        // private bool SetBalanceCanExecute()
        // {
        //     // Allow if a faction is selected
        //     return _factionModel.SelectedFaction != null;
        // }

        // private void SetBalanceExecuted()
        // {
        //     if (_factionModel.SelectedFaction != null)
        //     {

        //         _factionModel.Balance = _factionModel.SetBalance(_factionModel.SelectedFaction.FactionId, _factionModel.Balance);
        //         OnPropertyChanged(nameof(_factionModel.Balance));
        //     }
        // }

        private bool SetIconCanExecute() => _factionModel.SelectedFaction != null;

        private void SetIconExecuted()
        {
            if (_factionModel.SelectedFaction != null)
            {
                _factionModel.FactionIcon = MyStringId.GetOrCompute("DefaultIcon");
                OnPropertyChanged(nameof(_factionModel.FactionIcon));
            }
        }

        private bool SetDescriptionCanExecute() => _factionModel.SelectedFaction != null;

        private void SetDescriptionExecuted()
        {
            long factionId = 0;
            string newDescription = string.Empty;
            if (_factionModel.SelectedFaction != null)
            {
                _factionModel.SetDescription(factionId, newDescription);
                _factionModel.Description = newDescription;
                OnPropertyChanged(nameof(_factionModel.Description));
            }
        }

        private bool DeleteFactionCanExecute() => true;

        private void DeleteFactionExecuted()
        {
            if (_factionModel.SelectedFaction != null)
            {
                _factionModel.Factions.Remove(_factionModel.SelectedFaction.FactionId);
                _factionModel.IsFactionSelected = false;
                _factionModel.LoadFactions();
            }
        }



        private bool RenameFactionCanExecute() => _factionModel.SelectedFaction != null;

        private void RenameFactionExecuted()
        {
            if (_factionModel.SelectedFaction != null)
            {
                if (!string.IsNullOrEmpty(_factionModel.Name))
                {
                    try
                    {
                        _factionModel.SetName(_factionModel.SelectedFaction.FactionId, _factionModel.Name);
                        OnPropertyChanged(nameof(_factionModel.Name));
                    }
                    catch (Exception ex)
                    {
                        SConsole.WriteLine($"Error updating faction name: {ex.Message}");
                    }
                }
                OnPropertyChanged(nameof(_factionModel.Name));
            }
        }

        private bool GetBalanceCanExecute() => _factionModel.SelectedFaction != null;

        private void GetBalanceExecuted()
        {
            if (_factionModel.SelectedFaction != null)
            {
                _ = _factionModel.Balance;
                OnPropertyChanged(nameof(_factionModel.Balance));
            }
        }

        // private bool SetReputationCanExecute()
        // {
        //     return _factionModel.SelectedFaction != null;
        // }

        // private void SetReputationExecuted()
        // {
        //     if (_factionModel.SelectedFaction != null)
        //     {
        //         _factionModel.ReputationValue = _factionModel.SetReputation(_factionModel.SelectedFaction.FactionId, _factionModel.ReputationValue);

        //         OnPropertyChanged(nameof(_factionModel.ReputationValue));
        //     }
        // }


        private void SetFactionControl()
        {
            if (_factionModel.SelectedFaction != null)
            {
                _factionModel.IsNpcFaction = !_factionModel.IsNpcFaction;
                OnPropertyChanged(nameof(_factionModel.IsNpcFaction));
            }
        }

        private bool SetFactionControlCanExecute() => _factionModel.SelectedFaction != null;

        private bool GetIconsListCanExecute() => true;

        private void GetIconsListExecuted()
        {
            List<MyFactionIconsDefinition> icons = [];
            _factionModel.GetFactionIcons(icons);
        }

        private bool GetFactionCountCanExecute() => true;

        private void GetFactionCountExecuted()
        {
            _ = _factionModel.FactionCount;
        }

        private void AddPlayerToFactionExecuted()
        {
            if (_factionModel.SelectedFaction != null)
            {
                FactionModel factionModel = new(_factionModel.SelectedFaction);
                _factionModel.AddPlayersToFaction(factionModel, _factionModel.SelectedPlayers);
                OnPropertyChanged(nameof(_factionModel.Members));
            }
        }

        private void CreateFactionExecuted()
        {

            if (IsFactionCreated)
            {

                long factionId = 0;//what is the correct way to get a new faction ID?
                string tag = _factionModel.Tag;
                string name = _factionModel.Name;
                string description = _factionModel.Description;
                string privateInfo = _factionModel.PrivateInfo;
                long creatorId = _factionModel.FounderId;
                string factionTypeString = _factionModel.FactionTypes.FirstOrDefault().Key.ToString() ?? "PlayerMade";
                Vector3 customColor = _factionModel.CustomColor;
                Vector3 factionIconColor = _factionModel.IconColor;

                MyStringId? factionIcon = _factionModel.FactionIcon;
                string factionIconString = factionIcon.ToString();
                int score = 0;
                float objectivePercentageCompleted = 0f;


                _factionModel.CreateFaction(
                     factionId, tag, name, description, privateInfo, creatorId, factionTypeString, customColor, factionIconColor, factionIconString, null, score, objectivePercentageCompleted);
                IsFactionCreated = true;
                _factionModel.IsFactionSelected = true;


                _factionModel.LoadFactions();
                OnPropertyChanged(nameof(Factions));
            }

        }

    }
}
#endregion