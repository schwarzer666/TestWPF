using FTD2XX_NET;       //FT2232Hのアドレス取得に必要
using System.Management;

namespace TemperatureCharacteristics.Services.Communication
{
    public class FTDeviceInfo   //FT2232H専用クラス
    {
        public string SerialNumber { get; set; } = "";
        public override string ToString() => SerialNumber;
    }
    public class FT2232HDeviceFinder
    {
        private static FT2232HDeviceFinder? instance;      //インスタンスをnull許容型(xxx?)として宣言し初期値がnullでも問題ないと表示
        private FT2232HDeviceFinder()      //コンストラクタ　インスタンスが生成(=初期化)された段階で実行される
        {
            //初期化
        }
        public static FT2232HDeviceFinder Instance     //外部からのアクセス用
        {
            get
            {
                if (instance == null)
                    instance = new FT2232HDeviceFinder(); // クラス内でインスタンスを生成
                return instance;
            }
        }
        //*************************************************
        //動作
        // FT2232H用通信確認
        // シリアルナンバーでポートオープン+BitBangモード
        //*************************************************
        public async Task<(string response, bool success)> CheckFT2232HConnection(string serialNumber)
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
                return ("シリアルナンバー未入力", false);

            using var bitBang = new FT2232HBitBangService();
            bool success = await bitBang.OpenBySerialNumberAsync(serialNumber);
            if (!success)
                return ("OpenBySerialNumber 失敗", false);
            //bitBang.Dispose();

            return ($"FT2232H [{serialNumber}] BitBangモード OK", true);
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
                throw new Exception($"# WARN: FTDI アドレス取得でエラー: {ex.Message}");
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
