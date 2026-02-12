using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using TemperatureCharacteristics.Models.TabData;
using TemperatureCharacteristics.Services.UserConfig;

namespace TemperatureCharacteristics.ViewModels.Tabs.VI
{
    //*************************************************
    //VIタブの集合を管理するGroupViewModel
    //*************************************************
    public class VITabGroupViewModel : BaseViewModel, ITabGroupViewModel
    {
        //*************************************************
        //XAMLとViewModelの中継
        //*************************************************
        public ObservableCollection<ITabItemViewModel> Tabs { get; }
        //*************************************************
        //ロジック層向けの読み取り専用ビュー
        //*************************************************
        IReadOnlyList<ITabItemViewModel> ITabGroupViewModel.Tabs => Tabs;
        //*************************************************
        //xamlのComboItemの為ResouceViewModel（ComboItemMaster）注入
        //GroupTabViewModel内では未使用→拡張用
        //*************************************************
        public ResourceViewModel Resources { get; }
        //*************************************************
        //測定対象タブの状態管理
        //*************************************************
        public ObservableCollection<(string Id, string ItemHeader)> CheckedVITabNames { get; }
        public string[] CheckedTabNames =>
          CheckedVITabNames.Any()
              ? CheckedVITabNames.Select(x => x.ItemHeader).ToArray()
              : Array.Empty<string>();
        //*************************************************
        //Item名変更通知プロパティ
        //*************************************************
        private string _checkedTabNamesText = "対象なし";
        public string CheckedTabNamesText { get => _checkedTabNamesText; private set => SetProperty(ref _checkedTabNamesText, value); }
        //*************************************************
        //選択中のタブを再選択
        //*************************************************
        private int _selectedIndex;
        public int SelectedIndex { get => _selectedIndex; set => SetProperty(ref _selectedIndex, value); }
        //*************************************************
        //コンストラクタ
        //*************************************************
        public VITabGroupViewModel(IUserConfigService configService, ResourceViewModel resources)
        {
            Resources = resources;  //GroupTabViewModel内では未使用→拡張用
            Tabs = new ObservableCollection<ITabItemViewModel>();
            CheckedVITabNames = new ObservableCollection<(string Id, string ItemHeader)>();
            //CheckedVITabNames が変わったら文字列を更新
            CheckedVITabNames.CollectionChanged += (s, e) => UpdateCheckedTabNamesText();
            //Tabs の追加・削除を監視
            Tabs.CollectionChanged += Tabs_CollectionChanged;
            //デザイナー用または初期データ
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                for (int i = 1; i <= 3; i++)
                {
                    //デザイナー用のダミーデータ
                    Tabs.Add(new VITabViewModel(configService, resources)
                    {
                        ItemHeader = $"LongLongNameItemHedder{i}",
                        MeasureOn = false,
                        VIConfig =
                        {
                            StanbyValue = "Stanby",
                            StanbyUnitIndex = 0
                        },
                        SourceConfig =
                        {
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
                            SourceLimit1 = "100",
                            SourceLimit2 = "100",
                            SourceLimit3 = "100",
                            SourceLimit4 = "100",
                            SourceLimit1Index = 0,
                            SourceLimit2Index = 0,
                            SourceLimit3Index = 0,
                            SourceLimit4Index = 0
                        },
                        ConstConfig =
                        {
                            Const1 = "Const",
                            Const2 = "Const",
                            Const3 = "Const",
                            Const4 = "Const",
                            Const1UnitIndex = 0,
                            Const2UnitIndex = 0,
                            Const3UnitIndex = 0,
                            Const4UnitIndex = 0
                        },
                        DetectReleaseConfig =
                        {
                            DetectReleaseA = "det/rel",
                            DetectReleaseB = "det/rel",
                            CheckTime = "Time",
                            DetectReleaseIndex = 1,
                            DetectSourceAIndex = 1,
                            DetectSourceBIndex = 5,
                            DetectUnitAIndex = 0,
                            DetectUnitBIndex = 0,
                            CheckTimeUnitIndex = 0
                        },
                        DmmConfig =
                        {
                            Meas1plc = "NPLC",
                            Meas2plc = "NPLC",
                            Meas3plc = "NPLC",
                            Meas4plc = "NPLC",
                            MeasMode1Index = 0,
                            MeasMode2Index = 0,
                            MeasMode3Index = 0,
                            MeasMode4Index = 0,
                            MeasRang1Index = 0,
                            MeasRang2Index = 0,
                            MeasRang3Index = 0,
                            MeasRang4Index = 0,
                            MeasTrigIndex = 1,
                            Meas1DispOn = true,
                            Meas2DispOn = true,
                            Meas3DispOn = true,
                            Meas4DispOn = true
                        }
                    });
                }
            }
            else
            {
                //実行時用のデータ
                for (int i = 1; i <= 6; i++)
                {
                    VITabViewModel tab = new VITabViewModel(configService, resources)
                    {
                        ItemHeader = $"Item{i}",
                        MeasureOn = false,
                        VIConfig =
                        {
                            StanbyValue = "0",
                            StanbyUnitIndex = 0
                        },
                        SourceConfig =
                        {
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
                            SourceLimit1 = null,
                            SourceLimit2 = null,
                            SourceLimit3 = null,
                            SourceLimit4 = null,
                            SourceLimit1Index = 0,
                            SourceLimit2Index = 0,
                            SourceLimit3Index = 0,
                            SourceLimit4Index = 0
                        },
                        ConstConfig =
                        {
                            Const1 = "0",
                            Const2 = "0",
                            Const3 = "0",
                            Const4 = "0",
                            Const1UnitIndex = 0,
                            Const2UnitIndex = 0,
                            Const3UnitIndex = 0,
                            Const4UnitIndex = 0
                        },
                        DetectReleaseConfig =
                        {
                            DetectReleaseA = "0.0",
                            DetectReleaseB = "0.0",
                            CheckTime = "1",
                            DetectSourceAIndex = 1,
                            DetectSourceBIndex = 5,
                            DetectUnitAIndex = 0,
                            DetectUnitBIndex = 0,
                            CheckTimeUnitIndex = 0,
                            DetectReleaseIndex = 1
                        },
                        DmmConfig =
                        {
                            Meas1plc = "10",
                            Meas2plc = "10",
                            Meas3plc = "10",
                            Meas4plc = "10",
                            MeasMode1Index = 0,
                            MeasMode2Index = 0,
                            MeasMode3Index = 0,
                            MeasMode4Index = 0,
                            MeasRang1Index = 0,
                            MeasRang2Index = 0,
                            MeasRang3Index = 0,
                            MeasRang4Index = 0,
                            MeasTrigIndex = 1,
                            Meas1DispOn = true,
                            Meas2DispOn = true,
                            Meas3DispOn = true,
                            Meas4DispOn = true
                        },
                        TabId = Guid.NewGuid().ToString()      //識別用ID自動生成
                    };
                    Tabs.Add(tab);
                }
                UpdateCheckedTabNamesText();            //初期状態のTextBlock用文字列を設定
            }
        }
        //*************************************************
        //機能：測定対象タブのデータ取得
        //　　　MeasureOn checkboxにcheckが入っているタブが対象
        //*************************************************
        public IEnumerable<VITabData> GetMeasureOnTabData()
        {
            return Tabs
                .Where(tab => tab.MeasureOn)
                .Select(tab => ((VITabViewModel)tab).ToVITabData());
        }
        //*************************************************
        //Tabs にタブが追加・削除されたときの処理
        //*************************************************
        private void Tabs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (VITabViewModel tab in e.NewItems)
                {
                    // MeasureOn / ItemHeader の変更を監視
                    tab.PropertyChanged += Tab_PropertyChanged;

                    // 初期状態で MeasureOn が true のタブは CheckedVITabNames に追加
                    if (tab.MeasureOn && !CheckedVITabNames.Any(x => x.Id == tab.TabId))
                        CheckedVITabNames.Add((tab.TabId, tab.ItemHeader));
                }
            }

            if (e.OldItems != null)
            {
                foreach (VITabViewModel tab in e.OldItems)
                {
                    tab.PropertyChanged -= Tab_PropertyChanged;

                    // 削除されたタブを CheckedVITabNames から除外
                    var item = CheckedVITabNames.FirstOrDefault(x => x.Id == tab.TabId);
                    if (item != default)
                        CheckedVITabNames.Remove(item);
                }
            }
        }
        //*************************************************
        //タブの MeasureOn / ItemHeader の変更を監視
        //*************************************************
        private void Tab_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not VITabViewModel tab) return;
            //MeasureOnプロパティに変更があった場合
            if (e.PropertyName == nameof(VITabViewModel.MeasureOn))
            {
                //MeasureOnがチェックされた場合
                if (tab.MeasureOn)
                {
                    if (!CheckedVITabNames.Any(x => x.Id == tab.TabId))
                        CheckedVITabNames.Add((tab.TabId, tab.ItemHeader));
                }
                //MeasureOnのチェックが外れた場合
                else
                {
                    var item = CheckedVITabNames.FirstOrDefault(x => x.Id == tab.TabId);
                    if (item != default)
                        CheckedVITabNames.Remove(item);
                }
            }
            //ItemHeaderプロパティに変更があった場合
            else if (e.PropertyName == nameof(VITabViewModel.ItemHeader))
            {
                var match = CheckedVITabNames
                                    .Select((x, i) => (x, i))
                                    .FirstOrDefault(x => x.x.Id == tab.TabId);

                if (match.x != default)
                    CheckedVITabNames[match.i] = (tab.TabId, tab.ItemHeader);
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
        //動作
        // LoadUserConfig時、Tabを差し替え
        // プロパティの内容を変更してもUIが更新されないため
        //*************************************************
        public void ReplaceTab(string tabId, VITabViewModel newTab)
        {
            var index = Tabs.ToList().FindIndex(t => t.TabId == tabId);
            if (index >= 0)
            {
                int oldIndex = SelectedIndex;
                newTab.TabId = tabId;                           //TabId を引継ぎ
                newTab.PropertyChanged += Tab_PropertyChanged;  //PropertyChanged を再登録する
                Tabs[index] = newTab;                           //Tab置き換え
                SelectedIndex = oldIndex;                       //UI 更新
            }
        }
        //*************************************
        //動作
        // 測定中、子タブの要素を触れなくる
        //*************************************
        public void UpdateTabContentEnabled(bool enabled)
        {
            foreach (var tab in Tabs)
                tab.IsTabContentEnabled = enabled;
        }
    }
}
