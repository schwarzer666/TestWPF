using TemperatureCharacteristics.Models;
using TemperatureCharacteristics.Models.TabData;
using TemperatureCharacteristics.Services.UserConfig;

namespace TemperatureCharacteristics.ViewModels.Tabs.Sweep
{
    //*************************************************
    //VITabViewModel（単一タブ）
    //*************************************************
    public class SweepTabViewModel : BaseViewModel, ITabItemViewModel
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
        private bool _normalSweepCheck;         //Sweep動作のみ CheckBox（normal sweep）
        private bool _pulseGenUseCheck;         //PulseGenerator使用 CheckBox（normal sweep以外）
        public string ItemHeader { get => _itemHeader; set => SetProperty(ref _itemHeader, value); }
        public bool MeasureOn { get => _measureOn; set => SetProperty(ref _measureOn, value); }
        public bool NormalSweepCheck { get => _normalSweepCheck; set => SetProperty(ref _normalSweepCheck, value); }
        public bool PulseGenUseCheck { get => _pulseGenUseCheck; set => SetProperty(ref _pulseGenUseCheck, value); }
        //*************************************************
        //子Tabの有効状態プロパティ
        //*************************************************
        private bool _isTabContentEnabled = true;         //子タブ内の要素の有効状態
        public bool IsTabContentEnabled { get => _isTabContentEnabled; set => SetProperty(ref _isTabContentEnabled, value); }
        //*************************************************
        //Configクラス
        //*************************************************
        public SweepConfig SweepConfig { get; set; } = new SweepConfig();
        public SourceConfig SourceConfig { get; set; } = new SourceConfig();
        public ConstConfig ConstConfig { get; set; } = new ConstConfig();
        public DetectReleaseConfig DetectReleaseConfig { get; set; } = new DetectReleaseConfig();
        public OscConfig OscConfig { get; set; } = new OscConfig();
        public DmmConfig DmmConfig { get; set; } = new DmmConfig();
        public PulseGenConfig PulseGenConfig { get; set; } = new PulseGenConfig();
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
        public SweepTabViewModel(IUserConfigService configService, ResourceViewModel resources)
        {
            //xamlからアクセスするために渡す
            ConfigService = configService;
            Resources = resources;
        }
        //*************************************************
        // Sweepタブの設定値をDTO（DataTransferObject）に変換
        //*************************************************
        public SweepTabData ToSweepTabData()
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

            //SWEEPSET
            var sweepset = new[]
            {
                SweepConfig.MinValue ?? "",
                ComboItemMaster.UnitsItems[SweepConfig.MinVUnitIndex ?? 0].Tag!,
                SweepConfig.MaxValue ?? "",
                ComboItemMaster.UnitsItems[SweepConfig.MaxVUnitIndex ?? 0].Tag!,
                ComboItemMaster.DirectionalItems[SweepConfig.DirectionalIndex ?? 0].Tag!,
                SweepConfig.StepTime ?? "",
                ComboItemMaster.TimeUnits[SweepConfig.StepTimeUnitIndex ?? 0].Tag!,
                SweepConfig.StepValue ?? "",
                ComboItemMaster.UnitsItems[SweepConfig.StepUnitIndex ?? 0].Tag!,
                SweepConfig.StanbyValue ?? "",
                ComboItemMaster.TimeUnits[SweepConfig.StanbyUnitIndex ?? 0].Tag!,
                ComboItemMaster.UnitsItems[SweepConfig.MinVUnitIndex ?? 0].Name!,
                ComboItemMaster.UnitsItems[SweepConfig.MaxVUnitIndex ?? 0].Name!,
                ComboItemMaster.UnitsItems[SweepConfig.StepUnitIndex ?? 0].Name!
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
                ComboItemMaster.UnitsItems[ConstConfig.Const3UnitIndex ?? 0].Name!,
                ConstConfig.Const4 ?? "",
                ComboItemMaster.UnitsItems[ConstConfig.Const4UnitIndex ?? 0].Tag!,
                ComboItemMaster.UnitsItems[ConstConfig.Const4UnitIndex ?? 0].Name!
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

            // DMM DISP
            var dmmDisp = new[]
            {
                DmmConfig.Meas1DispOn ?? false,
                DmmConfig.Meas2DispOn ?? false,
                DmmConfig.Meas3DispOn ?? false,
                DmmConfig.Meas4DispOn ?? false
            };

            // DMM1
            var dmm1 = new[]
            {
                ComboItemMaster.ModeItems[DmmConfig.MeasMode1Index ?? 0].Tag!,
                ComboItemMaster.MeasureRangeItems[DmmConfig.MeasRang1Index ?? 0].Name!,
                ComboItemMaster.MeasureRangeItems[DmmConfig.MeasRang1Index ?? 0].Tag!,
                DmmConfig.Meas1plc ?? ""
            };

            // DMM2
            var dmm2 = new[]
            {
                ComboItemMaster.ModeItems[DmmConfig.MeasMode2Index ?? 0].Tag!,
                ComboItemMaster.MeasureRangeItems[DmmConfig.MeasRang2Index ?? 0].Name!,
                ComboItemMaster.MeasureRangeItems[DmmConfig.MeasRang2Index ?? 0].Tag!,
                DmmConfig.Meas2plc ?? ""
            };

            // DMM3
            var dmm3 = new[]
            {
                ComboItemMaster.ModeItems[DmmConfig.MeasMode3Index ?? 0].Tag!,
                ComboItemMaster.MeasureRangeItems[DmmConfig.MeasRang3Index ?? 0].Name!,
                ComboItemMaster.MeasureRangeItems[DmmConfig.MeasRang3Index ?? 0].Tag!,
                DmmConfig.Meas3plc ?? ""
            };

            // DMM4
            var dmm4 = new[]
            {
                ComboItemMaster.ModeItems[DmmConfig.MeasMode4Index ?? 0].Tag!,
                ComboItemMaster.MeasureRangeItems[DmmConfig.MeasRang4Index ?? 0].Name!,
                ComboItemMaster.MeasureRangeItems[DmmConfig.MeasRang4Index ?? 0].Tag!,
                DmmConfig.Meas4plc ?? ""
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

            return new SweepTabData(
                ItemHeader,
                MeasureOn,
                NormalSweepCheck,
                PulseGenUseCheck,
                source1,
                source2,
                source3,
                source4,
                oscset,
                sweepset,
                constset,
                ComboItemMaster.DetRelActItems[DetectReleaseConfig.DetectReleaseIndex ?? 0].Tag!,
                detrelset,
                dmmDisp,
                dmm1,
                dmm2,
                dmm3,
                dmm4,
                ComboItemMaster.DmmTrigSourceItems[DmmConfig.MeasTrigIndex ?? 0].Tag!,
                pgset
            );
        }
    }
}