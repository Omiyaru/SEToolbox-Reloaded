using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using BulletXNA.BulletCollision;
using VRage.Import;
using VRageMath;
using VRageMath.PackedVector;
using VRageRender.Animations;
using VRageRender.Import;

namespace SEToolbox.Interop.Models
{
    public static class MyModel
    {
        #region LoadModelData

        public static Dictionary<string, object> LoadModelData(string fileName)
        {
            MyModelImporter model = new();
            model.ImportData(fileName);
            return model.GetTagData();
        }

        /// <summary>
        /// Load Model Data
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static Dictionary<string, object> LoadCustomModelData(string fileName)
        {
            Dictionary<string, object> data = [];

            using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                var reader = new BinaryReader(stream);
                try
                {
                    LoadTagData(reader, data);
                }
                catch
                {
                    // Ignore errors
                }
            }

            return data;
        }

        #endregion

        #region SaveModelData

        public static void SaveModelData(string fileName, Dictionary<string, object> data)
        {
            MethodInfo[] methods = typeof(MyModel).GetMethods(BindingFlags.Static | BindingFlags.NonPublic);

            using FileStream fileStream = new(fileName, FileMode.Create);
            BinaryWriter writer = new(fileStream);
            foreach (KeyValuePair<string, object> kvp in data)
            {
                MethodInfo method = methods.FirstOrDefault(m => m.Name.Equals("ExportData") && m.GetParameters().Length > 2 && m.GetParameters()[2].ParameterType == kvp.Value.GetType());

                method?.Invoke(null, [writer, kvp.Key, kvp.Value]);
                
                method = methods.FirstOrDefault(m => m.Name.Equals("ExportData") && m.GetParameters().Length > 2 && m.GetParameters()[2].ParameterType == kvp.Value.GetType().MakeByRefType());
                method?.Invoke(null, [writer, kvp.Key, kvp.Value]);
            }
        }


        #endregion

        #region Write Helpers

        private static void WriteBone(this BinaryWriter writer, ref MyModelBone bone)
        {
            writer.Write(bone.Name);
            writer.Write(bone.Parent);
            WriteMatrix(writer, ref bone.Transform);
        }

        /// <summary>
        /// WriteTag
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="tagName"></param>
        private static void WriteTag(this BinaryWriter writer, string tagName)
        {
            writer.Write(tagName);
        }

        /// <summary>
        /// WriteVector3
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="vector"></param>
        private static void WriteVector3(this BinaryWriter writer, ref Vector3 vector)
        {
            writer.Write(vector.X);
            writer.Write(vector.Y);
            writer.Write(vector.Z);
        }

        /// <summary>
        /// WriteVector4
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="vector"></param>
        private static void WriteVector4(this BinaryWriter writer, ref Vector4 vector)
        {
            writer.Write(vector.X);
            writer.Write(vector.Y);
            writer.Write(vector.Z);
            writer.Write(vector.W);
        }

        /// <summary>
        /// WriteVector3I
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="vector"></param>
        private static void WriteVector3I(this BinaryWriter writer, ref Vector3I vector)
        {
            writer.Write(vector.X);
            writer.Write(vector.Y);
            writer.Write(vector.Z);
        }

        /// <summary>
        /// WriteVector4I
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="vector"></param>
        private static void WriteVector4I(this BinaryWriter writer, ref Vector4I vector)
        {
            writer.Write(vector.X);
            writer.Write(vector.Y);
            writer.Write(vector.Z);
            writer.Write(vector.W);
        }

        /// <summary>
        /// WriteVector2
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="vector"></param>
        private static void WriteVector2(this BinaryWriter writer, ref Vector2 vector)
        {
            writer.Write(vector.X);
            writer.Write(vector.Y);
        }

        /// <summary>
        /// WriteMatrix
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="matrix"></param>
        private static void WriteMatrix(this BinaryWriter writer, ref Matrix matrix)
        {
            writer.Write(matrix.M11);
            writer.Write(matrix.M12);
            writer.Write(matrix.M13);
            writer.Write(matrix.M14);

            writer.Write(matrix.M21);
            writer.Write(matrix.M22);
            writer.Write(matrix.M23);
            writer.Write(matrix.M24);

            writer.Write(matrix.M31);
            writer.Write(matrix.M32);
            writer.Write(matrix.M33);
            writer.Write(matrix.M34);

            writer.Write(matrix.M41);
            writer.Write(matrix.M42);
            writer.Write(matrix.M43);
            writer.Write(matrix.M44);
        }

        /// <summary>
        /// Write HalfVector4
        /// </summary>
        private static void WriteHalfVector4(this BinaryWriter writer, ref HalfVector4 value)
        {
            writer.Write(value.PackedValue);
        }

        /// <summary>
        /// Write HalfVector2
        /// </summary>
        private static void WriteHalfVector2(this BinaryWriter writer, ref HalfVector2 value)
        {
            writer.Write(value.PackedValue);
        }

        /// <summary>
        /// Write Byte4
        /// </summary>
        private static void WriteByte4(this BinaryWriter writer, ref Byte4 val)
        {
            writer.Write(val.PackedValue);
        }

        #endregion

        #region Export Data Packers

        private static bool ExportDataPackedAsHV4(this BinaryWriter writer, string tagName, Vector3[] vectorArray)
        {
            WriteTag(writer, tagName);

            if (vectorArray == null)
            {
                writer.Write(0);
                return true;
            }

            writer.Write(vectorArray.Length);
            foreach (Vector3 vectorVal in vectorArray)
            {
                Vector3 v = vectorVal;
                HalfVector4 vector = VF_Packer.PackPosition(ref v);
                WriteHalfVector4(writer, ref vector);
            }

            return true;
        }

        private static bool ExportDataPackedAsHV2(this BinaryWriter writer, string tagName, Vector2[] vectorArray)
        {
            WriteTag(writer, tagName);

            if (vectorArray == null)
            {
                writer.Write(0);
                return true;
            }

            writer.Write(vectorArray.Length);
            foreach (Vector2 vectorVal in vectorArray)
            {
                HalfVector2 vector = new(vectorVal);
                WriteHalfVector2(writer, ref vector);
            }

            return true;
        }

        private static bool ExportDataPackedAsB4(this BinaryWriter writer, string tagName, Vector3[] vectorArray)
        {
            WriteTag(writer, tagName);

            if (vectorArray == null)
            {
                writer.Write(0);
                return true;
            }

            writer.Write(vectorArray.Length);
            foreach (Vector3 vectorVal in vectorArray)
            {
                Vector3 v = vectorVal;
                Byte4 vector = new()
                {
                    PackedValue = VF_Packer.PackNormal(ref v)
                };
                WriteByte4(writer, ref vector);
            }

            return true;
        }

        private static bool ExportData(this BinaryWriter writer, string tagName, HalfVector4[] vectorArray)
        {
            WriteTag(writer, tagName);

            if (vectorArray == null)
            {
                writer.Write(0);
                return true;
            }

            writer.Write(vectorArray.Length);
            foreach (HalfVector4 vectorVal in vectorArray)
            {
                writer.Write(vectorVal.PackedValue);
            }

            return true;
        }

        private static bool ExportData(this BinaryWriter writer, string tagName, HalfVector2[] vectorArray)
        {
            WriteTag(writer, tagName);

            if (vectorArray == null)
            {
                writer.Write(0);
                return true;
            }

            writer.Write(vectorArray.Length);
            foreach (HalfVector2 vectorVal in vectorArray)
            {
                writer.Write(vectorVal.PackedValue);
            }

            return true;
        }

        private static bool ExportData(this BinaryWriter writer, string tagName, Byte4[] vectorArray)
        {
            WriteTag(writer, tagName);

            if (vectorArray == null)
            {
                writer.Write(0);
                return true;
            }

            writer.Write(vectorArray.Length);
            foreach (Byte4 vectorVal in vectorArray)
            {
                writer.Write(vectorVal.PackedValue);
            }

            return true;
        }

        /// <summary>
        /// ExportData
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="tagName"></param>
        /// <param name="vectorArray"></param>
        /// <returns></returns>
        private static bool ExportData(this BinaryWriter writer, string tagName, Vector3[] vectorArray)
        {
            if (vectorArray == null)
                return true;

            WriteTag(writer, tagName);
            writer.Write(vectorArray.Length);
            foreach (Vector3 vectorVal in vectorArray)
            {
                Vector3 vector = vectorVal;
                WriteVector3(writer, ref vector);
            }

            return true;
        }

        /// <summary>
        /// ExportData
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="tagName"></param>
        /// <param name="vectorArray"></param>
        /// <returns></returns>
        private static bool ExportData(this BinaryWriter writer, string tagName, Vector4[] vectorArray)
        {
            if (vectorArray == null)
                return true;

            WriteTag(writer, tagName);
            writer.Write(vectorArray.Length);
            foreach (Vector4 vectorVal in vectorArray)
            {
                Vector4 vector = vectorVal;
                WriteVector4(writer, ref vector);
            }

            return true;
        }

        /// <summary>
        /// ExportData
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="tagName"></param>
        /// <param name="vectorArray"></param>
        /// <returns></returns>
        private static bool ExportData(this BinaryWriter writer, string tagName, Vector3I[] vectorArray)
        {
            if (vectorArray == null)
                return true;

            WriteTag(writer, tagName);
            writer.Write(vectorArray.Length);
            foreach (Vector3I vectorVal in vectorArray)
            {
                Vector3I vector = vectorVal;
                WriteVector3I(writer, ref vector);
            }

            return true;
        }

        /// <summary>
        /// ExportData
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="tagName"></param>
        /// <param name="vectorArray"></param>
        /// <returns></returns>
        private static bool ExportData(this BinaryWriter writer, string tagName, Vector4I[] vectorArray)
        {
            if (vectorArray == null)
                return true;

            WriteTag(writer, tagName);
            writer.Write(vectorArray.Length);
            foreach (Vector4I vectorVal in vectorArray)
            {
                Vector4I vector = vectorVal;
                WriteVector4I(writer, ref vector);
            }

            return true;
        }

        /// <summary>
        /// ExportData
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="tagName"></param>
        /// <param name="matrixArray"></param>
        /// <returns></returns>
        private static bool ExportData(this BinaryWriter writer, string tagName, Matrix[] matrixArray)
        {
            if (matrixArray == null)
                return true;

            WriteTag(writer, tagName);
            writer.Write(matrixArray.Length);
            foreach (Matrix matVal in matrixArray)
            {
                Matrix mat = matVal;
                WriteMatrix(writer, ref mat);
            }

            return true;
        }

        /// <summary>
        /// ExportData
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="tagName"></param>
        /// <param name="vectorArray"></param>
        /// <returns></returns>
        private static bool ExportData(this BinaryWriter writer, string tagName, Vector2[] vectorArray)
        {
            WriteTag(writer, tagName);

            if (vectorArray == null)
            {
                writer.Write(0);
                return true;
            }

            writer.Write(vectorArray.Length);
            foreach (Vector2 vectorVal in vectorArray)
            {
                Vector2 vector = vectorVal;
                WriteVector2(writer, ref vector);
            }

            return true;
        }

        /// <summary>
        /// ExportData
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="tagName"></param>
        /// <param name="stringArrayay"></param>
        /// <returns></returns>
        private static bool ExportData(this BinaryWriter writer, string tagName, string[] stringArrayay)
        {
            WriteTag(writer, tagName);

            if (stringArrayay == null)
            {
                writer.Write(0);
                return true;
            }

            writer.Write(stringArrayay.Length);
            foreach (string sVal in stringArrayay)
                writer.Write(sVal);

            return true;
        }


        /// <summary>
        /// ExportData
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="tagName"></param>
        /// <param name="intArray"></param>
        /// <returns></returns>
        private static bool ExportData(this BinaryWriter writer, string tagName, int[] intArray)
        {
            WriteTag(writer, tagName);

            if (intArray == null)
            {
                writer.Write(0);
                return true;
            }

            writer.Write(intArray.Length);
            foreach (int iVal in intArray)
                writer.Write(iVal);

            return true;
        }

        private static bool ExportData(this BinaryWriter writer, string tagName, byte[] byteArray)
        {
            WriteTag(writer, tagName);

            if (byteArray == null)
            {
                writer.Write(0);
                return true;
            }

            writer.Write(byteArray.Length);
            writer.Write(byteArray);
            return true;
        }

        private static bool ExportData(this BinaryWriter writer, string tagName, MyModelInfo modelInfo)
        {
            WriteTag(writer, tagName);

            writer.Write(modelInfo.TrianglesCount);
            writer.Write(modelInfo.VerticesCount);
            WriteVector3(writer, ref modelInfo.BoundingBoxSize);
            return true;
        }


        /// <summary>
        /// ExportData
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="tagName"></param>
        /// <param name="boundingBox"></param>
        /// <returns></returns>
        private static bool ExportData(this BinaryWriter writer, string tagName, ref BoundingBox boundingBox)
        {
            WriteTag(writer, tagName);
            WriteVector3(writer, ref boundingBox.Min);
            WriteVector3(writer, ref boundingBox.Max);
            return true;
        }

        /// <summary>
        /// ExportData
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="tagName"></param>
        /// <param name="boundingSphere"></param>
        /// <returns></returns>
        private static bool ExportData(this BinaryWriter writer, string tagName, ref BoundingSphere boundingSphere)
        {
            WriteTag(writer, tagName);
            WriteVector3(writer, ref boundingSphere.Center);
            writer.Write(boundingSphere.Radius);
            return true;
        }

        /// <summary>
        /// ExportData
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="tagName"></param>
        /// <param name="bvh"></param>
        /// <returns></returns>
        private static bool ExportData(this BinaryWriter writer, string tagName, ref GImpactQuantizedBvh bvh)
        {
            WriteTag(writer, tagName);

            byte[] buffer = bvh.Save();

            writer.Write(buffer.Length);
            writer.Write(bvh.Save());

            return true;
        }

        private static bool ExportData(this BinaryWriter writer, string tagName, ref ModelAnimations animations)
        {
            WriteTag(writer, tagName);

            writer.Write(animations.Clips.Count);

            foreach (MyAnimationClip clip in animations.Clips)
            {
                writer.Write(clip.Name);
                writer.Write(clip.Duration);
                writer.Write(clip.Bones.Count);

                foreach (MyAnimationClip.Bone bone in clip.Bones)
                {
                    writer.Write(bone.Name);
                    writer.Write(bone.Keyframes.Count);

                    foreach (MyAnimationClip.Keyframe keyframe in bone.Keyframes)
                    {
                        writer.Write(keyframe.Time);
                        Vector4 rotation = keyframe.Rotation.ToVector4();
                        writer.WriteVector4(ref rotation);
                        writer.WriteVector3(ref keyframe.Translation);
                    }
                }
            }

            writer.Write(animations.Skeleton.Count);

            foreach (int skeleton in animations.Skeleton)
                writer.Write(skeleton);

            return true;
        }

        private static bool ExportData(this BinaryWriter writer, string tagName, MyModelBone[] boneArray)
        {
            WriteTag(writer, tagName);
            writer.Write(boneArray.Length);

            foreach (MyModelBone boneVal in boneArray)
            {
                MyModelBone bone = boneVal;
                WriteBone(writer, ref bone);
            }

            return true;
        }

        private static bool ExportData(this BinaryWriter writer, string tagName, MyLODDescriptor[] lodArray)
        {
            WriteTag(writer, tagName);
            writer.Write(lodArray.Length);

            foreach (MyLODDescriptor lodVal in lodArray)
            {
                lodVal.Write(writer);
            }

            return true;
        }

        /// <summary>
        /// ExportData
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="tagName"></param>
        /// <param name="dict"></param>
        /// <returns></returns>
        private static bool ExportData(this BinaryWriter writer, string tagName, Dictionary<string, Matrix> dict)
        {
            WriteTag(writer, tagName);
            writer.Write(dict.Count);
            foreach (KeyValuePair<string, Matrix> pair in dict)
            {
                writer.Write(pair.Key);
                Matrix mat = pair.Value;
                WriteMatrix(writer, ref mat);
            }
            return true;
        }

        /// <summary>
        /// ExportData
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="tagName"></param>
        /// <param name="dict"></param>
        /// <returns></returns>
        private static bool ExportData(this BinaryWriter writer, string tagName, Dictionary<int, MyMeshPartInfo> dict)
        {
            WriteTag(writer, tagName);
            writer.Write(dict.Count);
            foreach (KeyValuePair<int, MyMeshPartInfo> pair in dict)
            {
                MyMeshPartInfo meshInfo = pair.Value;
                meshInfo.Export(writer);
            }

            return true;
        }

        private static bool ExportData(this BinaryWriter writer, string tagName, List<MyMeshPartInfo> list)
        {
            WriteTag(writer, tagName);
            writer.Write(list.Count);
            foreach (MyMeshPartInfo meshInfo in list)
            {
                meshInfo.Export(writer);
            }

            return true;
        }

        /// <summary>
        /// ExportData
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="tagName"></param>
        /// <param name="dict"></param>
        /// <returns></returns>
        private static bool ExportData(this BinaryWriter writer, string tagName, Dictionary<string, MyModelDummy> dict)
        {
            WriteTag(writer, tagName);
            writer.Write(dict.Count);
            foreach (KeyValuePair<string, MyModelDummy> pair in dict)
            {
                writer.Write(pair.Key);
                Matrix mat = pair.Value.Matrix;
                WriteMatrix(writer, ref mat);

                writer.Write(pair.Value.CustomData.Count);
                foreach (KeyValuePair<string, object> customDataPair in pair.Value.CustomData)
                {
                    writer.Write(customDataPair.Key);
                    writer.Write(customDataPair.Value.ToString());
                }
            }
            return true;
        }

        /// <summary>
        /// ExportFloat
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="tagName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool ExportData(this BinaryWriter writer, string tagName, float value)
        {
            WriteTag(writer, tagName);
            writer.Write(value);
            return true;
        }

        /// <summary>
        /// ExportFloat
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="tagName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool ExportData(this BinaryWriter writer, string tagName, bool value)
        {
            WriteTag(writer, tagName);
            writer.Write(value);
            return true;
        }

        #endregion

        #region Read Helpers

        /// <summary>
        /// Read HalfVector4
        /// </summary>
        private static HalfVector4 ReadHalfVector4(BinaryReader reader)
        {
            return new HalfVector4 { PackedValue = reader.ReadUInt64() };
        }

        /// <summary>
        /// Read HalfVector2
        /// </summary>
        private static HalfVector2 ReadHalfVector2(BinaryReader reader)
        {
            return new HalfVector2 { PackedValue = reader.ReadUInt32() };
        }

        /// <summary>
        /// Read Byte4
        /// </summary>
        private static Byte4 ReadByte4(BinaryReader reader)
        {
            return new Byte4 { PackedValue = reader.ReadUInt32() };
        }

        /// <summary>
        /// ReadVector3
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private static Vector3 ReadVector3(BinaryReader reader)
        {
            Vector3 vector;
            vector.X = reader.ReadSingle();
            vector.Y = reader.ReadSingle();
            vector.Z = reader.ReadSingle();
            return vector;
        }

        /// <summary>
        /// ReadVector3I
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private static Vector3I ReadVector3I(BinaryReader reader)
        {
            Vector3I vector;
            vector.X = reader.ReadInt32();
            vector.Y = reader.ReadInt32();
            vector.Z = reader.ReadInt32();
            return vector;
        }

        /// <summary>
        /// ReadVector4
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private static Vector4 ReadVector4(BinaryReader reader)
        {
            Vector4 vector;
            vector.X = reader.ReadSingle();
            vector.Y = reader.ReadSingle();
            vector.Z = reader.ReadSingle();
            vector.W = reader.ReadSingle();
            return vector;
        }

        /// <summary>
        /// ReadVector4I
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private static Vector4I ReadVector4I(BinaryReader reader)
        {
            Vector4I vector;
            vector.X = reader.ReadInt32();
            vector.Y = reader.ReadInt32();
            vector.Z = reader.ReadInt32();
            vector.W = reader.ReadInt32();
            return vector;
        }

        /// <summary>
        /// ReadVector2
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private static Vector2 ReadVector2(BinaryReader reader)
        {
            Vector2 vector;
            vector.X = reader.ReadSingle();
            vector.Y = reader.ReadSingle();
            return vector;
        }

        /// <summary>
        /// Read array of Vector3
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private static Vector3[] ReadArrayOfVector3(BinaryReader reader)
        {
            int nCount = reader.ReadInt32();
            Vector3[] vectorArray = new Vector3[nCount];
            for (int i = 0; i < nCount; ++i)
            {
                vectorArray[i] = ReadVector3(reader);
            }

            return vectorArray;
        }

        /// <summary>
        /// Read array of Vector3I
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private static Vector3I[] ReadArrayOfVector3I(BinaryReader reader)
        {
            int nCount = reader.ReadInt32();
            Vector3I[] vectorArray = new Vector3I[nCount];
            for (int i = 0; i < nCount; ++i)
            {
                vectorArray[i] = ReadVector3I(reader);
            }

            return vectorArray;
        }

        /// <summary>
        /// Read array of Vector4
        /// </summary>
        private static Vector4[] ReadArrayOfVector4(BinaryReader reader)
        {
            int nCount = reader.ReadInt32();
            Vector4[] vectorArray = new Vector4[nCount];

            for (int i = 0; i < nCount; ++i)
            {
                vectorArray[i] = ReadVector4(reader);
            }

            return vectorArray;
        }

        private static Vector4I[] ReadArrayOfVector4I(BinaryReader reader)
        {
            int nCount = reader.ReadInt32();
            Vector4I[] vectorArray = new Vector4I[nCount];

            for (int i = 0; i < nCount; ++i)
            {
                vectorArray[i] = ReadVector4I(reader);
            }

            return vectorArray;
        }
        /// Read array of HalfVector4
        /// </summary>
        private static HalfVector4[] ReadArrayOfHalfVector4(BinaryReader reader)
        {
            int nCount = reader.ReadInt32();
            HalfVector4[] vectorArray = new HalfVector4[nCount];

            for (int i = 0; i < nCount; ++i)
            {
                vectorArray[i] = ReadHalfVector4(reader);
            }

            return vectorArray;
        }

        /// <summary>
        /// Read array of Byte4
        /// </summary>
        private static Byte4[] ReadArrayOfByte4(BinaryReader reader)
        {
            int nCount = reader.ReadInt32();
            Byte4[] vectorArray = new Byte4[nCount];

            for (int i = 0; i < nCount; ++i)
            {
                vectorArray[i] = ReadByte4(reader);
            }

            return vectorArray;
        }

        /// <summary>
        /// Read array of HalfVector2
        /// </summary>
        private static HalfVector2[] ReadArrayOfHalfVector2(BinaryReader reader)
        {
            int nCount = reader.ReadInt32();
            HalfVector2[] vectorArray = new HalfVector2[nCount];

            for (int i = 0; i < nCount; ++i)
            {
                vectorArray[i] = ReadHalfVector2(reader);
            }

            return vectorArray;
        }


        /// <summary>
        /// Read array of Vector2
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private static Vector2[] ReadArrayOfVector2(BinaryReader reader)
        {
            int nCount = reader.ReadInt32();
            Vector2[] vectorArray = new Vector2[nCount];
            for (int i = 0; i < nCount; ++i)
            {
                vectorArray[i] = ReadVector2(reader);
            }

            return vectorArray;
        }

        /// <summary>
        /// Read array of String
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private static string[] ReadArrayOfString(BinaryReader reader)
        {
            int nCount = reader.ReadInt32();
            string[] stringArray = new string[nCount];
            for (int i = 0; i < nCount; ++i)
            {
                stringArray[i] = reader.ReadString();
            }

            return stringArray;
        }

        /// <summary>
        /// ReadBoundingBox
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private static BoundingBox ReadBoundingBox(BinaryReader reader)
        {
            BoundingBox boundingBox;
            boundingBox.Min = ReadVector3(reader);
            boundingBox.Max = ReadVector3(reader);
            return boundingBox;
        }

        /// <summary>
        /// ReadBoundingSphere
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private static BoundingSphere ReadBoundingSphere(BinaryReader reader)
        {
            BoundingSphere boundingSphere;
            boundingSphere.Center = ReadVector3(reader);
            boundingSphere.Radius = reader.ReadSingle();
            return boundingSphere;
        }

        /// <summary>
        /// ReadMatrix
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private static Matrix ReadMatrix(BinaryReader reader)
        {
            Matrix matrix;

            matrix.M11 = reader.ReadSingle();
            matrix.M12 = reader.ReadSingle();
            matrix.M13 = reader.ReadSingle();
            matrix.M14 = reader.ReadSingle();

            matrix.M21 = reader.ReadSingle();
            matrix.M22 = reader.ReadSingle();
            matrix.M23 = reader.ReadSingle();
            matrix.M24 = reader.ReadSingle();

            matrix.M31 = reader.ReadSingle();
            matrix.M32 = reader.ReadSingle();
            matrix.M33 = reader.ReadSingle();
            matrix.M34 = reader.ReadSingle();

            matrix.M41 = reader.ReadSingle();
            matrix.M42 = reader.ReadSingle();
            matrix.M43 = reader.ReadSingle();
            matrix.M44 = reader.ReadSingle();

            return matrix;
        }

        /// <summary>
        /// ReadMeshParts
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private static List<MyMeshPartInfo> ReadMeshParts(BinaryReader reader)
        {
            List<MyMeshPartInfo> list = [];
            int nCount = reader.ReadInt32();
            for (int i = 0; i < nCount; ++i)
            {
                MyMeshPartInfo meshPart = new();
                int version = reader.ReadInt32();
                meshPart.Import(reader, version);
                meshPart.Import(reader, 0); // TODO: test version detail
                list.Add(meshPart);
            }

            return list;
        }

        private static List<MyMeshSectionInfo> ReadMeshSections(BinaryReader reader)
        {
            List<MyMeshSectionInfo> list = [];
            int nCount = reader.ReadInt32();
            for (int i = 0; i < nCount; ++i)
            {
                MyMeshSectionInfo meshSection = new();
                int version = reader.ReadInt32();
                meshSection.Import(reader, version);
                meshSection.Import(reader, 0); // TODO: test version detail
                list.Add(meshSection);
            }

            return list;
        }

        /// <summary>
        /// ReadDummies
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private static Dictionary<string, MyModelDummy> ReadDummies(BinaryReader reader)
        {
            Dictionary<string, MyModelDummy> dummies = [];
            int nCount = reader.ReadInt32();

            for (int i = 0; i < nCount; ++i)
            {
                string str = reader.ReadString();
                Matrix mat = ReadMatrix(reader);

                Dictionary<string, object> customData = [];
                int customDataCount = reader.ReadInt32();

                for (int j = 0; j < customDataCount; ++j)
                {
                    string name = reader.ReadString();
                    string value = reader.ReadString();
                    customData.Add(name, value);
                }

                MyModelDummy dummy = new() { Matrix = mat, CustomData = customData };
                dummies.Add(str, dummy);
            }

            return dummies;
        }

        /// <summary>
        /// ReadArrayOfInt
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private static int[] ReadArrayOfInt(BinaryReader reader)
        {
            int nCount = reader.ReadInt32();
            int[] intArr = new int[nCount];
            for (int i = 0; i < nCount; ++i)
            {
                intArr[i] = reader.ReadInt32();
            }

            return intArr;
        }

        private static byte[] ReadArrayOfBytes(BinaryReader reader)
        {
            int nCount = reader.ReadInt32();
            byte[] data = reader.ReadBytes(nCount);
            return data;
        }

        private static ModelAnimations ReadModelAnimations(BinaryReader reader)
        {
            ModelAnimations modelAnimations = new() { Clips = [] };
            int animationCount = reader.ReadInt32();

            for (int i = 0; i < animationCount; i++)
            {
                string clipName = reader.ReadString();
                double duration = reader.ReadDouble();
                MyAnimationClip animationClip = new() { Name = clipName, Duration = duration };

                int boneCount = reader.ReadInt32();
                for (int j = 0; j < boneCount; j++)
                {
                    string boneName = reader.ReadString();
                    MyAnimationClip.Bone bone = new() { Name = boneName };
                    int keyFrameCount = reader.ReadInt32();

                    for (int k = 0; k < keyFrameCount; k++)
                    {
                        double time = reader.ReadDouble();
                        Vector4 vector = ReadVector4(reader);
                        Quaternion rotation = new(vector.X, vector.Y, vector.Z, vector.W);
                        Vector3 translation = ReadVector3(reader);
                        bone.Keyframes.Add(new MyAnimationClip.Keyframe() { Time = time, Rotation = rotation, Translation = translation });
                    }

                    animationClip.Bones.Add(bone);
                }

                modelAnimations.Clips.Add(animationClip);
            }

            modelAnimations.Skeleton = [.. ReadArrayOfInt(reader)];
            return modelAnimations;
        }


        private static MyModelBone[] ReadMyModelBoneArray(BinaryReader reader)
        {
            int nCount = reader.ReadInt32();
            MyModelBone[] myModelBoneArray = new MyModelBone[nCount];

            for (int i = 0; i < nCount; i++)
            {
                string name = reader.ReadString();
                int parent = reader.ReadInt32();
                Matrix matrix = ReadMatrix(reader);
                myModelBoneArray[i] = new MyModelBone { Name = name, Parent = parent, Transform = matrix };
            }

            return myModelBoneArray;
        }

        private static MyLODDescriptor[] ReadMyLodDescriptorArray(BinaryReader reader)
        {
            int nCount = reader.ReadInt32();
            MyLODDescriptor[] myLodDescriptorArray = new MyLODDescriptor[nCount];

            for (int i = 0; i < nCount; i++)
            {
                float distance = reader.ReadSingle();
                string model = reader.ReadString();
                string renderQuality = reader.ReadString();
                myLodDescriptorArray[i] = new MyLODDescriptor { Distance = distance, Model = model, RenderQuality = renderQuality };
            }

            return myLodDescriptorArray;
        }

        #endregion
        private static MyModelInfo ReadMyModelInfo(BinaryReader reader)
        {

            int triCount = reader.ReadInt32();
            int vertCount = reader.ReadInt32();
            Vector3 boundingBoxSize = ReadVector3(reader);
            return new MyModelInfo(triCount, vertCount, boundingBoxSize);

        }
        #region Import Data Readers

        /// <summary>
        /// LoadTagData
        /// </summary>
        /// <returns></returns>

        private static void LoadTagData(BinaryReader reader, Dictionary<string, object> data)
        {
            while (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                string tagName = reader.ReadString();

                switch (tagName)
                {

                    case MyImporterConstants.TAG_DEBUG:
                        data.Add(tagName, reader.ReadBoolean());
                        break;

                    case MyImporterConstants.TAG_DUMMIES:
                        data.Add(tagName, ReadDummies(reader));
                        break;

                    case MyImporterConstants.TAG_VERTICES:
                        data.Add(tagName, ReadArrayOfHalfVector4(reader));
                        break;
                    case MyImporterConstants.TAG_TEXCOORDS0:
                    case MyImporterConstants.TAG_TEXCOORDS1:
                        data.Add(tagName, ReadArrayOfHalfVector2(reader));
                        break;

                    case MyImporterConstants.TAG_INDICES:
                        data.Add(tagName, ReadArrayOfInt(reader));
                        break;
                    case MyImporterConstants.TAG_MODEL_INFO:
                        data.Add(tagName, ReadMyModelInfo(reader));
                        break;
                    case MyImporterConstants.TAG_BOUNDING_BOX:
                        data.Add(tagName, ReadBoundingBox(reader));
                        break;
                    case MyImporterConstants.TAG_BOUNDING_SPHERE:
                        data.Add(tagName, ReadBoundingSphere(reader));
                        break;
                    case MyImporterConstants.TAG_MESH_PARTS:
                        data.Add(tagName, ReadMeshParts(reader));
                        break;
                    case MyImporterConstants.TAG_MESH_SECTIONS:
                        data.Add(tagName, ReadMeshSections(reader));
                        break;
                    case MyImporterConstants.TAG_BLENDINDICES:
                        data.Add(tagName, ReadArrayOfVector4I(reader));
                        break;

                    case MyImporterConstants.TAG_BLENDWEIGHTS:
                        data.Add(tagName, ReadArrayOfVector4(reader));
                        break;

                    case MyImporterConstants.TAG_ANIMATIONS:
                        data.Add(tagName, ReadModelAnimations(reader));
                        break;

                    case MyImporterConstants.TAG_BONES:
                        data.Add(tagName, ReadMyModelBoneArray(reader));
                        break;

                    case MyImporterConstants.TAG_BONE_MAPPING:
                        data.Add(tagName, ReadArrayOfVector3I(reader));//readsingle???
                        break;
                    case MyImporterConstants.TAG_LODS:
                        data.Add(tagName, ReadMyLodDescriptorArray(reader));
                        break;
                    case MyImporterConstants.TAG_PATTERN_SCALE:
                    case MyImporterConstants.TAG_RESCALE_FACTOR:
                        data.Add(tagName, reader.ReadSingle());
                        break;
                    case MyImporterConstants.TAG_MODEL_BVH:
                        GImpactQuantizedBvh bvh = new();
                        bvh.Load(ReadArrayOfBytes(reader));
                        data.Add(tagName, bvh);
                        break;
                    case MyImporterConstants.TAG_HAVOK_DESTRUCTION_GEOMETRY:
                    case MyImporterConstants.TAG_HAVOK_COLLISION_GEOMETRY:
                    case MyImporterConstants.TAG_HAVOK_DESTRUCTION:
                        data.Add(tagName, ReadArrayOfBytes(reader));
                        break;
                    case MyImporterConstants.TAG_GEOMETRY_DATA_ASSET:
                    //data.Add(tagName, ReadArrayOfBytes(reader));
                    // break; 
                    case MyImporterConstants.TAG_FBXHASHSTRING:
                    case MyImporterConstants.TAG_HKTHASHSTRING:
                    case MyImporterConstants.TAG_XMLHASHSTRING:
                        data.Add(tagName, reader.ReadString());
                        break;
                    case MyImporterConstants.TAG_USE_CHANNEL_TEXTURES:
                    case MyImporterConstants.TAG_IS_SKINNED:
                    case MyImporterConstants.TAG_SWAP_WINDING_ORDER:
                        data.Add(tagName, reader.ReadBoolean());
                        break;
                    case MyImporterConstants.TAG_NORMALS:
                    case MyImporterConstants.TAG_BINORMALS:
                    case MyImporterConstants.TAG_TANGENTS:
                        data.Add(tagName, ReadArrayOfByte4(reader));
                        break;
                    default:
                        throw new NotImplementedException(string.Format($"tag '{tagName}' has not been implemented"));
                }
            }
        }

        #endregion
    }
}
