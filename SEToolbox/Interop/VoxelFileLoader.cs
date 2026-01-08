using SEToolbox.Models;

using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

using VRageMath;
using SEToolbox.Support;
namespace SEToolbox.Interop
{

    public static class VoxelFileLoader
    {

        public static VoxelGridModel Load(string path)
        {
            using var fs = File.OpenRead(path);
            using var reader = new BinaryReader(fs);

            // === Header parsing ===
            // Skip 8-byte magic string
            var magic = Encoding.ASCII.GetString(reader.ReadBytes(8));
            if (magic != "VOXELMAP")
            {
                throw new InvalidDataException("Invalid voxel file header.");
            }

            int version = reader.ReadInt32();
            int sizeX = reader.ReadInt32();
            int sizeY = reader.ReadInt32();
            int sizeZ = reader.ReadInt32();

            int dataLengthContent = reader.ReadInt32();
            int dataLengthMaterial = reader.ReadInt32();

            // === Decompress content stream ===
            byte[] contentRaw = reader.ReadBytes(dataLengthContent);
            byte[] content = Decompress(contentRaw, sizeX * sizeY * sizeZ);

            // === Decompress material stream ===
            byte[] materialRaw = reader.ReadBytes(dataLengthMaterial);
            byte[] material = Decompress(materialRaw, sizeX * sizeY * sizeZ);

            // === Build VoxelGridModel ===
            var grid = new VoxelGridModel(sizeX, sizeY, sizeZ);

            var size = new Vector3I(sizeX, sizeY, sizeZ);
            int index = 0;
            int x = 0, y = 0, z = 0;
            PRange.ProcessRange(x, y, z, size);

            if (index < content.Length)
            {
                grid.SetContent(x, y, z, content[index]);
                grid.SetMaterial(x, y, z, material[index]);
            }
            return grid;
        }

        private static byte[] Decompress(byte[] input, int expectedSize)
        {
            using var ms = new MemoryStream(input);
            using var deflate = new DeflateStream(ms, CompressionMode.Decompress);
            byte[] output = new byte[expectedSize];
            int bytesRead;
            int offset = 0;
            while ((bytesRead = deflate.Read(output, offset, expectedSize - offset)) > 0)
            {
                offset += bytesRead;
            }
            return output;
        }
    }
}
