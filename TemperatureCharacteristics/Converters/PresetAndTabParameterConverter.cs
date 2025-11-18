using System.Globalization;
using System.Windows.Data;
using TemperatureCharacteristics.Models;
using DelayTab;
using SweepTab;
using VITab;

namespace TemperatureCharacteristics.Converters
{
    public class PresetAndTabParameterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string tabType = null;
            string tabId = null;

            if (value is SweepTabViewModel sweepTab)
            {
                tabType = "Sweep";
                tabId = sweepTab.Id;
            }
            else if (value is DelayTabViewModel delayTab)
            {
                tabType = "Delay";
                tabId = delayTab.Id;
            }
            else if (value is VITabViewModel viTab)
            {
                tabType = "VI";
                tabId = viTab.Id;
            }

            if (tabType != null && tabId != null)
            {
                return new PresetAndTabParameter
                {
                    TabType = tabType,
                    TabId = tabId
                };
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}