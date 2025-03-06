using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;


namespace POKEMON.Converters
{
    public class UrlToImageConverter : IValueConverter
    {
        public object? Convert(Object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string url)
            {
                return LoadImageFromUrl(url).Result;
            }
            return null;
        }

        public object? ConvertBack(Object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }

        private async Task<Bitmap> LoadImageFromUrl(string url)
        {
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetByteArrayAsync(url).ConfigureAwait(false);
                using (var stream = new System.IO.MemoryStream(response))
                {
                    var bitMap = new Bitmap(stream);
                    return bitMap;
                }
            }
        }
    }
    
}
