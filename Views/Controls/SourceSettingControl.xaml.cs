using System.Windows;
using System.Windows.Controls;
using TemperatureCharacteristics.Models;
using TemperatureCharacteristics.ViewModels;

namespace TemperatureCharacteristics.Views.Controls
{
    public partial class SourceSettingControl : UserControl
    {
        public SourceSettingControl()
        {
            InitializeComponent();
        }

        public SourceConfig SourceConfig
        {
            get => (SourceConfig)GetValue(SourceConfigProperty);
            set => SetValue(SourceConfigProperty, value);
        }

        public static readonly DependencyProperty SourceConfigProperty =
            DependencyProperty.Register(
                nameof(SourceConfig), 
                typeof(SourceConfig), 
                typeof(SourceSettingControl),
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
                typeof(SourceSettingControl),
                new PropertyMetadata(null));
    }

}
