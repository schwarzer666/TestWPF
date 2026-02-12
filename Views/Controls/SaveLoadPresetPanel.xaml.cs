using System.Windows;
using System.Windows.Controls;

namespace TemperatureCharacteristics.Views.Controls
{
    public partial class SaveLoadPresetPanel : UserControl
    {
        public SaveLoadPresetPanel()
        {
            InitializeComponent();
        }
        public object TabViewModel
        {
            get => GetValue(TabViewModelProperty);
            set => SetValue(TabViewModelProperty, value);
        }
        public static readonly DependencyProperty TabViewModelProperty = DependencyProperty.Register(
                nameof(TabViewModel),
                typeof(object),
                typeof(SaveLoadPresetPanel),
                new PropertyMetadata(null));
    }
}
