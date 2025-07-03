using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using Windows.Storage.FileProperties;

namespace Novatune.Converters
{
    public partial class ImageConverter : IValueConverter
    {
        public object? Convert (object value, Type targetType, object parameter, string language)
        {
            if (value is StorageItemThumbnail thumbnail)
            {
                var bitmap = new BitmapImage ();
                bitmap.SetSource (thumbnail);
                return bitmap;
            }
            return null;
        }

        public object ConvertBack (object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException ();
        }
    }
}
