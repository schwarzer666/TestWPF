using System.Windows;
using System.Windows.Controls;
using TemperatureCharacteristics.Models;
using TemperatureCharacteristics.ViewModels;

namespace TemperatureCharacteristics.Views.Controls
{
    public partial class OscSettingHorizontalControl : UserControl
    {
        public OscSettingHorizontalControl()
        {
            InitializeComponent();
        }

        public OscConfig OscConfig
        {
            get => (OscConfig)GetValue(OscConfigProperty);
            set => SetValue(OscConfigProperty, value);
        }

        public static readonly DependencyProperty OscConfigProperty =
            DependencyProperty.Register(
                nameof(OscConfig),
                typeof(OscConfig),
                typeof(OscSettingHorizontalControl),
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
                typeof(OscSettingHorizontalControl),
                new PropertyMetadata(null));
    }
}
