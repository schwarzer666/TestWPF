using System.Globalization;
using System.Windows.Data;

namespace TemperatureCharacteristics.Converters
{
    //****************************************************************************
    //変換
    // DetectReleaseIndexの内容によってTrue/Falseを返す
    //****************************************************************************
    public class DetectReleaseIndexToEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int index)
            {
                //DetectReleaseIndex == 1(normal+α)の場合にTrueを返す
                return index == 1;
            }
            return false;   //デフォルトはグレーアウト
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}