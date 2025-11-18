using UTility;

namespace TemperatureCharacteristics.Act
{
    public interface IDeviceCombinable
    {
        List<Device> CombineDeviceData(
                                    List<(bool IsChecked, string UsbId, string InstName, string Identifier)> meas_inst,
                                    object getMeasureOnTabDataObj);
    }
}
