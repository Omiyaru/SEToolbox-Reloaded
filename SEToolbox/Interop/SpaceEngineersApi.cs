using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Sandbox.Definitions;
using SEToolbox.Support;
using VRage;
using VRage.FileSystem;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using Res = SEToolbox.Properties.Resources;
using MOBSerializerKeen = VRage.ObjectBuilders.Private.MyObjectBuilderSerializerKeen;
using SEResources = SEToolbox.Interop.SpaceEngineersResources;

namespace SEToolbox.Interop
{
    /// <summary>
    /// Helper api for accessing and interacting with Space Engineers content.
    /// </summary>
    public static class SpaceEngineersApi
    {
        #region Serializers

        public static T TryReadSpaceEngineersFile<T>(Stream stream) where T : MyObjectBuilder_Base
        {
            MOBSerializerKeen.DeserializeXML(stream, out T outObject);
            return outObject;
        }

        /// <returns>True if it sucessfully deserialized the file.</returns>
        public static bool TryReadSpaceEngineersFile<T>(string fileName, out T outObject, out bool isCompressed,
                                                        out string errorInformation, bool snapshot = false,
                                                        bool specificExtension = false) where T : MyObjectBuilder_Base
        {
            string protoBufFile = null; 
              if (specificExtension && (Path.GetExtension(fileName) ?? string.Empty).EndsWith(SpaceEngineersConsts.ProtobuffersExtension, StringComparison.OrdinalIgnoreCase))
            {
                protoBufFile = Path.GetExtension(fileName) == SpaceEngineersConsts.ProtobuffersExtension ? fileName : fileName + SpaceEngineersConsts.ProtobuffersExtension;
            }

            if (protoBufFile != null && File.Exists(protoBufFile))
            {
                string tempFileName = protoBufFile;
                if (snapshot)
                {
                    // Snapshot used for Report on Dedicated servers to prevent locking of the orginal file whilst reading it.
                    tempFileName = TempFileUtil.NewFileName();
                    File.Copy(protoBufFile, tempFileName);
                }
                using (FileStream fileStream = new(tempFileName, FileMode.Open, FileAccess.Read))
                {
                    int b1 = fileStream.ReadByte();
                    int b2 = fileStream.ReadByte();
                    isCompressed = b1 == 0x1f && b2 == 0x8b;
                }

                bool retCode;
                try
                {
                    // A failure to load here, will only mean it falls back to try and read the xml file instead.
                    // So a file corruption could easily have been covered up.
                    retCode = MOBSerializerKeen.DeserializePB(tempFileName, out outObject);
                }
                catch (InvalidCastException ex)
                {
                    outObject = null;
                    errorInformation = string.Format(Res.ErrorLoadFileError, fileName, ex.AllMessages());
                    return false;
                }
                if (retCode && outObject != null)
                {
                    errorInformation = null;
                    return true;
                }
                return TryReadSpaceEngineersFileXml(fileName, out outObject, out isCompressed, out errorInformation, snapshot);
            }

            return TryReadSpaceEngineersFileXml(fileName, out outObject, out isCompressed, out errorInformation, snapshot);
        }

        private static bool TryReadSpaceEngineersFileXml<T>(string fileName, out T outObject, out bool isCompressed,
                                                            out string errorInformation, bool snapshot = false)
                                                            where T : MyObjectBuilder_Base
        {
            isCompressed = false;
            if (File.Exists(fileName))
            {
                string tempFileName = fileName;

                if (snapshot)
                {
                    // Snapshot used for Report on Dedicated servers to prevent locking of the orginal file whilst reading it.
                    tempFileName = TempFileUtil.NewFileName();
                    File.Copy(fileName, tempFileName, overwrite: true);
                }
                
                var fileInfo = new FileInfo(tempFileName);
                using var stream = fileInfo.OpenRead();
                var serializer = new XmlSerializer(typeof(T));
             
             byte[] buffer1 = new byte[stream.Length];
             byte[] buffer2 = new byte[stream.Length];

             stream.Read(buffer1, 0, buffer1.Length);
             Array.Copy(buffer1, buffer2, buffer1.Length);

             isCompressed = buffer1.SequenceEqual(buffer2);
                if(snapshot)
                {
                    File.Delete(tempFileName);
                }
                return DeserializeXml(tempFileName, out outObject, out errorInformation);
            }

            errorInformation = null;
            outObject = null;
            return false;
        }


        public static T Deserialize<T>(string xml) where T : MyObjectBuilder_Base
        {
            T outObject;
            using (var stream = new MemoryStream())
            {
                StreamWriter sw = new(stream);
                sw.Write(xml);
                sw.Flush();
                stream.Position = 0;

                MOBSerializerKeen.DeserializeXML(stream, out outObject);
            }
            return outObject;
        }

        public static string Serialize<T>(MyObjectBuilder_Base item)
        {
            using MemoryStream outStream = new();
            if (MOBSerializerKeen.SerializeXML(outStream, item))
            {
                outStream.Position = 0;

                StreamReader sw = new(outStream);
                return sw.ReadToEnd();
            }
            return null;
        }

        public static bool WriteSpaceEngineersFile<T>(T myObject, string fileName)
            where T : MyObjectBuilder_Base
        {
            bool ret;
            using StreamWriter sw = new(fileName);
            ret = MOBSerializerKeen.SerializeXML(sw.BaseStream, myObject);
            if (ret)
            {
                XmlTextWriter xmlTextWriter = new(sw.BaseStream, null);
                xmlTextWriter.WriteString("\r\n");
                xmlTextWriter.WriteComment($" Saved '{DateTime.Now:o}' with SEToolbox version '{GlobalSettings.GetAppVersion()}' ");
                xmlTextWriter.Flush();
            }
            return true;
        }

        public static bool WriteSpaceEngineersFilePB<T>(T myObject, string fileName, bool compress)
            where T : MyObjectBuilder_Base
        {
            return MOBSerializerKeen.SerializePB(fileName, compress, myObject);
        }

        /// <returns>True if it sucessfully deserialized the file.</returns>
        public static bool DeserializeXml<T>(string fileName, out T objectBuilder, out string errorInformation) where T : MyObjectBuilder_Base
        {
            bool result = false;
            objectBuilder = null;
            errorInformation = null;

            using var fileStream = MyFileSystem.OpenRead(fileName);
            using Stream readStream = fileStream.UnwrapGZip();
            
                if (fileStream != null && readStream != null)
                {
                    try
                    {
                        XmlSerializer serializer = MyXmlSerializerManager.GetSerializer(typeof(T));
                        XmlReaderSettings settings = new() { CheckCharacters = true };
                        MyXmlTextReader xmlReader = new(readStream, settings);

                                objectBuilder = (T)serializer.Deserialize(xmlReader);
                                result = true;
                            }
                            catch (Exception ex)
                            {
                                objectBuilder = null;
                                errorInformation = string.Format(Res.ErrorLoadFileError, fileName, ex.AllMessages());
                            }
                        }

            return result;
        }

        #endregion

        #region GenerateEntityId

        public static long GenerateEntityId(MyEntityIdentifier.ID_OBJECT_TYPE type)
        {
            return MyEntityIdentifier.AllocateId(type);
        }

        public static bool ValidateEntityType(MyEntityIdentifier.ID_OBJECT_TYPE type, long id)
        {
            return MyEntityIdentifier.GetIdObjectType(id) == type;
        }

        public static long GenerateEntityId()
        {
            // Not the offical SE way of generating IDs, but its fast and we don't have to worry about a random seed.
            var buffer = Guid.NewGuid().ToByteArray();
            return BitConverter.ToInt64(buffer, 0);
        }

        #endregion

        // #region GetIdentityById


        // public static MyObjectBuilder_Character GetPlayerById(long identityId)// it might Be from VRage.Game.ModAPI.IMyPlayer.Identity , VRage.Game.ModAPI.IMyIdentity.IsDead" or VRage.Game.ModAPI.IMyCharacter.IsDead"
        // {
        //     if (identityId <= 0)
        //         return null;

        //     return SpaceEngineersCore.WorldResource.SectorData.SectorObjects
        //         .OfType<MyObjectBuilder_Character>()
        //         .FirstOrDefault(character => character.OwningPlayerIdentityId == identityId);
        // }

        // #endregion

        #region FetchCubeBlockMass

        public static float FetchCubeBlockMass(MyObjectBuilderType typeId, MyCubeSize cubeSize, string subtypeName)
        {
            float mass = 0;

            var cubeBlockDefinition = GetCubeDefinition(typeId, cubeSize, subtypeName);

            if (cubeBlockDefinition != null)
            {
                return cubeBlockDefinition.Mass;
            }

            return mass;
        }

        public static void AccumulateCubeBlueprintRequirements(string subType, MyObjectBuilderType typeId, decimal amount, Dictionary<string, BlueprintRequirement> requirements, out TimeSpan timeTaken)
        {
            var time = new TimeSpan();
            var bp = GetBlueprint(typeId, subType);

            if (bp?.Results.Length > 0)
            {
                foreach (MyBlueprintDefinitionBase.Item item in bp.Prerequisites)
                {
                    if (requirements.ContainsKey(item.Id.SubtypeName))
                    {
                        // append existing
                        requirements[item.Id.SubtypeName].Amount = (amount / (decimal)bp.Results[0].Amount * (decimal)item.Amount) + requirements[item.Id.SubtypeName].Amount;
                    }
                    else
                    {
                        // add new
                        requirements.Add(item.Id.SubtypeName, new BlueprintRequirement
                        {
                            Amount = amount / (decimal)bp.Results[0].Amount * (decimal)item.Amount,
                            TypeId = item.Id.TypeId.ToString(),
                            SubtypeId = item.Id.SubtypeName,
                            Id = item.Id
                        });
                    }

                    double timeMassMultiplyer = 1;
                    if (typeId == typeof(MyObjectBuilder_Ore) || typeId == typeof(MyObjectBuilder_Ingot))
                        timeMassMultiplyer = (double)bp.Results[0].Amount;

                    var ts = TimeSpan.FromSeconds(bp.BaseProductionTimeInSeconds * (double)amount / timeMassMultiplyer);
                    time += ts;
                }
            }

            timeTaken = time;
        }

        
        public static MyBlueprintDefinitionBase GetBlueprint(MyObjectBuilderType resultTypeId, string resultSubTypeId)
        {
            // Get 'Last' item. Matches SE logic, which uses an array structure, and overrides previous found items of the same result.
            return SEResources.BlueprintDefinitions.LastOrDefault(b => b?.Results.Length == 1 && b.Results.Any(r => r.Id.TypeId == resultTypeId && r.Id.SubtypeName == resultSubTypeId));
        }

        #endregion

        #region GetCubeDefinition
        //new
        public static MyCubeBlockDefinition GetCubeDefinition(MyObjectBuilderType typeId, MyCubeSize cubeSize, string subtypeName)
        {
            if (!string.IsNullOrEmpty(subtypeName))
                return null;
            return MyDefinitionManager.Static.GetCubeBlockDefinition(new MyDefinitionId(typeId, subtypeName)) ??
                           SEResources.CubeBlockDefinitions?.FirstOrDefault(d => d.CubeSize == cubeSize && d.Id.TypeId == typeId) ??
                           SEResources.CubeBlockDefinitions.FirstOrDefault(d => d.Id.SubtypeName == subtypeName) ??
                           SEResources.CubeBlockDefinitions.FirstOrDefault(d => d.Variants.Any(v => subtypeName == d.Id.SubtypeName + v.Color));
                   
                                                                               
        }

        #endregion

        #region GetBoundingBox

        public static BoundingBoxD GetBoundingBox(MyObjectBuilder_CubeGrid entity)
        {
            var min = new Vector3D(int.MaxValue, int.MaxValue, int.MaxValue);
            var max = new Vector3D(int.MinValue, int.MinValue, int.MinValue);

            foreach (MyObjectBuilder_CubeBlock block in entity.CubeBlocks)
            {
                // Adjust min and max to account for block size
                Vector3I blockSize = GetCubeBlockSize(block, entity.GridSizeEnum);
                SerializableVector3I blockMin = block.Min;
                Vector3I blockMax = block.Min + blockSize - 1;

                min.X = Math.Min(min.X, blockMin.X);
                min.Y = Math.Min(min.Y, blockMin.Y);
                min.Z = Math.Min(min.Z, blockMin.Z);
                max.X = Math.Max(max.X, blockMax.X);
                max.Y = Math.Max(max.Y, blockMax.Y);
                max.Z = Math.Max(max.Z, blockMax.Z);
            }

            // Scale box to GridSize
            Vector3D size = max - min + Vector3D.One; // Add 1 to include the full block
            float gridSize = entity.GridSizeEnum.ToLength();
            size *= gridSize;

            // Translate box according to min/max, but reset origin
            BoundingBoxD bb = new(Vector3D.Zero, size);

            // Apply rotation and translation
            if (entity.PositionAndOrientation.HasValue)
            {
                Quaternion orientation = entity.PositionAndOrientation.Value.Orientation;
                SerializableVector3D position = entity.PositionAndOrientation.Value.Position;
                MatrixD transformationMatrix = MatrixD.CreateFromQuaternion(orientation);

                // Transform the min and max points of the bounding box
                Vector3D transformedMin = Vector3D.Transform(min, transformationMatrix) + position;
                Vector3D transformedMax = Vector3D.Transform(max, transformationMatrix) + position;
                // Create a new bounding box with the transformed points
                bb = new BoundingBoxD(transformedMin, transformedMax);
            }

            return bb;
        }

        private static Vector3I GetCubeBlockSize(MyObjectBuilder_CubeBlock block, MyCubeSize gridSizeEnum)
        {
            // Determine block size based on its type and grid size
            MyCubeBlockDefinition definition = GetCubeDefinition(block.TypeId, gridSizeEnum, block.SubtypeName);
            return definition?.Size ?? Vector3I.One;

        }

        #endregion

        #region LoadLocalization

        public static void LoadLocalization()
        {
            System.Globalization.CultureInfo culture = System.Threading.Thread.CurrentThread.CurrentUICulture;
            string languageTag = culture.IetfLanguageTag;

            string contentPath = ToolboxUpdater.GetApplicationContentPath();
            string localizationPath = Path.Combine(contentPath, @"Data\Localization");

            string[] codes = languageTag.Split(['-'], StringSplitOptions.RemoveEmptyEntries);
            string maincode = codes.Length > 0 ? codes[0] : null;
            string subcode = codes.Length > 1 ? codes[1] : null;

            MyTexts.Clear();

            if (GlobalSettings.Default.UseCustomResource.HasValue && GlobalSettings.Default.UseCustomResource.Value)
            {
                // no longer required, as Chinese is now officially in game.
                // left as an example for later additional custom languages.
                //AddLanguage(MyLanguagesEnum.ChineseChina, "zh-CN", null, "Chinese", 1f, true);
            }

            MyTexts.LoadTexts(localizationPath, maincode, subcode);

            if (GlobalSettings.Default.UseCustomResource.HasValue && GlobalSettings.Default.UseCustomResource.Value)
            {
                // Load alternate localization in instead using game refined resources, as they may not yet exist.
                ResourceManager customGameResourceManager = new("SEToolbox.Properties.MyTexts", Assembly.GetExecutingAssembly());
                ResourceSet customResourceSet = customGameResourceManager.GetResourceSet(culture, true, false);
                if (customResourceSet != null)
                {
                    // Reflection copy of MyTexts.PatchTexts(string resourceFile)
                    var m_strings = typeof(MyTexts).GetStaticField<Dictionary<MyStringId, string>>("m_strings");
                    var m_stringBuilders = typeof(MyTexts).GetStaticField<Dictionary<MyStringId, StringBuilder>>("m_stringBuilders");

                    foreach (DictionaryEntry dictionaryEntry in customResourceSet)
                    {
                        if (dictionaryEntry.Key is string text && !string.IsNullOrEmpty(text) &&
                            dictionaryEntry.Value is string text2 && !string.IsNullOrEmpty(text2))
                        {
                            MyStringId orCompute = MyStringId.GetOrCompute(text);

                            m_strings[orCompute] = text2;
                            m_stringBuilders[orCompute] = new StringBuilder(text2);
                        }
                    }
                }
            }
        }

        #endregion

        #region GetResourceName

        public static string GetResourceName(string value)
        {
            if (value == null)
                return null;

            MyStringId stringId = MyStringId.GetOrCompute(value);
            return MyTexts.GetString(stringId);
        }

        // Reflection copy of MyTexts.AddLanguage
        private static void AddLanguage(MyLanguagesEnum id, string cultureName, string subcultureName = null, string displayName = null, float guiTextScale = 1f, bool isCommunityLocalized = true)
        {
            // Create an empty instance of LanguageDescription.
            MyTexts.MyLanguageDescription languageDescription = ReflectionUtil.ConstructPrivateClass<MyTexts.MyLanguageDescription>(
                [typeof(MyLanguagesEnum), typeof(string), typeof(string), typeof(string), typeof(float), typeof(bool)],
                [id, displayName, cultureName, subcultureName, guiTextScale, isCommunityLocalized]);

            Dictionary<MyLanguagesEnum, MyTexts.MyLanguageDescription> m_languageIdToLanguage = typeof(MyTexts).GetStaticField<Dictionary<MyLanguagesEnum, MyTexts.MyLanguageDescription>>("m_languageIdToLanguage");
            Dictionary<string, MyLanguagesEnum> m_cultureToLanguageId = typeof(MyTexts).GetStaticField<Dictionary<string, MyLanguagesEnum>>("m_cultureToLanguageId");

            if (!m_languageIdToLanguage.ContainsKey(id))
            {
                m_languageIdToLanguage.Add(id, languageDescription);
                m_cultureToLanguageId.Add(languageDescription.FullCultureName, id);
            }
        }

        #endregion
    }
}