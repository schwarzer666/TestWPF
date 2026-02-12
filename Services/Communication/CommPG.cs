using USBcommunication;             //CommUSB.cs
using UTility;                      //Utility.cs
using TemperatureCharacteristics.Exceptions;    //例外スローの為

namespace PGcommunication
{
    public class PGcomm
    {
        private static PGcomm? instance;        //インスタンスをnull許容型(xxx?)として宣言し初期値がnullでも問題ないと表示
        private readonly USBcomm commSend;      //フィールド変数commSend
        private readonly USBcomm commQuery;     //フィールド変数commQuery
        private readonly UT timer_ms;           //フィールド変数timer_ms

        private PGcomm()
        {
            commSend = USBcomm.Instance;        // コンストラクタで初期化
            commQuery = USBcomm.Instance;       // コンストラクタで初期化
            timer_ms = UT.Instance;
        }
        public static PGcomm Instance     //外部からのアクセス用
        {
            get
            {
                if (instance == null)
                    instance = new PGcomm(); // クラス内でインスタンスを生成
                return instance;
            }
        }

        //*************************************************
        //アクセス：public
        //戻り値：<bool> 完了フラグ
        //機能：PG Initial
        //　　　Reset→値設定
        //引数1：deviceList
        //説明：List<Device> 測定器設定リスト
        // Identifier:PULSE
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
        public async Task PG_Initialize(List<Device> deviceList, string tabItem, CancellationToken cancellationToken = default)
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
                    //device.TabSettings[TabSet]をPGSettings型でキャストし
                    //成功すればpgSettingsインスタンス、失敗すれば新たにインスタンス生成
                    //**********************************
                    PGSettings pgSettings = device.TabSettings[tabItem] as PGSettings ?? new PGSettings();
                    string? pgUSBID = device.UsbId;
                    string? pgMode = pgSettings.Function;
                    double pgLLev = pgSettings.LowLevelValue;
                    double pgHLev = pgSettings.HighLevelValue;
                    double pgPeriod = pgSettings.PeriodValue;
                    double pgWidth = pgSettings.WidthValue;
                    string? pgOutCh = pgSettings.OutputCH;
                    string? pgOutputZ = pgSettings.OutputZ;
                    string? pgPol = pgSettings.Polarity;
                    string? pgTrigOUT = pgSettings.TrigOut;
                    //**********************************
                    //動作
                    // リセット（リセット完了まで待機）
                    // ファンクション設定
                    // 入力項目変更（単位変更）
                    // 出力抵抗設定
                    // Period,Width読取→変更
                    // High,Low読取→変更
                    // Triger設定
                    // Burst設定
                    //**********************************
                    cancellationToken.ThrowIfCancellationRequested();       //リセット前にキャンセルチェック
                    await PG_Reset(pgUSBID, cancellationToken);

                    cancellationToken.ThrowIfCancellationRequested();       //各種設定前にキャンセルチェック
                    await PG_Set_Function(pgUSBID, pgOutCh, pgMode);        //Function
                    await PG_ChangeUnits(pgUSBID, pgOutCh);                 //CH表示単位変更
                    await PG_Set_OutputLoad(pgUSBID, pgOutCh, pgOutputZ);   //出力Z
                    await PG_ChangePerWidt(pgUSBID, pgOutCh, pgPeriod, pgWidth, cancellationToken);//周期変更(順番あり
                    await PG_ChangeHighLow(pgUSBID, pgOutCh, pgHLev, pgLLev, cancellationToken);//電圧変更(順番あり
                    //await PG_Set_PulseEdge(pgUSBID, pgOutCh, 0.0000000033f);         //rise/fall時間=3.3ns固定
                    await PG_Set_PulseEdge(pgUSBID, pgOutCh, 0.000000010f);         //rise/fall時間=10.0ns固定
                    await PG_Set_TriggerSource(pgUSBID, pgOutCh, "BUS");     //トリガBUSトリガ固定
                    await PG_Set_SyncTrigger(pgUSBID, pgOutCh);             //SYNC出力+ソース選択(ソースは出力CHに固定
                    await PG_Set_TriggerOUT(pgUSBID, pgOutCh, pgTrigOUT);   //トリガ出力設定（出力する場合立ち上がりエッジのみ
                    await PG_Set_BurstMode(pgUSBID, pgOutCh, "TRIG");       //Burst TRIGモード(N cycle)
                    await PG_Set_BurstCount(pgUSBID, pgOutCh, 1);           //Burst 1cycle
                    await PG_Set_Burst(pgUSBID, pgOutCh, "ON");             //Burst ON
                    await PG_Set_OutputPol(pgUSBID, pgOutCh, pgPol);        //出力極性
                }
                catch (OperationCanceledException)
                {
                    throw;      //キャンセル要求を検知したら呼び出し元に通知
                }
                catch (MeasWarningException ex)
                {
                    throw new MeasWarningException($"# WARN:PG Initializeでエラー: {ex.Message}\n");
                }
                catch (MeasFatalException ex)
                {
                    throw new MeasFatalException($"# FATAL:PG Initializeでエラー: {ex.Message}\n");
                }
                catch (Exception ex)
                {
                    throw new MeasFatalException($"# 未分類エラー:PG Initializeでエラー: {ex.Message}\n");
                }
            }
        }
        //*************************************************
        //アクセス：public
        //戻り値：なし
        //機能：PG 出力OFF
        //引数1：deviceList
        //説明：List<Device> 測定器設定リスト
        //引数2：tabItem
        //説明：<string> Tab名
        //コメント
        // SOURCE_OutputOFFの外部アクセス用
        //*************************************************
        public async Task PG_SetHighLow(List<Device> deviceList, string tabItem, CancellationToken cancellationToken = default)
        {
            foreach (Device device in deviceList)
            {
                cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
                try
                {
                    //**********************************
                    //定義
                    //device.TabSettings[TabSet]をPGSettings型でキャストし
                    //成功すればpgSettingsインスタンス、失敗すれば新たにインスタンス生成
                    //**********************************
                    PGSettings pgSettings = device.TabSettings[tabItem] as PGSettings ?? new PGSettings();
                    string pgUSBID = device.UsbId;
                    string? pgOutCh = pgSettings.OutputCH;
                    double pgLLev = pgSettings.LowLevelValue;
                    double pgHLev = pgSettings.HighLevelValue;
                    //**********************************
                    //動作
                    //**********************************
                    //await PG_Set_OutputRange(pgUSBID, pgOutCh, "OFF");
                    await PG_ChangeHighLow(pgUSBID, pgOutCh, pgHLev, pgLLev, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;      //キャンセル要求を検知したら呼び出し元に通知
                }
                catch (MeasWarningException ex)
                {
                    throw new MeasWarningException($"# WARN:PG HighLowVolt設定でエラー: {ex.Message}\n");
                }
                catch (MeasFatalException ex)
                {
                    throw new MeasFatalException($"# FATAL:PG HighLowVolt設定でエラー: {ex.Message}\n");
                }
                catch (Exception ex)
                {
                    throw new MeasFatalException($"# 未分類エラー:PG HighLowVolt設定でエラー: {ex.Message}\n");
                }
            }
        }
        //*************************************************
        //アクセス：public
        //戻り値：なし
        //機能：PG 出力ON
        //引数1：deviceList
        //説明：List<Device> 測定器設定リスト
        //引数2：tabItem
        //説明：<string> Tab名
        // Item1,Item2(デフォルトTab名)
        //コメント
        // PG_OutputONの外部アクセス用
        //*************************************************
        public async Task PG_OutputON(List<Device> deviceList, string tabItem, CancellationToken cancellationToken = default)
        {
            foreach (Device device in deviceList)
            {
                cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
                try
                {
                    //**********************************
                    //定義
                    //device.TabSettings[TabSet]をPGSettings型でキャストし
                    //成功すればpgSettingsインスタンス、失敗すれば新たにインスタンス生成
                    //**********************************
                    PGSettings pgSettings = device.TabSettings[tabItem] as PGSettings ?? new PGSettings();
                    string pgUSBID = device.UsbId;
                    string? pgOutCh = pgSettings.OutputCH;
                    //**********************************
                    //動作
                    //**********************************
                    await PG_OutputON(pgUSBID, pgOutCh);
                }
                catch (OperationCanceledException)
                {
                    throw;      //キャンセル要求を検知したら呼び出し元に通知
                }
                catch (MeasWarningException ex)
                {
                    throw new MeasWarningException($"# WARN:PG OUTPUTonでエラー: {ex.Message}\n");
                }
                catch (MeasFatalException ex)
                {
                    throw new MeasFatalException($"# FATAL:PG OUTPUTonでエラー: {ex.Message}\n");
                }
                catch (Exception ex)
                {
                    throw new MeasFatalException($"# 未分類エラー:PG OUTPUTonでエラー: {ex.Message}\n");
                }
            }
        }
        //*************************************************
        //アクセス：public
        //戻り値：なし
        //機能：PG 出力OFF
        //引数1：deviceList
        //説明：List<Device> 測定器設定リスト
        //引数2：tabItem
        //説明：<string> Tab名
        //コメント
        // SOURCE_OutputOFFの外部アクセス用
        //*************************************************
        public async Task PG_OutputOFF(List<Device> deviceList, string tabItem, CancellationToken cancellationToken = default)
        {
            foreach (Device device in deviceList)
            {
                cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
                try
                {
                    //**********************************
                    //定義
                    //device.TabSettings[TabSet]をPGSettings型でキャストし
                    //成功すればpgSettingsインスタンス、失敗すれば新たにインスタンス生成
                    //**********************************
                    PGSettings pgSettings = device.TabSettings[tabItem] as PGSettings ?? new PGSettings();
                    string pgUSBID = device.UsbId;
                    string? pgOutCh = pgSettings.OutputCH;
                    //**********************************
                    //動作
                    //**********************************
                    await PG_OutputOFF(pgUSBID, pgOutCh);
                }
                catch (OperationCanceledException)
                {
                    throw;      //キャンセル要求を検知したら呼び出し元に通知
                }
                catch (MeasWarningException ex)
                {
                    throw new MeasWarningException($"# WARN:PG OUTPUToffでエラー: {ex.Message}\n");
                }
                catch (MeasFatalException ex)
                {
                    throw new MeasFatalException($"# FATAL:PG OUTPUToffでエラー: {ex.Message}\n");
                }
                catch (Exception ex)
                {
                    throw new MeasFatalException($"# 未分類エラー:PG OUTPUToffでエラー: {ex.Message}\n");
                }
            }
        }
        //*************************************************
        //アクセス：public
        //戻り値：なし
        //機能：PG 出力OFF
        //引数1：deviceList
        //説明：List<Device> 測定器設定リスト
        //引数2：tabItem
        //説明：<string> Tab名
        //コメント
        // PG_ChTriggerの外部アクセス用
        //*************************************************
        public async Task PG_ChTrigger(List<Device> deviceList, string tabItem, CancellationToken cancellationToken = default)
        {
            foreach (Device device in deviceList)
            {
                cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
                try
                {
                    //**********************************
                    //定義
                    //device.TabSettings[TabSet]をPGSettings型でキャストし
                    //成功すればpgSettingsインスタンス、失敗すれば新たにインスタンス生成
                    //**********************************
                    PGSettings pgSettings = device.TabSettings[tabItem] as PGSettings ?? new PGSettings();
                    string pgUSBID = device.UsbId;
                    string? pgOutCh = pgSettings.OutputCH;
                    //**********************************
                    //動作
                    //**********************************
                    await PG_ChTrigger(pgUSBID, pgOutCh);
                }
                catch (OperationCanceledException)
                {
                    throw;      //キャンセル要求を検知したら呼び出し元に通知
                }
                catch (MeasWarningException ex)
                {
                    throw new MeasWarningException($"# WARN:PG Trigger(CH)でエラー: {ex.Message}\n");
                }
                catch (MeasFatalException ex)
                {
                    throw new MeasFatalException($"# FATAL:PG Trigger(CH)でエラー: {ex.Message}\n");
                }
                catch (Exception ex)
                {
                    throw new MeasFatalException($"# 未分類エラー:PG Trigger(CH)でエラー: {ex.Message}\n");
                }
            }
        }
        //*************************************************
        //アクセス：public
        //戻り値：なし
        //機能：PG 出力OFF
        //引数1：deviceList
        //説明：List<Device> 測定器設定リスト
        //引数2：tabItem
        //説明：<string> Tab名
        //コメント
        // PG_BusTriggerの外部アクセス用
        //*************************************************
        public async Task PG_BusTrigger(List<Device> deviceList, string tabItem, CancellationToken cancellationToken = default)
        {
            foreach (Device device in deviceList)
            {
                cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
                try
                {
                    //**********************************
                    //定義
                    //**********************************
                    string pgUSBID = device.UsbId;
                    //**********************************
                    //動作
                    //**********************************
                    await PG_BusTrigger(pgUSBID);
                }
                catch (OperationCanceledException)
                {
                    throw;      //キャンセル要求を検知したら呼び出し元に通知
                }
                catch (MeasWarningException ex)
                {
                    throw new MeasWarningException($"# WARN:PG Trigger(BUS)でエラー: {ex.Message}\n");
                }
                catch (MeasFatalException ex)
                {
                    throw new MeasFatalException($"# FATAL:PG Trigger(BUS)でエラー: {ex.Message}\n");
                }
                catch (Exception ex)
                {
                    throw new MeasFatalException($"# 未分類エラー:PG Trigger(BUS)でエラー: {ex.Message}\n");
                }
            }
        }
        //*************************************************
        //アクセス：public
        //戻り値：<bool> 完了フラグ
        //機能：PG CH表示切替
        //引数1：deviceList
        //説明：List<Device> 測定器設定リスト
        // Identifier:PULSE
        // UsbId:USB ID
        // InstName:信号名
        // TabSettings:tab名
        //引数2：tabItem
        //説明：<string> Tab名
        // Item1,Item2(デフォルトTab名)
        //*************************************************
        public async Task PG_DispCH(List<Device> deviceList, string tabItem, CancellationToken cancellationToken = default)
        {
            foreach (Device device in deviceList)
            {
                cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
                try
                {
                    //**********************************
                    //定義
                    //device.TabSettings[TabSet]をPGSettings型でキャストし
                    //成功すればpgSettingsインスタンス、失敗すれば新たにインスタンス生成
                    //**********************************
                    PGSettings pgSettings = device.TabSettings[tabItem] as PGSettings ?? new PGSettings();
                    string? pgUSBID = device.UsbId;
                    string? pgOutCh = pgSettings.OutputCH;
                    //**********************************
                    //動作
                    //**********************************
                    cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
                    await PG_Set_DisplayFocus(pgUSBID, pgOutCh);
                }
                catch (OperationCanceledException)
                {
                    throw;      //キャンセル要求を検知したら呼び出し元に通知
                }
                catch (MeasWarningException ex)
                {
                    throw new MeasWarningException($"# WARN:PG CH表示でエラー: {ex.Message}\n");
                }
                catch (MeasFatalException ex)
                {
                    throw new MeasFatalException($"# FATAL:PG CH表示でエラー: {ex.Message}\n");
                }
                catch (Exception ex)
                {
                    throw new MeasFatalException($"# UNKNOWN:PG CH表示でエラー: {ex.Message}\n");
                }
            }
        }
        //*************************************************
        //アクセス：public
        //戻り値：<bool> 完了フラグ
        //機能：PG Range Hold
        //引数1：deviceList
        //説明：List<Device> 測定器設定リスト
        // Identifier:PULSE
        // UsbId:USB ID
        // InstName:信号名
        // TabSettings:tab名
        //引数2：tabItem
        //説明：<string> Tab名
        // Item1,Item2(デフォルトTab名)
        //引数3：sw
        //説明：<string> RangeHold ON/OFF
        //*************************************************
        public async Task PG_RangeAutoHold(List<Device> deviceList, string tabItem, string sw, CancellationToken cancellationToken = default)
        {
            foreach (Device device in deviceList)
            {
                cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
                try
                {
                    //**********************************
                    //定義
                    //device.TabSettings[TabSet]をPGSettings型でキャストし
                    //成功すればpgSettingsインスタンス、失敗すれば新たにインスタンス生成
                    //**********************************
                    PGSettings pgSettings = device.TabSettings[tabItem] as PGSettings ?? new PGSettings();
                    string? pgUSBID = device.UsbId;
                    string? pgOutCh = pgSettings.OutputCH;
                    //**********************************
                    //動作
                    //**********************************
                    cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
                    await PG_Set_OutputRange(pgUSBID, pgOutCh, sw);
                }
                catch (OperationCanceledException)
                {
                    throw;      //キャンセル要求を検知したら呼び出し元に通知
                }
                catch (MeasWarningException ex)
                {
                    throw new MeasWarningException($"# WARN:PG RangeHoldでエラー: {ex.Message}\n");
                }
                catch (MeasFatalException ex)
                {
                    throw new MeasFatalException($"# FATAL:PG RangeHoldでエラー: {ex.Message}\n");
                }
                catch (Exception ex)
                {
                    throw new MeasFatalException($"# UNKNOWN:PG RangeHoldでエラー: {ex.Message}\n");
                }
            }
        }
        //*************************************************
        //アクセス：public
        //戻り値：なし(Task)
        //機能：PG Remote解除
        //引数1：deviceList
        //説明：List<Device> 測定器設定リスト
        //コメント
        // PGのリモート解除用
        //*************************************************
        public async Task PG_RemoteOFF(List<Device> deviceList)
        {
            foreach (Device device in deviceList)
            {
                try
                {
                    //**********************************
                    //定義
                    //**********************************
                    string pgUSBID = device.UsbId;
                    //**********************************
                    //動作
                    //**********************************
                    await commSend.Remote_OFF(pgUSBID);
                }
                catch (MeasWarningException ex)
                {
                    throw new MeasWarningException($"# WARN:PG リモート解除でエラーでエラー {ex.Message}\n", ex);
                }
                catch (MeasFatalException ex)
                {
                    throw new MeasFatalException($"# FATAL:PG リモート解除でエラーでエラー {ex.Message}\n", ex);
                }
                catch (Exception ex)
                {
                    //MessageBox.Show($"# PulseGeneratorリモート解除でエラー: {ex.Message}");
                    throw new MeasFatalException($"# UNKNOWN:PG リモート解除でエラーでエラー {ex.Message}\n", ex);
                }
            }

        }

//以下privateアクセス**************************************************************************************************
        //*************************************************
        //アクセス：private
        //戻り値：<bool> 完了フラグ
        //機能：PG 直前コマンド完了チェック
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
                        await timer_ms.Wait_Timer(50, ct);                     //50ms wait
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"# PGコマンド完了チェックでエラー: {ex.Message}");
                return false;                                               // エラー時はfalseを返す
            }
            return compflag;
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし(Task)
        //機能：PG リセット
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //コメント
        // 使用するときはawait演算子を付けて呼び出し
        //*************************************************
        private async Task PG_Reset(string usbid, CancellationToken ct)
        {
            string command = "*RST";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
            await timer_ms.Wait_Timer(3500, ct);                     //3500ms wait
            //await commSend.Comm_sendB(usbid, "*CLS");
            bool comp = await Complete_Check(usbid, ct);                  //直前コマンド完了チェック
            if (!comp)
                throw new MeasWarningException("# WARN:PG リセット完了待ちでエラー\n");
        }
        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：PG 表示単位変更
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> PG設定変更対象ch
        //*************************************************
        private async Task PG_ChangeUnits(string usbid, string ch)
        {
            await PG_Set_DisplayFocus(usbid, ch);         //Display表示切替
            await PG_Set_DisplayUnitHori(usbid, "PER");   //単位をPeriodに変更
            await PG_Set_DisplayUnitPulse(usbid, "WIDT"); //単位をWidthに変更
            await PG_Set_DisplayUnitVolt(usbid, "HIGH");  //単位をHigh/Lowに変更
        }
        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：PG 周期変更
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> PG設定変更対象ch
        //引数3：ch
        //説明：<double> 周期
        //引数2：ch
        //説明：<double> 幅
        //*************************************************
        private async Task PG_ChangePerWidt(string usbid, string ch, double period, double width, CancellationToken ct)
        {
            string nowWidth = await PG_Read_PulseWidth(usbid, ch, ct);
            if (double.Parse(nowWidth) >= period)
            {
                //widthを先に変更
                await PG_Set_PulseWidth(usbid, ch, width);
                await PG_Set_PulsePeriod(usbid, ch, period);
            }
            else
            {
                //periodを先に変更
                await PG_Set_PulsePeriod(usbid, ch, period);
                await PG_Set_PulseWidth(usbid, ch, width);
            }
        }
        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：PG 出力電圧変更
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> PG設定変更対象ch
        //引数3：ch
        //説明：<double> High Volt
        //引数2：ch
        //説明：<double> Low Volt
        //*************************************************
        private async Task PG_ChangeHighLow(string usbid, string ch, double high, double low, CancellationToken ct)
        {
            string nowLow = await PG_Read_LowVolt(usbid, ch, ct);
            if (double.Parse(nowLow) >= high)
            {
                //lowを先に変更
                await PG_Set_LowVolt(usbid, ch, low);
                await PG_Set_HighVolt(usbid, ch, high);
            }
            else
            {
                //highを先に変更
                await PG_Set_HighVolt(usbid, ch, high);
                await PG_Set_LowVolt(usbid, ch, low);
            }
        }
        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：PG ファンクション設定
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> PG設定変更対象ch
        // "1" or "2" 
        //引数3：function
        //説明：<string> PGファンクション設定
        // "PULS" or "SQU"  or "DC" or その他
        //*************************************************
        private async Task PG_Set_Function(string usbid, string ch, string function)
        {
            string command = $"SOUR{ch}:FUNC {function}";       //function文字列は小文字でも一応問題ない
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信 
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：PG 周期設定（Waveform:PULSEの場合）
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> PG設定変更対象ch
        // "1" or "2" 
        //引数3：period
        //説明：<double> PG周期
        // 20.000ns～100.000s
        //*************************************************
        private async Task PG_Set_PulsePeriod(string usbid, string ch, double period)
        {
            string command = $"SOUR{ch}:FUNC:PULS:PER {period}";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：<string> 現在設定値
        //機能：PG 周期設定問い合わせ（Waveform:PULSEの場合）
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> PG設定変更対象ch
        // "1" or "2" 
        //*************************************************
        private async Task<string> PG_Read_PulsePeriod(string usbid, string ch, CancellationToken ct)
        {
            await Complete_Check(usbid, ct);                  //直前コマンド完了チェック
            string command = $"SOUR{ch}:FUNC:PULS:PER?";
            string responce = await commQuery.Comm_queryB(usbid, command, ct);      //リモート解除無し
            return responce;
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：PG パルス幅設定（Waveform:PULSEの場合）
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> PG設定変更対象ch
        // "1" or "2" 
        //引数3：width
        //説明：<double> PGパルス幅
        // 20.000ns～100.000s
        //*************************************************
        private async Task PG_Set_PulseWidth(string usbid, string ch, double width)
        {
            string command = $"SOUR{ch}:FUNC:PULS:WIDT {width}";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：<string> 現在設定値
        //機能：PG パルス幅設定問い合わせ（Waveform:PULSEの場合）
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> PG設定変更対象ch
        // "1" or "2" 
        //*************************************************
        private async Task<string> PG_Read_PulseWidth(string usbid, string ch, CancellationToken ct)
        {
            await Complete_Check(usbid, ct);                  //直前コマンド完了チェック
            string command = $"SOUR{ch}:FUNC:PULS:WIDT?";
            string responce = await commQuery.Comm_queryB(usbid, command, ct);      //リモート解除無し
            return responce;
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：PG Rise/Fall時間設定（Waveform:PULSEの場合）
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> PG設定変更対象ch
        // "1" or "2" 
        //引数3：width
        //説明：<float> PG Rise/Fall時間
        // 3.3ns～10.0us
        //コメント
        // 立ち上がり/立下り時間両方変更
        //*************************************************
        private async Task PG_Set_PulseEdge(string usbid, string ch, float time)
        {
            string command = $"SOUR{ch}:FUNC:PULS:TRAN:LEAD {time}; TRA {time}";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }
        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：PG 出力電圧設定（Unit:high/lowの場合）
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> PG設定変更対象ch
        // "1" or "2" 
        //引数3：value
        //説明：<double> PG設定電圧
        // -10.00V～10.00V
        //コメント
        // unitでhigh/lowを選択しているときのみ使用可能
        // amplitude/offsetは別コマンドになるので注意
        //*************************************************
        private async Task PG_Set_HighVolt(string usbid, string ch, double value)
        {
            string command = $"SOUR{ch}:VOLT:HIGH {value}";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }
        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：PG 出力電圧設定（Unit:high/lowの場合）
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> PG設定変更対象ch
        // "1" or "2" 
        //引数3：value
        //説明：<double> PG設定電圧
        // -10.00V～10.00V
        //コメント
        // unitでhigh/lowを選択しているときのみ使用可能
        // amplitude/offsetは別コマンドになるので注意
        //*************************************************
        private async Task PG_Set_LowVolt(string usbid, string ch, double value)
        {
            string command = $"SOUR{ch}:VOLT:LOW {value}";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：<string> 現在設定値
        //機能：PG 出力電圧設定問い合わせ（Unit:high/lowの場合）
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> PG設定変更対象ch
        // "1" or "2" 
        //*************************************************
        private async Task<string> PG_Read_HighVolt(string usbid, string ch, CancellationToken ct)
        {
            await Complete_Check(usbid, ct);                  //直前コマンド完了チェック
            string command = $"SOUR{ch}:VOLT:HIGH?";
            string responce = await commQuery.Comm_queryB(usbid, command, ct);      //リモート解除無し
            return responce;
        }
        //*************************************************
        //アクセス：private
        //戻り値：<string> 現在設定値
        //機能：PG 出力電圧設定問い合わせ（Unit:high/lowの場合）
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> PG設定変更対象ch
        // "1" or "2" 
        //*************************************************
        private async Task<string> PG_Read_LowVolt(string usbid, string ch, CancellationToken ct)
        {
            await Complete_Check(usbid, ct);                  //直前コマンド完了チェック
            string command = $"SOUR{ch}:VOLT:LOW?";
            string responce = await commQuery.Comm_queryB(usbid, command, ct);      //リモート解除無し
            return responce;
        }
        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：PG 出力電圧レンジ設定
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> PG設定変更対象ch
        // "1" or "2" 
        //引数3：range
        //説明：<string> PG出力オートレンジ機能ON/OFF
        // "ON" or "OFF" or "ONCE"
        //*************************************************
        private async Task PG_Set_OutputRange(string usbid, string ch, string range)
        {
            string command = $"SOUR{ch}:VOLT:RANG:AUTO {range}";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：PG ディスプレイ設定（フォーカス）
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> PG設定変更対象ch
        // "1" or "2" 
        //*************************************************
        private async Task PG_Set_DisplayFocus(string usbid, string ch)
        {
            string command = $"DISP:FOC CH{ch}";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：PG ディスプレイ設定（周期時間単位変更）
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：hori
        //説明：<string> 時間軸単位
        // "PER" or "FERQ"
        // PER:秒 FREQ:Hz
        //*************************************************
        private async Task PG_Set_DisplayUnitHori(string usbid, string hori)
        {
            string command = $"DISP:UNIT:RATE {hori}";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：PG ディスプレイ設定（パルス幅単位変更）
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：hori
        //説明：<string> パルス幅単位
        // "WIDT" or "DUTY"
        // WIDT:秒 DUTY:%
        //*************************************************
        private async Task PG_Set_DisplayUnitPulse(string usbid, string hori)
        {
            string command = $"DISP:UNIT:PULS {hori}";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：PG ディスプレイ設定（出力電圧単位変更）
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ver
        //説明：<string> 出力電圧幅単位
        // "HIGH" or "AMPL"
        // HIGH:high/low AMPL:amplitude/offset
        //*************************************************
        private async Task PG_Set_DisplayUnitVolt(string usbid, string ver)
        {
            string command = $"DISP:UNIT:VOLT {ver}";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }
        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：PG 出力設定（出力インピーダンス）
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> PG設定変更対象ch
        // "1" or "2" 
        //引数3：load
        //説明：<string> PG出力インピーダンス設定
        // "50" or "INF"
        //*************************************************
        private async Task PG_Set_OutputLoad(string usbid, string ch, string load)
        {
            string command = $"OUTP{ch}:LOAD {load}";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：PG 出力設定（出力極性）
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> PG設定変更対象ch
        // "1" or "2" 
        //引数3：polarity
        //説明：<string> PG出力極性設定
        // "NORM" or "INV"
        //*************************************************
        private async Task PG_Set_OutputPol(string usbid, string ch, string polarity)
        {
            string command = $"OUTP{ch}:POL {polarity}";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：PG バーストモード設定
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> PG設定変更対象ch
        // "1" or "2" 
        //引数3：mode
        //説明：<string> PGバーストモード選択
        // "TRIG" or "GAT"
        //*************************************************
        private async Task PG_Set_BurstMode(string usbid, string ch, string mode)
        {
            string command = $"SOUR{ch}:BURS:MODE {mode}";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：PG バーストサイクル
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> PG設定変更対象ch
        // "1" or "2" 
        //引数3：count
        //説明：<uint> PGバースト回数
        // 1～100000000
        //*************************************************
        private async Task PG_Set_BurstCount(string usbid, string ch, uint count)
        {
            string command = $"SOUR{ch}:BURS:NCYC {count}";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：PG バーストON/OFF
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> PG設定変更対象ch
        // "1" or "2" 
        //引数3：sw
        //説明：<string> PGバーストON/OFF選択
        // "ON" or "OFF"
        //*************************************************
        private async Task PG_Set_Burst(string usbid, string ch, string sw)
        {
            string command = $"SOUR{ch}:BURS:STAT {sw}";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：PG トリガソース設定
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> PG設定変更対象ch
        // "1" or "2" 
        //引数3：source
        //説明：<string> PGトリガソース選択
        // "IMM" or "EXT" or "TIM" or "BUS" 
        //コメント
        // BUS:ソフトウェアトリガ(*TRG/TRIG)
        // EXT:外部トリガ
        // IMM:内部トリガ
        // TIM:タイマートリガ1us～8s
        //*************************************************
        private async Task PG_Set_TriggerSource(string usbid, string ch, string source)
        {
            string command = $"TRIG{ch}:SOUR {source}";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：PG Syncトリガ設定
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> Syncトリガソース
        // "1" or "2" 
        //コメント
        // Syncトリガ出力設定
        //*************************************************
        private async Task PG_Set_SyncTrigger(string usbid, string ch)
        {
            string command = "OUTP:SYNC ON;" +
                                $" SYNC:SOUR CH{ch};" +
                                $":OUTP{ch}:SYNC:MODE NORM;" +
                                $":OUTP{ch}:SYNC:POL NORM";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：PG トリガ設定
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> トリガソース
        // "1" or "2" 
        //引数3：sw
        //説明：<string> トリガ出力
        // "ON" or "OFF" 
        //コメント
        // トリガ出力設定
        // 出力極性は立ち上がりエッジ固定
        //*************************************************
        private async Task PG_Set_TriggerOUT(string usbid, string ch, string sw)
        {
            string command = $"OUTP:TRIG {sw};" +
                            $":OUTP:TRIG:SOUR CH{ch};" +
                            ":OUTP:TRIG:SLOP POS";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：PG 出力ON
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> PG設定変更対象ch
        // "1" or "2" 
        //*************************************************
        private async Task PG_OutputON(string usbid, string ch)
        {
            string command = $"OUTP{ch} ON";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：PG 出力OFF
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> PG設定変更対象ch
        // "1" or "2" 
        //*************************************************
        private async Task PG_OutputOFF(string usbid, string ch)
        {
            string command = $"OUTP{ch} OFF";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：PG CHトリガ発生
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：ch
        //説明：<string> PG設定変更対象ch
        // "1" or "2" 
        //コメント
        // コマンド経由のトリガ
        // 各ch毎にトリガ発生
        //*************************************************
        private async Task PG_ChTrigger(string usbid, string ch)
        {
            string command = $"TRIG{ch}";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：PG バストリガ発生
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //コメント
        // リモートインターフェース経由のトリガ
        // 2ch同時トリガ発生時に使用
        //*************************************************
        private async Task PG_BusTrigger(string usbid)
        {
            string command = "*TRG";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }
    }
}

