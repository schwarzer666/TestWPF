namespace TemperatureCharacteristics.Models
{
    //*************************************************
    //定義
    // DebugOption専用class
    //*************************************************
    public class DebugOption
    {
        public bool FinalFileFotterRemove { get; set; }
        public bool Use8chOSC { get; set; }
        public bool StopOnWarning { get; set; }
        public bool EditThermoSoak { get; set; }
        public string ThermoSoakTime { get; set; } = "600";
        public int MaxLogLines { get; set; } = 100;
    }
}
