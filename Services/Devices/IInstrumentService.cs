using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TemperatureCharacteristics.Services.Results;
using TemperatureCharacteristics.ViewModels.Devices;

namespace TemperatureCharacteristics.Services.Devices
{
    public interface IInstrumentService
    {
        ObservableCollection<InstrumentViewModel> Instruments { get; }

        ObservableCollection<string> USBIDList { get; }
        ObservableCollection<string> GPIBList { get; }
        ObservableCollection<string> FT2232HList { get; }

        Task<Result> GetDeviceListsAsync();
        Task<Result<List<string>>> ConnectAllAsync();
        //Task<Result> CheckConnectionAsync(InstrumentViewModel inst);

    }

}
