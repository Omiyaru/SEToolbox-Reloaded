using Sandbox.Common.ObjectBuilders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.XPath;
using SEToolbox.Models;
using SEToolbox.Support;
using VRage.Game;
using static VRage.Game.MyObjectBuilder_Checkpoint;
using IDType = VRage.MyEntityIdentifier.ID_OBJECT_TYPE;
using Res = SEToolbox.Properties.Resources;

namespace SEToolbox.Interop
{
    public static class SpaceEngineersRepair
    {
        public static string RepairSandBox(WorldResource world)
        {
            StringBuilder str = new();
            bool statusNormal = true;
            bool missingFiles = false;
            bool saveAfterScan = false;


            // repair will use the WorldResource, thus it won't have access to the wrapper classes.
            // Any repair must be on the raw XML or raw serialized classes.

            var repairWorld = world;

            if (!repairWorld.LoadSector(out string sectorErrorInformation) ||
                !repairWorld.LoadCheckpoint(out string errorInformation))
            {
                statusNormal = false;
                Log.WriteLine(sectorErrorInformation);
                missingFiles = true;
            }

            var xDoc = repairWorld.LoadSectorXml();

            if (xDoc == null)
            {
                Log.WriteLine(Res.ClsRepairSectorBroken);
                Log.WriteLine(Res.ClsRepairUnableToRepair);
                missingFiles = true;
            }
            else
            {
                var nsManager = xDoc.BuildXmlNamespaceManager();
                var nav = xDoc.CreateNavigator();

                #region Update Group Control Format.

                var shipNodes = nav.Select("MyObjectBuilder_Sector/SectorObjects/MyObjectBuilder_EntityBase[@xsi:type='MyObjectBuilder_CubeGrid']", nsManager);
                while (shipNodes.MoveNext())
                {
                    var groupBlocksNode = shipNodes.Current.SelectSingleNode("BlockGroups/MyObjectBuilder_BlockGroup/Blocks", nsManager);
                    if (groupBlocksNode != null)
                    {
                        var entityIdNodes = groupBlocksNode.Select("long", nsManager);
                        List<XPathNavigator> removeNodes = [];
                        while (entityIdNodes.MoveNext())
                        {
                            var entityId = Convert.ToInt64(entityIdNodes.Current.Value);
                            var node = shipNodes.Current.SelectSingleNode(string.Format($"CubeBlocks/*[./EntityId='{nsManager}']", entityId));
                            if (node != null)
                            {
                                string x = node.ToValue<string>("Min/@x");
                                string y = node.ToValue<string>("Min/@y");
                                string z = node.ToValue<string>("Min/@z");

                                entityIdNodes.Current.InsertBefore(string.Format($"<Vector3I><X>{x}</X><Y>{y}</Y><Z>{z}</Z></Vector3I>"));
                                removeNodes.Add(entityIdNodes.Current.Clone());
                                Log.WriteLine(Res.ClsRepairReplacedBlockGroup);
                                saveAfterScan = true;
                                statusNormal = false;
                            }
                        }

                        foreach (var node in removeNodes)
                        {
                            node.DeleteSelf();
                        }
                    }
                }

                //<BlockGroups>
                //<MyObjectBuilder_BlockGroup>
                //    <Name>Open</Name>
                //    <Blocks>
                //    <long>-2287829012813351669</long>
                //    <long>-1828477283611406765</long>
                //    <long>73405095007807299</long>
                //    <long>-8785290580748247313</long>
                //    </Blocks>
                //</MyObjectBuilder_BlockGroup>
                //</BlockGroups>

                //<BlockGroups>
                //<MyObjectBuilder_BlockGroup>
                //    <Name>Open</Name>
                //    <Blocks>
                //    <Vector3I>
                //        <X>-1</X>
                //        <Y>2</Y>
                //        <Z>-4</Z>
                //    </Vector3I>
                //    <Vector3I>
                //        <X>-1</X>
                //        <Y>7</Y>
                //        <Z>2</Z>
                //    </Vector3I>
                //    <Vector3I>
                //        <X>-1</X>
                //        <Y>8</Y>
                //        <Z>-9</Z>
                //    </Vector3I>
                //    <Vector3I>
                //        <X>-1</X>
                //        <Y>13</Y>
                //        <Z>-3</Z>
                //    </Vector3I>
                //    </Blocks>
                //</MyObjectBuilder_BlockGroup>
                //</BlockGroups>

                if (saveAfterScan)
                {
                    repairWorld.SaveSectorXml(true, xDoc);
                    Log.WriteLine(Res.ClsRepairSavedChanges);
                }

                #endregion
            }

            repairWorld.LoadDefinitionsAndMods();

            if (!repairWorld.LoadSector(out errorInformation))
            {
                statusNormal = false;
                Log.WriteLine(errorInformation);
                missingFiles = true;
            }
            Dictionary<Object, string> loadErrors = new()
            {
                { repairWorld.Checkpoint, Res.ClsRepairCheckpointBroken },
                { repairWorld.SectorData, Res.ClsRepairSectorBroken }
            };
            foreach (var kvp in loadErrors)
            {

                if (loadErrors.Keys == null)
                {
                    statusNormal = false;
                    Log.WriteLine(loadErrors.Values);
                    Log.WriteLine(Res.ClsRepairUnableToRepair);
                    missingFiles = true;
                }


                if (!missingFiles)
                {
                    MyObjectBuilder_Character character;

                    saveAfterScan = false;
                    List<List<object>> identityCollections =
                    [
                        repairWorld.Checkpoint.Identities?.Cast<object>().ToList(),
                        repairWorld.Checkpoint.AllPlayersData?.Dictionary?.Values.Cast<object>().ToList()
                    ];
                    Dictionary<long, long> idReplacementTable = [];
                    if (repairWorld.Checkpoint.Identities != null)
                    {
                        foreach (var identity in identityCollections.FirstOrDefault())
                        {
                            if (identity is MyObjectBuilder_Identity || identity is KeyValuePair<PlayerId, MyObjectBuilder_Player>)
                            {
                                var identityId = identity is MyObjectBuilder_Identity id ? id.IdentityId : ((KeyValuePair<PlayerId, MyObjectBuilder_Player>)identity).Value.IdentityId;

                                if (!SpaceEngineersApi.ValidateEntityType(IDType.IDENTITY, identityId))
                                {

                                    identityId = MergeId(identityId, IDType.IDENTITY, ref idReplacementTable);

                                    statusNormal = false;
                                    Log.WriteLine(Res.ClsRepairFixedPlayerIdentity);
                                    saveAfterScan = true;
                                }
                            }
                        }


                        if (saveAfterScan)
                        {
                            repairWorld.SaveCheckPointAndSector(true);
                            Log.WriteLine(Res.ClsRepairSavedChanges);
                        }

                        if (world.SaveType == SaveWorldType.Local)
                        {
                            var player = repairWorld.FindPlayerCharacter();
                            var cockpit = repairWorld.FindPilotCharacter();
                            if (player == null)
                            {
                                statusNormal = false;
                                Log.WriteLine(Res.ClsRepairNoPlayerFound);

                                character = repairWorld.FindAstronautCharacter();
                                if (character == null)
                                {
                                    repairWorld.Checkpoint.ControlledObject = character.EntityId;
                                    repairWorld.Checkpoint.CameraController = MyCameraControllerEnum.Entity;
                                    repairWorld.Checkpoint.CameraEntity = character.EntityId;
                                    Log.WriteLine(Res.ClsRepairFoundSetPlayer);
                                    repairWorld.SaveCheckPointAndSector(true);
                                    Log.WriteLine(Res.ClsRepairSavedChanges);

                                }

                                else if (cockpit == null)
                                {

                                    repairWorld.Checkpoint.ControlledObject = cockpit.EntityId;
                                    repairWorld.Checkpoint.CameraController = MyCameraControllerEnum.ThirdPersonSpectator;
                                    repairWorld.Checkpoint.CameraEntity = 0;
                                    Log.WriteLine(Res.ClsRepairFoundSetPlayer);
                                    repairWorld.SaveCheckPointAndSector(true);
                                    Log.WriteLine(Res.ClsRepairSavedChanges);
                                }
                                saveAfterScan = false;
                            }

                            // Scan through all items.
                            foreach (var entity in repairWorld.SectorData.SectorObjects)
                            {
                                if (entity is MyObjectBuilder_CubeGrid cubeGrid)
                                {
                                    foreach (MyObjectBuilder_Cockpit c in cubeGrid.CubeBlocks.OfType<MyObjectBuilder_Cockpit>())
                                    {
                                        character = cockpit.GetHierarchyCharacters().FirstOrDefault();

                                        if (!SpaceEngineersResources.CharacterDefinitions.Any(c => c.Model == character?.CharacterModel || c.Name == character?.CharacterModel))
                                        {
                                            character.CharacterModel = Sandbox.Game.Entities.Character.MyCharacter.DefaultModel;
                                            statusNormal = false;
                                            Log.WriteLine(Res.ClsRepairFixedCharacterModel);
                                            saveAfterScan = true;

                                        }
                                    }
                                    foreach (var block in cubeGrid.CubeBlocks)
                                    {
                                        var definition = SpaceEngineersApi.GetCubeDefinition(block.GetType(), cubeGrid.GridSizeEnum, block.SubtypeName);
                                        if (definition == null)
                                        {
                                            Log.WriteLine($"Missing definition for block: {block.SubtypeName}");
                                            statusNormal = false;
                                            saveAfterScan = true;
                                        }
                                    }
                                }

                                character = entity as MyObjectBuilder_Character;

                                if (!SpaceEngineersResources.CharacterDefinitions.Any(c => c.Model == character?.CharacterModel || c.Name == character?.CharacterModel))
                                {
                                    character?.CharacterModel = Sandbox.Game.Entities.Character.MyCharacter.DefaultModel;
                                    statusNormal = false;
                                    Log.WriteLine(Res.ClsRepairFixedCharacterModel);
                                    saveAfterScan = true;
                                }

                                List<PlayerId> allPlayersDataKeys = [.. world.Checkpoint.AllPlayersData?.Dictionary.Keys];
                                for (int i = 0; i < allPlayersDataKeys.Count; i++)
                                {
                                    var key = allPlayersDataKeys[i];
                                    var item = world.Checkpoint.AllPlayersData.Dictionary[key];

                                    if (!SpaceEngineersResources.CharacterDefinitions.Any(c => c.Name == item.PlayerModel))
                                    {
                                        item.PlayerModel = SpaceEngineersResources.CharacterDefinitions[0].Name;
                                        statusNormal = false;
                                        Log.WriteLine(Res.ClsRepairFixedCharacterModel);
                                        saveAfterScan = true;
                                        // Validate and fix player ID

                                        if (item.PlayerId == 0)
                                        {
                                            item.PlayerId = SpaceEngineersApi.GenerateEntityId();
                                            world.Checkpoint.AllPlayers.Add(new PlayerItem(item.PlayerId, "Repair", false, item.SteamID, null));
                                            world.Checkpoint.AllPlayersData.Dictionary[key] = new MyObjectBuilder_PlayerItem
                                            {
                                                PlayerId = item.PlayerId,
                                                DisplayName = item.DisplayName,
                                                IsDead = false, //??in VRage.Game.ModAPI.IMyCharacter.IsDead or VRage.Game.ModAPI.IMyIdentity.IsDead
                                                SteamID = item.SteamID,
                                                IdentityId = item.IdentityId
                                            };
                                            statusNormal = false;
                                            Log.WriteLine("! Fixed corrupt or missing Player definition.");
                                            saveAfterScan = true;
                                        }
                                    }
                                }

                                if (saveAfterScan)
                                {
                                    repairWorld.SaveCheckPointAndSector(true);
                                    Log.WriteLine(Res.ClsRepairSavedChanges);
                                }
                            }

                            if (statusNormal)
                            {
                                Log.WriteLine(Res.ClsRepairNoIssues);
                            }

                        }
                    }
                }
            }
            return str.ToString();
        }

        internal class MyObjectBuilder_PlayerItem : MyObjectBuilder_Player
        {
            public bool IsDead { get; set; }
        }

        private static long MergeId(long currentId, IDType type, ref Dictionary<long, long> idReplacementTable)
        {
            if (currentId == 0)
            {
                return 0;
            }

            if (idReplacementTable.ContainsKey(currentId))
            {
                return idReplacementTable[currentId];
            }

            idReplacementTable[currentId] = SpaceEngineersApi.GenerateEntityId(type);
            return idReplacementTable[currentId];
        }

        public static void CheckAndRewriteSandboxFiles(WorldResource repairWorld, StringBuilder log)
        {
            try
            {
                bool hasErrors = false;
                // Ensure the WorldResource object is valid
                if (repairWorld == null)
                {
                    Log.WriteLine("Error: WorldResource is null. Cannot proceed.");
                }

                // Check and rewrite the Checkpoint (.sbc) file
                Log.WriteLine("Checking Checkpoint file...");
                if (repairWorld.Checkpoint != null)
                {
                    if (!ValidateCheckpoint(repairWorld?.Checkpoint, log))
                    {
                        hasErrors = true;
                        Log.WriteLine("Errors found in Checkpoint file. Rewriting...");
                        repairWorld.SaveCheckPoint(true); // Ensure backup is created
                        Log.WriteLine("Checkpoint file successfully rewritten.");
                    }


                    else
                    {
                        Log.WriteLine("Checkpoint file is missing or invalid.");
                        hasErrors = true;
                    }
                    // Check and rewrite the Sector (.sbs) file
                    log.AppendLine("Checking Sector file...");
                    if (!ValidateSector(repairWorld?.SectorData, log))
                    {
                        hasErrors = true;
                        Log.WriteLine("Errors found in Sector file. Rewriting...");
                        repairWorld.SaveSector(true); // Ensure backup is created
                        Log.WriteLine("Sector file successfully rewritten.");
                    }
                    else
                    {
                        Log.WriteLine("Sector file is valid.");
                    }


                    Log.WriteLine("Sector file is missing or invalid.");
                    hasErrors = true;


                    // Check and rewrite the Sector XML file
                    var xmlDoc = repairWorld.LoadSectorXml();

                    Log.WriteLine("Checking Sector XML file...");
                    var xDoc = System.Xml.Linq.XDocument.Parse(xmlDoc?.OuterXml); // Convert XmlDocument to XDocument
                    if (!ValidateSectorXml(xDoc, log))
                    {
                        hasErrors = true;
                        Log.WriteLine("Errors found in Sector XML file. Rewriting...");
                        repairWorld.SaveSectorXml(true, xmlDoc); // Save the original XmlDocument
                        Log.WriteLine("Sector XML file successfully rewritten.");
                    }
                    else
                    {
                        Log.WriteLine("Sector XML file is missing or invalid.");
                        hasErrors = true;
                    }
                    if (!hasErrors)
                    {
                        Log.WriteLine("All sandbox files are valid. No rewriting was necessary.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine($"An error occurred while checking and rewriting sandbox files: {ex.Message}");
            }
        }

        // Validate the Checkpoint (.sbc) file
        private static bool ValidateCheckpoint(MyObjectBuilder_Checkpoint checkpoint, StringBuilder log)
        {
            bool isValid = true;

            if (checkpoint.Identities == null || checkpoint.Identities.Count == 0)
            {
                Log.WriteLine("Checkpoint file: Missing or empty Identities.");
                isValid = false;
            }

            if (checkpoint.AllPlayersData == null || checkpoint.AllPlayersData.Dictionary.Count == 0)
            {
                Log.WriteLine("Checkpoint file: Missing or empty AllPlayersData.");
                isValid = false;
            }

            return isValid;
        }

        // Validate the Sector (.sbs) file
        private static bool ValidateSector(MyObjectBuilder_Sector sector, StringBuilder log)
        {
            bool isValid = true;

            if (sector.SectorObjects == null || sector.SectorObjects.Count == 0)
            {
                Log.WriteLine("Sector file: Missing or empty SectorObjects.");
                isValid = false;
            }

            return isValid;
        }

        // Validate the Sector XML file
        private static bool ValidateSectorXml(System.Xml.Linq.XDocument xDoc, StringBuilder log)
        {
            bool isValid = true;

            var root = xDoc.Root;
            if (root == null || root.Name.LocalName != nameof(MyObjectBuilder_Sector))
            {
                Log.WriteLine("Sector XML file: Invalid root element.");
                isValid = false;
            }

            return isValid;
        }
    }
}
