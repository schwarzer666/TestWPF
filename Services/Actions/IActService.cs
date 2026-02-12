using UTility;

namespace TemperatureCharacteristics.Services.Actions
{
    public interface IDeviceCombinable<TTabData>
    {
        //*************************************************
        //tabDataにはModels\TabData内にある以下の3つが入る
        //SweepTabData,DelayTabData,VITabData
        //*************************************************
        List<Device> CombineDeviceData(
            List<(bool IsChecked, string UsbId, string InstName, string Identifier)> measInst,
            IEnumerable<TTabData> tabData);
    }
}
