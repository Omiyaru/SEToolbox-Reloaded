using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using SEToolbox.Interop;
using VRage;
using VRage.Game;
using VRage.ObjectBuilders;
using Res = SEToolbox.Properties.Resources;

namespace SEToolbox.Models
{
    public class CubeItemModel : BaseModel
    {
        #region Fields

        private MyObjectBuilder_CubeBlock _cube;

        private MyObjectBuilderType _typeId;

        private string _subtypeId;

        private string _textureFile;

        private MyCubeSize _cubeSize;

        private string _friendlyName;

        private string _ownerName;

        private string _builtByName;

        private string _colorText;

        private float _colorHue;

        private float _colorSaturation;

        private float _colorLuminance;

        private BindablePoint3DIModel _position;

        private double _buildPercent;

        private System.Windows.Media.Brush _color;

        private int _pcu;

        private ObservableCollection<InventoryEditorModel> _inventory;

        #endregion

        #region Ctor

        public CubeItemModel(MyObjectBuilder_CubeBlock cube, MyCubeBlockDefinition definition)
        {
            SetProperties(cube, definition);
        }

        #endregion

        #region Properties

        public bool IsSelected { get; set; }

        public MyObjectBuilder_CubeBlock Cube
        {
            get => _cube;

            set => SetProperty(ref _cube, value, nameof(Cube));
            }

        

        public long Owner
        {
            get => _cube.Owner;
            set => SetProperty(ref _cube.Owner, value, nameof(Owner));
        }

        public long BuiltBy
        {
            get => Cube.BuiltBy;
            set => SetProperty(Cube.BuiltBy, value, nameof(BuiltBy));

        }

        public MyObjectBuilderType TypeId
        {
            get => _typeId;

            set => SetProperty(ref _typeId, value, nameof(TypeId));
        }

        public string SubtypeId
        {
            get => _subtypeId;

            set => SetProperty(ref _subtypeId, value, nameof(SubtypeId));
        }

        public string TextureFile
        {
            get => _textureFile;

            set => SetProperty(ref _textureFile, value, nameof(TextureFile));
        }

        public MyCubeSize CubeSize
        {
            get => _cubeSize;

            set => SetProperty(ref _cubeSize, value, nameof(CubeSize));
        }

        public string FriendlyName
        {
            get => _friendlyName;

            set => SetProperty(ref _friendlyName, value, nameof(FriendlyName));
        }

        public string OwnerName
        {
            get => _ownerName;

            set => SetProperty(ref _ownerName, value, nameof(OwnerName));
        }

        public string BuiltByName
        {
            get => _builtByName;

            set => SetProperty(ref _builtByName, value, nameof(BuiltByName));
        }

        public string ColorText
        {
            get => _colorText;

            set => SetProperty(ref _colorText, value, nameof(ColorText));
        }

        public float ColorHue
        {
            get => _colorHue;

            set => SetProperty(ref _colorHue, value, nameof(ColorHue));
        }

        public float ColorSaturation
        {
            get => _colorSaturation;

            set => SetProperty(ref _colorSaturation, value, nameof(ColorSaturation));
        }

        public float ColorLuminance
        {
            get => _colorLuminance;

            set => SetProperty(ref _colorLuminance, value, nameof(ColorLuminance));
        }

        public BindablePoint3DIModel Position
        {
            get => _position;

            set => SetProperty(ref _position, value, nameof(Position));
        }

        public override string ToString()
        {
            return FriendlyName;
        }

        public double BuildPercent
        {
            get => _buildPercent;

            set => SetProperty(ref _buildPercent, value, nameof(BuildPercent));
        }

        public System.Windows.Media.Brush Color
        {
            get => _color;

            set => SetProperty(ref _color, value, nameof(Color));
        }

        public int PCU
        {
            get => _pcu;

            set => SetProperty(ref _pcu, value, nameof(PCU));
        }

        public ObservableCollection<InventoryEditorModel> Inventory
        {
            get => _inventory;

            set => SetProperty(ref _inventory, value, nameof(Inventory));
        }

        #endregion

        public void SetColor(SerializableVector3 vector3)
        {
            Color = new System.Windows.Media.SolidColorBrush(vector3.FromHsvMaskToPaletteMediaColor());
            ColorText = Color.ToString();
            ColorHue = vector3.X;
            ColorSaturation = vector3.Y;
            ColorLuminance = vector3.Z;

            OnPropertyChanged(nameof(ColorText), nameof(ColorHue), nameof(ColorSaturation), nameof(ColorLuminance));
        }

        public void UpdateColor(SerializableVector3 vector3)
        {
            Cube.ColorMaskHSV = vector3;
            SetColor(vector3);
        }

        public void UpdateBuildPercent(double buildPercent)
        {
            Cube.IntegrityPercent = (float)buildPercent;
            Cube.BuildPercent = (float)buildPercent;
            BuildPercent = Cube.BuildPercent;
        }

        public static bool ConvertFromLightToHeavyArmor(MyObjectBuilder_CubeBlock cube)
        {
            var resource = SpaceEngineersResources.CubeBlockDefinitions.FirstOrDefault(b => b.Id.TypeId == cube.TypeId && b.Id.SubtypeName == cube.SubtypeName);

            if (resource == null)
            {
                return false;
            }

            var newSubTypeName = resource.Id.SubtypeName switch
            {
                { } SubTypeName when resource.Id.SubtypeName.StartsWith("LargeBlockArmor") => SubTypeName.Replace("LargeBlockArmor", "LargeHeavyBlockArmor"),
                { } SubTypeName when resource.Id.SubtypeName.StartsWith("Large") && (resource.Id.SubtypeName.EndsWith("HalfArmorBlock") || resource.Id.SubtypeName.EndsWith("HalfSlopeArmorBlock")) => SubTypeName.Replace("LargeHalf", "LargeHeavyHalf"),
                { } SubTypeName when resource.Id.SubtypeName.StartsWith("SmallBlockArmor") => SubTypeName.Replace("SmallBlockArmor", "SmallHeavyBlockArmor"),
                { } SubTypeName when !resource.Id.SubtypeName.StartsWith("Large") && (resource.Id.SubtypeName.EndsWith("HalfArmorBlock") || resource.Id.SubtypeName.EndsWith("HalfSlopeArmorBlock")) => Regex.Replace(SubTypeName, "^(Half)(.*)", "HeavyHalf$2", RegexOptions.IgnoreCase),
                _ => null
            };

            if (newSubTypeName == null || resource.Id.SubtypeName == newSubTypeName)
            {
                return false;
            }

            cube.SubtypeName = newSubTypeName;
            return true;
        }

        public static bool ConvertFromHeavyToLightArmor(MyObjectBuilder_CubeBlock cube)
        {
            var cubeBlockDefinitions = SpaceEngineersResources.CubeBlockDefinitions;
            var typeId = cube.TypeId;
            var subTypeId = cube.SubtypeName;
            var newSubTypeId = subTypeId switch
            {
                { } SubTypeId when subTypeId.StartsWith("LargeHeavyBlockArmor") => SubTypeId.Replace("LargeHeavyBlockArmor", "LargeBlockArmor"),
                { } SubTypeId when subTypeId.StartsWith("Large") && (subTypeId.EndsWith("HalfArmorBlock") || subTypeId.EndsWith("HalfSlopeArmorBlock")) => SubTypeId.Replace("LargeHeavyHalf", "LargeHalf"),
                { } SubTypeId when subTypeId.StartsWith("SmallHeavyBlockArmor") => SubTypeId.Replace("SmallHeavyBlockArmor", "SmallBlockArmor"),
                { } SubTypeId when !subTypeId.StartsWith("Large") && (subTypeId.EndsWith("HalfArmorBlock") || subTypeId.EndsWith("HalfSlopeArmorBlock")) => Regex.Replace(SubTypeId, "^(HeavyHalf)(.*)", "Half$2", RegexOptions.IgnoreCase),
                _ => null
            };

            if (newSubTypeId != null && cubeBlockDefinitions.Any(b => b.Id.TypeId == typeId && b.Id.SubtypeName == newSubTypeId))
            {
                cube.SubtypeName = newSubTypeId;
                return true;
            }

            return false;
        }

        public MyObjectBuilder_CubeBlock CreateCube(MyObjectBuilderType typeId, string subTypeId, MyCubeBlockDefinition definition)
        {
            MyObjectBuilder_CubeBlock newCube = SpaceEngineersResources.CreateNewObject<MyObjectBuilder_CubeBlock>(typeId, subTypeId);
            newCube.BlockOrientation = Cube.BlockOrientation;
            newCube.ColorMaskHSV = Cube.ColorMaskHSV;
            newCube.BuildPercent = Cube.BuildPercent;
            newCube.EntityId = Cube.EntityId;
            newCube.IntegrityPercent = Cube.IntegrityPercent;
            newCube.Min = Cube.Min;
            newCube.BuiltBy = Cube.BuiltBy;
            newCube.Owner = Cube.Owner;
            newCube.ShareMode = Cube.ShareMode;
            newCube.DeformationRatio = Cube.DeformationRatio;
            newCube.BlockGeneralDamageModifier = Cube.BlockGeneralDamageModifier;

            SetProperties(newCube, definition);

            return newCube;
        }

        public bool ChangeOwner(long newOwnerId)
        {
            // There appear to be quite a few exceptions, blocks that inherit from MyObjectBuilder_TerminalBlock but SE doesn't allow setting of Owner.
            if (Cube is MyObjectBuilder_InteriorLight
                || Cube  is MyObjectBuilder_ReflectorLight
                || Cube  is MyObjectBuilder_LandingGear
                || Cube  is MyObjectBuilder_Cockpit && SubtypeId == "PassengerSeatLarge"
                || Cube  is MyObjectBuilder_Thrust)
                return false;

            if (Cube is MyObjectBuilder_TerminalBlock)
            {
                Owner = newOwnerId;

                MyObjectBuilder_Identity identity = SpaceEngineersCore.WorldResource.Checkpoint.Identities.FirstOrDefault(p => p.PlayerId == Owner);
                string dead = $" ({Res.ClsCharacterDead})";
                if (SpaceEngineersCore.WorldResource.Checkpoint.AllPlayersData != null)
                {
                    var player = SpaceEngineersCore.WorldResource.Checkpoint.AllPlayersData.Dictionary.FirstOrDefault(kvp => kvp.Value.IdentityId == Owner);
                    dead = player.Value == null ? $" ({Res.ClsCharacterDead})" : "";
                }
                OwnerName = identity == null ? null : identity.DisplayName + dead;
                return true;
            }

            return false;
        }

        public bool ChangeBuiltBy(long newOwnerId)
        {
            BuiltBy = newOwnerId;

            MyObjectBuilder_Identity identity = SpaceEngineersCore.WorldResource.Checkpoint.Identities.FirstOrDefault(p => p.PlayerId == BuiltBy);
            string dead = $" ({Res.ClsCharacterDead})";
            if (SpaceEngineersCore.WorldResource.Checkpoint.AllPlayersData != null)
            {
                var player = SpaceEngineersCore.WorldResource.Checkpoint.AllPlayersData.Dictionary.FirstOrDefault(kvp => kvp.Value.IdentityId == BuiltBy);
                dead = player.Value == null ? $" ({Res.ClsCharacterDead})" : "";
            }
            BuiltByName = identity == null ? null : identity.DisplayName + dead;
            return true;
        }

        private void SetProperties(MyObjectBuilder_CubeBlock cube, MyCubeBlockDefinition definition)
        {
            Cube = cube;
            Position = new BindablePoint3DIModel(cube.Min);
            SetColor(cube.ColorMaskHSV);
            BuildPercent = cube.BuildPercent;

            if (definition == null)
            {
                // Obsolete block or Mod not loaded.
                return;
            }

            CubeSize = definition.CubeSize;
            FriendlyName = SpaceEngineersApi.GetResourceName(value: definition.DisplayNameText);

            MyObjectBuilder_Identity ownerIdentity = SpaceEngineersCore.WorldResource.Checkpoint.Identities.FirstOrDefault(p => p.PlayerId == Owner);
            MyObjectBuilder_Identity buyiltByIdentity = SpaceEngineersCore.WorldResource.Checkpoint.Identities.FirstOrDefault(p => p.PlayerId == BuiltBy);
            string ownerDead = $" ({Res.ClsCharacterDead})";
            string builtByDead = $" ({Res.ClsCharacterDead})";
            if (SpaceEngineersCore.WorldResource.Checkpoint.AllPlayersData != null)
            {
                var ownerPlayer = SpaceEngineersCore.WorldResource.Checkpoint.AllPlayersData.Dictionary.FirstOrDefault(kvp => kvp.Value.IdentityId == Owner);
                ownerDead = ownerPlayer.Value == null ? $" ({Res.ClsCharacterDead})" : "";

                var builtByPlayer = SpaceEngineersCore.WorldResource.Checkpoint.AllPlayersData.Dictionary.FirstOrDefault(kvp => kvp.Value.IdentityId == BuiltBy);
                builtByDead = builtByPlayer.Value == null ? $" ({Res.ClsCharacterDead})" : "";
            }

            OwnerName = ownerIdentity?.DisplayName + (ownerIdentity == null || SpaceEngineersCore.WorldResource.Checkpoint.AllPlayersData?.Dictionary.Values.Any(p => p.IdentityId == Owner) == false ? $" ({Res.ClsCharacterDead})" : "");
            BuiltByName = buyiltByIdentity?.DisplayName + (buyiltByIdentity == null || SpaceEngineersCore.WorldResource.Checkpoint.AllPlayersData?.Dictionary.Values.Any(p => p.IdentityId == BuiltBy) == false ? $" ({Res.ClsCharacterDead})" : "");

            TypeId = definition.Id.TypeId;
            SubtypeId = definition.Id.SubtypeName;
            PCU = definition.PCU;

            Inventory ??= [];
            Inventory.Clear();
            foreach (InventoryEditorModel item in cube.ComponentContainer.GetInventory(definition))
                Inventory.Add(item);

            while (Inventory.Count < 2)
            {
                Inventory.Add(new InventoryEditorModel(false));
            }
        }
    }
}