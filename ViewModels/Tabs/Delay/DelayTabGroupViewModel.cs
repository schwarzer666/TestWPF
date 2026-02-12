using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using TemperatureCharacteristics.Models.TabData;
using TemperatureCharacteristics.Services.UserConfig;

namespace TemperatureCharacteristics.ViewModels.Tabs.Delay
{
    //*************************************************
    //Delayタブの集合を管理するGroupViewModel
    //*************************************************
    public class DelayTabGroupViewModel : BaseViewModel, ITabGroupViewModel
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
        public ObservableCollection<(string Id, string ItemHeader)> CheckedDelayTabNames { get; }
        public string[] CheckedTabNames =>
          CheckedDelayTabNames.Any()
              ? CheckedDelayTabNames.Select(x => x.ItemHeader).ToArray()
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
        public DelayTabGroupViewModel(IUserConfigService configService, ResourceViewModel resources)
        {
            Resources = resources;  //GroupTabViewModel内では未使用→拡張用
            Tabs = new ObservableCollection<ITabItemViewModel>();
            CheckedDelayTabNames = new ObservableCollection<(string Id, string ItemHeader)>();
            //CheckedVITabNames が変わったら文字列を更新
            CheckedDelayTabNames.CollectionChanged += (s, e) => UpdateCheckedTabNamesText();
            //Tabs の追加・削除を監視
            Tabs.CollectionChanged += Tabs_CollectionChanged;
            //デザイナー用または初期データ
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                for (int i = 1; i <= 3; i++)
                {
                    //デザイナー用のダミーデータ
                    Tabs.Add(new DelayTabViewModel(configService, resources)
                    {
                        ItemHeader = $"LongLongNameItemHedder{i}",
                        MeasureOn = false,
                        DelayConfig =
                        {
                            MeasureChIndex = 3,
                            MeasureChDirectionalIndex = 1
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
                            SourceRang1Index = 0,
                            SourceRang2Index = 0,
                            SourceRang3Index = 0,
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
                            Const1UnitIndex = 0,
                            Const2UnitIndex = 0,
                            Const3UnitIndex = 0,
                            Const4UnitIndex = 0     //バインドエラー回避
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
                        OscConfig =
                        {
                            OSCPos1 = "Pos",
                            OSCPos2 = "Pos",
                            OSCPos3 = "Pos",
                            OSCPos4 = "Pos",
                            OSCTrigLevel = "Level",
                            OSCTimePos = "Pos",
                            OSCRang1Index = 2,
                            OSCRang2Index = 2,
                            OSCRang3Index = 2,
                            OSCRang4Index = 2,
                            TrigSourceIndex = 3,
                            TrigDirectionalIndex = 1,
                            LevelUnitIndex = 0,

                            OSCTimeRangeIndex = 0,
                            OSCTimeRangeUnitIndex = 0
                        },
                        PulseGenConfig =
                        {
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
                            WidthUnitIndex = 1,
                            TrigOutIndex = 0
                        }
                    });
                }
            }
            else
            {
                //実行用のデータ
                for (int i = 1; i <= 11; i++)
                {
                    var tab = new DelayTabViewModel(configService, resources)
                    {
                        ItemHeader = $"Item{i}",
                        MeasureOn = false,
                        DelayConfig =
                        {
                            MeasureChIndex = 3,
                            MeasureChDirectionalIndex = 1
                        },
                        SourceConfig =
                        {
                            SourceFunc1Index = 4,
                            SourceFunc2Index = 1,
                            SourceFunc3Index = 2,
                            SourceFunc4Index = 3,
                            SourceMode1Index = 0,
                            SourceMode2Index = 0,
                            SourceMode3Index = 0,
                            SourceMode4Index = 0,
                            SourceRang1Index = 2,
                            SourceRang2Index = 2,
                            SourceRang3Index = 2,
                            SourceRang4Index = 2,
                            SourceLimit1Index = 0,
                            SourceLimit2Index = 0,
                            SourceLimit3Index = 0,
                            SourceLimit4Index = 0,
                            SourceLimit1 = null,
                            SourceLimit2 = null,
                            SourceLimit3 = null,
                            SourceLimit4 = null
                        },
                        ConstConfig =
                        {
                            Const1 = "0",
                            Const2 = "0",
                            Const3 = "0",
                            Const1UnitIndex = 0,
                            Const2UnitIndex = 0,
                            Const3UnitIndex = 0,
                            Const4UnitIndex = 0     //バインドエラー回避
                        },
                        DetectReleaseConfig =
                        {
                            DetectReleaseA = "0.3",
                            DetectReleaseB = "0",
                            CheckTime = "1.2",
                            DetectSourceAIndex = 1,
                            DetectSourceBIndex = 5,
                            DetectUnitAIndex = 0,
                            DetectUnitBIndex = 0,
                            CheckTimeUnitIndex = 0,
                            DetectReleaseIndex = 1
                        },
                        OscConfig =
                        {
                            OSCPos1 = "1",
                            OSCPos2 = "1",
                            OSCPos3 = "-3",
                            OSCPos4 = "-3",
                            OSCTrigLevel = "0",
                            OSCTimePos = "20",
                            OSCRang1Index = 2,
                            OSCRang2Index = 2,
                            OSCRang3Index = 2,
                            OSCRang4Index = 2,
                            TrigSourceIndex = 3,
                            TrigDirectionalIndex = 1,
                            LevelUnitIndex = 0,
                            OSCTimeRangeIndex = 0,
                            OSCTimeRangeUnitIndex = 0
                        },
                        PulseGenConfig =
                        {
                            LowValue = "3.6",
                            HighValue = "4.8",
                            PeriodValue = "2",
                            WidthValue = "1",
                            LowVUnitIndex = 0,
                            HighVUnitIndex = 0,
                            PolarityIndex = 0,
                            OutputChIndex = 0,
                            OutputLoadIndex = 1,
                            PeriodUnitIndex = 0,
                            WidthUnitIndex = 0,
                            TrigOutIndex = 0
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
        public IEnumerable<DelayTabData> GetMeasureOnTabData()
        {
            return Tabs
                .Where(tab => tab.MeasureOn)
                .Select(tab => ((DelayTabViewModel)tab).ToDelayTabData());
        }
        //*************************************************
        //Tabs にタブが追加・削除されたときの処理
        //*************************************************
        private void Tabs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (DelayTabViewModel tab in e.NewItems)
                {
                    // MeasureOn / ItemHeader の変更を監視
                    tab.PropertyChanged += Tab_PropertyChanged;

                    // 初期状態で MeasureOn が true のタブは CheckedDelayTabNames に追加
                    if (tab.MeasureOn && !CheckedDelayTabNames.Any(x => x.Id == tab.TabId))
                        CheckedDelayTabNames.Add((tab.TabId, tab.ItemHeader));
                }
            }

            if (e.OldItems != null)
            {
                foreach (DelayTabViewModel tab in e.OldItems)
                {
                    tab.PropertyChanged -= Tab_PropertyChanged;

                    // 削除されたタブを CheckedDelayTabNames から除外
                    var item = CheckedDelayTabNames.FirstOrDefault(x => x.Id == tab.TabId);
                    if (item != default)
                        CheckedDelayTabNames.Remove(item);
                }
            }
        }
        //*************************************************
        //タブの MeasureOn / ItemHeader の変更を監視
        //*************************************************
        private void Tab_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not DelayTabViewModel tab) return;
            //MeasureOnプロパティに変更があった場合
            if (e.PropertyName == nameof(DelayTabViewModel.MeasureOn))
            {
                //MeasureOnがチェックされた場合
                if (tab.MeasureOn)
                {
                    if (!CheckedDelayTabNames.Any(x => x.Id == tab.TabId))
                        CheckedDelayTabNames.Add((tab.TabId, tab.ItemHeader));
                }
                //MeasureOnのチェックが外れた場合
                else
                {
                    var item = CheckedDelayTabNames.FirstOrDefault(x => x.Id == tab.TabId);
                    if (item != default)
                        CheckedDelayTabNames.Remove(item);
                }
            }
            //ItemHeaderプロパティに変更があった場合
            else if (e.PropertyName == nameof(DelayTabViewModel.ItemHeader))
            {
                var match = CheckedDelayTabNames
                                    .Select((x, i) => (x, i))
                                    .FirstOrDefault(x => x.x.Id == tab.TabId);

                if (match.x != default)
                    CheckedDelayTabNames[match.i] = (tab.TabId, tab.ItemHeader);
            }
        }
        //*************************************************
        //動作
        // MeasureOnがチェックされるとTextBlockに項目名追加
        // 何もチェックされていないと"対象なし"
        //*************************************************
        private void UpdateCheckedTabNamesText()
        {
            CheckedTabNamesText = CheckedDelayTabNames.Any()
                ? string.Join(", ", CheckedDelayTabNames.Select(x => x.ItemHeader))
                : "対象なし";
        }
        //*************************************************
        //動作
        // LoadUserConfig時、Tabを差し替え
        // プロパティの内容を変更してもUIが更新されないため
        //*************************************************
        public void ReplaceTab(string tabId, DelayTabViewModel newTab)
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
