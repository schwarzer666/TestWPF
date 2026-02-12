using USBcommunication;             //CommUSB.cs
using UTility;                      //Utility.cs
using TemperatureCharacteristics.Exceptions;    //例外スローの為

namespace OSCcommunication
{
    public class OSCcomm
    {
        private static OSCcomm? instance;       //インスタンスをnull許容型(xxx?)として宣言し初期値がnullでも問題ないと表示
        private readonly USBcomm commSend;      //フィールド変数commSend
        private readonly USBcomm commQuery;     //フィールド変数commQuery
        private readonly UT utility;            //フィールド変数utility

        private OSCcomm()
        {
            commSend = USBcomm.Instance;        //コンストラクタで初期化
            commQuery = USBcomm.Instance;       //コンストラクタで初期化
            utility = UT.Instance;
        }
        public static OSCcomm Instance     //外部からのアクセス用
        {
            get
            {
                if (instance == null)
                    instance = new OSCcomm(); // クラス内でインスタンスを生成
                return instance;
            }
        }

        //*************************************************
        //アクセス：public
        //戻り値：<bool> 完了フラグ
        //機能：OSC Initial
        //　　　Reset→値設定
        //引数1：deviceList
        //説明：List<Device> 測定器設定リスト
        // Identifier:OSC
        // UsbId:USB ID
        // TabSettings:tab名
        //引数2：tabItem
        //説明：<string> Tab名
        // Item1,Item2(デフォルトTab名)
        //コメント
        // OSCリセット（リセット完了まで待機）
        // レコード長設定
        // 各CH設定
        // 時間軸設定
        // トリガ設定
        //*************************************************
        public async Task OSC_Initialize(List<Device> deviceList, string tabItem, CancellationToken cancellationToken = default)
        {
            //**********************************
            //チェックの入っているOSCをリセット
            //リセット完了後初期設定
            //OSC2台使用の展開も考えてforeach文はそのまま
            //**********************************
            foreach (Device device in deviceList)
            {
                cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
                try
                {
                    //**********************************
                    //定義
                    //device.TabSettings[TabSet]をOscSettings型でキャストし
                    //成功すればOscSettingsインスタンス、失敗すれば新たにインスタンス生成
                    //**********************************
                    OscSettings oscSettings = device.TabSettings[tabItem] as OscSettings ?? new OscSettings();
                    string oscUSBID = device.UsbId;
                    string[]? oscCh = oscSettings.ChannelSettings;
                    string? oscTrigSource = oscSettings.TriggerSource;
                    string? oscTrigDir = oscSettings.TriggerDirection;
                    string? oscTrigLev = oscSettings.TriggerLevel;
                    uint recordLength = 1250000;                        //暫定RecordLength 1.25Mpoints
                    string[]? oscTime = oscSettings.TimeSettings;
                    string? oscTrange = oscTime[0];
                    string oscTpos = oscTime[1];
                    //**********************************
                    //動作
                    // OSCリセット（リセット完了まで待機）
                    // レコード長設定
                    // 各CH設定
                    // 時間軸設定
                    // トリガ設定
                    //**********************************
                    cancellationToken.ThrowIfCancellationRequested();       //リセット前にキャンセルチェック
                    await OSC_Reset(oscUSBID, cancellationToken);

                    cancellationToken.ThrowIfCancellationRequested();       //各種設定前にキャンセルチェック
                    await OSC_Set_ACQ(oscUSBID, recordLength);
                    for (int ch = 1; ch <= 4; ch++)
                    {
                        int baseIndex = (ch - 1) * 2;
                        if (baseIndex + 1 >= oscCh.Length) continue;

                        string chRange = oscCh[baseIndex];
                        string chPos = oscCh[baseIndex + 1];

                        await OSC_Set_CHRange(oscUSBID, ch.ToString(), chRange);
                        await OSC_Set_CHPosition(oscUSBID, ch.ToString(), float.Parse(chPos));
                    }
                    await OSC_Set_TRange(oscUSBID, oscTrange);
                    await OSC_Set_TPosition(oscUSBID, float.Parse(oscTpos));
                    await OSC_Set_Trigger(oscUSBID, oscTrigSource, oscTrigDir, float.Parse(oscTrigLev));
                }
                catch (OperationCanceledException)
                {
                    throw;      //キャンセル要求を検知したら呼び出し元に通知
                }
                catch (MeasWarningException ex)
                {
                    throw new MeasWarningException($"# WARN:OSC Initializeでエラー: {ex.Message}\n");
                }
                catch (MeasFatalException ex)
                {
                    throw new MeasFatalException($"# FATAL:OSC Initializeでエラー: {ex.Message}\n");
                }
                catch (Exception ex)
                {
                    throw new MeasFatalException($"# UNKNOWN:OSC Initializeでエラー: {ex.Message}\n");
                }
            }
        }
        //*************************************************
        //アクセス：public
        //戻り値：なし
        //機能：OSC CH表示OFF(CH5～CH8)
        //引数1：deviceList
        //説明：List<Device> 測定器設定リスト
        // Identifier:OSC
        // UsbId:USB ID
        // TabSettings:tab名
        //引数2：tabItem
        //説明：<string> Tab名
        // Item1,Item2(デフォルトTab名)
        //コメント
        //*************************************************
        public async Task<bool> OSCUnusedChOFF(List<Device> deviceList, string tabItem, CancellationToken cancellationToken = default)
        {
            bool compflag = true;
            //**********************************
            //チェックの入っているOSCをStop
            //OSC2台使用の展開も考えてforeach文はそのまま
            //**********************************
            foreach (Device device in deviceList)
            {
                cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
                //**********************************
                //定義
                //OSCが一台しかない場合foreach文は以下に置き換わる
                //Device device = deviceList.FirstOrDefault();
                //if (device == null)
                //{
                //    MessageBox.Show("OSC デバイスが見つかりませんでした");
                //    return false;
                //}
                //**********************************
                try
                {
                    //**********************************
                    //定義
                    //device.TabSettings[TabSet]をOscSettings型でキャストし
                    //成功すればOscSettingsインスタンス、失敗すれば新たにインスタンス生成
                    //**********************************
                    OscSettings oscSettings = device.TabSettings[tabItem] as OscSettings ?? new OscSettings();
                    string oscUSBID = device.UsbId;

                    cancellationToken.ThrowIfCancellationRequested();   //各種設定前にキャンセルチェック

                    for (int ch = 5; ch <= 8; ch++)
                    {
                        await OSC_CH_Display(oscUSBID, ch.ToString(), "OFF");
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;      //キャンセル要求を検知したら呼び出し元に通知
                }
                catch (MeasWarningException ex)
                {
                    throw new MeasWarningException($"# WARN:OSC 未使用CH表示OFFでエラー: {ex.Message}\n");
                }
                catch (MeasFatalException ex)
                {
                    throw new MeasFatalException($"# FATAL:OSC 未使用CH表示OFFでエラー: {ex.Message}\n");
                }
                catch (Exception ex)
                {
                    throw new MeasFatalException($"# UNKNOWN:OSC 未使用CH表示OFFでエラー: {ex.Message}\n");
                }
            }
            return compflag;
        }

        //*************************************************
        //アクセス：public
        //戻り値：なし
        //機能：OSC Delay測定設定
        //引数1：deviceList
        //説明：List<Device> 測定器設定リスト
        // Identifier:OSC
        // UsbId:USB ID
        // TabSettings:tab名
        //引数2：tabItem
        //説明：<string> Tab名
        // Item1,Item2(デフォルトTab名)
        //コメント
        //*************************************************
        public async Task<bool> OSCmeasureSet(List<Device> deviceList, string tabItem, CancellationToken cancellationToken = default)
        {
            bool compflag = true;
            //**********************************
            //チェックの入っているOSCをStop
            //OSC2台使用の展開も考えてforeach文はそのまま
            //**********************************
            foreach (Device device in deviceList)
            {
                cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
                //**********************************
                //定義
                //OSCが一台しかない場合foreach文は以下に置き換わる
                //Device device = deviceList.FirstOrDefault();
                //if (device == null)
                //{
                //    MessageBox.Show("OSC デバイスが見つかりませんでした");
                //    return false;
                //}
                //**********************************
                try
                {
                    //**********************************
                    //定義
                    //device.TabSettings[TabSet]をOscSettings型でキャストし
                    //成功すればOscSettingsインスタンス、失敗すれば新たにインスタンス生成
                    //**********************************
                    OscSettings oscSettings = device.TabSettings[tabItem] as OscSettings ?? new OscSettings();
                    string oscUSBID = device.UsbId;
                    string? measureCh = oscSettings.DelaySetupCh;     //Delay設定CH(measure設定CH)
                    string? polarity = oscSettings.Polarity;          //measure極性
                    string? refCh = oscSettings.RefCh;                //measure reference(今回未使用)
                    float tRange1 = oscSettings.TRange1;              //measure範囲始点
                    float tRange2 = oscSettings.TRange2;              //measure範囲終点

                    cancellationToken.ThrowIfCancellationRequested();       //各種設定前にキャンセルチェック

                    await OSC_Set_MeasureONOFF(oscUSBID, "ON");                     //自動測定機能ON
                    await OSC_Set_DelayIndicatorON(oscUSBID, measureCh);            //インジケーターON
                    await OSC_Set_MeasureDelayON(oscUSBID, measureCh);              //measureDelayの情報表示
                    await OSC_Set_MeasureDelay_REF(oscUSBID, measureCh);                //measureDelayのRefCH設定
                    await OSC_Set_MeasureDelay_MEAS(oscUSBID, measureCh, polarity); //measureDelayの測定対象CH設定
                    await OSC_Set_MeasureDelay_TRange(oscUSBID, tRange1, tRange2);  //measureDelayの範囲設定
                }
                catch (OperationCanceledException)
                {
                    throw;      //キャンセル要求を検知したら呼び出し元に通知
                }
                catch (MeasWarningException ex)
                {
                    throw new MeasWarningException($"# WARN:OSC DelayMeasure設定でエラー: {ex.Message}\n");
                }
                catch (MeasFatalException ex)
                {
                    throw new MeasFatalException($"# FATAL:OSC DelayMeasure設定でエラー: {ex.Message}\n");
                }
                catch (Exception ex)
                {
                    throw new MeasFatalException($"# UNKNOWN:OSC DelayMeasure設定でエラー: {ex.Message}\n");
                }
            }
            return compflag;
        }
        //*************************************************
        //アクセス：public
        //戻り値：<string> Delay Data
        //機能：OSC Delay測定データ取得
        //引数1：deviceList
        //説明：List<Device> 測定器設定リスト
        // Identifier:OSC
        // UsbId:USB ID
        // TabSettings:tab名
        //引数2：tabItem
        //説明：<string> Tab名
        // Item1,Item2(デフォルトTab名)
        //*************************************************
        public async Task<string> OSCmeasureDelay(List<Device> deviceList, string tabItem, CancellationToken cancellationToken = default)
        {
            string Data = "";
            foreach (Device device in deviceList)
            {
                cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
                try
                {
                    //**********************************
                    //定義
                    //device.TabSettings[TabSet]をOscSettings型でキャストし
                    //成功すればOscSettingsインスタンス、失敗すれば新たにインスタンス生成
                    //**********************************
                    OscSettings oscSettings = device.TabSettings[tabItem] as OscSettings ?? new OscSettings();
                    string oscUSBID = device.UsbId;
                    //DelaySettings? delaySettings = device.TabSettings[tabItem] as DelaySettings ?? new DelaySettings();
                    //string? measureCh = delaySettings.DelaySetupCh;     //Delay設定CH(measure設定CH)
                    string? measureCh = oscSettings.DelaySetupCh;     //Delay設定CH(measure設定CH)

                    Data = await OSC_Read_MeasureDelay(oscUSBID, measureCh, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;      //キャンセル要求を検知したら呼び出し元に通知
                }
                catch (MeasWarningException ex)
                {
                    throw new MeasWarningException($"# WARN:OSC MeasureDelay読み取りでエラー: {ex.Message}\n");
                }
                catch (MeasFatalException ex)
                {
                    throw new MeasFatalException($"# FATAL:OSC MeasureDelay読み取りでエラー: {ex.Message}\n");
                }
                catch (Exception ex)
                {
                    throw new MeasFatalException($"# UNKNOWN:OSC MeasureDelay読み取りでエラー: {ex.Message}\n");
                }
            }
            return Data;
        }
        //*************************************************
        //アクセス：public
        //戻り値：<bool> 完了フラグ
        //機能：OSC Triggered check
        //引数1：deviceList
        //説明：List<Device> 測定器設定リスト
        // Identifier:OSC
        // UsbId:USB ID
        // TabSettings:tab名
        //引数2：tabItem
        //説明：<string> Tab名
        // Item1,Item2(デフォルトTab名)
        //コメント
        // SingleRUNしている前提
        // Trigger検出後停止しているかどうかで判定
        //*************************************************
        public async Task<bool> OSCTriggeredCheck(List<Device> deviceList, string tabItem, CancellationToken cancellationToken = default)
        {
            bool compflag = true;
            //**********************************
            //OSC2台使用の展開も考えてforeach文はそのまま
            //**********************************
            foreach (Device device in deviceList)
            {
                cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
                //**********************************
                //定義
                //OSCが一台しかない場合foreach文は以下に置き換わる
                //Device device = deviceList.FirstOrDefault();
                //if (device == null)
                //{
                //    MessageBox.Show("OSC デバイスが見つかりませんでした");
                //    return false;
                //}
                //**********************************
                string oscUSBID = device.UsbId;
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
                    compflag = await OSC_Triggered_Check(oscUSBID, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;      //キャンセル要求を検知したら呼び出し元に通知
                }
                catch (MeasWarningException ex)
                {
                    throw new MeasWarningException($"# WARN:OSC Triggerd Checkでエラー: {ex.Message}\n");
                }
                catch (MeasFatalException ex)
                {
                    throw new MeasFatalException($"# FATAL:OSC Triggerd Checkでエラー: {ex.Message}\n");
                }
                catch (Exception ex)
                {
                    throw new MeasFatalException($"# WARN:OSC Triggerd Checkでエラー: {ex.Message}\n");
                }
            }
            return compflag;
        }
        //*************************************************
        //アクセス：public
        //戻り値：<bool> 完了フラグ
        //機能：OSC SingleRUN start
        //引数1：deviceList
        //説明：List<Device> 測定器設定リスト
        // Identifier:OSC
        // UsbId:USB ID
        // TabSettings:tab名
        //引数2：tabItem
        //説明：<string> Tab名
        // Item1,Item2(デフォルトTab名)
        //コメント
        // OSC_SINGLE外部アクセス用
        //*************************************************
        public async Task<bool> OSCsingleRUN(List<Device> deviceList, string tabItem, CancellationToken cancellationToken = default)
        {
            bool compflag = true;
            //**********************************
            //チェックの入っているOSCをSingleRUN
            //OSC2台使用の展開も考えてforeach文はそのまま
            //**********************************
            foreach (Device device in deviceList)
            {
                cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
                //**********************************
                //定義
                //OSCが一台しかない場合foreach文は以下に置き換わる
                //Device device = deviceList.FirstOrDefault();
                //if (device == null)
                //{
                //    MessageBox.Show("OSC デバイスが見つかりませんでした");
                //    return false;
                //}
                //**********************************
                string oscUSBID = device.UsbId;
                try
                {
                    await OSC_SINGLE(oscUSBID, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
                    await OSC_SINGLEcomp_Check(oscUSBID, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;      //キャンセル要求を検知したら呼び出し元に通知
                }
                catch (MeasWarningException ex)
                {
                    throw new MeasWarningException($"# WARN:OSC SingleRunでエラー: {ex.Message}\n");
                }
                catch (MeasFatalException ex)
                {
                    throw new MeasFatalException($"# FATAL:OSC SingleRunでエラー: {ex.Message}\n");
                }
                catch (Exception ex)
                {
                    throw new MeasFatalException($"# UNKNOWN:OSC SingleRunでエラー: {ex.Message}\n");
                }
            }
            return compflag;
        }

        //*************************************************
        //アクセス：public
        //戻り値：なし
        //機能：OSC stop
        //引数1：deviceList
        //説明：List<Device> 測定器設定リスト
        // Identifier:OSC
        // UsbId:USB ID
        // TabSettings:tab名
        //引数2：tabItem
        //説明：<string> Tab名
        // Item1,Item2(デフォルトTab名)
        //コメント
        // OSC_RUN外部アクセス用
        //*************************************************
        public async Task<bool> OSCSTOP(List<Device> deviceList, string tabItem, CancellationToken cancellationToken = default)
        {
            bool compflag = true;
            //**********************************
            //チェックの入っているOSCをStop
            //OSC2台使用の展開も考えてforeach文はそのまま
            //**********************************
            foreach (Device device in deviceList)
            {
                cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
                //**********************************
                //定義
                //OSCが一台しかない場合foreach文は以下に置き換わる
                //Device device = deviceList.FirstOrDefault();
                //if (device == null)
                //{
                //    MessageBox.Show("OSC デバイスが見つかりませんでした");
                //    return false;
                //}
                //**********************************
                string oscUSBID = device.UsbId;
                try
                {
                    await OSC_STOP(oscUSBID);
                }
                catch (OperationCanceledException)
                {
                    throw;      //キャンセル要求を検知したら呼び出し元に通知
                }
                catch (MeasWarningException ex)
                {
                    throw new MeasWarningException($"# WARN:OSC Stopでエラー: {ex.Message}\n");
                }
                catch (MeasFatalException ex)
                {
                    throw new MeasFatalException($"# FATAL:OSC Stopでエラー: {ex.Message}\n");
                }
                catch (Exception ex)
                {
                    throw new MeasFatalException($"# UNKNOWN: OSC Stopでエラー: {ex.Message}\n");
                }
            }
            return compflag;
        }
        //*************************************************
        //アクセス：public
        //戻り値：なし
        //機能：OSC RUN start
        //引数1：deviceList
        //説明：List<Device> 測定器設定リスト
        // Identifier:OSC
        // UsbId:USB ID
        // TabSettings:tab名
        //引数2：tabItem
        //説明：<string> Tab名
        // Item1,Item2(デフォルトTab名)
        //コメント
        // OSC_RUN外部アクセス用
        //*************************************************
        public async Task<bool> OSCRUN(List<Device> deviceList, string tabItem, CancellationToken cancellationToken = default)
        {
            bool compflag = true;
            //**********************************
            //チェックの入っているOSCをRUN
            //OSC2台使用の展開も考えてforeach文はそのまま
            //**********************************
            foreach (Device device in deviceList)
            {
                cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
                //**********************************
                //定義
                //OSCが一台しかない場合foreach文は以下に置き換わる
                //Device device = deviceList.FirstOrDefault();
                //if (device == null)
                //{
                //    MessageBox.Show("OSC デバイスが見つかりませんでした");
                //    return false;
                //}
                //**********************************
                string oscUSBID = device.UsbId;
                try
                {
                    await OSC_RUN(oscUSBID);
                }
                catch (OperationCanceledException)
                {
                    throw;      //キャンセル要求を検知したら呼び出し元に通知
                }
                catch (MeasWarningException ex)
                {
                    throw new MeasWarningException($"# WARN:OSC Runでエラー: {ex.Message}\n");
                }
                catch (MeasFatalException ex)
                {
                    throw new MeasFatalException($"# FATAL:OSC Runでエラー: {ex.Message}\n");
                }
                catch (Exception ex)
                {
                    throw new MeasFatalException($"# UNKNOWN:OSC Runでエラー: {ex.Message}\n");
                }
            }
            return compflag;
        }

        //*************************************************
        //アクセス：public
        //戻り値：なし(Task)
        //機能：OSC Remote解除
        //引数1：deviceList
        //説明：List<Device> 測定器設定リスト
        //コメント
        // OSCのリモート解除用
        //*************************************************
        public async Task OSC_RemoteOFF(List<Device> deviceList)
        {
            foreach (Device device in deviceList)
            {
                try
                {
                    //**********************************
                    //定義
                    //**********************************
                    string oscUSBID = device.UsbId;
                    //**********************************
                    //動作
                    //**********************************
                    await commSend.Remote_OFF(oscUSBID);
                }
                catch (MeasWarningException ex)
                {
                    throw new MeasWarningException($"# WARN:OSC リモート解除でエラー {ex.Message}\n", ex);
                }
                catch (MeasFatalException ex)
                {
                    throw new MeasFatalException($"# FATAL:OSC リモート解除でエラー {ex.Message}\n", ex);
                }
                catch (Exception ex)
                {
                    //MessageBox.Show($"# OSCリモート解除でエラー: {ex.Message}");
                    throw new MeasFatalException($"# UNKNOWN:OSC リモート解除でエラー {ex.Message}\n", ex);
                }
            }

        }

//以下privateアクセス**************************************************************************************************

        //*************************************************
        //アクセス：private
        //戻り値：<bool> 完了フラグ
        //機能：OSC トリガ検出チェック
        //　　　トリガ検出でTrue、未検出はFalse
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //コメント
        // 使用するときはawait演算子を付けて呼び出し
        //*************************************************
        private async Task<bool> OSC_Triggered_Check(string usbid, CancellationToken ct)
        {
            bool compflag = false;
            try
            {
                string responce = await OSC_Status(usbid, ct);                  //状態レジスタ読み込み
                ushort status = Convert.ToUInt16(responce);
                compflag = (status & 0x0001) == 0;                          //状態レジスタbit0が0b0か判定(RUN中:1
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"# OSC Trigger検出チェックでエラー: {ex.Message}");
                return false;                                               // エラー時はfalseを返す
            }
            return compflag;
        }

        //*************************************************
        //アクセス：private
        //戻り値：<bool> 完了フラグ
        //機能：OSC SINGLE波形取り込み開始チェック
        //　　　PreTrig経過待ち後True、経過してない場合はFalse
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //コメント
        // 使用するときはawait演算子を付けて呼び出し
        //*************************************************
        private async Task<bool> OSC_SINGLEcomp_Check(string usbid, CancellationToken ct, int maxWaitMs = 10000)
        {
            bool compflag = false;
            var sw = System.Diagnostics.Stopwatch.StartNew();       //通信ハング時のタイムアウト用タイマー
            try
            {
                await utility.Wait_Timer(500, ct);                              //初回状態レジスタ読み込み前に500ms wait
                while (!compflag && sw.ElapsedMilliseconds < maxWaitMs)
                {
                    ct.ThrowIfCancellationRequested();
                    string responce = await OSC_Status(usbid, ct);                    //状態レジスタ読み込み
                    ushort status = Convert.ToUInt16(responce);
                    compflag = (status & 0x0004) == 4;                      //状態レジスタbit0とbit2が0b1か判定(RUN中:1 SINGLE中:4+1 PreTrig経過後bit2が0b1
                    if (!compflag)
                        await utility.Wait_Timer(50, ct);                     //50ms wait
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"# OSC SINGLE RUN完了チェックでエラー: {ex.Message}");
                return false;                                               // エラー時はfalseを返す
            }
            return compflag;
        }

        //*************************************************
        //アクセス：private
        //戻り値：<bool> 完了フラグ
        //機能：OSC Measure機能測定完了後True、測定中はFalse
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //コメント
        // 使用するときはawait演算子を付けて呼び出し
        //*************************************************
        private async Task<bool> OSC_MEASUREcomp_Check(string usbid, CancellationToken ct, int maxWaitMs = 10000)
        {
            bool compflag = false;
            var sw = System.Diagnostics.Stopwatch.StartNew();       //通信ハング時のタイムアウト用タイマー
            try
            {
                while (!compflag && sw.ElapsedMilliseconds < maxWaitMs)
                {
                    ct.ThrowIfCancellationRequested();
                    string responce = await OSC_Status(usbid, ct);                    //状態レジスタ読み込み
                    ushort status = Convert.ToUInt16(responce);
                    compflag = (status & 0x0080) == 0;                      //状態レジスタbit7が0b0か判定(MEASURE中:1
                    if (!compflag)
                        await utility.Wait_Timer(10, ct);                     //10ms wait
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"# OSC MEASURE完了チェックでエラー: {ex.Message}");
                return false;                                               // エラー時はfalseを返す
            }
            return compflag;
        }
        //*************************************************
        //アクセス：private
        //戻り値：<bool> 完了フラグ
        //機能：OSC 直前コマンド完了チェック
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
                //MessageBox.Show($"# OSCコマンド完了チェックでエラー: {ex.Message}");
                return false;                                               // エラー時はfalseを返す
            }
            return compflag;
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし(Task)
        //機能：OSC リセット
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //コメント
        // 使用するときはawait演算子を付けて呼び出し
        //*************************************************
        private async Task OSC_Reset(string usbid, CancellationToken ct)
        {
            string command = "*RST";
            await commSend.Comm_sendB(usbid, command);               //リモート解除を無効にして送信
            bool comp = await Complete_Check(usbid, ct);                //直前コマンド完了チェック
            if (!comp)
                throw new MeasWarningException("# WARN:OSC リセット失敗\n");
        }

        //*************************************************
        //アクセス：private
        //戻り値：<string> 状態レジスタ値
        //機能：OSC 状態レジスタ問い合わせ
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //*************************************************
        private async Task<string> OSC_Status(string usbid, CancellationToken ct)
        {
            string command = ":STAT:COND?";
            string responce = await commQuery.Comm_queryB(usbid, command, ct);      //リモート解除無し
            return responce;
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：OSC ACQUIRE レコード長設定
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：length
        //説明：<uint> レコード長
        // 1250/12500/125000/1250000/6250000
        //*************************************************
        private async Task OSC_Set_ACQ(string usbid, uint length)
        {
            string command = $":ACQ:RLEN {length}";
            await commSend.Comm_sendB(usbid, command);           //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：OSC CHレンジ設定
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> 設定するCH番号
        // "1"～"4"
        //引数3：vrange
        //説明：<string> 電圧レンジ(volt/div)
        // 20E-3/50E-3/100E-3/200E-3/500E-3/1E+0/2E+0/5E+0/10E+0/20E+0/50E+0/100E+0
        //コメント
        // VDIVの代用でVARも使用できる
        // VARを使用するとパネルのFINEが点灯(ダイヤルで細かく設定できる)
        //*************************************************
        private async Task OSC_Set_CHRange(string usbid, string ch, string vrange)
        {
            string command = $":CHAN{ch}:VDIV {vrange}";
            await commSend.Comm_sendB(usbid, command);           //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：OSC CHポジション設定
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> 設定するCH番号
        // "1"～"4"
        //引数3：vpos
        //説明：<float> ポジション(div)
        // -4.00～4.00
        //*************************************************
        private async Task OSC_Set_CHPosition(string usbid, string ch, float vpos)
        {
            string command = $":CHAN{ch}:POS {vpos}";
            await commSend.Comm_sendB(usbid, command);           //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：OSC タイムレンジ設定
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：trange
        //説明：<string> 時間軸(time/div)
        // 50E-6/100E-6/200E-6/500E-6/
        // 1E-3/2E-3/5E-3/10E-3/20E-3/50E-3/100E-3/200E-3/500E-3/1E+0
        //*************************************************
        private async Task OSC_Set_TRange(string usbid, string trange)
        {
            string command = $":TIM:TDIV {trange}";
            await commSend.Comm_sendB(usbid, command);      //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：OSC トリガポジション設定
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：tpos
        //説明：<float> トリガ開始位置(%)
        // 0.0～100.0
        //*************************************************
        private async Task OSC_Set_TPosition(string usbid, float tpos)
        {
            string command = $":TRIG:POS {tpos}";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：OSC エッジトリガ設定
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> トリガソース
        // "1"～"4"/"EXT"
        //引数3：slope
        //説明：<string> エッジ方向
        // "RISE" or "FALL"
        //引数4：level
        //説明：<float> トリガレベル(V)
        // -1.00～5.00
        //*************************************************
        private async Task OSC_Set_Trigger(string usbid, string ch, string slope, float level)
        {
            string command = $":TRIG:SIMP:SOUR {ch};" +
                                        $"SLOP {slope};" +
                                        $"LEV {level}";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：OSC Indicator ON
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> Indicator対象CH番号
        // "1"～"4"
        //*************************************************
        private async Task OSC_Set_DelayIndicatorON(string usbid, string ch)
        {
            string command = $":MEAS:IND {ch},DEL";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：public
        //戻り値：なし
        //機能：OSC Measure機能 ON/OFF
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：sw
        //説明：<string> 測定ON/OFF
        // "ON" or "OFF"
        //*************************************************
        public async Task OSC_Set_MeasureONOFF(string usbid, string sw)
        {
            string command = $":MEAS:MODE {sw}";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：OSC Delay測定 ON
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> Delay測定するCH番号
        // "1"～"4"
        //*************************************************
        private async Task OSC_Set_MeasureDelayON(string usbid, string ch)
        {
            string command = $":MEAS:CHAN{ch}:DEL:STAT ON";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：OSC Delay測定 Reference設定
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> Delay測定するCH番号
        // "1"～"4"
        //
        //コメント
        // RefのソースはTriggerに固定
        // RefをTriggerからCHにするのであれば以下設定変更が必要
        // :DEL:REF:SOUR [CH番号]
        // :DEL:REF:SLOP [エッジ方向]
        // :DEL:REF:COUN [回数]
        //*************************************************
        private async Task OSC_Set_MeasureDelay_REF(string usbid, string ch)
        {
            string command = $":MEAS:CHAN{ch}:DEL:REF:SOUR TRIG";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：OSC Delay測定設定
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> Delay測定するCH番号
        // "1"～"4"
        //引数3：slope
        //説明：<string> エッジ方向
        // "RISE" or "FALL"
        //コメント
        // エッジカウントは1で固定
        //*************************************************
        private async Task OSC_Set_MeasureDelay_MEAS(string usbid, string ch, string slope)
        {
            string command = $":MEAS:CHAN{ch}:DEL:MEAS:" +
                                $"SLOP {slope};" +
                                $"COUN 1";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：OSC Delay測定 測定範囲設定
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：trange1
        //説明：<float> 測定範囲開始位置
        // -5.00～4.99
        //引数3：trange2
        //説明：<float> 測定範囲終了位置
        // -4.99～5.00
        //以下省略可能
        //引数4：area
        //説明：<string> 測定領域 通常時=1
        // "1" or "2"
        //*************************************************
        private async Task OSC_Set_MeasureDelay_TRange(string usbid, float trange1, float trange2, string area = "1")
        {
            string command = $":MEAS:TRAN{area} {trange1},{trange2}";
            await commSend.Comm_sendB(usbid, command);                  //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：OSC 波形取り込みRUN
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //*************************************************
        private async Task OSC_RUN(string usbid)
        {
            string command = ":START";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：OSC 波形取り込みSINGLE
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //コメント
        // SingleRunさせるだけなのでComm_sendBでも問題ないが
        // 直後のコマンドによってはOSCエラーコード410が発生するので
        // Comm_queryBを使用。応答は無視
        //*************************************************
        private async Task OSC_SINGLE(string usbid, CancellationToken ct)
        {
            string command = ":SST? 0";
            string responce = await commQuery.Comm_queryB(usbid, command, ct);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：OSC 波形取り込みSTOP
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //*************************************************
        private async Task OSC_STOP(string usbid)
        {
            string command = ":STOP";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：<string> Delay測定値
        //機能：OSC Delay測定値読み出し
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> Delay測定するCH番号
        // "1"～"4"
        //コメント
        // 使用するときはawait演算子を付けて呼び出し
        //*************************************************
        private async Task<string> OSC_Read_MeasureDelay(string usbid, string ch, CancellationToken ct)
        {
            try
            {
                await OSC_MEASUREcomp_Check(usbid, ct);                     //測定完了まで待機
                await utility.Wait_Timer(1000);                         //1000ms wait(測定完了後
                string command = $":MEAS:CHAN{ch}:DEL:VAL?";           //MEASURE完了後VALコマンド
                string responce = await commQuery.Comm_queryB(usbid, command, ct);      //リモート解除無し
                responce = responce.Replace($":MEAS:CHAN{ch}:DEL:VAL ", "");    //応答前半部分削除
                responce = responce.Trim();                             //空白・改行コード削除
                return responce;
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"# OSC MEASUREでエラー: {ex.Message}");
                return "DelayRead失敗";
            }

        }
        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：OSC CH表示ON/OFF
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> 表示変更するCH番号
        // "1"～"4"
        //引数3：sw
        //説明：<string> 表示ON/OFF
        // "ON" or "OFF"
        //*************************************************
        private async Task OSC_CH_Display(string usbid, string ch, string sw)
        {
            string command = $":CHAN{ch}:DISP {sw}";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }
    }
}

