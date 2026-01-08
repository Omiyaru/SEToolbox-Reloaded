using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Data;
using System.Windows.Media.Imaging;


namespace SEToolbox.Converters
{
    public class ResourceToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            string imageParameter = $"{value ?? parameter}";
            BitmapImage bitmapImage = null;

            // Application Resource - File Build Action is marked as None, but stored in Resources.resx
            // parameter= myresourceimagename

            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream(imageParameter);

            Bitmap bitmap;
            try
            {
                bitmap = Properties.ImageResources.ResourceManager.GetObject(imageParameter) as Bitmap;//?? Image.FromFile(imageParameter) as Bitmap;
            }
            catch
            {
                throw new FileNotFoundException($"Resource image not found: {imageParameter}");
            }
            static MemoryStream action() => new();
            var memoryStream = action();
            //var uri = new Uri(imageParameter, UriKind.RelativeOrAbsolute);

            //value = Support.Conditional.NullCoalesced(value, bitmap, stream, memoryStream , uri);
            
            switch (value)
            {
                case object when bitmap != null && value is Bitmap && memoryStream != null:
                    bitmap?.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memoryStream;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    return bitmapImage;
                case object when stream != null && value is Stream:
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = stream;
                    bitmapImage.EndInit();
                    return bitmapImage;
             
                case string when string.IsNullOrEmpty(imageParameter):
                default:
                    return null;
            }
        }
                // case object when Uri.TryCreate(imageParameter, UriKind.RelativeOrAbsolute, out Uri imageUriSource):
                //     bitmapImage.BeginInit();
                //     bitmapImage.UriSource = imageUriSource;
                //     bitmapImage.EndInit();
                //     return bitmapImage;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}