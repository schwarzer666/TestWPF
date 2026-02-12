using TemperatureCharacteristics.Models;
using TemperatureCharacteristics.Models.TabData;
using TemperatureCharacteristics.Services.UserConfig;

namespace TemperatureCharacteristics.ViewModels.Tabs.VI
{
    //*************************************************
    //VITabViewModel（単一タブ）
    //*************************************************
    public class VITabViewModel : BaseViewModel, ITabItemViewModel
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
        //*************************************************
        //子Tabの有効状態プロパティ
        //*************************************************
        private bool _isTabContentEnabled = true;         //子タブ内の要素の有効状態
        public bool IsTabContentEnabled { get => _isTabContentEnabled; set => SetProperty(ref _isTabContentEnabled, value); }
        //*************************************************
        //Configクラス
        //*************************************************
        public VIConfig VIConfig { get; set; } = new VIConfig();
        public SourceConfig SourceConfig { get; set; } = new SourceConfig();
        public ConstConfig ConstConfig { get; set; } = new ConstConfig();
        public DetectReleaseConfig DetectReleaseConfig { get; set; } = new DetectReleaseConfig();
        public DmmConfig DmmConfig { get; set; } = new DmmConfig();
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
        public VITabViewModel(IUserConfigService configService, ResourceViewModel resources)
        {
            //xamlからアクセスするために渡す
            ConfigService = configService;
            Resources = resources;
        }
        //*************************************************
        // VIタブの設定値をDTO（DataTransferObject）に変換
        //*************************************************
        public VITabData ToVITabData()
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

            // VI
            var viset = new[]
            {
                VIConfig.StanbyValue ?? "",
                ComboItemMaster.TimeUnits[VIConfig.StanbyUnitIndex ?? 0].Tag!
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

            return new VITabData(
                ItemHeader,
                MeasureOn,
                source1,
                source2,
                source3,
                source4,
                viset,
                constset,
                detrelset,
                dmmDisp,
                dmm1,
                dmm2,
                dmm3,
                dmm4,
                ComboItemMaster.DmmTrigSourceItems[DmmConfig.MeasTrigIndex ?? 0].Tag!
            );
        }
    }
}