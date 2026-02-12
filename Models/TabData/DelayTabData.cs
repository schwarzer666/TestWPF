namespace TemperatureCharacteristics.Models.TabData
{
    public record DelayTabData(
        string TabName,
        bool MeasureOn,
        string[] Source1set,
        string[] Source2set,
        string[] Source3set,
        string[] Source4set,
        string[] OSCset,
        string[] Constset,
        string[] Detrelset,
        string[] PGset,
        string[] Delayset
    );
}
