using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TemperatureCharacteristics.Converters
{
    public class TagToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string tag)
            {
                switch (tag)
                {
                    case "1": return Brushes.Gold;
                    case "2": return Brushes.SpringGreen;
                    case "3": return Brushes.HotPink;
                    case "4": return Brushes.DeepSkyBlue;
                    case "EXT": return Brushes.LightGray;
                    default: return Brushes.White;
                }
            }
            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
