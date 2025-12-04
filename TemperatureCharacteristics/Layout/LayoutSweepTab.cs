using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using TemperatureCharacteristics.Models;

namespace SweepTab
{
    public class SweepTabViewModel : INotifyPropertyChanged
    {
        //タブヘッダー
        private string _itemHeader;             //Itemタブ入力欄
        //CheckBox
        private bool _measureOn;                //測定ON/OFF CheckBox
        private bool _normalSweepCheck;         //Sweep動作のみ CheckBox（normal sweep）
        private bool _pulseGenUseCheck;         //PulseGenerator使用 CheckBox（normal sweep以外）
        private bool _meas1DispOn;              //DMM1のDisplay ON/OFF CheckBox
        private bool _meas2DispOn;              //DMM2のDisplay ON/OFF CheckBox
        private bool _meas3DispOn;              //DMM3のDisplay ON/OFF CheckBox
        private bool _meas4DispOn;              //DMM4のDisplay ON/OFF CheckBox
        //TextBox
        private string _pos1;                   //OSC Ch1 position
        private string _pos2;                   //OSC Ch2 position
        private string _pos3;                   //OSC Ch3 position
        private string _pos4;                   //OSC Ch4 position
        private string _oscTriglevel;           //OSC Trig level
        private string _minValue;               //Sweep電源 min value
        private string _maxValue;               //Sweep電源 max value
        private string _const1;                 //constant電源1 value
        private string _const2;                 //constant電源2 value
        private string _const3;                 //constant電源3 value
        private string _stepTime;               //Sweep電源 wait value（ex.vdet1測定時tvdet1を指定
        private string _stepValue;              //Sweep電源 step value（normal sweep時のstep値を指定
        private string _stanbyValue;            //Sweep電源 standby value（normal sweep時の測定スタンバイ時を指定
        private string _detectReleaseA;         //Sweep検出復帰の際にSweep電源をStart値に戻す＋別電源Aを追加で使用する場合の値
        private string _detectReleaseB;         //Sweep検出復帰の際にSweep電源をStart値に戻す＋別電源Bを追加で使用する場合の値
        private string _checkTime;              //Sweep検出復帰の際のwait value（ex.vdet1測定時tvrel1を指定
        private string _meas1plc;               //DMM1のNPLC value
        private string _meas2plc;               //DMM2のNPLC value
        private string _meas3plc;               //DMM3のNPLC value
        private string _meas4plc;               //DMM4のNPLC value
        private string _sourceLimit1;           //Source1のLimit value
        private string _sourceLimit2;           //Source2のLimit value
        private string _sourceLimit3;           //Source3のLimit value
        private string _sourceLimit4;           //Source4のLimit value
        private string _lowValue;               //PG Low value
        private string _highValue;              //PG High value
        private string _periodValue;            //PG Period value
        private string _widthValue;             //PG Width value
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
        private int _oscRang1Index;             //OSC Ch1 range
        private int _oscRang2Index;             //OSC Ch2 range
        private int _oscRang3Index;             //OSC Ch3 range
        private int _oscRang4Index;             //OSC Ch4 range
        private int _trigSourceIndex;           //OSC Trig ソース
        private int _trigDirectionalIndex;      //OSC Trig Rise/Fall
        private int _levelUnitIndex;            //OSC Trig Unit
        private int _minVUnitIndex;             //Sweep電源 minValue Unit
        private int _maxVUnitIndex;             //Sweep電源 maxValue Unit
        private int _directionalIndex;          //Sweep電源 Sweep方向
        private int _const1UnitIndex;           //Constant電源1 Unit
        private int _const2UnitIndex;           //Constant電源2 Unit
        private int _const3UnitIndex;           //Constant電源3 Unit
        private int _stepTimeUnitIndex;         //Sweep電源 wait Unit
        private int _stepUnitIndex;             //Sweep電源 step Unit（normal sweepのみ使用
        private int _stanbyUnitIndex;           //Sweep電源 standby Unit（normal sweepのみ使用
        private int _detectReleaseIndex;        //Sweep検出復帰の際の動作（normal→Sweep電源をStart値に戻す normal+α→別電源を変動させる（ラッチタイプでVMを0.3V程度に引き上げる想定
        private int _detectSourceAIndex;        //Sweep検出復帰の際にSweep電源をStart値に戻す＋別電源を追加で使用する場合の電源を指定A（VMを想定
        private int _detectSourceBIndex;        //Sweep検出復帰の際にSweep電源をStart値に戻す＋別電源を追加で使用する場合の電源を指定B（CSを想定
        private int _detectUnitAIndex;          //上記検出復帰時に使用する電源AのUnit
        private int _detectUnitBIndex;          //上記検出復帰時に使用する電源BのUnit
        private int _checkTimeUnitIndex;        //Sweep検出復帰の際のwait Unit
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
        private int _lowUnitIndex;              //PG LowValue Unit
        private int _highUnitIndex;             //PG HighValue Unit
        private int _polarityIndex;             //PG 出力極性
        private int _outputChIndex;             //PG 出力CH
        private int _periodUnitIndex;           //PG PeriodValue Unit
        private int _widthUnitIndex;            //PG WidthValue Unit
        private int _outputLoadIndex;           //PG 出力抵抗

        //外部からのアクセス用
        //OnPropertyChanged()でユーザーが変更すると即UI更新で反映
        //Properties
        public string ItemHeader { get => _itemHeader; set { _itemHeader = value; OnPropertyChanged(); } }
        public bool MeasureOn { get => _measureOn; set { _measureOn = value; OnPropertyChanged(); } }
        public bool NormalSweepCheck { get => _normalSweepCheck; set { _normalSweepCheck = value; OnPropertyChanged(); } }
        public bool PulseGenUseCheck { get => _pulseGenUseCheck; set { _pulseGenUseCheck = value; OnPropertyChanged(); } }
        public string MinValue { get => _minValue; set { _minValue = value; OnPropertyChanged(); } }
        public string MaxValue { get => _maxValue; set { _maxValue = value; OnPropertyChanged(); } }
        public int MinVUnitIndex { get => _minVUnitIndex; set { _minVUnitIndex = value; OnPropertyChanged(); } }
        public int MaxVUnitIndex { get => _maxVUnitIndex; set { _maxVUnitIndex = value; OnPropertyChanged(); } }
        public int DirectionalIndex { get => _directionalIndex; set { _directionalIndex = value; OnPropertyChanged(); } }
        public string Const1 { get => _const1; set { _const1 = value; OnPropertyChanged(); } }
        public string Const2 { get => _const2; set { _const2 = value; OnPropertyChanged(); } }
        public string Const3 { get => _const3; set { _const3 = value; OnPropertyChanged(); } }
        public int Const1UnitIndex { get => _const1UnitIndex; set { _const1UnitIndex = value; OnPropertyChanged(); } }
        public int Const2UnitIndex { get => _const2UnitIndex; set { _const2UnitIndex = value; OnPropertyChanged(); } }
        public int Const3UnitIndex { get => _const3UnitIndex; set { _const3UnitIndex = value; OnPropertyChanged(); } }
        public int StepTimeUnitIndex { get => _stepTimeUnitIndex; set { _stepTimeUnitIndex = value; OnPropertyChanged(); } }
        public int StepUnitIndex { get => _stepUnitIndex; set { _stepUnitIndex = value; OnPropertyChanged(); } }
        public int StanbyUnitIndex { get => _stanbyUnitIndex; set { _stanbyUnitIndex = value; OnPropertyChanged(); } }
        public string StepTime { get => _stepTime; set { _stepTime = value; OnPropertyChanged(); } }
        public string StepValue { get => _stepValue; set { _stepValue = value; OnPropertyChanged(); } }
        public string StanbyValue { get => _stanbyValue; set { _stanbyValue = value; OnPropertyChanged(); } }
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
        public int OSCRang1Index { get => _oscRang1Index; set { _oscRang1Index = value; OnPropertyChanged(); } }
        public int OSCRang2Index { get => _oscRang2Index; set { _oscRang2Index = value; OnPropertyChanged(); } }
        public int OSCRang3Index { get => _oscRang3Index; set { _oscRang3Index = value; OnPropertyChanged(); } }
        public int OSCRang4Index { get => _oscRang4Index; set { _oscRang4Index = value; OnPropertyChanged(); } }
        public string OSCPos1 { get => _pos1; set { _pos1 = value; OnPropertyChanged(); } }
        public string OSCPos2 { get => _pos2; set { _pos2 = value; OnPropertyChanged(); } }
        public string OSCPos3 { get => _pos3; set { _pos3 = value; OnPropertyChanged(); } }
        public string OSCPos4 { get => _pos4; set { _pos4 = value; OnPropertyChanged(); } }
        public int TrigSourceIndex { get => _trigSourceIndex; set { _trigSourceIndex = value; OnPropertyChanged(); } }
        public int TrigDirectionalIndex { get => _trigDirectionalIndex; set { _trigDirectionalIndex = value; OnPropertyChanged(); } }
        public string OSCTrigLevel { get => _oscTriglevel; set { _oscTriglevel = value; OnPropertyChanged(); } }
        public int LevelUnitIndex { get => _levelUnitIndex; set { _levelUnitIndex = value; OnPropertyChanged(); } }
        public int DetectReleaseIndex { get => _detectReleaseIndex; set { _detectReleaseIndex = value; OnPropertyChanged(); } }
        public int DetectSourceAIndex { get => _detectSourceAIndex; set { _detectSourceAIndex = value; OnPropertyChanged(); } }
        public int DetectSourceBIndex { get => _detectSourceBIndex; set { _detectSourceBIndex = value; OnPropertyChanged(); } }
        public int DetectUnitAIndex { get => _detectUnitAIndex; set { _detectUnitAIndex = value; OnPropertyChanged(); } }
        public int DetectUnitBIndex { get => _detectUnitBIndex; set { _detectUnitBIndex = value; OnPropertyChanged(); } }
        public string DetectReleaseA { get => _detectReleaseA; set { _detectReleaseA = value; OnPropertyChanged(); } }
        public string DetectReleaseB { get => _detectReleaseB; set { _detectReleaseB = value; OnPropertyChanged(); } }
        public string CheckTime { get => _checkTime; set { _checkTime = value; OnPropertyChanged(); } }
        public int CheckTimeUnitIndex { get => _checkTimeUnitIndex; set { _checkTimeUnitIndex = value; OnPropertyChanged(); } }
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
        public string SourceLimit1 { get => _sourceLimit1; set { _sourceLimit1 = value; OnPropertyChanged(); } }
        public string SourceLimit2 { get => _sourceLimit2; set { _sourceLimit2 = value; OnPropertyChanged(); } }
        public string SourceLimit3 { get => _sourceLimit3; set { _sourceLimit3 = value; OnPropertyChanged(); } }
        public string SourceLimit4 { get => _sourceLimit4; set { _sourceLimit4 = value; OnPropertyChanged(); } }
        public int SourceLimit1Index { get => _sourceLimit1Index; set { _sourceLimit1Index = value; OnPropertyChanged(); } }
        public int SourceLimit2Index { get => _sourceLimit2Index; set { _sourceLimit2Index = value; OnPropertyChanged(); } }
        public int SourceLimit3Index { get => _sourceLimit3Index; set { _sourceLimit3Index = value; OnPropertyChanged(); } }
        public int SourceLimit4Index { get => _sourceLimit4Index; set { _sourceLimit4Index = value; OnPropertyChanged(); } }
        public string LowValue { get => _lowValue; set { _lowValue = value; OnPropertyChanged(); } }
        public string HighValue { get => _highValue; set { _highValue = value; OnPropertyChanged(); } }
        public int LowVUnitIndex { get => _lowUnitIndex; set { _lowUnitIndex = value; OnPropertyChanged(); } }
        public int HighVUnitIndex { get => _highUnitIndex; set { _highUnitIndex = value; OnPropertyChanged(); } }
        public int PolarityIndex { get => _polarityIndex; set { _polarityIndex = value; OnPropertyChanged(); } }
        public int OutputChIndex { get => _outputChIndex; set { _outputChIndex = value; OnPropertyChanged(); } }
        public int OutputLoadIndex { get => _outputLoadIndex; set { _outputLoadIndex = value; OnPropertyChanged(); } }
        public string PeriodValue { get => _periodValue; set { _periodValue = value; OnPropertyChanged(); } }
        public string WidthValue { get => _widthValue; set { _widthValue = value; OnPropertyChanged(); } }
        public int PeriodUnitIndex { get => _periodUnitIndex; set { _periodUnitIndex = value; OnPropertyChanged(); } }
        public int WidthUnitIndex { get => _widthUnitIndex; set { _widthUnitIndex = value; OnPropertyChanged(); } }
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
        public SweepTabViewModel()
        {
            MeasureOn = false;
            Meas1DispOn = true;
        }
    }
    public class SweepTabWindow : INotifyPropertyChanged
    {
        private static SweepTabWindow? instance;                                                //唯一のインスタンスを保持 null許容型
        private ObservableCollection<SweepTabViewModel> _tabs;                                       //Tab表示用
        private ObservableCollection<(string Id, string ItemHeader)> _checkedSweepTabNames;     //IDとTabNameチェック用
        private string _checkedTabNamesText;                                                    //TextBlock用
        private bool _isBigTabCtrlEnabled = true;                                               //親TabControlの有効状態を管理するプロパティ
        private bool _isSmallTabCtrlEnabled = true;                                             //子タブ内の要素の有効状態

        public ObservableCollection<SweepTabViewModel> Tabs { get => _tabs; set { _tabs = value; OnPropertyChanged(); } }
        public ObservableCollection<(string Id, string ItemHeader)> CheckedSweepTabNames
        {
            get => _checkedSweepTabNames;
            set
            {
                _checkedSweepTabNames = value;
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

        public static SweepTabWindow Instance     //外部からのアクセス用
        {
            get
            {
                if (instance == null)
                    instance = new SweepTabWindow();                            // クラス内でインスタンスを生成
                return instance;
            }
        }
        public SweepTabWindow()
        {
            Tabs = new ObservableCollection<SweepTabViewModel>();
            CheckedSweepTabNames = new ObservableCollection<(string Id, string ItemHeader)>();
            CheckedSweepTabNames.CollectionChanged += CheckedSweepTabNames_CollectionChanged;

            //デザイナー用または初期データ
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                for (int i = 1; i <= 3; i++)
                {
                    //デザイナー用のダミーデータ
                    Tabs.Add(new SweepTabViewModel
                    {
                        ItemHeader = $"LongLongNameItemHedder{i}",
                        OSCPos1 = "Pos",
                        OSCPos2 = "Pos",
                        OSCPos3 = "Pos",
                        OSCPos4 = "Pos",
                        OSCTrigLevel = "Level",
                        MinValue = "Min",
                        MaxValue = "Max",
                        Const1 = "Const",
                        Const2 = "Const",
                        Const3 = "Const",
                        StepTime = "Step",
                        StepValue = "Step",
                        StanbyValue = "Stanby",
                        DetectReleaseA = "det/rel",
                        DetectReleaseB = "det/rel",
                        CheckTime = "Time",
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
                        OSCRang1Index = 2,
                        OSCRang2Index = 2,
                        OSCRang3Index = 2,
                        OSCRang4Index = 2,
                        TrigSourceIndex = 3,
                        TrigDirectionalIndex = 1,
                        LevelUnitIndex = 0,
                        MinVUnitIndex = 0,
                        MaxVUnitIndex = 0,
                        DirectionalIndex = 0,
                        Const1UnitIndex = 0,
                        Const2UnitIndex = 0,
                        Const3UnitIndex = 0,
                        StepTimeUnitIndex = 0,
                        StepUnitIndex = 0,
                        StanbyUnitIndex = 0,
                        DetectReleaseIndex = 0,
                        DetectSourceAIndex = 1,
                        DetectSourceBIndex = 5,
                        DetectUnitAIndex = 0,
                        DetectUnitBIndex = 0,
                        CheckTimeUnitIndex = 0,
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
                        NormalSweepCheck = false,
                        PulseGenUseCheck = false,
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
                        SourceLimit4Index = 0,
                        LowValue = "Low",
                        HighValue = "High",
                        PeriodValue = "Period",
                        WidthValue = "Width",
                        LowVUnitIndex = 0,
                        HighVUnitIndex = 0,
                        PolarityIndex = 0,
                        OutputChIndex = 0,
                        OutputLoadIndex = 1,
                        PeriodUnitIndex = 0,
                        WidthUnitIndex = 1
                    });
                }
            }
            else
            {
                //実行時用のデータ
                for (int i = 1; i <= 15; i++)
                {
                    SweepTabViewModel tab = new SweepTabViewModel
                    {
                        ItemHeader = $"Item{i}",
                        SourceFunc1Index = 0,
                        SourceFunc2Index = 1,
                        SourceFunc3Index = 2,
                        SourceFunc4Index = 3,
                        SourceMode1Index = 0,
                        SourceMode2Index = 0,
                        SourceMode3Index = 0,
                        SourceMode4Index = 0,
                        SourceRang1Index = 2,
                        SourceRang2Index = 3,
                        SourceRang3Index = 3,
                        SourceRang4Index = 0,
                        OSCRang1Index = 2,
                        OSCRang2Index = 2,
                        OSCRang3Index = 2,
                        OSCRang4Index = 2,
                        TrigSourceIndex = 3,
                        TrigDirectionalIndex = 1,
                        LevelUnitIndex = 0,
                        MinVUnitIndex = 0,
                        MaxVUnitIndex = 0,
                        DirectionalIndex = 0,
                        Const1UnitIndex = 0,
                        Const2UnitIndex = 0,
                        Const3UnitIndex = 0,
                        StepTimeUnitIndex = 0,
                        StepUnitIndex = 0,
                        StanbyUnitIndex = 0,
                        DetectReleaseIndex = 0,
                        DetectSourceAIndex = 1,
                        DetectSourceBIndex = 5,
                        DetectUnitAIndex = 0,
                        DetectUnitBIndex = 0,
                        CheckTimeUnitIndex = 0,
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
                        NormalSweepCheck = false,
                        PulseGenUseCheck = false,
                        Meas1DispOn = true,
                        Meas2DispOn = true,
                        Meas3DispOn = true,
                        Meas4DispOn = true,
                        OSCPos1 = "1",
                        OSCPos2 = "1",
                        OSCPos3 = "-3",
                        OSCPos4 = "-3",
                        OSCTrigLevel = "0",
                        MinValue = "0.0",
                        MaxValue = "1.0",
                        Const1 = "0",
                        Const2 = "0",
                        Const3 = "0",
                        StepTime = "1",
                        StepValue = "0.1",
                        StanbyValue = "0",
                        DetectReleaseA = "0.3",
                        DetectReleaseB = "0",
                        CheckTime = "1",
                        Meas1plc = "10",
                        Meas2plc = "10",
                        Meas3plc = "10",
                        Meas4plc = "10",
                        SourceLimit1Index = 0,
                        SourceLimit2Index = 0,
                        SourceLimit3Index = 0,
                        SourceLimit4Index = 0,
                        LowValue = "0",
                        HighValue = "50",
                        PeriodValue = "200",
                        WidthValue = "100",
                        LowVUnitIndex = 0,
                        HighVUnitIndex = 1,
                        PolarityIndex = 0,
                        OutputChIndex = 0,
                        OutputLoadIndex = 1,
                        PeriodUnitIndex = 1,
                        WidthUnitIndex = 1
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
            //    Tabs.Add(new SweepTabViewModel { ItemHeader = "Item1" });
            //}
        }

        private void Tab_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is SweepTabViewModel tab && !string.IsNullOrEmpty(tab.ItemHeader))
            {
                //MeasureOnプロパティに変更があった場合
                if (e.PropertyName == nameof(SweepTabViewModel.MeasureOn))
                {
                    //MeasureOnチェックされた場合
                    if (tab.MeasureOn)
                    {
                        if (!CheckedSweepTabNames.Any(x => x.Id == tab.Id))
                            CheckedSweepTabNames.Add((tab.Id, tab.ItemHeader));
                    }
                    //MeasureOnチェックが外れた場合
                    else
                    {
                        var item = CheckedSweepTabNames.FirstOrDefault(x => x.Id == tab.Id);
                        if (item != default)
                            CheckedSweepTabNames.Remove(item);
                    }
                }
                //ItemHeaderプロパティに変更があった場合
                else if (e.PropertyName == nameof(SweepTabViewModel.ItemHeader))
                {
                    var index = CheckedSweepTabNames.IndexOf(CheckedSweepTabNames.FirstOrDefault(x => x.Id == tab.Id));
                    if (index >= 0)
                        CheckedSweepTabNames[index] = (tab.Id, tab.ItemHeader);
                }
            }
        }
        private void CheckedSweepTabNames_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateCheckedTabNamesText();
        }

        //*************************************************
        //動作
        // MeasureOnがチェックされるとTextBlockに項目名追加
        // 何もチェックされていないと"対象なし"
        //*************************************************
        private void UpdateCheckedTabNamesText()
        {
            CheckedTabNamesText = CheckedSweepTabNames.Any()
                ? string.Join(", ", CheckedSweepTabNames.Select(x => x.ItemHeader))         //measureOnチェックがある場合","追加して連結
                : "対象なし";                                                               //measureOnチェックが1つもない場合
        }

        //*************************************************
        //アクセス：public
        //戻り値：
        //　　　　Tabname
        //　　　　MeasureOn
        //　　　　NormalSweepCheck
        //　　　　PGCheck
        //　　　　Source1set
        //　　　　  act/mode/range
        //　　　　Source2set
        //　　　　  act/mode/range
        //　　　　Source3set
        //　　　　  act/mode/range
        //　　　　Source4set
        //　　　　  act/mode/range
        //　　　　OSCset
        //　　　　  ch1/ch2/ch3/ch4/trig
        //　　　　Sweepset
        //　　　　  min/max/directional/steptime(det/rel waittime)/stepvalue/stanby(startwait)
        //　　　　Constset
        //　　　　  const1/const2/const3
        //　　　　Detect/Release
        //          normal/special
        //　　　　Detect/Release
        //　　　　  detrelact/source1/source2/waittime
        //　　　　DMM1set
        //          mode/range/nplc
        //　　　　DMM2set
        //          mode/range/nplc
        //　　　　DMM3set
        //          mode/range/nplc
        //　　　　DMM4set
        //          mode/range/nplc
        //　　　　DMMTrig
        //　　　　PGset
        //          outputCH/low/high/polarity/period/width/outputZ/trigout
        //機能：SweepTabから設定値を取得
        //　　　MeasureOn checkboxにcheckが入っているタブが対象
        //コメント
        // XAMLの<Window.Resources>で準備したComboItemの要素を手入力しているが
        // 修正の余地あり
        //*************************************************
        public IEnumerable<(
            string TabName,
            bool MeasureOn, 
            bool NormalSweepCheck,
            bool PGCheck,
            string[] Source1set, string[] Source2set, string[] Source3set, string[] Source4set,
            string[] OSCset,
            string[] Sweepset,
            string[] Constset,
            string Detrelact,
            string[] Detrelset,
            bool[] DMMDisp,
            string[] DMM1set, string[] DMM2set, string[] DMM3set, string[] DMM4set,
            string DMMTrig,
            string[] PGset)> 
            GetMeasureOnTabData()
        {
            //以下のコードをcomboItem.Tagから動的に取得することも可能だが、記述が長くなる
            string[] sourceActItems = new[] { "sweep", "constant1", "constant2", "constant3", "NotUsed" };
            string[] funcItems = new[] { "sweep", "const", "const", "const", "pulse" };
            string[] modeItems = new[] { "VOLT", "CURR" };
            string[] sourceRangeValue = new[] { "AUTO", "30V", "10V", "1V", "100mV", "10mV", "200mA", "100mA", "10mA", "1mA" };
            string[] sourceRangUnitsItems = new[] { "range0", "range1", "range1", "range1", "range2", "range2", "range2", "range2", "range2", "range2" };
            string[] oscRangValue = new[] { "10V", "5V", "2V", "1V", "500mV", "200mV", "100mV", "50mV", "20mV" };
            string[] oscRangUnitsItems = new[] { "range1", "range1", "range1", "range1", "range2", "range2", "range2", "range2", "range2" };
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
            string[] outputChItems = new[] { "1", "2" };
            string[] polarityItems = new[] { "NORM", "INV" };
            string[] outputZItems = new[] { "50", "INF" };

            return Tabs
                .Where(tab => tab.MeasureOn)                    //MeasureOnがtrueのタブのみ設定値取得
                .Select(tab => (
                tab.ItemHeader,
                tab.MeasureOn,
                tab.NormalSweepCheck,
                tab.PulseGenUseCheck,
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
                    oscRangValue[tab.OSCRang1Index], oscRangUnitsItems[tab.OSCRang1Index], tab.OSCPos1,
                    oscRangValue[tab.OSCRang2Index], oscRangUnitsItems[tab.OSCRang2Index], tab.OSCPos2,
                    oscRangValue[tab.OSCRang3Index], oscRangUnitsItems[tab.OSCRang3Index], tab.OSCPos3,
                    oscRangValue[tab.OSCRang4Index], oscRangUnitsItems[tab.OSCRang4Index], tab.OSCPos4,
                    trigSourceItems[tab.TrigSourceIndex], trigDirectionalItems[tab.TrigDirectionalIndex], tab.OSCTrigLevel, unitsItems[tab.LevelUnitIndex]
                },
                new[] {
                    tab.MinValue, unitsItems[tab.MinVUnitIndex], tab.MaxValue, unitsItems[tab.MaxVUnitIndex],
                    directionalItems[tab.DirectionalIndex],
                    tab.StepTime, tUnitsItems[tab.StepTimeUnitIndex], tab.StepValue, unitsItems[tab.StepUnitIndex],
                    tab.StanbyValue, tUnitsItems[tab.StanbyUnitIndex],
                    unitsItemsString[tab.MinVUnitIndex], unitsItemsString[tab.MaxVUnitIndex], unitsItemsString[tab.StepUnitIndex]
                },
                new[] {
                    tab.Const1, unitsItems[tab.Const1UnitIndex], tab.Const2, unitsItems[tab.Const2UnitIndex], tab.Const3, unitsItems[tab.Const3UnitIndex],
                    unitsItemsString[tab.Const1UnitIndex], unitsItemsString[tab.Const2UnitIndex], unitsItemsString[tab.Const3UnitIndex]
                },
                detectreleaseItems[tab.DetectReleaseIndex],
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
                measureTrigSourceItems[tab.MeasTrigIndex],
                new[] {
                    outputChItems[tab.OutputChIndex],
                    tab.LowValue, unitsItems[tab.LowVUnitIndex], tab.HighValue, unitsItems[tab.HighVUnitIndex],
                    polarityItems[tab.PolarityIndex],
                    tab.PeriodValue, tUnitsItems[tab.PeriodUnitIndex], tab.WidthValue, tUnitsItems[tab.WidthUnitIndex],
                    outputZItems[tab.OutputLoadIndex]
                }
                )).ToList();
        }
        //*************************************************
        //定義
        // プロパティが変更通知時に呼び出され値を更新(Window用)
        //*************************************************
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}