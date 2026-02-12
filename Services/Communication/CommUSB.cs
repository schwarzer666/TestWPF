using System.Management;
using Ivi.Visa.Interop;             //Visaライブラリ
using TemperatureCharacteristics.Exceptions;    //例外スローの為

namespace USBcommunication
{
    public class USBcomm
    {
        private readonly ResourceManager rm;    //resourceManager
        private FormattedIO488? inst;           //instrument null許容型
        private IUsb? usbdev;                   //リモート解除用 null許容型
        private IGpib? gpibdev;                 //リモート解除用 null許容型
        private static USBcomm? instance;       //インスタンスをnull許容型(xxx?)として宣言し初期値がnullでも問題ないと表示
        private readonly Guid usbClassGuid = new Guid("a9fdbb24-128a-11d5-9961-00108335e361");  //USB Test and Measurement Devices(IVI)のクラスGUID

        private USBcomm()
        {
            //何か初期化する変数等
            rm = new ResourceManager();   // コンストラクタで初期化
        }
        public static USBcomm Instance     //外部からのアクセス用
        {
            get
            {
                if (instance == null)
                    instance = new USBcomm(); // クラス内でインスタンスを生成
                return instance;
            }
        }

        //*************************************************
        //アクセス：public
        //戻り値：<bool> 通信結果
        //機能：測定器との通信確認
        //　　　*IDNを送信し応答があればTrue、無ければFalse
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //*************************************************
        public async Task<(string, bool)> Connection_Check(string usbid)
        {
            bool response = false;       //通信失敗しても確実に応答が返るように
            string command = "*IDN?";
            string res = string.Empty;
            try
            {
                res = await Comm_query(usbid, command);
                if (!string.IsNullOrEmpty(res))
                    response = true;
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"# 測定器通信確認エラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                res = $"# 測定器通信確認エラー: {ex.Message}\n";
                response = false;
            }
            return (res, response);
        }
        //*************************************************
        //アクセス：public
        //戻り値：なし(Task)
        //機能：測定器リモート解除外部アクセス用
        //*************************************************
        public async Task Remote_OFF(string USBID)
        {
            //**********************************
            //チェックの入っている測定器のリモート解除
            //**********************************
            try
            {
                //**********************************
                //動作
                // リモート解除対象測定器に対してインスタンス生成しopen
                // その後インスタンス再利用してリモート解除
                //**********************************
                inst = new FormattedIO488
                {
                    IO = (IMessage)rm.Open(USBID)
                };
                inst.IO.Timeout = 10000;
                await RemoteOFF(inst);
            }
            catch(TimeoutException tex)
            {
                throw new MeasWarningException($"# WARN:USB リモート解除タイムアウト {tex.Message}\n", tex);
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"# リモート解除でエラー: {ex.Message}");
                throw new MeasWarningException($"# WARN:USB リモート解除エラー\n {ex.Message}\n", ex);
            }
        }

        //*************************************************
        //アクセス：public
        //戻り値：なし
        //機能：測定器へコマンド送信
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：command
        //説明：<string> 送信コマンド
        //*************************************************
        public async Task Comm_send(string usbid, string command)
        {
            try
            {
                //***********
                //測定器Open
                //***********
                inst = new FormattedIO488
                {
                    IO = (IMessage)rm.Open(usbid)
                };
                inst.IO.Timeout = 10000;
                //********
                //cmd送信
                //********
                inst.WriteString(command, true);  //trueで終端文字を追加
            }
            catch (TimeoutException tex)
            {
                throw new MeasWarningException($"# WARN:USB コマンド送信タイムアウト {tex.Message}\n", tex);
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"# 送信コマンドエラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                throw new MeasWarningException($"# WARN:USB コマンド送信エラー\n {ex.Message}\n", ex);
            }
            finally
            {
                //***********
                //測定器Close
                //***********
                if (inst != null)
                {
                    await RemoteOFF(inst); // 既存インスタンスを活用
                    inst.IO.Close();
                    inst = null;     // 明示的にnullでリセット
                }
            }
        }

        //*************************************************
        //アクセス：public
        //戻り値：なし
        //機能：測定器へコマンド送信(リモート解除なし)
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：command
        //説明：<string> 送信コマンド
        //*************************************************
        public async Task Comm_sendB(string usbid, string command)
        {
            try
            {
                //***********
                //測定器Open
                //***********
                inst = new FormattedIO488
                {
                    IO = (IMessage)rm.Open(usbid)
                };
                inst.IO.Timeout = 10000;
                //********
                //cmd送信
                //********
                inst.WriteString(command, true);  //trueで終端文字を追加
            }
            catch (TimeoutException tex)
            {
                throw new MeasWarningException($"# WARN:USB コマンド送信タイムアウト {tex.Message}\n", tex);
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"# 送信コマンドエラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                throw new MeasWarningException($"# WARN:USB コマンド送信エラー\n {ex.Message}\n", ex);
            }
            finally
            {
                //***********
                //測定器Close
                //***********
                if (inst != null)
                {
                    inst.IO.Close();
                    inst = null;     // 明示的にnullでリセット
                }
            }
        }

        //*************************************************
        //アクセス：public
        //戻り値：<string> 応答文字列
        //機能：測定器とのコマンド送受信
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：command
        //説明：<string> 送信コマンド
        //*************************************************
        public async Task<string> Comm_query(string usbid,
                                                string command,
                                                int ioTimeoutMs = 1000,
                                                int maxAttempts = 10)
        {
            string response = "受信失敗";       //通信失敗しても確実に応答が返るように
            FormattedIO488? inst = null;
            try
            {
                //***********
                //測定器Open
                //***********
                inst = new FormattedIO488
                {
                    IO = (IMessage)rm.Open(usbid)
                };
                inst.IO.Timeout = ioTimeoutMs;
                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    try
                    {
                        //********
                        //cmd送信
                        //********
                        inst.WriteString(command, true);  //trueで終端文字を追加
                        //********
                        //応答受信
                        //********
                        response = inst.ReadString();
                        return response; //成功したら即返す
                    }
                    catch (Exception ex)
                    {
                        //最終試行なら例外を投げる
                        if (attempt == maxAttempts - 1)
                        {
                            //MessageBox.Show($"# 送受信エラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                            response = $"# 送受信エラー: {ex.Message}\n";
                            throw new MeasWarningException($"# WARN:USB コマンド送受信エラー\n {ex.Message}\n", ex);
                        }
                        //100ms待って再試行
                        await Task.Delay(100);
                    }
                }
            }
            finally
            {
                //***********
                //測定器Close
                //***********
                if (inst != null)
                {
                    await RemoteOFF(inst); // 既存インスタンスを活用
                    inst.IO.Close();
                    inst = null;     // 明示的にnullでリセット
                }
            }
            return response;
        }

        //*************************************************
        //アクセス：public
        //戻り値：<string> 応答文字列
        //機能：測定器とのコマンド送受信(リモート解除無し)
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：command
        //説明：<string> 送信コマンド
        //*************************************************
        public async Task<string> Comm_queryB(string usbid, 
                                                string command,
                                                CancellationToken cancellationToken, 
                                                int ioTimeoutMs = 1000, 
                                                int maxAttempts = 10)
        {
            string response = "受信失敗";       //通信失敗しても確実に応答が返るように
            FormattedIO488? inst = null;
            try
            {
                //***********
                //測定器Open
                //***********
                inst = new FormattedIO488
                {
                    IO = (IMessage)rm.Open(usbid)
                };
                inst.IO.Timeout = ioTimeoutMs;
                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        //********
                        //cmd送信
                        //********
                        inst.WriteString(command, true);  //trueで終端文字を追加
                        //********
                        //応答受信
                        //********
                        response = inst.ReadString();
                        return response; //成功したら即返す
                    }
                    catch (Exception ex)
                    {
                        //最終試行なら例外を投げる
                        if (attempt == maxAttempts - 1)
                        {
                            //MessageBox.Show($"# 送受信エラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                            response = $"# 送受信エラー: {ex.Message}\n";
                            throw new MeasWarningException($"# WARN:USB コマンド送受信エラー\n {ex.Message}\n", ex);
                        }
                        //100ms待って再試行
                        await Task.Delay(100, cancellationToken);
                    }
                }
            }
            finally
            {
                //***********
                //測定器Close
                //***********
                if (inst != null)
                {
                    inst.IO.Close();
                    inst = null;     // 明示的にnullでリセット
                }
            }
            return response;
        }

        //*************************************************
        //アクセス：public
        //戻り値：<string> 応答文字列
        //機能：測定器からの応答受信のみ
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //コメント
        // 使用しないかも
        //*************************************************
        private async Task<string> Comm_receive(string usbid)
        {
            string response = "受信失敗";       //通信失敗しても確実に応答が返るように
            try
            {
                try
                {
                    //***********
                    //測定器Open
                    //***********
                    inst = new FormattedIO488
                    {
                        IO = (IMessage)rm.Open(usbid)
                    };
                    inst.IO.Timeout = 10000;
                }
                catch (Exception ex)
                {
                    //MessageBox.Show($"# 受信時測定器オープンエラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    response = $"# 受信時測定器オープンエラー: {ex.Message}\n";
                }
                try
                {
                    //********
                    //応答受信
                    //********
                    response = inst.ReadString();
                }
                catch (Exception ex)
                {
                    //MessageBox.Show($"# 受信エラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    //response = "受信失敗";
                    response = $"# 受信エラー: {ex.Message}\n";
                }
            }
            finally
            {
                //***********
                //測定器Close
                //***********
                if (inst != null)
                {
                    await RemoteOFF(inst); // 既存インスタンスを活用
                    inst.IO.Close();
                    inst = null;     // 明示的にnullでリセット
                }
            }
            return response;
        }

        //*************************************************
        //アクセス：public
        //戻り値：USB IDリスト
        //機能：IVIに対応したUSB機器のアドレスを抽出(VISAから)
        //*************************************************
        public List<string> GetUSBIDList()
        {
            var usbList = new List<string>();

            try
            {
                var rm = new ResourceManager();
                string[] resources = rm.FindRsrc("?*INSTR");

                foreach (string res in resources)
                {
                    if (res.StartsWith("USB"))
                        usbList.Add(res);
                }
            }
            catch
            {
                //VISAが失敗した場合はWMIで補完する
                //呼び出した側にVISAが失敗した事を通知したい場合用
            }

            //VISAで見つからなかった場合はWMIで補完
            if (usbList.Count == 0)
                usbList.AddRange(GetUsbDevicesFromWmi());

            return usbList;
        }

        //*************************************************
        //アクセス：private
        //戻り値：USB IDリスト
        //機能：IVIに対応したUSB機器のアドレスを抽出(WMIから)
        //*************************************************
        private List<string> GetUsbDevicesFromWmi()
        {
            var list = new List<string>();

            try
            {
                //Win32_PnPEntityからクラスGUIDでフィルタ
                string query = $"SELECT * FROM Win32_PnPEntity WHERE ClassGuid = '{{{usbClassGuid}}}'";
                using var searcher = new ManagementObjectSearcher(query);

                foreach (ManagementObject? device in searcher.Get())
                {
                    string deviceId = device?["DeviceID"]?.ToString() ?? "";    // device["DeviceID"]があれば文字列形式で取り込み、なければnullになるので""
                    if (string.IsNullOrEmpty(deviceId)) continue;

                    int vidIndex = deviceId.IndexOf("VID_");                    //VID_の先頭番号(←4)を検索
                    int pidIndex = deviceId.IndexOf("PID_");                    //PID_の先頭番号(←13)を検索

                    if (vidIndex < 0 || pidIndex < 0) continue;

                    string strVID = deviceId.Substring(vidIndex + 4, 4);        //USB\VID_(←4+4)を除いて4文字取得
                    string strPID = deviceId.Substring(pidIndex + 4, 4);        //USB\VID_xxxx\PID_(←13+4)を除いて4文字取得

                    string strSN = deviceId.Substring(pidIndex + 9);            //USB\VID_xxxx\PID_xxxx\(←13+9)を除いて残りを取得

                    //VISA形式に整形
                    string usbADD = $"USB::0x{strVID}::0x{strPID}::{strSN}::0::INSTR";
                    list.Add(usbADD);
                }
            }
            catch (Exception ex)
            {
                throw new MeasFatalException($"FATAL:USB 一覧取得エラー\n {ex.Message}\n", ex);
            }

            return list;
        }
        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：リモート解除
        //引数1：instrument
        //説明：[FormattedIO488] インスタンス内容から呼び出し
        //*************************************************
        private async Task RemoteOFF(FormattedIO488 instrument)
        {
            if (instrument?.IO == null) return;

            string usbid = instrument.IO.ResourceName;
            switch (usbid.Substring(0, 3).ToUpper())           //大文字にして文字列の先頭(0)から3文字抽出               
            {
                case "USB":
                    usbdev = (IUsb)instrument.IO;
                    instrument.IO.Clear();
                    usbdev.ControlREN(RENControlConst.GPIB_REN_GTL);        //リモート状態を維持しつつローカル制御を許可
                    //usbdev.ControlREN(RENControlConst.GPIB_REN_DEASSERT); //リモート状態自体を解除
                    break;
                case "GPI":
                    gpibdev = (IGpib)instrument.IO;
                    gpibdev.ControlREN(RENControlConst.GPIB_REN_GTL);       //リモート状態を維持しつつローカル制御を許可
                    //gpibdev.ControlREN(RENControlConst.GPIB_REN_DEASSERT);//リモート状態自体を解除
                    break;
                default:
                    usbdev = (IUsb)instrument.IO;
                    usbdev.ControlREN(RENControlConst.GPIB_REN_GTL);        //リモート状態を維持しつつローカル制御を許可
                    //usbdev.ControlREN(RENControlConst.GPIB_REN_DEASSERT); //リモート状態自体を解除
                    break;
            }
        }
    }
}
