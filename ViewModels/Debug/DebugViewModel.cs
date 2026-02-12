using System.IO;
using System.Windows;
using USBcommunication;

namespace TemperatureCharacteristics.ViewModels.Debug
{
    public class DebugViewModel : BaseViewModel
    {
        private readonly USBcomm _comm;
        //*************************************************
        //DebugSettingsプロパティ
        //*************************************************
        private bool _debugEditThermoSoak;
        public bool DebugEditThermoSoak { get => _debugEditThermoSoak; set => SetProperty(ref _debugEditThermoSoak, value); }

        private string _debugThermoSoakTime = "600";
        public string DebugThermoSoakTime { get => _debugThermoSoakTime; set => SetProperty(ref _debugThermoSoakTime, value); }

        private bool _debugFinalFileFotterRemove;
        public bool DebugFinalFileFotterRemove { get => _debugFinalFileFotterRemove; set => SetProperty(ref _debugFinalFileFotterRemove, value); }

        private bool _debugUse8chOSC;
        public bool DebugUse8chOSC { get => _debugUse8chOSC; set => SetProperty(ref _debugUse8chOSC, value); }

        private bool _debugStopOnWarning;
        public bool DebugStopOnWarning { get => _debugStopOnWarning; set => SetProperty(ref _debugStopOnWarning, value); }

        private int _debugMaxLogLines = 100;
        public int DebugMaxLogLines { get => _debugMaxLogLines; set => SetProperty(ref _debugMaxLogLines, value); }

        private bool _debugEditMaxLogLines;
        public bool DebugEditMaxLogLines { get => _debugEditMaxLogLines; set => SetProperty(ref _debugEditMaxLogLines, value); }

        private string _saveLoadDefaultFolder;
        public string SaveLoadDefaultFolder { get => _saveLoadDefaultFolder; set => SetProperty(ref _saveLoadDefaultFolder, value); }
        public bool SkipUsbIdCheck { get; set; }
        //*************************************************
        //Debug表示用プロパティ
        //*************************************************
        private string _debugTextBox;
        public string DebugTextBox { get => _debugTextBox; set => SetProperty(ref _debugTextBox, value); }
        private string _debugLog;
        public string DebugLog { get => _debugLog; set => SetProperty(ref _debugLog, value); }
        private string _debugUSBID = "debug_USBID";
        public string DebugUSBID { get => _debugUSBID; set => SetProperty(ref _debugUSBID, value); }
        private string _debugSendCmd = "Send_cmd";
        public string DebugSendCmd { get => _debugSendCmd; set => SetProperty(ref _debugSendCmd, value); }
        //*************************************************
        //コンストラクタ
        //*************************************************
        public DebugViewModel()
        {
            _comm = USBcomm.Instance;
            DebugTextBox = string.Empty;
            DebugLog = string.Empty;
            //**********************************
            //ユーザー設定ファイル初期フォルダ生成
            //**********************************
            string saveloadDefaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Settings");
            //フォルダがなければ作成
            if (!Directory.Exists(saveloadDefaultPath))
                Directory.CreateDirectory(saveloadDefaultPath);
            SaveLoadDefaultFolder = saveloadDefaultPath;
        }
        //*************************************************
        //コマンド単独送信用
        //*************************************************
        public async Task DebugSendAsync()
        {
            string usbid = DebugUSBID;
            string cmd = DebugSendCmd;

            DebugLog += $"→{cmd} \n";   //送信cmdをlogに追記
            await _comm.Comm_send(usbid, cmd);
        }
        //*************************************************
        //コマンド単独送受信用
        //*************************************************
        public async Task DebugQueryAsync()
        {
            string usbid = DebugUSBID;
            string cmd = DebugSendCmd;

            DebugLog += $"→{cmd} \n";   //送信cmdをlogに追記
            string res = await _comm.Comm_query(usbid, cmd);
            DebugLog += $"←{res} \n";   //応答をlogに追記
        }
        //*************************************************
        //デバッグログ表示用
        //*************************************************
        public void LogDebug(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                //新しいログを追加
                DebugTextBox += $"{DateTime.Now:HH:mm:ss}\n {message}{Environment.NewLine}";

                //行数が上限を超えたら古い行を削除
                var lines = DebugTextBox.Split(
                    new[] { Environment.NewLine },
                    StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length > DebugMaxLogLines)
                {
                    //最新 MaxLogLines 行だけ残す
                    DebugTextBox = string.Join(Environment.NewLine,
                        lines.Skip(lines.Length - DebugMaxLogLines));
                }
            });
        }
    }
}
