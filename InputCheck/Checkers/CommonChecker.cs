
namespace InputCheck.Checkers
{
    //共通の入力チェック（空白、文字列検索、ID重複、数値形式など）
    internal class CommonChecker
    {
        public CommonChecker()
        {
        }

        //*************************************************
        //空白文字チェック
        // 戻り値：(List<string>, bool) エラーメッセージリストとチェック結果
        // 引数1：input - チェック対象の文字列
        // 引数2：fieldName - エラーメッセージのフィールド名（例: "MinValueUnit"）
        //*************************************************
        public (List<string>, bool) CheckNoWhitespace(string input, string fieldName)
        {
            List<string> errMessages = new List<string>();

            if (string.IsNullOrEmpty(input))
            {
                errMessages.Add($"# {fieldName}: 値が空または null です。");
                return (errMessages, false);
            }
            if (input.Any(char.IsWhiteSpace))
            {
                errMessages.Add($"# {fieldName}: 空白文字が含まれています。もしくは不正な文字列です。");
                return (errMessages, false);
            }
            return (errMessages, true);        //問題ない場合、空のリストとtrueを返す
        }
        //*************************************************
        //必須文字列検索チェック
        // 戻り値：(List<string>, bool) エラーメッセージリストとチェック結果
        // 引数1：input - チェック対象の文字列
        // 引数2：fieldName - エラーメッセージのフィールド名（例: "測定器アドレス"）
        // 引数3：requiredStrings - 必須文字列のリスト
        // 引数4：caseSensitive - 大文字小文字を区別するか（デフォルト: false）
        // 引数5：anyMatch - いずれか1つが含まれていれば成功（true）、すべて必要（false）
        //*************************************************
        public (List<string>, bool) CheckFindString(
                                                    string input, 
                                                    string fieldName,
                                                    IEnumerable<string> requiredStrings, 
                                                    bool caseSensitive = false, 
                                                    bool anyMatch = true)
        {
            {
                List<string> errMessage = new List<string>();

                if (string.IsNullOrEmpty(input))
                {
                    errMessage.Add($"# {fieldName}: 値が空または null です。");
                    return (errMessage, false);
                }
                if (requiredStrings == null || !requiredStrings.Any())
                {
                    errMessage.Add($"# {fieldName}: チェック対象文字列が空または null です。");
                    return (errMessage, false);
                }

                StringComparison comparison = caseSensitive
                    ? StringComparison.Ordinal
                    : StringComparison.OrdinalIgnoreCase;

                List<string> missingStrings = new List<string>();

                foreach (string required in requiredStrings)
                {
                    if (string.IsNullOrEmpty(required))
                        continue;                   //チェック対象文字列が空の場合スキップ

                    bool contains = input.Contains(required, comparison);       //チェック対象文字列が含まれているかチェック
                    if (!contains)
                        missingStrings.Add(required);
                }

                if (anyMatch)
                {
                    //いずれか1つでも含む
                    if (missingStrings.Count == requiredStrings.Count(s => !string.IsNullOrEmpty(s)))
                    {
                        errMessage.Add($"# {fieldName}: 必須文字列 {string.Join(", ", requiredStrings.Where(s => !string.IsNullOrEmpty(s)))} のいずれかが含まれていません。");
                        return (errMessage, false);
                    }
                }
                else
                {
                    //すべて含む
                    if (missingStrings.Any())
                    {
                        errMessage.Add($"# {fieldName}: 必須文字列 {string.Join(", ", missingStrings)} が含まれていません。");
                        return (errMessage, false);
                    }
                }
                return (errMessage, true);            //問題ない場合、空のリストとtrueを返す
            }
        }
        //*************************************************
        //USB ID 重複チェック
        // 戻り値：(List<string>, bool) エラーメッセージリストとチェック結果
        // 引数1：measInstData - 測定器データのリスト
        // 引数2：fieldName - エラーメッセージのフィールド名（例: "USBアドレス"）
        //*************************************************
        public (List<string>, bool) CheckDuplicateUsbId(
                                                        List<(bool IsChecked, string UsbId, string InstName, string Identifier)> measInstData,
                                                        string fieldName)
        {
            List<string> errMessage = new List<string>();

            if (measInstData == null || !measInstData.Any())
            {
                errMessage.Add($"# {fieldName}: 測定器データが空またはnullです。");
                return (errMessage, false);
            }

            //チェックされたデバイスのUsbIdとIdentifierを収集
            var checkedDevices = measInstData
                .Where(device => device.IsChecked)
                .Select(device => (device.UsbId, device.Identifier))
                .ToList();

            if (!checkedDevices.Any())
            {
                errMessage.Add($"# {fieldName}: チェックされた測定器がありません。");
                return (errMessage, false);
            }

            //USB ID の重複を検出
            var duplicateUsbIds = checkedDevices
                .GroupBy(device => device.UsbId)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();

            if (duplicateUsbIds.Any())
            {
                foreach (var duplicateId in duplicateUsbIds)
                {
                    var duplicateIdentifiers = checkedDevices
                        .Where(device => device.UsbId == duplicateId)
                        .Select(device => device.Identifier);
                    errMessage.Add($"# {fieldName}: USB ID '{duplicateId}' が複数の測定器 ({string.Join(", ", duplicateIdentifiers)}) で重複しています。");
                }
                return (errMessage, false);
            }
            //重複がない場合
            return (errMessage, true);
        }
        //*************************************************
        //数値形式チェック（TextBox用）
        // 戻り値：(List<string>, bool) チェックエラーメッセージ, チェック結果（true: 正常, false: エラー）
        // 引数1：inputText - チェックする入力文字列
        // 引数2：fieldName - エラーメッセージに表示するフィールド名
        // 引数3：tabName - エラーメッセージに表示するTab名
        //*************************************************
        public (List<string>, bool) CheckNumericInput(string inputText, string fieldName, string tabName)
        {
            List<string> errMessage = new List<string>();
            try
            {
                //入力が空またはnullの場合
                if (string.IsNullOrWhiteSpace(inputText))
                {
                    errMessage.Add($"# {tabName}の{fieldName}が入力されていません。数値を入力してください。");
                    return (errMessage, false);
                }

                //小数点付き数字（double）に変換可能かチェック→数字のみであればdouble型に変換可能
                if (!double.TryParse(inputText, out double result))
                {
                    errMessage.Add($"# {tabName}の{fieldName}が正しい数値形式ではありません。文字を含めないでください。");
                    return (errMessage, false);
                }
                //正常な場合
                return (errMessage, true);
            }
            catch (Exception ex)
            {
                errMessage.Add($"# {fieldName}の数値チェック中にエラーが発生しました: {ex.Message}");
                return (errMessage, false);
            }
        }
    }
}
