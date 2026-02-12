using System.Globalization;
using System.Windows.Data;

namespace TemperatureCharacteristics.Converters
{
    //****************************************************************************
    //変換
    // SourceFuncIndexの内容によってTrue/Falseを返す
    //****************************************************************************
    public class SourceFuncIndexToEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int index)
            {
                //SourceFuncIndex != 4(Pulse)以外の場合にTrueを返す
                return index != 4;
            }
            return false;   //Pulseの時はグレーアウト
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}