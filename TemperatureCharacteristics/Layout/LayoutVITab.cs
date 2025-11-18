using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace VITab
{
    public class VITabViewModel : INotifyPropertyChanged
    {
        //タブヘッダー
        private string _itemHeader;             //Itemタブ入力欄
        //CheckBox
        private bool _measureOn;                //測定ON/OFF CheckBox
        private bool _meas1DispOn;              //DMM1のDisplay ON/OFF CheckBox
        private bool _meas2DispOn;              //DMM2のDisplay ON/OFF CheckBox
        private bool _meas3DispOn;              //DMM3のDisplay ON/OFF CheckBox
        private bool _meas4DispOn;              //DMM4のDisplay ON/OFF CheckBox
        //TextBox
        private string _const1;                 //constant電源1 value
        private string _const2;                 //constant電源2 value
        private string _const3;                 //constant電源3 value
        private string _const4;                 //constant電源4 value
        private string _detectReleaseA;         //Sweep検出復帰の際にSweep電源をStart値に戻す＋別電源Aを追加で使用する場合の値
        private string _detectReleaseB;         //Sweep検出復帰の際にSweep電源をStart値に戻す＋別電源Bを追加で使用する場合の値
        private string _checkTime;              //初期状態検出復帰の際のwait value（ex.tvdrel1測定時tvdet1を指定
        private string _stanbyValue;            //standby value
        private string _meas1plc;               //DMM1のNPLC value
        private string _meas2plc;               //DMM2のNPLC value
        private string _meas3plc;               //DMM3のNPLC value
        private string _meas4plc;               //DMM4のNPLC value
        private string _sourceLimit1;           //Source1のLimit value
        private string _sourceLimit2;           //Source2のLimit value
        private string _sourceLimit3;           //Source3のLimit value
        private string _sourceLimit4;           //Source4のLimit value
        //ComboBox
        private int _sourcefunc1Index;          //電源1の動作（初期値Sweep
        private int _sourcefunc2Index;          //電源2の動作（初期値constant1→値は_const1 .Tagはconst
        private int _sourcefunc3Index;          //電源3の動作（初期値constant2→値は_const2 .Tagはconst
        private int _sourcefunc4Index;          //電源4の動作（初期値constant3→値は_const3 .Tagはconst
        private int _sourcemode1Index;          //電源1の印加モード
        private int _sourcemode2Index;          //電源2の印加モード
        private int _sourcemode3Index;          //電源3の印加モード
        private int _sourcemode4Index;          //電源4の印加モード
        private int _sourceRang1Index;          //電源1のレンジ
        private int _sourceRang2Index;          //電源2のレンジ
        private int _sourceRang3Index;          //電源3のレンジ
        private int _sourceRang4Index;          //電源4のレンジ
        private int _const1UnitIndex;           //Constant電源1 Unit
        private int _const2UnitIndex;           //Constant電源2 Unit
        private int _const3UnitIndex;           //Constant電源3 Unit
        private int _const4UnitIndex;           //Constant電源4 Unit
        private int _detectSourceAIndex;        //検出復帰の際にPGをStart値に戻す＋別電源を追加で使用する場合の電源を指定A（VMを想定
        private int _detectSourceBIndex;        //検出復帰の際にPGをStart値に戻す＋別電源を追加で使用する場合の電源を指定B（CSを想定
        private int _detectUnitAIndex;          //上記検出復帰時に使用する電源AのUnit
        private int _detectUnitBIndex;          //上記検出復帰時に使用する電源BのUnit
        private int _checkTimeUnitIndex;        //初期状態検出復帰の際のwait Unit
        private int _stanbyUnitIndex;           //standby Unit
        private int _measMode1Index;            //DMM1の測定モード
        private int _measMode2Index;            //DMM2の測定モード
        private int _measMode3Index;            //DMM3の測定モード
        private int _measMode4Index;            //DMM4の測定モード
        private int _measRang1Index;            //DMM1のレンジ
        private int _measRang2Index;            //DMM2のレンジ
        private int _measRang3Index;            //DMM3のレンジ
        private int _measRang4Index;            //DMM4のレンジ
        private int _measTrigIndex;             //DMMのトリガソース
        private int _sourceLimit1Index;         //電源1のLimitレンジ
        private int _sourceLimit2Index;         //電源2のLimitレンジ
        private int _sourceLimit3Index;         //電源3のLimitレンジ
        private int _sourceLimit4Index;         //電源4のLimitレンジ

        // プロパティ
        public string ItemHeader { get => _itemHeader; set { _itemHeader = value; OnPropertyChanged(); } }
        public bool MeasureOn { get => _measureOn; set { _measureOn = value; OnPropertyChanged(); } }
        public string Const1 { get => _const1; set { _const1 = value; OnPropertyChanged(); } }
        public string Const2 { get => _const2; set { _const2 = value; OnPropertyChanged(); } }
        public string Const3 { get => _const3; set { _const3 = value; OnPropertyChanged(); } }
        public string Const4 { get => _const4; set { _const4 = value; OnPropertyChanged(); } }
        public int Const1UnitIndex { get => _const1UnitIndex; set { _const1UnitIndex = value; OnPropertyChanged(); } }
        public int Const2UnitIndex { get => _const2UnitIndex; set { _const2UnitIndex = value; OnPropertyChanged(); } }
        public int Const3UnitIndex { get => _const3UnitIndex; set { _const3UnitIndex = value; OnPropertyChanged(); } }
        public int Const4UnitIndex { get => _const4UnitIndex; set { _const4UnitIndex = value; OnPropertyChanged(); } }
        public string StanbyValue { get => _stanbyValue; set { _stanbyValue = value; OnPropertyChanged(); } }
        public int StanbyUnitIndex { get => _stanbyUnitIndex; set { _stanbyUnitIndex = value; OnPropertyChanged(); } }
        public int SourceFunc1Index { get => _sourcefunc1Index; set { _sourcefunc1Index = value; OnPropertyChanged(); } }
        public int SourceFunc2Index { get => _sourcefunc2Index; set { _sourcefunc2Index = value; OnPropertyChanged(); } }
        public int SourceFunc3Index { get => _sourcefunc3Index; set { _sourcefunc3Index = value; OnPropertyChanged(); } }
        public int SourceFunc4Index { get => _sourcefunc4Index; set { _sourcefunc4Index = value; OnPropertyChanged(); } }
        public int SourceMode1Index { get => _sourcemode1Index; set { _sourcemode1Index = value; OnPropertyChanged(); } }
        public int SourceMode2Index { get => _sourcemode2Index; set { _sourcemode2Index = value; OnPropertyChanged(); } }
        public int SourceMode3Index { get => _sourcemode3Index; set { _sourcemode3Index = value; OnPropertyChanged(); } }
        public int SourceMode4Index { get => _sourcemode4Index; set { _sourcemode4Index = value; OnPropertyChanged(); } }
        public int SourceRang1Index { get => _sourceRang1Index; set { _sourceRang1Index = value; OnPropertyChanged(); } }
        public int SourceRang2Index { get => _sourceRang2Index; set { _sourceRang2Index = value; OnPropertyChanged(); } }
        public int SourceRang3Index { get => _sourceRang3Index; set { _sourceRang3Index = value; OnPropertyChanged(); } }
        public int SourceRang4Index { get => _sourceRang4Index; set { _sourceRang4Index = value; OnPropertyChanged(); } }
        public string SourceLimit1 { get => _sourceLimit1; set { _sourceLimit1 = value; OnPropertyChanged(); } }
        public string SourceLimit2 { get => _sourceLimit2; set { _sourceLimit2 = value; OnPropertyChanged(); } }
        public string SourceLimit3 { get => _sourceLimit3; set { _sourceLimit3 = value; OnPropertyChanged(); } }
        public string SourceLimit4 { get => _sourceLimit4; set { _sourceLimit4 = value; OnPropertyChanged(); } }
        public int SourceLimit1Index { get => _sourceLimit1Index; set { _sourceLimit1Index = value; OnPropertyChanged(); } }
        public int SourceLimit2Index { get => _sourceLimit2Index; set { _sourceLimit2Index = value; OnPropertyChanged(); } }
        public int SourceLimit3Index { get => _sourceLimit3Index; set { _sourceLimit3Index = value; OnPropertyChanged(); } }
        public int SourceLimit4Index { get => _sourceLimit4Index; set { _sourceLimit4Index = value; OnPropertyChanged(); } }
        public int MeasMode1Index { get => _measMode1Index; set { _measMode1Index = value; OnPropertyChanged(); } }
        public int MeasMode2Index { get => _measMode2Index; set { _measMode2Index = value; OnPropertyChanged(); } }
        public int MeasMode3Index { get => _measMode3Index; set { _measMode3Index = value; OnPropertyChanged(); } }
        public int MeasMode4Index { get => _measMode4Index; set { _measMode4Index = value; OnPropertyChanged(); } }
        public int MeasRang1Index { get => _measRang1Index; set { _measRang1Index = value; OnPropertyChanged(); } }
        public int MeasRang2Index { get => _measRang2Index; set { _measRang2Index = value; OnPropertyChanged(); } }
        public int MeasRang3Index { get => _measRang3Index; set { _measRang3Index = value; OnPropertyChanged(); } }
        public int MeasRang4Index { get => _measRang4Index; set { _measRang4Index = value; OnPropertyChanged(); } }
        public int MeasTrigIndex { get => _measTrigIndex; set { _measTrigIndex = value; OnPropertyChanged(); } }
        public string Meas1plc { get => _meas1plc; set { _meas1plc = value; OnPropertyChanged(); } }
        public string Meas2plc { get => _meas2plc; set { _meas2plc = value; OnPropertyChanged(); } }
        public string Meas3plc { get => _meas3plc; set { _meas3plc = value; OnPropertyChanged(); } }
        public string Meas4plc { get => _meas4plc; set { _meas4plc = value; OnPropertyChanged(); } }
        public bool Meas1DispOn { get => _meas1DispOn; set { _meas1DispOn = value; OnPropertyChanged(); } }
        public bool Meas2DispOn { get => _meas2DispOn; set { _meas2DispOn = value; OnPropertyChanged(); } }
        public bool Meas3DispOn { get => _meas3DispOn; set { _meas3DispOn = value; OnPropertyChanged(); } }
        public bool Meas4DispOn { get => _meas4DispOn; set { _meas4DispOn = value; OnPropertyChanged(); } }
        public int DetectSourceAIndex { get => _detectSourceAIndex; set { _detectSourceAIndex = value; OnPropertyChanged(); } }
        public int DetectSourceBIndex { get => _detectSourceBIndex; set { _detectSourceBIndex = value; OnPropertyChanged(); } }
        public int DetectUnitAIndex { get => _detectUnitAIndex; set { _detectUnitAIndex = value; OnPropertyChanged(); } }
        public int DetectUnitBIndex { get => _detectUnitBIndex; set { _detectUnitBIndex = value; OnPropertyChanged(); } }
        public string DetectReleaseA { get => _detectReleaseA; set { _detectReleaseA = value; OnPropertyChanged(); } }
        public string DetectReleaseB { get => _detectReleaseB; set { _detectReleaseB = value; OnPropertyChanged(); } }
        public string CheckTime { get => _checkTime; set { _checkTime = value; OnPropertyChanged(); } }
        public int CheckTimeUnitIndex { get => _checkTimeUnitIndex; set { _checkTimeUnitIndex = value; OnPropertyChanged(); } }
        //*************************************************
        //定義
        // ItemHederにランダムID付与
        //*************************************************
        private readonly string _id = Guid.NewGuid().ToString();    //被りがないIDを自動生成
        public string Id => _id;                                    //読み取り専用ID
        //*************************************************
        //定義
        // プロパティが変更通知時に呼び出され値を更新(ViewModel用)
        //*************************************************
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //*************************************************
        //動作
        // コンストラクタで初期化
        //*************************************************
        public VITabViewModel()
        {
            MeasureOn = false;
            ItemHeader = "Item1";
        }
    }

    public class VITabWindow : INotifyPropertyChanged
    {
        private static VITabWindow instance;
        private ObservableCollection<VITabViewModel> _tabs;
        private ObservableCollection<(string Id, string ItemHeader)> _checkedVITabNames;
        private string _checkedTabNamesText;
        private bool _isBigTabCtrlEnabled = true;                                               //親TabControlの有効状態を管理するプロパティ
        private bool _isSmallTabCtrlEnabled = true;                                             //子タブ内の要素の有効状態

        public ObservableCollection<VITabViewModel> Tabs { get => _tabs; set { _tabs = value; OnPropertyChanged(); } }

        public ObservableCollection<(string Id, string ItemHeader)> CheckedVITabNames
        {
            get => _checkedVITabNames;
            set
            {
                _checkedVITabNames = value;
                OnPropertyChanged();
                UpdateCheckedTabNamesText();
            }
        }
        //*************************************************
        //Item名変更通知プロパティ
        //*************************************************
        public string CheckedTabNamesText { get => _checkedTabNamesText; set { _checkedTabNamesText = value; OnPropertyChanged(); } }
        //*************************************************
        //親TabControlの有効状態プロパティ
        //*************************************************
        public bool IsBigTabCtrlEnabled { get => _isBigTabCtrlEnabled; set { _isBigTabCtrlEnabled = value; OnPropertyChanged(); } }
        //*************************************************
        //子TabControlの有効状態プロパティ
        //*************************************************
        public bool IsSmallTabCtrlEnabled { get => _isSmallTabCtrlEnabled; set { _isSmallTabCtrlEnabled = value; OnPropertyChanged(); } }

        public static VITabWindow Instance
        {
            get
            {
                if (instance == null)
                    instance = new VITabWindow();
                return instance;
            }
        }

        public VITabWindow()
        {
            Tabs = new ObservableCollection<VITabViewModel>();
            CheckedVITabNames = new ObservableCollection<(string Id, string ItemHeader)>();
            CheckedVITabNames.CollectionChanged += (s, e) => UpdateCheckedTabNamesText();
            //デザイナー用または初期データ
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                for (int i = 1; i <= 3; i++)
                {
                    //デザイナー用のダミーデータ
                    Tabs.Add(new VITabViewModel
                    {
                        ItemHeader = $"LongLongNameItemHedder{i}",
                        Const1 = "Const",
                        Const2 = "Const",
                        Const3 = "Const",
                        Const4 = "Const",
                        StanbyValue = "Stanby",
                        Meas1plc = "NPLC",
                        Meas2plc = "NPLC",
                        Meas3plc = "NPLC",
                        Meas4plc = "NPLC",
                        SourceFunc1Index = 0,
                        SourceFunc2Index = 1,
                        SourceFunc3Index = 2,
                        SourceFunc4Index = 3,
                        SourceMode1Index = 0,
                        SourceMode2Index = 0,
                        SourceMode3Index = 0,
                        SourceMode4Index = 0,
                        SourceRang1Index = 0,
                        SourceRang2Index = 0,
                        SourceRang3Index = 0,
                        SourceRang4Index = 0,
                        Const1UnitIndex = 0,
                        Const2UnitIndex = 0,
                        Const3UnitIndex = 0,
                        Const4UnitIndex = 0,
                        StanbyUnitIndex = 0,
                        MeasMode1Index = 0,
                        MeasMode2Index = 0,
                        MeasMode3Index = 0,
                        MeasMode4Index = 0,
                        MeasRang1Index = 0,
                        MeasRang2Index = 0,
                        MeasRang3Index = 0,
                        MeasRang4Index = 0,
                        MeasTrigIndex = 1,
                        MeasureOn = false,
                        Meas1DispOn = true,
                        Meas2DispOn = true,
                        Meas3DispOn = true,
                        Meas4DispOn = true,
                        SourceLimit1 = "100",
                        SourceLimit2 = "100",
                        SourceLimit3 = "100",
                        SourceLimit4 = "100",
                        SourceLimit1Index = 0,
                        SourceLimit2Index = 0,
                        SourceLimit3Index = 0,
                        SourceLimit4Index = 0
                    });
                }
            }
            else
            {
                //実行時用のデータ
                for (int i = 1; i <= 6; i++)
                {
                    VITabViewModel tab = new VITabViewModel
                    {
                        ItemHeader = $"Item{i}",
                        SourceFunc1Index = 1,
                        SourceFunc2Index = 2,
                        SourceFunc3Index = 3,
                        SourceFunc4Index = 3,
                        SourceMode1Index = 0,
                        SourceMode2Index = 0,
                        SourceMode3Index = 0,
                        SourceMode4Index = 0,
                        SourceRang1Index = 2,
                        SourceRang2Index = 3,
                        SourceRang3Index = 2,
                        SourceRang4Index = 0,
                        Const1UnitIndex = 0,
                        Const2UnitIndex = 0,
                        Const3UnitIndex = 0,
                        Const4UnitIndex = 0,
                        StanbyUnitIndex = 0,
                        MeasMode1Index = 0,
                        MeasMode2Index = 0,
                        MeasMode3Index = 0,
                        MeasMode4Index = 0,
                        MeasRang1Index = 0,
                        MeasRang2Index = 0,
                        MeasRang3Index = 0,
                        MeasRang4Index = 0,
                        MeasTrigIndex = 1,
                        MeasureOn = false,
                        Meas1DispOn = true,
                        Meas2DispOn = true,
                        Meas3DispOn = true,
                        Meas4DispOn = true,
                        Const1 = "0",
                        Const2 = "0",
                        Const3 = "0",
                        Const4 = "0",
                        StanbyValue = "0",
                        Meas1plc = "10",
                        Meas2plc = "10",
                        Meas3plc = "10",
                        Meas4plc = "10",
                        DetectReleaseA = "0.0",
                        DetectReleaseB = "0.0",
                        CheckTime = "1",
                        SourceLimit1Index = 0,
                        SourceLimit2Index = 0,
                        SourceLimit3Index = 0,
                        SourceLimit4Index = 0,
                        DetectSourceAIndex = 1,
                        DetectSourceBIndex = 5,
                        DetectUnitAIndex = 0,
                        DetectUnitBIndex = 0,
                        CheckTimeUnitIndex = 0
                    };
                    //MeasureOnの変更を監視
                    tab.PropertyChanged += Tab_PropertyChanged;
                    Tabs.Add(tab);
                }
                UpdateCheckedTabNamesText();            //初期状態のTextBlock用文字列を設定
            }
            //問題なければ削除でも可
            //初期化後にTabが空でないことの保障（タブが空だったらプロパティ手動更新によってタブを追加
            //if (!Tabs.Any())
            //{
            //    Tabs.Add(new VITabViewModel { ItemHeader = "Item1" });
            //}
        }

        private void Tab_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is VITabViewModel tab && !string.IsNullOrEmpty(tab.ItemHeader))
            {
                //MeasureOnプロパティに変更があった場合
                if (e.PropertyName == nameof(VITabViewModel.MeasureOn))
                {
                    //MeasureOnチェックされた場合
                    if (tab.MeasureOn)
                    {
                        if (!CheckedVITabNames.Any(x => x.Id == tab.Id))
                            CheckedVITabNames.Add((tab.Id, tab.ItemHeader));
                    }
                    //MeasureOnチェックが外れた場合
                    else
                    {
                        var item = CheckedVITabNames.FirstOrDefault(x => x.Id == tab.Id);
                        if (item != default)
                            CheckedVITabNames.Remove(item);
                    }
                }
                //ItemHeaderプロパティに変更があった場合
                else if (e.PropertyName == nameof(VITabViewModel.ItemHeader))
                {
                    var index = CheckedVITabNames.IndexOf(CheckedVITabNames.FirstOrDefault(x => x.Id == tab.Id));
                    if (index >= 0)
                        CheckedVITabNames[index] = (tab.Id, tab.ItemHeader);
                }
            }
        }
        //*************************************************
        //動作
        // MeasureOnがチェックされるとTextBlockに項目名追加
        // 何もチェックされていないと"対象なし"
        //*************************************************
        private void UpdateCheckedTabNamesText()
        {
            CheckedTabNamesText = CheckedVITabNames.Any()
                ? string.Join(", ", CheckedVITabNames.Select(x => x.ItemHeader))
                : "対象なし";
        }
        //*************************************************
        //アクセス：public
        //戻り値：
        //　　　　Tabname
        //　　　　MeasureOn
        //　　　　Source1set
        //　　　　  act/mode/range
        //　　　　Source2set
        //　　　　  act/mode/range
        //　　　　Source3set
        //　　　　  act/mode/range
        //　　　　Source4set
        //　　　　  act/mode/range
        //　　　　VI
        //　　　　  stanby(startwait)
        //　　　　Constset
        //　　　　  const1/const2/const3/const4
        //　　　　DMM1set
        //          mode/range/nplc
        //　　　　DMM2set
        //          mode/range/nplc
        //　　　　DMM3set
        //          mode/range/nplc
        //　　　　DMM4set
        //          mode/range/nplc
        //　　　　DMMTrig
        //機能：VITabから設定値を取得
        //　　　MeasureOn checkboxにcheckが入っているタブが対象
        //*************************************************
        public IEnumerable<(
            string TabName,
            bool MeasureOn,
            string[] Source1set, string[] Source2set, string[] Source3set, string[] Source4set,
            string[] VIset,
            string[] Constset,
            string[] Detrelset,
            bool[] DMMDisp,
            string[] DMM1set, string[] DMM2set, string[] DMM3set, string[] DMM4set,
            string DMMTrig)>
            GetMeasureOnTabData()
        {
            //以下のコードをcomboItem.Tagから動的に取得することも可能だが、記述が長くなる
            string[] sourceActItems = new[] { "sweep", "constant1", "constant2", "constant3", "NotUsed" };
            string[] funcItems = new[] { "sweep", "const", "const", "const", "pulse" };
            string[] modeItems = new[] { "VOLT", "CURR" };
            string[] sourceRangeValue = new[] { "AUTO", "30V", "10V", "1V", "100mV", "10mV", "200mA", "100mA", "10mA", "1mA" };
            string[] sourceRangUnitsItems = new[] { "range0", "range1", "range1", "range1", "range2", "range2", "range2", "range2", "range2", "range2" };
            string[] trigSourceItems = new[] { "1", "2", "3", "4", "EXT" };
            string[] trigDirectionalItems = new[] { "RISE", "FALL" };
            string[] unitsItems = new[] { "range1", "range2", "range1", "range2" };
            string[] unitsItemsString = new[] { "V", "mV", "A", "mA" };
            string[] tUnitsItems = new[] { "range1", "range2", "range3" };
            string[] detectreleaseItems = new[] { "ActNormal", "ActSpecial1" };
            string[] directionalItems = new[] { "rise", "fall", "risefall", "fallrise" };
            string[] relsourceItems = new[] { "SOURCE1", "SOURCE2", "SOURCE3", "SOURCE4", "PG", "SOURCEnull" };
            string[] measureRangeValue = new[] { "AUTO", "1000V", "100V", "10V", "1V", "100mV", "3A", "1A", "100mA", "10mA", "1mA", "100uA" };
            string[] measureRangUnitsItems = new[] { "range0", "range1", "range1", "range1", "range1", "range2", "range1", "range1", "range2", "range2", "range2", "range3" };
            string[] measureTrigSourceItems = new[] { "IMM", "BUS", "EXT" };

            string[] sourceLimit = new[] { "mA", "V" };
            string[] sourceLimitItems = new[] { "range2", "range1" };

            return Tabs.Where(tab => tab.MeasureOn)                    //MeasureOnがtrueのタブのみ設定値取得
                .Select(tab => (
                tab.ItemHeader,
                tab.MeasureOn,
                new[] {
                    sourceActItems[tab.SourceFunc1Index], funcItems[tab.SourceFunc1Index], modeItems[tab.SourceMode1Index],
                    sourceRangeValue[tab.SourceRang1Index], sourceRangUnitsItems[tab.SourceRang1Index],
                    tab.SourceLimit1,sourceLimitItems[tab.SourceLimit1Index],sourceLimit[tab.SourceLimit1Index]
                },
                new[] {
                    sourceActItems[tab.SourceFunc2Index], funcItems[tab.SourceFunc2Index], modeItems[tab.SourceMode2Index],
                    sourceRangeValue[tab.SourceRang2Index], sourceRangUnitsItems[tab.SourceRang2Index],
                    tab.SourceLimit2,sourceLimitItems[tab.SourceLimit2Index],sourceLimit[tab.SourceLimit2Index]
                },
                new[] {
                    sourceActItems[tab.SourceFunc3Index], funcItems[tab.SourceFunc3Index], modeItems[tab.SourceMode3Index],
                    sourceRangeValue[tab.SourceRang3Index], sourceRangUnitsItems[tab.SourceRang3Index],
                    tab.SourceLimit3,sourceLimitItems[tab.SourceLimit3Index],sourceLimit[tab.SourceLimit3Index]
                },
                new[] {
                    sourceActItems[tab.SourceFunc4Index], funcItems[tab.SourceFunc4Index], modeItems[tab.SourceMode4Index],
                    sourceRangeValue[tab.SourceRang4Index], sourceRangUnitsItems[tab.SourceRang4Index],
                    tab.SourceLimit4,sourceLimitItems[tab.SourceLimit4Index],sourceLimit[tab.SourceLimit4Index]
                },
                new[] {
                    tab.StanbyValue, tUnitsItems[tab.StanbyUnitIndex]
                },
                new[] {
                    tab.Const1, unitsItems[tab.Const1UnitIndex], tab.Const2, unitsItems[tab.Const2UnitIndex], tab.Const3, unitsItems[tab.Const3UnitIndex],
                    unitsItemsString[tab.Const1UnitIndex], unitsItemsString[tab.Const2UnitIndex], unitsItemsString[tab.Const3UnitIndex],
                    tab.Const4, unitsItems[tab.Const4UnitIndex], unitsItemsString[tab.Const4UnitIndex]
                },
                new[] {
                    relsourceItems[tab.DetectSourceAIndex], tab.DetectReleaseA, unitsItems[tab.DetectUnitAIndex],
                    relsourceItems[tab.DetectSourceBIndex], tab.DetectReleaseB, unitsItems[tab.DetectUnitBIndex],
                    tab.CheckTime, tUnitsItems[tab.CheckTimeUnitIndex]
                },
                new[] {
                    tab.Meas1DispOn, tab.Meas2DispOn,tab.Meas3DispOn,tab.Meas4DispOn,
                },
                new[] {
                    modeItems[tab.MeasMode1Index],
                    measureRangeValue[tab.MeasRang1Index], measureRangUnitsItems[tab.MeasRang1Index],
                    tab.Meas1plc
                },
                new[] {
                    modeItems[tab.MeasMode2Index],
                    measureRangeValue[tab.MeasRang2Index], measureRangUnitsItems[tab.MeasRang2Index],
                    tab.Meas2plc
                },
                new[] {
                    modeItems[tab.MeasMode3Index],
                    measureRangeValue[tab.MeasRang3Index], measureRangUnitsItems[tab.MeasRang3Index],
                    tab.Meas3plc
                },
                new[] {
                    modeItems[tab.MeasMode4Index],
                    measureRangeValue[tab.MeasRang4Index], measureRangUnitsItems[tab.MeasRang4Index],
                    tab.Meas4plc
                },
                measureTrigSourceItems[tab.MeasTrigIndex]
                )).ToList();
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}