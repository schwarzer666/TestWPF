using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using TemperatureCharacteristics.Models.TabData;
using TemperatureCharacteristics.Services.UserConfig;

namespace TemperatureCharacteristics.ViewModels.Tabs.Sweep
{
    //*************************************************
    //Sweepタブの集合を管理するGroupViewModel
    //*************************************************
    public class SweepTabGroupViewModel : BaseViewModel, ITabGroupViewModel
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
        public ObservableCollection<(string Id, string ItemHeader)> CheckedSweepTabNames { get; }
        public string[] CheckedTabNames =>
          CheckedSweepTabNames.Any()
              ? CheckedSweepTabNames.Select(x => x.ItemHeader).ToArray()
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
        public SweepTabGroupViewModel(IUserConfigService configService, ResourceViewModel resources)
        {
            Resources = resources;  //GroupTabViewModel内では未使用→拡張用
            Tabs = new ObservableCollection<ITabItemViewModel>();
            CheckedSweepTabNames = new ObservableCollection<(string Id, string ItemHeader)>();
            //CheckedSweepTabNames が変わったら文字列を更新
            CheckedSweepTabNames.CollectionChanged += (s, e) => UpdateCheckedTabNamesText();
            //Tabs の追加・削除を監視
            Tabs.CollectionChanged += Tabs_CollectionChanged;
            //デザイナー用または初期データ
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                for (int i = 1; i <= 3; i++)
                {
                    //デザイナー用のダミーデータ
                    Tabs.Add(new SweepTabViewModel(configService, resources)
                    {
                        ItemHeader = $"LongLongNameItemHedder{i}",
                        MeasureOn = false,
                        NormalSweepCheck = false,
                        PulseGenUseCheck = false,
                        SweepConfig =
                        {
                            MinValue = "Min",
                            MaxValue = "Max",
                            MinVUnitIndex = 0,
                            MaxVUnitIndex = 0,
                            DirectionalIndex = 0,
                            StepTime = "Step",
                            StepValue = "Step",
                            StanbyValue = "Stanby",
                            StepTimeUnitIndex = 0,
                            StepUnitIndex = 0,
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
                            DetectReleaseIndex = 0,
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
                            OSCRang1Index = 2,
                            OSCRang2Index = 2,
                            OSCRang3Index = 2,
                            OSCRang4Index = 2,
                            TrigSourceIndex = 3,
                            TrigDirectionalIndex = 1,
                            LevelUnitIndex = 0
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
                //実行時用のデータ
                for (int i = 1; i <= 15; i++)
                {
                    SweepTabViewModel tab = new SweepTabViewModel(configService, resources)
                    {
                        ItemHeader = $"Item{i}",
                        MeasureOn = false,
                        NormalSweepCheck = false,
                        PulseGenUseCheck = false,
                        SweepConfig =
                        {
                            MinValue = "0.0",
                            MaxValue = "1.0",
                            MinVUnitIndex = 0,
                            MaxVUnitIndex = 0,
                            DirectionalIndex = 0,
                            StepTime = "1",
                            StepValue = "0.1",
                            StanbyValue = "0",
                            StepTimeUnitIndex = 0,
                            StepUnitIndex = 0,
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
                            Const1UnitIndex = 0,
                            Const2UnitIndex = 0,
                            Const3UnitIndex = 0,
                            Const4UnitIndex = 0     //バインドエラー回避
                        },
                        DetectReleaseConfig =
                        {
                            DetectReleaseA = "0.3",
                            DetectReleaseB = "0",
                            CheckTime = "1",
                            DetectReleaseIndex = 0,
                            DetectSourceAIndex = 1,
                            DetectSourceBIndex = 5,
                            DetectUnitAIndex = 0,
                            DetectUnitBIndex = 0,
                            CheckTimeUnitIndex = 0
                        },
                        OscConfig =
                        {
                            OSCPos1 = "1",
                            OSCPos2 = "1",
                            OSCPos3 = "-3",
                            OSCPos4 = "-3",
                            OSCTrigLevel = "0",
                            OSCRang1Index = 2,
                            OSCRang2Index = 2,
                            OSCRang3Index = 2,
                            OSCRang4Index = 2,
                            TrigSourceIndex = 3,
                            TrigDirectionalIndex = 1,
                            LevelUnitIndex = 0
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
                        PulseGenConfig =
                        {
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
                            WidthUnitIndex = 1,
                            TrigOutIndex = 0
                        },
                        TabId = Guid.NewGuid().ToString()      //識別用ID自動生成
                    };
                    Tabs.Add(tab);
                }
                UpdateCheckedTabNamesText();            //初期状態のTextBlock用文字列を設定
            }

            Resources = resources;
        }
        //*************************************************
        //機能：測定対象タブのデータ取得
        //　　　MeasureOn checkboxにcheckが入っているタブが対象
        //*************************************************
        public IEnumerable<SweepTabData> GetMeasureOnTabData()
        {
            return Tabs
                .Where(tab => tab.MeasureOn)
                .Select(tab => ((SweepTabViewModel)tab).ToSweepTabData());
        }
        //*************************************************
        //Tabs にタブが追加・削除されたときの処理
        //*************************************************
        private void Tabs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (SweepTabViewModel tab in e.NewItems)
                {
                    // MeasureOn / ItemHeader の変更を監視
                    tab.PropertyChanged += Tab_PropertyChanged;

                    // 初期状態で MeasureOn が true のタブは CheckedSweepTabNames に追加
                    if (tab.MeasureOn && !CheckedSweepTabNames.Any(x => x.Id == tab.TabId))
                        CheckedSweepTabNames.Add((tab.TabId, tab.ItemHeader));
                }
            }

            if (e.OldItems != null)
            {
                foreach (SweepTabViewModel tab in e.OldItems)
                {
                    tab.PropertyChanged -= Tab_PropertyChanged;

                    // 削除されたタブを CheckedSweepTabNames から除外
                    var item = CheckedSweepTabNames.FirstOrDefault(x => x.Id == tab.TabId);
                    if (item != default)
                        CheckedSweepTabNames.Remove(item);
                }
            }
        }
        //*************************************************
        //タブの MeasureOn / ItemHeader の変更を監視
        //*************************************************
        private void Tab_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not SweepTabViewModel tab) return;
            //MeasureOnプロパティに変更があった場合
            if (e.PropertyName == nameof(SweepTabViewModel.MeasureOn))
            {
                //MeasureOnがチェックされた場合
                if (tab.MeasureOn)
                {
                    if (!CheckedSweepTabNames.Any(x => x.Id == tab.TabId))
                        CheckedSweepTabNames.Add((tab.TabId, tab.ItemHeader));
                }
                //MeasureOnのチェックが外れた場合
                else
                {
                    var item = CheckedSweepTabNames.FirstOrDefault(x => x.Id == tab.TabId);
                    if (item != default)
                        CheckedSweepTabNames.Remove(item);
                }
            }
            //ItemHeaderプロパティに変更があった場合
            else if (e.PropertyName == nameof(SweepTabViewModel.ItemHeader))
            {
                var match = CheckedSweepTabNames
                                    .Select((x, i) => (x, i))
                                    .FirstOrDefault(x => x.x.Id == tab.TabId);

                if (match.x != default)
                    CheckedSweepTabNames[match.i] = (tab.TabId, tab.ItemHeader);
            }
        }
        //*************************************************
        //動作
        // MeasureOnがチェックされるとTextBlockに項目名追加
        // 何もチェックされていないと"対象なし"
        //*************************************************
        private void UpdateCheckedTabNamesText()
        {
            CheckedTabNamesText = CheckedSweepTabNames.Any()
                ? string.Join(", ", CheckedSweepTabNames.Select(x => x.ItemHeader))
                : "対象なし";
        }
        //*************************************************
        //動作
        // LoadUserConfig時、Tabを差し替え
        // プロパティの内容を変更してもUIが更新されないため
        //*************************************************
        public void ReplaceTab(string tabId, SweepTabViewModel newTab)
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
