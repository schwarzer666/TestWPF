using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TemperatureCharacteristics.Layout
{
    public class DebugSettingsViewModel : INotifyPropertyChanged
    {
        private bool _debugEditThermoSoak;
        public bool DebugEditThermoSoak { get => _debugEditThermoSoak; set { if (_debugEditThermoSoak != value) { _debugEditThermoSoak = value; OnPropertyChanged(); } } }
        private string _debugThermoSoakTime = "600";
        public string DebugThermoSoakTime { get => _debugThermoSoakTime; set { _debugThermoSoakTime = value; OnPropertyChanged(); } }
        private bool _debugFinalFileFotterRemove;
        public bool DebugFinalFileFotterRemove { get => _debugFinalFileFotterRemove; set { if (_debugFinalFileFotterRemove != value) { _debugFinalFileFotterRemove = value; OnPropertyChanged(); } } }
        private bool _debugUse8chOSC;
        public bool DebugUse8chOSC { get => _debugUse8chOSC; set { if (_debugUse8chOSC != value) { _debugUse8chOSC = value; OnPropertyChanged(); } } }
        private bool _debugStopOnWarning;
        public bool DebugStopOnWarning { get => _debugStopOnWarning; set { if (_debugStopOnWarning != value) { _debugStopOnWarning = value; OnPropertyChanged(); } } }
        private int _debugMaxLogLines = 100;
        public int DebugMaxLogLines { get => _debugMaxLogLines; set { _debugMaxLogLines = value; OnPropertyChanged(); } }
        private bool _debugEditMaxLogLines;
        public bool DebugEditMaxLogLines { get => _debugEditMaxLogLines; set { if (_debugEditMaxLogLines != value) { _debugEditMaxLogLines = value; OnPropertyChanged(); } } }
        //*************************************************
        //定義
        // プロパティが変更通知時に呼び出され値を更新(Window用)
        //*************************************************
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
