using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using SEToolbox.Models;
using SEToolbox.Support;
using VRage;
using VRage.Game;
using VRage.Game.ObjectBuilders.Components;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.Voxels;
using VRage.ObjectBuilders;
using VRageMath;
using MOBTypeIds = SEToolbox.Interop.SpaceEngineersTypes.MOBTypeIds;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Media3D = System.Windows.Media.Media3D;


namespace SEToolbox.Interop
{
    /// <summary>
    /// Contains Extension methods specifically for Keen classes and structures.
    /// </summary>
    public static class SpaceEngineersExtensions
    {
        internal static SerializableVector3I Mirror(this SerializableVector3I vector, Mirror xMirror, int xAxis, Mirror yMirror, int yAxis, Mirror zMirror, int zAxis)
        {
            int vecAxisX = vector.X - xAxis;
            int vecAxisY = vector.Y - yAxis;
            int vecAxisZ = vector.Z - zAxis;
            int offsetX = xMirror == Support.Mirror.Odd ? -vecAxisX : 0;
            offsetX += xMirror == Support.Mirror.EvenUp ? 1 :
                      (xMirror == Support.Mirror.EvenDown ? -1 : 0);
            int offsetY = yMirror == Support.Mirror.Odd ? -vecAxisY : 0;
            offsetY += yMirror == Support.Mirror.EvenUp ? 1 :
                      (yMirror == Support.Mirror.EvenDown ? -1 : 0);
            int offsetZ = zMirror == Support.Mirror.Odd ? -vecAxisZ : 0;
            offsetZ += zMirror == Support.Mirror.EvenUp ? 1 :
                      (zMirror == Support.Mirror.EvenDown ? -1 : 0);

            Vector3I newVector = new(vector.X + offsetX,
                                     vector.Y + offsetY,
                                     vector.Z + offsetZ);
            return newVector;
        }

        public static double LinearVector(this Vector3 vector)
        {
            return Math.Sqrt(Math.Pow(vector.X, 2) + Math.Pow(vector.Y, 2) + Math.Pow(vector.Z, 2));
        }

        public static Vector3I ToVector3I(this SerializableVector3I vector)
        {
            return new(vector.X, vector.Y, vector.Z);
        }

        public static Vector3I RoundToVector3I(this Vector3 vector)
        {
            return new((int)Math.Round(vector.X, 0, MidpointRounding.ToEven),
                       (int)Math.Round(vector.Y, 0, MidpointRounding.ToEven),
                       (int)Math.Round(vector.Z, 0, MidpointRounding.ToEven));
        }

        public static Vector3I RoundToVector3I(this Vector3D vector)
        {
            return new((int)Math.Round(vector.X, 0, MidpointRounding.ToEven),
                       (int)Math.Round(vector.Y, 0, MidpointRounding.ToEven),
                       (int)Math.Round(vector.Z, 0, MidpointRounding.ToEven));
        }

        public static Vector3 ToVector3(this SerializableVector3I vector)
        {
            return new(vector.X, vector.Y, vector.Z);
        }

        public static Vector3D ToVector3D(this SerializableVector3I vector)
        {
            return new(vector.X, vector.Y, vector.Z);
        }

        public static Vector3 ToVector3(this SerializableVector3 vector)
        {
            return new(vector.X, vector.Y, vector.Z);
        }

        public static Vector3I SizeInt(this BoundingBox box)
        {
            var size = box.Size;
            return new((int)size.X,
                       (int)size.Y,
                       (int)size.Z);
        }

        public static Vector3I SizeInt(this BoundingBoxD box)
        {
            var size = box.Size;
            return new((int)size.X,
                       (int)size.Y,
                       (int)size.Z);
        }

        public static Media3D.Vector3D ToVector3D(this SerializableVector3 vector)
        {
            return new Media3D.Vector3D(vector.X, vector.Y, vector.Z);
        }

        public static Media3D.Vector3D ToVector3D(this Vector3 vector)
        {
            return new Media3D.Vector3D(vector.X, vector.Y, vector.Z);
        }

        public static Point3D ToPoint3D(this Vector3D vector)
        {
            return new Point3D(vector.X, vector.Y, vector.Z);
        }

        public static Point3D ToPoint3D(this SerializableVector3 point)
        {
            return new Point3D(point.X, point.Y, point.Z);
        }

        public static Point3D ToPoint3D(this SerializableVector3D point)
        {
            return new Point3D(point.X, point.Y, point.Z);
        }

        public static System.Windows.Point ToPoint(this Vector2 vector)
        {
            return new System.Windows.Point(vector.X, vector.Y);
        }

        public static Vector3 ToVector3(this Point3D point)
        {
            return new((float)point.X,
                       (float)point.Y,
                       (float)point.Z);
        }

        public static Vector3D ToVector3D(this Point3D point)
        {
            return new(point.X, point.Y, point.Z);
        }

        public static Vector3 ToVector3(this Media3D.Size3D size3D)
        {
            return new((float)size3D.X,
                       (float)size3D.Y,
                       (float)size3D.Z);
        }

        public static Vector3D ToVector3D(this Media3D.Size3D size3D)
        {
            return new(size3D.X, size3D.Y, size3D.Z);
        }

        public static Vector3 ToVector3(this Media3D.Vector3D size3D)
        {
            return new((float)size3D.X,
                       (float)size3D.Y,
                       (float)size3D.Z);
        }

        public static Vector3D ToVector3D(this Media3D.Vector3D size3D)
        {
            return new(size3D.X, size3D.Y, size3D.Z);
        }

        public static Quaternion ToQuaternion(this SerializableBlockOrientation blockOrientation)
        {
            var matrix = Matrix.CreateFromDir(Base6Directions.GetVector(blockOrientation.Forward), Base6Directions.GetVector(blockOrientation.Up));
            return Quaternion.CreateFromRotationMatrix(matrix);
        }

        public static Quaternion ToQuaternion(this MyPositionAndOrientation positionOrientation)
        {
            return Quaternion.CreateFromForwardUp(positionOrientation.Forward, positionOrientation.Up);
        }

        public static QuaternionD ToQuaternionD(this MyPositionAndOrientation positionOrientation)
        {
            return QuaternionD.CreateFromForwardUp(new(positionOrientation.Forward),
                                                   new(positionOrientation.Up));
        }

        public static Matrix ToMatrix(this MyPositionAndOrientation positionOrientation)
        {
            return Matrix.CreateFromQuaternion(Quaternion.CreateFromForwardUp(positionOrientation.Forward, positionOrientation.Up));
        }

        public static Matrix ToMatrix(this Quaternion quaternion)
        {
            return Matrix.CreateFromQuaternion(quaternion);
        }

        public static Vector3 Transform(this Vector3 vector, SerializableBlockOrientation orientation)
        {
            var matrix = Matrix.CreateFromDir(Base6Directions.GetVector(orientation.Forward),
                                              Base6Directions.GetVector(orientation.Up));

            return Vector3.Transform(vector, matrix);
        }

        public static Vector3D Transform(this Vector3D vector, SerializableBlockOrientation orientation)
        {
            var matrix = MatrixD.CreateFromDir(Base6Directions.GetVector(orientation.Forward),
                                               Base6Directions.GetVector(orientation.Up));

            return Vector3D.Transform(vector, matrix);
        }

        public static Vector3I Transform(this SerializableVector3I size, SerializableBlockOrientation orientation)
        {
            var matrix = Matrix.CreateFromDir(Base6Directions.GetVector(orientation.Forward),
                                              Base6Directions.GetVector(orientation.Up));

            var rotation = Quaternion.CreateFromRotationMatrix(matrix);
            return Vector3I.Transform(size.ToVector3I(), rotation);
        }

        public static Vector3I Transform(this Vector3I size, SerializableBlockOrientation orientation)
        {
            var matrix = Matrix.CreateFromDir(Base6Directions.GetVector(orientation.Forward),
                                              Base6Directions.GetVector(orientation.Up));

            var rotation = Quaternion.CreateFromRotationMatrix(matrix);
            return Vector3I.Transform(size, rotation);
        }

        public static SerializableVector3I Add(this SerializableVector3I size, int value)
        {
            return new(size.X + value,
                       size.Y + value,
                       size.Z + value);

        }

        public static Vector3I Add(this Vector3I size, int value)
        {
            return new(size.X + value,
                       size.Y + value,
                       size.Z + value);

        }

        public static Vector3I Abs(this Vector3I size)
        {
            return new(Math.Abs(size.X),
                       Math.Abs(size.Y),
                       Math.Abs(size.Z));

        }

        public static Vector3D ToVector3D(this Vector3I vector)
        {
            return new(vector.X, vector.Y, vector.Z);
        }

        public static BoundingBoxD ToBoundingBoxD(this BoundingBoxI box)
        {
            return new(box.Min, box.Max);

        }

        public static SerializableVector3 RoundOff(this SerializableVector3 vector, float roundTo)
        {
            return new((float)Math.Round(vector.X / roundTo, 0, MidpointRounding.ToEven) * roundTo,
                       (float)Math.Round(vector.Y / roundTo, 0, MidpointRounding.ToEven) * roundTo,
                       (float)Math.Round(vector.Z / roundTo, 0, MidpointRounding.ToEven) * roundTo);
        }

        public static SerializableVector3D RoundOff(this SerializableVector3D vector, float roundTo)
        {
            return new(Math.Round(vector.X / roundTo, 0, MidpointRounding.ToEven) * roundTo,
                        Math.Round(vector.Y / roundTo, 0, MidpointRounding.ToEven) * roundTo,
                        Math.Round(vector.Z / roundTo, 0, MidpointRounding.ToEven) * roundTo);

        }

        public static MatrixD ToMatrixD(this QuaternionD value)
        {
            double xx = value.X * value.X;
            double yy = value.Y * value.Y;
            double zz = value.Z * value.Z;
            double xy = value.X * value.Y;
            double xz = value.X * value.Z;
            double yz = value.Y * value.Z;
            double wx = value.X * value.W;
            double wy = value.Y * value.W;
            double wz = value.Z * value.W;

            MatrixD result = new(
                1.0d - 2.0d * (yy + zz),
                2.0d * (xy + wz),
                2.0d * (xz - wy),
                0d,
                2.0d * (xy - wz),
                1.0d - 2.0d * (xx + zz),
                2.0d * (yz + wx),
                0d,
                2.0d * (xz + wy),
                2.0d * (yz - wx),
                1.0d - 2.0d * (xx + yy),
                0d, 0d, 0d, 0d, 1d);

            return result;
        }

        public static SerializableVector3 RoundToAxis(this SerializableVector3 vector)
        {
            if (Math.Abs(vector.X) > Math.Abs(vector.Y) && Math.Abs(vector.X) > Math.Abs(vector.Z))
                return new SerializableVector3(Math.Sign(vector.X), 0, 0);

            if (Math.Abs(vector.Y) > Math.Abs(vector.X) && Math.Abs(vector.Y) > Math.Abs(vector.Z))
                return new SerializableVector3(0, Math.Sign(vector.Y), 0);

            if (Math.Abs(vector.Z) > Math.Abs(vector.X) && Math.Abs(vector.Z) > Math.Abs(vector.Y))
                return new SerializableVector3(0, 0, Math.Sign(vector.Z));

            return new SerializableVector3();
        }

        private static decimal Clamp(decimal value, decimal min, decimal max)
        {
            value = (value > max) ? max : value;
            value = (value < min) ? min : value;
            return value;
        }

        /// <summary>
        /// Converts from Keen's HSV stored format to RGB matching the in game color picker palatte.
        /// </summary>
        /// <param name="hsv">the HSV stored value in the range of Hue=X=0.0 to +1.0, Saturation=Y=-1.0 to +1.0, Value=Z=-1.0 to +1.0</param>
        /// <param name="red">converted red value</param>
        /// <param name="green">converted green value</param>
        /// <param name="blue">converted blue value</param>
        /// <remarks>sourced from wikipedia.</remarks>
        private static void FromHsvMaskToPaletteColor(SerializableVector3 hsv, out int red, out int green, out int blue)
        {
            // I've used decimal because of floating point aberation during calculations.
            // This needs to maintain the color accuracy as much as possible.
            // I'm still not happy with this, as the game color palette picker is not exactly representative of the in game colors,
            // and looking through the calculations, the picker is actually ignoring part of the saturation and value.
            decimal hue = (decimal)hsv.X * 360;
            decimal saturation = Clamp((decimal)hsv.Y + (decimal)MyColorPickerConstants.SATURATION_DELTA, 0, 1);
            decimal value = Clamp((decimal)hsv.Z + (decimal)MyColorPickerConstants.VALUE_DELTA - (decimal)MyColorPickerConstants.VALUE_COLORIZE_DELTA, 0, 1);

            decimal chroma = value * saturation;
            decimal hue1 = hue / 60;
            decimal x = chroma * (1 - Math.Abs(hue1 % 2 - 1));

            decimal r1 = 0, g1 = 0, b1 = 0;

            switch ((int)hue)
            {
                case 0:
                    r1 = chroma; g1 = x;
                    break;
                case 1:
                    r1 = x; g1 = chroma;
                    break;
                case 2:
                    g1 = chroma; b1 = x;
                    break;
                case 3:
                    g1 = x; b1 = chroma;
                    break;
                case 4:
                    r1 = x; b1 = chroma;
                    break;
                case 5:
                    r1 = chroma; b1 = x;
                    break;
                default:
                    if (chroma != 0)
                        throw new InvalidOperationException("Unexpected value");
                    r1 = 0; g1 = 0; b1 = 0;
                    break;
            }
            decimal m = value - chroma;

            // round off (not up or truncate down) values to correct for aberration.
            red = (int)Math.Round((r1 + m) * 255);
            green = (int)Math.Round((g1 + m) * 255);
            blue = (int)Math.Round((b1 + m) * 255);
        }

        public static System.Drawing.Color FromHsvMaskToPaletteColor(this SerializableVector3 hsv)
        {

            FromHsvMaskToPaletteColor(hsv, out int r, out int g, out int b);
            return System.Drawing.Color.FromArgb(r, g, b);
        }

        public static System.Windows.Media.Color FromHsvMaskToPaletteMediaColor(this SerializableVector3 hsv)
        {
            FromHsvMaskToPaletteColor(hsv, out int r, out int g, out int b);
            return System.Windows.Media.Color.FromArgb(255, (byte)r, (byte)g, (byte)b);
        }

        /// <summary>
        /// Converts from RGB matching the in game color picker palatte, to Keen's HSV stored format.
        /// </summary>
        /// <param name="r">the System RGB color</param>
        /// <param name="g">the System RGB color</param>
        /// <param name="b">the System RGB color</param>
        /// <returns>the HSV stored value.</returns>
        /// <remarks>sourced from wikipedia</remarks>
        private static SerializableVector3 FromPaletteColorToHsvMask(decimal r, decimal g, decimal b)
        {
            decimal max = Math.Max(r, Math.Max(g, b));
            decimal min = Math.Min(r, Math.Min(g, b));
            decimal chroma = max - min;
            decimal hue1 = chroma == 0 ? 0 :
                           max == r ? (g - b) / chroma % 6 :
                           max == g ? (b - r) / chroma + 2 :
                           ((r - g) / chroma) + 4;


            decimal hue = 60 * hue1;
            decimal value = max;
            decimal saturation = 0;

            if (value != 0)
                saturation = chroma / value;

            return new((float)hue / 360, (float)saturation - MyColorPickerConstants.SATURATION_DELTA,
                       (float)value - MyColorPickerConstants.VALUE_DELTA + MyColorPickerConstants.VALUE_COLORIZE_DELTA);

        }

        public static SerializableVector3 FromPaletteColorToHsvMask(this System.Drawing.Color color)
        {
            return FromPaletteColorToHsvMask((decimal)color.R / 255,
                                             (decimal)color.G / 255,
                                             (decimal)color.B / 255);

        }

        public static SerializableVector3 FromPaletteColorToHsvMask(this System.Windows.Media.Color color)
        {
            return FromPaletteColorToHsvMask((decimal)color.R / 255,
                                             (decimal)color.G / 255,
                                             (decimal)color.B / 255);

        }

        /// <summary>
        /// Returns block size.
        /// </summary>
        /// <remarks>see: http://spaceengineerswiki.com/index.php?title=FAQs
        /// Why are the blocks 0.5 and 2.5 meter blocks?
        /// </remarks>
        /// <param name="cubeSize"></param>
        /// <returns></returns>
        public static float ToLength(this MyCubeSize cubeSize)
        {
            return MyDefinitionManager.Static.GetCubeSize(cubeSize);
        }

        public static MyFixedPoint ToFixedPoint(this decimal value)
        {
            return MyFixedPoint.DeserializeString(value.ToString(CultureInfo.InvariantCulture));
        }

        public static MyFixedPoint ToFixedPoint(this double value)
        {
            return MyFixedPoint.DeserializeString(value.ToString(CultureInfo.InvariantCulture));
        }

        public static MyFixedPoint ToFixedPoint(this float value)
        {
            return MyFixedPoint.DeserializeString(value.ToString(CultureInfo.InvariantCulture));
        }

        public static MyFixedPoint ToFixedPoint(this int value)
        {
            return MyFixedPoint.DeserializeString(value.ToString(CultureInfo.InvariantCulture));
        }

        public static Vector3D? IntersectsRayAt(this BoundingBoxD boundingBox, Vector3D position, Vector3D rayTo)
        {
            int[][] triangles = [
                [2,1,0],
                [3,2,0],
                [4,5,6],
                [4,6,7],
                [0,1,5],
                [0,5,4],
                [7,6,2],
                [7,2,3],
                [0,4,7],
                [0,7,3],
                [5,1,2],
                [5,2,6]];

            foreach (int[] triangle in triangles)
            {

                Vector3D c0 = boundingBox.GetCorner(triangle[0]);
                Vector3D c1 = boundingBox.GetCorner(triangle[1]);
                Vector3D c2 = boundingBox.GetCorner(triangle[2]);
                Media3D.Point3DCollection vectors = [];
                foreach (var vector in new Vector3D[] { c0, c1, c2 })
                {
                    vectors.Add(new(vector.X, vector.Y, vector.Z));
                }

                if (MeshHelper.RayIntersectTriangleRound(vectors, position.ToPoint3D(), rayTo.ToPoint3D(),
                                                         out Point3D intersection, out int normal))
                {
                    return intersection.ToVector3D();
                }
            }
            return null;
        }

        public static SerializableVector3UByte Transform(this SerializableVector3UByte value, Quaternion rotation)
        {
            Vector3I vector = Vector3I.Transform(new(value.X - 127, value.Y - 127, value.Z - 127), rotation);
            return new SerializableVector3UByte((byte)(vector.X + 127),
                                                (byte)(vector.Y + 127),
                                                (byte)(vector.Z + 127));

        }

        public static Vector3D Transform(this Vector3D value, QuaternionD rotation)
        {
            double x2 = rotation.X * 2;
            double y2 = rotation.Y * 2;
            double z2 = rotation.Z * 2;
            double xx = rotation.X * x2;
            double xy = rotation.X * y2;
            double xz = rotation.X * z2;
            double yy = rotation.Y * y2;
            double yz = rotation.Y * z2;
            double zz = rotation.Z * z2;
            double wx = rotation.W * x2;
            double wy = rotation.W * y2;
            double wz = rotation.W * z2;
            double x = value.X * (1.0 - yy - zz) + value.Y * (xy - wz) + value.Z * (xz + wy);
            double y = value.X * (xy + wz) + value.Y * (1.0 - xx - zz) + value.Z * (yz - wx);
            double z = value.X * (xz - wy) + value.Y * (yz + wx) + value.Z * (1.0 - xx - yy);
            Vector3D result = new(x, y, z);
            return result;
        }

        public static int Read7BitEncodedInt(this BinaryReader reader)
        {
            int num = 0;
            int num2 = 0;
            while (num2 != 35)
            {
                byte b = reader.ReadByte();
                num |= (b & 127) << num2;
                num2 += 7;
                if ((b & 128) == 0)
                {
                    return num;
                }
            }
            return -1;
        }

        public static ObservableCollection<InventoryEditorModel> GetInventory(this MyObjectBuilder_EntityBase objectBuilderBase, MyCubeBlockDefinition definition = null)
        {
            ObservableCollection<InventoryEditorModel> inventoryEditors = [];
            var inventoryBase = objectBuilderBase.ComponentContainer.Components.FirstOrDefault(e => e.TypeId == "MyInventoryBase");
            var inventoryBaseComponent = inventoryBase.Component;
            if (objectBuilderBase.ComponentContainer == null || inventoryBaseComponent == null)
            {
                return inventoryEditors;
            }
            if (inventoryBaseComponent is MyObjectBuilder_Inventory singleInventory)
            {
                if (ParseInventory(singleInventory, definition) is InventoryEditorModel iem)
                {
                    inventoryEditors.Add(iem);
                }
            }
            if (inventoryBaseComponent is MyObjectBuilder_InventoryAggregate aggregate)
            {
                foreach (var field in aggregate.Inventories.OfType<MyObjectBuilder_Inventory>())
                {
                    if (ParseInventory(field, definition) is InventoryEditorModel iem)
                    {
                        inventoryEditors.Add(iem);
                    }
                }
            }
            return inventoryEditors;
        }

        public static List<MyObjectBuilder_Character> GetHierarchyCharacters(this MyObjectBuilder_CubeBlock cube)
        {
            List<MyObjectBuilder_Character> list = [];

            if (cube is not MyObjectBuilder_Cockpit cockpit)
                return list;

            if (cockpit.ComponentContainer?.Components
                       .FirstOrDefault(e => e.TypeId == "MyHierarchyComponentBase")?.Component is MyObjectBuilder_HierarchyComponentBase hierarchyBase)
            {
                list.AddRange(hierarchyBase.Children.Where(e => e is MyObjectBuilder_Character).Cast<MyObjectBuilder_Character>());
            }
            return list;
        }

        /// <summary>
        /// Removes all sign of a pilot/characrter from a cockpit cube.
        /// </summary>
        /// <param name="cockpit">The specific cube.</param>
        /// <param name="character">Specific character to remove, if required, otherwise ANY chararcter will be removed.</param>
        /// <returns>Returns true if a character was removed.</returns>
        public static bool RemoveHierarchyCharacter(this MyObjectBuilder_Cockpit cockpit, MyObjectBuilder_Character character = null)
        {
            bool retValue = false;

            MyObjectBuilder_ComponentContainer.ComponentData hierarchyComponentBase = cockpit.ComponentContainer?.Components?.FirstOrDefault(e => e.TypeId == "MyHierarchyComponentBase");

            if (hierarchyComponentBase?.Component is MyObjectBuilder_HierarchyComponentBase hierarchyBase && hierarchyBase.Children.Count > 0)
            {
                for (int i = 0; i < hierarchyBase.Children.Count; i++)
                {
                    var charHierarchy = hierarchyBase.Children[i];
                    int index = hierarchyBase.Children.IndexOf(character ?? charHierarchy as MyObjectBuilder_Character);

                    if (index != -1)
                    {
                        retValue = true;
                        hierarchyBase.Children.RemoveAt(index);
                    }
                }
                if (hierarchyBase.Children.Count == 0)
                {
                    cockpit.ComponentContainer.Components.Remove(hierarchyComponentBase);
                }
            }
            if (retValue && cockpit != null)
            {
                cockpit.ClearPilotAndAutopilot();
                cockpit.PilotRelativeWorld = null; // This should also clear Pilot.
                cockpit.Pilot = null;
            }
            return retValue;
        }

        /// <summary>
        /// Remove all pilots, co-pilots and any other character entities from consoles, cockpits and passenger seats.
        /// </summary>
        public static void RemoveHierarchyCharacter(this MyObjectBuilder_CubeGrid cubeGrid)
        {
            cubeGrid.CubeBlocks.Where(c => c.TypeId == MOBTypeIds.Cockpit).Select(c => // Cockpit
            {
                ((MyObjectBuilder_Cockpit)c).RemoveHierarchyCharacter();
                return c;
            }).ToArray();
        }

        public static ObservableCollection<InventoryEditorModel> GetInventory(this MyObjectBuilder_ComponentContainer componentContainer, MyCubeBlockDefinition definition = null)
        {
            ObservableCollection<InventoryEditorModel> inventoryEditors = [];

            if (componentContainer != null)
            {
                var inventoryBase = componentContainer.Components.FirstOrDefault(e => e.TypeId == "MyInventoryBase");

                if (inventoryBase?.Component is MyObjectBuilder_Inventory singleInventory && singleInventory != null)
                {
                    InventoryEditorModel iem = ParseInventory(singleInventory, definition);
                    if (iem != null)
                        inventoryEditors.Add(iem);
                }

                if (inventoryBase.Component is MyObjectBuilder_InventoryAggregate aggregate)
                {
                    foreach (var inventory in aggregate.Inventories)
                    {
                        var iem = ParseInventory(inventory as MyObjectBuilder_Inventory, definition);
                        if (iem != null)
                            inventoryEditors.Add(iem);
                    }
                }
            }

            return inventoryEditors;
        }

        private static InventoryEditorModel ParseInventory(MyObjectBuilder_Inventory inventory, MyCubeBlockDefinition definition, MyObjectBuilder_Character character = null)
        {
            if (inventory == null)
                return null;
            float volumeMultiplier = 1f; // Unsure if there should be a default of 1 if there isn't a InventorySize defined.

            if (definition == null)
                volumeMultiplier = 0.4f;
            else
            {
                var definitionType = definition.GetType();
                var invSizeField = definitionType.GetField("InventorySize");
                var inventoryMaxVolumeField = definitionType.GetField("InventoryMaxVolume");

                var invSize = (Vector3)invSizeField?.GetValue(definition);
                volumeMultiplier = invSize.X * invSize.Y * invSize.Z;

                var maxSize = (float)inventoryMaxVolumeField?.GetValue(definition);
                volumeMultiplier = MathHelper.Min(volumeMultiplier, maxSize);

            }

            var settings = SpaceEngineersCore.WorldResource.Checkpoint.Settings;
            return new(inventory, volumeMultiplier * 1000 * settings.InventorySizeMultiplier, character ?? throw new ArgumentNullException(nameof(character)));
        }

        public static List<MyGasProperties> GetGasDefinitions(this MyDefinitionManager definitionManager)
        {
            return [.. definitionManager.GetAllDefinitions().Where(e => e.Id.TypeId == typeof(VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties)).Cast<MyGasProperties>()];
        }

        public static MyDefinitionBase GetDefinition(this MyDefinitionManager definitionManager, MyObjectBuilderType typeId, string subTypeId)
        {
            return definitionManager.GetAllDefinitions().FirstOrDefault(e => e.Id.TypeId == typeId && e.Id.SubtypeName == subTypeId);
        }

        public static string GetVoxelDisplayTexture(this MyVoxelMaterialDefinition voxelMaterialDefinition)
        {
            string texture = voxelMaterialDefinition.RenderParams.TextureSets[0].ColorMetalXZnY ?? null;

            texture ??= voxelMaterialDefinition.RenderParams.TextureSets[0].NormalGlossXZnY;
            // The VoxelHandPreview texture is oddly shaped, and not suitable for SEToolbox.
            // It is a texture of last resort.
            texture ??= voxelMaterialDefinition.VoxelHandPreview;

            return texture;
        }

        public static void GetMaterialContent(this VRage.Game.Voxels.IMyStorage self, ref Vector3I voxelCoords, out byte material, out byte content)
        {
            MyStorageData myStorageData = new(MyStorageDataTypeFlags.ContentAndMaterial);
            myStorageData.Resize(Vector3I.One);
            myStorageData.ClearMaterials(0);
            self.ReadRange(myStorageData, MyStorageDataTypeFlags.ContentAndMaterial, 0, voxelCoords, voxelCoords);

            material = myStorageData.Material(0);
            content = myStorageData.Content(0);
        }

        public static int Max(int a, int b, int c, int d)
        {
            int abMax = a > b ? a : b;
            int cdMax = c > d ? c : d;
            return abMax > cdMax ? abMax : cdMax;
        }
    }
}