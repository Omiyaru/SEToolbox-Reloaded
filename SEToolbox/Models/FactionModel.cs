using Sandbox.Game.World;
using SEToolbox.Interfaces;
using SEToolbox.Interop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using VRage.Game;
using VRage.Utils;
using VRageMath;

using SVector3 = VRage.SerializableVector3;
using SGW = Sandbox.Game.World;
using VRage.GameServices;
using VRage.Game.Factions.Definitions;
using SEToolbox.Support;

using VRage;
using System.Collections.Concurrent;

namespace SEToolbox.Models
{
   public class FactionModel : BaseModel
{
   
        #region Fields

        private  Dictionary<long, MyFaction> _factions = [];
        private  Dictionary<long, MyFactionMember> _members = [];
        private  Dictionary<long, MyPlayer> _selectedPlayers = [];


        private string _factionDescription = string.Empty;
        private int _balance;
        private string _tag;
        private long _member;
        private string _factionPrivateInfo = string.Empty;
        private string _factionName = string.Empty;
        private MyStringId? _factionIcon;
        private int _score;
        private int _memberCount;
        private string _factionLeader = string.Empty;
        private string _factionTypeString = string.Empty;
        private long _founderId;
        private Vector3 _iconColor;
        private Vector3 _customColor;
        private float _objectivePercentageCompleted;
        //private int _hashCode;
        private MyFactionMember _selectedMember;
        private MyFaction _selectedFaction;
        private MyStationTypeEnum _stationType;
        private bool _isNpcFaction;
        private long _factionId;
        private SVector3 _factionColor;
        private string _factionType;
        private ObservableCollection<MyPlayer> _players;
        private MyStringId? _factionIconString;
        private string _privateInfo;
        private int _factionIconId;
        private long _stationId;
       // private float _reputationValue;
        private string _stationName;
        private IFactionBase _selectedPlayer;
        private string _leader;
        private MyFaction _faction;
        private int _factionCount;
        private string _selectedFactionType;
        private bool? _isFactionSelected;
        private List<MyFactionIconsDefinition> _icons;

        private ConcurrentDictionary<MyPlayer, List<Tuple<int, float, MyPlayer>>> _playerToPlayerRelationship;
        private ConcurrentDictionary<MyPlayer, List<Tuple<int, float, MyFaction>>> _factionToPlayerRelationship;
        private ConcurrentDictionary<MyFaction, List<Tuple<int, float, MyFaction>>> _factionToFactionRelationship;

        private int _reputation;
        private int _factionReputation;
       // private float FactionRelation;
        private int _playerReputation;
        // private float PlayerRelation;

        #endregion

        #region Properties


        public List<MyFactionIconsDefinition> Icons  
        { 
            get => _icons; 
            set => SetProperty(ref _icons, value); }
        public Dictionary<long, MyFaction> Factions  
        { 
            get => _factions; 
            set => SetProperty(ref _factions, value); }
        public Dictionary<long, MyFactionMember> Members  
        { 
            get => _members; 
            set => SetProperty(ref _members, value); }
        public Dictionary<long, MyPlayer> SelectedPlayers  
        { 
            get => _selectedPlayers; 
            set => SetProperty(ref _selectedPlayers, value); }
        public Dictionary<long, MyFactionMember> SelectedMembers  
        { 
            get => _selectedMembers; 
            set => SetProperty(ref _selectedMembers, value); }
        public ConcurrentDictionary<MyFaction, List<Tuple<int, float, MyFaction>>> FactionToFactionRelationship;
        public ConcurrentDictionary<MyFaction, List<Tuple<int, float, MyPlayer>>> FactionToPlayerRelationship;
        public ConcurrentDictionary<MyPlayer, List<Tuple<int, float, MyPlayer>>> PlayerToPlayerRelationship;
        private WorkshopId? _factionIconWorkshopId;
        private Dictionary<long, MyFactionMember> _selectedMembers;

        public string Name  
        { 
            get => _factionName; 
            set => SetProperty(ref _factionName, nameof(Name));
        }
        public string Tag  
        { 
            get => _tag; 
            set => SetProperty(ref _tag, nameof(Tag));
        }

        public string FactionTypeString  
        { 
            get => _factionTypeString; 
            set => SetProperty(ref _factionTypeString, nameof(FactionTypeString));
        }
        public string Description  
        { 
            get => _factionDescription; 
            set => SetProperty(ref _factionDescription, nameof(Description));
        }
        public string PrivateInfo  
        { 
            get => _privateInfo; 
            set => SetProperty(ref _privateInfo, nameof(PrivateInfo));
        }
        public WorkshopId? FactionIconWorkshopId  
        { 
            get => _factionIconWorkshopId; 
            set => SetProperty(ref _factionIconWorkshopId, nameof(FactionIconWorkshopId));
        }
        public string FactionLeader  
        { 
            get => _factionLeader; 
            set => SetProperty(ref _factionLeader, nameof(FactionLeader));
        }
        public MyStringId? FactionIcon  
        { 
            get => _factionIcon; 
            set => SetProperty(ref _factionIcon, nameof(FactionIcon));
        }
        public int Score  
        { 
            get => _score; 
            set => SetProperty(ref _score, nameof(Score));
        }
        public string FactionType  
        { 
            get => _factionType; 
            set => SetProperty(ref _factionType, nameof(FactionType));
        }
        public long FactionId  
        { 
            get => _factionId; 
            set => SetProperty(ref _factionId, nameof(FactionId));
        }
        public int Balance  
        { 
            get => _balance; 
            set => SetProperty(ref _balance, nameof(Balance));
        }
        public MyStringId? FactionIconString  
        { 
            get => _factionIconString; 
            set => SetProperty(ref _factionIconString, nameof(FactionIconString));
        }
        public string FactionPrivateInfo  
        { 
            get => _factionPrivateInfo; 
            set => SetProperty(ref _factionPrivateInfo, nameof(FactionPrivateInfo));
        }
        public SVector3 FactionColor  
        { 
            get => _factionColor; 
            set => SetProperty(ref _factionColor, nameof(FactionColor));
        }
        public Vector3 IconColor  
        { 
            get => _iconColor; 
            set => SetProperty(ref _iconColor, nameof(IconColor));
        }
        public Vector3 CustomColor  
        { 
            get => _customColor; 
            set => SetProperty(ref _customColor, nameof(CustomColor));
        }
        public long FounderId  
        { 
            get => _founderId; 
            set => SetProperty(ref _founderId, nameof(FounderId));
        }
        public int MemberCount  
        { 
            get => _memberCount; 
            set => SetProperty(ref _memberCount, nameof(MemberCount));
        }
        public float ObjectivePercentageCompleted  
        { 
            get => _objectivePercentageCompleted; 
            set => SetProperty(ref _objectivePercentageCompleted, nameof(ObjectivePercentageCompleted));
        }

        public long Member  
        { 
            get => _member; 
            set => SetProperty(ref _member, nameof(Member));
        } 
        public int FactionIconId  
        { 
            get => _factionIconId; 
            set => SetProperty(ref _factionIconId, nameof(FactionIconId));
        } 
         public int FactionCount 
         { 
            get => _factionCount; 
            set => SetProperty(ref _factionCount, nameof(FactionCount));
        } 
        public bool IsNpcFaction  
        { 
            get => _isNpcFaction; 
            set => SetProperty(ref _isNpcFaction, nameof(IsNpcFaction));
        }
        public MyFaction SelectedFaction  
        { 
            get => _selectedFaction; 
            set => SetProperty(ref _selectedFaction, nameof(SelectedFaction));
        }
        public MyFactionMember SelectedMember  
        { 
            get => _selectedMember; 
            set => SetProperty(ref _selectedMember, nameof(SelectedMember));
        }
        public MyFaction Faction  
        { 
            get => _faction; 
            set => SetProperty(ref _faction, nameof(Faction));
        }
        public IFactionBase SelectedPlayer  
        { 
            get => _selectedPlayer; 
            set => SetProperty(ref _selectedPlayer, nameof(SelectedPlayer));
        }
        public int Reputation  
        { 
            get => _reputation; 
            set => SetProperty(ref _reputation, nameof(Reputation));
        }
        public int FactionReputation  
        { 
            get => _factionReputation; 
            set => SetProperty(ref _factionReputation, nameof(FactionReputation));
        } // holder for when the factions reputation Reputation = FactionReputation
        public int PlayerReputation  
        { 
            get => _playerReputation; 
            set => SetProperty(ref _playerReputation, nameof(PlayerReputation));
        } //this is a holder for the players reputation   Reputation = PlayerReputation
         public long StationId  
        { 
            get => _stationId; 
            set => SetProperty(ref _stationId, nameof(StationId));
        }
        public string StationName  
        { 
            get => _stationName; 
            set => SetProperty(ref _stationName, nameof(StationName));
        }
        public MyStationTypeEnum StationType  
        { 
            get => _stationType; 
            set => SetProperty(ref _stationType, nameof(StationType));
        }
        public ObservableCollection<MyPlayer> Players  
        { 
            get => _players; 
            set => SetProperty(ref _players, nameof(Players));
        }
        public bool? IsFactionSelected  
        { 
            get => _isFactionSelected; 
            set => SetProperty(ref _isFactionSelected, nameof(IsFactionSelected));
        }

        public string Leader  
        { 
            get => _leader; 
            set => SetProperty(ref _leader, nameof(Leader));
        }
        public string SelectedFactionType  
        { 
            get => _selectedFactionType; 
            set => SetProperty(ref _selectedFactionType, nameof(SelectedFactionType));
        }

        #endregion

        #region Constructor
        

        public FactionModel(MyFaction selectedFaction)
        {
            if (selectedFaction == null)
                throw new ArgumentNullException(nameof(selectedFaction));

            _factionIcon = selectedFaction.FactionIcon;
            _factionId = selectedFaction.FactionId;
            _factionIconString = selectedFaction.FactionIcon;
            _tag = selectedFaction.Tag;
            _factionName = selectedFaction.Name;
            _factionLeader = selectedFaction.Members.FirstOrDefault(m => m.Value.IsLeader).Value.PlayerId.ToString() ?? "No Leader";
            _factionDescription = selectedFaction.Description;
            _factionPrivateInfo = selectedFaction.PrivateInfo;
            _memberCount = selectedFaction.Members.Count;
            _founderId = selectedFaction.FounderId;
            _iconColor = selectedFaction.IconColor;
            _customColor = selectedFaction.CustomColor;
            _factionTypeString = selectedFaction.FactionTypeString;
            _score = selectedFaction.Score;
            _objectivePercentageCompleted = selectedFaction.ObjectivePercentageCompleted;

            
            

           _factions.Add(selectedFaction.FactionId, selectedFaction);
        }
        public FactionModel()
        {
            LoadFactions();
           _players = [];
        }
      

        #endregion

        #region Methods

        public void LoadFactions()
        {
           _factions.Clear();

            var sessionFactions = SGW.MySession.Static?.Factions;
            if (sessionFactions != null)
            {
                foreach (var factionPair in sessionFactions)
                {
                    if (!_factions.ContainsKey(factionPair.Key))
                    {
                       _factions.Add(factionPair.Key, factionPair.Value);
                    }
                }
            }
        }

        public static List<MyFactionMember> GetMembers(List<MyFaction> factions, long factionId)
        {
            var faction = factions.FirstOrDefault(f => f?.FactionId == factionId);
            if (faction == null)
                return [];

            return [.. faction.Members.Values];
        }

        public List<string> AddPlayersToFaction(FactionModel selectedFaction, Dictionary<long, MyPlayer> selectedPlayers)
        {
            var notInFaction = selectedPlayers.Keys.Except(selectedFaction.Members.Keys).ToList();
            var addedPlayerNames = new List<string>();

            foreach (var identityId in notInFaction)
            {
                _selectedPlayers[identityId] = selectedPlayers[identityId];
                selectedFaction.Members[identityId] = CreateMyFactionMember(identityId);
                addedPlayerNames.Add(selectedPlayers[identityId].Identity?.DisplayName ?? "Unknown");
            }

            return addedPlayerNames;
        }

        public void RemovePlayersFromFaction(FactionModel selectedFaction, Dictionary<long, MyPlayer> selectedPlayers)
        {
            var notInFaction = selectedFaction.Members.Keys.Except(selectedPlayers.Keys).ToList();
            foreach (var identityId in notInFaction)
            {
                _selectedPlayers.Remove(identityId);
                selectedFaction.Members.Remove(identityId);
            }
        }

        internal void SetTag(long factionId, string newTag)
        {
            if (_factions.TryGetValue(factionId, out MyFaction faction))
            {
                faction.Tag = newTag;
            }
        }

        internal void SetName(long factionId, string newName)
        {
            if (_factions.TryGetValue(factionId, out MyFaction faction))
            {
                faction.Name = newName;
            }
        }

        internal void SetDescription(long factionId, string newDescription)
        {
            if (_factions.TryGetValue(factionId, out MyFaction faction))
            {
                faction.Description = newDescription;
            }
        }


        public List<MyFactionIconsDefinition> GetFactionIcons(List<MyFactionIconsDefinition> icons)
        {
            _icons = [];

            var definitions = typeof(MyFactionIconsDefinition).Assembly.GetTypes()
                                                              .Where(t => t.IsSubclassOf(typeof(MyFactionIconsDefinition)) && 
                                                                          t.GetConstructor(Type.EmptyTypes) != null)
                                                              .Select(t => (MyFactionIconsDefinition)Activator.CreateInstance(t))
                                                              .ToList() ?? throw new NullReferenceException("Definitions is null");
            icons.AddRange(definitions);

            return icons;
        }

        internal void SetFactionIcon(long itemId, string icon, List<MyFactionIconsDefinition> factionIcons)
        {
            if (_factions == null)
            {
                throw new NullReferenceException("_factions is null");
            }

            if (_factions.TryGetValue(itemId, out MyFaction faction))
            {
                try
                {
                    var iconId = factionIcons.FirstOrDefault(i => i.Icons.Contains(icon))?.Id ?? default(MyDefinitionId?);
                    faction.FactionIcon = ProcessStringIds.ProcessIds(iconId.HasValue ? iconId.ToString() : icon);
                    FactionIcon = faction.FactionIcon;
                }
                catch (Exception ex)
                {
                    throw new Exception("Could not set faction icon", ex);
                }
            }
        }

        internal void SetFactionColors(long factionId, (string icon, Vector3 iconColor, Vector3 customColor) colors)
        {
            if (_factions.TryGetValue(factionId, out MyFaction faction))
            {
                faction.FactionIcon = ProcessStringIds.ProcessIds(colors.icon);
                FactionIcon = faction.FactionIcon;
                faction.IconColor = colors.iconColor;
                faction.CustomColor = colors.customColor;
            }
        }

        public int GetFactionCount(long factionId, int factionCount)
        {
            if (_factions == null || _factions.Count == 0)
                return 0;

            if (factionId < 0)
            {
                return !_factions.ContainsKey(factionId) ? factionCount + 1 : factionCount;
            }

            if (_factions.TryGetValue(factionId, out MyFaction faction))
            {
                return faction.Members.Count;
            }

            return 0;
        }

        public void SetFounder(long factionId, long founderId, MyFactionMember newFounder)
        {
            if (_factions.TryGetValue(factionId, out MyFaction faction))
            {
                if (faction.FounderId == founderId)
                {
                    return;
                }

                newFounder.IsFounder = true;
                FounderId = founderId;

                LoadFactions();
            }
        }

        public void SetAsLeader(long factionId, long leaderId)
        {
            if (factionId < 0 || leaderId < 0)
                return;

            if (_factions.TryGetValue(factionId, out MyFaction faction) &&
                faction.Members.TryGetValue(leaderId, out MyFactionMember member))
            {
                member.IsLeader = true;
                LoadFactions();
            }
        }

        //public void Update factions list insteadd of reloading all factions andd members/

        public void SetFactionScore(long factionId, int score)
        {
            if (_factions.TryGetValue(factionId, out MyFaction faction))
            {
                faction.Score = score;
            }
        }
        //todo: implement a way to edit the reputation /reputation value
        public void SetObjectivePercentageCompleted(long factionId, float objectivePercentageCompleted)
        {
            if (_factions.TryGetValue(factionId, out MyFaction faction))
            {
                faction.ObjectivePercentageCompleted = objectivePercentageCompleted;
            }
        }

        public void CreateFaction(long factionId, string tag, string name, string description,
            string privateInfo, long founderId, string factionType,
            Vector3 customColor, Vector3 factionIconColor, string factionIcon = "",
            WorkshopId? factionWorkshopId = null, int score = 0, float objectivePercentageCompleted = 0f)
        {
            if (factionId == 0 || string.IsNullOrWhiteSpace(tag) || string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(privateInfo) ||
                founderId == 0 || string.IsNullOrWhiteSpace(factionType) || customColor == Vector3.Zero ||
                factionIconColor == Vector3.Zero || string.IsNullOrWhiteSpace(factionIcon) ||
                factionWorkshopId == null || score < 0 || objectivePercentageCompleted < 0 || objectivePercentageCompleted > 1)
            {
                throw new ArgumentException("Invalid faction parameters", nameof(factionId));
            }

            if (!_factions.TryGetValue(factionId, out MyFaction faction))
            {
                faction = new MyFaction(factionId, tag, name, description, privateInfo,
                    founderId, factionType, customColor, factionIconColor,
                    factionIcon, factionWorkshopId, score, objectivePercentageCompleted);
               _factions[factionId] = faction;
            }
            else
            {
                faction.Tag = tag;
                faction.Name = name;
                faction.Description = description;
                faction.PrivateInfo = privateInfo;
                //faction.FounderId = founderId;
                faction.FactionTypeString = factionType;
                faction.CustomColor = customColor;
                faction.IconColor = factionIconColor;
                faction.FactionIcon = MyStringId.GetOrCompute(factionIcon);
                //faction.WorkshopId = factionWorkshopId;
                faction.Score = score;
                faction.ObjectivePercentageCompleted = objectivePercentageCompleted;
            }
        }

        public readonly Dictionary<FactionTypes, string> FactionTypes = new(
             new Dictionary<FactionTypes, string>
             {
                    { Interop.FactionTypes.None, "None" },
                    { Interop.FactionTypes.PlayerMade, "PlayerMade" },
                    { Interop.FactionTypes.Miner, "Miner" },
                    { Interop.FactionTypes.Trader, "Trader" },
                    { Interop.FactionTypes.Builder, "Builder" },
                    { Interop.FactionTypes.Pirate, "Pirate" },
                    { Interop.FactionTypes.Military, "Military" },
                    { Interop.FactionTypes.Custom, "Custom" } // Define custom faction type here
             }
         );
         public IEnumerable<KeyValuePair<FactionTypes, string>> FactionTypeList => FactionTypes;

        // Implement custom FactionTypes when factionType = Custom
        // public void SetCustomFactionType(long factionId, string customFactionType)
        // {
        //     if (_factions.TryGetValue(factionId, out MyFaction faction))
        //     {
        //         if (faction.FactionTypeString == FactionTypes[Interop.FactionTypes.Custom])
        //         {
        //             faction.FactionTypeString = customFactionType;
        //         }
        //     }
        // }


        public static void SetFactionType(MyFaction faction, string selectedFaction, ReadOnlyDictionary<FactionTypes, string> factionTypes, MyFaction selectedFactionType)
        {
            if (faction == null || string.IsNullOrWhiteSpace(selectedFaction) || factionTypes == null)
            {
                return;
            }

            if (selectedFactionType != null && factionTypes.Any(ft => ft.Value == selectedFaction))
            {
                var factionType = factionTypes.First(ft => ft.Value == selectedFaction).Key;
                faction.FactionTypeString = factionType.ToString();
                faction.FactionTypeString = selectedFactionType.FactionTypeString;
            }
        }


        public void ListFactionInfoStations(Dictionary<long, MyStation> stations , MyStation station)
        {
            KeyValuePair<long, MyFaction> factionPair = _factions.FirstOrDefault(static x => x.Value.Name.Equals(SGW.MySession.Static.Factions));
            if (!factionPair.Equals(default(KeyValuePair<long, MyFaction>)))
            {

                MyFaction faction = factionPair.Value;
                {

                    if (station != null)
                    {
                        StationId = station.FactionId;
                        StationName = station.GetType().ToString();

                    }
                }
            }
        }

        public override bool Equals(object obj)
        {
            return obj is FactionModel model &&
                   EqualityComparer<Dictionary<long, MyFaction>>.Default.Equals(_factions, model._factions);
        }

        public override int GetHashCode()
        {
            return _factions != null ? _factions.GetHashCode() : 0;
        }

        private MyFactionMember CreateMyFactionMember(long identityId)
        {
            return new MyFactionMember
            {
                PlayerId = identityId,
                IsLeader = false,
                IsFounder = false
            };
        }

        #endregion
    }
}
