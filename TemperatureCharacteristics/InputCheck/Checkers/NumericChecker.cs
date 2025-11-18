using UTility;

namespace InputCheck.Checkers
{
    //数値関連の入力チェック（大小、ゼロ、ステップ数など）
    internal class NumericChecker
    {
        private readonly UT _utility;
        public NumericChecker(UT utility) => _utility = utility;

        //*************************************************
        //最小値 < 最大値 チェック
        // 戻り値：(List<string>, bool) エラーメッセージリストとチェック結果
        // 引数1：minValue - 最小値の文字列
        // 引数2：maxValue - 最大値の文字列
        // 引数3：tabName - エラーメッセージに表示するTab名
        //*************************************************
        public (List<string>, bool) CheckMinMaxValues(double minValue, double maxValue, string tabName)
        {
            List<string> errMessage = new List<string>();
            try
            {
                if (minValue >= maxValue)
                {
                    errMessage.Add($"# {tabName}の最小値({minValue:F2})が最大値({maxValue:F2})以上です。修正してください。");     //F2→小数点2桁
                    return (errMessage, false);
                }
                return (errMessage, true);      //問題ない場合、空のリストとtrueを返す
            }
            catch (FormatException)
            {
                errMessage.Add($"# {tabName}の最小値/最大値が正しい数値形式ではありません。");
                return (errMessage, false);
            }
            catch (Exception ex)
            {
                errMessage.Add($"# {tabName}の最小値/最大値チェック中にエラーが発生しました: {ex.Message}");
                return (errMessage, false);
            }
        }

        //*************************************************
        //ゼロ値チェック
        // 戻り値：(List<string>, bool) エラーメッセージリストとチェック結果
        // 引数1：value - チェック対象の値
        // 引数2：fieldName - エラーメッセージに表示するフィールド名
        // 引数3：tabName - エラーメッセージに表示するTab名
        //*************************************************
        public (List<string>, bool) CheckNonZeroValue(double value, string fieldName, string tabName)
        {
            List<string> errMessage = new List<string>();
            try
            {
                if (value == 0.0)
                {
                    errMessage.Add($"# {tabName}の{fieldName}の値が0です。0以外の値を入力してください。");
                    return (errMessage, false);
                }
                return (errMessage, true);      //問題ない場合、空のリストとtrueを返す
            }
            catch (FormatException)
            {
                errMessage.Add($"# {tabName}の{fieldName}の値が正しい数値形式ではありません。");
                return (errMessage, false);
            }
            catch (Exception ex)
            {
                errMessage.Add($"# {tabName}の{fieldName}の値チェック中にエラーが発生しました: {ex.Message}");
                return (errMessage, false);
            }
        }

        //*************************************************
        //ステップ数チェック（(Max - Min) / Step >= 1）
        // 戻り値：(List<string>, bool) エラーメッセージリストとチェック結果
        // 引数1：minValue - 最小値の文字列
        // 引数2：maxValue - 最大値の文字列
        // 引数3：stepValue - ステップ値の文字列
        // 引数4：tabName - エラーメッセージに表示するTab名
        //*************************************************
        public (List<string>, bool) CheckStepCount(double minValue, double maxValue, double stepValue, string tabName)
        {
            List<string> errMessage = new List<string>();
            try
            {
                long minValue_n = (long)(minValue * 1_000_000_000);     //100nVまで扱うためminValueを整数型に変換
                long maxValue_n = (long)(maxValue * 1_000_000_000);     //100nVまで扱うためmaxValueを整数型に変換
                long stepValue_n = (long)(stepValue * 1_000_000_000);   //100nVまで扱うためstepValueを整数型に変換
                int stepCount = (int)((maxValue_n - minValue_n) / stepValue_n);

                if (stepCount <= 0)
                {
                    errMessage.Add($"# {tabName}のステップ値が0です。0以外の値を入力してください。");
                    return (errMessage, false);
                }

                if (stepCount < 1)
                {
                    errMessage.Add($"# {tabName}のステップ数が1未満です。適切な値を入力してください（(Max: {maxValue:F2} - Min: {minValue:F2}) / Step: {stepValue:F2}）。");
                    return (errMessage, false);
                }
                return (errMessage, true);      //問題ない場合、空のリストとtrueを返す
            }
            catch (FormatException)
            {
                errMessage.Add($"# {tabName}のステップ値が正しい数値形式ではありません。");
                return (errMessage, false);
            }
            catch (Exception ex)
            {
                errMessage.Add($"# {tabName}のステップ計算チェック中にエラーが発生しました: {ex.Message}");
                return (errMessage, false);
            }
        }
    }
}
