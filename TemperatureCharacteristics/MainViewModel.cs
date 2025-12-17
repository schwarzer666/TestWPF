using DelayAction;                  //ActDelay.cs
using DelayTab;                     //LayoutDelayTab.cs
using GPIBcommunication;
using InputCheck;                   //ErrCheck.cs
using SweepAction;                  //ActSweep.cs
using SweepTab;                     //LayoutSweepTab.cs
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using TemperatureCharacteristics.Act;
using TemperatureCharacteristics.Layout;
using TemperatureCharacteristics.Models;
using TemperatureCharacteristics.Services;
using ThermoAction;
using USBcommunication;             //CommUSB.cs
using UTility;                      //Utility.cs
using VIAction;                     //ActVI.cs
using VITab;                        //LayoutVITab.cs


namespace TemperatureCharacteristics
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly SweepTabWindow _sweepTab;
        private readonly DelayTabWindow _delayTab;
        private readonly VITabWindow _viTab;
        private readonly Sweep _sweepAct;
        private readonly Delay _delayAct;
        private readonly VI _viAct;
        private readonly Thermo _thermoAct;
        private readonly InpCheck _errCheck;
        private readonly UT _utility;
        private readonly USBcomm _getUSBID;
        private readonly GPIBComm _getGPIBID;
        private readonly FT2232HDeviceFinder _getFtdiID;
        private readonly USBcomm _commSend;
        private readonly USBcomm _commQuery;
        private readonly List<(CheckBox checkBox, ComboBox? textBox_ID, TextBox? textBox_NAME)> _measInst;   //フィールド変数meas_inst
        private bool _isRunning;                    //動作フラグ
        private object _currentTabViewModel;        //選択中のTabViewModelの保持用
        private string _measurementStatus;
        private CancellationTokenSource? _cts;       //キャンセルトークンのソース
        private ObservableCollection<string> _usbIdList;
        private ObservableCollection<string> _gpibList;
        private ObservableCollection<string> _ft2232hList;
        private string _debugTextBox;
        private string _debugLog;
        private string _debugUSBID = "debug_USBID";
        private string _debugSendCmd = "Send_cmd";
        private string _allCheckedTabNamesText;     //全Tabのチェック状態を集約
        private List<Device>? _cachedSweepDevices;  //プロパティ値のキャッシュ用
        private List<Device>? _cachedDelayDevices;  //プロパティ値のキャッシュ用
        private List<Device>? _cachedVIDevices;     //プロパティ値のキャッシュ用
        //*************************************************
        //公開プロパティ（シングルトンインスタンスへの参照）
        //*************************************************
        public SweepTabWindow SweepTab => _sweepTab;
        public DelayTabWindow DelayTab => _delayTab;
        public VITabWindow VITab => _viTab;
        //*************************************************
        //DebugWindow
        //*************************************************
        public DebugSettingsViewModel DebugSettings { get; } = new DebugSettingsViewModel();
        //*************************************************
        //プリセットボタン用Popup
        //*************************************************
        private bool _isPopupOpen;
        private ObservableCollection<PresetItemBase> _presetButtons;
        private ObservableCollection<PresetItemBase> _filteredPresetButtons;    //フィルタ適用後のプリセットボタン
        public bool IsPopupOpen { get => _isPopupOpen;
            set
            {
                if (_isPopupOpen != value)
                {
                    _isPopupOpen = value;
                    OnPropertyChanged(nameof(IsPopupOpen));
                }
            }
        }
        //*************************************************
        //リレーマニュアルON/OFF用
        //*************************************************
        private readonly FT2232HBitBangService _bitBang;
        private int? _selectedRelay = 1;
        public int? SelectedRelay { get => _selectedRelay;
            set
            {
                if (_selectedRelay != value)
                {
                    _selectedRelay = value;
                    OnPropertyChanged();
                }
            }
        }
        //*************************************************
        //複数測定、温度測定CheckBoxチェック用
        //*************************************************
        private readonly Dictionary<int, Action> _instrumentCheckHandlers = new();
        private void RegisterInstrumentHandlers()
        {
            _instrumentCheckHandlers[10] = () =>
            {
                if (!Instruments[10].IsChecked && MultiTemperature)
                    MultiTemperature = false;
            };

            _instrumentCheckHandlers[11] = () =>
            {
                if (!Instruments[11].IsChecked && MultiSample)
                    MultiSample = false;
            };
        }
        private void SubscribeInstrumentCheckChanges()
        {
            foreach (var kvp in _instrumentCheckHandlers)
            {
                int index = kvp.Key;
                Action handler = kvp.Value;

                Instruments[index].PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(InstrumentViewModel.IsChecked))
                        handler.Invoke();
                };
            }
        }
        //*************************************************
        //サンプルカウント用
        //*************************************************
        private bool _multiSample;                //複数サンプル測定 CheckBox
        public bool MultiSample { get => _multiSample;
            set
            {
                if (_multiSample != value)
                {
                    _multiSample = value;
                    OnPropertyChanged();
                }
            }
        }
        public bool Instruments11Checked { get => Instruments[11].IsChecked;
            set
            {
                if (Instruments[11].IsChecked != value)
                {
                    Instruments[11].IsChecked = value;
                    OnPropertyChanged();

                    //false になったらMultiSampleを強制falseに
                    if (!value && MultiSample)
                        MultiSample = false;
                }
            }
        }
        private int _sampleCount = 1;
        public int SampleCount { get => _sampleCount;
            set
            {
                if (_sampleCount != value)
                {
                    _sampleCount = value;
                    OnPropertyChanged();
                }
            }
        }
        public int? sampleCount => SampleCount;
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
        public bool Instruments10Checked { get => Instruments[10].IsChecked;
            set
            {
                if (Instruments[10].IsChecked != value)
                {
                    Instruments[10].IsChecked = value;
                    OnPropertyChanged();

                    //false になったらMultiTemperatureを強制falseに
                    if (!value && MultiTemperature)
                        MultiTemperature = false;
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

        public object CurrentTabViewModel { get => _currentTabViewModel; set { _currentTabViewModel = value;  OnPropertyChanged(nameof(CurrentTabViewModel)); } }
        //*************************************************
        //JSONファイルReadWrite用
        //*************************************************
        private readonly IJsonDataService _dataService;
        private ObservableCollection<PresetItemBase> _userConfigs;

        public ObservableCollection<InstrumentViewModel> Instruments { get; } = new ObservableCollection<InstrumentViewModel>
        {
            new InstrumentViewModel { Identifier = "電源1", Tag = "SOURCE1", UsbId = "input USB ID" },
            new InstrumentViewModel { Identifier = "電源2", Tag = "SOURCE2", UsbId = "input USB ID" },
            new InstrumentViewModel { Identifier = "電源3", Tag = "SOURCE3", UsbId = "input USB ID" },
            new InstrumentViewModel { Identifier = "電源4", Tag = "SOURCE4", UsbId = "input USB ID" },
            new InstrumentViewModel { Identifier = "OSC", Tag = "OSC", UsbId = "input USB ID" },
            new InstrumentViewModel { Identifier = "PULSE", Tag = "PULSE", UsbId = "input USB ID" },
            new InstrumentViewModel { Identifier = "DMM1", Tag = "DMM1", UsbId = "input USB ID" },
            new InstrumentViewModel { Identifier = "DMM2", Tag = "DMM2", UsbId = "input USB ID" },
            new InstrumentViewModel { Identifier = "DMM3", Tag = "DMM3", UsbId = "input USB ID" },
            new InstrumentViewModel { Identifier = "DMM4", Tag = "DMM4", UsbId = "input USB ID" },
            new InstrumentViewModel { Identifier = "サーモ", Tag = "THERMO", UsbId = "input GPIB Addr." },
            new InstrumentViewModel { Identifier = "リレー", Tag = "RELAY", UsbId = "input Serial No." }
        };

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
                    (GetUSBIDCommand as RelayCommandAsync)?.RaiseCanExecuteChanged();
                    (ConnectUSBIDCommand as RelayCommandAsync)?.RaiseCanExecuteChanged();
                    (NotYetCommand as RelayCommandAsync)?.RaiseCanExecuteChanged();
                    (OpenPopupCommand as RelayCommandAsync)?.RaiseCanExecuteChanged();
                    (SelectPresetCommand as RelayCommandAsync)?.RaiseCanExecuteChanged();
                }
            }
        }
        public string MeasurementStatus { get => _measurementStatus; set { _measurementStatus = value; OnPropertyChanged(); } }
        public ObservableCollection<string> USBIDList { get => _usbIdList; set { _usbIdList = value; OnPropertyChanged(); } }
        public ObservableCollection<string> GPIBList { get => _gpibList; set { _gpibList = value; OnPropertyChanged(); } }
        public ObservableCollection<string> FT2232HList { get => _ft2232hList; set { _ft2232hList = value; OnPropertyChanged(); } }
        public ObservableCollection<PresetItemBase> PresetButtons { get => _presetButtons; private set { _presetButtons = value; OnPropertyChanged(); } }
        public ObservableCollection<PresetItemBase> FilteredPresetButtons { get => _filteredPresetButtons; private set { _filteredPresetButtons = value; OnPropertyChanged(); } }
        public ObservableCollection<PresetItemBase> UserConfigs { get => _userConfigs; set { _userConfigs = value; OnPropertyChanged(); } }
        public string DebugTextBox { get => _debugTextBox; set { _debugTextBox = value; OnPropertyChanged(); } }
        public string DebugLog { get => _debugLog; set { _debugLog = value; OnPropertyChanged(); } }
        public string DebugUSBID { get => _debugUSBID; set { _debugUSBID = value; OnPropertyChanged(); } }
        public string DebugSendCmd { get => _debugSendCmd; set { _debugSendCmd = value; OnPropertyChanged(); } }
        public ICommand StartMeasurementCommand { get; }
        public ICommand GetUSBIDCommand { get; }
        public ICommand ConnectUSBIDCommand { get; }
        public ICommand DebugSendCommand { get; }
        public ICommand DebugQueryCommand { get; }
        public ICommand DebugLogClearCommand { get; }
        public ICommand DebugTextboxClearCommand { get; }
        public ICommand NotYetCommand { get; }
        //*************************************************
        //プリセットボタン用
        //*************************************************
        public ICommand OpenPopupCommand { get; }
        public ICommand SelectPresetCommand { get; }
        //*************************************************
        //設定保存,読み込みボタン用
        //*************************************************
        public ICommand SaveUserConfigCommand { get; }
        public ICommand LoadUserConfigCommand { get; }
        //*************************************************
        //リレーON/OFFボタン用
        //*************************************************
        public ICommand SetRelayOnCommnad { get; }
        public ICommand SetRelayOffCommand { get; }
        public string? RelaySerialNumber => Instruments[11].UsbId;  //リレー用シリアル番号
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
        public string AllCheckedTabNamesText { get => _allCheckedTabNamesText; set { _allCheckedTabNamesText = value; OnPropertyChanged(); } }
        //*************************************************
        //デザイン時用コンストラクタ
        //*************************************************
        public MainViewModel()
        {
            //以下すべてデザイン時のダミーデータ
            _measInst = new List<(CheckBox, ComboBox?, TextBox?)>();
            _sweepTab = SweepTabWindow.Instance;
            _delayTab = DelayTabWindow.Instance;
            _viTab = VITabWindow.Instance;
            _presetButtons = new ObservableCollection<PresetItemBase>();
            _filteredPresetButtons = new ObservableCollection<PresetItemBase>();
            _userConfigs = new ObservableCollection<PresetItemBase>();
        }
        //*************************************************
        //ランタイム用コンストラクタ
        //*************************************************
        public MainViewModel(List<(CheckBox, ComboBox?, TextBox?)> measInst, IJsonDataService dataService)
        {
            //以下インスタンス生成(=初期化)を別クラス内で実行
            _sweepTab = SweepTabWindow.Instance;
            _delayTab = DelayTabWindow.Instance;
            _viTab = VITabWindow.Instance;
            _sweepAct = Sweep.Instance;
            _delayAct = Delay.Instance;
            _viAct = VI.Instance;
            _thermoAct = Thermo.Instance;
            _errCheck = InpCheck.Instance;
            _utility = UT.Instance;
            _getUSBID = USBcomm.Instance;
            _getGPIBID = GPIBComm.Instance;
            _getFtdiID = FT2232HDeviceFinder.Instance;
            _commSend = USBcomm.Instance;
            _commQuery = USBcomm.Instance;
            _measInst = measInst ?? throw new ArgumentNullException(nameof(measInst));
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));

            USBIDList = new ObservableCollection<string>();
            GPIBList = new ObservableCollection<string>();
            FT2232HList = new ObservableCollection<string>();
            DebugTextBox = string.Empty;
            DebugLog = string.Empty;

            AllCheckedTabNamesText = "対象なし";
            //**********************************
            //各タブのPropertyChangedを監視
            //**********************************
            _sweepTab.PropertyChanged += TabWindow_PropertyChanged;
            _delayTab.PropertyChanged += TabWindow_PropertyChanged;
            _viTab.PropertyChanged += TabWindow_PropertyChanged;
            UpdateAllCheckedTabNamesText();
            //**********************************
            //JSONファイルからプリセットを読み込み
            //**********************************
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Data", "presets.json");
            var (presets, presetLog, selectedPathpreset) = _dataService.LoadItems<PresetItemBase>(filePath);
            PresetButtons = presets;
            this.LogDebug(presetLog);
            //**********************************
            //Popup表示用フィルタ
            //**********************************
            FilteredPresetButtons = new ObservableCollection<PresetItemBase>(presets);
            //**********************************
            //ユーザー設定初期化
            //**********************************
            _userConfigs = new ObservableCollection<PresetItemBase>();
            //**********************************
            //リレーON/OFF初期化
            //**********************************
            _bitBang = new FT2232HBitBangService();
            //**********************************
            //CheckBoxチェックON/OFF
            //**********************************
            RegisterInstrumentHandlers();
            SubscribeInstrumentCheckChanges();

            StartMeasurementCommand = new RelayCommandAsync(execute: async (param) => await StartMeasurementAsync(), () => true);    //ボタンは常に有効
            GetUSBIDCommand = new RelayCommandAsync(execute: async (param) => await GetUSBIDAsync(), canExecute: () => !IsRunning);
            ConnectUSBIDCommand = new RelayCommandAsync(execute: async (param) => await ConnectUSBIDAsync(), canExecute: CanExecuteCommands);
            OpenPopupCommand = new RelayCommandAsync(async (param) => await OpenPopupAsync(param), () => !IsRunning);
            SelectPresetCommand = new RelayCommandAsync(async (param) => await SelectPresetAsync(param), () => !IsRunning && IsPopupOpen);
            SaveUserConfigCommand = new RelayCommandAsync(async (param) => await SaveUserConfigAsync(param), canExecute: () => !IsRunning);
            LoadUserConfigCommand = new RelayCommandAsync(async (param) => await LoadUserConfigAsync(param), canExecute: () => !IsRunning);

            SetRelayOnCommnad = new RelayCommandAsync(execute: async (param) => await SetRelayOnExclusiveAsync(), canExecute: CanExecuteCommands);
            SetRelayOffCommand = new RelayCommandAsync(execute: async (param) => await SetAllRelaysOffAsync(), canExecute: CanExecuteCommands);

            NotYetCommand = new RelayCommandAsync(execute: async (param) => await NotYet(), canExecute: () => !IsRunning);

            DebugSendCommand = new RelayCommandAsync(execute: async (param) => await DebugSend(), CanExecuteCommands);
            DebugQueryCommand = new RelayCommandAsync(execute: async (param) => await DebugQuery(), CanExecuteCommands);
            DebugLogClearCommand = new RelayCommandAsync(async (param) => DebugLog = string.Empty, CanExecuteCommands);             //DebugLogをClear
            DebugTextboxClearCommand = new RelayCommandAsync(async (param) => DebugTextBox = string.Empty, CanExecuteCommands);     //DebugTextBoxをClear

            MeasurementStatus = "準備完了";
//#if DEBUG
//            MultiTemperature = true;
//            MultiSample = true;
//            SampleCount = 3;
//            TemperatureListText = "25.0";
//#endif
        }
        //****************************************************************************
        //動作
        // プリセットオープン（Popupオープン）
        //****************************************************************************
        private async Task OpenPopupAsync(object parameter)
        {
            System.Diagnostics.Debug.WriteLine($"OpenPopupAsync: Parameter={parameter?.GetType()?.FullName}");  //debug用途
            CurrentTabViewModel = parameter; //タブの DataContext を保存
            //Popup表示時フィルタ適用
            FilterPresetsForTab(CurrentTabViewModel);
            IsPopupOpen = !IsPopupOpen;
        }
        //****************************************************************************
        //動作
        // ユーザー設定保存
        //****************************************************************************
        private async Task SaveUserConfigAsync(object parameter)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (parameter != null)
                {
                    UserConfigs = new ObservableCollection<PresetItemBase>();
                    PresetItemBase newConfig = null;
                    switch (parameter)
                    {
                        case SweepTabViewModel sweepTab:
                            var sweepConfig = new PresetItemSweep
                            {
                                Id = UserConfigs.Any() ? (int.Parse(UserConfigs.Max(c => c.Id)) + 1).ToString() : "300",
                                Name = sweepTab.ItemHeader ?? "Item",
                                BackgroundColor = "#FFFFFF",
                                Type = "Sweep"
                            };
                            CopyProperties(sweepTab, sweepConfig,
                                            new object[]
                                            {
                                                sweepConfig.SweepConfig,
                                                sweepConfig.SourceConfig,
                                                sweepConfig.ConstConfig,
                                                sweepConfig.DetectReleaseConfig,
                                                sweepConfig.OscConfig,
                                                sweepConfig.DmmConfig,
                                                sweepConfig.PulseGenConfig
                                            });
                            newConfig = sweepConfig;
                            break;

                        case DelayTabViewModel delayTab:
                            var delayConfig = new PresetItemDelay
                            {
                                Id = UserConfigs.Any() ? (int.Parse(UserConfigs.Max(c => c.Id)) + 1).ToString() : "400",
                                Name = delayTab.ItemHeader ?? "Item",
                                BackgroundColor = "#FFFFFF",
                                Type = "Delay"
                            };
                            CopyProperties(delayTab, delayConfig,
                                            new object[]
                                            {
                                                delayConfig.DelayConfig,
                                                delayConfig.SourceConfig,
                                                delayConfig.ConstConfig,
                                                delayConfig.DetectReleaseConfig,
                                                delayConfig.OscConfig,
                                                delayConfig.PulseGenConfig
                                            });
                            newConfig = delayConfig;
                            break;

                        case VITabViewModel viTab:
                            var viConfig = new PresetItemVI
                            {
                                Id = UserConfigs.Any() ? (int.Parse(UserConfigs.Max(c => c.Id)) + 1).ToString() : "500",
                                Name = viTab.ItemHeader ?? "Item",
                                BackgroundColor = "#FFFFFF",
                                Type = "VI"
                            };

                            CopyProperties(viTab, viConfig,
                                            new object[]
                                            {
                                                viConfig.VIConfig,
                                                viConfig.SourceConfig,
                                                viConfig.ConstConfig,
                                                viConfig.DetectReleaseConfig,
                                                viConfig.DmmConfig,
                                            });
                            newConfig = viConfig;
                            break;

                        default:
                            this.LogDebug($"無効なタブ型: {parameter.GetType().Name}");
                            break;
                    }
                    if (newConfig != null)
                    {
                        UserConfigs.Add(newConfig);
                        var (success, log, selectedPath) = _dataService.SaveItems<PresetItemBase>(null, UserConfigs);
                        this.LogDebug(log);
                        this.LogDebug(success ? 
                            $"ユーザー設定を保存しました: ID={newConfig.Id}, Name={newConfig.Name}, Path={selectedPath ?? "未選択"}"
                            : "ユーザー設定の保存に失敗しました"
                            );
                    }
                }
                else
                    this.LogDebug($"エラー: parameter is null");
            });
        }
        //****************************************************************************
        //動作
        // ユーザー設定読み込み
        //****************************************************************************
        private async Task LoadUserConfigAsync(object parameter)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (parameter is PresetAndTabParameter param &&
                            !string.IsNullOrEmpty(param.TabType) && !string.IsNullOrEmpty(param.TabId))
                {
                    var (configs, loadLog, selectedPath) = _dataService.LoadItems<PresetItemBase>();
                    this.LogDebug(loadLog);
                    if (configs?.Any() != true)
                    {
                        this.LogDebug("ユーザー設定読み込み失敗"); 
                        return; 
                    }

                    UserConfigs = configs;
                    this.LogDebug($"{configs.Count}設定を {selectedPath} から読み込み");

                    foreach (var config in configs)
                        ApplyToTabs(config, FindTargetTab(config, param.TabId));
                }
            });
        }
        //****************************************************************************
        //動作
        // プリセットTypeとTabIdから対象タブを検索
        //****************************************************************************
        private object FindTargetTab(PresetItemBase config, string tabId)
        {
            if (config is PresetItemSweep) 
                return _sweepTab.Tabs.FirstOrDefault(t => t.Id == tabId) ?? _sweepTab.Tabs.FirstOrDefault();
            if (config is PresetItemDelay) 
                return _delayTab.Tabs.FirstOrDefault(t => t.Id == tabId) ?? _delayTab.Tabs.FirstOrDefault();
            if (config is PresetItemVI) 
                return _viTab.Tabs.FirstOrDefault(t => t.Id == tabId) ?? _viTab.Tabs.FirstOrDefault();
            return null;
        }
        //****************************************************************************
        //動作
        // プリセット選択
        //****************************************************************************
        private async Task SelectPresetAsync(object parameter)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (parameter is PresetItemBase preset && CurrentTabViewModel != null)
                {
                    ApplyToTabs(preset, CurrentTabViewModel);
                    IsPopupOpen = false;
                }
                else
                    this.LogDebug($"無効なパラメータ: Preset={parameter?.GetType()?.FullName}, CurrentTabViewModel={CurrentTabViewModel?.GetType()?.FullName}");
            });
        }
        //****************************************************************************
        //動作
        // プリセット適用
        //****************************************************************************
        private void ApplyToTabs(PresetItemBase preset, object targetTab)
        {
            if (targetTab == null)
            {
                this.LogDebug("エラー: 対象タブなし"); 
                return; 
            }
            ApplyProperties(preset, targetTab, GetTabClass(targetTab));
            MeasurementStatus = $"プリセット {preset.Name} を {GetTabType(targetTab)}タブに適用";
        }
        //****************************************************************************
        //動作
        // タブオブジェクトからプリセット種別名を取得
        //****************************************************************************
        private string GetTabType(object tab) => tab switch
        {
            SweepTabViewModel => "Sweep",
            DelayTabViewModel => "Delay",
            VITabViewModel => "VI",
            _ => "Unknown"
        };
        //****************************************************************************
        //動作
        // タブオブジェクトからViewModelクラス型を取得
        //****************************************************************************
        private Type GetTabClass(object tab) => tab switch
        {
            SweepTabViewModel => typeof(SweepTabViewModel),
            DelayTabViewModel => typeof(DelayTabViewModel),
            VITabViewModel => typeof(VITabViewModel),
            _ => null
        };

        //****************************************************************************
        //動作
        // ViewModelからプロパティをコピー
        //****************************************************************************
        public void CopyProperties(object source, object target, IEnumerable<object> nestedTargets = null)
        {
            var sourceProps = source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var targetType = target.GetType();

            foreach (var prop in sourceProps)
            {
                if (!prop.CanRead || prop.Name == "Id") continue;

                var value = prop.GetValue(source);

                //直下のプロパティに設定
                var directProp = targetType.GetProperty(prop.Name);
                if (directProp?.CanWrite == true)
                {
                    directProp.SetValue(target, value);
                    this.LogDebug($"Copied {prop.Name}={value} to {targetType.Name}");
                    continue;
                }

                //ネストされた構成オブジェクトに設定
                if (nestedTargets != null)
                {
                    foreach (var nested in nestedTargets)
                    {
                        var nestedProp = nested.GetType().GetProperty(prop.Name);
                        if (nestedProp?.CanWrite == true)
                        {
                            nestedProp.SetValue(nested, value);
                            this.LogDebug($"Copied {prop.Name}={value} to {nested.GetType().Name}");
                            break;
                        }
                    }
                }
            }
        }

        //****************************************************************************
        //動作
        // 各プロパティに値を反映させる
        //****************************************************************************
        private void ApplyProperties(PresetItemBase preset, object tab, Type tabType)
        {
            foreach (var (path, value) in GetAllProperties(preset))
            {
                var viewModelPropName = path.Contains(".") ? path.Split('.')[1] : path;
                var tabProp = tabType.GetProperty(viewModelPropName);
                if (tabProp != null && tabProp.CanWrite)
                    tabProp.SetValue(tab, value);
            }
        }
        //****************************************************************************
        //動作
        // プリセットボタン表示のフィルタ
        // JSONファイル読み込み後、各Tabに表示するためフィルタをかける
        //****************************************************************************
        private void FilterPresetsForTab(object tabViewModel)
        {
            string? targetType = tabViewModel switch
            {
                SweepTabViewModel _ => "Sweep",
                DelayTabViewModel _ => "Delay",
                VITabViewModel _ => "VI",
                _ => null  //全表示
            };

            FilteredPresetButtons = targetType == null
                ? new ObservableCollection<PresetItemBase>(PresetButtons)
                : new ObservableCollection<PresetItemBase>(PresetButtons.Where(p => p.Type == targetType));
        }
        //****************************************************************************
        //動作
        // JSONファイル読み込み時、サブクラス読み込み用に再帰的にプロパティをコピー
        //****************************************************************************
        private IEnumerable<(string Path, object Value)> GetAllProperties(object obj)
        {
            var props = obj.GetType().GetProperties();
            foreach (var prop in props)
            {
                var value = prop.GetValue(obj);
                if (value != null)
                {
                    var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    if (propType.IsClass && propType != typeof(string))
                        //サブクラス読み込み
                        foreach (var nested in GetAllProperties(value))
                            yield return ($"{prop.Name}.{nested.Path}", nested.Value);
                    else
                        yield return (prop.Name, value);
                }
            }
        }
        //****************************************************************************
        //動作
        // 各TabのCheckedTabNamesText変更を監視
        // 変更が発生したら更新
        //****************************************************************************
        private void TabWindow_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SweepTabWindow.CheckedTabNamesText) ||
                e.PropertyName == nameof(DelayTabWindow.CheckedTabNamesText) ||
                e.PropertyName == nameof(VITabWindow.CheckedTabNamesText))
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
            List<string> checkedTabs = new List<string>();
            if (!string.IsNullOrWhiteSpace(_sweepTab.CheckedTabNamesText) && _sweepTab.CheckedTabNamesText != "対象なし")
                checkedTabs.Add($"SweepTab:{_sweepTab.CheckedTabNamesText}");
            if (!string.IsNullOrWhiteSpace(_delayTab.CheckedTabNamesText) && _delayTab.CheckedTabNamesText != "対象なし")
                checkedTabs.Add($"DelayTab:{_delayTab.CheckedTabNamesText}");
            if (!string.IsNullOrWhiteSpace(_viTab.CheckedTabNamesText) && _viTab.CheckedTabNamesText != "対象なし")
                checkedTabs.Add($"VITab:{_viTab.CheckedTabNamesText}");

            AllCheckedTabNamesText = checkedTabs.Any() ? string.Join(", ", checkedTabs) : "対象なし";
        }
        //****************************************************************************
        //動作
        // 連続動作用
        // 指定されたリレー番号をON
        //****************************************************************************
        private async Task<bool> SetRelayPortOnAsync(int port)
        {
            //シリアルナンバー取得
            string? serial = RelaySerialNumber;
            if (string.IsNullOrWhiteSpace(serial))
            {
                this.LogDebug("リレーのシリアルナンバーが未設定");
                return false;
            }
            //シリアルナンバーでポートオープン+BitBangモード
            if (!await _bitBang.OpenBySerialNumberAsync(serial))
            {
                this.LogDebug("FT2232H 接続失敗");
                return false;
            }

            try
            {
                //全ピンOFF
                for (int pin = 0; pin < 6; pin++)
                    await _bitBang.SetPinAsync(pin, false);

                //選択ピンのみON
                await _bitBang.SetPinAsync(port - 1, true);
                MeasurementStatus = $"リレーport{port} ON";
                return true;
            }
            catch (Exception ex)
            {
                this.LogDebug($"リレー制御エラー: {ex.Message}");
                return false;
            }
            finally
            {
                _bitBang.Dispose();
            }
        }
        //****************************************************************************
        //動作
        // 連続動作用
        // ONしたリレーをOFF
        //****************************************************************************
        private async Task SetRelayPortOffAsync(int port)
        {
            try
            {
                await _bitBang.SetPinAsync(port - 1, false);
            }
            catch { /* 無視 */ }
            finally
            {
                _bitBang.Dispose();
            }
        }
        //****************************************************************************
        //動作
        // マニュアルON/OFF用
        // 選択したリレー番号をON、その他をOFFする
        //****************************************************************************
        private async Task SetRelayOnExclusiveAsync()
        {
            if (SelectedRelay == null) return;
            //シリアルナンバー取得
            string? serial = RelaySerialNumber;
            if (string.IsNullOrWhiteSpace(serial))
            {
                this.LogDebug("リレーのシリアルナンバーが未設定");
                return;
            }
            //シリアルナンバーでポートオープン+BitBangモード
            if (!await _bitBang.OpenBySerialNumberAsync(serial))
            {
                this.LogDebug("FT2232H 接続失敗");
                return;
            }

            try
            {
                //全ピンOFF
                for (int pin = 0; pin < 6; pin++)
                    await _bitBang.SetPinAsync(pin, false);

                //選択ピンのみON
                await _bitBang.SetPinAsync(SelectedRelay.Value - 1, true);
                MeasurementStatus = $"リレー{SelectedRelay} ON";
            }
            finally
            {
                _bitBang.Dispose();
            }
        }
        //****************************************************************************
        //動作
        // マニュアルON/OFF用
        // 全リレーOFF
        //****************************************************************************
        private async Task SetAllRelaysOffAsync()
        {
            if (SelectedRelay == null) return;
            //シリアルナンバー取得
            string? serial = RelaySerialNumber;
            if (string.IsNullOrWhiteSpace(serial))
            {
                this.LogDebug("リレーのシリアルナンバーが未設定");
                return;
            }
            //シリアルナンバーでポートオープン+BitBangモード
            if (!await _bitBang.OpenBySerialNumberAsync(serial))
            {
                this.LogDebug("FT2232H 接続失敗");
                return;
            }

            try
            {
                //全ピンOFF
                for (int pin = 0; pin < 6; pin++)
                    await _bitBang.SetPinAsync(pin, false);

                MeasurementStatus = "全リレー OFF";
            }
            finally
            {
                _bitBang.Dispose();
            }
        }
        //****************************************************************************
        //動作
        // 未実装通知
        //****************************************************************************
        private async Task NotYet()
        {
            string message = "未実装です";
            MessageBox.Show(message);
        }

        //*************************************************
        //動作
        // PCに接続されている測定器のUSBIDを取得(getUSBID.USBIDList)
        //*************************************************
        private async Task GetUSBIDAsync()
        {
            string[] searchString = { "\r\n" };
            List<string> usbList = await Task.Run(() => _getUSBID.GetUSBIDList());
            List<string> gpibLsit = await Task.Run(() => _getGPIBID.GetGPIBList());
            List<FTDeviceInfo> ftList = await Task.Run(() => _getFtdiID.GetFT2232HList());
            USBIDList.Clear();
            if (usbList != null && usbList.Count > 0)
            {
                foreach (string id in usbList)
                {
                    USBIDList.Add(id.Trim());
                }
            }
            GPIBList.Clear();
            if (gpibLsit != null && gpibLsit.Count > 0)
            {
                foreach (string id in gpibLsit)
                {
                    GPIBList.Add(id.Trim());
                }
            }
            FT2232HList.Clear();
            if (ftList != null && ftList.Count > 0)
            {
                foreach (var ft in ftList)
                {
                    FT2232HList.Add(ft.SerialNumber);  //シリアルのみ追加
                }
            }
            //debug*********************************
            var allIds = USBIDList.Concat(GPIBList).Concat(FT2232HList);
            this.LogDebug(string.Join(Environment.NewLine, allIds));
            //*********************************debug
            if (USBIDList.Count == 0 && GPIBList.Count == 0)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Window? owner = Application.Current.MainWindow;
                    MessageBox.Show(
                        owner ?? Application.Current.MainWindow,
                        "IVIに対応した測定器もGPIBも接続されていません",
                        "エラー",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            }
        }
        //*************************************************
        //動作
        // 全測定器のチェックボックスとIDと名前をリスト化
        // チェックボックスがチェックされている測定器のIDを抽出(utility.Get_Active_USBaddr)
        // 各測定器に*IDNを送信し応答があるかチェック(commQuery.Connection_Check)
        // 問題がなければ各IDテキストボックスの色を変える（未実装）
        // ※※とりあえずdebug_textboxにメッセージ
        // try-catch文を使わずに記述
        //*************************************************
        private async Task ConnectUSBIDAsync()
        {
            //*********************
            //定義
            //*********************
            bool isCheck;
            bool allCheck = true;
            string message = string.Empty;                  //メッセージ表示用変数初期化
            string response = string.Empty;
            //*********************
            //UIスレッドがブロックされるため
            //測定器アドレスをコピー
            //*********************
            List<(bool IsChecked, string UsbId, string InstName, string Identifier)> measInstData = Instruments
                .Select(inst => (inst.IsChecked, 	//CheckBoxの状態
                inst.UsbId ?? string.Empty, 		//TextBox_IDが入力されていなければ空白
                inst.InstName ?? string.Empty, 		//TextBox_NAMEが入力されていなければ空白
                inst.Identifier))
                .ToList();
            //*********************
            //checkboxがチェックされているUSBアドレスリスト取得
            //*********************
            List<(string identifier, string usbid, string instname)> activeUsbId = _utility.GetActiveUSBAddr(measInstData);
            if (!activeUsbId.Any())
            {
                message += "チェックされた測定器無し";
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Window? owner = Application.Current.MainWindow;
                    MessageBox.Show(owner ?? Application.Current.MainWindow, message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                });
                return;                 //測定器チェックでエラーなら以降中止
            }
            //*********************
            //測定器入力アドレスチェック
            //*********************
            (List<string> messageCheck, bool InsAddrCheck) = await _errCheck.VerifyInsAddr(measInstData);
            if (messageCheck.Any())
            {
                message = string.Join(Environment.NewLine, messageCheck);
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Window? owner = Application.Current.MainWindow;
                    MessageBox.Show(owner ?? Application.Current.MainWindow, message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                });
                return;                 //測定器アドレスチェックでエラーなら以降中止
            }
            //*********************
            //接続確認(通信が成功するか)
            //*********************
            foreach ((string identifier, string usbid, string instname) device in activeUsbId)
            {
                //debug*********************************
                this.LogDebug($"{device.identifier}←*IDN?");
                //*********************************debug
                if (device.identifier == "リレー")
                {
                    (response, isCheck) = await CheckFT2232HConnection(device.usbid);
                    if (!isCheck)
                        message += $"{device.identifier} (FT2232H) との接続確認失敗";
                }
                else
                {
                    (response, isCheck) = await _commQuery.Connection_Check(device.usbid);
                    if (!isCheck)
                        message += $"{device.identifier}との接続確認失敗";
                }
                allCheck &= isCheck;
                //debug*********************************
                this.LogDebug($"{device.identifier}→{response}");   //応答
                //*********************************debug
            }
            if (allCheck)
                message = "接続確認問題なし";
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Window? owner = Application.Current.MainWindow;
                MessageBox.Show(owner ?? Application.Current.MainWindow, message, "接続確認", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }
        //*************************************************
        //動作
        // FT2232H用通信確認
        // シリアルナンバーでポートオープン+BitBangモード
        //*************************************************
        private async Task<(string response, bool success)> CheckFT2232HConnection(string serialNumber)
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
                return ("シリアルナンバー未入力", false);

            bool success = await _bitBang.OpenBySerialNumberAsync(serialNumber);
            if (!success)
                return ("OpenBySerialNumber 失敗", false);
            _bitBang.Dispose();

            return ($"FT2232H [{serialNumber}] BitBangモード OK", true);
        }
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
                IsRunning = true;                               //処理の実行状態管理プロパティ経由で設定
                MeasurementStatus = "測定中...";
                var rows = new List<string>();                  //全データ保存用
                bool wasCanceled = false;                       //キャンセル時動作用
                _cts = new CancellationTokenSource();           //キャンセルトークン定義

                List<(bool IsChecked, string UsbId, string InstName, string Identifier)> measInstData = null;
               var debugOption = new DebugOption
                {
                    FinalFileFotterRemove = DebugSettings.DebugFinalFileFotterRemove,
                    Use8chOSC = DebugSettings.DebugUse8chOSC,
                    StopOnWarning = DebugSettings.DebugStopOnWarning,
                    EditThermoSoak = DebugSettings.DebugEditThermoSoak,
                    ThermoSoakTime = DebugSettings.DebugThermoSoakTime
               };
                //*********************
                //コールバック関数（キャンセル用
                //*********************
                async Task<bool> NoConfirmCallback() => true;
                try
                {
                    //*********************
                    //UIスレッドがブロックされるため（バックグラウンドからアクセスできない）
                    //測定器アドレスをコピー
                    //*********************
                    measInstData =
                        _measInst.Where(inst => inst.checkBox != null)                              //CheckBoxがnull(存在しない)以外の時
                        .Select(inst =>
                        {
                            string usbId = inst.textBox_ID?.Text ?? string.Empty;                   //TextBox_IDが入力されていなければ空白(明示)
                            string instName = inst.textBox_NAME?.Text ?? string.Empty;              //TextBox_NAMEが入力されていなければ空白(明示)
                            string identifier = inst.checkBox.Tag?.ToString() ?? "Unknown";
                            return (
                                    inst.checkBox.IsChecked ?? false,                               //CheckBoxがチェックされていなければfalse(明示)
                                    usbId,
                                    instName,
                                    identifier);
                        }).ToList();
                    //*********************
                    //測定器入力アドレスチェック
                    //*********************
                    (List<string> messageCheck, bool insAddrCheck) = await _errCheck.VerifyInsAddr(measInstData);
                    if (messageCheck.Any())
                        rows.Add($"# {string.Join(" ", messageCheck).Replace(Environment.NewLine, " ")}");
                    //*********************
                    //アドレス入力に不備があれば開始しない
                    //*********************
                    if (!insAddrCheck)
                        return;

                    if (AllCheckedTabNamesText == "対象なし")
                    {
                        rows.Add("# 測定対象が選択されていません");
                        return;
                    }
                    //*********************
                    //中間データ保存用連番カウンターリセット
                    //*********************
                    _utility.ResetTabFileCounters();
                    //*********************
                    //コールバック確認（電源RangeがAUTOの場合
                    //*********************
                    bool continueMeasurement = true;
                    bool needConfirm = false;
                    //*********************
                    //全タブ中、測定ONにチェックの入っているタブのプロパティを取得
                    //電源がAUTOの可能性があるかチェック
                    //*********************
                    List<string> _warnings = new List<string>();  //警告メッセージ用
                    if (SweepTab.Tabs.Any(t => t.MeasureOn))
                        _cachedSweepDevices = await GetDevicesAndCheckAutoInTab(measInstData, SweepTab, _sweepAct, "Sweep", _warnings);
                    if (DelayTab.Tabs.Any(t => t.MeasureOn))
                        _cachedDelayDevices = await GetDevicesAndCheckAutoInTab(measInstData, DelayTab, _delayAct, "Delay", _warnings);
                    if (VITab.Tabs.Any(t => t.MeasureOn))
                        _cachedVIDevices = await GetDevicesAndCheckAutoInTab(measInstData, VITab, _viAct, "VI", _warnings);
                    //*********************
                    //並列処理準備
                    //*********************
                    var tasks = new List<Task<List<Device>>>();           //並列処理Task
                    if (SweepTab.Tabs.Any(t => t.MeasureOn))
                        tasks.Add(GetDevicesAndCheckAutoInTab(measInstData, SweepTab, _sweepAct, "Sweep", _warnings));
                    if (DelayTab.Tabs.Any(t => t.MeasureOn))
                        tasks.Add(GetDevicesAndCheckAutoInTab(measInstData, DelayTab, _delayAct, "Delay", _warnings));
                    if (VITab.Tabs.Any(t => t.MeasureOn))
                        tasks.Add(GetDevicesAndCheckAutoInTab(measInstData, VITab, _viAct, "VI", _warnings));
                    //*********************
                    //並列処理実行＋結果をマッピング
                    //*********************
                    var results = await Task.WhenAll(tasks);
                    int i = 0;
                    if (SweepTab.Tabs.Any(t => t.MeasureOn))
                        _cachedSweepDevices = results[i++];
                    if (DelayTab.Tabs.Any(t => t.MeasureOn))
                        _cachedDelayDevices = results[i++];
                    if (VITab.Tabs.Any(t => t.MeasureOn))
                        _cachedVIDevices = results[i++];
                    //*********************
                    //測定開始前確認
                    //*********************
                    needConfirm = _warnings.Any();
                    if (needConfirm)
                    {
                        //UIスレッドでMessageBoxを表示するため、バックグラウンドスレッドから処理を委譲
                        continueMeasurement = await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            //オーナーウィンドウとしてApplication.Current.MainWindowを使用
                            Window? owner = Application.Current.MainWindow;
                            //MainWindowがnullの場合、デフォルトのMessageBoxを使用
                            var result = MessageBox.Show(
                                owner,
                                $"電源のRangeがAUTOになっているItemがあります。{Environment.NewLine}" +
                                string.Join(Environment.NewLine, _warnings) + $"{Environment.NewLine}" + $"{Environment.NewLine}" +
                                $"Sweep電源をAUTO設定のまま開始すると印加値に影響を及ぼす可能性があります。{Environment.NewLine}" +
                                $"このまま測定を開始しますか？",
                                "測定開始確認",
                                MessageBoxButton.OKCancel,
                                MessageBoxImage.Question);
                            return result == MessageBoxResult.OK;
                        });

                        if (!continueMeasurement)
                        {
                            rows.Add("# ユーザーが測定をキャンセルしました");
                            return;
                        }
                    }

                    //*********************
                    //温度を変化させる場合
                    //*********************
                    bool thermoSuccess = false;
                    List<string> logRows = new List<string>();
                    if (MultiTemperature)
                    {
                        //*********************
                        //サーモ初期化
                        //*********************
                        (thermoSuccess, logRows) = await _thermoAct.ThermoInitial(measInstData, _cts.Token, debugOption.ThermoSoakTime);
                        //#if DEBUG
                        //                        thermoSuccess = true;
                        //#endif
                        if (!thermoSuccess)
                            return;
                        //*********************
                        //温度変化ループ
                        //*********************
                        foreach (var targetTemp in Temperatures)
                        {
                            _cts.Token.ThrowIfCancellationRequested();  //キャンセルチェック
                            MeasurementStatus = $"サーモ 温度{targetTemp}℃ 設定+安定待ち...";
                            (thermoSuccess, logRows) = await _thermoAct.ThermoAction(measInstData, targetTemp, _cts.Token, NoConfirmCallback);
                            //#if DEBUG
                            //                            thermoSuccess = true;
                            //#endif
                            if (!thermoSuccess)
                            {
                                this.LogDebug($"サーモ 温度{targetTemp}℃ 設定失敗");
                                rows.Add(MeasurementStatus);
                                continue;
                            }
                            MeasurementStatus = $"サーモ 温度{targetTemp}℃ 設定完了";
                            //*********************
                            //リレー動作＋測定
                            //*********************
                            rows.Add($"サーモ 温度{targetTemp}℃");
                            await ExcuteMesurementRellayLoop(measInstData, rows, sampleCount, debugOption, _cts.Token, NoConfirmCallback);
                        }
                    }
                    //*********************
                    //温度変化なしの場合
                    //*********************
                    else
                    {
                        //*********************
                        //リレー動作＋測定
                        //*********************
                        await ExcuteMesurementRellayLoop(measInstData, rows, sampleCount, debugOption, _cts.Token, NoConfirmCallback);
                    }
                }
                catch (OperationCanceledException)
                {
                    //*********************
                    //キャンセル要求をキャッチしたら
                    //*********************
                    wasCanceled = true;
                    rows.Add("# 測定が中断 中間データ保存済");
                }
                catch (Exception ex)
                {
                    //その他の例外は従来通りエラー扱い
                    rows.Add($"# 測定エラー: {ex.Message}");
                    this.LogDebug($"例外: {ex.Message}");
                }
                finally
                {
                    //*********************
                    //全条件測定完了
                    //コメント行（#で始まる）を抽出して結果メッセージに追加
                    //LINQのWhereとSelectを使うため、IEnumerable<string> comments
                    //*********************
                    IEnumerable<string> comments = rows.Where(row => row.StartsWith("#")).Select(row => row.Substring(2).Trim());
                    string message = comments.Any()
                        ? string.Join(Environment.NewLine, comments)
                        : "測定は正常に終了";
                    //*********************
                    //最終データ追記用にフッター生成
                    //*********************
                    string footer = BuildSettingsFooter(measInstData);
                    if (debugOption.FinalFileFotterRemove)
                        footer = string.Empty;
                    //*********************
                    //コメント行（#で始まる）を除外して最終データにする
                    //*********************
                    var dataRows = rows.Where(r => !r.StartsWith("#")).ToList();
                    if (dataRows.Count > 1)
                    {
                        try
                        {
                            //*********************
                            //最終データ保存先
                            //*********************
                            string finalFilePath = _utility.GetFinalFilePath();
                            //*********************
                            //データ並び替え
                            //*********************
                            var pivotRows = CreatePivotRows(dataRows, MultiTemperature, MultiSample);
                            //*********************
                            //フッター追加
                            //*********************
                            pivotRows.AddRange(footer);
                            //*********************
                            //データ保存
                            //*********************
                            await _utility.WriteCsvFileAsync(finalFilePath, pivotRows, append: false, useShiftJis: true);
                            message += $"{Environment.NewLine}データが {finalFilePath} に保存されました。";
                        }
                        catch (Exception ex)
                        {
                            message += $"{Environment.NewLine}最終データ保存エラー: {ex.Message}";
                        }
                    }
                    else
                    {
                        message += $"{Environment.NewLine}測定データがありませんでした。";
                    }
                    //サーモがある場合25℃へ戻す（ただしキャンセル時はスキップ）
                    if (MultiTemperature && !wasCanceled)
                    {
                        MeasurementStatus = "終了処理サーモ温度25℃ 設定+安定待ち...";
                        try
                        {
                            await _thermoAct.SetThermoTo25C(measInstData, _cts.Token, NoConfirmCallback);
                            MeasurementStatus = "終了処理サーモ温度25℃ 設定完了";
                        }
                        catch (Exception)
                        {
                            MeasurementStatus = "終了処理サーモ温度25℃ 設定失敗";
                        }
                    }
                    else if (MultiTemperature && wasCanceled)
                    {
                        await _thermoAct.SetThermoFlowOff(measInstData, NoConfirmCallback);
                        MeasurementStatus = "測定がキャンセルされたため、温度安定待ち中断";
                    }
                    //*********************
                    //測定完了メッセージを表示して
                    //後処理
                    // 各親Tab有効
                    // 実行状態管理フラグ解除
                    // キャンセルトークンクリア
                    //*********************
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Window? owner = Application.Current.MainWindow;
                        MessageBox.Show(owner ?? Application.Current.MainWindow, message, "処理完了報告");
                    });
                    //*********************
                    //測定ステータス更新
                    //*********************
                    MeasurementStatus = "測定開始ボタン受付中";
                    //*********************
                    //各子Tab有効
                    //*********************
                    SweepTab.IsSmallTabCtrlEnabled = true;
                    DelayTab.IsSmallTabCtrlEnabled = true;
                    VITab.IsSmallTabCtrlEnabled = true;
                    //*********************
                    //各親Tab有効
                    //*********************
                    SweepTab.IsBigTabCtrlEnabled = true;
                    DelayTab.IsBigTabCtrlEnabled = true;
                    VITab.IsBigTabCtrlEnabled = true;
                    IsRunning = false;                  //処理の実行状態管理プロパティ経由で有効化
                    _cts.Dispose();
                    _cts = null;
                }
            }
            //*********************
            //処理を停止する
            //キャンセルトークン発生
            //*********************
            else
                _cts?.Cancel();
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
        private List<string> CreatePivotRows(List<string> sourceRows, bool multiTemp, bool multiSample)
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
            if (multiTemp && multiSample)
            {
                //複数温度＋複数ポート
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

            else if (multiTemp)
            {
                //複数温度のみ
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
            else if (multiSample)
            {
                //複数ポートのみ
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
            else
            {
                //単一測定 ピボット不要
                return sourceRows;
            }

            return result;
        }
        //****************************************************************************
        //動作
        // 各タブプロパティ値取得＋電源Autoレンジチェック
        //****************************************************************************
        private async Task<List<Device>> GetDevicesAndCheckAutoInTab<T>(
                                        List<(bool IsChecked, string UsbId, string InstName, string Identifier)> measInstData,
                                        object tabContainer,                            // SweepTab, DelayTab, VITab
                                        T act,                                          // _sweepAct, _delayAct, _viAct
                                        string tabType,                                 //"Sweep","Delay","VI"
                                        List<string> warningMessages) where T : IDeviceCombinable             
        {
            //GetMeasureOnTabDataメソッドを取得
            var getTabDataMethod = tabContainer.GetType().GetMethod(
                                                        "GetMeasureOnTabData", 
                                                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (getTabDataMethod == null)
                return new List<Device>();
            //GetMeasureOnTabDataメソッドを実行しプロパティ値を取得
            var getTabData = getTabDataMethod.Invoke(tabContainer, null);
            if (getTabData == null)
                return new List<Device>();

            //CombineDeviceDataで紐づけ
            var devices = act.CombineDeviceData(measInstData, getTabData);

            //devices からタブ名をすべて抽出
            var actualTabNames = devices
                .SelectMany(d => d.TabSettings.Keys)
                .Distinct()
                .ToArray();

            //各タブのSOURCEにAutoレンジが含まれていないかチェック
            var (messages, _) = await _errCheck.SourceRangeAuto(devices, actualTabNames, tabType);
            warningMessages.AddRange(messages);

            return devices;
        }

        //****************************************************************************
        //動作
        // リレー動作（複数個測定ループ、単体測定
        //****************************************************************************
        private async Task ExcuteMesurementRellayLoop(
                                                List<(bool, string, string, string)> measInstData, 
                                                List<string> rows,
                                                int? sampleCount,
                                                DebugOption option,
                                                CancellationToken cancellationToken = default,
                                                Func<Task<bool>> confirmCallback = null)
        {
            //*********************
            //複数個測定
            //*********************
            if (MultiSample) 
            {
                //*********************
                //リレーループ
                //*********************
                for (int port = 1; port <= sampleCount; port++)
                {
                    _cts.Token.ThrowIfCancellationRequested();  //キャンセルチェック
                    //*********************
                    //リレーON
                    //*********************
                    bool relaySuccess = await SetRelayPortOnAsync(port);
//#if DEBUG
//                    relaySuccess = true;
//#endif
                    if (!relaySuccess)
                    {
                        rows.Add($"# リレー Sample {port} ON 失敗");
                        continue; //次のポートへ
                    }
                    //*********************
                    //各Tab測定
                    //*********************
                    MeasurementStatus = $"Sample {port} 測定中...";
                    rows.Add($"Sample{port}");
                    await MeasurementTabs(measInstData, rows, option, cancellationToken, confirmCallback);
                    //*********************
                    //ONしたリレーをOFF
                    //*********************
                    await SetRelayPortOffAsync(port);
                }
            }
            //*********************
            //単体測定
            //*********************
            else
            {
                //*********************
                //各Tab測定
                //*********************
                MeasurementStatus = "単体測定中...";
                rows.Add("単体測定");
                await MeasurementTabs(measInstData, rows, option, cancellationToken, confirmCallback);
            }
        }
        //****************************************************************************
        //動作
        // 各Tab測定
        //****************************************************************************
        private async Task MeasurementTabs(
                                            List<(bool, string, string, string)> measInstData, 
                                            List<string> rows,
                                            DebugOption option,
                                            CancellationToken cancellationToken = default,
                                            Func<Task<bool>> confirmCallback = null)
        {
            //*********************
            //Sweep測定
            //*********************
            if (SweepTab.Tabs.Any(t => t.MeasureOn))
            {
                //親Tabは有効のまま
                //子タブは無効
                SweepTab.IsBigTabCtrlEnabled = true;
                SweepTab.IsSmallTabCtrlEnabled = false;
                MeasurementStatus += "Sweep測定中...";
                var sweepRows = await _sweepAct.SWEEPAction(measInstData, option, cancellationToken, confirmCallback, _cachedSweepDevices);
                rows.AddRange(sweepRows);
            }
            //*********************
            //Delay測定
            //*********************
            if (DelayTab.Tabs.Any(t => t.MeasureOn))
            {
                //親Tabは有効のまま
                //子タブは無効
                DelayTab.IsBigTabCtrlEnabled = true;
                DelayTab.IsSmallTabCtrlEnabled = false;
                MeasurementStatus += "Delay測定中...";
                var delayRows = await _delayAct.DELAYAction(measInstData, option, cancellationToken, confirmCallback, _cachedDelayDevices);
                rows.AddRange(delayRows);
            }
            //*********************
            //VI測定
            //*********************
            if (VITab.Tabs.Any(t => t.MeasureOn))
            {
                //親Tabは有効のまま
                //子タブは無効
                VITab.IsBigTabCtrlEnabled = true;
                VITab.IsSmallTabCtrlEnabled = false;
                MeasurementStatus += "VI測定中...";
                var viRows = await _viAct.VIAction(measInstData, option, cancellationToken, confirmCallback, _cachedVIDevices);
                rows.AddRange(viRows);
            }
        }
        //****************************************************************************
        //動作
        // 最終データ追記用フッター生成
        //****************************************************************************
        private string BuildSettingsFooter(List<(bool IsChecked, string UsbId, string InstName, string Identifier)> measInst)
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
            if (SweepTab.Tabs.Any(t => t.MeasureOn))
            {
                var sweepTabs = SweepTab.Tabs.Where(t => t.MeasureOn).ToArray();
                var json = JsonSerializer.Serialize(sweepTabs, options);
                sections.Add($"--- Sweep Settings ---{Environment.NewLine}{json}");
            }
            //*********************
            //Delayタブ
            //*********************
            if (DelayTab.Tabs.Any(t => t.MeasureOn))
            {
                var delayTabs = DelayTab.Tabs.Where(t => t.MeasureOn).ToArray();
                var json = JsonSerializer.Serialize(delayTabs, options);
                sections.Add($"--- Delay Settings ---{Environment.NewLine}{json}");
            }
            //*********************
            //VIタブ
            //*********************
            if (VITab.Tabs.Any(t => t.MeasureOn))
            {
                var viTabs = VITab.Tabs.Where(t => t.MeasureOn).ToArray();
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
        //*************************************************
        //プロパティが変更された場合にUIを更新する
        //*************************************************
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        //****************************************************************************
        //動作
        // 未実装通知
        //****************************************************************************
        private void Not_yet(object sender, RoutedEventArgs e)
        {
            string message = "未実装です";
            MessageBox.Show(message);
        }
        //**************************************************************************************************************************
        //debug用↓
        private async Task DebugSend()
        {
            //*********************
            //USBIDとCommandを取得
            //*********************
            string usbid = DebugUSBID;
            string cmd = DebugSendCmd;

            DebugLog += $"{cmd} \n";   //送信cmdをlogに追記
            await _commSend.Comm_send(usbid, cmd);
        }
        private async Task DebugQuery()
        {
            //*********************
            //USBIDとCommandを取得
            //*********************
            string usbid = DebugUSBID;
            string cmd = DebugSendCmd;
            DebugLog += $"{cmd} \n";   //送信cmdをlogに追記
            string res = await _commQuery.Comm_query(usbid, cmd);
            DebugLog += $"{res} \n";   //応答をlogに追記
        }
        private void LogDebug(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                //デバッグ窓表示上限取得
                var debugOption = new DebugOption
                {
                    MaxLogLines = DebugSettings.DebugMaxLogLines
                };
                //新しいログを追加
                DebugTextBox += $"{DateTime.Now:HH:mm:ss}\n {message}{Environment.NewLine}";

                //行数が上限を超えたら古い行を削除
                var lines = DebugTextBox.Split(
                    new[] { Environment.NewLine },
                    StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length > debugOption.MaxLogLines)
                {
                    //最新 MaxLogLines 行だけ残す
                    DebugTextBox = string.Join(Environment.NewLine,
                        lines.Skip(lines.Length - debugOption.MaxLogLines));
                }
            });
        }
        //debug用↑
        //**************************************************************************************************************************
    }
    public class InstrumentViewModel : INotifyPropertyChanged
    {
        private bool _isChecked;
        private string? _usbId;
        private string? _instName;
        private string? _identifier;
        private string? _tag;

        public bool IsChecked { get => _isChecked; set { _isChecked = value; OnPropertyChanged(); } }
        public string? UsbId { get => _usbId; set { _usbId = value; OnPropertyChanged(); } }
        public string? InstName { get => _instName; set { _instName = value; OnPropertyChanged(); } }
        public string? Identifier { get => _identifier; set { _identifier = value; OnPropertyChanged(); } }
        public string? Tag { get => _tag; set { _tag = value; OnPropertyChanged(); } }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
            bool canExecute = _canExecute?.Invoke() ?? true;
            return canExecute;
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