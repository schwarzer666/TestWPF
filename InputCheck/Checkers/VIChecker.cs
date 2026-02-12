using UTility;

namespace InputCheck.Checkers
{
    //VITab用チェック
    internal class VIChecker
    {
        private readonly UT _utility;
        private readonly NumericChecker _numericChecker;
        private readonly RangeChecker _rangeChecker;
        //private readonly CommonChecker _commonChecker;

        public VIChecker(UT utility)
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
            //各測定器設定抽出、初期化
            //*********************
            var (sourceDevices, _) = _utility.FilterSettings(deviceList, "SOURCE", tabName);
            var (dmmDevices, _) = _utility.FilterSettings(deviceList, "DMM", tabName);

            //*********************
            //Detect,Release条件抽出
            //*********************
            var (detRelList, _) = _utility.FilterSettings(deviceList, "DETREL", tabName);
            var detRelSettings = detRelList?.FirstOrDefault()?.TabSettings[tabName] as DetRelSettings ?? new DetRelSettings();
            float checkTime = detRelSettings.CheckTime;

            //*********************
            //1. 0値チェック（checkTime,dmmPlc）
            //*********************
            var (msgCheck, okCheck) = _numericChecker.CheckNonZeroValue(checkTime, "CheckTime値", tabName);
            messages.AddRange(msgCheck); isValid &= okCheck;

            foreach (var d in dmmDevices)
            {
                var dmmSet = d.TabSettings[tabName] as DmmSettings ?? new DmmSettings();
                double plc = _utility.String2Double_Conv(dmmSet.Plc, "range1");
                var (msgPlc, okPlc) = _numericChecker.CheckNonZeroValue(plc, $"{d.Identifier} PLC値", tabName);
                messages.AddRange(msgPlc); isValid &= okPlc;
            }

            //*********************
            //2. 設定レンジ範囲内チェック
            //*********************
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
            //3. SweepもPulseも選択されていないかチェック
            //*********************
            var (msgSp, okSp) = CheckSweepPulseUnselected(sourceDevices, tabName);
            messages.AddRange(msgSp); isValid &= okSp;

            //*********************
            //4. モード一致チェック1
            //*********************
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

            //*********************
            //5. モード一致チェック2
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
        //SweepもPulseも選択されていないかチェック
        // 戻り値：(List<string>, bool) エラーメッセージリストとチェック結果
        // 引数1：sourceDevices - デバイスリスト
        // 引数2：tabName - エラーメッセージに表示するTab名
        //*************************************************
        private (List<string> Messages, bool IsValid) CheckSweepPulseUnselected(List<Device>? sourceDevices, string tabName)
        {
            var messages = new List<string>();
            if (sourceDevices == null) return (messages, true);

            var sweepDevices = sourceDevices.Where(d =>
                d.TabSettings.ContainsKey(tabName) &&
                d.TabSettings[tabName] is SourceSettings s && s.Function == "sweep").ToList();
            var pulseDevices = sourceDevices.Where(d =>
                d.TabSettings.ContainsKey(tabName) &&
                d.TabSettings[tabName] is SourceSettings s && s.Function == "pluse").ToList();

            if (sweepDevices.Any())
            {
                messages.Add($"# {tabName}で'Sweep'は選択できません。");
                return (messages, false);
            }

            if (pulseDevices.Any())
            {
                messages.Add($"# {tabName}で'Pulse'は選択できません。");
                return (messages, false);
            }
            return (messages, true);
        }
    }
}
