using TemperatureCharacteristics.Models;
using TemperatureCharacteristics.Models.TabData;
using TemperatureCharacteristics.Services.UserConfig;

namespace TemperatureCharacteristics.ViewModels.Tabs.Delay
{
    //*************************************************
    //DelayTabViewModel（単一タブ）
    //*************************************************
    public class DelayTabViewModel : BaseViewModel, ITabItemViewModel
    {
        //*************************************************
        //xamlのSave,Load,Presetボタンの為ConfigService注入
        //*************************************************
        public IUserConfigService ConfigService { get; }
        //*************************************************
        //xamlのComboItemの為ResouceViewModel（ComboItemMaster）注入
        //*************************************************
        public ResourceViewModel Resources { get; }
        //*************************************************
        //プロパティ
        //*************************************************
        public string TabId { get; set; }       //識別用TabID
        private string _itemHeader;             //Itemタブ入力欄
        private bool _measureOn;                //測定ON/OFF CheckBox
        public string ItemHeader { get => _itemHeader; set => SetProperty(ref _itemHeader, value); }
        public bool MeasureOn { get => _measureOn; set => SetProperty(ref _measureOn, value); }
        public int TrigOutIndex
        {
            get => PulseGenConfig.TrigOutIndex;
            set
            {
                if (PulseGenConfig.TrigOutIndex == value)
                    return;

                PulseGenConfig.TrigOutIndex = value;
                UpdateTrigDirectionalIndex();
                OnPropertyChanged();
            }
        }
        public int TrigDirectionalIndex
        { 
            get => OscConfig.TrigDirectionalIndex;
            set
            {
                if (OscConfig.TrigDirectionalIndex != value)
                {
                    OscConfig.TrigDirectionalIndex = value;
                    OnPropertyChanged();
                }
            }
        }
        //*************************************************
        //子Tabの有効状態プロパティ
        //*************************************************
        private bool _isTabContentEnabled = true;         //子タブ内の要素の有効状態
        public bool IsTabContentEnabled { get => _isTabContentEnabled; set => SetProperty(ref _isTabContentEnabled, value); }
        //*************************************************
        //Configクラス
        //*************************************************
        public DelayConfig DelayConfig { get; set; } = new DelayConfig();
        public SourceConfig SourceConfig { get; set; } = new SourceConfig();
        public ConstConfig ConstConfig { get; set; } = new ConstConfig();
        public DetectReleaseConfig DetectReleaseConfig { get; set; } = new DetectReleaseConfig();
        public OscConfig OscConfig { get; set; } = new OscConfig();
        public PulseGenConfig PulseGenConfig { get; set; } = new PulseGenConfig();
        //*************************************************
        //動作
        // PG TRIGOUT=1(ON)にした場合、OSC TrigDirectional=0(Rise)に変更
        //*************************************************
        private void UpdateTrigDirectionalIndex()
        {
            //TrigOutIndex=1の場合:TrigDirectionalIndex=0
            if (TrigOutIndex == 1)
            {
                TrigDirectionalIndex = 0;
            }
        }
        //*************************************************
        //イベント
        // DetectSourceでPGを選択した場合使用不可の通知
        //*************************************************
        public event EventHandler? DetectSourceIndexPGSelected;
        public int? DetectSourceAIndex
        {
            get => DetectReleaseConfig.DetectSourceAIndex;
            set
            {
                if (DetectReleaseConfig.DetectSourceAIndex != value)
                {
                    DetectReleaseConfig.DetectSourceAIndex = value;

                    if (value == 4)     //ComboItemMaster SourceItems内の"PG"を選択したら
                        DetectSourceIndexPGSelected?.Invoke(this, EventArgs.Empty);

                    OnPropertyChanged();
                }
            }
        }
        public int? DetectSourceBIndex
        {
            get => DetectReleaseConfig.DetectSourceBIndex;
            set
            {
                if (DetectReleaseConfig.DetectSourceBIndex != value)
                {
                    DetectReleaseConfig.DetectSourceBIndex = value;

                    if (value == 4)     //ComboItemMaster SourceItems内の"PG"を選択したら
                        DetectSourceIndexPGSelected?.Invoke(this, EventArgs.Empty);

                    OnPropertyChanged();
                }
            }
        }

        //*************************************************
        //動作
        // コンストラクタで初期化
        //*************************************************
        public DelayTabViewModel(IUserConfigService configService, ResourceViewModel resources)
        {
            //xamlからアクセスするために渡す
            ConfigService = configService;
            Resources = resources;
        }
        //*************************************************
        // Delayタブの設定値をDTO（DataTransferObject）に変換
        //*************************************************
        public DelayTabData ToDelayTabData()
        {
            // SOURCE1
            var source1 = new[]
            {
                ComboItemMaster.FuncItems[SourceConfig.SourceFunc1Index ?? 0].Name!,
                ComboItemMaster.FuncItems[SourceConfig.SourceFunc1Index ?? 0].Tag!,
                ComboItemMaster.ModeItems[SourceConfig.SourceMode1Index ?? 0].Tag!,
                ComboItemMaster.SourceRangeItems[SourceConfig.SourceRang1Index ?? 0].Name!,
                ComboItemMaster.SourceRangeItems[SourceConfig.SourceRang1Index ?? 0].Tag!,
                SourceConfig.SourceLimit1 ?? "0",
                ComboItemMaster.SourceLimitItems[SourceConfig.SourceLimit1Index ?? 0].Tag!,
                ComboItemMaster.SourceLimitItems[SourceConfig.SourceLimit1Index ?? 0].Name!
            };

            // SOURCE2
            var source2 = new[]
            {
                ComboItemMaster.FuncItems[SourceConfig.SourceFunc2Index ?? 0].Name!,
                ComboItemMaster.FuncItems[SourceConfig.SourceFunc2Index ?? 0].Tag!,
                ComboItemMaster.ModeItems[SourceConfig.SourceMode2Index ?? 0].Tag!,
                ComboItemMaster.SourceRangeItems[SourceConfig.SourceRang2Index ?? 0].Name!,
                ComboItemMaster.SourceRangeItems[SourceConfig.SourceRang2Index ?? 0].Tag!,
                SourceConfig.SourceLimit2 ?? "0",
                ComboItemMaster.SourceLimitItems[SourceConfig.SourceLimit2Index ?? 0].Tag!,
                ComboItemMaster.SourceLimitItems[SourceConfig.SourceLimit2Index ?? 0].Name!
            };

            // SOURCE3
            var source3 = new[]
            {
                ComboItemMaster.FuncItems[SourceConfig.SourceFunc3Index ?? 0].Name!,
                ComboItemMaster.FuncItems[SourceConfig.SourceFunc3Index ?? 0].Tag!,
                ComboItemMaster.ModeItems[SourceConfig.SourceMode3Index ?? 0].Tag!,
                ComboItemMaster.SourceRangeItems[SourceConfig.SourceRang3Index ?? 0].Name!,
                ComboItemMaster.SourceRangeItems[SourceConfig.SourceRang3Index ?? 0].Tag!,
                SourceConfig.SourceLimit3 ?? "0",
                ComboItemMaster.SourceLimitItems[SourceConfig.SourceLimit3Index ?? 0].Tag!,
                ComboItemMaster.SourceLimitItems[SourceConfig.SourceLimit3Index ?? 0].Name!
            };

            // SOURCE4
            var source4 = new[]
            {
                ComboItemMaster.FuncItems[SourceConfig.SourceFunc4Index ?? 0].Name!,
                ComboItemMaster.FuncItems[SourceConfig.SourceFunc4Index ?? 0].Tag!,
                ComboItemMaster.ModeItems[SourceConfig.SourceMode4Index ?? 0].Tag!,
                ComboItemMaster.SourceRangeItems[SourceConfig.SourceRang4Index ?? 0].Name!,
                ComboItemMaster.SourceRangeItems[SourceConfig.SourceRang4Index ?? 0].Tag!,
                SourceConfig.SourceLimit4 ?? "0",
                ComboItemMaster.SourceLimitItems[SourceConfig.SourceLimit4Index ?? 0].Tag!,
                ComboItemMaster.SourceLimitItems[SourceConfig.SourceLimit4Index ?? 0].Name!
            };

            // OSC
            var oscset = new[]
            {
                ComboItemMaster.OscRangeItems[OscConfig.OSCRang1Index ?? 0].Name!,
                ComboItemMaster.OscRangeItems[OscConfig.OSCRang1Index ?? 0].Tag!,
                OscConfig.OSCPos1 ?? "",
                ComboItemMaster.OscRangeItems[OscConfig.OSCRang2Index ?? 0].Name!,
                ComboItemMaster.OscRangeItems[OscConfig.OSCRang2Index ?? 0].Tag!,
                OscConfig.OSCPos2 ?? "",
                ComboItemMaster.OscRangeItems[OscConfig.OSCRang3Index ?? 0].Name!,
                ComboItemMaster.OscRangeItems[OscConfig.OSCRang3Index ?? 0].Tag!,
                OscConfig.OSCPos3 ?? "",
                ComboItemMaster.OscRangeItems[OscConfig.OSCRang4Index ?? 0].Name!,
                ComboItemMaster.OscRangeItems[OscConfig.OSCRang4Index ?? 0].Tag!,
                OscConfig.OSCPos4 ?? "",
                ComboItemMaster.TrigSourceItems[OscConfig.TrigSourceIndex ?? 0].Tag!,
                ComboItemMaster.TrigDirectionalItems[OscConfig.TrigDirectionalIndex].Tag!,
                OscConfig.OSCTrigLevel ?? "",
                ComboItemMaster.UnitsItems[OscConfig.LevelUnitIndex ?? 0].Tag!,
                ComboItemMaster.OscTimeRangeItems[OscConfig.OSCTimeRangeIndex ?? 0].Tag!,
                ComboItemMaster.OscTDivUnitItems[OscConfig.OSCTimeRangeUnitIndex ?? 0].Tag!,
                OscConfig.OSCTimePos ?? ""
            };

            // CONST
            var constset = new[]
            {
                ConstConfig.Const1 ?? "",
                ComboItemMaster.UnitsItems[ConstConfig.Const1UnitIndex ?? 0].Tag!,
                ConstConfig.Const2 ?? "",
                ComboItemMaster.UnitsItems[ConstConfig.Const2UnitIndex ?? 0].Tag!,
                ConstConfig.Const3 ?? "",
                ComboItemMaster.UnitsItems[ConstConfig.Const3UnitIndex ?? 0].Tag!,
                ComboItemMaster.UnitsItems[ConstConfig.Const1UnitIndex ?? 0].Name!,
                ComboItemMaster.UnitsItems[ConstConfig.Const2UnitIndex ?? 0].Name!,
                ComboItemMaster.UnitsItems[ConstConfig.Const3UnitIndex ?? 0].Name!
            };

            // DETREL
            var detrelset = new[]
            {
                ComboItemMaster.SourceItems[DetectReleaseConfig.DetectSourceAIndex ?? 0].Tag!,
                DetectReleaseConfig.DetectReleaseA ?? "",
                ComboItemMaster.UnitsItems[DetectReleaseConfig.DetectUnitAIndex ?? 0].Tag!,
                ComboItemMaster.SourceItems[DetectReleaseConfig.DetectSourceBIndex ?? 0].Tag!,
                DetectReleaseConfig.DetectReleaseB ?? "",
                ComboItemMaster.UnitsItems[DetectReleaseConfig.DetectUnitBIndex ?? 0].Tag!,
                DetectReleaseConfig.CheckTime ?? "",
                ComboItemMaster.TimeUnits[DetectReleaseConfig.CheckTimeUnitIndex ?? 0].Tag!
            };

            // PG
            var pgset = new[]
            {
                ComboItemMaster.OutputCHItems[PulseGenConfig.OutputChIndex ?? 0].Tag!,
                PulseGenConfig.LowValue ?? "",
                ComboItemMaster.UnitsItems[PulseGenConfig.LowVUnitIndex ?? 0].Tag!,
                PulseGenConfig.HighValue ?? "",
                ComboItemMaster.UnitsItems[PulseGenConfig.HighVUnitIndex ?? 0].Tag!,
                ComboItemMaster.PolarityItems[PulseGenConfig.PolarityIndex ?? 0].Tag!,
                PulseGenConfig.PeriodValue ?? "",
                ComboItemMaster.TimeUnits[PulseGenConfig.PeriodUnitIndex ?? 0].Tag!,
                PulseGenConfig.WidthValue ?? "",
                ComboItemMaster.TimeUnits[PulseGenConfig.WidthUnitIndex ?? 0].Tag!,
                ComboItemMaster.OutputZItems[PulseGenConfig.OutputLoadIndex ?? 0].Tag!,
                ComboItemMaster.OutputOnOffItems[PulseGenConfig.TrigOutIndex].Tag!
            };

            // DELAY
            var delayset = new[]
            {
                ComboItemMaster.MeasureSourceItems[DelayConfig.MeasureChIndex ?? 0].Tag!,
                ComboItemMaster.TrigDirectionalItems[DelayConfig.MeasureChDirectionalIndex ?? 0].Tag!
            };

            return new DelayTabData(
                ItemHeader,
                MeasureOn,
                source1,
                source2,
                source3,
                source4,
                oscset,
                constset,
                detrelset,
                pgset,
                delayset
            );
        }
    }
}