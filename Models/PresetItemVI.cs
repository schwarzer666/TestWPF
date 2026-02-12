using System.Text.Json.Serialization;

namespace TemperatureCharacteristics.Models
{
    public class PresetItemVI : PresetItemBase
    {
        public PresetItemVI()
        {
            Type = "VI";
            VIConfig = new VIConfig();
            SourceConfig = new SourceConfig();
            ConstConfig = new ConstConfig();
            DetectReleaseConfig = new DetectReleaseConfig();
            DmmConfig = new DmmConfig();
        }
        [JsonPropertyOrder(3000)]
        public VIConfig VIConfig { get; set; } = new();
        [JsonPropertyOrder(3001)]
        public SourceConfig SourceConfig { get; set; } = new();
        [JsonPropertyOrder(3002)]
        public ConstConfig ConstConfig { get; set; } = new();
        [JsonPropertyOrder(3003)]
        public DmmConfig DmmConfig { get; set; } = new();
        [JsonPropertyOrder(3004)]
        public DetectReleaseConfig DetectReleaseConfig { get; set; } = new();
    }
    public class VIConfig
    {
        [JsonPropertyOrder(1)]
        [JsonPropertyName("StanbyValue")]
        public string? StanbyValue { get; set; }                //VI measure standby time
        [JsonPropertyOrder(2)]
        [JsonPropertyName("StanbyUnitIndex")]
        public int? StanbyUnitIndex { get; set; }               //VI measure standby time unit
    }
}
