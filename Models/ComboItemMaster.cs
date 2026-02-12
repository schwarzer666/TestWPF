namespace TemperatureCharacteristics.Models
{
    public static class ComboItemMaster
    {
        // ComboItems_Func
        public static readonly ComboItem[] FuncItems =
        {
            new ComboItem { Name = "Sweep",     Tag = "sweep" },
            new ComboItem { Name = "Constant1", Tag = "const" },
            new ComboItem { Name = "Constant2", Tag = "const" },
            new ComboItem { Name = "Constant3", Tag = "const" },
            new ComboItem { Name = "NotUsed",   Tag = "pulse" }
        };

        // ComboItems_Mode
        public static readonly ComboItem[] ModeItems =
        {
            new ComboItem { Name = "V", Tag = "VOLT" },
            new ComboItem { Name = "I", Tag = "CURR" }
        };

        // ComboItems_SourceRang
        public static readonly ComboItem[] SourceRangeItems =
        {
            new ComboItem { Name = "AUTO",   Tag = "range0" },
            new ComboItem { Name = "30V",    Tag = "range1" },
            new ComboItem { Name = "10V",    Tag = "range1" },
            new ComboItem { Name = "1V",     Tag = "range1" },
            new ComboItem { Name = "100mV",  Tag = "range2" },
            new ComboItem { Name = "10mV",   Tag = "range2" },
            new ComboItem { Name = "200mA",  Tag = "range2" },
            new ComboItem { Name = "100mA",  Tag = "range2" },
            new ComboItem { Name = "10mA",   Tag = "range2" },
            new ComboItem { Name = "1mA",    Tag = "range2" }
        };

        // ComboItems_MeasureRang
        public static readonly ComboItem[] MeasureRangeItems =
        {
            new ComboItem { Name = "AUTO",   Tag = "range0" },
            new ComboItem { Name = "1000V",  Tag = "range1" },
            new ComboItem { Name = "100V",   Tag = "range1" },
            new ComboItem { Name = "10V",    Tag = "range1" },
            new ComboItem { Name = "1V",     Tag = "range1" },
            new ComboItem { Name = "100mV",  Tag = "range2" },
            new ComboItem { Name = "3A",     Tag = "range1" },
            new ComboItem { Name = "1A",     Tag = "range1" },
            new ComboItem { Name = "100mA",  Tag = "range2" },
            new ComboItem { Name = "10mA",   Tag = "range2" },
            new ComboItem { Name = "1mA",    Tag = "range2" },
            new ComboItem { Name = "100uA",  Tag = "range3" }
        };

        // ComboItems_OSCRang
        public static readonly ComboItem[] OscRangeItems =
        {
            new ComboItem { Name = "10V",   Tag = "range1" },
            new ComboItem { Name = "5V",    Tag = "range1" },
            new ComboItem { Name = "2V",    Tag = "range1" },
            new ComboItem { Name = "1V",    Tag = "range1" },
            new ComboItem { Name = "500mV", Tag = "range2" },
            new ComboItem { Name = "200mV", Tag = "range2" },
            new ComboItem { Name = "100mV", Tag = "range2" },
            new ComboItem { Name = "50mV",  Tag = "range2" },
            new ComboItem { Name = "20mV",  Tag = "range2" }
        };

        // ComboItems_TrigSource
        public static readonly ComboItem[] TrigSourceItems =
        {
            new ComboItem { Name = "CH1", Tag = "1" },
            new ComboItem { Name = "CH2", Tag = "2" },
            new ComboItem { Name = "CH3", Tag = "3" },
            new ComboItem { Name = "CH4", Tag = "4" },
            new ComboItem { Name = "EXT", Tag = "EXT" }
        };

        // ComboItems_dmmTrigSource
        public static readonly ComboItem[] DmmTrigSourceItems =
        {
            new ComboItem { Name = "内部Trigger", Tag = "IMM" },
            new ComboItem { Name = "バスTrigger", Tag = "BUS" },
            new ComboItem { Name = "外部Trigger", Tag = "EXT" }
        };

        // ComboItems_Directional
        public static readonly ComboItem[] DirectionalItems =
        {
            new ComboItem { Name = "↗",   Tag = "rise" },
            new ComboItem { Name = "↘",   Tag = "fall" },
            new ComboItem { Name = "↗↘", Tag = "risefall" },
            new ComboItem { Name = "↘↗", Tag = "fallrise" }
        };

        // ComboItems_TrigDirectional
        public static readonly ComboItem[] TrigDirectionalItems =
        {
            new ComboItem { Name = "⤴", Tag = "RISE" },
            new ComboItem { Name = "⤵", Tag = "FALL" }
        };

        // ComboItems_Units
        public static readonly ComboItem[] UnitsItems =
        {
            new ComboItem { Name = "V",  Tag = "range1" },
            new ComboItem { Name = "mV", Tag = "range2" },
            new ComboItem { Name = "A",  Tag = "range1" },
            new ComboItem { Name = "mA", Tag = "range2" }
        };

        // ComboItems_tUnits
        public static readonly ComboItem[] TimeUnits =
        {
            new ComboItem { Name = "s",  Tag = "range1" },
            new ComboItem { Name = "ms", Tag = "range2" },
            new ComboItem { Name = "us", Tag = "range3" }
        };

        // ComboItems_Source
        public static readonly ComboItem[] SourceItems =
        {
            new ComboItem { Name = "電源1", Tag = "SOURCE1" },
            new ComboItem { Name = "電源2", Tag = "SOURCE2" },
            new ComboItem { Name = "電源3", Tag = "SOURCE3" },
            new ComboItem { Name = "電源4", Tag = "SOURCE4" },
            new ComboItem { Name = "PG",    Tag = "PG" },
            new ComboItem { Name = " ",     Tag = "SOURCEnull" }
        };

        // ComboItems_DetRelAct
        public static readonly ComboItem[] DetRelActItems =
        {
            new ComboItem { Name = "normal",    Tag = "ActNormal" },
            new ComboItem { Name = "normal+α", Tag = "ActSpecial1" }
        };

        // ComboItems_SourceLimit
        public static readonly ComboItem[] SourceLimitItems =
        {
            new ComboItem { Name = "mA", Tag = "range2" },
            new ComboItem { Name = "V",  Tag = "range1" }
        };

        // ComboItems_OutputCH
        public static readonly ComboItem[] OutputCHItems =
        {
            new ComboItem { Name = "CH1", Tag = "1" },
            new ComboItem { Name = "CH2", Tag = "2" }
        };

        // ComboItems_OutputZ
        public static readonly ComboItem[] OutputZItems =
        {
            new ComboItem { Name = "50Ω",   Tag = "50" },
            new ComboItem { Name = "High Z", Tag = "INF" }
        };

        // ComboItems_Polarity
        public static readonly ComboItem[] PolarityItems =
        {
            new ComboItem { Name = "Normal",   Tag = "NORM" },
            new ComboItem { Name = "Inverted", Tag = "INV" }
        };

        // ComboItems_OutputONOFF
        public static readonly ComboItem[] OutputOnOffItems =
        {
            new ComboItem { Name = "OFF", Tag = "OFF" },
            new ComboItem { Name = "ON",  Tag = "ON" }
        };

        // ComboItems_OSCTimeRang
        public static readonly ComboItem[] OscTimeRangeItems =
        {
            new ComboItem { Name = "1",   Tag = "1" },
            new ComboItem { Name = "2",   Tag = "2" },
            new ComboItem { Name = "5",   Tag = "5" },
            new ComboItem { Name = "10",  Tag = "10" },
            new ComboItem { Name = "20",  Tag = "20" },
            new ComboItem { Name = "50",  Tag = "50" },
            new ComboItem { Name = "100", Tag = "100" },
            new ComboItem { Name = "200", Tag = "200" },
            new ComboItem { Name = "500", Tag = "500" }
        };

        // ComboItems_OSCtDivUnit
        public static readonly ComboItem[] OscTDivUnitItems =
        {
            new ComboItem { Name = "s/div",  Tag = "range1" },
            new ComboItem { Name = "ms/div", Tag = "range2" },
            new ComboItem { Name = "us/div", Tag = "range3" },
            new ComboItem { Name = "ns/div", Tag = "range4" }
        };

        // ComboItems_MeasureSource
        public static readonly ComboItem[] MeasureSourceItems =
        {
            new ComboItem { Name = "CH1", Tag = "1" },
            new ComboItem { Name = "CH2", Tag = "2" },
            new ComboItem { Name = "CH3", Tag = "3" },
            new ComboItem { Name = "CH4", Tag = "4" }
        };
    }
}
