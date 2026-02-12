using USBcommunication;             //CommUSB.cs
using UTility;                      //Utility.cs
using TemperatureCharacteristics.Exceptions;    //例外スローの為

namespace THERMOcommunication
{
    public class THERMOcomm
    {
        private static THERMOcomm? instance;    //インスタンスをnull許容型(xxx?)として宣言し初期値がnullでも問題ないと表示
        private readonly USBcomm commSend;      //フィールド変数commSend
        private readonly USBcomm commQuery;     //フィールド変数commQuery
        private readonly UT utility;            //フィールド変数utility

        private THERMOcomm()
        {
            commSend = USBcomm.Instance;        // コンストラクタで初期化
            commQuery = USBcomm.Instance;       // コンストラクタで初期化
            utility = UT.Instance;
        }
        public static THERMOcomm Instance     //外部からのアクセス用
        {
            get
            {
                if (instance == null)
                    instance = new THERMOcomm(); // クラス内でインスタンスを生成
                return instance;
            }
        }

        //*************************************************
        //アクセス：public
        //戻り値：なし(Task)
        //機能：THERMO 初期化
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //以下省略可能
        //引数2：sensor
        //説明：<string> 温度測定に使用するセンサーの種類 default =1:typeT
        // 0:no_sensor 1:typeT 2:typeK 3: 4:
        //引数3：control
        //説明：<string> エアーのコントロール方法 default =1:DUT_control
        // 0:air_control 1:DUT_control 2
        //引数4：point
        //説明：<string> 温度ポイントの選択 default =1:ambient
        // 0:hot 1:ambient 2:cold
        //コメント
        // Soak時間は600sで固定しているがdebugアクセス可能にする
        //*************************************************
        public async Task THERMO_Initialize(string thermoID, CancellationToken cancellationToken = default,
            string soak = "600", string sensor = "1", string control = "1", string point = "1")
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();       //リセット前にキャンセルチェック
                //await THERMO_Reset(thermoID);                         //リセットするとプログラムモードに遷移してしまうためコメントアウト
                for (short i = 0; i < 3; i++)
                {
                    await THERMO_Set_poin(thermoID, i.ToString());
                    await THERMO_Set_ramp(thermoID, "20.0");                    //RAMP 20℃/min
                    await THERMO_Set_soak(thermoID, soak);                        //Soak時間は600sで固定 debugアクセスで変更可能なように修正予定
                }
                cancellationToken.ThrowIfCancellationRequested();       //各種設定前にキャンセルチェック
                await commSend.Comm_sendB(thermoID, "ULIM 150;LLIM -80");        //温度リミット設定固定 上限+150℃ 下限-80℃
                await THERMO_Set_sens(thermoID, sensor);                         //default 熱電対TypeT
                await THERMO_Set_ctrl(thermoID, control);                        //default DUTコントロール
                await THERMO_Set_poin(thermoID, point);                          //default ambient選択
                await THERMO_Set_flow(thermoID, "0");                           //Flow OFF
            }
            catch (OperationCanceledException)
            {
                throw;      //キャンセル要求を検知したら呼び出し元に通知
            }
            catch (MeasWarningException ex)        //例外処理
            {
                throw new MeasFatalException($"# FATAL:THERMO イニシャルでエラーが発生しました: {ex.Message}\n");
            }
            catch (MeasFatalException ex)        //例外処理
            {
                throw new MeasFatalException($"# FATAL:THERMO イニシャルでエラーが発生しました: {ex.Message}\n");
            }
            catch (Exception ex)        //例外処理
            {
                throw new MeasFatalException($"# FATAL:THERMO イニシャルでエラーが発生しました: {ex.Message}\n");
            }
        }

        //*************************************************
        //アクセス：public
        //戻り値：なし(Task)
        //機能：THERMO 温度設定+安定待ち
        //　　　温度を各ポイントに設定後、Flow ON
        //　　　温度安定完了(Soak時間経過)でTrue、安定待ち中はFalse
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：temp
        //説明：<float> 設定温度(℃)
        // -55.0～155.0
        //コメント
        // 30min(1800000msec)でタイムアウト
        //*************************************************
        public async Task THERMO_WaitStability(
                                            string thermoID, float temp, 
                                            CancellationToken cancellationToken = default,
                                            int timeoutMs = 30 * 60 * 1000)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();       //設定変更前キャンセルチェック
                bool stability = false;
                if (temp > 30)
                    await THERMO_Set_poin(thermoID, "0");     //SETN 0:hot
                else if (temp < 20)
                    await THERMO_Set_poin(thermoID, "2");     //SETN 2:cold
                else
                    await THERMO_Set_poin(thermoID, "1");    //SETN 1:ambient

                await THERMO_Set_temp(thermoID, temp);
                await THERMO_Set_head(thermoID, "1");        //Head down
                await utility.Wait_Timer(1500, cancellationToken);  //Head Down待ち
                await THERMO_Set_flow(thermoID, "1");        //Flow ON

                var sw = System.Diagnostics.Stopwatch.StartNew();   //StopWatchスタート

                while (!stability)                      //温度安定待ちポーリング
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string response = await THERMO_Response(thermoID, cancellationToken);
                    //stability = ((Convert.ToByte(response) << 7) == 128);
                    stability = ((Convert.ToByte(response) & 0x01) == 1);
                    if (stability)
                        break;
                    //*********************
                    //30min経っても安定しない場合
                    //*********************
                    if (sw.ElapsedMilliseconds > timeoutMs)
                    {
                        await THERMO_Set_poin(thermoID, "1");
                        await THERMO_Set_flow(thermoID, "1");
                        throw new TimeoutException("# FATAL:THERMO 温度が30分以内に安定しませんでした。\n");
                    }
                    await utility.Wait_Timer(500, cancellationToken);     //500ms毎にTHERMO_Responseメソッドでレジスタ確認
                }
            }
            catch (OperationCanceledException)
            {
                throw;      //キャンセル要求を検知したら呼び出し元に通知
            }
            catch (MeasWarningException ex)        //例外処理
            {
                await THERMO_Set_flow(thermoID, "0");        //Flow OFF
                throw new MeasFatalException($"# FATAL:THERMO 温度安定待ちでエラーが発生しました: {ex.Message}\n");
            }
            catch (MeasFatalException ex)        //例外処理
            {
                await THERMO_Set_flow(thermoID, "0");        //Flow OFF
                throw new MeasFatalException($"# FATAL:THERMO 温度安定待ちでエラーが発生しました: {ex.Message}\n");
            }
            catch (Exception ex)        //例外処理
            {
                await THERMO_Set_flow(thermoID, "0");        //Flow OFF
                throw new MeasFatalException($"# FATAL:THERMO 温度安定待ちでエラーが発生しました: {ex.Message}\n");
            }
        }
        //*************************************************
        //アクセス：public
        //戻り値：なし(Task)
        //機能：THERMO 温度設定+安定待ち
        //　　　温度を各ポイントに設定後、Flow ON
        //　　　温度安定完了(Soak時間経過)でTrue、安定待ち中はFalse
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //*************************************************
        public async Task THERMO_Finalize(string thermoID, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();       //設定変更前キャンセルチェック
                bool stability = false;
                await THERMO_Set_poin(thermoID, "1");    //SETN 1:ambient
                await THERMO_Set_temp(thermoID, 25.0f);

                while (!stability)                      //温度安定待ちポーリング
                {
                    string response = await THERMO_Response(thermoID, cancellationToken);
                    //stability = ((Convert.ToByte(response) << 7) == 128);
                    stability = ((Convert.ToByte(response) & 0x01) == 1);
                    if (!stability)
                        await utility.Wait_Timer(500, cancellationToken);     //500ms毎にTHERMO_Responseメソッドでレジスタ確認
                }
            }
            catch (OperationCanceledException)
            {
                throw;      //キャンセル要求を検知したら呼び出し元に通知
            }
            catch (MeasWarningException ex)        //例外処理
            {
                await THERMO_Set_flow(thermoID, "0");        //Flow OFF
                throw new MeasFatalException($"# FATAL:THERMO 温度安定待ちでエラーが発生しました: {ex.Message}\n");
            }
            catch (MeasFatalException ex)        //例外処理
            {
                await THERMO_Set_flow(thermoID, "0");        //Flow OFF
                throw new MeasFatalException($"# FATAL:THERMO 温度安定待ちでエラーが発生しました: {ex.Message}\n");
            }
            catch (Exception ex)        //例外処理
            {
                await THERMO_Set_flow(thermoID, "0");        //Flow OFF
                throw new MeasFatalException($"# FATAL:THERMO 温度安定待ちでエラーが発生しました: {ex.Message}\n");
            }
            finally
            {
                await THERMO_Set_flow(thermoID, "0");        //Flow OFF
                await THERMO_Set_head(thermoID, "0");        //Head up
            }
        }
        //*************************************************
        //アクセス：public
        //戻り値：なし(Task)
        //機能：THERMO Remote解除
        //引数1：thermoID
        //説明：<string> サーモとの通信用ID
        //コメント
        // THERMOのリモート解除用
        //*************************************************
        public async Task THERMO_FlowOFF(string thermoID)
        {
            try
            {
                //**********************************
                //動作
                //**********************************
                await THERMO_Set_flow(thermoID, "0");        //Flow OFF
            }
            catch (MeasWarningException ex)
            {
                throw new MeasFatalException($"# FATAL:THERMO FlowOffでエラー {ex.Message}\n", ex);
            }
            catch (MeasFatalException ex)
            {
                throw new MeasFatalException($"# FATAL:THERMO FlowOffでエラー {ex.Message}\n", ex);
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"# THERMO FlowOffでエラー: {ex.Message}");
                throw new MeasFatalException($"# FATAL:THERMO FlowOffでエラー {ex.Message}\n", ex);
            }

        }
        //*************************************************
        //アクセス：public
        //戻り値：なし(Task)
        //機能：THERMO Remote解除
        //引数1：thermoID
        //説明：<string> サーモとの通信用ID
        //コメント
        // THERMOのリモート解除用
        //*************************************************
        public async Task THERMO_RemoteOFF(string thermoID)
        {
            try
            {
                //**********************************
                //動作
                //**********************************
                await commSend.Remote_OFF(thermoID);
            }
            catch (MeasWarningException ex)
            {
                throw new MeasWarningException($"# WARN:THERMO リモート解除エラー {ex.Message}\n", ex);
            }
            catch (MeasFatalException ex)
            {
                throw new MeasFatalException($"# FATAL:THERMO リモート解除エラー {ex.Message}\n", ex);
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"# THERMOリモート解除でエラー: {ex.Message}");
                throw new MeasFatalException($"# UNKNOWN:THERMO リモート解除エラー {ex.Message}\n", ex);
            }

        }
        //以下privateアクセス**************************************************************************************************
        //*************************************************
        //アクセス：private
        //戻り値：なし(Task)
        //機能：THERMO リセット
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //コメント
        // THERMOに*OPCが実装されていないので4sの時間経過待ち
        //*************************************************
        private async Task THERMO_Reset(string usbid)
        {
            //string command = "*RST";
            string command = "*RSTO";                           //リセット後オペレータモードへ遷移
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
            await utility.Wait_Timer(4100);                         //OPC未実装の為4000ms+100msWait 仕様書では4s待ち
        }

        //*************************************************
        //アクセス：private
        //戻り値：<string> イベントレジスタ値
        //機能：THERMO イベントレジスタ問い合わせ
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //*************************************************
        private async Task<string> THERMO_Response(string usbid, CancellationToken ct)
        {
            string command = "TECR?";
            string responce = await commQuery.Comm_queryB(usbid, command, ct);      //リモート解除無し
            return responce;
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：THERMO 温度設定
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：temp
        //説明：<float> 設定温度(℃)
        // -55.0～155.0
        //*************************************************
        private async Task THERMO_Set_temp(string usbid, float temp)
        {
            string command = $"SETP {temp}";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：public
        //戻り値：なし
        //機能：THERMO Soak時間設定
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：soak
        //説明：<string> Soak時間(s)
        // 0～600
        //コメント
        // debugアクセス用にPublic
        //*************************************************
        public async Task THERMO_Set_soak(string usbid, string soak)
        {
            string command = $"SOAK {soak}";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：THERMO Sensor設定
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：sensor
        //説明：<string> Sensorタイプ選択
        // 0:no_sensor 1:typeT 2:typeK 3: 4:
        //*************************************************
        private async Task THERMO_Set_sens(string usbid, string sensor)
        {
            string command = $"DSNS {sensor}";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：THERMO 温度ポイント設定
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：point
        //説明：<string> 設定変更する温度ポイント選択
        // 0:hot 1:ambient 2:cold
        //*************************************************
        private async Task THERMO_Set_poin(string usbid, string point)
        {
            string command = $"SETN {point}";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：THERMO 温度コントロール設定
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：control
        //説明：<string> 温度コントロール方法を選択
        // 0:air_control 1:DUT_control 2
        //*************************************************
        private async Task THERMO_Set_ctrl(string usbid, string control)
        {
            string command = $"DUTM {control}";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：THERMO Flow設定
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：flow
        //説明：<string> Flow ON/OFF
        // 0:off 1:on
        //*************************************************
        private async Task THERMO_Set_flow(string usbid, string flow)
        {
            string command = $"FLOW {flow}";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：THERMO ヘッド制御
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：head
        //説明：<string> ヘッド UP/DOWN
        // 0:up 1:down
        //*************************************************
        private async Task THERMO_Set_head(string usbid, string head)
        {
            string command = $"HEAD {head}";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：THERMO Flow rate調整
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：rate
        //説明：<ushort> Flow量
        // 5～18
        //*************************************************
        private async Task THERMO_Set_rate(string usbid, ushort rate)
        {
            string command = $"FLWM {rate}";     //コマンドFLSEも可
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし
        //機能：THERMO Ramp設定
        //引数1：usbid
        //説明：<string> 通信用アドレス
        // USB or GPIB
        //引数2：rate
        //説明：<string> RAMP値（℃/min）
        // 5～18
        //*************************************************
        private async Task THERMO_Set_ramp(string usbid, string ramp)
        {
            string command = $"RAMP {ramp}";
            await commSend.Comm_sendB(usbid, command);          //リモート解除を無効にして送信
        }
    }
}

