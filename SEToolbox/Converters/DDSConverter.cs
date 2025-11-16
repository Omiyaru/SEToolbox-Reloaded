

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SEToolbox.Support;
using VRage.FileSystem;
using TexUtil = SEToolbox.ImageLibrary.ImageTextureUtil;

namespace SEToolbox.Converters
{
    public class DDSConverter : IValueConverter
    {
        private static readonly Dictionary<string, ImageSource> Cache = [];

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string fileName)
                throw new NotSupportedException($"{GetType().FullName} cannot convert from {value.GetType().FullName}.");

            (int width, int height, bool noAlpha) = ParseSizeParameter(parameter as string);
            string cacheKey = GenerateCacheKey(fileName, width, height);

            if (Cache.TryGetValue(cacheKey, out ImageSource cachedImage))
            {
                return cachedImage;
            }

            string extension = Path.GetExtension(fileName).ToLower();
            ImageSource imageSource = extension switch
            {
                ".png" => LoadPngImage(fileName, width, height),
                ".dds" => LoadDdsImage(fileName, width, height, noAlpha),
                _ => null
            };

            if (imageSource != null)
            {
                Cache[cacheKey] = imageSource;
            }

            return imageSource;
        }

         private static (int width, int height, bool noAlpha) ParseSizeParameter(string sizeParameter)
        {
            string[] sizeArray = sizeParameter.Split(',');
            int width = sizeArray.Length > 0 && int.TryParse(sizeArray[0], out int w) ? w : -1;
            int height = sizeArray.Length > 1 && int.TryParse(sizeArray[1], out int h) ? h : -1;
            bool noAlpha = sizeArray.Length > 2 && sizeArray[2].Equals("noalpha", StringComparison.OrdinalIgnoreCase);
            return (width, height, noAlpha);
        }

        private static string GenerateCacheKey(string fileName, int width, int height)
        {
            return Conditional.ConditionNot(-1,width, height) ? $"{fileName},{width},{height}": fileName;
        }

        private static ImageSource LoadPngImage(string fileName, int width, int height)
        {
            try
            {
                using Stream textureStream = MyFileSystem.OpenRead(fileName);
                using Bitmap bitmap = (Bitmap)Image.FromStream(textureStream, true);
                BitmapImage bitmapImage = new();

                using MemoryStream ms = new();
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0; // Reset stream position

                bitmapImage.BeginInit();
                bitmapImage.StreamSource = ms;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                if (width > 0 && height > 0)
                {
                    return RescaleBitmap(bitmapImage, width, height);
                }

                return bitmapImage;
            }
            catch
            {
                return null;
            }
        }

        private static ImageSource LoadDdsImage(string fileName, int width, int height, bool noAlpha)
        {
            using Stream textureStream = MyFileSystem.OpenRead(fileName);
            return TexUtil.CreateImage(textureStream, 0, width, height, noAlpha);
        }

        private static ImageSource RescaleBitmap(BitmapImage bitmapImage, int width, int height)
        {
            TransformedBitmap image = new(bitmapImage, new ScaleTransform((double)width / bitmapImage.PixelWidth, (double)height / bitmapImage.PixelHeight));
            return image;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // TODO: #21 localize
            throw new NotSupportedException($"{GetType().FullName} does not support converting back.");
        }

        public static void ClearCache()
        {
            Cache.Clear();
        }
    }
}