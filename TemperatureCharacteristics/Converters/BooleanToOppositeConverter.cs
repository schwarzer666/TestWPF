using System.Globalization;
using System.Windows.Data;

namespace TemperatureCharacteristics.Converters
{
    public class BooleanToOppositeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                if (parameter is string param && param.Contains("|"))
                {
                    var options = param.Split('|');
                    return boolValue ? options[1] : options[0];         //true: "停止", false: "測定開始"
                }
                return !boolValue;              //IsEnabled の場合
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}