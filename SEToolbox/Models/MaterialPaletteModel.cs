using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using System.Text.Json;



namespace SEToolbox.Models
{

    public class MaterialPalette
    {
        private readonly Dictionary<byte, Color> materialColors = [];

        public enum PaletteType
        {
            Default,
            Custom,
        }

        public PaletteType Type { get; set; }

        /// <summary>
        /// Loads material colors from a JSON palette file.
        /// Expected format: { "0": "#000000", "1": "#FF0000", ... }
        /// </summary>
        public static MaterialPalette Load(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            var palette = new MaterialPalette
            {
                Type = PaletteType.Custom

            };

            foreach (var kvp in data)
            {
                if (byte.TryParse(kvp.Key, out byte id) &&
                    (ColorConverter.ConvertFromString(kvp.Value) is Color color))
                {
                    palette.materialColors[id] = color;
                }
            }

            return palette;
        }
    }

        /// <summary>
        /// Returns a color for the given material ID. If not found, returns gray.
        /// </summary>
    namespace VoxelViewerHelix
    {
        public class MaterialPalette
        {
            private readonly Dictionary<byte, Color> colors = [];

            public static MaterialPalette Default => new();

            public MaterialPalette()
            {
                for (byte i = 0; i < 255; i++)
                {
                    colors[i] = Color.FromRgb((byte)(i % 64 * 4), (byte)(i / 4 % 64 * 4), (byte)(255 - (i % 255)));
                }

                colors[0] = Colors.Transparent;
            }

            public Color GetColor(byte id)
            {
                return colors.ContainsKey(id) ? colors[id] : Colors.Gray;
            }

            /// <summary>
            /// Returns a color with alpha component set
            /// </summary>
            public Color GetColor(byte materialId, byte alpha)
            {
                var color = GetColor(materialId);
                color.A = alpha;
                return color;
            }

            public static readonly Dictionary<string, string> MaterialColors = new()
            {
            { "Stone", "#747474ff" },
            { "Iron Ore", "#8a5300ff" },
            { "Silicon Ore", "#424242ff" },
            { "Nickel Ore", "#c0c0c0ff" },
            { "Magnesium Ore", "#ffc1c1ff" },
            { "Cobalt Ore", "#00eeffff" },
            { "Gold Ore", "#ffae00ff" },
            { "Platinum Ore", "#fff5d1ff" },
            { "Uranium Ore", "#1eff00ff" },
        };
        }
    }
}