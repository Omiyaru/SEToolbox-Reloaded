using System.Collections.Generic;
using Sandbox.Game.World;

namespace SEToolbox.Interop
{
    public enum FactionData
    {
        FactionCount,
        Name,
        Tag,
        FactionId,
        FactionType,
        FactionIconId,
        Member,
        Members,
        MemberCount,
        Leader,
        Icon,
        IconColor,
        CustomColor,
        Description,
        FactionScore,
        ObjectivePercentageCompleted,
        PrivateInfo,
        FactionReputation = FactionProperties.Reputation,
        Relation = FactionProperties.ReputationValue,
        FactionTypes,
        Balance,
    }


    public enum FactionProperties
    {
        ObjectivePercentageCompleted,
        FactionScore,
        Reputation,
        ReputationValue

    }
     public enum FactionReputation
    {
        Owner,
        Neutral = Reputation.Neutral,
        Friendly = Reputation.Friend,
        Hostile = Reputation.Hostile,
    }
    public enum Reputation
    {
        Neutral,
        Friend,
        Hostile
    }

    public enum FactionControl
    {
        IsPlayerFaction,
        IsNPCFaction
    }

    public enum FactionMember
    {
        FactionId,
        PlayerId,
        PlayerName,
        PlayerColor,
        PlayerBalance,
        IsNpcMember

    }

    public enum FactionTypes
    {
        None,
        PlayerMade,
        Miner,
        Trader,
        Builder,
        Pirate,
        Military,
        Custom
    }

    public enum FactionIcons
    {
        None = -1,
        Default = 0,
        Icon1 = 1,
        Icon2 = 2,
        Icon3 = 3,
        Icon4 = 4,
        Icon5 = 5,
        Icon6 = 6,
        Icon7 = 7,
        Icon8 = 8,
        Icon9 = 9,
        Icon10 = 10
    }

    public enum ColorMask
    {
        All,
        Red,
        Green,
        Blue
    }

    public enum FactionReputationColor
    {
        None,
        Red,
        Green,
        Blue
    }

    public enum StationTypes
    {
        MiningStation,
        OrbitalStation,
        Outpost,
        SpaceStation
    }
}

