/* 
  THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
  ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
  THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
  PARTICULAR PURPOSE. 
  
    This is sample code and is freely distributable. 
*/
using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;

namespace SEToolbox.ImageLibrary
{
    /// <summary>
    /// Quantize using an Octree
    /// </summary>
    public unsafe class OctreeQuantizer : Quantizer
    {
        /// <summary>
        /// Construct the octree quantizer
        /// </summary>
        /// <remarks>
        /// The Octree quantizer is a two pass algorithm. The initial pass sets up the octree,
        /// the second pass quantizes a color based on the nodes in the tree
        /// </remarks>
        /// <param name="maxColors">The maximum number of colors to return</param>
        /// <param name="maxColorBits">The number of significant bits</param>
        public OctreeQuantizer(int maxColors, int maxColorBits)
            : base(false)
        {
            ;
            Exception e = maxColors switch
            {
                _ when maxColors > 255 => throw new ArgumentException($"maxColors:{maxColors} The number of colors should be less than 256"),
                _ when maxColorBits < 1 || maxColorBits > 8 => throw new ArgumentException($"maxColorBits:{maxColorBits} This should be between 1 and 8"),
                _ => null
            };
            // Construct the octree
            _octree = new Octree(maxColorBits);

            _maxColors = maxColors;

        }

        /// <summary>
        /// Process the pixel in the first pass of the algorithm
        /// </summary>
        /// <param name="pixel">The pixel to quantize</param>
        /// <remarks>
        /// This function need only be overridden if your quantize algorithm needs two passes,
        /// such as an Octree quantizer.
        /// </remarks>
        protected override void InitialQuantizePixel(Color32* pixel)
        {
            // Add the color to the octree
            _octree.AddColor(pixel);
        }

        /// <summary>
        /// Override this to process the pixel in the second pass of the algorithm
        /// </summary>
        /// <param name="pixel">The pixel to quantize</param>
        /// <returns>The quantized value</returns>
        protected override byte QuantizePixel(Color32* pixel)
        {
            byte paletteIndex = (byte)_maxColors;   // The color at [_maxColors] is set to transparent

            // Get the palette index if this non-transparent
            return paletteIndex = pixel->Alpha > 0 ? paletteIndex = (byte)_octree.GetPaletteIndex(pixel) : paletteIndex;

        }

        /// <summary>
        /// Retrieve the palette for the quantized image
        /// </summary>
        /// <param name="original">Any old palette, this is overrwritten</param>
        /// <returns>The new color palette</returns>
        protected override ColorPalette GetPalette(ColorPalette original, bool clearPalette)
        {
            if (clearPalette)
            {
                for (int i = 0; i < original.Entries.Length; i++)
                {
                    original.Entries[i] = Color.FromArgb(0, 0, 0, 0);
                }
            }
            // First off convert the octree to _maxColors colors
            ArrayList palette = _octree.Palletize(_maxColors - 1);

            // Then convert the palette based on those colors
            for (int index = 0; index < palette.Count; index++)
            {
                original.Entries[index] = (Color)palette[index];
            }

            // Add the transparent color
            original.Entries[_maxColors] = Color.FromArgb(0, 0, 0, 0);

            return original;
        }

        protected override ColorPalette CLearPalette(ColorPalette original)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Stores the tree
        /// </summary>
        private readonly Octree _octree;

        /// <summary>
        /// Maximum allowed color depth
        /// </summary>
        private readonly int _maxColors;

        /// <summary>
        /// Class which does the actual quantization
        /// </summary>
        private class Octree
        {  
             
            private int _maxColorBits;
            private int _leafCount;
            private OctreeNode[] _reducibleNodes;
            private OctreeNode _root;

            private OctreeNode _previousNode;
            private int _previousColor;
            /// <summary>
            /// Construct the octree
            /// </summary>
            /// <param name="maxColorBits">The maximum number of significant bits in the image</param>
            public Octree(int maxColorBits)
            {
                _maxColorBits = maxColorBits;
                _leafCount = 0;
                _reducibleNodes = new OctreeNode[9];
                _root = new OctreeNode(0, _maxColorBits, this);
                _previousColor = 0;
                _previousNode = null;
            }

            /// <summary>
            /// Add a given color value to the octree
            /// </summary>
            /// <param name="pixel"></param>
            public void AddColor(Color32* pixel)
            {
                // Check if this request is for the same color as the last
                if (_previousColor == pixel->Argb && _previousNode == null)
                {
                    _previousColor = pixel->Argb;
                    _root.AddColor(pixel, _maxColorBits, 0, this);

                }
                else
                {
                    _previousNode.Increment(pixel);
                }
            }

            /// <summary>
            /// Reduce the depth of the tree
            /// </summary>
            public void Reduce()
            {
                int index;

                // Find the deepest level containing at least one reducible node
                for (index = _maxColorBits - 1; (index > 0) && (_reducibleNodes[index] == null); index--)
                {
                    // Reduce the node most recently added to the list at level 'index'
                    OctreeNode node = _reducibleNodes[index];
                    _reducibleNodes[index] = node.NextReducible;

                    // Decrement the leaf count after reducing the node
                    _leafCount -= node.Reduce();

                    // If this was the last color to be added, and the next color to
                    // be added is the same, invalidate the previousNode.
                    _previousNode = null;
                }
            }

            /// <summary>
            /// Get/Set the number of leaves in the tree
            /// </summary>
            public int Leaves
            {
                get => _leafCount;
                set => _leafCount = value;
            }

            /// <summary>
            /// Return the array of reducible nodes
            /// </summary>
            protected OctreeNode[] ReducibleNodes
            {
                get => _reducibleNodes;
            }

            /// <summary>
            /// Keep track of the previous node that was quantized
            /// </summary>
            /// <param name="node">The node last quantized</param>
            protected void TrackPrevious(OctreeNode node)
            {
                _previousNode = node;
            }

            /// <summary>
            /// Convert the nodes in the octree to a palette with a maximum of colorCount colors
            /// </summary>
            /// <param name="colorCount">The maximum number of colors</param>
            /// <returns>An arraylist with the palettized colors</returns>
            public ArrayList Palletize(int colorCount)
            {
                while (Leaves > colorCount)
                {
                    Reduce();
                }

                // Now palettize the nodes
                ArrayList palette = new(Leaves);
                int paletteIndex = 0;
                _root.ConstructPalette(palette, ref paletteIndex);

                // And return the palette
                return palette;
            }

            /// <summary>
            /// Get the palette index for the passed color
            /// </summary>
            /// <param name="pixel"></param>
            /// <returns></returns>
            public int GetPaletteIndex(Color32* pixel)
            {
                return _root.GetPaletteIndex(pixel, 0);
            }

            /// <summary>
            /// Class which encapsulates each node in the tree
            /// </summary>
            protected class OctreeNode
            {
                private bool _leaf;
                private int _pixelCount;
                private int _red;
                private int _green;
                private int _blue;
                private OctreeNode[] _children;
                private OctreeNode _nextReducible;
                private int _paletteIndex;

                /// <summary>
                /// Construct the node
                /// </summary>
                /// <param name="level">The level in the tree = 0 - 7</param>
                /// <param name="colorBits">The number of significant color bits in the image</param>
                /// <param name="octree">The tree to which this node belongs</param>
                public OctreeNode(int level, int colorBits, Octree octree)
                {
                    // Construct the new node
                    _leaf = level == colorBits;
                    _red = _green = _blue = 0;
                    _pixelCount = 0;

                    // If a leaf, increment the leaf count
                    if (_leaf)
                    {
                        octree.Leaves++;
                        _nextReducible = null;
                        _children = null;
                    }
                    else
                    {
                        // Otherwise add this to the reducible nodes
                        _nextReducible = octree.ReducibleNodes[level];
                        octree.ReducibleNodes[level] = this;
                        _children = new OctreeNode[8];
                    }
                }
                
                private static readonly int[] mask = [0x80, 0x40, 0x20, 0x10, 0x08, 0x04, 0x02, 0x01];
                /// <summary>
                /// Add a color into the tree
                /// </summary>
                /// <param name="pixel">The color</param>
                /// <param name="colorBits">The number of significant color bits</param>
                /// <param name="level">The level in the tree</param>
                /// <param name="octree">The tree to which this node belongs</param>
                public void AddColor(Color32* pixel, int colorBits, int level, Octree octree)
                {
                    // Update the color information if this is a leaf
                    if (_leaf)
                    {
                        Increment(pixel);
                        // Setup the previous node
                        octree.TrackPrevious(this);
                    }
                    else
                    {
                        // Go to the next level down in the tree
                        int shift = 7 - level;
                        int index = (pixel->Red & mask[level]) >> (shift - 2) |
                                    (pixel->Green & mask[level]) >> (shift - 1) |
                                    (pixel->Blue & mask[level]) >> shift;

                        OctreeNode child = _children[index];

                        if (child == null)
                        {
                            // Create a new child node & store in the array
                            child = new OctreeNode(level + 1, colorBits, octree);
                            _children[index] = child;
                        }

                        // Add the color to the child node
                        child.AddColor(pixel, colorBits, level + 1, octree);
                    }
                }

                /// <summary>
                /// Get/Set the next reducible node
                /// </summary>
                public OctreeNode NextReducible
                {
                    get => _nextReducible;
                    set => _nextReducible = value;
                }

                /// <summary>
                /// Return the child nodes
                /// </summary>
                public OctreeNode[] Children
                {
                    get => _children;
                }

                /// <summary>
                /// Reduce this node by removing all of its children 
                /// </summary>
                /// <returns>The number of leaves removed</returns>
                public int Reduce()
                {
                    _red = _green = _blue = 0;
                    int children = 0;

                    // Loop through all children and add their information to this node
                    for (int index = 0; index < 8; index++)
                    {
                        if (_children[index] != null)
                        {   int[] colors = [_children[index]._red,_children[index]._green, _children[index]._blue];

                            Increment(colors, _children[index]._pixelCount);

                            ++children;
                            _children[index] = null;
                            _children[index]._nextReducible = null;
                        }
                    }
                    // Now change this to a leaf node
                    _leaf = true;

                    // Return the number of nodes to decrement the leaf count by
                    return children - 1;
                }

                /// <summary>
                /// Traverse the tree, building up the color palette
                /// </summary>
                /// <param name="palette">The palette</param>
                /// <param name="paletteIndex">The current palette index</param>
                public void ConstructPalette(ArrayList palette, ref int paletteIndex)
                {
                    if (_leaf)
                    {
                        // Consume the next palette index
                        _paletteIndex = paletteIndex++;
                        // And set the color of the palette entry
                        palette.Add(Color.FromArgb(_red / _pixelCount, _green / _pixelCount, _blue / _pixelCount));
                    }
                    else
                    {
                        // Loop through children looking for leaves
                        for (int index = 0; index < 8; index++)
                        {
                            _children[index]?.ConstructPalette(palette, ref paletteIndex);
                        }
                    }
                }

                /// <summary>
                /// Return the palette index for the passed color
                /// </summary>
                public int GetPaletteIndex(Color32* pixel, int level)
                {
                    int paletteIndex = _paletteIndex;

                    if (!_leaf)
                    {
                        int shift = 7 - level;
                        int index = ((pixel->Red & mask[level]) >> (shift - 2)) |
                                    ((pixel->Green & mask[level]) >> (shift - 1)) |
                                    ((pixel->Blue & mask[level]) >> shift);

                        paletteIndex = _children[index] != null ? _children[index].GetPaletteIndex(pixel, level + 1) : throw new Exception("Palette index not found"); ;
                    }

                    return paletteIndex;
                }

                /// <summary>
                /// Increment the pixel count and add to the color information
                /// </summary>
                public void Increment(Color32* pixel)
                {
                    _pixelCount++;
                    _red += pixel->Red;
                    _green += pixel->Green;
                    _blue += pixel->Blue;
                }

                /// <summary>
                /// Increment the pixel count and add to the color information
                /// </summary>
                public void Increment(int[] colors, int pixelCount)
                {

                    _red += colors[0];
                    _green += colors[1];
                    _blue += colors[2];
                    _pixelCount += pixelCount;
                }
            }
        }
    }
}
