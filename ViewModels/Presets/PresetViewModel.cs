using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using TemperatureCharacteristics.Models;
using TemperatureCharacteristics.Services.Data;
using TemperatureCharacteristics.Services.Dialog;
using TemperatureCharacteristics.Services.UserConfig;
using TemperatureCharacteristics.ViewModels.Debug;
using TemperatureCharacteristics.ViewModels.Tabs;
using TemperatureCharacteristics.ViewModels.Tabs.Delay;
using TemperatureCharacteristics.ViewModels.Tabs.Sweep;
using TemperatureCharacteristics.ViewModels.Tabs.VI;

namespace TemperatureCharacteristics.ViewModels.Presets
{
    public class PresetViewModel : BaseViewModel
    {
        private readonly IUserConfigService _userConfigService;
        private readonly Func<bool> _isRunningProvider;
        private ITabItemViewModel? _currentTab;
        private readonly IJsonDataService _dataService;
        private ObservableCollection<PresetItemBase> _filteredPresetButtons;    //フィルタ適用後のプリセットボタン
        private readonly IDialogService _dialogService;
        //**********************************
        // Popup 状態
        //**********************************
        private bool _isPresetPopupOpen;
        public bool IsPresetPopupOpen { get => _isPresetPopupOpen; set => SetProperty(ref _isPresetPopupOpen, value); }
        private bool _isSavePopupOpen;
        public bool IsSavePopupOpen { get => _isSavePopupOpen; set => SetProperty(ref _isSavePopupOpen, value); }
        private bool _isLoadPopupOpen;
        public bool IsLoadPopupOpen { get => _isLoadPopupOpen; set => SetProperty(ref _isLoadPopupOpen, value); }
        //**********************************
        // プリセットボタン
        //**********************************
        public ObservableCollection<PresetItemBase> PresetButtons { get; private set; }
        public ObservableCollection<PresetItemBase> FilteredPresetButtons { get => _filteredPresetButtons; private set => SetProperty(ref _filteredPresetButtons, value); }
        public ObservableCollection<PresetItemBase> UserConfigs { get; private set; }
        //**********************************
        // コマンド
        //**********************************
        public ICommand OpenPresetPopupCommand { get; }
        public ICommand SelectPresetCommand { get; }
        public ICommand OpenSavePopupCommand { get; }
        public ICommand OpenLoadPopupCommand { get; }
        public ICommand SaveUserConfigsCommand { get; }
        public ICommand SaveAllUserConfigsCommand { get; }
        public ICommand LoadUserConfigsCommand { get; }
        public ICommand LoadAllUserConfigsCommand { get; }
        //*************************************************
        //CurrentTabViewModelを参照
        //*************************************************
        public ITabItemViewModel? CurrentTab { get => _currentTab; set => SetProperty(ref _currentTab, value); }
        //**********************************
        // コンストラクタ
        //**********************************
        public PresetViewModel(
            IUserConfigService userConfigService, 
            IJsonDataService dataService, 
            DebugViewModel debugVM, 
            Func<bool> isRunningProvider,
            IDialogService dialogService)
        {
            _userConfigService = userConfigService;
            _isRunningProvider = isRunningProvider;
            _userConfigService.DefaultFolder = debugVM.SaveLoadDefaultFolder;
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _dialogService = dialogService;
            PresetButtons = new ObservableCollection<PresetItemBase>();
            FilteredPresetButtons = new ObservableCollection<PresetItemBase>();
            UserConfigs = new ObservableCollection<PresetItemBase>();
            //**********************************
            //JSONファイルからプリセットを読み込み
            //**********************************
            try
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Data", "presets.json");

                var (presets, presetLog, selectedPath) = _dataService.LoadItems<PresetItemBase>(null, filePath);
                debugVM.LogDebug(presetLog);

                PresetButtons = presets;
                FilteredPresetButtons = new ObservableCollection<PresetItemBase>(presets);
            }
            catch (Exception ex)
            {
                debugVM.LogDebug($"Preset load failed: {ex.Message}");
                PresetButtons = new ObservableCollection<PresetItemBase>();
                FilteredPresetButtons = new ObservableCollection<PresetItemBase>();
            }
            //**********************************
            //ポップアップオープンコマンド（Preset）
            //**********************************
            OpenPresetPopupCommand = new RelayCommandAsync(
                async param =>
                {
                    if (param is ITabItemViewModel tab)
                        OpenPresetPopup(tab);
                },
                () => !_isRunningProvider()
            );
            //**********************************
            //Preset選択コマンド
            // 適用はUserConfigService
            //**********************************
            SelectPresetCommand = new RelayCommandAsync(
                async param =>
                {
                    if (param is PresetItemBase preset)
                    {
                        await _userConfigService.ApplyPresetAsync(preset);
                        IsPresetPopupOpen = false;
                    }
                },
                () => IsPresetPopupOpen
            );
            //**********************************
            //ポップアップオープンコマンド（Save）
            //**********************************
            OpenSavePopupCommand = new RelayCommandAsync(
                async param =>
                {
                    if (param is ITabItemViewModel tab)
                        OpenSavePopup(tab);
                },
                () => !_isRunningProvider()
            );
            //**********************************
            //ポップアップオープンコマンド（Load）
            //**********************************
            OpenLoadPopupCommand = new RelayCommandAsync(
                async param =>
                {
                    if (param is ITabItemViewModel tab)
                        OpenLoadPopup(tab);
                },
                () => !_isRunningProvider()
            );
            //**********************************
            //タブ単体保存コマンド
            //　動作実態はUserConfigService
            //**********************************
            SaveUserConfigsCommand = new RelayCommandAsync(
                async param =>
                {
                    IsSavePopupOpen = false;
                    if (param is not ITabItemViewModel tab)
                        return;
                    await _userConfigService.SaveAsync(tab);
                },
                () => !_isRunningProvider()
            );
            //**********************************
            //全タブ保存コマンド
            //　動作実態はUserConfigService
            //**********************************
            SaveAllUserConfigsCommand = new RelayCommandAsync(
                async param =>
                {
                    IsSavePopupOpen = false;
                    if (param is not ITabItemViewModel tab)
                        return;
                    //タブ単体 → タブグループへ変換
                    //変換してnullなら不正なので抜ける
                    var group = _userConfigService.ResolveGroupFromTab?.Invoke(tab);
                    if (group == null)
                        return;

                    string fileName = group switch
                    {
                        SweepTabGroupViewModel => _userConfigService.SweepAllFileName,
                        DelayTabGroupViewModel => _userConfigService.DelayAllFileName,
                        VITabGroupViewModel => _userConfigService.VIAllFileName,
                        _ => null
                    };
                    if (fileName != null)
                        await _userConfigService.SaveAllAsync(group, fileName);
                },
                () => !_isRunningProvider()
            );
            //**********************************
            //タブ単体読込コマンド
            //　動作実態はUserConfigServiceで実行
            //　タブ差し替えのみMainViewModelで実行(UserConfigService.ReplaceTabCallback)
            //**********************************
            LoadUserConfigsCommand = new RelayCommandAsync(
                async param =>
                {
                    IsLoadPopupOpen = false;
                    if (param is not ITabItemViewModel tab)
                        return;
                    //タブ単体 → タブグループへ変換
                    //変換してnullなら不正なので抜ける
                    var group = _userConfigService.ResolveGroupFromTab?.Invoke(tab);
                    if (group == null)
                        return;
                    //タブ単体読込
                    await _userConfigService.LoadAsync(tab, _userConfigService.ReplaceTabCallback);
                },
                () => !_isRunningProvider()
            );
            //**********************************
            //全タブ読込コマンド
            //　動作実態はUserConfigServiceで実行
            //　タブ差し替えのみMainViewModelで実行(UserConfigService.ReplaceTabCallback)
            //**********************************
            LoadAllUserConfigsCommand = new RelayCommandAsync(
                async param =>
                {
                    IsLoadPopupOpen = false;
                    if (param is not ITabItemViewModel tab)
                        return;
                    //タブ単体 → タブグループへ変換
                    //変換してnullなら不正なので抜ける
                    var group = _userConfigService.ResolveGroupFromTab?.Invoke(tab);
                    if (group == null)
                        return;
                    //タブグループ読込
                    var (success, error) = await _userConfigService.LoadAllAsync(group, _userConfigService.ReplaceTabCallback);

                    if (!success)
                    {
                        _dialogService.ShowError(error, "読み込みエラー");
                    }
                },
                () => !_isRunningProvider()
            );
            //****************************************************************************
            //動作
            // DebugViewModelのプロパティ変更を監視
            // 初期フォルダから変更が発生したら更新
            //****************************************************************************
            debugVM.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(debugVM.SaveLoadDefaultFolder))
                    _userConfigService.DefaultFolder = debugVM.SaveLoadDefaultFolder;
            };
        }
        //****************************************************************************
        //動作
        // ポップアップオープン（Preset,Save,Load）
        //****************************************************************************
        private void OpenPresetPopup(ITabItemViewModel tab)
        {
            CurrentTab = tab;                               //PresetViewModel内で使うCurrentTabViewModel
            _userConfigService.CurrentTabViewModel = tab;   //ポップアップがオープンしたときの親タブを保存しUserConfigServiceに渡す
            //Popup表示時フィルタ適用
            FilterPresetsForTab(tab);

            IsSavePopupOpen = false;
            IsLoadPopupOpen = false;
            IsPresetPopupOpen = !IsPresetPopupOpen;
        }
        private void OpenSavePopup(ITabItemViewModel tab)
        {
            CurrentTab = tab;                               //PresetViewModel内で使うCurrentTabViewModel
            _userConfigService.CurrentTabViewModel = tab;   //ポップアップがオープンしたときの親タブを保存しUserConfigServiceに渡す
            IsPresetPopupOpen = false;
            IsLoadPopupOpen = false;
            IsSavePopupOpen = !IsSavePopupOpen;
        }
        private void OpenLoadPopup(ITabItemViewModel tab)
        {
            CurrentTab = tab;                               //PresetViewModel内で使うCurrentTabViewModel
            _userConfigService.CurrentTabViewModel = tab;   //ポップアップがオープンしたときの親タブを保存しUserConfigServiceに渡す
            IsPresetPopupOpen = false;
            IsSavePopupOpen = false;
            IsLoadPopupOpen = !IsLoadPopupOpen;
        }
        //****************************************************************************
        //動作
        // プリセットボタン表示のフィルタ
        // JSONファイル読み込み後、各Tabに表示するためフィルタをかける
        //****************************************************************************
        private void FilterPresetsForTab(ITabItemViewModel tab)
        {
            string? targetType = tab switch
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
    }
}

