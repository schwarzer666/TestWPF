namespace TemperatureCharacteristics.Models.TabData
{
    public record VITabData(
        string TabName,
        bool MeasureOn,
        string[] Source1set,
        string[] Source2set,
        string[] Source3set,
        string[] Source4set,
        string[] VIset,
        string[] Constset,
        string[] Detrelset,
        bool[] DMMDisp,
        string[] DMM1set,
        string[] DMM2set,
        string[] DMM3set,
        string[] DMM4set,
        string DMMTrigSrc
    );
}
