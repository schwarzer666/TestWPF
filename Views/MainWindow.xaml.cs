using InputCheck;
using System.Windows;
using System.Windows.Controls;
using TemperatureCharacteristics.Services.Actions;
using TemperatureCharacteristics.Services.Data;
using TemperatureCharacteristics.Services.Devices;
using TemperatureCharacteristics.Services.Relay;
using TemperatureCharacteristics.Services.UserConfig;
using TemperatureCharacteristics.ViewModels;
using TemperatureCharacteristics.ViewModels.Debug;
using TemperatureCharacteristics.ViewModels.Devices;
using TemperatureCharacteristics.ViewModels.Presets;
using TemperatureCharacteristics.ViewModels.Tabs.Delay;
using TemperatureCharacteristics.ViewModels.Tabs.Sweep;
using TemperatureCharacteristics.ViewModels.Tabs.VI;
using UTility;

namespace TemperatureCharacteristics.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            //************************************
            //Serviceの初期化
            //************************************
            var debug = new DebugViewModel();
            var dialogService = new DialogService();
            var dataService = new DataService();
            var instrumentService = new InstrumentService(debug);
            //************************************
            //Logicの初期化
            //************************************
            var sweepAct = Sweep.Instance;
            var delayAct = Delay.Instance;
            var viAct = VI.Instance;
            var thermoAct = Thermo.Instance;
            var errCheck = InpCheck.Instance;
            var utility = UT.Instance;
            //************************************
            //プリセット、UserConfigの初期化
            //************************************
            var presetManager = new PresetManager(debug.LogDebug);
            var userConfigService = new UserConfigService(dataService, presetManager, debug.LogDebug);
            //************************************
            //ViewModelの初期化
            //************************************
            var relayService = new RelayService();
            var relayVM = new RelayViewModel(relayService, debug.LogDebug);
            var resources = new ResourceViewModel();
            var sweepTabs = new SweepTabGroupViewModel(userConfigService, resources);
            var delayTabs = new DelayTabGroupViewModel(userConfigService, resources);
            var viTabs = new VITabGroupViewModel(userConfigService, resources);
            var presetVM = new PresetViewModel(userConfigService, dataService, debug, () => _viewModel?.IsRunning ?? false, dialogService);
            //************************************
            //TabFactoryの初期化
            //************************************
            var tabFactory = new TabFactory(userConfigService, resources);
            presetManager.TabFactory = tabFactory;
            //************************************
            //MainViewModelの初期化
            //************************************
            _viewModel = new MainViewModel(
                                        instrumentService,
                                        dataService,
                                        debug,
                                        dialogService,
                                        relayVM,
                                        sweepTabs,
                                        delayTabs,
                                        viTabs,
                                        presetVM,
                                        userConfigService,
                                        presetManager,
                                        tabFactory,
                                        sweepAct,
                                        delayAct,
                                        viAct,
                                        thermoAct,
                                        errCheck,
                                        utility,
                                        resources);

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
