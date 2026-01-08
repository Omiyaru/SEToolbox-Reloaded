/* 
  THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
  ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
  THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
  PARTICULAR PURPOSE. 
  
    This is sample code and is freely distributable. 
*/

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SEToolbox.ImageLibrary
{
    /// <summary>
    /// Summary description for Class1.
    /// </summary>
    /// <remarks>
    /// Construct the quantizer
    /// </remarks>
    /// <param name="singlePass">If true, the quantization only needs to loop through the source pixels once</param>
    /// <remarks>
    /// If you construct this class with a true value for singlePass, then the code will, when quantizing your image,
    /// only call the 'QuantizeImage' function. If two passes are required, the code will call 'InitialQuantizeImage'
    /// and then 'QuantizeImage'.
    /// </remarks>
    public unsafe abstract class Quantizer(bool singlePass)
    {

        /// <summary>
        /// Quantize an image and return the resulting output bitmap
        /// </summary>
        /// <param name="source">The image to quantize</param>
        /// <returns>A quantized version of the image</returns>
        public Bitmap Quantize(Image source)
        {    
            bool clearPalette = false;
            // Get the size of the source image
            int height = source.Height;
            int width = source.Width;
            Rectangle bounds = new(0, 0, width, height);

            Bitmap copy = new(width, height, PixelFormat.Format32bppArgb);

   
            Bitmap output = new(width, height, PixelFormat.Format8bppIndexed);


            using Graphics graphics = Graphics.FromImage(copy);
            graphics.PageUnit = GraphicsUnit.Pixel;
            graphics.DrawImageUnscaled(source, bounds); // Draw the source image onto the copy bitmap


            BitmapData sourceData = null;

            try
            {
                sourceData = copy.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                if (!_singlePass)
                {
                    FirstPass(sourceData, width, height); 
                }
                output.Palette = GetPalette(output.Palette, false | clearPalette);
                SecondPass(sourceData, output, width, height, bounds);
            }
            finally
            {
                copy.UnlockBits(sourceData);
            }

            // Last but not least, return the output bitmap
            return output;
        }

        /// <summary>
        /// Execute the first pass through the pixels in the image
        /// </summary>
        /// <param name="sourceData">The source data</param>
        /// <param name="width">The width in pixels of the image</param>
        /// <param name="height">The height in pixels of the image</param>
        protected virtual void FirstPass(BitmapData sourceData, int width, int height)
        {
            byte* pixelsSourceRowPtr = (byte*)sourceData.Scan0.ToPointer();
            int* pixelSourcePtr;

            for (int row = 0; row < height; row++)
            {
                pixelSourcePtr = (int*)pixelsSourceRowPtr;
                for (int col = 0; col < width; col++, pixelSourcePtr++)
                {
                    InitialQuantizePixel((Color32*)pixelSourcePtr);
                }

                pixelsSourceRowPtr += sourceData.Stride;
            }
        }

        /// <summary>
        /// Execute a second pass through the bitmap
        /// </summary>
        /// <param name="sourceData">The source bitmap, locked into memory</param>
        /// <param name="output">The output bitmap</param>
        /// <param name="width">The width in pixels of the image</param>
        /// <param name="height">The height in pixels of the image</param>
        /// <param name="bounds">The bounding rectangle</param>
        protected virtual void SecondPass(BitmapData sourceData, Bitmap output, int width, int height, Rectangle bounds)
        {
            BitmapData outputData = null;

            try
            {
                outputData = output.LockBits(bounds, ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
                byte* pixelsSourceRow = (byte*)sourceData.Scan0.ToPointer(); 
                int* pixelSourcePtr = (int*)pixelsSourceRow;
                int* pixelPrevPtr = pixelSourcePtr;

                byte* pixelDestRow = (byte*)outputData.Scan0.ToPointer();
                byte* pixelDestPtr = pixelDestRow;
                byte pixelValue = QuantizePixel((Color32*)pixelSourcePtr);

                *pixelDestPtr = pixelValue; // assign the first pixel then the loop
                for (int row = 0; row < height; row++) 
                {
                    pixelSourcePtr = (int*)pixelsSourceRow;
                    pixelDestPtr = pixelDestRow; 
                    for (int col = 0; col < width; col++, pixelSourcePtr++, pixelDestPtr++)
                    {
                            // Only quantize the pixel if it has changed
                        if (*pixelPrevPtr != *pixelSourcePtr)
                        {
                            pixelValue = QuantizePixel((Color32*)pixelSourcePtr);

                            pixelPrevPtr = pixelSourcePtr;
                        }
                        *pixelDestPtr = pixelValue;
                    }
                    pixelsSourceRow += sourceData.Stride;
                    pixelDestRow += outputData.Stride; 
                }
            }
            finally
            {
                // Ensure that we unlock the output bits
                output.UnlockBits(outputData);
            }
        }

        /// <summary>
        /// Override this to process the pixel in the first pass of the algorithm
        /// </summary>
        /// <param name="pixel">The pixel to quantize</param>
        /// <remarks>
        /// This function need only be overridden if your quantize algorithm needs two passes,
        /// such as an Octree quantizer.
        /// </remarks>
        protected virtual void InitialQuantizePixel(Color32* pixel)
        {
        }

        /// <summary>
        /// Override this to process the pixel in the second pass of the algorithm
        /// </summary>
        /// <param name="pixel">The pixel to quantize</param>
        /// <returns>The quantized value</returns>
        protected abstract byte QuantizePixel(Color32* pixel);

        /// <summary>
        /// Retrieve the palette for the quantized image
        /// </summary>
        /// <param name="original">Any old palette, this is overrwritten</param>
        /// <returns>The new color palette</returns>
        protected abstract ColorPalette GetPalette(ColorPalette original, bool clearPalette);
        protected abstract ColorPalette CLearPalette(ColorPalette original);
        /// <summary>
        /// Flag used to indicate whether a single pass or two passes are needed for quantization.
        /// </summary>
        private readonly bool _singlePass = singlePass;

        /// <summary>
        /// Struct that defines a 32 bpp colour
        /// </summary>
        /// <remarks>
        /// This struct is used to read data from a 32 bits per pixel image
        /// in memory, and is ordered in this manner as this is the way that
        /// the data is layed out in memory
        /// </remarks>
        [StructLayout(LayoutKind.Explicit)]
        public struct Color32
        {
            /// <summary>
            /// Holds the blue component of the colour
            /// </summary>
            [FieldOffset(0)]
            public byte Blue;
            /// <summary>
            /// Holds the green component of the colour
            /// </summary>
            [FieldOffset(1)]
            public byte Green;
            /// <summary>
            /// Holds the red component of the colour
            /// </summary>
            [FieldOffset(2)]
            public byte Red;
            /// <summary>
            /// Holds the alpha component of the colour
            /// </summary>
            [FieldOffset(3)]
            public byte Alpha;

            /// <summary>
            /// Permits the color32 to be treated as an int32
            /// </summary>
            [FieldOffset(0)]
            public int Argb;

            /// <summary>
            /// Return the color for this Color32 object
            /// </summary>
            public Color Color
            {
                get => Color.FromArgb(Alpha, Red, Green, Blue);
            }
        }
    }
}
