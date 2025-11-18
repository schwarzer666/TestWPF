using System.Text;      //StringBuilderを使用するのに必要
using System.Management;
using System.Windows;
using Ivi.Visa.Interop;
using FTD2XX_NET;       //FT2232Hのアドレス取得に必要

namespace USBID
{
    public class FTDeviceInfo   //FT2232H専用クラス
    {
        public string SerialNumber { get; set; } = "";
        public override string ToString() => SerialNumber;
    }
    public class GetUSBID       //クラス名
    {
        private readonly Guid usbClassGuid = new Guid("a9fdbb24-128a-11d5-9961-00108335e361");  //USB Test and Measurement Devices(IVI)のクラスGUID
        private readonly StringBuilder deviceInfo = new StringBuilder();     //このクラス内でアクセス可能なフィールド変数
        private readonly ResourceManager rm;    //ISAリソースマネージャ
        private static GetUSBID? instance;      //インスタンスをnull許容型(xxx?)として宣言し初期値がnullでも問題ないと表示
        private GetUSBID()      //コンストラクタ　インスタンスが生成(=初期化)された段階で実行される
        {
            //初期化
            rm = new ResourceManager(); // VISAリソースマネージャを初期化
        }
        public static GetUSBID Instance     //外部からのアクセス用
        {
            get
            {
                if (instance == null)
                {
                    instance = new GetUSBID(); // クラス内でインスタンスを生成
                }
                return instance;
            }
        }

        //*************************************************
        //アクセス：public
        //戻り値：<string> USBID List
        //機能：GetUSBaddrを呼び出してUSBアドレスを取得
        //コメント
        // フィールド変数deviceInfoの内容を返答
        //*************************************************
        public string USBIDList()           //外部からのアクセス用
        {
            deviceInfo.Clear();
            GetUSBaddr();
            if(FindGPIBCon())
                GetGPIBaddr();
            return deviceInfo.ToString();
        }

        //*************************************************
        //アクセス：public
        //戻り値：<FTDeviceInfo> FT2232H List
        //機能：GetFT2232HDevicesを呼び出してFT2232Hアドレスを取得
        //      FTD2XX.NETからの読み込みに失敗したらWMI
        //*************************************************
        public List<FTDeviceInfo> GetFT2232HList()           //外部からのアクセス用
        {
            var result = new List<FTDeviceInfo>();
            if (!GetFT2232HDevices(result))
            {
                GetFT2232HDevicesForWMI(result);
            }
            return result;
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：IVIに対応したUSB機器のアドレスを抽出
        //コメント
        // フィールド変数deviceInfoにアドレス群を格納
        //*************************************************
        private void GetUSBaddr()
        {
            try
            {
                // Win32_PnPEntityからクラスGUIDでフィルタ
                string query = $"SELECT * FROM Win32_PnPEntity WHERE ClassGuid = '{{{usbClassGuid}}}'";
                ManagementObjectSearcher? searcher = new ManagementObjectSearcher(query);

                foreach (ManagementObject? device in searcher.Get())
                {
                    string deviceId = device["DeviceID"]?.ToString() ?? "不明";   //条件 ? 真の場合 : 偽の場合 ??=null参照エラー防止 → device["DeviceID"]があれば文字列形式で取り込み、なければnullになるので"不明"という文字列を入れる

                    string strVID = deviceId.Substring(deviceId.IndexOf("VID_") + 4, 4);     //indexofでVID_の先頭番号(←4)を探し、USB\VID_(←4+4)を除いて4文字取得
                    string strPID = deviceId.Substring(deviceId.IndexOf("PID_") + 4, 4);   //indexofでPID_の先頭番号(←13)を探し、USB\VID_xxxx\PID_(←13+4)を除いて4文字取得
                    string strSN = deviceId.Substring(deviceId.IndexOf("PID_") + 9);      //indexofでPID_の先頭番号(←13)を探し、USB\VID_xxxx\PID_xxxx\(←13+9)を除いて残りを取得

                    //usbADD = "USB::0x" + strVID.ToString() + "::0x" + strPID.ToString() + "::" + strSN.ToString() + "::0::INSTR";
                    string usbADD = $"USB::0x{strVID}::0x{strPID}::{strSN}::0::INSTR";
                    deviceInfo.AppendLine($"{usbADD}");
                }
                if (deviceInfo.Length == 0)
                {
                    MessageBox.Show("IVIに対応した測定器が接続されていません");
                }
            }
            catch (Exception ex)        //例外処理
            {
                MessageBox.Show($"USBアドレス取得エラー: {ex.Message}");
            }
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：NI製GPIBで繋がれた機器のアドレスを抽出
        //コメント
        // フィールド変数deviceInfoにアドレス群を格納
        //*************************************************
        private void GetGPIBaddr()
        {
            try
            {
                //string[] gpibResources = rm.FindResources("GPIB?*INSTR").Cast<string>().ToArray();
                //string[] resources = rm.FindRsrc("GPIB?*::INTFC");

                string[] gpibResources = rm.FindRsrc("GPIB?*INSTR");
                foreach (string resource in gpibResources)
                {
                    deviceInfo.AppendLine(resource); // 例: "GPIB0::5::INSTR"
                }
                if (deviceInfo.Length == 0)
                {
                    MessageBox.Show("GPIBに対応した測定器が接続されていません");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"GPIBアドレス取得エラー: {ex.Message}");
            }
        }

        //*************************************************
        //アクセス：private
        //戻り値：<bool> 完了フラグ
        //機能：NI製GPIB-USBコントローラ検出
        //*************************************************
        private bool FindGPIBCon()
        {
            try
            {
                string[] resource = rm.FindRsrc("GPIB?*::INTFC");
                return resource.Length > 0;
            }
            catch (Exception)
            {
                //MessageBox.Show($"GPIBコントローラなし");
                return false;
            }
        }
        //*************************************************
        //アクセス：private
        //戻り値：List
        //機能：FT2232Hのアドレス(シリアル)を抽出(FTD2XXから)
        //コメント
        // フィールド変数deviceInfoにアドレス群を格納
        //*************************************************
        private bool GetFT2232HDevices(List<FTDeviceInfo> list)
        {
            try
            {
                var ftdi = new FTDI();
                uint numDevices = 0;
                var status = ftdi.GetNumberOfDevices(ref numDevices);

                if (status != FTDI.FT_STATUS.FT_OK || numDevices == 0)
                {
                    //MessageBox.Show("FT2232H: デバイスが見つかりません");
                    ftdi.Close();  //手動解放
                    ftdi = null;
                    return false;
                }

                var deviceList = new FTDI.FT_DEVICE_INFO_NODE[numDevices];
                status = ftdi.GetDeviceList(deviceList);  //GetDeviceListを使用
                if (status != FTDI.FT_STATUS.FT_OK)
                {
                    //MessageBox.Show("FT2232H: 情報取得失敗");
                    ftdi.Close();
                    ftdi = null;
                    return false;
                }

                for (uint i = 0; i < numDevices; i++)
                {
                    var node = deviceList[i];
                    //DescriptionでFT2232Hをフィルタ
                    if (node.Description.Contains("FT2232"))
                    {
                        list.Add(new FTDeviceInfo { SerialNumber = node.SerialNumber });
                    }
                }

                ftdi.Close();
                ftdi = null;
                return list.Count > 0;
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"FT2232H エラー: {ex.Message}");
                return false;
            }
        }
        //*************************************************
        //アクセス：private
        //戻り値：List
        //機能：FT2232Hのアドレス(シリアル)を抽出(WMIから)
        //コメント
        // FTD2XX.NETからFT2232Hのシリアル番号を取得できなかった時のバックアップ
        //*************************************************
        private bool GetFT2232HDevicesForWMI(List<FTDeviceInfo> list)
        {
            try
            {
                string query = "SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE 'USB\\\\VID_0403&PID_6010%' OR DeviceID LIKE 'USB\\\\VID_0403&PID_6014%'";
                using var searcher = new ManagementObjectSearcher(query);

                foreach (ManagementObject? device in searcher.Get())
                {
                    string deviceId = device?["DeviceID"]?.ToString() ?? "";
                    if (string.IsNullOrEmpty(deviceId)) continue;

                    int snStart = deviceId.LastIndexOf('\\') + 1;
                    string serial = snStart > 0 ? deviceId.Substring(snStart) : "不明";

                    list.Add(new FTDeviceInfo { SerialNumber = serial });
                }

                return list.Count > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
