using System.Windows;
using System.Windows.Controls;
using TemperatureCharacteristics.Models;
using TemperatureCharacteristics.ViewModels;

namespace TemperatureCharacteristics.Views.Controls
{
    public partial class PulseGenSettingControl : UserControl
    {
        public PulseGenSettingControl()
        {
            InitializeComponent();
        }

        public PulseGenConfig PulseGenConfig
        {
            get => (PulseGenConfig)GetValue(PulseGenConfigProperty);
            set => SetValue(PulseGenConfigProperty, value);
        }

        public static readonly DependencyProperty PulseGenConfigProperty =
            DependencyProperty.Register(
                nameof(PulseGenConfig),
                typeof(PulseGenConfig),
                typeof(PulseGenSettingControl),
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
                typeof(PulseGenSettingControl),
                new PropertyMetadata(null));

        //*************************************************
        //TrigOut ComboBoxを表示するかどうか
        //*************************************************
        public bool ShowTrigOutComboBox
        {
            get => (bool)GetValue(ShowTrigOutComboBoxProperty);
            set => SetValue(ShowTrigOutComboBoxProperty, value);
        }

        public static readonly DependencyProperty ShowTrigOutComboBoxProperty =
            DependencyProperty.Register(
                nameof(ShowTrigOutComboBox),
                typeof(bool),
                typeof(PulseGenSettingControl),
                new PropertyMetadata(false));

        //*************************************************
        //TrigOutIndexの選択用
        //SweepTabとDelayTabでバインドが違うため
        // SweepTab→バインドしない（このオプションを記述しない）
        // DelayTab→DelayTabViewModelのプロパティをバインド（ロジックがあるため）
        //*************************************************
        public int SelectedTrigOutIndex
        {
            get => (int)GetValue(SelectedTrigOutIndexProperty);
            set => SetValue(SelectedTrigOutIndexProperty, value);
        }

        public static readonly DependencyProperty SelectedTrigOutIndexProperty =
            DependencyProperty.Register(
                nameof(SelectedTrigOutIndex),
                typeof(int),
                typeof(PulseGenSettingControl),
                new PropertyMetadata(0));

        //*************************************************
        //Sweep専用のToolTipを表示するかどうか
        //*************************************************
        public bool ShowToolTip
        {
            get => (bool)GetValue(ShowToolTipProperty);
            set => SetValue(ShowToolTipProperty, value);
        }

        public static readonly DependencyProperty ShowToolTipProperty =
            DependencyProperty.Register(
                nameof(ShowToolTip),
                typeof(bool),
                typeof(PulseGenSettingControl),
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
                typeof(PulseGenSettingControl),
                new PropertyMetadata("PG設定"));
    }

}
