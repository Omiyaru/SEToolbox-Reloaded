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
            set => SetProperty(ref _cubeAssets, value, nameof(CubeAssets));
        }

        /// <summary>
        /// This is detail of the breakdown of components in the ship.
        /// </summary>
        public ObservableCollection<ComponentItemModel> ComponentAssets
        {
            get => _componentAssets;
            set => SetProperty(ref _componentAssets, value, nameof(ComponentAssets));
        }

        /// <summary>
        /// This is detail of the breakdown of items.
        /// </summary>
        public ObservableCollection<ComponentItemModel> ItemAssets
        {
            get => _itemAssets;
            set => SetProperty(ref _itemAssets, value, nameof(ItemAssets));
        }

        /// <summary>
        /// This is detail of the breakdown of materials used by asteroids.
        /// </summary>
        public ObservableCollection<ComponentItemModel> MaterialAssets
        {
            get => _materialAssets;
            set => SetProperty(ref _materialAssets, value, nameof(MaterialAssets));
        }

        /// <summary>
        /// Gets or sets a value indicating whether the View is currently in the middle of an asynchonise operation.
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value, nameof(IsBusy), () =>
            {
                if (_isBusy)
                {
                    System.Windows.Forms.Application.DoEvents();
                }
            });

        }

        public ComponentItemModel SelectedCubeAsset
        {
            get => _selectedCubeAsset;
            set => SetProperty(ref _selectedCubeAsset, value, nameof(SelectedCubeAsset));
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
                string icon = cubeDefinition?.Icons.FirstOrDefault();

                if (icon != null)
                {
                    textureFile = SpaceEngineersCore.GetDataPathOrDefault(icon, Path.Combine(contentPath, icon));
                }

                var buildTime = TimeSpan.Zero;

                if (cubeDefinition.IntegrityPointsPerSec != 0)
                {
                    double buildTimeSeconds = (double)cubeDefinition.MaxIntegrity / cubeDefinition.IntegrityPointsPerSec;

                    if (buildTimeSeconds <= TimeSpan.MaxValue.TotalSeconds)
                    {
                        buildTime = TimeSpan.FromSeconds(buildTimeSeconds);
                    }
                }

                CubeAssets.Add(new ComponentItemModel
                {
                    Name = cubeDefinition.DisplayNameText,
                    Definition = cubeDefinition,
                    TypeId = cubeDefinition.Id.TypeId,
                    TypeIdString = $"{cubeDefinition.Id.TypeId}",
                    SubtypeName = cubeDefinition.Id.SubtypeName,
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
                var bluePrint = SpaceEngineersApi.GetBlueprint(componentDefinition.Id.TypeId, componentDefinition.Id.SubtypeName);
                float amount = 0;
                if (bluePrint?.Results?.Length > 0)
                {
                    amount = (float)bluePrint.Results[0].Amount;
                }

                ComponentAssets.Add(new ComponentItemModel
                {
                    Name = componentDefinition.DisplayNameText,
                    TypeId = componentDefinition.Id.TypeId,
                    TypeIdString = $"{componentDefinition.Id.TypeId}",
                    SubtypeName = componentDefinition.Id.SubtypeName,
                    Mass = componentDefinition.Mass,
                    TextureFile = (componentDefinition.Icons == null || componentDefinition.Icons.First() == null) ? null : SpaceEngineersCore.GetDataPathOrDefault(componentDefinition.Icons.First(), Path.Combine(contentPath, componentDefinition.Icons.First())),
                    Volume = componentDefinition.Volume * SpaceEngineersConsts.VolumeMultiplier,
                    Accessible = componentDefinition.Public,
                    Time = bluePrint != null ? TimeSpan.FromSeconds(bluePrint.BaseProductionTimeInSeconds / amount) : null,
                    IsMod = !componentDefinition.Context.IsBaseGame,
                });
            }

            foreach (var physItemDef in SpaceEngineersResources.PhysicalItemDefinitions)
            {
                var bluePrint = SpaceEngineersApi.GetBlueprint(physItemDef.Id.TypeId, physItemDef.Id.SubtypeName);
                float amount = 0;
                if (bluePrint?.Results?.Length > 0)
                {
                    amount = (float)bluePrint.Results[0].Amount;
                }

                float timeMassMultiplier = 1f;
                if (physItemDef.Id.TypeId == typeof(MyObjectBuilder_Ore)
                    || physItemDef.Id.TypeId == typeof(MyObjectBuilder_Ingot))
                {
                    timeMassMultiplier = physItemDef.Mass;
                }

                ItemAssets.Add(new ComponentItemModel
                {
                    Name = physItemDef.DisplayNameText,
                    TypeId = physItemDef.Id.TypeId,
                    TypeIdString = $"{physItemDef.Id.TypeId}",
                    SubtypeName = physItemDef.Id.SubtypeName,
                    Mass = physItemDef.Mass,
                    Volume = physItemDef.Volume * SpaceEngineersConsts.VolumeMultiplier,
                    TextureFile = physItemDef.Icons == null ? null : SpaceEngineersCore.GetDataPathOrDefault(physItemDef.Icons.First(), Path.Combine(contentPath, physItemDef.Icons.First())),
                    Accessible = physItemDef.Public,
                    Time = bluePrint != null ? TimeSpan.FromSeconds(bluePrint.BaseProductionTimeInSeconds / amount / timeMassMultiplier) : null,
                    IsMod = !physItemDef.Context.IsBaseGame,
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
                        writer.RenderTagStart("tr");

                        writer.RenderTagStart("td");

                        string texture = GetTextureToBase64(asset?.TextureFile, 32, 32);
                        if (!string.IsNullOrEmpty(texture))
                        {
                            writer.AddAttribute("src", "data:image/png;base64," + texture);
                            writer.AddAttribute("width", "32");
                            writer.AddAttribute("height", "32");
                            writer.AddAttribute("alt", Path.GetFileNameWithoutExtension(asset.TextureFile));
                            writer.RenderTagStart("img");
                            writer.RenderTagEnd("tr");
                        }

                        writer.RenderTagEnd("td"); // Td

                        writer.RenderElement("td", asset.FriendlyName);
                        writer.RenderElement("td", asset.TypeId);
                        writer.RenderElement("td", asset.SubtypeName);
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
                    writer.RenderTagStart("tr");

                    writer.RenderTagStart("td");

                    string texture = GetTextureToBase64(asset?.TextureFile, 32, 32, true);
                    if (!string.IsNullOrEmpty(texture))
                    {
                        writer.AddAttribute("src", "data:image/png;base64," + texture);
                        writer.AddAttribute("width", "32");
                        writer.AddAttribute("height", "32");
                        writer.AddAttribute("alt", Path.GetFileNameWithoutExtension(asset.TextureFile));
                        writer.RenderTagStart("img");
                        writer.RenderTagEnd("tr");
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
            File.WriteAllText(fileName, $"{stringWriter}");
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
                return $"{vector.X}, {vector.Y}, {vector.Z}";
            }

            if (field.FieldType == typeof(SerializableVector3))
            {
                SerializableVector3 vector = (SerializableVector3)item;
                return $"{vector.X}, {vector.Y}, {vector.Z}";
            }

            if (field.FieldType == typeof(SerializableBounds))
            {
                SerializableBounds bounds = (SerializableBounds)item;
                return $"Default: {bounds.Default}, Min: {bounds.Min}, Max: {bounds.Max}".ToString(CultureInfo.CurrentUICulture);
            }

            if (field.FieldType == typeof(VRageMath.Vector3))
            {
                var vector3 = (VRageMath.Vector3)item;
                return $"X:{vector3.X:F2}, Y:{vector3.Y:F2}, Z:{vector3.Z:F2}".ToString(CultureInfo.CurrentCulture);
            }

            if (field.FieldType == typeof(string))
            {
                return item as string;
            }

            if (item == null)
            {
                return string.Empty;
            }

            return $"{item}";
        }

        #endregion

        #endregion
    }
}
