using System.Windows;
using System.Windows.Controls;
using TemperatureCharacteristics.Models;
using TemperatureCharacteristics.ViewModels;

namespace TemperatureCharacteristics.Views.Controls
{
    public partial class DetectReleaseControl : UserControl
    {
        public DetectReleaseControl()
        {
            InitializeComponent();
        }

        public DetectReleaseConfig DetectReleaseConfig
        {
            get => (DetectReleaseConfig)GetValue(DetectReleaseConfigProperty);
            set => SetValue(DetectReleaseConfigProperty, value);
        }

        public static readonly DependencyProperty DetectReleaseConfigProperty =
            DependencyProperty.Register(
                nameof(DetectReleaseConfig),
                typeof(DetectReleaseConfig),
                typeof(DetectReleaseControl),
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
                typeof(DetectReleaseControl),
                new PropertyMetadata(null));

        //*************************************************
        //Sweep専用のAction ComboBoxを表示するかどうか
        //*************************************************
        public bool ShowAction
        {
            get => (bool)GetValue(ShowActionProperty);
            set => SetValue(ShowActionProperty, value);
        }

        public static readonly DependencyProperty ShowActionProperty =
            DependencyProperty.Register(
                nameof(ShowAction),
                typeof(bool),
                typeof(DetectReleaseControl),
                new PropertyMetadata(false));

        //*************************************************
        //GroupBoxのHeader名
        //*************************************************
        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(
                nameof(Header),
                typeof(string),
                typeof(DetectReleaseControl),
                new PropertyMetadata("Sweep検出/復帰挙動"));
    }

}
