using UTility;

namespace InputCheck.Checkers
{
    //SweepTab用チェック
    internal class SweepChecker
    {
        private readonly UT _utility;
        private readonly NumericChecker _numericChecker;
        private readonly RangeChecker _rangeChecker;
        //private readonly CommonChecker _commonChecker;

        public SweepChecker(UT utility)
        {
            _utility = utility;
            _numericChecker = new NumericChecker(utility);
            _rangeChecker = new RangeChecker(utility);
            //_commonChecker = new CommonChecker();
        }
        //*************************************************
        //SweepTabチェック(リスト)
        // 戻り値：(List<string>, bool) エラーメッセージリストとチェック結果
        // 引数1：deviceList - デバイスリスト
        // 引数2：tabNames - Tabリスト
        //*************************************************
        public async Task<(List<string>, bool)> Verify(List<Device> deviceList, string[] tabNames)
        {
            var allMessages = new List<string>();
            bool isValid = true;

            foreach (var tabName in tabNames)
            {
                (List<string> messages, bool valid) = await VerifySingleTab(deviceList, tabName);
                allMessages.AddRange(messages);
                isValid &= valid;
            }
            return (allMessages, isValid);
        }
        //*************************************************
        //SweepTabチェック(1Tab分)
        // 戻り値：(List<string>, bool) エラーメッセージリストとチェック結果
        // 引数1：deviceList - デバイスリスト
        // 引数2：tabNames - Tab
        //*************************************************
        private async Task<(List<string> Messages, bool IsValid)> VerifySingleTab(List<Device> deviceList, string tabName)
        {
            var messages = new List<string>();
            bool isValid = true;

            //*********************
            //対象測定器チェック
            //*********************
            bool enOsc = deviceList.Any(d => d.Identifier.Equals("OSC", StringComparison.OrdinalIgnoreCase));
            bool enDmm = deviceList.Any(d => d.Identifier.StartsWith("DMM", StringComparison.OrdinalIgnoreCase));
            bool enPg = deviceList.Any(d => d.Identifier.Equals("PULSE", StringComparison.OrdinalIgnoreCase));

            //*********************
            //各測定器設定抽出、初期化
            //*********************
            var (sourceDevices, _) = _utility.FilterSettings(deviceList, "SOURCE", tabName);
            var (oscDevices, _) = enOsc ? _utility.FilterSettings(deviceList, "OSC", tabName) : (null, false);
            var (dmmDevices, _) = enDmm ? _utility.FilterSettings(deviceList, "DMM", tabName) : (null, false);
            var (pulseDevices, _) = enPg ? _utility.FilterSettings(deviceList, "PULSE", tabName) : (null, false);

            //*********************
            //PG設定抽出
            //*********************
            bool enPulse = false;
            double pgLLev = 0, pgHLev = 0, pgPeriod = 0, pgWidth = 0;
            if (enPg && pulseDevices?.Any() == true)
            {
                var pgSettings = pulseDevices.First().TabSettings[tabName] as PGSettings ?? new PGSettings();
                enPulse = pgSettings.PGCheck;
                pgLLev = pgSettings.LowLevelValue;
                pgHLev = pgSettings.HighLevelValue;
                pgPeriod = pgSettings.PeriodValue;
                pgWidth = pgSettings.WidthValue;
            }

            //*********************
            //Sweep設定抽出
            //*********************
            var (sweepSettingsList, _) = _utility.FilterSettings(deviceList, "SWEEP", tabName);
            var sweepSettings = sweepSettingsList?.FirstOrDefault()?.TabSettings[tabName] as SweepSettings ?? new SweepSettings();
            bool normalSweep = sweepSettings.Normalsweep;
            double minValue = sweepSettings.MinValue;
            double maxValue = sweepSettings.MaxValue;
            double stepValue = sweepSettings.StepValue;
            float stepTime = sweepSettings.StepTime;
            string? minValueUnit = sweepSettings.MinValueUnit;
            string? maxValueUnit = sweepSettings.MaxValueUnit;
            string? stepValueUnit = sweepSettings.StepValueUnit;

            //*********************
            //Detect,Release条件抽出
            //*********************
            var (detRelList, _) = _utility.FilterSettings(deviceList, "DETREL", tabName);
            var detRelSettings = detRelList?.FirstOrDefault()?.TabSettings[tabName] as DetRelSettings ?? new DetRelSettings();
            float checkTime = detRelSettings.CheckTime;

            //*********************
            //1. MinValueとMaxValueの大小チェック
            //*********************
            var (msgMinMax, okMinMax) = _numericChecker.CheckMinMaxValues(minValue, maxValue, tabName);
            messages.AddRange(msgMinMax); isValid &= okMinMax;

            if (enPulse)
            {
                var (msgPulse, okPulse) = _numericChecker.CheckMinMaxValues(pgLLev, pgHLev, tabName);
                messages.AddRange(msgPulse); isValid &= okPulse;
            }

            //*********************
            //2. 0値チェック（stepTime,stepValue,checkTime,dmmPlc,pgPeriod,pgWidth）
            //*********************
            var (msgStepTime, okStepTime) = _numericChecker.CheckNonZeroValue(stepTime, "StepTime", tabName);
            messages.AddRange(msgStepTime); isValid &= okStepTime;

            if (normalSweep)
            {
                var (msgStep, okStep) = _numericChecker.CheckNonZeroValue(stepValue, "Step値", tabName);
                messages.AddRange(msgStep); isValid &= okStep;
            }
            else
            {
                var (msgCheck, okCheck) = _numericChecker.CheckNonZeroValue(checkTime, "CheckTime値", tabName);
                messages.AddRange(msgCheck); isValid &= okCheck;
            }

            if (enDmm && dmmDevices != null)
            {
                foreach (var d in dmmDevices)
                {
                    var dmmSet = d.TabSettings[tabName] as DmmSettings ?? new DmmSettings();
                    double plc = _utility.String2Double_Conv(dmmSet.Plc, "range1");
                    var (msgPlc, okPlc) = _numericChecker.CheckNonZeroValue(plc, $"{d.Identifier} PLC値", tabName);
                    messages.AddRange(msgPlc); isValid &= okPlc;
                }
            }

            if (enPulse)
            {
                var (msgPer, okPer) = _numericChecker.CheckNonZeroValue(pgPeriod, "PulseGen Period値", tabName);
                messages.AddRange(msgPer); isValid &= okPer;

                var (msgWid, okWid) = _numericChecker.CheckNonZeroValue(pgWidth, "PulseGen Width値", tabName);
                messages.AddRange(msgWid); isValid &= okWid;
            }

            //*********************
            //3. 設定レンジ範囲内チェック
            //*********************
            var sweepDevices = sourceDevices?.Where(d =>                                                //sourceDevicesの中で
                d.TabSettings.ContainsKey(tabName) &&                                                   //測定項目名(tabname)が一致して
                d.TabSettings[tabName] is SourceSettings s && s.Function == "sweep").ToList() ?? new(); //TabSettings[tabname]がSourceSettings型で定義されていてかつFunctionが"sweep"

            foreach (var d in sweepDevices)
            {
                var s = d.TabSettings[tabName] as SourceSettings;
                if (s?.SourceRange != "AUTO")
                {
                    var (msgMin, okMin) = _rangeChecker.CheckValueRange(minValue, s.SourceRange, $"{s.SourceAct} Min値", tabName);
                    messages.AddRange(msgMin); isValid &= okMin;

                    var (msgMax, okMax) = _rangeChecker.CheckValueRange(maxValue, s.SourceRange, $"{s.SourceAct} Max値", tabName);
                    messages.AddRange(msgMax); isValid &= okMax;
                }
            }

            var constDevices = sourceDevices?.Where(d =>
                d.TabSettings.ContainsKey(tabName) &&
                d.TabSettings[tabName] is SourceSettings s && s.Function == "const").ToList() ?? new();

            foreach (var d in constDevices)
            {
                var s = d.TabSettings[tabName] as SourceSettings;
                var (msgVal, okVal) = _rangeChecker.CheckValueRange(s.SourceValue, s.SourceRange, $"{s.SourceAct} Const値", tabName);
                messages.AddRange(msgVal); isValid &= okVal;
            }

            //*********************
            //4. SweepかPulseが選択されているかチェック
            //*********************
            var (msgSp, okSp) = CheckSweepPulseSelected(sourceDevices, tabName, enPulse);
            messages.AddRange(msgSp); isValid &= okSp;

            //*********************
            //5. ステップ数チェック
            //*********************
            if (normalSweep)
            {
                var (msgStepCount, okStepCount) = _numericChecker.CheckStepCount(minValue, maxValue, stepValue, tabName);
                messages.AddRange(msgStepCount); isValid &= okStepCount;
            }

            //*********************
            //6. モード一致チェック1
            //*********************
            foreach (var d in sweepDevices)
            {
                var s = d.TabSettings[tabName] as SourceSettings;
                var units = new List<string>
                {
                    InpCheck.GetRangeUnit(s.RangeValue),
                    InpCheck.GetRangeUnit(minValueUnit),
                    InpCheck.GetRangeUnit(maxValueUnit),
                    InpCheck.GetRangeUnit(stepValueUnit)
                };
                var (msgMode, okMode) = _rangeChecker.CheckModeConsistency(s.Mode, units, "SWEEP電源設定", tabName);
                messages.AddRange(msgMode); isValid &= okMode;
            }

            foreach (var d in constDevices)
            {
                var s = d.TabSettings[tabName] as SourceSettings;
                if (s.SourceRange != "AUTO")
                {
                    var units = new List<string> { InpCheck.GetRangeUnit(s.RangeValue), InpCheck.GetRangeUnit(s.SourceRangeUnit) };
                    var (msgMode, okMode) = _rangeChecker.CheckModeConsistency(s.Mode, units, "CONST電源設定", tabName);
                    messages.AddRange(msgMode); isValid &= okMode;
                }
            }

            if (enDmm && dmmDevices != null)
            {
                foreach (var d in dmmDevices)
                {
                    var s = d.TabSettings[tabName] as DmmSettings;
                    var units = new List<string> { InpCheck.GetRangeUnit(s.RangeValue) };
                    if (units[0] != "AUTO")
                    {
                        var (msgMode, okMode) = _rangeChecker.CheckModeConsistency(s.Mode, units, $"{d.Identifier} Range設定", tabName);
                        messages.AddRange(msgMode); isValid &= okMode;
                    }
                }
            }

            //*********************
            //7. モード一致チェック2
            //*********************
            foreach (var d in sourceDevices ?? new())
            {
                var s = d.TabSettings[tabName] as SourceSettings ?? new();
                var units = new List<string> { InpCheck.GetRangeUnit(s.SourceLimitUnit) };
                var (msgLimit, okLimit) = _rangeChecker.CheckLimitUnit(s.Mode, units, $"{d.Identifier} Limit設定単位", tabName);
                messages.AddRange(msgLimit); isValid &= okLimit;
            }

            return (messages, isValid);
        }

        //*************************************************
        //Sweep/Pulse 選択チェック
        // 戻り値：(List<string>, bool) エラーメッセージリストとチェック結果
        // 引数1：sourceDevices - デバイスリスト
        // 引数2：tabName - エラーメッセージに表示するTab名
        // 引数3：enPulse - PluseGenerator使用チェック
        //*************************************************
        private (List<string> Messages, bool IsValid) CheckSweepPulseSelected(List<Device>? sourceDevices, string tabName, bool enPulse)
        {
            var messages = new List<string>();
            if (sourceDevices == null) return (messages, true);

            var sweepDevices = sourceDevices.Where(d =>
                d.TabSettings.ContainsKey(tabName) &&
                d.TabSettings[tabName] is SourceSettings s && s.Function == "sweep").ToList();

            if (enPulse && sweepDevices.Any())
            {
                messages.Add($"# {tabName}で'PulseGen'を使用しています。'Sweep'は選択できません。");
                return (messages, false);
            }

            if (!enPulse && !sweepDevices.Any())
            {
                messages.Add($"# {tabName}で'Sweep'が選択されていません。1つは'Sweep'を選択してください。");
                return (messages, false);
            }

            return (messages, true);
        }
    }
}
