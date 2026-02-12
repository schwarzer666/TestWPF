using InputCheck.Utils;
using UTility;

namespace InputCheck
{
    public class InpCheck
    {
        private static InpCheck? _instance;
        private readonly UT _utility;
        private readonly Checkers.CommonChecker _commonChecker;
        private readonly Checkers.NumericChecker _numericChecker;
        private readonly Checkers.RangeChecker _rangeChecker;
        private readonly Checkers.SweepChecker _sweep;
        private readonly Checkers.DelayChecker _delay;
        private readonly Checkers.VIChecker _vi;

        private InpCheck()
        {
            _utility = UT.Instance;
            _commonChecker = new Checkers.CommonChecker();
            _numericChecker = new Checkers.NumericChecker(_utility);
            _rangeChecker = new Checkers.RangeChecker(_utility);
            _sweep = new Checkers.SweepChecker(_utility);
            _delay = new Checkers.DelayChecker(_utility);
            _vi = new Checkers.VIChecker(_utility);
        }

        public static InpCheck Instance => _instance ??= new InpCheck();
        internal static string GetRangeUnit(string? range) => RangeUnitExtractor.GetRangeUnit(range);

        //*************************************************
        //測定器のアドレス入力チェック
        // 引数1：measInstData - 測定器のアドレスリスト
        //*************************************************
        public async Task<(List<string>, bool)> VerifyInsAddr(List<(bool, string, string, string)> measInstData)
        {
            bool isVerify = true;
            List<string> errMessage = new List<string>();
            IEnumerable<string> requiredStrings = new[] { "INSTR" };    //チェック対象文字列(この文字列が含まれているか)
            if (measInstData == null)
            {
                errMessage.Add("#チェックされた測定器無し");
                return (errMessage, false);
            }
            //*********************
            //0. USB ID の重複チェック
            //*********************
            var (msgDup, okDup) = _commonChecker.CheckDuplicateUsbId(measInstData, "USBアドレス");
            errMessage.AddRange(msgDup); isVerify &= okDup;

            foreach ((bool IsChecked, string UsbId, string InstName, string Identifier) device in measInstData)
            {
                if (device.IsChecked)       //測定器にチェックが入っているものに対して
                {
                    //*********************
                    //1. 各測定器アドレスに空白文字が含まれていないかチェック
                    //*********************
                    var (msgWs, okWs) = _commonChecker.CheckNoWhitespace(device.UsbId, $"{device.Identifier}のアドレス");
                    errMessage.AddRange(msgWs); isVerify &= okWs;

                    //*********************
                    //2. 各測定器アドレスが有効かどうか
                    //*********************
                    if (device.Identifier != "RELAY") //リレー用アドレス以外をチェック
                    {
                        var (msgFind, okFind) = _commonChecker.CheckFindString(device.UsbId, $"{device.Identifier}のアドレス", requiredStrings, true, false);
                        errMessage.AddRange(msgFind); isVerify &= okFind;
                    }
                }
            }
            return (errMessage, isVerify);
        }

        //*************************************************
        //入力数値のチェック
        //*************************************************
        public async Task<(List<string>, bool)> CheckNumInput(List<string> inputTexts, string tabName)
        {
            bool isVerify = true;
            List<string> errMessage = new List<string>();
            if (inputTexts.Any())
            {
                foreach (string inputText in inputTexts)
                {
                    (List<string> errMessages, isVerify) = _commonChecker.CheckNumericInput(inputText, $"{tabName}内TextBox", tabName);
                    if (!isVerify)
                        errMessage.AddRange(errMessages);
                }
            }
            return (errMessage, isVerify);
        }

        //*************************************************
        //SweepTabのチェック
        //*************************************************
        public async Task<(List<string>, bool)> VerifySweepTabInputs(List<Device> deviceList, string[] tabNames)
            => await _sweep.Verify(deviceList, tabNames);
        //*************************************************
        //DelayTabのチェック
        //*************************************************
        public async Task<(List<string>, bool)> VerifyDelayTabInputs(List<Device> deviceList, string[] tabNames)
            => await _delay.Verify(deviceList, tabNames);
        //*************************************************
        //VITabのチェック
        //*************************************************
        public async Task<(List<string>, bool)> VerifyVITabInputs(List<Device> deviceList, string[] tabNames)
            => await _vi.Verify(deviceList, tabNames);
        //*************************************************
        //電源RangeAutoチェック
        //*************************************************
        public async Task<(List<string>, bool)> SourceRangeAuto(List<Device> deviceList, string[] tabNames, string tabType)
            =>  await _rangeChecker.CheckSourceRangeNotAuto(deviceList, tabNames, tabType);    
    }
}
