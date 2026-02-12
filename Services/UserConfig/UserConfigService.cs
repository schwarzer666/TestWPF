using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using TemperatureCharacteristics.Models;
using TemperatureCharacteristics.Services.Data;
using TemperatureCharacteristics.ViewModels.Tabs;

namespace TemperatureCharacteristics.Services.UserConfig
{
    public class UserConfigService : IUserConfigService
    {
        private readonly IJsonDataService _dataService;
        private readonly PresetManager _presetManager;
        //private readonly string _defaultFolder;
        private readonly Action<string> _log;
        //*************************************************
        //SaveAllConfigコマンド用ファイル名
        //*************************************************
        public string SweepAllFileName => "SweepSettings_All.json";
        public string DelayAllFileName => "DelaySettings_All.json";
        public string VIAllFileName => "VISettings_All.json";
        //*************************************************
        //Load時Tab差し替え動作用
        //*************************************************
        public Action<string, ITabItemViewModel>? ReplaceTabCallback { get; set; }
        //*************************************************
        //UserConfigServiceがCurrentTabViewModelを参照できるように注入
        //*************************************************
        public ITabItemViewModel? CurrentTabViewModel { get; set; }
        //*************************************************
        //初期フォルダ参照用
        //*************************************************
        public string DefaultFolder { get; set; }
        //*************************************************
        //タブ単体からタブグループに変換
        //*************************************************
        public Func<ITabItemViewModel, ITabGroupViewModel?>? ResolveGroupFromTab { get; set; }

        public UserConfigService(
                                IJsonDataService dataService,
                                PresetManager presetManager,
                                Action<string> log)
        {
            _dataService = dataService;
            _presetManager = presetManager;
            //DefaultFolder = defaultFolder;
            _log = log;
        }
        //****************************************************************************
        //動作
        // ユーザー設定保存
        //****************************************************************************
        public async Task SaveAsync(ITabItemViewModel tab)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                //プリセット生成
                var preset = _presetManager.CreatePresetFromTab(tab, null);
                if (preset == null)
                    return;

                //ファイル名を ItemHeader から生成
                string safeName = MakeSafeFileName(tab.ItemHeader);
                string fileName = $"{safeName}.json";

                //保存用コレクションを作成
                var list = new ObservableCollection<PresetItemBase> { preset };

                //保存
                var (success, log, path) = _dataService.SaveItems(DefaultFolder, fileName, list);
                _log(log);
            });
        }
        //****************************************************************************
        //動作
        // 全ユーザー設定保存
        //****************************************************************************
        public async Task SaveAllAsync(ITabGroupViewModel tabGroup, string fileName)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                //保存するプリセットのリスト
                var list = new ObservableCollection<PresetItemBase>();

                foreach (var tab in tabGroup.Tabs)
                {
                    var preset = _presetManager.CreatePresetFromTab(tab, null);
                    if (preset != null)
                        list.Add(preset);
                }

                var (success, log, path) = _dataService.SaveItems(DefaultFolder, fileName, list);
                _log(log);
            });
        }
        //****************************************************************************
        //動作
        // ユーザー設定読み込み
        //****************************************************************************
        public async Task LoadAsync(ITabItemViewModel tab, Action<string, ITabItemViewModel> replaceTab)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                //読み込み実行
                var (configs, log, path) = _dataService.LoadItems<PresetItemBase>(DefaultFolder, null);
                _log(log);

                if (configs == null || !configs.Any())
                    return;

                foreach (var preset in configs)
                {
                    //新しいTabViewModelを生成
                    var newTab = _presetManager.CreateTabFromPreset(preset);
                    if (newTab == null)
                        continue;
                    //生成したTabを該当Tabと差し替え（差し替えはMainViewModelで実行）
                    replaceTab(tab.TabId, newTab);
                }
            });
        }
        //****************************************************************************
        //動作
        // 全ユーザー設定読み込み
        //****************************************************************************
        public async Task<(bool success, string? errorMessage)> LoadAllAsync(ITabGroupViewModel tabGroup, Action<string, ITabItemViewModel> replaceTab)
        {
            //読み込み実行
            var (configs, log, path) = _dataService.LoadItems<PresetItemBase>(DefaultFolder, null);
            _log(log);

            if (configs == null || !configs.Any())
                return (false, "プリセットが読み込めませんでした。");
            //列挙中に変更されるのを防ぐためtabsをコピー
            var tabsCopy = tabGroup.Tabs.ToList();

            //読み込んだ設定（presets）をリスト化
            var presets = configs.ToList();

            //読み込んだ設定（presets） > タブ数 → エラー（将来は読込選択UIに置き換える）
            if (presets.Count > tabsCopy.Count)
                return (false, $"設定数（{presets.Count}）がタブ数（{tabsCopy.Count}）を超えています。");

            //読み込んだ設定（presets） < タブ数 → プリセット数だけ上書き
            int count = presets.Count;

            //差し替え内容を一時的に保持するリスト
            var replaceList = new List<(string id, ITabItemViewModel newTab)>();

            //バックグラウンドで新しいタブを生成
            for (int i = 0; i < count; i++)
            {
                var preset = presets[i];
                var tab = tabsCopy[i];

                //新しいTabViewModelを生成
                var newTab = _presetManager.CreateTabFromPreset(preset);
                if (newTab != null)
                    replaceList.Add((tab.TabId, newTab));
            }
            // UIスレッドで一括差し替え
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var (id, newTab) in replaceList)
                    replaceTab(id, newTab);
            });
            return (true, null);
        }
        //****************************************************************************
        //動作
        // プリセット内容を適用
        //****************************************************************************
        public async Task ApplyPresetAsync(PresetItemBase preset)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (preset == null)
                    return;

                // 現在のタブを取得
                if (CurrentTabViewModel is not ITabItemViewModel currentTab)
                    return;

                string tabId = currentTab.TabId;

                // 新しいタブを生成
                var newTab = _presetManager.CreateTabFromPreset(preset);
                if (newTab == null)
                    return;

                // MainViewModel に差し替えを依頼
                ReplaceTabCallback?.Invoke(tabId, newTab);
            });
        }
        //****************************************************************************
        //動作
        // ファイル名から使用不可文字を除去
        //****************************************************************************
        private string MakeSafeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}
