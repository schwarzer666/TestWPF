using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TemperatureCharacteristics.Models;
using TemperatureCharacteristics.Services.Relay;

namespace TemperatureCharacteristics.ViewModels.Devices
{
    public class RelayViewModel : BaseViewModel
    {
        private readonly IRelayService _relayService;
        //*************************************************
        //呼び出し元ログ伝搬用
        //*************************************************
        private readonly Action<string> _log;
        //*************************************************
        //リレーマニュアルON/OFF用
        //*************************************************
        private int? _selectedRelay = 1;
        public int? SelectedRelay { get => _selectedRelay; set => SetProperty(ref _selectedRelay, value); }
        //*************************************************
        //複数サンプル測定CheckBox用
        //*************************************************
        private bool _multiSample;
        public bool MultiSample { get => _multiSample; set => SetProperty(ref _multiSample, value); }
        //*************************************************
        //複数サンプルカウント用
        //*************************************************
        private int _sampleCount = 1;
        public int SampleCount { get => _sampleCount; set => SetProperty(ref _sampleCount, value); }
        //*************************************************
        //リレー制御基板シリアルナンバー
        //*************************************************
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
                    _log("リレーのシリアル番号未入力");
                    return;
                }
                var success = await _relayService.SetRelayOnExclusiveAsync(RelaySerialNumber, SelectedRelay.Value);
                _log(success ? $"リレー{SelectedRelay} ON" : "FT2232H 接続失敗");
            });

            SetRelayOffCommand = new RelayCommandAsync(async (param) =>
            {
                if (string.IsNullOrWhiteSpace(RelaySerialNumber))
                {
                    _log("リレーのシリアル番号未入力");
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
                _log("リレーのシリアル番号未入力");
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
                _log("リレーのシリアル番号未入力");
                return false;
            }
            return await _relayService.SetRelayPortOffAsync(RelaySerialNumber, port);
        }
        //****************************************************************************
        //動作
        // リレーループ
        // MainViewModel.csから呼び出しされonRelaySwitchedでコールバックし別メソッド実行
        //****************************************************************************
        public async Task ExcuteMeasurementRelayLoop(
                    List<(bool, string, string, string)> measInstData,
                    List<string> rows,
                    DebugOption option,
                    CancellationToken cancellationToken = default,
                    Func<List<(bool, string, string, string)>, List<string>, DebugOption, CancellationToken, Task> onRelaySwitched = null,
                    Action<string> statusCallback = null)
        {
            if (MultiSample)
            {
                for (int port = 1; port <= SampleCount; port++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    //*********************
                    //リレーON
                    //*********************
                    bool relaySuccess = await _relayService.SetRelayPortOnAsync(RelaySerialNumber, port);
                    _log($"複数測定Sample{port} 測定開始");
                    if (!relaySuccess)
                    {
                        rows.Add($"# リレー Sample {port} ON 失敗");
                        continue;   //次のポートへ
                    }

                    //呼び出し側にステータスを通知
                    statusCallback?.Invoke($"Sample {port} 測定中...");
                    rows.Add($"Sample{port}");

                    //*********************
                    //各Tab測定
                    //*********************
                    if (onRelaySwitched != null)
                        await onRelaySwitched(measInstData, rows, option, cancellationToken);
                    //*********************
                    //ONしたリレーをOFF
                    //*********************
                    await _relayService.SetRelayPortOffAsync(RelaySerialNumber, port);
                    _log($"複数測定Sample{port} 測定完了");
                }
            }
            else
            {
                _log($"単体測定開始");
                //呼び出し側にステータスを通知
                statusCallback?.Invoke("単体測定中...");
                rows.Add("単体測定");
                //*********************
                //各Tab測定
                //*********************
                if (onRelaySwitched != null)
                    await onRelaySwitched(measInstData, rows, option, cancellationToken);
                _log($"単体測定完了");
            }
        }
    }
}
