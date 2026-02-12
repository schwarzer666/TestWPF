using GPIBcommunication;
using InputCheck;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TemperatureCharacteristics.Services.Communication;
using TemperatureCharacteristics.Services.Results;
using TemperatureCharacteristics.ViewModels.Debug;
using TemperatureCharacteristics.ViewModels.Devices;
using USBcommunication;
using UTility;

namespace TemperatureCharacteristics.Services.Devices
{
    public class InstrumentService : IInstrumentService
    {
        private readonly DebugViewModel _debug;
        private readonly UT _utility;
        private readonly InpCheck _errCheck;
        private readonly USBcomm _usb;
        private readonly GPIBComm _gpib;
        private readonly FT2232HDeviceFinder _ftdi;
        private readonly Action<string> _log;
        public ObservableCollection<InstrumentViewModel> Instruments { get; }
        public ObservableCollection<string> USBIDList { get; } = new();
        public ObservableCollection<string> GPIBList { get; } = new();
        public ObservableCollection<string> FT2232HList { get; } = new();
        //*************************************************
        //複数測定、温度測定CheckBoxチェック用
        //*************************************************
        private readonly Dictionary<int, Action> _instrumentCheckHandlers = new();
        //*************************************************
        //コンストラクタ
        //*************************************************
        public InstrumentService(DebugViewModel debug)
        {
            _debug = debug;
            _utility = UT.Instance;
            _errCheck = InpCheck.Instance;
            _usb = USBcomm.Instance;
            _gpib = GPIBComm.Instance;
            _ftdi = FT2232HDeviceFinder.Instance;

            Instruments = new ObservableCollection<InstrumentViewModel>
            {
                new("SOURCE1", "input USB ID", "", "電源1", false),
                new("SOURCE2", "input USB ID", "", "電源2", false),
                new("SOURCE3", "input USB ID", "", "電源3", false),
                new("SOURCE4", "input USB ID", "", "電源4", false),
                new("OSC", "input USB ID", "", "OSC", false),
                new("PULSE", "input USB ID", "", "PULSE", false),
                new("DMM1", "input USB ID", "", "DMM1", false),
                new("DMM2", "input USB ID", "", "DMM2", false),
                new("DMM3", "input USB ID", "", "DMM3", false),
                new("DMM4", "input USB ID", "", "DMM4", false),
                new("THERMO", "input GPIB Addr.", "", "サーモ", false),
                new("RELAY", "input Serial No.", "", "リレー", false)
            };
        }
        //*************************************************
        //動作
        // USB,GPIB,FTDI接続機器検出
        //*************************************************
        public async Task<Result> GetDeviceListsAsync()
        {
            USBIDList.Clear();
            GPIBList.Clear();
            FT2232HList.Clear();

            var usbList = await Task.Run(() => _usb.GetUSBIDList());
            var gpibList = await Task.Run(() => _gpib.GetGPIBList());
            var ftList = await Task.Run(() => _ftdi.GetFT2232HList());

            foreach (var id in usbList) USBIDList.Add(id.Trim());
            foreach (var id in gpibList) GPIBList.Add(id.Trim());
            foreach (var ft in ftList) FT2232HList.Add(ft.SerialNumber);

            var allIds = USBIDList.Concat(GPIBList).Concat(FT2232HList);
            _debug.LogDebug(string.Join(Environment.NewLine, allIds));

            bool any = USBIDList.Any() || GPIBList.Any() || FT2232HList.Any();

            return any
                ? Result.Ok("デバイスを検出しました\nプルダウンから選択してください") //OKの場合UIを切り替えるのでメッセージは表示されない
                : Result.Fail("IVI対応測定器もGPIBも接続されていません");
        }
        //*************************************************
        //動作
        // 全測定器のチェックボックスとIDと名前をリスト化
        // チェックボックスがチェックされている測定器のIDを抽出(utility.Get_Active_USBaddr)
        // 各測定器に*IDNを送信し応答があるかチェック
        // 問題がなければ各IDテキストボックスの色を変える（未実装）
        //*************************************************
        public async Task<Result<List<string>>> ConnectAllAsync()
        {
            //*********************
            //UIスレッドがブロックされるため
            //測定器アドレスをコピー
            //*********************
            var measInstData = Instruments
                .Select(inst => (inst.IsChecked,
                inst.UsbId ?? "", 		//TextBox_IDが入力されていなければ空白
                inst.InstName ?? "", 	//TextBox_NAMEが入力されていなければ空白
                inst.Identifier))
                .ToList();
            //*********************
            //チェックされている測定器がない
            //*********************
            var activeUsbId = _utility.GetActiveUSBAddr(measInstData);
            if (!activeUsbId.Any())
                return Result<List<string>>.Fail("チェックされた測定器がありません");
            //*********************
            //アドレスチェック
            //*********************
            (var messageCheck, bool addrOk) = await _errCheck.VerifyInsAddr(measInstData);
            if (!addrOk)
                return Result<List<string>>.Fail(string.Join(Environment.NewLine, messageCheck));
            //*********************
            //接続確認
            //*********************
            var successList = new List<string>();
            bool allCheck = true;
            foreach (var device in Instruments.Where(i => i.IsChecked))
            {
                var result = await CheckConnectionAsync(device);
                //通信成功したIDをリスト化
                if (result.Success)
                    successList.Add(device.Identifier);
                allCheck &= result.Success;
            }

            return allCheck
                ? Result<List<string>>.Ok(successList, "接続確認問題なし")
                : Result<List<string>>.Fail("接続確認に失敗した測定器があります");
        }
        //*************************************************
        //動作
        // 接続確認
        //*************************************************
        public async Task<Result> CheckConnectionAsync(InstrumentViewModel inst)
        {
            if (string.IsNullOrWhiteSpace(inst.UsbId))
                return Result.Fail("USB ID 未入力");

            _debug.LogDebug($"{inst.Identifier} ← *IDN?");

            if (inst.Identifier == "RELAY")
            {
                var (response, success) = await _ftdi.CheckFT2232HConnection(inst.UsbId);
                _debug.LogDebug($"{inst.Identifier} → {response}");

                return success
                    ? Result.Ok(response)
                    : Result.Fail(response);
            }
            else
            {
                var (response, success) = await _usb.Connection_Check(inst.UsbId);
                _debug.LogDebug($"{inst.Identifier} → {response}");

                return success
                    ? Result.Ok(response)
                    : Result.Fail(response);
            }
        }
    }
}
