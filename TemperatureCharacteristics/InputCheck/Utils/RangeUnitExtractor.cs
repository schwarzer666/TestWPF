using System.Text.RegularExpressions;

namespace InputCheck.Utils
{
    // レンジ文字列から単位（V, mA, uV など）を抽出
    internal static class RangeUnitExtractor
    {
        //*************************************************
        //V.mA.mV等の単位を抜き出す
        // 戻り値：<string> 
        // 引数1：value - チェック対象の値（文字列）
        // 引数2：range - 単位（例: 10E+0,30E+0など）
        //*************************************************
        public static string GetRangeUnit(string range)
        {
            if (range == "AUTO") return "AUTO";
            var replaced = Regex.Replace(range, @"^\d+", "");
            if (replaced.StartsWith("m")) replaced = replaced.TrimStart('m');
            if (replaced.StartsWith("u")) replaced = replaced.TrimStart('u');
            return replaced;
        }
    }
}
