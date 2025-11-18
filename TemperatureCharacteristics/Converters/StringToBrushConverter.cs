using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TemperatureCharacteristics.Converters
{
    public class StringToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string colorString)
            {
                try
                {
                    return (Brush)new BrushConverter().ConvertFromString(colorString);
                }
                catch
                {
                    return Brushes.White; //default色（変換エラー時White）
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
