using System.Windows;
using System.Windows.Controls;
using TemperatureCharacteristics.Models;
using TemperatureCharacteristics.ViewModels;

namespace TemperatureCharacteristics.Views.Controls
{
    public partial class ConstSettingControl : UserControl
    {
        public ConstSettingControl()
        {
            InitializeComponent();
        }

        public ConstConfig ConstConfig
        {
            get => (ConstConfig)GetValue(ConstConfigProperty);
            set => SetValue(ConstConfigProperty, value);
        }

        public static readonly DependencyProperty ConstConfigProperty =
            DependencyProperty.Register(
                nameof(ConstConfig),
                typeof(ConstConfig),
                typeof(ConstSettingControl),
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
                typeof(ConstSettingControl),
                new PropertyMetadata(null));
    }
}
