using System.Text;
using System.Windows;
using USBcommunication;             //CommUSB.cs
using UTility;                      //Utility.cs
using TemperatureCharacteristics.Exceptions;    //例外スローの為

namespace MEASUREcommunication
{
    public class MEASUREcomm
    {
        private static MEASUREcomm? instance;       //インスタンスをnull許容型(xxx?)として宣言し初期値がnullでも問題ないと表示
        private readonly USBcomm commSend;          //フィールド変数commSend
        private readonly USBcomm commQuery;         //フィールド変数commQuery
        private readonly UT utility;                //フィールド変数utility

        private MEASUREcomm()
        {
            commSend = USBcomm.Instance;        // コンストラクタで初期化
            commQuery = USBcomm.Instance;       // コンストラクタで初期化
            utility = UT.Instance;
        }
        public static MEASUREcomm Instance     //外部からのアクセス用
        {
            get
            {
                if (instance == null)
                    instance = new MEASUREcomm(); // クラス内でインスタンスを生成
                return instance;
            }
        }

        //*************************************************
        //アクセス：public
        //戻り値：<bool> 完了フラグ
        //機能：DMM Initialize
        //　　　Reset→初期設定
        //引数1：deviceList
        //説明：List<Device> 測定器設定リスト
        // Identifier:DMM1-4
        // UsbId:USB ID
        // InstName:信号名
        // TabSettings:tab名
        //引数2：tabItem
        //説明：<string> Tab名
        // Item1,Item2(デフォルトTab名)
        //コメント
        // DMMリセット（リセット完了まで待機）
        // 測定ファンクション設定
        // 測定レンジ設定
        // NPLC設定
        // トリガ設定
        // Display設定
        //*************************************************
        public async Task MEASURE_Initialize(List<Device> deviceList, string tabItem, CancellationToken cancellationToken = default)
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
                    //device.TabSettings[TabSet]をDmmSettings型でキャストし
                    //成功すればDmmSettingsインスタンス、失敗すれば新たにインスタンス生成
                    //**********************************
                    DmmSettings dmmSettings = device.TabSettings[tabItem] as DmmSettings ?? new DmmSettings();
                    string dmmUSBID = device.UsbId;
                    string? dmmMode = dmmSettings.Mode;
                    string? dmmRange = dmmSettings.DmmRange;
                    string? dmmPlc = dmmSettings.Plc;
                    string? dmmTrigSrc = dmmSettings.TriggerSource;
                    bool dmmDisplayOn = dmmSettings.DisplayOn;
                    //**********************************
                    //動作
                    // DMMリセット（リセット完了まで待機）
                    // 測定ファンクション設定
                    // 測定レンジ設定
                    // NPLC設定
                    // トリガ設定
                    // Display設定
                    //**********************************
                    cancellationToken.ThrowIfCancellationRequested();       //リセット前にキャンセルチェック
                    await MEASURE_Reset(dmmUSBID, cancellationToken);

                    cancellationToken.ThrowIfCancellationRequested();       //各種設定前にキャンセルチェック
                    await MEASURE_Set_Function(dmmUSBID, dmmMode);
                    await MEASURE_Set_Range(dmmUSBID, dmmMode, dmmRange);
                    await MEASURE_Set_NPLC(dmmUSBID, dmmMode, float.Parse(dmmPlc));
                    await MEASURE_Set_TrggerSource(dmmUSBID, dmmTrigSrc);
                    if (!dmmDisplayOn)
                        await MEASURE_Display(dmmUSBID, "OFF");
                    if (dmmTrigSrc != "IMM")
                        await MEASURE_Set_Standby(dmmUSBID);          //トリガ待機状態に遷移
                }
                catch (OperationCanceledException)
                {
                    throw;      //キャンセル要求を検知したら呼び出し元に通知
                }
                catch (MeasWarningException ex)
                {
                    throw new MeasWarningException($"# WARN:DMM Initializeでエラー: {ex.Message}\n");
                }
                catch (MeasFatalException ex)
                {
                    throw new MeasFatalException($"# FATAL:DMM Initializeでエラー: {ex.Message}\n");
                }
                catch (Exception ex)
                {
                    throw new MeasFatalException($"# UNKNOWN:DMM Initializeでエラー: {ex.Message}\n");
                }
            }
        }
        //*************************************************
        //アクセス：public
        //戻り値：なし
        //機能：DMM 測定スタンバイ状態に遷移
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //コメント
        // MEASURE_Set_Standby外部アクセス用
        // トリガソース設定→INITでWaitTrigger状態に遷移→*TRG→FETC?
        //*************************************************
        public async Task MEASURE_SetStandby(List<Device> deviceList, string tabItem, CancellationToken cancellationToken = default)
        {
            foreach (Device device in deviceList)
            {
                cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
                try
                {
                    //**********************************
                    //定義
                    //**********************************
                    string dmmUSBID = device.UsbId;
                    //**********************************
                    //動作
                    // DMM トリガ受付待機へ遷移
                    //**********************************
                    await MEASURE_Set_Standby(dmmUSBID);
                }
                catch (OperationCanceledException)
                {
                    throw;      //キャンセル要求を検知したら呼び出し元に通知
                }
                catch (MeasWarningException ex)
                {
                    throw new MeasWarningException($"# WARN:DMM トリガ受付待機遷移でエラー: {ex.Message}\n");
                }
                catch (MeasFatalException ex)
                {
                    throw new MeasFatalException($"# FATAL:DMM トリガ受付待機遷移でエラー: {ex.Message}\n");
                }
                catch (Exception ex)
                {
                    throw new MeasFatalException($"# UNKNOWN:DMM トリガ受付待機遷移でエラー: {ex.Message}\n");
                }
            }
        }
        //*************************************************
        //アクセス：public
        //戻り値：なし
        //機能：DMM バストリガ発生
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //コメント
        // MEASURE_Bus_Trigger外部アクセス用
        // トリガソース設定→INITでWaitTrigger状態に遷移→*TRG→FETC?
        // リモートインターフェースを経由してトリガ発生
        //*************************************************
        public async Task MEASURE_BusTrigger(List<Device> deviceList, string tabItem, CancellationToken cancellationToken = default)
        {
            foreach (Device device in deviceList)
            {
                cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
                try
                {
                    //**********************************
                    //定義
                    //**********************************
                    string dmmUSBID = device.UsbId;
                    //**********************************
                    //動作
                    // DMM BUSトリガ発生
                    //**********************************
                    await MEASURE_Bus_Trigger(dmmUSBID);
                }
                catch (OperationCanceledException)
                {
                    throw;      //キャンセル要求を検知したら呼び出し元に通知
                }
                catch (MeasWarningException ex)
                {
                    throw new MeasWarningException($"# WARN:DMM BUSトリガ発生でエラー: {ex.Message}\n");
                }
                catch (MeasFatalException ex)
                {
                    throw new MeasFatalException($"# FATAL:DMM BUSトリガ発生でエラー: {ex.Message}\n");
                }
                catch (Exception ex)
                {
                    throw new MeasFatalException($"# UNKNOWN:DMM BUSトリガ発生でエラー: {ex.Message}\n");
                }
            }
        }
        //*************************************************
        //アクセス：public
        //戻り値：<StringBuilder> DMM Data
        //機能：DMM 測定データ取得
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //コメント
        // MEASURE_Read_A外部アクセス用
        // トリガソース設定→INITでWaitTrigger状態に遷移→*TRG→FETC?
        //*************************************************
        public async Task<StringBuilder> MEASURE_ReadData(List<Device> deviceList, string tabItem, CancellationToken cancellationToken = default)
        {
            StringBuilder Data = new StringBuilder();
            foreach (Device device in deviceList)
            {
                cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
                try
                {
                    //**********************************
                    //定義
                    //**********************************
                    string dmmUSBID = device.UsbId;
                    //**********************************
                    //動作
                    // DMM data出力
                    //**********************************
                    string result = await MEASURE_Read_A(dmmUSBID, cancellationToken);
                    Data.AppendLine(result);
                }
                catch (OperationCanceledException)
                {
                    throw;      //キャンセル要求を検知したら呼び出し元に通知
                }
                catch (MeasWarningException ex)
                {
                    throw new MeasWarningException($"# WARN:DMM Data読み取りでエラー: {ex.Message}\n");
                }
                catch (MeasFatalException ex)
                {
                    throw new MeasFatalException($"# FATAL:DMM Data読み取りでエラー: {ex.Message}\n");
                }
                catch (Exception ex)
                {
                    throw new MeasFatalException($"# UNKNOWN:DMM Data読み取りでエラー: {ex.Message}\n");
                }
            }
            return Data;
        }
        //*************************************************
        //アクセス：public
        //戻り値：<StringBuilder> DMM Data
        //機能：DMM 測定データ取得
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //コメント
        // MEASURE_Read_B外部アクセス用
        // トリガソース設定→INITでWaitTrigger状態に遷移→*TRG→FETC?
        //*************************************************
        public async Task<StringBuilder> MEASURE_Data(List<Device> deviceList, string tabItem, CancellationToken cancellationToken = default)
        {
            StringBuilder Data = new StringBuilder();
            foreach (Device device in deviceList)
            {
                cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
                try
                {
                    //**********************************
                    //定義
                    //**********************************
                    string dmmUSBID = device.UsbId;
                    //**********************************
                    //動作
                    // DMM data出力
                    //**********************************
                    string result = await MEASURE_Read_B(dmmUSBID, cancellationToken);
                    Data.AppendLine(result);
                }
                catch (OperationCanceledException)
                {
                    throw;      //キャンセル要求を検知したら呼び出し元に通知
                }
                catch (MeasWarningException ex)
                {
                    throw new MeasWarningException($"# WARN:DMM Data読み取りでエラー: {ex.Message}\n");
                }
                catch (MeasFatalException ex)
                {
                    throw new MeasFatalException($"# FATAL:DMM Data読み取りでエラー: {ex.Message}\n");
                }
                catch (Exception ex)
                {
                    throw new MeasFatalException($"# UNKNOWN:DMM Data読み取りでエラー: {ex.Message}\n");
                }
            }
            return Data;
        }
        //*************************************************
        //アクセス：public
        //戻り値：なし
        //機能：DMM ディスプレイ表示ON/OFF
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //*************************************************
        public async Task MEASURE_Disp(List<Device> deviceList, string tabItem, CancellationToken cancellationToken = default)
        {
            foreach (Device device in deviceList)
            {
                cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
                try
                {
                    //**********************************
                    //定義
                    //device.TabSettings[TabSet]をDmmSettings型でキャストし
                    //成功すればDmmSettingsインスタンス、失敗すれば新たにインスタンス生成
                    //**********************************
                    DmmSettings dmmSettings = device.TabSettings[tabItem] as DmmSettings ?? new DmmSettings();
                    string dmmUSBID = device.UsbId;
                    bool dmmDisplayOn = dmmSettings.DisplayOn;
                    //**********************************
                    //動作
                    // DMM ディスプレイ表示切替
                    //**********************************
                    if (!dmmDisplayOn)
                        await MEASURE_Display(dmmUSBID, "OFF");
                    else
                        await MEASURE_Display(dmmUSBID, "ON");
                }
                catch (OperationCanceledException)
                {
                    throw;      //キャンセル要求を検知したら呼び出し元に通知
                }
                catch (MeasWarningException ex)
                {
                    throw new MeasWarningException($"# WARN:DMM BUSトリガ発生でエラー: {ex.Message}\n");
                }
                catch (MeasFatalException ex)
                {
                    throw new MeasFatalException($"# WARN:DMM BUSトリガ発生でエラー: {ex.Message}\n");
                }
                catch (Exception ex)
                {
                    throw new MeasFatalException($"# UNKNOWN:DMM BUSトリガ発生でエラー: {ex.Message}\n");
                }
            }
        }
        //*************************************************
        //アクセス：public
        //戻り値：なし(Task)
        //機能：DMM Remote解除
        //引数1：deviceList
        //説明：List<Device> 測定器設定リスト
        //コメント
        // DMMのリモート解除用
        //*************************************************
        public async Task MEASURE_RemoteOFF(List<Device> deviceList)
        {
            foreach (Device device in deviceList)
            {
                try
                {
                    //**********************************
                    //定義
                    //**********************************
                    string dmmUSBID = device.UsbId;
                    //**********************************
                    //動作
                    //**********************************
                    await commSend.Remote_OFF(dmmUSBID);
                }
                catch (MeasWarningException ex)
                {
                    throw new MeasWarningException($"# WARN:DMM リモート解除でエラー: {ex.Message}\n");
                }
                catch (MeasFatalException ex)
                {
                    throw new MeasFatalException($"# FATAL: MMリモート解除でエラー: {ex.Message}\n");
                }
                catch (Exception ex)
                {
                    throw new MeasFatalException($"# UNKNOWN:DMM リモート解除でエラー: {ex.Message}\n");
                }
            }

        }

//以下privateアクセス**************************************************************************************************
        //*************************************************
        //アクセス：private
        //戻り値：<bool> 完了フラグ
        //機能：DMM 直前コマンド完了チェック
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
                    string responce = await commQuery.Comm_queryB(usbid, "*OPC?", ct); //標準イベントレジスタを読み込み リモート解除なし
                    byte status = Convert.ToByte(responce);
                    compflag = (status & 0x01) == 1;                        //標準イベントレジスタbit0が1かどうか(OPC直前に投げたコマンドが完了したかどうか)
                    if (!compflag)
                        await utility.Wait_Timer(10, ct);                     //10ms wait
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"# Measureコマンド完了チェックでエラー: {ex.Message}");
                return false;                                               // エラー時はfalseを返す
            }
            return compflag;
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし(Task)
        //機能：DMM リセット
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB 
        //*************************************************
        private async Task MEASURE_Reset(string usbid, CancellationToken ct)
        {
            string command = "*RST";
            await commSend.Comm_sendB(usbid, command);      //リモート解除を無効にして送信
            bool comp = await Complete_Check(usbid, ct);        //直前コマンド完了チェック
            if (!comp)
                throw new MeasWarningException("# WARN:DMM リセット失敗\n");
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：DMM 測定ファンクション設定(暫定DCのみ)
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：function
        //説明：<string> DMM 測定ファンクション設定
        // "VOLT" or "CURR" 
        //*************************************************
        private async Task MEASURE_Set_Function(string usbid, string function)
        {
            string command = $"SENS:FUNC '{function}:DC'";
            await commSend.Comm_sendB(usbid, command);      //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：DMM 測定レンジ設定(暫定DCのみ)
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：function
        //説明：<string> DMM 測定ファンクション設定
        // "VOLT" or "CURR" 
        //以下省略可能
        //引数3：range
        //説明：<string> DMM 測定レンジ設定
        // 100uA/100mV等
        //コメント
        // rangeは省略した場合AUTO設定
        //*************************************************
        private async Task MEASURE_Set_Range(string usbid, string function, string range="AUTO")
        {
            string command;
            if (range != "AUTO")
                command = $"SENS:{function}:DC:RANG {range}";
            else
                command = $"SENS:{function}:DC:RANG:AUTO ON";
            await commSend.Comm_sendB(usbid, command);      //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：DMM NPLC設定(暫定DCのみ)
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：function
        //説明：<string> DMM 測定function設定
        // "VOLT" or "CURR" 
        //引数3：nplc
        //説明：<float> DMM NPLC設定
        // 0.02/0.2/1/10/100
        //*************************************************
        private async Task MEASURE_Set_NPLC(string usbid, string function, float nplc)
        {
            string command = $"SENS:{function}:DC:NPLC {nplc}";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：DMM トリガソース設定
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：source
        //説明：<string> トリガソース設定
        // "IMM" or "BUS" or "EXT"
        //コメント
        // DMMが複数台あり、ほぼ同時測定する場合に有効
        // トリガソース設定→INITでWaitTrigger状態に遷移→*TRG→FETC?
        // IMM:内部トリガ BUS:リモート経由 EXT:外部トリガ
        //*************************************************
        private async Task MEASURE_Set_TrggerSource(string usbid, string source)
        {
            string command = $"TRIG:SOUR {source}";     //IMM:内部トリガ BUS:リモート経由*TRG使用 EXT:外部トリガ
            await commSend.Comm_sendB(usbid, command);  //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：DMM WaitTrigger状態に遷移
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //コメント
        // DMMが複数台あり、ほぼ同時測定する場合に有効
        // トリガソース設定→INITでWaitTrigger状態に遷移→*TRG→FETC?
        // トリガソースがIMMになっていると即時測定
        // INITコマンド後リモート解除するとトリガ待機状態が解除されるため
        // リモート解除を無効にして送信
        //*************************************************
        private async Task MEASURE_Set_Standby(string usbid)
        {
            string command = "INIT";            //トリガ待機状態に遷移
            await commSend.Comm_sendB(usbid, command);    //リモート解除を無効にして送信
            await Complete_Check(usbid, CancellationToken.None);    //コマンド完了待ち

        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：DMM バストリガ発生
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //コメント
        // DMMが複数台あり、ほぼ同時測定する場合に有効
        // トリガソース設定→INITでWaitTrigger状態に遷移→*TRG→FETC?
        // リモートインターフェースを経由してトリガ発生
        //*************************************************
        private async Task MEASURE_Bus_Trigger(string usbid)
        {
            string command = "*TRG";
            await commSend.Comm_sendB(usbid, command);      //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：<string> DMM測定値
        //機能：DMM 1回測定して測定値を返す
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //コメント
        // DMMが複数台あり、ほぼ同時測定する場合に有効
        // トリガソース設定→INITでWaitTrigger状態に遷移→*TRG→FETC?
        // FETC?"は測定メモリ内のデータを返すため、新規測定コマンドではない
        //*************************************************
        private async Task<string> MEASURE_Read_A(string usbid, CancellationToken ct)
        {
            await Complete_Check(usbid, ct);    //コマンド完了待ち
            string command = "FETC?";           //測定メモリ内データをバッファに渡す
            string responce = await commQuery.Comm_queryB(usbid, command, ct);          //リモート解除なし
            return responce;
        }

        //*************************************************
        //アクセス：private
        //戻り値：<string> DMM測定値
        //機能：DMM 1回測定して測定値を返す
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //*************************************************
        private async Task<string> MEASURE_Read_B(string usbid, CancellationToken ct)
        {
            await Complete_Check(usbid, ct);    //コマンド完了待ち
            string command = "READ?";           //1回測定して測定値をバッファに渡す
            string responce = await commQuery.Comm_queryB(usbid, command, ct);      //リモート解除なし
            return responce;
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：DMM ディスプレイON/OFF
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：sw
        //説明：<string> ディスプレイON/OFF
        // "ON" or "OFF"
        //*************************************************
        private async Task MEASURE_Display(string usbid, string sw)
        {
            string command = $"DISP:STAT {sw}";
            await commSend.Comm_sendB(usbid, command);      //リモート解除を無効にして送信
        }
    }
}

