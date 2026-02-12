using UTility;

namespace InputCheck.Checkers
{
    //レンジ・単位・モード一致チェック
    internal class RangeChecker
    {
        private readonly UT _utility;
        public RangeChecker(UT utility) => _utility = utility;

        //*************************************************
        //値がレンジ内かチェック
        // 戻り値：(List<string>, bool) エラーメッセージリストとチェック結果
        // 引数1：value - チェック対象の値（文字列）
        // 引数2：range - 単位（例: 10E+0,30E+0など）
        // 引数3：fieldName - エラーメッセージに表示するフィールド名
        // 引数4：tabName - エラーメッセージに表示するTab名
        //*************************************************
        public (List<string>, bool) CheckValueRange(double value, string range, string fieldName, string tabName)
        {
            List<string> errMessage = new List<string>();
            try
            {
                if (range != "AUTO")
                {
                    //設定レンジを数字に変換
                    double convertedValue = _utility.String2Double_Conv(range, "range1");
                    if (convertedValue < value)
                    {
                        errMessage.Add($"# {tabName}の{fieldName}の値({value:F2})が設定範囲外です。適切な値を入力してください。");
                        return (errMessage, false);
                    }
                }
                return (errMessage, true);      //問題ない場合、空のリストとtrueを返す
            }
            catch (FormatException)
            {
                errMessage.Add($"# {tabName}の{fieldName}の値({value:F2})が正しい数値形式ではありません。");
                return (errMessage, false);
            }
            catch (Exception ex)
            {
                errMessage.Add($"# {tabName}の{fieldName}の値({value:F2})チェック中にエラーが発生しました: {ex.Message}");
                return (errMessage, false);
            }
        }
        //*************************************************
        //特定のComboItems_Modeの"V"が他のComboItems_Modeと一致しているかチェック
        // 戻り値：(List<string>, bool) エラーメッセージリストとチェック結果
        // 引数1：referenceModeIndex - 基準となるコンボボックスのインデックス（"V"は0）
        // 引数2：targetModeIndexes - 比較対象のコンボボックスのインデックスリスト
        // 引数3：fieldName - エラーメッセージに表示するフィールド名
        // 引数4：tabName - エラーメッセージに表示するTab名
        //*************************************************
        public (List<string>, bool) CheckModeConsistency(string referenceMode, List<string> targetModeStrings, string fieldName, string tabName)
        {
            List<string> errMessage = new List<string>();
            //入力チェック(空)
            if (string.IsNullOrEmpty(referenceMode))
            {
                errMessage.Add($"# {tabName}の{fieldName}: 基準モードが無効です。");
                return (errMessage, false);
            }
            //入力チェック(空)
            if (targetModeStrings == null || !targetModeStrings.Any())
            {
                errMessage.Add($"# {tabName}の{fieldName}: 単位リストが空または null です。");
                return (errMessage, false);
            }
            //"AUTO"を除外
            List<string> validUnits = targetModeStrings
                .Where(unit => !string.Equals(unit, "AUTO", StringComparison.OrdinalIgnoreCase))
                .ToList();
            //有効な単位がない場合
            if (!validUnits.Any())
            {
                errMessage.Add($"# {tabName}の{fieldName}: 有効な単位がありません（すべて AUTO）。");
                return (errMessage, false);
            }
            //期待される単位を決定
            string? expectedUnit = referenceMode switch
            {
                "VOLT" => "V",
                "CURR" => "A",
                _ => null
            };
            if (expectedUnit == null)
            {
                errMessage.Add($"# {tabName}の{fieldName}: 無効な基準モード '{referenceMode}' です。");
                return (errMessage, false);
            }
            //referenceModeとvalidUnits(targetModeStrings)内の単位が一致するかチェック
            bool allMatch = validUnits.All(unit => unit == expectedUnit);
            if (!allMatch)
            {
                errMessage.Add($"# {tabName}の{fieldName}: モードが一致していません。動作モードが{referenceMode}の場合、すべての単位は {expectedUnit}である必要があります。検出された単位: {string.Join(", ", validUnits)}");
                return (errMessage, false);
            }
            return (errMessage, true);      //問題ない場合、空のリストとtrueを返す
        }
        //*************************************************
        //Limit単位一致チェック
        // 戻り値：(List<string>, bool) エラーメッセージリストとチェック結果
        // 引数1：referenceModeIndex - 基準となるコンボボックスのインデックス（"V"は0）
        // 引数2：targetModeIndexes - 比較対象のコンボボックスのインデックスリスト
        // 引数3：fieldName - エラーメッセージに表示するフィールド名
        // 引数4：tabName - エラーメッセージに表示するTab名
        //*************************************************
        public (List<string>, bool) CheckLimitUnit(string referenceMode, List<string> targetLimitStrings, string fieldName, string tabName)
        {
            List<string> errMessage = new List<string>();
            //入力チェック(空)
            if (string.IsNullOrEmpty(referenceMode))
            {
                errMessage.Add($"# {tabName}の{fieldName}: 基準モードが無効です。");
                return (errMessage, false);
            }
            //入力チェック(空)
            if (targetLimitStrings == null || !targetLimitStrings.Any())
            {
                errMessage.Add($"# {tabName}の{fieldName}: 単位リストが空または null です。");
                return (errMessage, false);
            }
            //"AUTO" を除外
            List<string> validLimits = targetLimitStrings
                .Where(unit => !string.Equals(unit, "AUTO", StringComparison.OrdinalIgnoreCase))
                .ToList();
            // 有効な単位がない場合
            if (!validLimits.Any())
            {
                errMessage.Add($"# {tabName}の{fieldName}: 有効な単位がありません（すべて AUTO）。");
                return (errMessage, false);
            }
            //期待される単位を決定
            string? expectedUnit = referenceMode switch
            {
                "VOLT" => "A",
                "CURR" => "V",
                _ => null
            };
            if (expectedUnit == null)
            {
                errMessage.Add($"# {tabName}の{fieldName}: 無効な基準モード '{referenceMode}' です。");
                return (errMessage, false);
            }
            //referenceModeとvalidUnits(targetModeStrings)内の単位が一致するかチェック
            bool allMatch = validLimits.All(unit => unit == expectedUnit);
            if (!allMatch)
            {
                errMessage.Add($"# {tabName}の{fieldName}: モードが一致していません。動作モードが{referenceMode}の場合、Limitの単位は {expectedUnit}である必要があります。検出された単位: {string.Join(", ", validLimits)}");
                return (errMessage, false);
            }
            return (errMessage, true);      //問題ない場合、空のリストとtrueを返す
        }
        //*************************************************
        //レンジ設定がAutoになっていないかチェック
        // 戻り値：(List<string>, bool) エラーメッセージリストとチェック結果
        // 引数1：deviceList - 測定器設定リスト
        // 引数2：sourceName - エラーメッセージに表示する電源名
        // 引数3：tabNames - エラーメッセージに表示するタブ名
        //コメント
        // rangeValueにAUTOが設定されている場合false
        //*************************************************
        //public (List<string>, bool) DetectRangeAuto(string rangeValue, string sourceName, string tabName)
        //{
        //    {
        //        List<string> errMessage = new List<string>();
        //        //入力が空またはnullの場合
        //        if (string.IsNullOrWhiteSpace(rangeValue))
        //        {
        //            errMessage.Add($"# {tabName}の{sourceName}のRange設定がされていません。");
        //            return (errMessage, false);
        //        }
        //        if (rangeValue == "AUTO")
        //        {
        //            errMessage.Add($"# {tabName}の{sourceName}のRange設定がAUTOです。");
        //            return (errMessage, false);
        //        }
        //        return (errMessage, true);
        //    }
        //}
        //*************************************************
        //Sweep方向チェック
        // 戻り値：(List<string>, bool) エラーメッセージリストとチェック結果
        // 引数1：normalsweep - 通常sweepチェック
        // 引数2：directional - sweep方向
        // 引数3：tabNames - エラーメッセージに表示するタブ名
        //コメント
        // normalSweepにチェックが入っていない状態でsweep方向をrisefall/fallriseにしている場合false
        //*************************************************
        public (List<string>, bool) SweepDirectionalCheck(bool normalsweep, string directional, string tabName)
        {
            List<string> errMessage = new List<string>();
            //入力が空またはnullの場合
            if (string.IsNullOrWhiteSpace(directional))
            {
                errMessage.Add($"# {tabName}のSweep方向が設定されていません。{directional}");
                return (errMessage, false);
            }
            if (!normalsweep)
            {
                if (directional == "risefall" || directional == "fallrise")
                {
                    errMessage.Add($"# {tabName}のSweep方向が{directional}に設定されています。");
                    return (errMessage, false);
                }
            }
            return (errMessage, true);
        }
        //*************************************************
        //電源レンジがAutoになっていないか
        // 戻り値：(List<string>, bool) エラーメッセージリストとチェック結果
        // 引数1：deviceList - 測定器設定リスト
        // 引数2：sourceName - エラーメッセージに表示する電源名
        // 引数3：tabNames - エラーメッセージに表示するタブ名
        //*************************************************
        public async Task<(List<string>, bool)> CheckSourceRangeNotAuto(List<Device> deviceList, string[] tabNames, string tabType)
        {
            var errMessage = new List<string>();
            bool isValid = true;

            foreach (var d in deviceList.Where(d => d.Identifier.StartsWith("SOURCE")))
            {
                foreach (var kvp in d.TabSettings)
                {
                    string actualTabName = kvp.Key;

                    //tabNames に含まれる子タブ名だけをチェック
                    if (!tabNames.Contains(actualTabName))
                        continue;

                    if (kvp.Value is SourceSettings s)
                    {
                        //NotUsed のときは AUTO でも許容
                        bool isNotUsed = string.Equals(s.SourceAct, "NotUsed", StringComparison.OrdinalIgnoreCase);
                        bool isAutoRange = string.Equals(s.RangeValue, "AUTO", StringComparison.OrdinalIgnoreCase);

                        if (!isNotUsed && isAutoRange)
                        {
                            errMessage.Add($"# {tabType}：[{actualTabName}] の {d.Identifier} は Range=AUTO です。");
                            isValid = false;
                        }
                        else if (string.IsNullOrWhiteSpace(s.RangeValue))
                        {
                            errMessage.Add($"# {tabType}：[{actualTabName}] の {d.Identifier} のRange設定がされていません。");
                            isValid = false;
                        }
                    }
                }
            }
            return (errMessage, isValid);
        }
    }
}
