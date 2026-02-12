using System.Windows;
using System.Windows.Controls;
using TemperatureCharacteristics.Models;
using TemperatureCharacteristics.ViewModels;

namespace TemperatureCharacteristics.Views.Controls
{
    public partial class DmmSettingControl : UserControl
    {
        public DmmSettingControl()
        {
            InitializeComponent();
        }

        public DmmConfig DmmConfig
        {
            get => (DmmConfig)GetValue(DmmConfigProperty);
            set => SetValue(DmmConfigProperty, value);
        }

        public static readonly DependencyProperty DmmConfigProperty =
            DependencyProperty.Register(
                nameof(DmmConfig),
                typeof(DmmConfig),
                typeof(DmmSettingControl),
                new PropertyMetadata(null));

        public ResourceViewModel Resources
        {
            get => (ResourceViewModel)GetValue(ResourcesProperty);
            set => SetValue(ResourcesProperty, value);
        }

        public static readonly DependencyProperty ResourcesProperty =
            DependencyProperty.Register(
                nameof(Resources),
                typeof(ResourceViewModel),
                typeof(DmmSettingControl),
                new PropertyMetadata(null));
    }
}
