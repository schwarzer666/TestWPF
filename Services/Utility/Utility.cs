using System.Globalization;
using System.IO;
using System.Text;

namespace UTility
{
    //*************************************************
    //定義
    // 測定器設定用の専用class（識別）
    //*************************************************
    public class Device
    {
        public string Identifier { get; set; }          //XAMLのcheckbox.tagで設定した識別（SOURCE1,DMM1,OSC等
        public string UsbId { get; set; }               //USB,GPIBのアドレス
        public string InstName { get; set; }            //信号名（ユーザー設定
        public Dictionary<string, object> TabSettings { get; set; }         //タブ名(ItemHeader)で設定を分けるため objectにはSourceSettingsやDmmSettingsが入る
        public Device(string identifier, string usbId, string instName)
        {
            TabSettings = new Dictionary<string, object>();
            Identifier = identifier;
            UsbId = usbId;
            InstName = instName;
        }
    }

    //*************************************************
    //定義
    // 測定器設定用の専用class（電源）
    //*************************************************
    public class SourceSettings
    {
        public string? SourceAct { get; set; }       //ex."Sweep","Constant1"
        public string? Function { get; set; }        //ex."sweep","const"
        public string? Mode { get; set; }            //ex."VOLT","CURR"
        public string? RangeValue { get; set; }      //ex."AUTO","10V","100mA"　電源設定レンジ
        public string? SourceRange { get; set; }     //コマンド送信用電源設定レンジ（10V→10E+0、100mV→100E-3)
        public string? SourceRangeUnit { get; set; } //Const設定時の電源設定値単位("V","mA")
        public double SourceValue { get; set; }      //電源設定値
        public double SourceLimit { get; set; }     //電源Limit設定値
        public string? SourceLimitUnit { get; set; } //電源Limit設定レンジ

    }

    //*************************************************
    //定義
    // 測定器設定用の専用class（DMM）
    //*************************************************
    public class DmmSettings
    {
        public string? Mode { get; set; }            //ex."VOLT","CURR"
        public string? RangeValue { get; set; }      //ex."10V","100mA" DMM設定レンジ
        public string? RangeUnit { get; set; }       //ex."range1","range2" debug用途
        public string? Plc { get; set; }             //ex."10"
        public bool DisplayOn { get; set; }         //DisplayOnFlag
        public string? DmmRange { get; set; }        //ex."10V","100mA"
        public string? TriggerSource { get; set; }   //"IMM","BUS","EXT"
    }

    //*************************************************
    //定義
    // 測定器設定用の専用class（OSC）
    //*************************************************
    public class OscSettings
    {
        public string[]? ChannelSettings { get; set; }      //ch1-4 RangeとPosition
        public string? TriggerSource { get; set; }          //ex."1","EXT"
        public string? TriggerDirection { get; set; }       //ex."RISE","FALL"
        public string? TriggerLevel { get; set; }           //Triger Level
        public string[]? TimeSettings { get; set; }         //ex."100ms","1s","range1","range2","range3","50.0%"
        //public string[]? MeasSetting { get; set; }           //MeasureCHとEdge方向とMeasure範囲(-5～5)
        public string? DelaySetupCh { get; set; }           //Delay測定対象CH
        public string? Polarity { get; set; }               //Delay測定対象CH極性
        public string? RefCh { get; set; }                  //Delay測定用RefCH
        public float TRange1 { get; set; }                  //Measure測定範囲1
        public float TRange2 { get; set; }                  //Measure測定範囲2
    }
    //*************************************************
    //定義
    // 測定器設定用の専用class（PulseGeneretor）
    //*************************************************
    public class PGSettings
    {
        public bool PGCheck { get; set; }                   //PulseGenerator使用チェック
        public string? Function { get; set; }               //ex."PULS","SQUA"
        public double LowLevelValue { get; set; }           //LowLevel設定値
        public double HighLevelValue { get; set; }          //HighLevel設定値
        public string? OutputCH { get; set; }               //ex."1","2" 出力CH
        public string? Polarity { get; set; }               //ex."NORM","INV" 出力極性
        public double PeriodValue { get; set; }             //Period設定値
        public double WidthValue { get; set; }              //Width設定値
        public string? OutputZ { get; set; }                //ex."50","INF" 出力インピーダンス
        public string? TrigOut { get; set; }                //トリガ出力ON/OFF
    }
    //*************************************************
    //定義
    // Sweep電源設定用の専用class
    //*************************************************
    public class SweepSettings
    {
        public double StepValue { get; set; }
        public string? StepValueUnit { get; set; }
        public float StepTime { get; set; }
        public string? StepTimeUnit { get; set; }
        public float StandbyTime{ get; set; }
        public string? StandbyTimeUnit { get; set; }
        public string? Directional { get; set; }
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public bool Normalsweep { get; set; }
        public string? MinValueUnit { get; set; }
        public string? MaxValueUnit { get; set; }
    }
    //*************************************************
    //定義
    // Detect/Release挙動設定用の専用class
    //*************************************************
    public class DetRelSettings
    {
        public string? Act { get; set; }
        public string? SourceA { get; set; }
        public string? SourceB { get; set; }
        public string? SourceAUnit { get; set; }
        public string? SourceBUnit { get; set; }
        public double ValueA { get; set; }
        public double ValueB { get; set; }
        public float CheckTime{ get; set; }
        public string? CheckTimeUnit { get; set; }
        public bool DetRelSpecial { get; set; }
    }
    //*************************************************
    //定義
    // VI測定の専用class(measure standby)
    //*************************************************
    public class VISettings
    {
        public float StandbyTime { get; set; }
        public string? StandbyTimeUnit { get; set; }
    }
    //*************************************************
    //定義
    // utility
    //*************************************************
    public class UT
    {
        private static UT? instance;   //インスタンスをnull許容型(xxx?)として宣言し初期値がnullでも問題ないと表示
        private static readonly Dictionary<string, int> _tabFileCounters = new Dictionary<string, int>(); // タブごとの連番管理

        private UT()        //コンストラクタ　インスタンスが生成(=初期化)された段階で実行される
        {
            //何か初期化する変数等
        }
        public static UT Instance     //外部からのアクセス用
        {
            get
            {
                if (instance == null)
                    instance = new UT();                            // クラス内でインスタンスを生成
                return instance;
            }
        }

        //*************************************************
        //アクセス：public
        //戻り値：なし(Task)
        //機能：指定時間ウェイト設定
        //引数1：wait_ms
        //説明：<int> ウェイト時間(ms)
        // 0～4294967295 (32bit)
        //コメント
        // 使用するときはawait演算子を付けて呼び出し
        //*************************************************
        public async Task Wait_Timer(int wait_ms, CancellationToken cancellationToken = default)
        {
            if (wait_ms == 0 || cancellationToken.IsCancellationRequested)      //引数wait_ms=0やキャンセル要求が来た時
                return;                                                         //即時タイマー完了

            await Task.Delay(wait_ms, cancellationToken).ConfigureAwait(false);
        }

        //*************************************************
        //アクセス：public
        //戻り値：List<string> チェックされている各測定器の識別、アドレス、信号名
        //機能：使用する測定器の通信用アドレス抽出
        //引数1：device
        //説明：List(checkbox,textbox_ID,textbox_NAME)
        // 0～4294967295 (32bit)
        //コメント
        // 受け取ったリストの中で
        // チェックボックスがチェックされている測定器のアドレスを抽出し応答として返す
        // .Addメソッドは、タプル型(string, string, string)を1つの引数として受け取る
        // そのため、Add((identifier, usbid, instname))のように、タプルを括弧で囲んで渡す必要がある
        //*************************************************
        public List<(string identifier, string usbid, string instname)> GetActiveUSBAddr(
                                    List<(bool IsChecked, string UsbId, string InstName, string Identifier)> devices)
        {
            List<(string identifier, string usbid, string instname)> activeUsbaddr = new List<(string, string, string)>();        //識別・USBID情報・信号名を呼び出し元に渡す

            foreach (var device in devices)
            {
                if (device.IsChecked == true)
                {
                    string usbid = device.UsbId.Trim();                          //各測定器のアドレス(ID)を空白を削除して読み込み
                    string identifier = device.Identifier ?? "Unknown";      //各測定器のcheckboxに設定されたTag値を読み込み("SOURCE1","DMM1","OSC"
                    string instname = device.InstName.Trim() ?? "NoName";
                    activeUsbaddr.Add((identifier, usbid, instname));
                }
            }
            return activeUsbaddr;
        }

        //*************************************************
        //アクセス：public
        //戻り値：
        //　　　　List<Device> フィルタリングされた設定リスト
        //　　　　<bool>フィルタリング結果(true or false)
        //機能：受け取った紐づけリストから、各測定器の設定をフィルタリング
        //引数1：FilterDevice
        //説明：<List><Device> チェックされているUSBアドレスと測定Tabから各測定器設定を取得し紐づけされているList
        //引数2:identifierPrefix
        //説明：<string> フィルタしたい測定器の種類
        // "SOURCE","DMM","OSC"等
        //引数3:tabItem
        //説明：<string> tab名(測定項目名)
        // "Item1"等
        //*************************************************
        public (List<Device> FilterDevice, bool Success) FilterSettings(
                                    List<Device> deviceList, string identifierPrefix, string tabItem)
        {
            //受け取ったdeviceListから指定した測定器(identifierPrefix)の設定を抜き出し測定項目毎(tabItem)にまとめる
            List<Device> filterDevices = deviceList
                .Where(d => d.Identifier.StartsWith(identifierPrefix) && d.TabSettings.ContainsKey(tabItem)).ToList();

            //測定器が存在しない場合
            if (!filterDevices.Any())
            {
                //MessageBox.Show($"# {identifierPrefix} 設定抽出でエラー");
                return (filterDevices, false);
            }

            return (filterDevices, true);
        }

        //*************************************************
        //アクセス：public
        //戻り値：<string> 単位変換された値
        //機能：受け取った文字列を単位に変換
        //引数1：range
        //説明：<string> 変換したい文字列
        // 1V/100mV等
        //*************************************************
        public (string unit, double scalingFactor) Range_Conv(string range)
        {
            string unit;
            double scalingFactor;
            switch (range)
            {
                case "range0":
                    unit = "AUTO";
                    scalingFactor = 1.0d;
                    break;
                case "range1":
                    unit = "E+0";
                    scalingFactor = 1.0d; //×1
                    break;
                case "range2":
                    unit = "E-3";
                    scalingFactor = 0.001d; //mV,mA ×0.001
                    break;
                case "range3":
                    unit = "E-6";
                    scalingFactor = 0.000001d; //uV,uA ×0.000001
                    break;
                default:
                    unit = "AUTO";
                    scalingFactor = 1.0d;
                    break;
            }
            return (unit, scalingFactor);
        }

        //*************************************************
        //アクセス：public
        //戻り値：<string> レンジ
        //機能：設定するレンジを文字列に変換（ex.10V→10E+0、1mV→1E-3
        //引数1：rangeValue
        //説明：<string> レンジ
        // "AUTO","10V","1mA","100us"
        //引数2：rangeUnit
        //説明：<string> レンジ単位
        // "range1","range2"
        //*************************************************
        public string GetRangeString(string rangeValue, string rangeUnit)
        {
            if (string.IsNullOrWhiteSpace(rangeValue)) return "AUTO";

            //"AUTO"の場合そのまま返す
            if (rangeValue == "AUTO") return "AUTO";

            //単位を除去
            string numericPart = rangeValue.TrimEnd('V', 'A', 's');
            if (numericPart.EndsWith("m"))
                numericPart = numericPart.TrimEnd('m');
            if (numericPart.EndsWith("u"))
                numericPart = numericPart.TrimEnd('u');

            //数値として有効か確認
            if (float.TryParse(numericPart, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                //rangeUnitを変換 range1～3
                string unit = Range_Conv(rangeUnit ?? "").unit;
                return $"{numericPart}{unit}";          //numericPart + unit;
            }
            return "AUTO";
        }

        //*************************************************
        //アクセス：public
        //戻り値：<double> String型→Double型変換
        //機能：数値文字列とレンジ文字列をDouble数値に変換
        //引数1：Value
        //説明：<string> 数値文字列
        //引数2：rangeUnit
        //説明：<string> レンジ単位
        // "range1","range2"
        //コメント
        // Valueがnullか""の時0を返す
        //*************************************************
        public double String2Double_Conv(string Value, string Unit)
        {
            if (string.IsNullOrEmpty(Value)) return 0.0;
            double result = double.Parse(Value);
            double scalingFactor = Range_Conv(Unit).scalingFactor;
            return result * scalingFactor;
        }

        //*************************************************
        //アクセス：public
        //戻り値：<float> String型→Float型変換
        //機能：数値文字列とレンジ文字列をFloat数値に変換
        //引数1：Value
        //説明：<string> 数値文字列
        //引数2：rangeUnit
        //説明：<string> レンジ単位
        // "range1","range2"
        //*************************************************
        public float String2Float_Conv(string Value, string Unit)
        {
            float result = float.Parse(Value);
            float scalingFactor = (float)Range_Conv(Unit).scalingFactor;
            return result * scalingFactor;
        }

        //*************************************************
        //アクセス：public
        //戻り値：なし
        //機能：ファイルパスとデータを受け取ってファイルに書き込み
        //引数1：filePath
        //説明：<string> 書き込みファイルのパス
        //引数2：csvRows
        //説明：List<string> 書き込みデータ
        //以下省略可能
        //引数3：append
        //説明：<bool> 書き込みモード（default false）
        // append=true (追記)、append=false (上書き)
        //引数4：useShiftJis
        //説明：<bool> エンコードの種類（default false）
        // useShiftJis=true (Shift-JIS)、useShiftJis=false (UTF8)
        //*************************************************
        public async Task WriteCsvFileAsync(string filePath, List<string> csvRows, bool append = false, bool useShiftJis = false)
        {
            try
            {
                Encoding encoding = useShiftJis ? Encoding.GetEncoding("shift-jis") : Encoding.UTF8;
                //append=true (追記)
                if (append)
                    await File.AppendAllLinesAsync(filePath, csvRows, encoding);
                //append=false (上書き)
                else
                    await File.WriteAllLinesAsync(filePath, csvRows, encoding);
            }
            catch (Exception ex)
            {
                throw new Exception($"CSVファイルの書き込みに失敗しました: {filePath}, エラー: {ex.Message}", ex);
            }
        }
        //*************************************************
        //アクセス：private
        //戻り値：<string> 保管フォルダパス
        //機能：デスクトップの保管フォルダパスを取得
        //*************************************************
        private string GetMeasDataFolder()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);      //ユーザーのデスクトップパス取得
            string measDataFolder = Path.Combine(desktopPath, "温特自動測定結果");                 //保管フォルダ設定
            Directory.CreateDirectory(measDataFolder);                                             //フォルダが存在しない場合は作成
            return measDataFolder;
        }
        //*************************************************
        //アクセス：private
        //戻り値：<string> 中間データ保管フォルダパス
        //機能：デスクトップの中間データ保管フォルダパスを取得
        //*************************************************
        private string GetTempDataFolder()
        {
            string tempDataFolder = Path.Combine(GetMeasDataFolder(), "中間データ");
            Directory.CreateDirectory(tempDataFolder);
            return tempDataFolder;
        }
        //*************************************************
        //アクセス：public
        //戻り値：<string> ファイル名付き中間データ保存パス
        //機能：中間データ保存先のパスを取得
        //*************************************************
        public string GetTempFilePath(string timestamp)
        {
            return Path.Combine(GetTempDataFolder(), $"_TemporaryData_{timestamp}.csv");
        }
        //*************************************************
        //アクセス：public
        //戻り値：<string> 中間データ保存パス
        //機能：タブごとの中間ファイルパスを取得（連番付き）
        //*************************************************
        // タブごとの中間ファイルパスを取得（連番付き）
        public string GetTabTempFilePath(string tabName, string baseTimestamp)
        {
            if (!_tabFileCounters.ContainsKey(tabName))
                _tabFileCounters[tabName] = 0;              //初回は0
            int counter = _tabFileCounters[tabName]++;
            return Path.Combine(GetTempDataFolder(), $"_TemporaryData_{tabName}_{baseTimestamp}_{counter}.csv");
        }
        //*************************************************
        //アクセス：public
        //戻り値：<string> 最終データ保存パス
        //機能：最終データ保存先のパスを取得
        //*************************************************
        public string GetFinalFilePath(string? userFileName)
        {
            //string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            //return Path.Combine(GetMeasDataFolder(), $"DraftData_{timestamp}.csv");

            string folder = GetMeasDataFolder();
            if (string.IsNullOrWhiteSpace(userFileName))
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                return Path.Combine(folder, $"ResultData_{timestamp}.csv");
            }

            return Path.Combine(folder, $"{userFileName}.csv");
        }

        //*************************************************
        //アクセス：public
        //戻り値：なし
        //機能：連番カウンターをリセット
        //*************************************************
        public void ResetTabFileCounters()
        {
            _tabFileCounters.Clear();
        }
        //*************************************************
        //アクセス：public
        //戻り値：最終ファイル名
        //機能：最終ファイル名生成
        //*************************************************
        public string GetDefaultFileNameOnly()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"ResultData_{timestamp}";
        }

    }
}

