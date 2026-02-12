using InputCheck;
using System.Windows;
using TemperatureCharacteristics.Models;
using TemperatureCharacteristics.Services.Actions;
using TemperatureCharacteristics.Services.Measurement;
using TemperatureCharacteristics.ViewModels.Debug;
using TemperatureCharacteristics.ViewModels.Devices;
using TemperatureCharacteristics.ViewModels.Tabs.Delay;
using TemperatureCharacteristics.ViewModels.Tabs.Sweep;
using TemperatureCharacteristics.ViewModels.Tabs.VI;
using UTility;

namespace TemperatureCharacteristics.Services
{
    public class MeasurementManager
    {
        private readonly IMeasurementContext _ctx;
        private readonly RelayViewModel _relayAct;
        private readonly Thermo _thermoAct;
        private readonly Sweep _sweepAct;
        private readonly Delay _delayAct;
        private readonly VI _viAct;
        private readonly DebugViewModel _debug;
        private readonly UT _utility;
        private readonly InpCheck _errCheck;

        private CancellationTokenSource _cts;
        private List<(bool, string, string, string)> _measInstData;
        private List<string> _rows;
        private string _finalFileName;
        private DebugOption _debugOption;

        private List<Device>? _cachedSweepDevices;  //プロパティ値のキャッシュ用
        private List<Device>? _cachedDelayDevices;  //プロパティ値のキャッシュ用
        private List<Device>? _cachedVIDevices;     //プロパティ値のキャッシュ用

        public MeasurementManager(
                                    IMeasurementContext ctx,
                                    RelayViewModel relayAct,
                                    Thermo thermoAct,
                                    Sweep sweepAct,
                                    Delay delayAct,
                                    VI viAct,
                                    DebugViewModel debug,
                                    UT utility,
                                    InpCheck errCheck)
        {
            _ctx = ctx;
            _relayAct = relayAct;
            _thermoAct = thermoAct;
            _sweepAct = sweepAct;
            _delayAct = delayAct;
            _viAct = viAct;
            _debug = debug;
            _utility = utility;
            _errCheck = errCheck;
        }

        //****************************************************************************
        // メイン実行
        //****************************************************************************
        public async Task RunAsync(string finalFileName)
        {
            _ctx.IsRunning = true;
            _ctx.MeasurementStatus = "測定中...";
            _rows = new List<string>();
            _cts = new CancellationTokenSource();
            bool wasCanceled = false;
            _utility.ResetTabFileCounters();
            _finalFileName = finalFileName;

            try
            {
                //*********************
                //1. DebugOption取り込み
                //*********************
                PrepareDebugOption();
                //*********************
                //2.測定器アドレス取り込み
                //*********************
                CollectInstrumentAddr();
                //*********************
                //3.測定器アドレスチェック
                //*********************
                await CheckInstrumentAddresses();
                //*********************
                //4.測定対象タブが選択されているか確認
                //*********************
                if (!_ctx.SweepTabs.Tabs.Any(t => t.MeasureOn) &&
                    !_ctx.DelayTabs.Tabs.Any(t => t.MeasureOn) &&
                    !_ctx.VITabs.Tabs.Any(t => t.MeasureOn))
                {
                    _rows.Add("# 測定対象が選択されていません");
                    return;
                }
                //*********************
                //5.タブ設定の取得（キャッシュ生成）
                //*********************
                await PrepareTabSettings();
                //*********************
                //6.温度ループ or リレー測定ループ
                //*********************
                if (_ctx.MultiTemperature)
                    await ExecuteTemperatureLoop();
                else
                    await ExecuteRelayLoopOnly();
                //*********************
                //7.最終データ保存
                //*********************
                await SaveFinalData();
            }
            catch (OperationCanceledException)
            {
                //*********************
                //キャンセル要求をキャッチしたら
                //*********************
                wasCanceled = true;
                _rows.Add("# 測定が中断 中間データ保存済");
            }
            catch (Exception ex)
            {
                _rows.Add($"# 測定エラー: {ex.Message}");
                _ctx.LogDebug($"例外: {ex.Message}");
            }
            finally
            {
                //*********************
                //8.終了処理（UI復帰・サーモ処理・メッセージ表示）
                //*********************
                await FinalizeMeasurement(_rows, _measInstData, wasCanceled);
            }
        }

        //****************************************************************************
        // 1. DebugOption取り込み
        //****************************************************************************
        private void PrepareDebugOption()
        {
            _debugOption = new DebugOption
            {
                FinalFileFotterRemove = _debug.DebugFinalFileFotterRemove,
                Use8chOSC = _debug.DebugUse8chOSC,
                StopOnWarning = _debug.DebugStopOnWarning,
                EditThermoSoak = _debug.DebugEditThermoSoak,
                ThermoSoakTime = _debug.DebugThermoSoakTime
            };
        }

        //****************************************************************************
        // 2. 測定器アドレス取り込み
        //****************************************************************************
        private void CollectInstrumentAddr()
        {
            _measInstData =
                _ctx.MeasInst.Select(inst =>
                {
                    string usbId = inst.UsbId ?? "";
                    string instName = inst.InstName ?? "";
                    string identifier = inst.Identifier ?? "Unknown";

                    return (
                        inst.IsChecked,
                        usbId,
                        instName,
                        identifier
                    );
                }).ToList();
        }

        //****************************************************************************
        // 3. 測定器アドレスチェック
        //****************************************************************************
        private async Task CheckInstrumentAddresses()
        {
            var (messages, ok) = await _errCheck.VerifyInsAddr(_measInstData);

            if (messages.Any())
                _rows.Add("# " + string.Join(" ", messages));

            if (!ok)
                throw new Exception("測定器アドレスに不備があります");
        }

        //****************************************************************************
        // 5. タブ設定の取得（Sweep/Delay/VI）
        //****************************************************************************
        private async Task PrepareTabSettings()
        {
            var warnings = new List<string>();
            var tasks = new List<Task<List<Device>>>();
            //*********************
            //並列処理準備（プロパティ取得＋電源Autoレンジチェック
            //*********************
            if (_ctx.SweepTabs.Tabs.Any(t => t.MeasureOn))
                tasks.Add(GetSweepDevices(_measInstData, _ctx.SweepTabs, warnings));

            if (_ctx.DelayTabs.Tabs.Any(t => t.MeasureOn))
                tasks.Add(GetDelayDevices(_measInstData, _ctx.DelayTabs, warnings));

            if (_ctx.VITabs.Tabs.Any(t => t.MeasureOn))
                tasks.Add(GetVIDevices(_measInstData, _ctx.VITabs, warnings));
            //*********************
            //並列処理実行＋結果をマッピング
            //*********************
            var results = await Task.WhenAll(tasks);
            //*********************
            //結果をマッピング（キャッシュ生成）
            //*********************
            int i = 0;
            if (_ctx.SweepTabs.Tabs.Any(t => t.MeasureOn))
                 _cachedSweepDevices = results[i++];

            if (_ctx.DelayTabs.Tabs.Any(t => t.MeasureOn))
                _cachedDelayDevices = results[i++];

            if (_ctx.VITabs.Tabs.Any(t => t.MeasureOn))
                _cachedVIDevices = results[i++];
            //*********************
            //測定開始前確認
            //*********************
            if (warnings.Any())
            {
                bool ok = _ctx.DialogService.ShowConfirm(
                    $"電源のRangeがAUTOになっているItemがあります。\n" +
                    string.Join("\n", warnings) + "\n\n" +
                    $"Sweep電源をAUTO設定のまま開始すると印加値に影響を及ぼす可能性があります。\n" +
                    $"このまま測定を開始しますか？",
                    "測定開始確認"
                );

                if (!ok)
                    throw new Exception("ユーザーが測定をキャンセルしました");

            }
        }

        //****************************************************************************
        // 6. 温度制御ループ
        //****************************************************************************
        private async Task ExecuteTemperatureLoop()
        {
            //*********************
            //サーモ初期化
            //*********************
            var (ok, log) = await _thermoAct.ThermoInitial(_measInstData, _cts.Token, _debugOption.ThermoSoakTime);
            if (!ok)
                throw new Exception("サーモ初期化失敗");
            //*********************
            //温度変化ループ
            //*********************
            foreach (var temp in _ctx.Temperatures)
            {
                _cts.Token.ThrowIfCancellationRequested();
                //*********************
                //温度変化＋安定待ち
                //*********************
                _ctx.MeasurementStatus = $"サーモ 温度{temp}℃ 設定+安定待ち...";
                (ok, log) = await _thermoAct.ThermoAction(_measInstData, temp, _cts.Token);

                if (!ok)
                {
                    _rows.Add($"# サーモ 温度{temp}℃ 設定失敗");
                    continue;
                }

                _ctx.MeasurementStatus = $"サーモ 温度{temp}℃ 設定完了";
                _rows.Add($"サーモ 温度{temp}℃");

                await ExecuteRelayLoopOnly();
            }
        }

        //****************************************************************************
        // 7. リレー測定ループ
        //****************************************************************************
        private async Task ExecuteRelayLoopOnly()
        {
            await _relayAct.ExcuteMeasurementRelayLoop(
                _measInstData,
                _rows,
                _debugOption,
                _cts.Token,
                async (measInstData, rows, option, token) =>
                {
                    await MeasurementTabs(measInstData, rows, option, _cachedSweepDevices, _cachedDelayDevices, _cachedVIDevices, token);
                },
                status => _ctx.MeasurementStatus = status
            );
        }

        //****************************************************************************
        // 8. 結果保存
        //****************************************************************************
        private async Task SaveFinalData()
        {
            var dataRows = _rows.Where(r => !r.StartsWith("#")).ToList();
            if (dataRows.Count <= 1)
                return;
            string finalPath = _utility.GetFinalFilePath(_finalFileName);
            var pivot = _ctx.CreatePivotRows(dataRows, _ctx.MultiTemperature, _relayAct.MultiSample);

            string footer = _ctx.BuildSettingsFooter(_measInstData);
            if (!_debugOption.FinalFileFotterRemove)
                pivot.AddRange(footer);

            await _utility.WriteCsvFileAsync(finalPath, pivot, false, true);
        }

        //****************************************************************************
        // 9. 終了処理
        //****************************************************************************
        private async Task FinalizeMeasurement(
            List<string> rows,
            List<(bool IsChecked, string UsbId, string InstName, string Identifier)> measInstData,
            bool wasCanceled)
        {
            try
            {
                //*********************
                // コメント行（#）を抽出してメッセージ生成
                //*********************
                var comments = rows
                    .Where(r => r.StartsWith("#"))
                    .Select(r => r.Substring(2).Trim());

                string message = comments.Any()
                    ? string.Join(Environment.NewLine, comments)
                    : "測定は正常に終了";

                //*********************
                // コメント行を除外したデータ行
                //*********************
                var dataRows = rows.Where(r => !r.StartsWith("#")).ToList();

                if (dataRows.Count > 1)
                {
                    try
                    {
                        //*********************
                        // 最終データ保存先
                        //*********************
                        string finalFilePath = _utility.GetFinalFilePath(_finalFileName);

                        //*********************
                        // データ並び替え（ピボット）
                        //*********************
                        var pivotRows = _ctx.CreatePivotRows(
                            dataRows,
                            _ctx.MultiTemperature,
                            _relayAct.MultiSample);

                        //*********************
                        // フッター生成
                        //*********************
                        string footer = _ctx.BuildSettingsFooter(measInstData);
                        if (!_debugOption.FinalFileFotterRemove)
                            pivotRows.AddRange(footer);

                        //*********************
                        // CSV 保存
                        //*********************
                        await _utility.WriteCsvFileAsync(
                            finalFilePath,
                            pivotRows,
                            append: false,
                            useShiftJis: true);

                        message += $"{Environment.NewLine}データが {finalFilePath} に保存されました。";
                    }
                    catch (Exception ex)
                    {
                        message += $"{Environment.NewLine}最終データ保存エラー: {ex.Message}";
                    }
                }
                else
                {
                    message += $"{Environment.NewLine}測定データがありませんでした。";
                }

                //*********************
                // サーモ終了処理（MultiTemperature の場合）
                //*********************
                if (_ctx.MultiTemperature)
                {
                    if (!wasCanceled)
                    {
                        _ctx.MeasurementStatus = "終了処理サーモ温度25℃ 設定+安定待ち...";
                        try
                        {
                            await _thermoAct.SetThermoTo25C(measInstData, _cts.Token);
                            _ctx.MeasurementStatus = "終了処理サーモ温度25℃ 設定完了";
                        }
                        catch
                        {
                            _ctx.MeasurementStatus = "終了処理サーモ温度25℃ 設定失敗";
                        }
                    }
                    else
                    {
                        await _thermoAct.SetThermoFlowOff(measInstData);
                        _ctx.MeasurementStatus = "測定がキャンセルされたため、温度安定待ち中断";
                    }
                }

                //*********************
                // 測定完了メッセージ表示
                //*********************
                _ctx.DialogService.ShowMessage(message, "処理完了報告");
            }
            finally
            {
                //*********************
                //測定ステータス更新
                //*********************
                _ctx.IsRunning = false;
                _ctx.MeasurementStatus = "測定開始ボタン受付中";

                _cts?.Dispose();
                _cts = null;
            }
        }
        //****************************************************************************
        // キャンセルトークン発生
        //****************************************************************************
        public void Cancel()
        { 
            _cts?.Cancel();
        }
        //****************************************************************************
        // Sweep デバイス取得＋AutoRangeチェック
        //****************************************************************************
        public async Task<List<Device>> GetSweepDevices(
                                                List<(bool IsChecked, string UsbId, string InstName, string Identifier)> measInstData,
                                                SweepTabGroupViewModel sweepTabs,
                                                List<string> warningMessages)
        {
            var tabData = sweepTabs.GetMeasureOnTabData();  //SweepTabData.csの要素をタブから抜き出し
            if (!tabData.Any())
                return new List<Device>();

            //CombineDeviceDataで紐づけ
            var devices = _sweepAct.CombineDeviceData(measInstData, tabData);

            //AutoRange チェック
            var messages = await CheckAutoRange(devices, "Sweep");
            warningMessages.AddRange(messages);

            return devices;
        }

        //****************************************************************************
        // Delay デバイス取得＋AutoRangeチェック
        //****************************************************************************
        public async Task<List<Device>> GetDelayDevices(
                                                List<(bool IsChecked, string UsbId, string InstName, string Identifier)> measInstData,
                                                DelayTabGroupViewModel delayTabs,
                                                List<string> warningMessages)
        {
            var tabData = delayTabs.GetMeasureOnTabData(); // IEnumerable<DelayTabData>
            if (!tabData.Any())
                return new List<Device>();

            var devices = _delayAct.CombineDeviceData(measInstData, tabData);

            var messages = await CheckAutoRange(devices, "Delay");
            warningMessages.AddRange(messages);

            return devices;
        }

        //****************************************************************************
        // VI デバイス取得＋AutoRangeチェック
        //****************************************************************************
        public async Task<List<Device>> GetVIDevices(
                                                List<(bool IsChecked, string UsbId, string InstName, string Identifier)> measInstData,
                                                VITabGroupViewModel viTabs,
                                                List<string> warningMessages)
        {
            var tabData = viTabs.GetMeasureOnTabData(); // IEnumerable<VITabData>
            if (!tabData.Any())
                return new List<Device>();

            var devices = _viAct.CombineDeviceData(measInstData, tabData);

            var messages = await CheckAutoRange(devices, "VI");
            warningMessages.AddRange(messages);

            return devices;
        }
        //****************************************************************************
        // AutoRange チェック共通処理
        //****************************************************************************
        private async Task<List<string>> CheckAutoRange(List<Device> devices, string tabType)
        {
            //devices からタブ名をすべて抽出
            var actualTabNames = devices
                .SelectMany(d => d.TabSettings.Keys)
                .Distinct()
                .ToArray();
            //各タブのSOURCEにAutoレンジが含まれていないかチェック
            var (messages, _) = await _errCheck.SourceRangeAuto(devices, actualTabNames, tabType);

            return messages;
        }
        //****************************************************************************
        //動作
        // 各Tab測定
        //****************************************************************************
        public async Task MeasurementTabs(
                                        List<(bool, string, string, string)> measInstData,
                                        List<string> rows,
                                        DebugOption option,
                                        List<Device>? sweepDevices,
                                        List<Device>? delayDevices,
                                        List<Device>? viDevices,
                                        CancellationToken cancellationToken = default)
        {
            //*********************
            //Sweep測定
            //*********************
            if (_ctx.SweepTabs.Tabs.Any(t => t.MeasureOn))
            {
                //子タブは無効
                //_ctx.SweepTabs.IsSmallTabCtrlEnabled = false;
                _ctx.MeasurementStatus += "Sweep測定中...";

                var tabData = _ctx.SweepTabs.GetMeasureOnTabData();
                var tabNames = _ctx.SweepTabs.CheckedTabNames;

                var sweepRows = await _sweepAct.SWEEPAction(measInstData, tabData, tabNames, option, cancellationToken, sweepDevices);
                rows.AddRange(sweepRows);
            }

            //*********************
            //Delay測定
            //*********************
            if (_ctx.DelayTabs.Tabs.Any(t => t.MeasureOn))
            {
                //子タブは無効
                //_ctx.DelayTabs.IsSmallTabCtrlEnabled = false;
                _ctx.MeasurementStatus += "Delay測定中...";

                var tabData = _ctx.DelayTabs.GetMeasureOnTabData();
                var tabNames = _ctx.DelayTabs.CheckedTabNames;

                var delayRows = await _delayAct.DELAYAction(measInstData, tabData, tabNames, option, cancellationToken, delayDevices);
                rows.AddRange(delayRows);
            }

            //*********************
            //VI測定
            //*********************
            if (_ctx.VITabs.Tabs.Any(t => t.MeasureOn))
            {
                //子タブは無効
                //_ctx.VITabs.IsSmallTabCtrlEnabled = false;
                _ctx.MeasurementStatus += "VI測定中...";

                var tabData = _ctx.VITabs.GetMeasureOnTabData();      // IEnumerable<VITabData>
                var tabNames = _ctx.VITabs.CheckedTabNames;            // string[]

                var viRows = await _viAct.VIAction(measInstData, tabData, tabNames, option, cancellationToken, viDevices);
                rows.AddRange(viRows);
            }
        }

    }
}
