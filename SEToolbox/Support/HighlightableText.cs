// using System;
// using System.Collections.Generic;
// using VRageMath;
// using SEToolbox.Interop;
// using SEToolbox.Models;
// using Sandbox.Game.World;
// using VRage.Game;

// namespace SEToolbox.Support
// {
//     public class HighlightableText
//     {
//
//         public string Text { get; set; }
//         public MyFactionMember SelectedMember { get; set; }

//         public void Highlight(HighlightInfo highlight)
//         {
//             if (highlight == null)
//                 throw new ArgumentNullException(nameof(highlight));

//             try
//             {
//                 if (!string.IsNullOrEmpty(Text))
//                     HighlightText(highlight);
//             }
//             catch (Exception ex) when (!(ex is ArgumentNullException || ex is NullReferenceException))
//             {
//                 SConsole.WriteLine($"An error occurred while highlighting: {ex.Message}");
//             }
//         }

//         private void HighlightText(HighlightInfo highlight)
//         {
//             if (highlight == null)
//                 throw new ArgumentNullException(nameof(highlight));

//             if (highlight.EnableHighlight)
//             {
//                 Text = $"<span style=\"color: {highlight.HighlightColor};\">{Text}</span>";
//             }
//         }
//         private Dictionary<long, MyFaction> _factions;

//         public void HighlightFactionReputation(long factionId)
//         {
//             if (_factions == null)
//                 throw new InvalidOperationException(nameof(_factions));

//             if (_factions.TryGetValue(factionId, out MyFaction faction) && faction != null)
//             {
//                 var reputation = faction.Reputation;
//                 var highlight = CreateHighlightInfo(reputation);
//                 new HighlightableText { Text = reputation.ToString() }.Highlight(highlight);
//             }
//         }

//         //public void HighlightMemberReputation(long factionId, long playerId)
//         //{
//         //    if (_factions == null)
//         //        throw new InvalidOperationException(nameof(_factions));

//         //    if (_factions.TryGetValue(factionId, out MyFaction faction) && faction != null && SelectedMember.PlayerId == playerId && SelectedMember.PlayerId == playerId)
//         //    {
//         //        var memberReputation = faction.GetMemberReputation(playerId);
//         //        var highlight = CreateHighlightInfo(memberReputation);
//         //        var reputationComponent = faction.GetMemberReputation(playerId);
//         //        if (!string.IsNullOrEmpty(reputationComponent))
//         //        {
//         //            new HighlightableText { Text = reputationComponent }.Highlight(highlight);
//         //        }
//         //    }
//         //}

//         private HighlightInfo CreateHighlightInfo(int reputation)
//         {
//             return new HighlightInfo
//             {
//                 HighlightColor = reputation > 0 ? Color.Green : reputation < 0 ? Color.Red : Color.Yellow,
//                 HighlightColorMask = ColorMask.All,
//                 EnableHighlight = true,
//                 PulseTimeSeconds = 0.5f,
//                 PulseScaleFactor = 1f,
//                 FadeInTimeSeconds = 0.5f,
//                 FadeOutTimeSeconds = 0.5f
//             };
//         }

//         public override bool Equals(object obj)
//         {
//             return obj is FactionModel model &&
//                    EqualityComparer<MyFaction>.Default.Equals(_selectedFaction, model.SelectedFaction);
//         }

//         public override int GetHashCode()
//         {
//             return HashCode.Combine(_factions);
//         }
//     }

//     public class HighlightInfo
//     {
//         public Color HighlightColor { get; set; }
//         public ColorMask HighlightColorMask { get; set; }
//         public bool EnableHighlight { get; set; }
//         public float PulseTimeSeconds { get; set; }
//         public float PulseScaleFactor { get; set; }
//         public float FadeInTimeSeconds { get; set; }
//         public float FadeOutTimeSeconds { get; set; }
//         public object Text { get; internal set; }
//     }
// }