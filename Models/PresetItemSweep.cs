using System.Text.Json.Serialization;

namespace TemperatureCharacteristics.Models
{
    public class PresetItemSweep : PresetItemBase
    {
        public PresetItemSweep()
        {
            Type = "Sweep"; //型識別子を初期化
            SweepConfig = new SweepConfig();
            SourceConfig = new SourceConfig();
            ConstConfig = new ConstConfig();
            DetectReleaseConfig = new DetectReleaseConfig();
            OscConfig = new OscConfig();
            DmmConfig = new DmmConfig();
            PulseGenConfig = new PulseGenConfig();
        }
        [JsonPropertyOrder(1000)] 
        [JsonPropertyName("NormalSweepCheck")]
        public bool NormalSweepCheck { get; set; }  //Sweep動作のみ CheckBox（normal sweep）
        [JsonPropertyOrder(1001)] 
        [JsonPropertyName("PulseGenUseCheck")]
        public bool PulseGenUseCheck { get; set; }  //PulseGenerator使用 CheckBox（normal sweep以外）
        [JsonPropertyOrder(1002)]
        public SweepConfig SweepConfig { get; set; } = new();
        [JsonPropertyOrder(1003)]
        public SourceConfig SourceConfig { get; set; } = new();
        [JsonPropertyOrder(1004)]
        public ConstConfig ConstConfig { get; set; } = new();
        [JsonPropertyOrder(1005)]
        public DetectReleaseConfig DetectReleaseConfig { get; set; } = new();
        [JsonPropertyOrder(1006)]
        public OscConfig OscConfig { get; set; } = new();
        [JsonPropertyOrder(1007)]
        public DmmConfig DmmConfig { get; set; } = new();
        [JsonPropertyOrder(1008)]
        public PulseGenConfig PulseGenConfig { get; set; } = new();
    }
    public class SweepConfig
    {
        [JsonPropertyOrder(1)]
        [JsonPropertyName("MinValue")]
        public string? MinValue { get; set; }        //Sweep電源 min value
        [JsonPropertyOrder(2)]
        [JsonPropertyName("MinVUnitIndex")]
        public int? MinVUnitIndex { get; set; }      //Sweep電源 minValue Unit
        [JsonPropertyOrder(3)]
        [JsonPropertyName("MaxValue")]
        public string? MaxValue { get; set; }        //Sweep電源 max value
        [JsonPropertyOrder(4)]
        [JsonPropertyName("MaxVUnitIndex")]
        public int? MaxVUnitIndex { get; set; }      //Sweep電源 maxValue Unit
        [JsonPropertyOrder(5)]
        [JsonPropertyName("DirectionalIndex")]
        public int? DirectionalIndex { get; set; }   //Sweep電源 Sweep方向
        [JsonPropertyOrder(6)]
        [JsonPropertyName("StepTime")]
        public string? StepTime { get; set; }        //Sweep電源 wait value（ex.vdet1測定時tvdet1を指定
        [JsonPropertyOrder(7)]
        [JsonPropertyName("StepTimeUnitIndex")]
        public int? StepTimeUnitIndex { get; set; }  //Sweep電源 wait Unit
        [JsonPropertyOrder(8)]
        [JsonPropertyName("StepValue")]
        public string? StepValue { get; set; }       //Sweep電源 step value（normal sweep時のstep値を指定
        [JsonPropertyOrder(9)]
        [JsonPropertyName("StepUnitIndex")]
        public int? StepUnitIndex { get; set; }      //Sweep電源 step Unit（normal sweepのみ使用
        [JsonPropertyOrder(10)]
        [JsonPropertyName("StanbyValue")]
        public string? StanbyValue { get; set; }     //Sweep電源 standby value（normal sweep時の測定スタンバイ時を指定
        [JsonPropertyOrder(11)]
        [JsonPropertyName("StanbyUnitIndex")]
        public int? StanbyUnitIndex { get; set; }    //Sweep電源 standby Unit（normal sweepのみ使用
    }
}
