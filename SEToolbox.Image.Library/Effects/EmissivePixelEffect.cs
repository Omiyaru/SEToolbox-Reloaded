namespace SEToolbox.ImageLibrary.Effects
{
    /// <summary>
    /// Summary description for EmissiveEffect.
    /// </summary>
    /// <remarks>
    /// Construct the Emissive pixel effect
    /// </remarks>
    /// <remarks>
    /// Emissive pixel effect only requires a single effect step
    /// </remarks>
    public unsafe class EmissivePixelEffect(byte alphaEmmissiveValue) : PixelEffect(true)
    {
        private readonly byte _alphaEmmissiveValue = alphaEmmissiveValue;

        /// <summary>
        /// Override this to process the pixel in the second pass of the algorithm
        /// </summary>
        /// <param name="pixel">The pixel to quantize</param>
        /// <param name="destinationPixel"></param>
        /// <returns>The quantized value</returns>
        protected override void QuantizePixel(Color32* pixel, Color32* destinationPixel)
        {
            destinationPixel->Red = pixel->Red;
            destinationPixel->Green = pixel->Green;
            destinationPixel->Blue = pixel->Blue;

            destinationPixel->Alpha = (byte)(pixel->Alpha == _alphaEmmissiveValue ? 255 : 0);
        }
    }
}
