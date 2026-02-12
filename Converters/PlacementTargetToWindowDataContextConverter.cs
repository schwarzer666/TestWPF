using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TemperatureCharacteristics.Converters
{
    public class PlacementTargetToWindowDataContextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FrameworkElement fe)
            {
                var window = Window.GetWindow(fe);
                return window?.DataContext;   // MainViewModel
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

}

