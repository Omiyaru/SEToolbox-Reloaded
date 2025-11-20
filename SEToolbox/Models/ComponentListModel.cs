using SEToolbox.Converters;
using SEToolbox.ImageLibrary;
using SEToolbox.Interop;
using SEToolbox.Support;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using VRage;
using VRage.FileSystem;
using VRage.Game;
using Res = SEToolbox.Properties.Resources;
using PhysItemDef = Sandbox.Definitions.MyPhysicalItemDefinition;
using ComponentDef = Sandbox.Definitions.MyComponentDefinition;
using BlueprintDef = Sandbox.Definitions.MyBlueprintDefinitionBase;
using Ext = SEToolbox.Support.HtmlExtensions;
using TexUtil = SEToolbox.ImageLibrary.ImageTextureUtil;

namespace SEToolbox.Models
{
    public class ComponentListModel : BaseModel
    {
        #region Fields

        private ObservableCollection<ComponentItemModel> _cubeAssets;

        private ObservableCollection<ComponentItemModel> _componentAssets;

        private ObservableCollection<ComponentItemModel> _itemAssets;

        private ObservableCollection<ComponentItemModel> _materialAssets;

        private bool _isBusy;

        private ComponentItemModel _selectedCubeAsset;

        #endregion

        #region Properties

        /// <summary>
        /// This is detail of the breakdown of cubes in the ship.
        /// </summary>
        public ObservableCollection<ComponentItemModel> CubeAssets
        {
            get => _cubeAssets;
            set => SetProperty(ref _cubeAssets, nameof(CubeAssets));
        }

        /// <summary>
        /// This is detail of the breakdown of components in the ship.
        /// </summary>
        public ObservableCollection<ComponentItemModel> ComponentAssets
        {
            get => _componentAssets;
            set => SetProperty(ref _componentAssets, nameof(ComponentAssets));
        }

        /// <summary>
        /// This is detail of the breakdown of items.
        /// </summary>
        public ObservableCollection<ComponentItemModel> ItemAssets
        {
            get => _itemAssets;
            set => SetProperty(ref _itemAssets, nameof(ItemAssets));
        }

        /// <summary>
        /// This is detail of the breakdown of materials used by asteroids.
        /// </summary>
        public ObservableCollection<ComponentItemModel> MaterialAssets
        {
            get => _materialAssets;
            set => SetProperty(ref _materialAssets, nameof(MaterialAssets));
        }

        /// <summary>
        /// Gets or sets a value indicating whether the View is currently in the middle of an asynchonise operation.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;

            set
            {
                SetProperty(ref _isBusy, nameof(IsBusy));
                if (_isBusy)
                {
                    System.Windows.Forms.Application.DoEvents();
                }
            }
        }

        public ComponentItemModel SelectedCubeAsset
        {
            get => _selectedCubeAsset;
            set => SetProperty(ref _selectedCubeAsset, nameof(SelectedCubeAsset));
        }

        #endregion

        #region Methods

        #region Load

        public void Load()
        {
            CubeAssets = [];
            ComponentAssets = [];
            ItemAssets = [];
            MaterialAssets = [];

            string contentPath = ToolboxUpdater.GetApplicationContentPath();

            foreach (Sandbox.Definitions.MyCubeBlockDefinition cubeDefinition in SpaceEngineersResources.CubeBlockDefinitions)
            {
                Dictionary<string, string> props = [];
                var fields = cubeDefinition.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

                foreach (var field in fields)
                {
                    props.Add(field.Name, GetValue(field, cubeDefinition));
                }

                string textureFile = null;

                if (cubeDefinition.Icons != null)
                {
                    string icon = cubeDefinition.Icons.FirstOrDefault();

                    if (icon != null)
                        textureFile = SpaceEngineersCore.GetDataPathOrDefault(icon, Path.Combine(contentPath, icon));
                }

                var buildTime = TimeSpan.Zero;

                if (cubeDefinition.IntegrityPointsPerSec != 0)
                {
                    double buildTimeSeconds = (double)cubeDefinition.MaxIntegrity / cubeDefinition.IntegrityPointsPerSec;

                    if (buildTimeSeconds <= TimeSpan.MaxValue.TotalSeconds)
                        buildTime = TimeSpan.FromSeconds(buildTimeSeconds);
                }

                CubeAssets.Add(new ComponentItemModel
                {
                    Name = cubeDefinition.DisplayNameText,
                    Definition = cubeDefinition,
                    TypeId = cubeDefinition.Id.TypeId,
                    TypeIdString = cubeDefinition.Id.TypeId.ToString(),
                    SubtypeId = cubeDefinition.Id.SubtypeName,
                    TextureFile = textureFile,
                    Time = buildTime,
                    Accessible = cubeDefinition.Public,
                    PCU = cubeDefinition.PCU,
                    Mass = SpaceEngineersApi.FetchCubeBlockMass(cubeDefinition.Id.TypeId, cubeDefinition.CubeSize, cubeDefinition.Id.SubtypeName),
                    CubeSize = cubeDefinition.CubeSize,
                    Size = new BindableSize3DIModel(cubeDefinition.Size),
                    CustomProperties = props,
                    IsMod = !cubeDefinition.Context.IsBaseGame,
                });
            }

            foreach (var componentDefinition in SpaceEngineersResources.ComponentDefinitions)
            {
                var bp = SpaceEngineersApi.GetBlueprint(componentDefinition.Id.TypeId, componentDefinition.Id.SubtypeName);
                float amount = 0;
                if (bp?.Results?.Length > 0)
                    amount = (float)bp.Results[0].Amount;

                ComponentAssets.Add(new ComponentItemModel
                {
                    Name = componentDefinition.DisplayNameText,
                    TypeId = componentDefinition.Id.TypeId,
                    TypeIdString = componentDefinition.Id.TypeId.ToString(),
                    SubtypeId = componentDefinition.Id.SubtypeName,
                    Mass = componentDefinition.Mass,
                    TextureFile = (componentDefinition.Icons == null || componentDefinition.Icons.First() == null) ? null : SpaceEngineersCore.GetDataPathOrDefault(componentDefinition.Icons.First(), Path.Combine(contentPath, componentDefinition.Icons.First())),
                    Volume = componentDefinition.Volume * SpaceEngineersConsts.VolumeMultiplyer,
                    Accessible = componentDefinition.Public,
                    Time = bp != null ? TimeSpan.FromSeconds(bp.BaseProductionTimeInSeconds / amount) : null,
                    IsMod = !componentDefinition.Context.IsBaseGame,
                });
            }

            foreach (var physicalItemDefinition in SpaceEngineersResources.PhysicalItemDefinitions)
            {
                var bp = SpaceEngineersApi.GetBlueprint(physicalItemDefinition.Id.TypeId, physicalItemDefinition.Id.SubtypeName);
                float amount = 0;
                if (bp?.Results?.Length > 0)
                    amount = (float)bp.Results[0].Amount;


                float timeMassMultiplyer = 1f;
                if (physicalItemDefinition.Id.TypeId == typeof(MyObjectBuilder_Ore)
                    || physicalItemDefinition.Id.TypeId == typeof(MyObjectBuilder_Ingot))
                    timeMassMultiplyer = physicalItemDefinition.Mass;

                ItemAssets.Add(new ComponentItemModel
                {
                    Name = physicalItemDefinition.DisplayNameText,
                    TypeId = physicalItemDefinition.Id.TypeId,
                    TypeIdString = physicalItemDefinition.Id.TypeId.ToString(),
                    SubtypeId = physicalItemDefinition.Id.SubtypeName,
                    Mass = physicalItemDefinition.Mass,
                    Volume = physicalItemDefinition.Volume * SpaceEngineersConsts.VolumeMultiplyer,
                    TextureFile = physicalItemDefinition.Icons == null ? null : SpaceEngineersCore.GetDataPathOrDefault(physicalItemDefinition.Icons.First(), Path.Combine(contentPath, physicalItemDefinition.Icons.First())),
                    Accessible = physicalItemDefinition.Public,
                    Time = bp != null ? TimeSpan.FromSeconds(bp.BaseProductionTimeInSeconds / amount / timeMassMultiplyer) : null,
                    IsMod = !physicalItemDefinition.Context.IsBaseGame,
                });
            }

            foreach (MyVoxelMaterialDefinition voxelMaterialDefinition in SpaceEngineersResources.VoxelMaterialDefinitions)
            {
                string texture = voxelMaterialDefinition.GetVoxelDisplayTexture();

                MaterialAssets.Add(new ComponentItemModel
                {
                    Name = voxelMaterialDefinition.Id.SubtypeName,
                    TextureFile = texture == null ? null : SpaceEngineersCore.GetDataPathOrDefault(texture, Path.Combine(contentPath, texture)),
                    IsRare = voxelMaterialDefinition.IsRare,
                    OreName = voxelMaterialDefinition.MinedOre,
                    MineOreRatio = voxelMaterialDefinition.MinedOreRatio,
                    IsMod = !voxelMaterialDefinition.Context.IsBaseGame,
                });
            }
        }

        #endregion

        #region GenerateHtmlReport

        public void GenerateHtmlReport(string fileName)
        {
            StringWriter stringWriter = new();

            // Put HtmlTextWriter in using block because it needs to call Dispose.
            using (StringWriter writer = new())
            {
                #region Header

                writer.BeginDocument(Res.CtlComponentTitleReport,
                   @"
                    body { background-color: #E6E6FA }
                    h1 { font-family: Arial, Helvetica, sans-serif; }
                    table { background-color: #FFFFFF; }
                    table tr td { font-family: Arial, Helvetica, sans-serif; font-size: small; line-height: normal; color: #000000; }
                    table thead td { background-color: #BABDD6; font-weight: bold; Color: #000000; }
                    td.right { text-align: right; }");

                #endregion

                #region Cubes/Components/Items

                Dictionary<string, ObservableCollection<ComponentItemModel>> componentCollections = new()
                {
                    {Res.CtlComponentTitleCubes, CubeAssets },
                    {Res.CtlComponentTitleComponents, ComponentAssets},
                    {Res.CtlComponentTitleItems, ItemAssets },
                };

                foreach (var componentCollection in componentCollections)
                {
                    writer.RenderElement("h1", componentCollection.Key);
                    writer.BeginTable("1", "3", "0",
                        [Res.CtlComponentColIcon, Res.CtlComponentColName, Res.CtlComponentColType, Res.CtlComponentColSubType, Res.CtlComponentColCubeSize, Res.CtlComponentColPCU, Res.CtlComponentColAccessible, Res.CtlComponentColSize, Res.CtlComponentColMass, Res.CtlComponentColBuildTime, Res.CtlComponentColMod]);

                    foreach (var asset in componentCollection.Value)
                    {
                        writer.RenderTagStart("td");

                        writer.RenderTagStart("td");
                        if (asset.TextureFile != null)
                        {
                            string texture = GetTextureToBase64(asset.TextureFile, 32, 32);
                            if (!string.IsNullOrEmpty(texture))
                            {
                                writer.AddAttribute("src", "data:image/png;base64," + texture);
                                writer.AddAttribute("width", "32");
                                writer.AddAttribute("height", "32");
                                writer.AddAttribute("alt", Path.GetFileNameWithoutExtension(asset.TextureFile));
                                writer.RenderTagStart("img");
                                writer.RenderTagEnd("tr");
                            }
                        }
                        writer.RenderTagEnd("td"); // Td

                        writer.RenderElement("td", asset.FriendlyName);
                        writer.RenderElement("td", asset.TypeId);
                        writer.RenderElement("td", asset.SubtypeId);
                        writer.RenderElement("td", asset.CubeSize);
                        writer.RenderElement("td", asset.PCU);
                        writer.RenderElement("td", new EnumToResourceConverter().Convert(asset.Accessible, typeof(string), null, CultureInfo.CurrentUICulture));
                        writer.RenderElement("td", $"{asset.Size.Width}x{asset.Size.Height}x{asset.Size.Depth}", null);
                        writer.AddAttribute("class", "right");
                        writer.RenderElement("td", $"{asset.Mass:#,##0.00}");
                        writer.AddAttribute("class", "right");
                        writer.RenderElement("td", $"{asset.Time:hh\\:mm\\:ss\\.ff}");
                        writer.RenderElement("td", new EnumToResourceConverter().Convert(asset.IsMod, typeof(string), null, CultureInfo.CurrentUICulture));

                        writer.RenderTagEnd("tr"); // Tr
                    }
                    writer.RenderTagEnd("table"); // Table
                }
                #endregion

                #region Materials

                writer.RenderElement("h1", Res.CtlComponentTitleMaterials);
                writer.BeginTable("1", "3", "0",
                    [Res.CtlComponentColTexture, Res.CtlComponentColName, Res.CtlComponentColOreName, Res.CtlComponentColRare, Res.CtlComponentColMinedOreRatio, Res.CtlComponentColMod]);

                foreach (ComponentItemModel asset in MaterialAssets)
                {
                    writer.RenderTagStart("td");

                    writer.RenderTagStart("td");
                    if (asset.TextureFile != null)
                    {
                        string texture = GetTextureToBase64(asset.TextureFile, 32, 32, true);
                        if (!string.IsNullOrEmpty(texture))
                        {
                            writer.AddAttribute("src", "data:image/png;base64," + texture);
                            writer.AddAttribute("width", "32");
                            writer.AddAttribute("height", "32");
                            writer.AddAttribute("alt", Path.GetFileNameWithoutExtension(asset.TextureFile));
                            writer.RenderTagStart("img");
                            writer.RenderTagEnd("tr");
                        }
                    }
                    writer.RenderTagEnd("td"); // Td

                    writer.RenderElement("td", asset.Name);
                    writer.RenderElement("td", asset.OreName);
                    writer.RenderElement("td", new EnumToResourceConverter().Convert(asset.IsRare, typeof(string), null, CultureInfo.CurrentUICulture));
                    writer.RenderElement("td", $"{asset.MineOreRatio:#,##0.00}");
                    writer.RenderElement("td", new EnumToResourceConverter().Convert(asset.IsMod, typeof(string), null, CultureInfo.CurrentUICulture));

                    writer.RenderTagEnd("tr"); // Tr
                }
                writer.RenderTagEnd("table"); // Table

                #endregion

                #region Footer

                writer.EndDocument();

                #endregion
            }

            // Write to disk.
            File.WriteAllText(fileName, stringWriter.ToString());
        }

        #endregion

        private static string GetTextureToBase64(string fileName, int width, int height, bool ignoreAlpha = false)
        {
            using Stream stream = MyFileSystem.OpenRead(fileName);
            return TexUtil.GetTextureToBase64(stream, fileName, width, height, ignoreAlpha);
        }

        #region GetRowValue

        public static string GetValue(FieldInfo field, object objt)
        {
            object item = field.GetValue(objt);

            if (field.FieldType == typeof(SerializableVector3I))
            {
                var vector = (SerializableVector3I)item;
                return string.Format($"{vector.X}, {vector.Y}, {vector.Z}");
            }

            if (field.FieldType == typeof(SerializableVector3))
            {
                SerializableVector3 vector = (SerializableVector3)item;
                return string.Format($"{vector.X}, {vector.Y}, {vector.Z}");
            }

            if (field.FieldType == typeof(SerializableBounds))
            {
                SerializableBounds bounds = (SerializableBounds)item;
                CultureInfo culture = CultureInfo.CurrentUICulture;
                return string.Format(culture, $"Default: {bounds.Default}, Min: {bounds.Min}, Max: {bounds.Max}");
            }

            if (field.FieldType == typeof(VRageMath.Vector3))
            {
                VRageMath.Vector3 vector3 = (VRageMath.Vector3)item;
                CultureInfo culture = CultureInfo.CurrentUICulture;
                return string.Format(culture, $"X:{vector3.X:F2}, Y:{vector3.Y:F2}, Z:{vector3.Z:F2}");
            }

            if (field.FieldType == typeof(string))
            {
                return item as string;
            }

            if (item == null)
            {
                return string.Empty;
            }

            return item.ToString();
        }

        #endregion

        #endregion
    }
}
