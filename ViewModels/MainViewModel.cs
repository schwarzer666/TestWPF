using InputCheck;                   //ErrCheck.cs
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Text.Json;
using System.Windows.Input;
using TemperatureCharacteristics.Models;
using TemperatureCharacteristics.Services;
using TemperatureCharacteristics.Services.Actions;
using TemperatureCharacteristics.Services.Data;
using TemperatureCharacteristics.Services.Devices;
using TemperatureCharacteristics.Services.Dialog;
using TemperatureCharacteristics.Services.Measurement;
using TemperatureCharacteristics.Services.Results;
using TemperatureCharacteristics.Services.UserConfig;
using TemperatureCharacteristics.ViewModels.Debug;
using TemperatureCharacteristics.ViewModels.Devices;
using TemperatureCharacteristics.ViewModels.Presets;
using TemperatureCharacteristics.ViewModels.Tabs.Delay;
using TemperatureCharacteristics.ViewModels.Tabs.Sweep;
using TemperatureCharacteristics.ViewModels.Tabs.VI;
using UTility;                      //Utility.cs


namespace TemperatureCharacteristics.ViewModels
{
    public class MainViewModel : BaseViewModel, IMeasurementContext
    {
        private readonly Sweep _sweepAct;
        private readonly Delay _delayAct;
        private readonly VI _viAct;
        private readonly Thermo _thermoAct;
        private readonly InpCheck _errCheck;
        private readonly UT _utility;
        private bool _isRunning;                    //動作フラグ
        private bool _isScanningDevices;
        private string _measurementStatus;
        private string _allCheckedTabNamesText;     //全Tabのチェック状態を集約
        //*************************************************
        //Viewからバインドされるタブ用
        //*************************************************
        public SweepTabGroupViewModel SweepTabs { get; }
        public DelayTabGroupViewModel DelayTabs { get; }
        public VITabGroupViewModel VITabs { get; }
        //*************************************************
        //DebugWindow注入
        //*************************************************
        public DebugViewModel DebugVM { get; }
        //*************************************************
        //PresetViewModel注入
        //*************************************************
        public PresetViewModel PresetVM { get; }
        //*************************************************
        //RelayViewModel注入
        //*************************************************
        public RelayViewModel Relay { get; }
        //*************************************************
        //ResourceViewModel注入（ComboItemMaster取込）
        //未使用→拡張用
        //*************************************************
        public ResourceViewModel Resources { get; }
        //*************************************************
        //各測定器定義用
        //InstrumentService注入し公開プロパティに渡す
        //*************************************************
        public IInstrumentService InstrumentService { get; }
        public ObservableCollection<InstrumentViewModel> Instruments => InstrumentService.Instruments;
        public List<InstrumentViewModel> MeasInst => InstrumentService.Instruments.ToList();
        //*************************************************
        //DialogService注入
        //*************************************************
        public IDialogService DialogService { get; }
        //*************************************************
        //プリセットボタン用
        //*************************************************
        private readonly PresetManager? _presetManager;
        //*************************************************
        //InstrumentのIdentifier検索用ヘルパー
        //*************************************************
        private InstrumentViewModel? FindInstrument(string identifier)
                                                => Instruments.FirstOrDefault(i => i.Identifier == identifier);
        //*************************************************
        //複数測定、温度測定CheckBoxチェック用
        //*************************************************
        private readonly Dictionary<int, Action> _instrumentCheckHandlers = new();
        private void RegisterInstrumentHandlers()
        {
            var thermo = FindInstrument("THERMO");
            var relay = FindInstrument("RELAY");

            if (thermo != null)
            {
                thermo.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(InstrumentViewModel.IsChecked))
                    {
                        OnPropertyChanged(nameof(ThermoChecked));
                        if (!thermo.IsChecked && MultiTemperature)
                            MultiTemperature = false;
                    }
                };
            }

            if (relay != null)
            {
                relay.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(InstrumentViewModel.IsChecked))
                    {
                        OnPropertyChanged(nameof(RelayChecked));
                        if (!relay.IsChecked && Relay.MultiSample)
                            Relay.MultiSample = false;
                    }
                };
            }
        }
        public bool ThermoChecked => FindInstrument("THERMO")?.IsChecked ?? false;
        public bool RelayChecked => FindInstrument("RELAY")?.IsChecked ?? false;
        //*************************************************
        //温度リスト用
        //*************************************************
        private bool _multiTemperature;                //複数温度測定 CheckBox
        public bool MultiTemperature { get => _multiTemperature; 
            set 
            {
                if (_multiTemperature != value)
                {
                    _multiTemperature = value;
                    OnPropertyChanged();
                    if (value)
                        TemperatureListText = "25.0";   //TemperatureListTextに25.0自動入力
                    //else
                    //    TemperatureListText = "";       //MultiTemperatureチェックを外すとTemperatureListTextクリア
                }
            } 
        }
        private string _temperatureListText;
        private List<float> _temperatures = new();
        public string TemperatureListText { get => _temperatureListText;
            set
            {
                if (_temperatureListText != value)
                {
                    _temperatureListText = value;
                    OnPropertyChanged();
                    UpdateTemperatures();
                }
            }
        }
        public List<float> Temperatures => _temperatures;
        //*************************************************
        //測定マネージャー用
        //*************************************************
        private MeasurementManager? _manager;
        //*************************************************
        //JSONファイルReadWrite用
        //*************************************************
        private readonly IJsonDataService _dataService;
        private ObservableCollection<PresetItemBase> _userConfigs;
        public ObservableCollection<PresetItemBase> UserConfigs { get => _userConfigs; set => SetProperty(ref _userConfigs, value); }
        //*************************************************
        //処理の実行状態管理プロパティ
        //*************************************************
        public bool IsRunning { get => _isRunning;
            set
            {
                if (_isRunning != value)
                {
                    _isRunning = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsUiEnabled)); //IsUiEnabled の更新を通知
                    SweepTabs.UpdateTabContentEnabled(!_isRunning);
                    DelayTabs.UpdateTabContentEnabled(!_isRunning);
                    VITabs.UpdateTabContentEnabled(!_isRunning);
                    (GetUSBIDCommand as RelayCommandAsync)?.RaiseCanExecuteChanged();
                    (ConnectUSBIDCommand as RelayCommandAsync)?.RaiseCanExecuteChanged();
                    //(NotYetCommand as RelayCommandAsync)?.RaiseCanExecuteChanged();
                }
            }
        }
        //*************************************************
        //デバイス情報取得ボタン状態管理プロパティ
        //*************************************************
        public bool IsScanningDevices { get => _isScanningDevices; set => SetProperty(ref _isScanningDevices, value); }
        //*************************************************
        //測定ステータス管理プロパティ
        //*************************************************
        public string MeasurementStatus { get => _measurementStatus; set => SetProperty(ref _measurementStatus, value); }
        //*************************************************
        //MainWindowとInstrumentService間受け渡し
        //*************************************************
        public ObservableCollection<string> USBIDList => InstrumentService.USBIDList;
        public ObservableCollection<string> GPIBList => InstrumentService.GPIBList;
        public ObservableCollection<string> FT2232HList => InstrumentService.FT2232HList;
        //*************************************************
        //選択したUSBIDを取得
        //*************************************************
        private string _selectedUSBID;
        public string SelectedUSBID { get => _selectedUSBID; set => SetProperty(ref _selectedUSBID, value); }
        //*************************************************
        //コマンド一覧
        //*************************************************
        public ICommand StartMeasurementCommand { get; }
        public ICommand GetUSBIDCommand { get; }
        public ICommand ConnectUSBIDCommand { get; }
        public ICommand DebugSendCommand { get; }
        public ICommand DebugQueryCommand { get; }
        public ICommand DebugLogClearCommand { get; }
        public ICommand DebugTextboxClearCommand { get; }
        //public ICommand NotYetCommand { get; }
        //*************************************************
        //設定保存,読み込みサービス注入
        //*************************************************
        public IUserConfigService UserConfigService { get; }
        //*************************************************
        //UIを有効/無効にするためのプロパティ（!_isRunning）
        //*************************************************
        public bool IsUiEnabled => !_isRunning;
        //*************************************************
        //測定器のチェックがどれか入っていれば有効
        //*************************************************
        private bool CanExecuteCommands() => !IsRunning && Instruments.Any(i => i.IsChecked);
        //*************************************************
        //全Tabのチェック状態を集約するプロパティ
        //*************************************************
        public string AllCheckedTabNamesText { get => _allCheckedTabNamesText; set => SetProperty(ref _allCheckedTabNamesText, value); }
        //*************************************************
        //最終結果出力ファイル名
        //*************************************************
        public string FinalFileName { get; set; } = "";
        //*************************************************
        //測定器信号名テキストボックス表示フラグ
        //*************************************************
        private void InitializeSignalNameFlags()
        {
            // SignalName を持つ測定器
            var hasSignal = new[]
            {
                "SOURCE1", "SOURCE2", "SOURCE3", "SOURCE4",
                "DMM1", "DMM2", "DMM3", "DMM4"
            };
            foreach (var id in hasSignal)
            {
                var inst = FindInstrument(id);
                if (inst != null)
                    inst.HasSignalName = true;
            }

            // SignalName を持たない測定器
            var noSignal = new[]
            {
                "OSC", "PG", "THERMO", "RELAY"
            };
            foreach (var id in noSignal)
            {
                var inst = FindInstrument(id);
                if (inst != null)
                    inst.HasSignalName = false;
            }
        }

        //*************************************************
        //デザイン時用コンストラクタ
        //*************************************************
        public MainViewModel()
        {
            //以下すべてデザイン時のダミーデータ
            InstrumentService = new DesignInstrumentService();
            Resources = new ResourceViewModel();
            SweepTabs = new SweepTabGroupViewModel(UserConfigService, Resources);
            DelayTabs = new DelayTabGroupViewModel(UserConfigService, Resources);
            VITabs = new VITabGroupViewModel(UserConfigService, Resources);
            InitializeSignalNameFlags();
            foreach (var inst in Instruments)
            {
                inst.USBIDList = InstrumentService.USBIDList;
            }
        }
        //*************************************************
        //ランタイム用コンストラクタ
        //*************************************************
        public MainViewModel(
            IInstrumentService instrumentService,
            IJsonDataService dataService,
            DebugViewModel debug,
            IDialogService dialogService,
            RelayViewModel relayVM,
            SweepTabGroupViewModel sweepTabs,
            DelayTabGroupViewModel delayTabs,
            VITabGroupViewModel viTabs,
            PresetViewModel presetVM,
            IUserConfigService userConfigService,
            PresetManager presetManager,
            TabFactory tabFactory,
            Sweep sweepAct,
            Delay delayAct,
            VI viAct,
            Thermo thermoAct,
            InpCheck errCheck,
            UT utility,
            ResourceViewModel resources)
        {
            DebugVM = debug;
            InstrumentService = instrumentService;
            DialogService = dialogService;
            Relay = relayVM;
            SweepTabs = sweepTabs;
            DelayTabs = delayTabs;
            VITabs = viTabs;
            PresetVM = presetVM;
            UserConfigService = userConfigService;
            _presetManager = presetManager;
            _presetManager.TabFactory = tabFactory;
            Resources = resources;

            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));

            AllCheckedTabNamesText = "対象なし";
            //**********************************
            //測定器入力欄生成
            //**********************************
            InitializeSignalNameFlags();
            foreach (var inst in Instruments)
            {
                inst.USBIDList = InstrumentService.USBIDList;
            }
            //**********************************
            //リレーシリアル番号取得
            //**********************************
            var relay = FindInstrument("RELAY");
            if (relay != null)
            {
                //**********************************
                //初期設定
                //**********************************
                Relay.RelaySerialNumber = relay.UsbId;
                //**********************************
                //リレーシリアル番号が変更になった時更新
                //**********************************
                relay.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(InstrumentViewModel.UsbId))
                        Relay.RelaySerialNumber = relay.UsbId;
                };
            }

            //**********************************
            //CheckBoxチェックON/OFF
            //**********************************
            RegisterInstrumentHandlers();
            //**********************************
            //タブ単体から所属するタブグループを逆引き
            //**********************************
            UserConfigService.ResolveGroupFromTab = tab =>
            {
                if (SweepTabs.Tabs.Contains(tab)) return SweepTabs;
                if (DelayTabs.Tabs.Contains(tab)) return DelayTabs;
                if (VITabs.Tabs.Contains(tab)) return VITabs;
                return null;
            };
            //**********************************
            //測定開始コマンド（ボタンは常に有効）
            //**********************************
            StartMeasurementCommand = new RelayCommandAsync(execute: async (param) => await StartMeasurementAsync(), () => true);
            //**********************************
            //USBID取得コマンド
            //**********************************
            GetUSBIDCommand = new RelayCommandAsync(
                                                    async _ =>
                                                    {
                                                        IsScanningDevices = true;   //OverLayControl=ON
                                                        var result = await InstrumentService.GetDeviceListsAsync();
                                                        if (!result.Success && !DebugVM.SkipUsbIdCheck)
                                                        {
                                                            DialogService.ShowError(result.Message);
                                                            return;
                                                        }
                                                        IsScanningDevices = false;  //OverLayControl=OFF
                                                    },
                                                    () => !IsRunning);
            //**********************************
            //測定器接続確認コマンド
            //**********************************
            ConnectUSBIDCommand = new RelayCommandAsync(
                                                    async _ =>
                                                    {
                                                        //各ID ComboBox背景色リセット
                                                        foreach (var inst in Instruments)
                                                        {
                                                            inst.IsConnected = false;
                                                        }
                                                        //通信確認
                                                        var result = await InstrumentService.ConnectAllAsync();
                                                        //通信確認成功したIDのみ背景色変更
                                                        if (result.Success)
                                                        {
                                                            foreach (var inst in Instruments)
                                                            {
                                                                if (result.Data.Contains(inst.Identifier))
                                                                    inst.IsConnected = true;
                                                            }
                                                        }
                                                        DialogService.ShowMessage(result.Message);
                                                    },
                                                    CanExecuteCommands);
            //**********************************
            //タブ差し替え実行（Load用）
            //**********************************
            UserConfigService.ReplaceTabCallback = (id, newTab) =>
            {
                switch (newTab)
                {
                    case SweepTabViewModel sweepVm:
                        SweepTabs.ReplaceTab(id, sweepVm);
                        break;

                    case DelayTabViewModel delayVm:
                        DelayTabs.ReplaceTab(id, delayVm);

                        break;

                    case VITabViewModel viVm:
                        VITabs.ReplaceTab(id, viVm);
                        break;
                }
            };

            //NotYetCommand = new RelayCommandAsync(execute: async (param) => await NotYet(), canExecute: () => !IsRunning);
            //**********************************
            //Debugコマンド
            //**********************************
            DebugSendCommand = new RelayCommandAsync(execute: async (param) => await DebugVM.DebugSendAsync(), CanExecuteCommands);
            DebugQueryCommand = new RelayCommandAsync(execute: async (param) => await DebugVM.DebugQueryAsync(), CanExecuteCommands);
            DebugLogClearCommand = new RelayCommandAsync(async (param) => DebugVM.DebugLog = string.Empty, CanExecuteCommands);             //DebugLogをClear
            DebugTextboxClearCommand = new RelayCommandAsync(async (param) => DebugVM.DebugTextBox = string.Empty, CanExecuteCommands);     //DebugTextBoxをClear

            //**********************************
            //各タブのPropertyChangedを監視
            //**********************************
            SweepTabs.PropertyChanged += TabGroup_PropertyChanged;
            DelayTabs.PropertyChanged += TabGroup_PropertyChanged;
            VITabs.PropertyChanged += TabGroup_PropertyChanged;
            UpdateAllCheckedTabNamesText();

            //**********************************
            //検出復帰動作もしくは初期電圧設定でPGを選択した場合、ユーザーに通知
            //**********************************
            foreach (var tab in SweepTabs.Tabs)
                tab.DetectSourceIndexPGSelected += OnPGSelected;
            foreach (var tab in DelayTabs.Tabs)
                tab.DetectSourceIndexPGSelected += OnPGSelected;
            foreach (var tab in VITabs.Tabs)
                tab.DetectSourceIndexPGSelected += OnPGSelected;

            //**********************************
            //default最終ファイル名生成
            //**********************************
            FinalFileName = utility.GetDefaultFileNameOnly();

            MeasurementStatus = "準備完了";
        }
         //****************************************************************************
        //動作
        // 各TabのCheckedTabNamesText変更を監視
        // 変更が発生したら更新
        //****************************************************************************
        private void TabGroup_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SweepTabGroupViewModel.CheckedTabNamesText) ||
                e.PropertyName == nameof(DelayTabGroupViewModel.CheckedTabNamesText) ||
                e.PropertyName == nameof(VITabGroupViewModel.CheckedTabNamesText))
            {
                UpdateAllCheckedTabNamesText();
            }
        }
        //****************************************************************************
        //動作
        // AllCheckedTabNamesTextを更新
        //****************************************************************************
        private void UpdateAllCheckedTabNamesText()
        {
            var checkedTabs = new List<string>();

            AddCheckedTabText(checkedTabs, "SweepTab", SweepTabs.CheckedTabNamesText);
            AddCheckedTabText(checkedTabs, "DelayTab", DelayTabs.CheckedTabNamesText);
            AddCheckedTabText(checkedTabs, "VITab", VITabs.CheckedTabNamesText);

            AllCheckedTabNamesText = checkedTabs.Any()
                ? string.Join(", ", checkedTabs)
                : "対象なし";
        }
        private void AddCheckedTabText(List<string> list, string label, string text)
        {
            if (!string.IsNullOrWhiteSpace(text) && text != "対象なし")
                list.Add($"{label}:{text}");
        }
        //****************************************************************************
        //動作
        // 検出復帰動作もしくは初期電圧設定でPGを選択した場合、ユーザーに通知
        //****************************************************************************
        private void OnPGSelected(object? sender, EventArgs e)
        {
            string message = "PGは現在使用できません";
            DialogService.ShowError(message);
        }
        //****************************************************************************
        //動作
        // 未実装通知
        //****************************************************************************
        //private async Task NotYet()
        //{
        //    string message = "未実装です";
        //    DialogService.ShowMessage(message);
        //}
        //*************************************************
        //動作
        // 温度リスト更新
        //*************************************************
        private void UpdateTemperatures()
        {
            //入力文字列をコンマで分割 → 空要素は除外
            var parts = TemperatureListText?
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)    //コンマ区切り
                .Select(p => p.Trim())                                          //前後の空白を削除
                .Where(p => !string.IsNullOrWhiteSpace(p))                      //空白文字列除外
                ?? Array.Empty<string>();                                       //null対策に空配列

            var list = new List<float>();
            var invalid = new List<string>();

            foreach (var part in parts)
            {
                if (float.TryParse(part, out float temp))
                {
                    list.Add(temp);
                }
                else
                {
                    invalid.Add(part);
                }
            }
            //リスト内重複削除＋昇順ソート＋空入力はdefault(25.0℃)
            _temperatures = list.Any() ? list.Distinct().OrderBy(x => x).ToList()
                                       : new List<float> { 25.0f };
        }
        //****************************************************************************
        //動作
        // 測定開始ボタンが押された時
        //****************************************************************************
        private async Task StartMeasurementAsync()
        {
            if (!IsRunning)
            {
                _manager = new MeasurementManager(
                    this,
                    Relay,
                    _thermoAct,
                    _sweepAct,
                    _delayAct,
                    _viAct,
                    DebugVM,
                    _utility,
                    _errCheck
                );

                await _manager.RunAsync(FinalFileName);
            }
            else
            {
                _manager.Cancel();
            }
        }
        //****************************************************************************
        //動作
        // 下記最終出力結果を並び替え
        //----------------  ----------------
        // 温度             項目名,温度  
        // Sample           Sample,測定値
        // 項目名,測定値
        //----------------  ----------------
        //****************************************************************************
        public List<string> CreatePivotRows(List<string> sourceRows, bool multiTemp, bool multiSample)
        {
            var data = new Dictionary<(float Temp, string Item, string Port), double>();
            var itemOrder = new List<string>();   //項目の出現順
            var portOrder = new Dictionary<string, List<string>>(); //各項目ごとのポート順

            float currentTemp = float.NaN;
            string currentSample = "単体";
            bool normalsweep = false;
            //取り込み中止トリガリスト
            //var stopTriggers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            //    {
            //        "=====",          // 区切り線
            //        "設定値",          // フッター開始
            //        "以下測定したタブの設定値",
            //        "DeviceList",
            //        "---",
            //        "#"
            //    };
            //*********************
            //sourceRowsからデータ取り込み
            //*********************
            foreach (var raw in sourceRows)
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line)) 
                    continue;
                if (normalsweep)
                    break;
                // 停止トリガーにマッチしたら即終了（それ以降は無視）
                //if (stopTriggers.Any(trigger => line.Contains(trigger)) ||
                //    line.StartsWith("#") ||
                //    line.StartsWith("---"))
                //{
                //    break;  // ここでループ終了
                //}

                //--- 温度行検出 ---
                if (line.Contains("℃") && (line.StartsWith("サーモ") || line.Contains("温度")))
                {
                    var m = System.Text.RegularExpressions.Regex.Match(line, @"-?\d+\.?\d*");
                    if (m.Success && float.TryParse(m.Value, out float t))
                        currentTemp = t;
                    continue;
                }

                //--- リレー行検出 ---
                if (line.StartsWith("Sample"))
                {
                    currentSample = line.Trim();
                    continue;
                }
                //--- normalsweep検出 ---
                if (line.StartsWith("normalsweep"))
                {
                    normalsweep = true;
                    continue;
                }

                //--- 測定項目行＋測定値 ---
                if (line.Contains(','))
                {
                    var parts = line.Split(',');
                    if (parts.Length >= 2 &&
                        double.TryParse(parts[1], System.Globalization.NumberStyles.Any,
                                       System.Globalization.CultureInfo.InvariantCulture, out double value))
                    {
                        string item = parts[0].Trim();

                        //温度が未設定 → 単一測定 or ポートのみ → 25℃（仮）or 0 で埋める
                        float tempToUse = float.IsNaN(currentTemp) ? 25.0f : currentTemp;

                        //サンプルが未設定 → 単一測定 or 温度のみ → "単体" で統一
                        string portToUse = string.IsNullOrEmpty(currentSample) ||
                                            currentSample == "単体" ? "単体" : currentSample;

                        //出現順を保持
                        if (!itemOrder.Contains(item))
                            itemOrder.Add(item);

                        if (!portOrder.ContainsKey(item))
                            portOrder[item] = new List<string>();
                        if (!portOrder[item].Contains(portToUse))
                            portOrder[item].Add(portToUse);

                        // データ格納（温度もサンプルも必ず入る！）
                        data[(tempToUse, item, portToUse)] = value;
                    }
                }
            }
            //*********************
            //取り込み失敗や中止した場合、渡されたデータを返す
            //*********************
            if (data.Count == 0 || normalsweep) 
                return sourceRows;

            var temperatures = data.Keys.Select(k => k.Temp).Distinct().OrderBy(t => t).ToList();

            var result = new List<string>();
            //*********************
            //温度、サンプル、項目並び替え
            //*********************
            //複数温度＋複数ポート
            if (multiTemp && multiSample)
            {
                result.Add("項目,サンプル番号," + string.Join(",", temperatures.Select(t => $"{t}℃")));

                foreach (var item in itemOrder)
                {
                    foreach (var port in portOrder[item])
                    {
                        var row = new List<string> { item, port };
                        foreach (var temp in temperatures)
                        {
                            if (data.TryGetValue((temp, item, port), out double v))
                                row.Add(v.ToString("F8"));
                            else
                                row.Add("");
                        }
                        result.Add(string.Join(",", row));
                    }
                }
            }
            //複数温度のみ
            else if (multiTemp)
            {
                result.Add("項目," + string.Join(",", temperatures.Select(t => $"{t}℃")));

                foreach (var item in itemOrder)
                {
                    var row = new List<string> { item };
                    var port = portOrder[item].First(); //1つしかない
                    foreach (var temp in temperatures)
                        row.Add(data.TryGetValue((temp, item, port), out double v) 
                            ? v.ToString("F8") : "");
                    result.Add(string.Join(",", row));
                }
            }
            //複数ポートのみ
            else if (multiSample)
            {
                var allPorts = portOrder.Values.SelectMany(x => x).Distinct().OrderBy(x => x).ToList();
                result.Add("項目," + string.Join(",", allPorts));

                foreach (var item in itemOrder)
                {
                    var row = new List<string> { item };
                    foreach (var port in allPorts)
                        row.Add(data.TryGetValue((temperatures.First(), item, port), out double v)
                            ? v.ToString("F8") : "");
                    result.Add(string.Join(",", row));
                }
            }
            //単一測定 ピボット不要
            else
            {
                return sourceRows;
            }

            return result;
        }
        //****************************************************************************
        //動作
        // 最終データ追記用フッター生成
        //****************************************************************************
        public string BuildSettingsFooter(List<(bool IsChecked, string UsbId, string InstName, string Identifier)> measInst)
        {
            var sections = new List<string>();
            var options = new JsonSerializerOptions { WriteIndented = true };

            sections.Add($"{Environment.NewLine}===== 以下測定したタブの設定値 =====");
            //*********************
            //測定器リスト
            //*********************
            sections.Add("--- DeviceList ---");
            var checkedDevices = measInst.Where(inst => inst.IsChecked).ToList();
            if (checkedDevices.Count > 0)
            {
                var insList = CreateInsList(checkedDevices);  //measInstを直接渡してフィルタ済みリスト生成
                sections.AddRange(insList);
            }
            else
            {
                sections.Add("No devices used.");  //デバイスなしの場合のフォールバック
            }
            //*********************
            //Sweepタブ
            //*********************
            if (SweepTabs.Tabs.Any(t => t.MeasureOn))
            {
                var sweepTabs = SweepTabs.Tabs.Where(t => t.MeasureOn).ToArray();
                var json = JsonSerializer.Serialize(sweepTabs, options);
                sections.Add($"--- Sweep Settings ---{Environment.NewLine}{json}");
            }
            //*********************
            //Delayタブ
            //*********************
            if (DelayTabs.Tabs.Any(t => t.MeasureOn))
            {
                var delayTabs = DelayTabs.Tabs.Where(t => t.MeasureOn).ToArray();
                var json = JsonSerializer.Serialize(delayTabs, options);
                sections.Add($"--- Delay Settings ---{Environment.NewLine}{json}");
            }
            //*********************
            //VIタブ
            //*********************
            if (VITabs.Tabs.Any(t => t.MeasureOn))
            {
                var viTabs = VITabs.Tabs.Where(t => t.MeasureOn).ToArray();
                var json = JsonSerializer.Serialize(viTabs, options);
                sections.Add($"--- VI Settings ---{Environment.NewLine}{json}");
            }

            return sections.Any()
                ? string.Join("\n", sections) + "\n"
                : "";
        }
        private List<string> CreateInsList(List<(bool IsChecked, string UsbId, string InstName, string Identifier)> measInstData)
        {
            return measInstData
                .Where(inst => inst.IsChecked)
                .Select(inst => $"{inst.Identifier ?? ""},{inst.UsbId ?? ""},{inst.InstName ?? ""}")
                .ToList();
        }
        //****************************************************************************
        //DebugViewModelへの中継
        //****************************************************************************
        public void LogDebug(string message)
        {
            DebugVM.LogDebug(message);
        }
        //****************************************************************************
        //デザイン用コンストラクタのダミークラス
        //****************************************************************************
        private class DesignInstrumentService : IInstrumentService
        {
            public ObservableCollection<string> USBIDList { get; } =
                new ObservableCollection<string>
                {
                    "デザイン用ダミーUSBID1",
                    "デザイン用ダミーUSBID2"
                };
            public ObservableCollection<string> GPIBList { get; } =
                new ObservableCollection<string>
                {
                    "デザイン用ダミーGPIBID"
                };
            public ObservableCollection<string> FT2232HList { get; } =
                new ObservableCollection<string>
                {
                    "デザイン用ダミーFTDI1",
                    "デザイン用ダミーFTDI2"
                };
            public ObservableCollection<InstrumentViewModel> Instruments { get; } =
                new ObservableCollection<InstrumentViewModel>
                {
                    new("SOURCE1", "input USB ID", "", "電源1", false),
                    new("SOURCE2", "input USB ID", "", "電源2", false),
                    new("SOURCE3", "input USB ID", "", "電源3", false),
                    new("SOURCE4", "input USB ID", "", "電源4", false),
                    new("OSC", "input USB ID", "", "OSC", false),
                    new("PULSE", "input USB ID", "", "PULSE", false),
                    new("DMM1", "input USB ID", "", "DMM1", false),
                    new("DMM2", "input USB ID", "", "DMM2", false),
                    new("DMM3", "input USB ID", "", "DMM3", false),
                    new("DMM4", "input USB ID", "", "DMM4", false),
                    new("THERMO", "input GPIB Addr.", "", "サーモ", false),
                    new("RELAY", "input Serial No.", "", "リレー", false)
                };
            public Task<Result> GetDeviceListsAsync()
            {
                return Task.FromResult(Result.Ok("Design mode"));
            }
            public Task<Result<List<string>>> ConnectAllAsync()
            {
                return Task.FromResult(Result<List<string>>.Ok(new List<string>(), "Design mode"));
            }
        }

    }
    //*********************
    //コマンドの有効/無効制御
    //*********************
    public class RelayCommandAsync : ICommand
    {
        private readonly Func<object, Task> _execute;
        private readonly Func<bool> _canExecute;
        private EventHandler _canExecuteChanged;
        public Func<bool>? ExternalCanExecute { get; set; }
        public RelayCommandAsync(Func<object, Task> execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add
            {
                _canExecuteChanged += value;
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                _canExecuteChanged -= value;
                CommandManager.RequerySuggested -= value;
            }
        }

        public bool CanExecute(object parameter)
        {
            //MainViewModel からの外部 canExecute があればそちらを優先
            if (ExternalCanExecute != null)
                return ExternalCanExecute();

            //それ以外は従来の canExecute を使う
            return _canExecute?.Invoke() ?? true;
        }
        public async void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                await _execute(parameter);
            }
        }
        public void RaiseCanExecuteChanged()
        {
            _canExecuteChanged?.Invoke(this, EventArgs.Empty);
            CommandManager.InvalidateRequerySuggested();
        }
    }
}