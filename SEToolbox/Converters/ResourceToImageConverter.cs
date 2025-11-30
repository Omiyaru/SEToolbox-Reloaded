using System;
using System.IO;
using System.Reflection;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace SEToolbox.Converters
{
    public class ResourceToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string imageParameter = value as string ?? parameter as string;
            var stringNullOrEmpty = string.IsNullOrEmpty(imageParameter) as bool?;

            if (stringNullOrEmpty == true)
            {
                return null;
            }

            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream(imageParameter);
            var bitmapImage = new BitmapImage();
           
            if (stream == null)
            {
                 var bitmap = System.Drawing.Image.FromStream(stream) ?? Properties.Resources.ResourceManager.GetObject(imageParameter) as System.Drawing.Bitmap;
                     
                try
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        bitmap?.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = memoryStream;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.EndInit();
                        return bitmapImage;
                    }
                }
                catch
                {
                    return null;
                }
            }
            if (stream != null || Uri.TryCreate(imageParameter, UriKind.RelativeOrAbsolute, out Uri imageUriSource))
                {
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                return bitmapImage;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
