using System;
using static System.Array;

namespace SEToolbox.Models
{
    public class VoxelGridModel(int sizeX, int sizeY, int sizeZ)
    {
        public int SizeX { get; } = sizeX;
        public int SizeY { get; } = sizeY;
        public int SizeZ { get; } = sizeZ;

        private readonly byte[,,] _content = new byte[sizeX, sizeY, sizeZ];
        private readonly byte[,,] _material = new byte[sizeX, sizeY, sizeZ];

        public byte GetContent(int x, int y, int z) => _content[x, y, z];
        public byte GetMaterial(int x, int y, int z) => _material[x, y, z];

        public void SetContent(int x, int y, int z, byte value)
        {
            ref byte target = ref _content[x, y, z];
            target = value;
        }

        public void SetMaterial(int x, int y, int z, byte value)
        {
            ref byte target = ref _material[x, y, z];
            target = value;
        }

        public void FillContent(byte[,,] data)
        {
            if (data.GetLength(0) != SizeX || data.GetLength(1) != SizeY || data.GetLength(2) != SizeZ)
            {
                throw new ArgumentException("Dimension mismatch in FillContent");
            }

            Copy(data, _content, data.Length);
        }

        public void FillMaterial(byte[,,] data)
        {
          if (data.GetLength(0) != SizeX || data.GetLength(1) != SizeY || data.GetLength(2) != SizeZ)
            {
                throw new ArgumentException("Dimension mismatch in FillMaterial");
            }

            if (_material != null)
            {
                Copy(data, _material, data.Length);
            }
        }
    }
}

