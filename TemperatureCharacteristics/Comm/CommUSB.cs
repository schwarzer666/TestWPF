using Ivi.Visa.Interop;             //Visaライブラリ
using System.Windows;

namespace USBcommunication
{
    public class USBcomm
    {
        private readonly ResourceManager rm;    //resourceManager
        private FormattedIO488? inst;           //instrument null許容型
        private IUsb? usbdev;                   //リモート解除用 null許容型
        private IGpib? gpibdev;                 //リモート解除用 null許容型
        private static USBcomm? instance;       //インスタンスをnull許容型(xxx?)として宣言し初期値がnullでも問題ないと表示

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
                MessageBox.Show($"# 測定器通信確認エラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
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
            catch (Exception ex)
            {
                MessageBox.Show($"# リモート解除でエラー: {ex.Message}");
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
            catch (Exception ex)
            {
                MessageBox.Show($"# 送信コマンドエラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
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
            catch (Exception ex)
            {
                MessageBox.Show($"# 送信コマンドエラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
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
                                                CancellationToken ct,
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
                    ct.ThrowIfCancellationRequested();
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
                        return response; // 成功したら即返す
                    }
                    catch (Exception ex)
                    {
                        //最終試行なら例外を投げる
                        if (attempt == maxAttempts - 1)
                        {
                            MessageBox.Show($"# 送受信エラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                            throw;
                        }
                        //100ms待って再試行
                        await Task.Delay(100, ct);
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
                                                CancellationToken ct, 
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
                    ct.ThrowIfCancellationRequested();
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
                        return response; // 成功したら即返す
                    }
                    catch (Exception ex)
                    {
                        //最終試行なら例外を投げる
                        if (attempt == maxAttempts - 1)
                        {
                            MessageBox.Show($"# 送受信エラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                            throw;
                        }
                        //100ms待って再試行
                        await Task.Delay(100, ct);
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
                    MessageBox.Show($"# 受信時測定器オープンエラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    MessageBox.Show($"# 受信エラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    response = "受信失敗";
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
