using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TemperatureCharacteristics.Services;

namespace TemperatureCharacteristics.ViewModels.Devices
{
    public class RelayViewModel : INotifyPropertyChanged
    {
        private readonly IRelayService _relayService;
        private readonly Action<string> _log;

        private int? _selectedRelay = 1;
        public int? SelectedRelay { get => _selectedRelay; set { _selectedRelay = value; OnPropertyChanged(); } }
        public string? RelaySerialNumber { get; set; }
        //*************************************************
        //公開コマンド
        //*************************************************
        public ICommand SetRelayOnCommand { get; }
        public ICommand SetRelayOffCommand { get; }
        public RelayViewModel(IRelayService relayService, Action<string> logAction)
        {
            _relayService = relayService;
            _log = logAction;

            SetRelayOnCommand = new RelayCommandAsync(async (param) =>
            {
                if (SelectedRelay == null || string.IsNullOrWhiteSpace(RelaySerialNumber))
                {
                    _log("リレーのシリアルナンバーが未設定");
                    return;
                }
                var success = await _relayService.SetRelayOnExclusiveAsync(RelaySerialNumber, SelectedRelay.Value);
                _log(success ? $"リレー{SelectedRelay} ON" : "FT2232H 接続失敗");
            });

            SetRelayOffCommand = new RelayCommandAsync(async (param) =>
            {
                if (string.IsNullOrWhiteSpace(RelaySerialNumber))
                {
                    _log("リレーのシリアルナンバーが未設定");
                    return;
                }
                var success = await _relayService.SetAllRelaysOffAsync(RelaySerialNumber);
                _log(success ? "全リレー OFF" : "FT2232H 接続失敗");
            });
        }
        //****************************************************************************
        //動作
        // 選択したリレーON
        //****************************************************************************
        public async Task<bool> SetRelayPortOnAsync(int port)
        {
            if (string.IsNullOrWhiteSpace(RelaySerialNumber))
            {
                _log("リレーのシリアルナンバーが未設定");
                return false;
            }
            return await _relayService.SetRelayPortOnAsync(RelaySerialNumber, port);
        }
        //****************************************************************************
        //動作
        // 選択したリレーOFF
        //****************************************************************************
        public async Task<bool> SetRelayPortOffAsync(int port)
        {
            if (string.IsNullOrWhiteSpace(RelaySerialNumber))
            {
                _log("リレーのシリアルナンバーが未設定");
                return false;
            }
            return await _relayService.SetRelayPortOffAsync(RelaySerialNumber, port);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
