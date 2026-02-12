using System.Text.Json.Serialization;

namespace TemperatureCharacteristics.Models
{
    public class PresetItemDelay : PresetItemBase
    {
        public PresetItemDelay()
        {
            Type = "Delay";
            DelayConfig = new DelayConfig();
            SourceConfig = new SourceConfig();
            ConstConfig = new ConstConfig();
            DetectReleaseConfig = new DetectReleaseConfig();
            OscConfig = new OscConfig();
            PulseGenConfig = new PulseGenConfig();
        }
        [JsonPropertyOrder(2000)]
        public DelayConfig DelayConfig { get; set; } = new();
        [JsonPropertyOrder(2001)]
        public SourceConfig SourceConfig { get; set; } = new();
        [JsonPropertyOrder(2002)]
        public ConstConfig ConstConfig { get; set; } = new();
        [JsonPropertyOrder(2003)]
        public DetectReleaseConfig DetectReleaseConfig { get; set; } = new();
        [JsonPropertyOrder(2004)]
        public OscConfig OscConfig { get; set; } = new();
        [JsonPropertyOrder(2005)]
        public PulseGenConfig PulseGenConfig { get; set; } = new();
    }
    public class DelayConfig
    {
        [JsonPropertyOrder(1)]
        [JsonPropertyName("MeasureChIndex")]
        public int? MeasureChIndex { get; set; }    //遅延時間測定対象ch
        [JsonPropertyOrder(2)]
        [JsonPropertyName("MeasureChDirectionalIndex")]
        public int? MeasureChDirectionalIndex { get; set; } //遅延時間測定対象ch Rise/Fall
    }
}
