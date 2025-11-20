using System.Collections.Generic;
using System.Collections.ObjectModel;
using VRage.Game;
using VRageMath;
using VRage.Utils;
using Sandbox.Game.World;


namespace SEToolbox.Interfaces
{
    public interface IFactionBase
    {
        ObservableCollection<MyPlayer> Players { get; set; }
        ObservableCollection<MyFaction> Factions { get; set; }
        MyPlayer Player { get; set; }
        MyFaction SelectedFaction { get; set; }
        MyFactionMember Member { get; set; }

        MyFaction Faction { get; set; }
        string Name { get; set; }
        string Tag { get; set; }
        long FactionId { get; set; }

        Dictionary<long, MyFactionMember> Members { get; set; }
        long MemberId { get; set; } 
        int MemberCount { get; set; }
        string Leader { get; set; }
        MyStringId? FactionIcon { get; set; }
        string Description { get; set; }
        int Balance { get; set; }
        int FactionCount { get; set; }
        int FactionIconId { get; set; }
        int Score { get; set; }
        Vector3 IconColor { get; set; }
        Vector3 CustomColor { get; set; }
        long FounderId { get; set; }
        string PrivateInfo { get; set; }
        string FactionIconGroupId { get; set; }
        string FactionTypeString { get; set; }
        float ObjectivePercentageCompleted { get; set; }
        MyFactionMember SelectedMember { get; set; }

        Dictionary<long, MyPlayer> SelectedPlayers { get; set; }
        int Reputation { get; set; } //Friendly,Neutral,Hostile

        int FactionRelation { get; set; }  // value between 0 and 1
        int PlayerRelation { get; set; } // value between 0 and 1

        //order is (reputation, relation, player/faction),player/faction)
        public Dictionary<MyPlayer, (int, float, MyPlayer)> PlayerToPlayerRelationship { get; set; }
        
        public Dictionary<MyPlayer, (int, float, MyFaction)> FactionToPlayerRelationship { get; set; }
        
        public Dictionary< MyFaction,(int, float, MyFaction)> FactionToFactionRelationship { get; set; }
        


    }
}



