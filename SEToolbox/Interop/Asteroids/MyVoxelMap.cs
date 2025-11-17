using Sandbox.Engine.Voxels;
using Sandbox.Engine.Voxels.Planet;
using Sandbox.Game.Entities.Cube;
using SEToolbox.Interop;
using SEToolbox.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;

using VRage.FileSystem;
using VRage.Game.Voxels;
using VRage.Library.Compression;
using VRage.ObjectBuilders;
using VRage.Voxels;
using VRageMath;
using Res = SEToolbox.Properties.Resources;

namespace SEToolbox.Interop.Asteroids
{
    public class MyVoxelMapBase : Sandbox.Game.Entities.MyVoxelBase, IDisposable
    {
        public struct FileExtension
        {
            public const string V1 = "vox";
            public const string V2 = "vx2";
        }

        internal const string TagCell = "Cell";

        #region Fields

        private BoundingBoxI _boundingContent;

        private Dictionary<byte, long> _assetCount;

        #endregion

        #region Properties

        public new Vector3I Size { get; private set; }

        public BoundingBoxI BoundingContent => _boundingContent;

        /// <summary>
        /// The BoundingContent + 1 around all sides.
        /// This allows operations for copying the voxel correctly.
        /// The volume itself, plus 1 extra layer surrounding the volume which affects the visual appearance at lower LODs.
        /// </summary>
        public BoundingBoxI InflatedBoundingContent
        {
            get
            {
                BoundingBoxI content = _boundingContent;

                content.Inflate(1);
                content.Min = Vector3I.Max(content.Min, Vector3I.Zero);
                content.Max = Vector3I.Min(content.Max, Size - Vector3I.One);

                return content;
            }
        }

        public Vector3D ContentCenter => _boundingContent.ToBoundingBoxD().Center;

        public byte VoxelMaterial { get; private set; }

        public bool IsValid { get; private set; }

        public long VoxCells { get; private set; }

        #endregion

        #region Init

        public override void Init(MyObjectBuilder_EntityBase builder, IMyStorage storage)
        {
            m_storage = (MyStorageBase)storage;
        }

        public override Sandbox.Game.Entities.MyVoxelBase RootVoxel => this;

        public override IMyStorage Storage
        {
            get => m_storage;

            set => m_storage = value;
        }

        public void Create(Vector3I size, byte materialIndex)
        {
            m_storage?.Close();

            MyOctreeStorage octreeStorage = new(null, size);
            octreeStorage.Geometry.Init(octreeStorage);
            m_storage = octreeStorage;
            OverwriteAllMaterials(materialIndex);

            IsValid = true;
            Size = octreeStorage.Size;
            _boundingContent = new BoundingBoxI();
            VoxCells = 0;
        }

        private void OverwriteAllMaterials(byte materialIndex)
        {
            // For some reason the cacheSize will NOT work at the same size of the storage when less than 64.
            // This can be seen by trying to read the material, update and write back, then read again to verify.
            // Trying to adjust the size in BlockFillMaterial will only lead to memory corruption.
            // Normally I wouldn't recommend usig an oversized cache, but in this case it should not be an issue as we are changing the material for the entire voxel space.
            Vector3I cacheSize = Vector3I.Min(new Vector3I(64), m_storage.Size * 2);
            // read the asteroid in chunks to avoid the Arithmetic overflow issue.
            Vector3I block = Vector3I.Zero;
            PRange.ProcessRange(block, m_storage.Size / 64);

            MyStorageData cache = new();
            cache.Resize(cacheSize);
            // LOD1 is not detailed enough for content information on asteroids.
            Vector3I maxRange = block + cacheSize - 1;
            m_storage.ReadRange(cache, MyStorageDataTypeFlags.Material, 0, block, maxRange);
            cache.BlockFillMaterial(Vector3I.Zero, cacheSize - 1, materialIndex);
            m_storage.WriteRange(cache, MyStorageDataTypeFlags.Material, block, maxRange);
        }


        #endregion

        public static Dictionary<string, long> GetMaterialAssetDetails(string fileName)
        {
            Dictionary<string, long> list = [];
            MyVoxelMapBase map = new();
            map.Load(fileName);

            if (!map.IsValid)
                return list;

            list = map.RefreshAssets();
            map.Dispose();

            return list;
        }

        #region IsVoxelMapFile

        /// <summary>
        /// check for Magic Number: 1f 8b
        /// </summary>
        public static bool IsVoxelMapFile(string fileName)
        {
            string extension = Path.GetExtension(fileName);
            if  (extension !=null && extension.Equals(FileExtension.V1, StringComparison.InvariantCultureIgnoreCase))
            {
                using FileStream stream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                try
                {
                    int msgLength1 = stream.ReadByte();
                    int msgLength2 = stream.ReadByte();
                    int msgLength3 = stream.ReadByte();
                    int msgLength4 = stream.ReadByte();
                    int b1 = stream.ReadByte();
                    int b2 = stream.ReadByte();
                    return b1 == 0x1f && b2 == 0x8b;
                }
                catch
                {
                    return false;
                }
            }
            if (extension != null && extension.Equals(FileExtension.V2, StringComparison.InvariantCultureIgnoreCase))
            {
                try
                {
                    return ZipTools.IsGzipedFile(fileName);
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        #endregion

        #region Load

        public void Load(string fileName)
        {
            try
            {
                m_storage = MyStorageBase.LoadFromFile(fileName, cache: false);
                IsValid = true;
                Size = m_storage.Size;

                GC.Collect();
            }
            catch (FileNotFoundException)
            {
                // this exception may hide a dll dependancy from the game that is required, so it needs to be rethrown.
                throw;
            }
            catch (Exception ex)
            {
                Size = Vector3I.Zero;
                _boundingContent = new BoundingBoxI();
                VoxCells = 0;
                IsValid = false;
                Log.Warning(string.Format(Res.ExceptionState_CorruptAsteroidFile, fileName), ex);
            }
        }

        /// implemented from Sandbox.Engine.Voxels.MyStorageBase.UpdateFileFormat(string originalVoxFile), but we need control of the destination file.
        public static void UpdateFileFormat(string originalVoxFile, string newVoxFile)
        {
            using MyCompressionFileLoad myCompressionFileLoad = new(originalVoxFile);
            using Stream stream = MyFileSystem.OpenWrite(newVoxFile, FileMode.Create);
            using GZipStream gZipStream = new(stream, CompressionMode.Compress);
            using BufferedStream bufferedStream = new(gZipStream);
            bufferedStream.WriteNoAlloc(TagCell, null);
            bufferedStream.Write7BitEncodedInt(myCompressionFileLoad.GetInt32());

            byte[] array = new byte[16384];
            for (int bytes = myCompressionFileLoad.GetBytes(array.Length, array); bytes != 0; bytes = myCompressionFileLoad.GetBytes(array.Length, array))
            {
                bufferedStream.Write(array, 0, bytes);
            }
        }

        #endregion

        #region LoadVoxelSize

        /// <summary>
        /// Loads the header details only for voxel files, without having to decompress the entire file.
        /// </summary>
        /// <param name="fileName"></param>
        public static Vector3I LoadVoxelSize(string fileName)
        {
            try
            {
                if (Path.GetExtension(fileName).Equals(FileExtension.V2, StringComparison.InvariantCultureIgnoreCase))
                {
                    MyVoxelMapBase map = new();
                    map.Load(fileName);
                    map.Dispose();
                    return map.Size;
                }

                // Leaving the .vox file to the old code, as we only need to interrogate it for the voxel size, not load it into memory.

                // only 29 bytes are required for the header, but I'll leave it for 32 for a bit of extra leeway.
                byte[] buffer = Uncompress(fileName, 32);

                using BinaryReader reader = new(new MemoryStream(buffer));
                reader.ReadInt32();// fileVersion
                int sizeX = reader.ReadInt32();
                int sizeY = reader.ReadInt32();
                int sizeZ = reader.ReadInt32();

                return new Vector3I(sizeX, sizeY, sizeZ);
            }
            catch
            {
                return Vector3I.Zero;
            }
        }

        #endregion

        #region Save

        /// <summary>
        /// Saves the asteroid to the specified filename.
        /// </summary>
        /// <param name="fileName">the file extension indicates the version of file been saved.</param>
        public new void Save(string fileName)
        {
            SConsole.Write("Saving binary.");

            m_storage.Save(out byte[] array);

            File.WriteAllBytes(fileName, array);

            SConsole.Write("Done.");
        }

        #endregion

        #region Compress

        /// <summary>
        /// Used to compress the old .vox format voxel files.
        /// </summary>
        public static void CompressV1(string sourceFileName, string destinationFileName)
        {
            // Low memory, fast compress.
            using FileStream originalByteStream = new(sourceFileName, FileMode.Open);
            if (File.Exists(destinationFileName))
                File.Delete(destinationFileName);

            using FileStream compressedByteStream = new(destinationFileName, FileMode.CreateNew);
            compressedByteStream.Write(BitConverter.GetBytes(originalByteStream.Length), 0, 4);

            // GZipStream requires using. Do not optimize the stream.
            using (GZipStream compressionStream = new(compressedByteStream, CompressionMode.Compress, true))
            {
                originalByteStream.CopyTo(compressionStream);
            }

            SConsole.WriteLine($"Compressed from {originalByteStream.Length:#,###0} bytes to {compressedByteStream.Length:#,###0} bytes.");
        }
        #endregion

        #region Uncompress
        /// <summary>
        /// Used to decompress the old .vox format voxel files.
        /// </summary>
        /// <param name="sourceFileName"></param>
        /// <param name="destinationFileName"></param>
        public static void UncompressV1(string sourceFileName, string destinationFileName)
        {
            // Low memory, fast extract.
            using FileStream compressedByteStream = new(sourceFileName, FileMode.Open, FileAccess.Read);
            BinaryReader reader = new(compressedByteStream);
            // message Length.
            reader.ReadInt32();

            if (File.Exists(destinationFileName))
                File.Delete(destinationFileName);

            using FileStream outStream = new(destinationFileName, FileMode.CreateNew);
            // GZipStream requires using. Do not optimize the stream.
            using GZipStream zip = new(compressedByteStream, CompressionMode.Decompress);
            zip.CopyTo(outStream);
            SConsole.WriteLine($"Decompressed from {compressedByteStream.Length:#,###0} bytes to {outStream.Length:#,###0} bytes.");
        }

        /// <summary>
        /// Used for loading older format voxel file streams, like .vox and first version of .vx2
        /// This is kept for legacy purposes, nothing more.
        /// </summary>
        public static byte[] Uncompress(string sourceFileName, int numberBytes)
        {
            using FileStream compressedByteStream = new(sourceFileName, FileMode.Open, FileAccess.Read);
            BinaryReader reader = new(compressedByteStream);
            // message Length.
            reader.ReadInt32();

            // GZipStream requires using. Do not optimize the stream.
            using GZipStream zip = new(compressedByteStream, CompressionMode.Decompress);
            byte[] arr = new byte[numberBytes];
            zip.Read(arr, 0, numberBytes);
            return arr;
        }

        #endregion

        #region Methods

        #region SetVoxelContentRegion

        public void SetVoxelContentRegion(byte content, int? xMin, int? xMax, int? yMin, int? yMax, int? zMin, int? zMax)
        {

            Vector3I cacheSize = Vector3I.Min(new(64), m_storage.Size);
            Vector3I block = Vector3I.Zero;
            PRange.ProcessRange(block, cacheSize / 64);
            MyStorageData cache = new();
            cache.Resize(cacheSize);
            Vector3I maxRange = block + cacheSize - 1;
            m_storage.ReadRange(cache, MyStorageDataTypeFlags.Content, 0, block, maxRange);

            bool changed = false;
            Vector3I p = Vector3I.Zero;
            PRange.ProcessRange(p, cacheSize);
            Vector3I coords = block + p;
            if (IsWithinBounds(coords, xMin, xMax, yMin, yMax, zMin, zMax))
            {
                p = new(p.X, p.Y, p.Z);
                cache.Content(ref p, content);
                changed = true;
            }

            if (changed)
            {
                m_storage.WriteRange(cache, MyStorageDataTypeFlags.Content, block, maxRange);
            }
        }

        private static bool IsWithinBounds(Vector3I coords, int? xMin, int? xMax, int? yMin, int? yMax, int? zMin, int? zMax)
        {
            return (!xMin.HasValue || coords.X >= xMin.Value) &&
                   (!xMax.HasValue || coords.X <= xMax.Value) &&
                   (!yMin.HasValue || coords.Y >= yMin.Value) &&
                   (!yMax.HasValue || coords.Y <= yMax.Value) &&
                   (!zMin.HasValue || coords.Z >= zMin.Value) &&
                   (!zMax.HasValue || coords.Z <= zMax.Value);
        }

        #endregion

        #region GetAdjustedCacheSize
        private Vector3I GetAdjustedCacheSize(Vector3I cacheSize)
        {
            var vRange = from x in Enumerable.Range(0, cacheSize.X)
                         from y in Enumerable.Range(0, cacheSize.Y)
                         from z in Enumerable.Range(0, cacheSize.Z)
                         select new Vector3I(x, y, z);

            Vector3I adjustedCacheSize = cacheSize;
            Vector3I vectors = Vector3I.Zero;

            for (int i = 0; i < 3; i++)

                foreach (var vec in vRange)
                {
                    if (adjustedCacheSize[i] < 64)
                    {
                        adjustedCacheSize[i] *= 2;
                    };
                    break;
                }

            return adjustedCacheSize;
        }

        #endregion

        #region SeedMaterialSphere

        /// <summary>
        /// Set a material for a random voxel cell and possibly nearest ones to it.
        /// </summary>
        /// <param name="materialIndex">material name</param>
        /// <param name="radius">radius in voxels, defaults to zero, meaning only a random grid.</param>
        public void SeedMaterialSphere(byte materialIndex, double radius = 0)
        {
            List<Vector3I> fullCells = [];
            Vector3I block = Vector3I.Zero;
            //var cacheSize = new Vector3I(64 >> 3);
            Vector3I cacheSize = new(8);
           var adjustedCacheSize = GetAdjustedCacheSize(cacheSize);
            PRange.ProcessRange(block, m_storage.Size);


            var cache = new MyStorageData();
            cache.Resize(adjustedCacheSize);
            m_storage.ReadRange(cache, MyStorageDataTypeFlags.Content, 3, block, block + cacheSize - 1);

            Vector3I p = Vector3I.Zero;

            // Unless volume is read, the call to ComputeContentConstitution() causes the fullCells list to not clear properly.
            byte volume = cache.Content(ref p);

            if (volume > 0)
            {
                // If the cell is empty, clear the fullCells list without modifying the original list
                if (cache.ComputeContentConstitution() == MyVoxelContentConstitution.Empty)
                {
                    fullCells.Clear();
                    return;
                }
            }
        
            if (cache.ComputeContentConstitution() != MyVoxelContentConstitution.Empty)
                // Collect the non-empty cell coordinates
                fullCells.Add(block << 3);



            cacheSize = new Vector3I(8);

            // Choose random cell and switch material there
            fullCells.Shuffle();
            int cellCount = fullCells.Count;
            List<Vector3I> validCells = [];

            for (int i = 0; i < cellCount; i++)
            {
                block = fullCells[i];
                cache = new();
                cache.Resize(cacheSize);
                m_storage.ReadRange(cache, MyStorageDataTypeFlags.ContentAndMaterial, 0, block, block + cacheSize - 1);

                if (cache.ComputeContentConstitution() == MyVoxelContentConstitution.Empty)
                {
                    // Skip empty cells without modifying the original list
                    validCells.Clear();
                    continue;
                }

                // If the cell is valid, add it to the validCells list
                validCells.Add(block);

                // Fill the material for the valid cell
                cache.BlockFillMaterial(Vector3I.Zero, cache.Size3D, materialIndex);
                m_storage.WriteRange(cache, MyStorageDataTypeFlags.Material, block, block + cacheSize - 1);
            }

            // Update fullCells with valid cells
            fullCells = validCells;

            // Optionally seek adjacent cells and set their material too.
            if (radius == 0)
                return;

            for (int i = 0; i < fullCells.Count; i++)
            {
                Vector3I vlen = fullCells[0] - fullCells[i];
                if (vlen.RectangularLength() <= radius)
                {
                    block = fullCells[i];
                    cache = new();
                    cache.Resize(cacheSize);
                    m_storage.ReadRange(cache, MyStorageDataTypeFlags.ContentAndMaterial, 0, block, block + cacheSize - 1);

                    if (cache.ComputeContentConstitution() == MyVoxelContentConstitution.Empty)
                    {
                        // If the cell is empty, skip it and continue with the next one.
                        fullCells.Clear();
                                continue;
                    }

                    cache.BlockFillMaterial(Vector3I.Zero, cache.Size3D, materialIndex);
                    m_storage.WriteRange(cache, MyStorageDataTypeFlags.Material, block, block + cacheSize - 1);
                }
            }

            // Might need to clear the list, as the Structs sit in memory otherwise and kill the List.
            // Could be caused by calling ComputeContentConstitution() on the cache without doing anything else on it.
            fullCells.Clear();
           // fullCells = null;
        }


        //Safe to remove???
        // /// <summary>
        // /// Set a material for a random voxel cell and possibly nearest ones to it.
        // /// </summary>
        // /// <param name="materialName">material name</param>
        // /// <param name="radius">radius in voxels, defaults to zero, meaning only a random grid.</param>
        // [Obsolete]
        // public void SeedMaterialSphere(string materialName, byte radius = 0)
        // {
        //    var fullCells = new List<Vector3I>();
        //    Vector3I cellCoord;
        //    // Collect the non-empty cell coordinates
        //    for (cellCoord.X = 0; cellCoord.X < _dataCellsCount.X; cellCoord.X++)
        //        for (cellCoord.Y = 0; cellCoord.Y < _dataCellsCount.Y; cellCoord.Y++)
        //            for (cellCoord.Z = 0; cellCoord.Z < _dataCellsCount.Z; cellCoord.Z++)
        //                if (!CheckCellType(ref _voxelContentCells[cellCoord.X][cellCoord.Y][cellCoord.Z], MyVoxelCellType.EMPTY))
        //                    fullCells.Add(cellCoord);

        //    // Choose random cell and switch material there
        //    fullCells.Shuffle();
        //    int cellCount = fullCells.Count;
        //    Vector3I cell, vlen;
        //    for (int i = 0; i < cellCount; i++)
        //    {
        //        cell = fullCells[i];
        //        if (i == 0)
        //        {
        //            SetVoxelMaterialRegion(materialName, ref cell);
        //            continue;
        //        }
        //        // Optionally seek adjanced cells and set their material too.
        //        if (radius == 0)
        //            return;
        //        vlen = fullCells[0] - cell;
        //        if (vlen.RectangularLength() <= radius)
        //        {
        //            SetVoxelMaterialRegion(materialName, ref cell);
        //        }
        //    }
        // }

        #endregion

        #region ForceBaseMaterial

        /// <summary>
        /// This will replace all the materials inside the asteroid with specified material.
        /// </summary>
        /// <param name="defaultMaterial"></param>
        /// <param name="materialName"></param>
        public void ForceBaseMaterial(string defaultMaterial, string materialName)
        {
            byte materialIndex = SpaceEngineersResources.GetMaterialIndex(materialName);
            Vector3I block = Vector3I.Zero;
            Vector3I cacheSize = Vector3I.Min(new Vector3I(64), m_storage.Size);

            // read the asteroid in chunks of 64 to avoid the Arithmetic overflow issue.
            PRange.ProcessRange(block, cacheSize / 64);
            MyStorageData cache = new();
            cache.Resize(cacheSize);
            Vector3I maxRange = block + cacheSize - 1;
            m_storage.ReadRange(cache, MyStorageDataTypeFlags.Material, 0, block, maxRange);

            Vector3I p = Vector3I.Zero;
            PRange.ProcessRange(p, cacheSize);
            cache.Material(ref p, materialIndex);

            m_storage.WriteRange(cache, MyStorageDataTypeFlags.Material, block, maxRange);
        }

        #endregion

        #region ForceVoxelFaceMaterial

        /// <summary>
        /// Changes all the min and max face materials to a default to overcome the the hiding rare ore inside of nonrare ore.
        /// </summary>
        [Obsolete("This is no longer required, as the voxel's no longer take their 'surface' texture from the outer most cell.")]
        public void ForceVoxelFaceMaterial(byte materialIndex)
        {
            Vector3I block = Vector3I.Zero;
            Vector3I cacheSize = Vector3I.Min(new Vector3I(64), m_storage.Size);
            // read the asteroid in chunks of 64 to avoid the Arithmetic overflow issue.
           var pRange = PRange.ProcessRange(block, cacheSize );
            

            for (int i = 0; i < 3; i++)
                if (block[i] == 0 || block[i] + cacheSize[i] == m_storage.Size[i] - 1)
                {
                    MyStorageData cache = new();
                    cache.Resize(cacheSize);
                    // LOD1 is not detailed enough for content information on asteroids.
                    Vector3I maxRange = block + cacheSize - 1;
                    m_storage.ReadRange(cache, MyStorageDataTypeFlags.Material, 0, block, maxRange);

                    bool changed = false;

                    Vector3I p = Vector3I.Zero;
                    PRange.ProcessRange(p, cacheSize);

                    Vector3I min = p + block;
                    if (min.X == 0 || min.Y == 0 || min.Z == 0 ||
                        min.X == m_storage.Size.X - 1 || min.Y == m_storage.Size.Y - 1 || min.Z == m_storage.Size.Z - 1)
                    {
                        if (cache.Material(ref p) != materialIndex)
                        {
                            cache.Material(ref p, materialIndex);
                            changed = true;
                        }
                    }

                    if (changed)
                        m_storage.WriteRange(cache, MyStorageDataTypeFlags.Material, block, maxRange);
                }
            }
        

        #endregion

        #region ForceShellFaceMaterials

        /// <summary>
        /// Force the material of the outermost mixed voxcells to the given material
        /// </summary>
        /// <param name="materialName"></param>
        /// <param name="targtThickness"></param>
        public void ForceShellMaterial(string materialName, byte targtThickness = 0)
        {
            byte curThickness;
            byte materialIndex = SpaceEngineersResources.GetMaterialIndex(materialName);

            // read the asteroid in chunks of 8 to simulate datacells.
            var cacheSize = new Vector3I(m_storage.Size.X >> 3);
            var cache = new MyStorageData();
            cache.Resize(cacheSize);
            m_storage.ReadRange(cache, MyStorageDataTypeFlags.ContentAndMaterial, 3, Vector3I.Zero, cacheSize - 1);

            Vector3I writebufferSize = new(8);
            MyStorageData writebuffer = new();
            writebuffer.Resize(writebufferSize);

            var cellsToProcess = new HashSet<Vector3I>();
            int x = 0, y = 0, z = 0;
            Vector3I cell = new(x, y, z);

            PRange.ProcessRange(cell, cacheSize / 64);
       
            var dataCell = cell;
            if (cache.Content(ref dataCell) != 0)
            {
                cellsToProcess.Add(dataCell);
            }

            foreach (var cellToProcess in cellsToProcess)
            {
                dataCell = cellToProcess;
                var nextCells = new[]
                {
                    dataCell + new Vector3I(0, 0, 1),
                    dataCell + new Vector3I(0, 0, -1),
                    dataCell + new Vector3I(0, 1, 0),
                    dataCell + new Vector3I(0, -1, 0),
                    dataCell + new Vector3I(1, 0, 0),
                    dataCell + new Vector3I(-1, 0, 0)
                };

                foreach (var nc in nextCells)
                {
                    var nextCell = nc;
                    if (cache.Content(ref nextCell) != 0)
                    {
                        curThickness = 0;
                        SetMaterialOnFace(dataCell, materialIndex, ref cache, ref writebuffer, writebufferSize, m_storage, targtThickness, ref curThickness);
                        break;
                    }
                }
            }
        }


        public static void SetMaterialOnFace(Vector3I dataCell, byte materialIndex, ref MyStorageData cache, ref MyStorageData writebuffer, Vector3I writebufferSize, IMyStorage m_storage, byte targtThickness, ref byte curThickness)
        {
            Vector3I bufferPosition = new(dataCell.X << 3, dataCell.Y << 3, dataCell.Z << 3);
            Vector3I maxRange = bufferPosition + writebufferSize - 1;
            writebuffer.ClearMaterials(0);
            m_storage.ReadRange(writebuffer, MyStorageDataTypeFlags.Material, 0, bufferPosition, maxRange);
            writebuffer.BlockFillMaterial(Vector3I.Zero, writebufferSize - 1, materialIndex);
            m_storage.WriteRange(writebuffer, MyStorageDataTypeFlags.Material, bufferPosition, maxRange);
            if ((targtThickness > 0 && ++curThickness >= targtThickness) || cache.Content(ref dataCell) == 255)
                return;
        }

        #endregion

        #region RemoveContent

        public void RemoveContent(string materialName, string replaceFillMaterial)
        {
            byte materialIndex = SpaceEngineersResources.GetMaterialIndex(materialName);
            byte replaceMaterialIndex = materialIndex;
            if (!string.IsNullOrEmpty(replaceFillMaterial))
                replaceMaterialIndex = SpaceEngineersResources.GetMaterialIndex(replaceFillMaterial);

            Vector3I cacheSize = Vector3I.Min(new Vector3I(64), m_storage.Size);
            var block = Vector3I.Zero;
            // read the asteroid in chunks of 64 to avoid the Arithmetic overflow issue.
            PRange.ProcessRange(block, cacheSize);
            MyStorageData cache = new();
            cache.Resize(cacheSize);
            // LOD1 is not detailed enough for content information on asteroids.
            Vector3I maxRange = block + cacheSize - 1;
            m_storage.ReadRange(cache, MyStorageDataTypeFlags.ContentAndMaterial, 0, block, maxRange);

            bool changed = false;
            Vector3I p = Vector3I.Zero;
            PRange.ProcessRange(p, cacheSize);

            if (cache.Material(ref p) == materialIndex)
            {
                cache.Content(ref p, 0);
                if (replaceMaterialIndex != materialIndex)
                    cache.Material(ref p, replaceMaterialIndex);
                changed = true;
            }

            if (changed)
                m_storage.WriteRange(cache, MyStorageDataTypeFlags.ContentAndMaterial, block, maxRange);
        }

        public void RemoveMaterial(int? xMin, int? xMax, int? yMin, int? yMax, int? zMin, int? zMax)
        {
            SetVoxelContentRegion(0x00, xMin, xMax, yMin, yMax, zMin, zMax);
        }

        #endregion

        #region ReplaceMaterial

        public void ReplaceMaterial(string materialName, string replaceFillMaterial)
        {
            byte materialIndex = SpaceEngineersResources.GetMaterialIndex(materialName);
            byte replaceMaterialIndex = SpaceEngineersResources.GetMaterialIndex(replaceFillMaterial);
            Vector3I block = Vector3I.Zero;

            Vector3I cacheSize = Vector3I.Min(new Vector3I(64), m_storage.Size);

            // read the asteroid in chunks of 64 to avoid the Arithmetic overflow issue.
            PRange.ProcessRange(block, cacheSize);
            MyStorageData cache = new();
            cache.Resize(cacheSize);
            // LOD1 is not detailed enough for content information on asteroids.
            Vector3I maxRange = block + cacheSize - 1;
            m_storage.ReadRange(cache, MyStorageDataTypeFlags.Material, 0, block, maxRange);

            bool changed = false;
            Vector3I p = Vector3I.Zero;
            PRange.ProcessRange(p, cacheSize);
            if (cache.Material(ref p) == materialIndex)
            {
                cache.Material(ref p, replaceMaterialIndex);
                changed = true;
            }

            if (changed)
                m_storage.WriteRange(cache, MyStorageDataTypeFlags.Material, block, maxRange);
        }

        #endregion

        #region SumVoxelCells

        private void CalcVoxelCells()
        {
            long sum = 0;

            if (!IsValid)
            {
                _assetCount = [];
                _boundingContent = new BoundingBoxI();
                VoxCells = sum;
                return;
            }

            if (m_storage.DataProvider is MyPlanetStorageProvider)
            {
                _assetCount = [];
                _boundingContent = new BoundingBoxI(Vector3I.Zero, m_storage.Size);
                VoxCells = sum;
                return;
            }

            Vector3I min = Vector3I.MaxValue;
            Vector3I max = Vector3I.MinValue;

            Vector3I cacheSize = Vector3I.Min(new Vector3I(64), m_storage.Size);
            Dictionary<byte, long> assetCount = [];

            MyStorageData cache = new();
            cache.Resize(cacheSize);

            // read the asteroid in chunks of 64 to avoid the Arithmetic overflow issue.
            Vector3I block = Vector3I.Zero;
            PRange.ProcessRange(block, m_storage.Size);
            // LOD1 is not detailed enough for content information on asteroids.
            Vector3I maxRange = block + cacheSize - 1;
            m_storage.ReadRange(cache, MyStorageDataTypeFlags.ContentAndMaterial, 0, block, maxRange);

            Vector3I p = Vector3I.Zero;
            PRange.ProcessRange(p, cacheSize);

            byte content = cache.Content(ref p);

            if (content > 0)
            {
                min = Vector3I.Min(min, p + block);
                max = Vector3I.Max(max, p + block + 1);

                byte material = cache.Material(ref p);

                if (assetCount.TryGetValue(material, out long c))
                    assetCount[material] = c + content;
                else
                    assetCount.Add(material, content);
                sum += content;
            }

            _assetCount = assetCount;

            if (min == Vector3I.MaxValue && max == Vector3I.MinValue)
                _boundingContent = new BoundingBoxI();
            else
                _boundingContent = new BoundingBoxI(min, max - 1);

            VoxCells = sum;
        }

        #endregion

        #region UpdateContentBounds

        /// <summary>
        /// Updates the content bounds of the voxel map by recalculating the minimum and maximum non-empty voxel coordinates.
        /// </summary>
        public static void UpdateContentBounds(MyVoxelMapBase voxelMap)
        {
            if (voxelMap == null || voxelMap.Storage == null)
                return;

            Vector3I min = new(int.MaxValue);
            Vector3I max = new(int.MinValue);

            Vector3I size = voxelMap.Storage.Size;
            MyStorageData cache = new();
            cache.Resize(size);
            // Read all content data
            voxelMap.Storage.ReadRange(cache, MyStorageDataTypeFlags.Content, 0, Vector3I.Zero, size - 1);

            Vector3I p = Vector3I.Zero;
            _ = new MyVoxelMapBase();
            PRange.ProcessRange(p, size);

            if (cache.Content(ref p) > 0)
            {
                min = Vector3I.Min(min, p);
                max = Vector3I.Max(max, p);
            }
            // Store or use min/max as needed, e.g.:
            voxelMap._boundingContent = new BoundingBoxI(min, max);
        }


        #endregion

        #region CalculateMaterialCellAssets

        public IList<byte> CalcVoxelMaterialList()
        {
            if (!IsValid)
                return null;

            const int chunkSize = 64;

            IMyStorage storage = m_storage;
            Vector3I storageSize = storage.Size;
            Vector3I cacheSize = Vector3I.Min(new Vector3I(chunkSize), storageSize);

            MyStorageData cache = new();
            cache.Resize(cacheSize);

            List<byte> voxelMaterialList = [];

            Vector3I block = Vector3I.Zero;
            // Read the asteroid in chunks of 64 to avoid the Arithmetic overflow issue.

            PRange.ProcessRange(block, cacheSize);
            Vector3I maxRange = Vector3I.Min(block + cacheSize, storageSize) - 1;

            storage.ReadRange(cache, MyStorageDataTypeFlags.ContentAndMaterial, 0, block, maxRange);

            for (int i = 0; i < cache.SizeLinear; i++)
            {
                byte content = cache.Content(i);

                if (content > 0)
                {
                    voxelMaterialList.Add(cache.Material(i));
                }
            }

            return voxelMaterialList;
        }

        public void SetVoxelMaterialList(IList<byte> materials)
        {
            if (!IsValid)
                return;

            Vector3I block = Vector3I.Zero;
            Vector3I cacheSize = Vector3I.Min(new Vector3I(64), m_storage.Size);
            int index = 0;

            // read the asteroid in chunks of 64 to avoid the Arithmetic overflow issue.
            PRange.ProcessRange(block, m_storage.Size);
            MyStorageData cache = new();
            cache.Resize(cacheSize);
            // LOD1 is not detailed enough for content information on asteroids.
            Vector3I maxRange = block + cacheSize - 1;
            m_storage.ReadRange(cache, MyStorageDataTypeFlags.ContentAndMaterial, 0, block, maxRange);

            Vector3I p = Vector3I.Zero;

            PRange.ProcessRange(p, cacheSize);
            byte content = cache.Content(ref p);
            if (content > 0)
            {
                cache.Material(ref p, materials[++index]);
               
            }

           

            m_storage.WriteRange(cache, MyStorageDataTypeFlags.Material, block, maxRange);
        }

        #endregion

        #endregion

        #region CountAssets

        public Dictionary<string, long> RefreshAssets()
        {
            CalcVoxelCells();
            return CountAssets();
        }

        private Dictionary<string, long> CountAssets()
        {
            var materialDefinitions = SpaceEngineersResources.VoxelMaterialDefinitions;
            Dictionary<string, long> assetNameCount = [];
            byte defaultMaterial = SpaceEngineersResources.GetMaterialIndex(SpaceEngineersResources.GetDefaultMaterialName());

            foreach (var kvp in _assetCount)
            {
                string name;

                if (kvp.Key >= materialDefinitions.Count)
                    name = materialDefinitions[defaultMaterial].Id.SubtypeName;
                else
                    name = materialDefinitions[kvp.Key].Id.SubtypeName;

                if (assetNameCount.ContainsKey(name))
                {
                    assetNameCount[name] += kvp.Value;
                }
                else
                {
                    assetNameCount.Add(name, kvp.Value);
                }
            }

            return assetNameCount;
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MyVoxelMapBase()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_storage?.Close();
            }
        }

        #endregion

        // mapped from: Sandbox.Game.Entities.MyVoxelBase because it's private.
        // can't be bothered using Reflection.
        public void UpdateVoxelShape(OperationType type, MyShape shape, byte material)
        {
            switch (type)
            {
                case OperationType.Fill:
                    // Warning: FillInShape calls MySandboxGame.Invoke()
                    MyVoxelGenerator.FillInShape(this, shape, material);
                    break;
                case OperationType.Paint:
                    MyVoxelGenerator.PaintInShape(this, shape, material);
                    break;
                case OperationType.Cut:
                     //MySession.Settings.EnableVoxelDestruction has to be enabled for Shapes to be deleted.
                    MyVoxelGenerator.CutOutShape(this, shape);
                    break;
            }
        }
    }
}