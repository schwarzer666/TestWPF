using UTility;

namespace InputCheck.Checkers
{
    //DelayTab用チェック
    internal class DelayChecker
    {
        private readonly UT _utility;
        private readonly NumericChecker _numericChecker;
        private readonly RangeChecker _rangeChecker;
        //private readonly CommonChecker _commonChecker;

        public DelayChecker(UT utility)
        {
            _utility = utility;
            _numericChecker = new NumericChecker(utility);
            _rangeChecker = new RangeChecker(utility);
            //_commonChecker = new CommonChecker();
        }
        //*************************************************
        //DelayTabチェック(リスト)
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
        //DelayTabチェック(1Tab分)
        // 戻り値：(List<string>, bool) エラーメッセージリストとチェック結果
        // 引数1：deviceList - デバイスリスト
        // 引数2：tabNames - Tab
        //*************************************************
        private async Task<(List<string> Messages, bool IsValid)> VerifySingleTab(List<Device> deviceList, string tabName)
        {
            var messages = new List<string>();
            bool isValid = true;

            //*********************
            // 各測定器設定抽出
            //*********************
            List<Device>? sourceDevices = null;
            List<Device>? oscDevices = null;
            List<Device>? pulseDevices = null;
            (sourceDevices, bool _) = _utility.FilterSettings(deviceList, "SOURCE", tabName);
            (oscDevices, bool __) = _utility.FilterSettings(deviceList, "OSC", tabName);
            (pulseDevices, bool ___) = _utility.FilterSettings(deviceList, "PULSE", tabName);

            //*********************
            // PG設定抽出
            //*********************
            Device? pulseDevice = pulseDevices?.FirstOrDefault();
            PGSettings? pgSettings = pulseDevice?.TabSettings[tabName] as PGSettings ?? new PGSettings();
            double pgLLev = pgSettings.LowLevelValue;
            double pgHLev = pgSettings.HighLevelValue;
            double pgPeriod = pgSettings.PeriodValue;
            double pgWidth = pgSettings.WidthValue;

            //*********************
            // Detect,Release条件抽出
            //*********************
            List<Device>? detectreleaseSettings = null;
            (detectreleaseSettings, bool ____) = _utility.FilterSettings(deviceList, "DETREL", tabName);
            Device? detreldevice = detectreleaseSettings?.FirstOrDefault();
            DetRelSettings? detrelSettings = detreldevice?.TabSettings[tabName] as DetRelSettings ?? new DetRelSettings();
            float checkTime = detrelSettings.CheckTime;

            //*********************
            // 1. MinValueとMaxValueの大小チェック
            //*********************
            var (msgMinMaxLLev, okMinMaxLLev) = _numericChecker.CheckMinMaxValues(pgLLev, pgHLev, tabName);
            messages.AddRange(msgMinMaxLLev); isValid &= okMinMaxLLev;

            var (msgMinMaxWidth, okMinMaxWidth) = _numericChecker.CheckMinMaxValues(pgWidth, pgPeriod, tabName);
            messages.AddRange(msgMinMaxWidth); isValid &= okMinMaxWidth;

            //*********************
            // 2. 0値チェック（checkTime,pgPeriod,pgWidth）
            //*********************
            var (msgCheckTime, okCheckTime) = _numericChecker.CheckNonZeroValue(checkTime, "CheckTime値", tabName);
            messages.AddRange(msgCheckTime); isValid &= okCheckTime;

            var (msgPeriod, okPeriod) = _numericChecker.CheckNonZeroValue(pgPeriod, "PulseGen Period値", tabName);
            messages.AddRange(msgPeriod); isValid &= okPeriod;

            var (msgWidth, okWidth) = _numericChecker.CheckNonZeroValue(pgWidth, "PulseGen Width値", tabName);
            messages.AddRange(msgWidth); isValid &= okWidth;

            //*********************
            // 3. 設定レンジ範囲内チェック（Const電源）
            //*********************
            var constDevices = sourceDevices?.Where(d =>
                d.TabSettings.ContainsKey(tabName) &&
                d.TabSettings[tabName] is SourceSettings s && s.Function == "const")
                .ToList() ?? new();

            foreach (var constDevice in constDevices)
            {
                var s = constDevice.TabSettings[tabName] as SourceSettings;
                if (s?.SourceRange != "AUTO")
                {
                    var (msg, ok) = _rangeChecker.CheckValueRange(s.SourceValue, s.SourceRange, $"{s.SourceAct} Const値", tabName);
                    messages.AddRange(msg); isValid &= ok;
                }
            }

            //*********************
            // 4. Sweepが選択されていないかチェック
            //*********************
            var (msgSweep, okSweep) = CheckSweepUnselected(sourceDevices, tabName);
            messages.AddRange(msgSweep); isValid &= okSweep;

            //*********************
            // 6. モード一致チェック1（Const電源）
            //*********************
            foreach (var constDevice in constDevices)
            {
                var s = constDevice.TabSettings[tabName] as SourceSettings;
                if (s?.SourceRange != "AUTO")
                {
                    var units = new List<string>
                    {
                        InpCheck.GetRangeUnit(s.RangeValue),
                        InpCheck.GetRangeUnit(s.SourceRangeUnit)
                    };
                    var (msg, ok) = _rangeChecker.CheckModeConsistency(s.Mode, units, "CONST電源設定", tabName);
                    messages.AddRange(msg); isValid &= ok;
                }
            }

            //*********************
            // 7. モード一致チェック2（Limit単位）
            //*********************
            foreach (var sourceDevice in sourceDevices ?? new())
            {
                var s = sourceDevice.TabSettings[tabName] as SourceSettings ?? new();
                var units = new List<string> { InpCheck.GetRangeUnit(s.SourceLimitUnit) };
                var (msg, ok) = _rangeChecker.CheckLimitUnit(s.Mode, units, $"{sourceDevice.Identifier} Limit設定単位", tabName);
                messages.AddRange(msg); isValid &= ok;
            }

            return (messages, isValid);
        }

        //*************************************************
        //Sweepが選択されていないかチェック
        // 戻り値：(List<string>, bool) エラーメッセージリストとチェック結果
        // 引数1：sourceDevices - デバイスリスト
        // 引数2：tabName - エラーメッセージに表示するTab名
        //*************************************************
        private (List<string> Messages, bool IsValid) CheckSweepUnselected(List<Device>? sourceDevices, string tabName)
        {
            var messages = new List<string>();
            if (sourceDevices == null) return (messages, true);

            var sweepDevices = sourceDevices.Where(d =>
                d.TabSettings.ContainsKey(tabName) &&
                d.TabSettings[tabName] is SourceSettings s && s.Function == "sweep").ToList();

            if (sweepDevices.Any())
            {
                messages.Add($"# {tabName}で'Sweep'が選択されています。'Sweep'は使用できません。");
                return (messages, false);
            }

            return (messages, true);
        }
    }
}
