using InputCheck;                   //ErrCheck.cs
using THERMOcommunication;          //CommTHERMO.cs
using UTility;                      //Utility.cs
using TemperatureCharacteristics.Exceptions;    //例外スロー

namespace TemperatureCharacteristics.Services.Actions
{
    public class Thermo
    {
        private static Thermo? instance;             //インスタンスをnull許容型(xxx?)として宣言し初期値がnullでも問題ないと表示
        private readonly THERMOcomm commTHERMO;     //フィールド変数commTHERMO
        private readonly UT utility;                //フィールド変数utility
        private readonly InpCheck errCheck;         //フィールド変数errCheck

        private Thermo()
        {
            //初期化する変数
            utility = UT.Instance;                  //インスタンス生成(=初期化)を別クラス内で実行
            commTHERMO = THERMOcomm.Instance;       //インスタンス生成(=初期化)を別クラス内で実行
            errCheck = InpCheck.Instance;           //インスタンス生成(=初期化)を別クラス内で実行
        }
        public static Thermo Instance     //外部からのアクセス用
        {
            get
            {
                if (instance == null)
                    instance = new Thermo(); // クラス内でインスタンスを生成
                return instance;
            }
        }
        //*************************************************
        //アクセス：public
        //戻り値：<bool> Thermo Initialize結果
        //機能：Thermo初期化
        //引数1：meas_inst
        //説明：
        //*************************************************
        public async Task<(bool Success, List<string> LogRows)> ThermoInitial(
                                            List<(bool IsChecked, string UsbId, string InstName, string Identifier)> meas_inst,
                                            CancellationToken cancellationToken = default,
                                            string soakTime = "600")
        {
            cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
            var log = new List<string>();                           //エラーメッセージ保管ログ
            string? thermoId = meas_inst
                                .FirstOrDefault(x => x.IsChecked && x.Identifier == "THERMO")
                                .UsbId;
            if (string.IsNullOrEmpty(thermoId))
            {
                log.Add("# サーモストリーマが選択されていません");
                return (false, log);
            }
            try
            {
                await commTHERMO.THERMO_Initialize(thermoId, cancellationToken, soakTime);
            }
            catch (OperationCanceledException)
            {
                log.Add("# サーモストリーマ初期設定がキャンセルされました。");
                throw;          //キャンセル要求を検知したら呼び出し元に通知
            }
            catch (MeasWarningException ex)
            {
                log.Add($"サーモストリーマ初期設定中に警告レベルエラーが発生しました: {ex.Message}");
                return (false, log);
            }
            catch (MeasFatalException ex)
            {
                log.Add($"サーモストリーマ初期設定中に致命レベルエラーが発生しました: {ex.Message}");
                return (false, log);
            }
            catch (Exception ex)
            {
                log.Add($"サーモストリーマ初期設定中にエラーが発生しました: {ex.Message}");
                return (false, log);
            }
            return (true, log);
        }

        //*************************************************
        //アクセス：public
        //戻り値：<bool> Thermo 温度変化+安定待ち時間結果
        //機能：Thermo温度変化＋安定待ち
        //引数1：meas_inst
        //説明：
        // 30分経過してもサーモストリーマの温度が安定しない場合タイムアウト
        //*************************************************
        public async Task<(bool Success, List<string> LogRows)> ThermoAction(
                                            List<(bool IsChecked, string UsbId, string InstName, string Identifier)> meas_inst,
                                            float temperature,
                                            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
            var log = new List<string>();                           //エラーメッセージ保管ログ
            string? thermoId = meas_inst
                                .FirstOrDefault(x => x.IsChecked && x.Identifier == "THERMO")
                                .UsbId;
            if (string.IsNullOrEmpty(thermoId))
            {
                log.Add("# サーモストリーマが選択されていません");
                return (false, log);
            }
            try
            {
                await commTHERMO.THERMO_WaitStability(thermoId, temperature, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                log.Add("# 温度設定がキャンセルされました。");
                throw;          //キャンセル要求を検知したら呼び出し元に通知
            }
            catch (TimeoutException tex)
            {
                log.Add($"# 温度安定待ちタイムアウト: {tex.Message}");
                return (false, log);
            }
            catch (MeasWarningException ex)
            {
                log.Add($"# 温度設定中に警告レベルエラーが発生しました: {ex.Message}");
                return (false, log);
            }
            catch (MeasFatalException ex)
            {
                log.Add($"# 温度設定中に致命レベルエラーが発生しました: {ex.Message}");
                return (false, log);
            }
            catch (Exception ex)
            {
                log.Add($"# 温度設定中にエラーが発生しました: {ex.Message}");
                return (false, log);
            }
            await commTHERMO.THERMO_RemoteOFF(thermoId);
            return (true, log);
        }

        //*************************************************
        //アクセス：public
        //戻り値：<bool> Thermo 温度変化
        //機能：Thermo温度変化
        //引数1：meas_inst
        //説明：
        //コメント
        //　終了処理用
        //*************************************************
        public async Task SetThermoTo25C(List<(bool IsChecked, string UsbId, string InstName, string Identifier)> meas_inst,
                                            CancellationToken cancellationToken = default)
        {
            string? thermoId = meas_inst
                                .FirstOrDefault(x => x.IsChecked && x.Identifier == "THERMO")
                                .UsbId;
            if (string.IsNullOrEmpty(thermoId))
                return;
            await commTHERMO.THERMO_Finalize(thermoId, cancellationToken);
            await commTHERMO.THERMO_RemoteOFF(thermoId);
            return;
        }
        //*************************************************
        //アクセス：public
        //戻り値：<bool> Thermo 温度変化
        //機能：Thermo温度変化
        //引数1：meas_inst
        //説明：
        //コメント
        //　終了処理用
        //*************************************************
        public async Task SetThermoFlowOff(List<(bool IsChecked, string UsbId, string InstName, string Identifier)> meas_inst)
        {
            string? thermoId = meas_inst
                                .FirstOrDefault(x => x.IsChecked && x.Identifier == "THERMO")
                                .UsbId;
            if (string.IsNullOrEmpty(thermoId))
                return;
            await commTHERMO.THERMO_FlowOFF(thermoId);
            await commTHERMO.THERMO_RemoteOFF(thermoId);
            return;
        }
    }
}
