using USBcommunication;             //CommUSB.cs
using UTility;                      //Utility.cs
using TemperatureCharacteristics.Exceptions;    //例外スローの為

namespace SOURCEcommunication
{
    public class SOURCEcomm
    {
        private static SOURCEcomm? instance;    //インスタンスをnull許容型(xxx?)として宣言し初期値がnullでも問題ないと表示
        private readonly USBcomm commSend;      //フィールド変数commSend
        private readonly USBcomm commQuery;     //フィールド変数commQuery
        private readonly UT utility;            //フィールド変数utility

        private SOURCEcomm()
        {
            commSend = USBcomm.Instance;        // コンストラクタで初期化
            commQuery = USBcomm.Instance;       // コンストラクタで初期化
            utility = UT.Instance;
        }
        public static SOURCEcomm Instance     //外部からのアクセス用
        {
            get
            {
                if (instance == null)
                    instance = new SOURCEcomm(); // クラス内でインスタンスを生成
                return instance;
            }
        }

        //*************************************************
        //アクセス：public
        //戻り値：<bool> 完了フラグ
        //機能：SOURCE Initial
        //　　　Reset→値設定
        //引数1：deviceList
        //説明：List<Device> 測定器設定リスト
        // Identifier:SOURCE1-4
        // UsbId:USB ID
        // InstName:信号名
        // TabSettings:tab名
        //引数2：tabItem
        //説明：<string> Tab名
        // Item1,Item2(デフォルトTab名)
        //コメント
        // 電源リセット（リセット完了まで待機）
        // 電源ファンクション設定
        // 初期値設定
        //*************************************************
        public async Task SOURCE_Initialize(List<Device> deviceList,string tabItem,CancellationToken cancellationToken = default)
        {
            //**********************************
            //チェックの入っている電源をリセット
            //リセット完了後初期値設定
            //**********************************
            foreach (Device device in deviceList)
            {
                cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
                try
                {
                    //**********************************
                    //定義
                    //device.TabSettings[TabSet]をSourceSettings型でキャストし
                    //成功すればSourceSettingsインスタンス、失敗すれば新たにインスタンス生成
                    //**********************************
                    SourceSettings sourceSettings = device.TabSettings[tabItem] as SourceSettings ?? new SourceSettings();
                    string sourceUSBID = device.UsbId;
                    string? sourceMode = sourceSettings.Mode;
                    string? sourceRange = sourceSettings.SourceRange;
                    double sourceValue = sourceSettings.SourceValue;
                    double sourceLimit = sourceSettings.SourceLimit;
                    //**********************************
                    //動作
                    // 電源リセット（リセット完了まで待機）
                    // 電源ファンクション設定
                    // 初期値設定
                    //**********************************
                    cancellationToken.ThrowIfCancellationRequested();       //リセット前にキャンセルチェック
                    await SOURCE_Reset(sourceUSBID, cancellationToken);

                    cancellationToken.ThrowIfCancellationRequested();       //各種設定前にキャンセルチェック
                    await SOURCE_Set_Function(sourceUSBID, sourceMode);
                    if (sourceLimit != 0)
                        await SOURCE_Set_Limit(sourceUSBID, sourceMode, sourceLimit);
                    await SOURCE_Set_Value(sourceUSBID, sourceValue, sourceRange);
                }
                catch (OperationCanceledException)
                {
                    throw;      //キャンセル要求を検知したら呼び出し元に通知
                }
                catch (MeasWarningException ex)
                {
                    throw new MeasWarningException($"# WARN:Source イニシャルエラー\n {ex.Message}");
                }
                catch (MeasFatalException ex)
                {
                    throw new MeasFatalException($"# FATAL:Source イニシャルエラー\n {ex.Message}");
                }
                catch (Exception ex)
                {
                    throw new MeasFatalException($"# UNKNOWN:Source イニシャルエラー\n {ex.Message}");
                }
            }
        }
        //*************************************************
        //アクセス：public
        //戻り値：なし
        //機能：SOURCE 出力ON
        //引数1：deviceList
        //説明：List<Device> 測定器設定リスト
        //引数2：tabItem
        //説明：<string> Tab名
        // Item1,Item2(デフォルトTab名)
        //コメント
        // SOURCE_OutputONの外部アクセス用
        //*************************************************
        public async Task SOURCE_OutputON(List<Device> deviceList, string tabItem, CancellationToken cancellationToken = default)
        {
            foreach (Device device in deviceList)
            {
                cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
                try
                {
                    //**********************************
                    //定義
                    //device.TabSettings[TabSet]をSourceSettings型でキャストし
                    //成功すればSourceSettingsインスタンス、失敗すれば新たにインスタンス生成
                    //**********************************
                    SourceSettings sourceSettings = device.TabSettings[tabItem] as SourceSettings ?? new SourceSettings();
                    string? sourceAct = sourceSettings.SourceAct;
                    string sourceUSBID = device.UsbId;
                    //**********************************
                    //動作
                    //**********************************
                    if(sourceAct != "NotUsed")
                        await SOURCE_Output_ON(sourceUSBID);
                }
                catch (OperationCanceledException)
                {
                    throw;      //キャンセル要求を検知したら呼び出し元に通知
                }
                catch (MeasWarningException ex)
                {
                    throw new MeasWarningException($"# WARN:Source OUTPUTonでエラー\n {ex.Message}\n");
                }
                catch (MeasFatalException ex)
                {
                    throw new MeasFatalException($"# FATAL:Source OUTPUTonでエラー\n {ex.Message}\n");
                }
                catch (Exception ex)
                {
                    throw new MeasFatalException($"# UNKNOWN:Source OUTPUTonでエラー\n {ex.Message}\n");
                }
            }
        }
        //*************************************************
        //アクセス：public
        //戻り値：なし
        //機能：SOURCE 出力OFF
        //引数1：deviceList
        //説明：List<Device> 測定器設定リスト
        //引数2：tabItem
        //説明：<string> Tab名
        //コメント
        // SOURCE_OutputOFFの外部アクセス用
        //*************************************************
        public async Task SOURCE_OutputOFF(List<Device> deviceList, string tabItem, CancellationToken cancellationToken = default)
        {
            foreach (Device device in deviceList)
            {
                cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
                try
                {
                    //**********************************
                    //定義
                    //**********************************
                    string sourceUSBID = device.UsbId;
                    //**********************************
                    //動作
                    //**********************************
                    await SOURCE_Output_OFF(sourceUSBID);
                }
                catch (OperationCanceledException)
                {
                    throw;      //キャンセル要求を検知したら呼び出し元に通知
                }
                catch (MeasWarningException ex)
                {
                    throw new MeasWarningException($"# WARN:Source OUTPUToffでエラー\n {ex.Message}\n");
                }
                catch (MeasFatalException ex)
                {
                    throw new MeasFatalException($"# FATAL:Source OUTPUToffでエラー\n {ex.Message}\n");
                }
                catch (Exception ex)
                {
                    throw new MeasFatalException($"# UNKNOWN:Source OUTPUToffでエラー\n {ex.Message}\n");
                }
            }
        }
        //*************************************************
        //アクセス：public
        //戻り値：なし
        //機能：SOURCE 出力値設定
        //引数1：deviceList
        //説明：List<Device> 測定器設定リスト
        //引数2：tabItem
        //説明：<string> Tab名
        //コメント
        // SOURCE_Set_Valueの外部アクセス用
        //*************************************************
        public async Task SOURCE_SetValue(List<Device> deviceList, string tabItem, CancellationToken cancellationToken = default)
        {
            foreach (Device device in deviceList)
            {
                cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
                try
                {
                    //**********************************
                    //定義
                    //device.TabSettings[TabSet]をSourceSettings型でキャストし
                    //成功すればSourceSettingsインスタンス、失敗すれば新たにインスタンス生成
                    //**********************************
                    SourceSettings sourceSettings = device.TabSettings[tabItem] as SourceSettings ?? new SourceSettings();
                    string sourceUSBID = device.UsbId;
                    string? sourceRange = sourceSettings.SourceRange;
                    double sourceValue = sourceSettings.SourceValue;
                    //**********************************
                    //動作
                    //**********************************
                    await SOURCE_Set_Value(sourceUSBID, sourceValue, sourceRange);
                }
                catch (OperationCanceledException)
                {
                    throw;      //キャンセル要求を検知したら呼び出し元に通知
                }
                catch (MeasWarningException ex)
                {
                    throw new MeasWarningException($"# WARN:Source SetValueでエラー\n {ex.Message}\n");
                }
                catch (MeasFatalException ex)
                {
                    throw new MeasFatalException($"# FATAL:Source SetValueでエラー\n {ex.Message}\n");
                }
                catch (Exception ex)
                {
                    throw new MeasFatalException($"# UNKNOWN:Source SetValueでエラー\n {ex.Message}\n");
                }
            }
            
        }
        //*************************************************
        //アクセス：public
        //戻り値：なし(Task)
        //機能：SOURCE Remote解除
        //引数1：deviceList
        //説明：List<Device> 測定器設定リスト
        //コメント
        // SOURCEのリモート解除用
        //*************************************************
        public async Task SOURCE_RemoteOFF(List<Device> deviceList)
        {
            foreach (Device device in deviceList)
            {
                try
                {
                    //**********************************
                    //定義
                    //**********************************
                    string sourceUSBID = device.UsbId;
                    //**********************************
                    //動作
                    //**********************************
                    await commSend.Remote_OFF(sourceUSBID);
                }
                catch (MeasWarningException ex)
                {
                    throw new MeasWarningException($"# WARN:Source リモート解除エラー\n {ex.Message}\n", ex);
                }
                catch (MeasFatalException ex)
                {
                    throw new MeasFatalException($"# FATAL:Source リモート解除エラー\n {ex.Message}\n", ex);
                }
                catch (Exception ex)
                {
                    //MessageBox.Show($"# Sourceリモート解除でエラー: {ex.Message}");
                    throw new MeasFatalException($"# UNKNOWN:Source リモート解除エラー\n {ex.Message}\n", ex);
                }
            }

        }

//以下privateアクセス**************************************************************************************************
        //*************************************************
        //アクセス：private
        //戻り値：<bool> 完了フラグ
        //機能：SOURCE 直前コマンド完了チェック
        //　　　直前コマンド処理完了後True、処理中はFalse
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //コメント
        // 使用するときはawait演算子を付けて呼び出し
        //*************************************************
        private async Task<bool> Complete_Check(string usbid, CancellationToken ct, int maxWaitMs = 10000)
        {
            bool compflag = false;
            var sw = System.Diagnostics.Stopwatch.StartNew();       //通信ハング時のタイムアウト用タイマー
            try
            {
                while (!compflag && sw.ElapsedMilliseconds < maxWaitMs)
                {
                    ct.ThrowIfCancellationRequested();
                    string responce = await commQuery.Comm_queryB(usbid, "*OPC?", ct); //標準イベントレジスタを読み込み リモート解除無し
                    byte status = Convert.ToByte(responce);
                    compflag = (status & 0x01) == 1;                        //標準イベントレジスタbit0が1かどうか(OPC直前に投げたコマンドが完了したかどうか)
                    if (!compflag)
                        await utility.Wait_Timer(10, ct);                     //10ms wait
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"# Sourceコマンド完了チェックでエラー: {ex.Message}");
                return false;                                               // エラー時はfalseを返す
            }
            return compflag;
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし(Task)
        //機能：SOURCE リセット
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //コメント
        // 使用するときはawait演算子を付けて呼び出し
        //*************************************************
        private async Task SOURCE_Reset(string usbid, CancellationToken ct)
        {
            string command = "*RST";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
            bool comp = await Complete_Check(usbid, ct);                  //直前コマンド完了チェック
            if (!comp)
                throw new MeasWarningException("# WARN:Source リセット失敗\n");
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：SOURCE ファンクション設定
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：function
        //説明：<string> SOURCE 出力ファンクション設定
        // "VOLT" or "CURR" 
        //コメント
        //*************************************************
        private async Task SOURCE_Set_Function(string usbid, string function)
        {
            string command = $":SOUR:FUNC {function}";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：SOURCE 出力値設定
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：value
        //説明：<double> SOURCE 出力値(V/A)
        // 0.00000～6.000
        //以下省略可能
        //引数3：range
        //説明：<string> SOURCE レンジ設定
        // 100uA/100mV等
        //コメント
        // rangeは省略した場合AUTO設定
        //*************************************************
        private async Task SOURCE_Set_Value(string usbid, double value, string range = "AUTO")
        {
            string command;
            if (range != "AUTO")
                command = $":SOUR:RANG {range};LEV {value}";
            else
                command = $":SOUR:LEV:AUTO {value}";

            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：SOURCE リミット値設定
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：function
        //説明：<string> SOURCE 出力ファンクション設定
        // "VOLT" or "CURR" 
        //引数2：value
        //説明：<double> SOURCE リミット値(V/A)
        // 0.00000～6.000
        //*************************************************
        private async Task SOURCE_Set_Limit(string usbid, string function, double value)
        {
            string command;
            if (function == "VOLT")
                command = $":SOUR:PROT:CURR {value}";
            else
                command = $":SOUR:PROT:VOLT {value}";

            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }
        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：SOURCE 出力ON
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //*************************************************
        private async Task SOURCE_Output_ON(string usbid)
        {
            string command = ":OUTP ON";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：SOURCE 出力OFF
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //*************************************************
        private async Task SOURCE_Output_OFF(string usbid)
        {
            string command = ":OUTP OFF";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

    }
}

