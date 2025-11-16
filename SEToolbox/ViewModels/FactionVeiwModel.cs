using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.World;
using Sandbox.Game.Entities.Cube;




/* public class FactionManager
{
    private MyWorldInfo world;

    public FactionManager(MyWorldInfo world)
    {
        this.world = world;
    }

    public void ViewFactions()
    {
        foreach (var faction in MySession.Static.Factions)
        {
            var factionInfo = faction.Value;
            System.Diagnostics.Debug.WriteLine($"Faction ID: {factionInfo.FactionId}, Name: {factionInfo.Name}, IsFriendly: {factionInfo.IsFriendly}");
        }
    }

    public void EditFaction(long factionId, string newName, bool isFriendly)
    {
        if (MySession.Static.Factions.TryGetValue(factionId, out var faction))
        {
            faction.Name = newName;
            faction.IsFriendly = isFriendly;
            System.Diagnostics.Debug.WriteLine($"Updated Faction ID: {factionId}, New Name: {newName}, IsFriendly: {isFriendly}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"Faction ID: {factionId} not found.");
        }
    }
}

public static class FactionInfo
{
    public static void ListFactionInformation()
    {
        var factions = MySession.Static.Factions;
        foreach (var faction in factions)
        {
            var factionId = faction.Key;
            var factionData = faction.Value;
            var factionName = factionData.Tag;
            VRageMath.Vector3 factionColor = factionData.IconColor;

            System.Diagnostics.Debug.WriteLine($"Faction ID: {factionId}, Name: {factionName}, Color: {factionColor}");
        }
    }
}*/