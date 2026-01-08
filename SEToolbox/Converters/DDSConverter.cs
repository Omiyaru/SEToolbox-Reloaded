

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
             value ??= null;
             if (value is not string fileName )
            {
                throw new NotSupportedException($"{GetType().FullName} cannot convert from {value.GetType().FullName}.");
            }

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


            return Cache[cacheKey] = imageSource ??= null ?? imageSource;
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
            return Conditional.False(-1, width, height) ? $"{fileName},{width},{height}": fileName;
        }

        private static ImageSource LoadPngImage(string fileName, int width, int height)
        {
            try
            {
                using Stream textureStream = MyFileSystem.OpenRead(fileName);
                using Bitmap bitmap = (Bitmap)Image.FromStream(textureStream, true);
                BitmapImage bitmapImage = new();

                using MemoryStream memStream = new();
                bitmap.Save(memStream, System.Drawing.Imaging.ImageFormat.Png);
                memStream.Position = 0; // Reset stream position

                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memStream;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                if (width > 0 && height > 0)
                {
                    return RescaleBitmap(bitmapImage, width, height);
                }
                Cache.Add(fileName, bitmapImage);
                return bitmapImage;
            }
            catch
            {
                throw new FileNotFoundException($"File not found: {fileName}" + Environment.NewLine + Environment.StackTrace);
            }
        }

        private static ImageSource LoadDdsImage(string fileName, int width, int height, bool noAlpha)
        {
            using Stream textureStream = MyFileSystem.OpenRead(fileName);
            ImageSource image = TexUtil.CreateImage(textureStream, 0, width, height, noAlpha);
              Cache.Add(fileName, image);
                return image;
        }

        private static ImageSource RescaleBitmap(BitmapImage bitmapImage, int width, int height)
        {
            TransformedBitmap image = new(bitmapImage, new ScaleTransform((double)width / bitmapImage.PixelWidth, (double)height / bitmapImage.PixelHeight));
            return image;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string message = string.Format($"{GetType().FullName}  does not support converting back." , CultureInfo.CurrentUICulture);
            throw new NotSupportedException(message);
        }

        public static void ClearCache()
        {
            Cache.Clear();
        }
    }
}