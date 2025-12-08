using System.Windows;
using System.Windows.Controls;
using USBcommunication;             //CommUSB.cs
using TemperatureCharacteristics.Services;

namespace TemperatureCharacteristics
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly USBcomm _getUSBID;         //フィールド変数getUSBID
        private readonly USBcomm _commSend;          //フィールド変数commSend
        private readonly USBcomm _commQuery;         //フィールド変数commQuery
        private readonly List<(CheckBox checkBox, ComboBox? textBox_ID, TextBox? textBox_NAME)> meas_inst; //フィールド変数meas_inst

        public MainWindow()
        {
            InitializeComponent();
            _getUSBID = USBcomm.Instance;           //インスタンス生成(=初期化)を別クラス内で実行
            _commSend = USBcomm.Instance;            //インスタンス生成(=初期化)を別クラス内で実行
            _commQuery = USBcomm.Instance;           //インスタンス生成(=初期化)を別クラス内で実行

            CheckBox Sweepset_check = new CheckBox();   //meas_instにSweep設定用の仮想CheckBoxを追加
            CheckBox Delayset_check = new CheckBox();   //meas_instにDelay設定用の仮想CheckBoxを追加
            CheckBox DetRelset_check = new CheckBox();  //meas_instにDetect/Release条件用の仮想CheckBoxを追加
            meas_inst = new List<(CheckBox, ComboBox?, TextBox?)>    //コンストラクタで測定器リストを初期化
            {
                (CheckBox_Ins1, TextBox_ID1, TextBox_Name1),    //電源1
                (CheckBox_Ins2, TextBox_ID2, TextBox_Name2),    //電源2
                (CheckBox_Ins3, TextBox_ID3, TextBox_Name3),    //電源3
                (CheckBox_Ins4, TextBox_ID4, TextBox_Name4),    //電源4
                (CheckBox_Ins5, TextBox_ID5, null),             //OSC   名前空間null
                (CheckBox_Ins6, TextBox_ID6, null),             //PG    名前空間null
                (CheckBox_Ins7, TextBox_ID7, TextBox_Name7),    //DMM1
                (CheckBox_Ins8, TextBox_ID8, TextBox_Name8),    //DMM2
                (CheckBox_Ins9, TextBox_ID9, TextBox_Name9),    //DMM3
                (CheckBox_Ins10, TextBox_ID10, TextBox_Name10), //DMM4
                (CheckBox_Ins11, TextBox_ID11, null),           //THERMO   名前空間null
                (CheckBox_Ins12, TextBox_ID12, null),           //リレー   名前空間null
                (Sweepset_check, null, null),                   //Sweep設定用
                (DetRelset_check, null, null),                  //Detect/Release条件用
                (Delayset_check, null, null),                   //Delay設定用
            };
            DataService dataService = new DataService();
            _viewModel = new MainViewModel(meas_inst, dataService);
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
