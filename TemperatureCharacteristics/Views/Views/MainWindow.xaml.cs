using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TemperatureCharacteristics.Services.Data;
using TemperatureCharacteristics.ViewModels;
using USBcommunication;             //CommUSB.cs

namespace TemperatureCharacteristics.Views
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            DataService dataService = new DataService();
            _viewModel = new MainViewModel(dataService);
            DataContext = _viewModel;                   //SweepTab,DelayTab,VITabを描写
        }
        //*************************************************
        //動作
        // メインウィンドウ拡張メソッド
        //*************************************************
        private void ExpandWindowCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToElementState(this, "ExpandedState", true);
        }

        private void ExpandWindowCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToElementState(this, "NormalState", true);
        }
        //************************************
        //動作
        // USB ID ListBoxのテキストを選択した時
        // クリップボードの内容をクリアし
        // クリップボードに選択した内容を張り付け
        //コメント
        // USBID_ListBoxで選択しているアイテムがある状態でListBoxが更新されるとnullになってしまいエラーとなる為(nullではstring変換できない
        // null条件演算子（?.）を使ってnullの場合、selectitemにnullが代入され、ToString()は呼び出されない
        //************************************
        private void USBID_ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Clipboard.Clear();
            string? selectitem = USBID_ListBox.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(selectitem))
                Clipboard.SetText(USBID_ListBox.SelectedItem.ToString());
        }

        //****************************************************************************
        //動作
        // TabがLoadされた時、各タブのUIを強制更新
        //****************************************************************************
        private void TabControl_Loaded(object sender, RoutedEventArgs e)
        {
            TabControl? tabControl = sender as TabControl;
            //最初のタブを選択
            tabControl.SelectedIndex = 0;
        }
    }

}
