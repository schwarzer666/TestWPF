using System.Globalization;
using System.Windows.Data;
using TemperatureCharacteristics.Models;
using TemperatureCharacteristics.ViewModels.Tabs.Delay;
using TemperatureCharacteristics.ViewModels.Tabs.Sweep;
using TemperatureCharacteristics.ViewModels.Tabs.VI;

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
                tabId = sweepTab.TabId;
            }
            else if (value is DelayTabViewModel delayTab)
            {
                tabType = "Delay";
                tabId = delayTab.TabId;
            }
            else if (value is VITabViewModel viTab)
            {
                tabType = "VI";
                tabId = viTab.TabId;
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