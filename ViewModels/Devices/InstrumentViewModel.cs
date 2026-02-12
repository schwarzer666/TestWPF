using System.Collections.ObjectModel;

namespace TemperatureCharacteristics.ViewModels.Devices
{
    public class InstrumentViewModel : BaseViewModel
    {
        private bool _isChecked;
        public bool IsChecked { get => _isChecked; set => SetProperty(ref _isChecked, value); }
        private string _usbId;
        public string UsbId { get => _usbId; set => SetProperty(ref _usbId, value); }
        private string _instName;
        public string InstName { get => _instName; set => SetProperty(ref _instName, value); }
        private string _displayName;
        public string DisplayName { get => _displayName; set => SetProperty(ref _displayName, value); }
        public string Identifier { get; }
        private bool _hasSignalName;
        public bool HasSignalName { get => _hasSignalName; set => SetProperty(ref _hasSignalName, value); }
        private bool _isConnected;
        public bool IsConnected { get => _isConnected; set => SetProperty(ref _isConnected, value); }

        public ObservableCollection<string> USBIDList { get; set; }
        public InstrumentViewModel(string identifier, string usbId, string instName, string displayName, bool isChecked)
        {
            Identifier = identifier;
            UsbId = usbId;
            InstName = instName;
            DisplayName = displayName;
            IsChecked = isChecked;
        }
    }
}
