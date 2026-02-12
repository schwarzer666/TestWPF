namespace TemperatureCharacteristics.Models.TabData
{
    public record SweepTabData(
        string TabName,
        bool MeasureOn,
        bool NormalSweepCheck,
        bool PulseGenUseCheck,
        string[] Source1set,
        string[] Source2set,
        string[] Source3set,
        string[] Source4set,
        string[] OSCset,
        string[] Sweepset,
        string[] Constset,
        string Detrelact,
        string[] Detrelset,
        bool[] DMMDisp,
        string[] DMM1set,
        string[] DMM2set,
        string[] DMM3set,
        string[] DMM4set,
        string DMMTrigSrc,
        string[] PGset
    );
}
