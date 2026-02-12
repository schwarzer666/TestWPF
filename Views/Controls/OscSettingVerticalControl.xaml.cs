using System.Windows;
using System.Windows.Controls;
using TemperatureCharacteristics.Models;
using TemperatureCharacteristics.ViewModels;

namespace TemperatureCharacteristics.Views.Controls
{
    public partial class OscSettingVerticalControl : UserControl
    {
        public OscSettingVerticalControl()
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
                typeof(OscSettingVerticalControl),
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
                typeof(OscSettingVerticalControl),
                new PropertyMetadata(null));

        //*************************************************
        //TrigDirectionalIndexの選択用
        //SweepTabとDelayTabでバインドが違うため
        // SweepTab→OscConfigのプロパティをバインド
        // DelayTab→DelayTabViewModelのプロパティをバインド（ロジックがあるため）
        //*************************************************
        public int SelectedTrigDirectionalIndex
        {
            get => (int)GetValue(SelectedTrigDirectionalIndexProperty);
            set => SetValue(SelectedTrigDirectionalIndexProperty, value);
        }

        public static readonly DependencyProperty SelectedTrigDirectionalIndexProperty =
            DependencyProperty.Register(
                nameof(SelectedTrigDirectionalIndex),
                typeof(int),
                typeof(OscSettingVerticalControl),
                new PropertyMetadata(0));

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
                typeof(OscSettingVerticalControl),
                new PropertyMetadata("OSC設定1"));
    }

}
