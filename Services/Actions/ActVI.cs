using InputCheck;                   //ErrCheck.cs
using MEASUREcommunication;         //CommMEASURE.cs
using SOURCEcommunication;          //CommSOURCE.cs
using System.Globalization;         //NumberStyles.Float, CultureInfo.InvariantCultureを使用するのに必要
using System.Text;
using TemperatureCharacteristics.Exceptions;    //例外スロー
using TemperatureCharacteristics.Models;
using TemperatureCharacteristics.Models.TabData;
using UTility;                      //Utility.cs

namespace TemperatureCharacteristics.Services.Actions
{
    public class VI: IDeviceCombinable<VITabData>
    {
        private static VI? instance;             //インスタンスをnull許容型(xxx?)として宣言し初期値がnullでも問題ないと表示
        private readonly SOURCEcomm commSOURCE;     //フィールド変数commSOURCE
        private readonly MEASUREcomm commMEASURE;   //フィールド変数commMEASURE
        private readonly UT utility;                //フィールド変数utility
        private readonly InpCheck errCheck;         //フィールド変数errCheck

        private VI()
        {
            //初期化する変数
            utility = UT.Instance;                  //インスタンス生成(=初期化)を別クラス内で実行
            commSOURCE = SOURCEcomm.Instance;       //インスタンス生成(=初期化)を別クラス内で実行
            commMEASURE = MEASUREcomm.Instance;     //インスタンス生成(=初期化)を別クラス内で実行
            errCheck = InpCheck.Instance;           //インスタンス生成(=初期化)を別クラス内で実行
        }
        //*************************************************
        //初期化
        //*************************************************
        public static VI Instance     //外部からのアクセス用
        {
            get
            {
                if (instance == null)
                    instance = new VI(); // クラス内でインスタンスを生成
                return instance;
            }
        }
        //*************************************************
        //動作
        // checkboxがチェックされているUSBアドレスリストと
        // checkboxがチェックされているTabから各測定器設定を取得し紐づけ
        //*************************************************
        public List<Device> CombineDeviceData(
                                        List<(bool IsChecked, string UsbId, string InstName, string Identifier)> meas_inst,
                                        IEnumerable<VITabData> tabData)
        {
            if (tabData == null || !tabData.Any())
                return new List<Device>();
            //*************************************
            //checkboxがチェックされている測定器の
            //識別・USBアドレス・信号名をリスト化
            //+ VISettings用のidentifier追加
            //+ Detect/Release用のidentifier追加
            //*************************************
            var activeDevices = utility.GetActiveUSBAddr(meas_inst);
            if (!activeDevices.Any())
                return new List<Device>();          //測定器のチェックボックスに一つもチェックが入っていない場合CombineDeviceDataを抜ける
            activeDevices.Add(("VI","",""));
            activeDevices.Add(("DETREL", "", ""));
            //*************************************
            //デバイスを辞書で管理
            //*************************************
            var deviceDict = activeDevices.ToDictionary(
                d => d.identifier,
                d => new Device(d.identifier, d.usbid, d.instname));
            //*************************************
            //各測定器設定を取得したtabDataをtab.TabName(=ItemHeader)毎でまとめる
            //*************************************
            foreach (var tab in tabData)
            {
                //*************************************
                //VI条件(measure standby)
                //*************************************
                if (tab.VIset != null && tab.VIset.Any())
                {
                    deviceDict["VI"].TabSettings[tab.TabName] = new VISettings
                    {
                        StandbyTime = utility.String2Float_Conv(tab.VIset[0], tab.VIset[1])                 //測定スタンバイ時間
                    };
                }
                //*************************************
                //Detect/Release条件
                //*************************************
                if (deviceDict.ContainsKey("DETREL") && tab.Detrelset != null)
                {
                    deviceDict["DETREL"].TabSettings[tab.TabName] = new DetRelSettings
                    {
                        Act = "ActSpecial1",                                                                //ACT:"ActNormal","ActSpecial1"
                        SourceA = tab.Detrelset[0],                                                         //検出復帰動作にVDD以外の電源を追加で使用する場合A（VMを想定
                        ValueA = utility.String2Double_Conv(tab.Detrelset[1], tab.Detrelset[2]),            //検出復帰動作で電源Aで印加する値
                        SourceB = tab.Detrelset[3],                                                         //検出復帰動作にVDD以外の電源を追加で使用する場合B（CSを想定
                        ValueB = utility.String2Double_Conv(tab.Detrelset[4], tab.Detrelset[5]),            //検出復帰動作で電源Bで印加する値
                        CheckTime = utility.String2Float_Conv(tab.Detrelset[6], tab.Detrelset[7])           //検出復帰動作の時間
                    };
                }
                //*************************************
                //SOURCE1-4
                //*************************************
                if (deviceDict.ContainsKey("SOURCE1") && tab.Source1set != null)
                {
                    deviceDict["SOURCE1"].TabSettings[tab.TabName] = new SourceSettings
                    {
                        SourceAct = tab.Source1set[0],                                                      //ACT:"Sweep","Constant1"
                        Function = tab.Source1set[1],                                                       //Func:"sweep","const"
                        Mode = tab.Source1set[2],                                                           //MODE:"VOLT","CURR"
                        SourceValue = GetSourceValue(tab.Source1set[0], tab.Constset),                      //電源設定値 Act="Constant1-4"でSourceValue場合分け
                        SourceRange = utility.GetRangeString(tab.Source1set[3], tab.Source1set[4]),         //コマンド送信用に電源設定レンジを数値文字列に変換(100mV→100E-3)
                        RangeValue = tab.Source1set[3],                                                     //電源設定レンジ("AUTO","2V")
                        SourceRangeUnit = GetSourceRangeUnit(tab.Source1set[0], tab.Constset),              //Const設定時の電源設定値単位("V","mA")
                        SourceLimit = utility.String2Double_Conv(tab.Source1set[5], tab.Source1set[6]),     //電源Limit設定値
                        SourceLimitUnit = tab.Source1set[7]                                                 //電源Limit設定レンジ
                    };
                }
                if (deviceDict.ContainsKey("SOURCE2") && tab.Source2set != null)
                {
                    deviceDict["SOURCE2"].TabSettings[tab.TabName] = new SourceSettings
                    {
                        SourceAct = tab.Source2set[0],                                                      //ACT:"Sweep","Constant1"
                        Function = tab.Source2set[1],                                                       //Func:"sweep","const"
                        Mode = tab.Source2set[2],                                                           //MODE:"VOLT","CURR"
                        SourceValue = GetSourceValue(tab.Source2set[0], tab.Constset),                      //電源設定値 Act="Constant1-4"でSourceValue場合分け
                        SourceRange = utility.GetRangeString(tab.Source2set[3], tab.Source2set[4]),         //コマンド送信用に電源設定レンジを数値文字列に変換(100mV→100E-3)
                        RangeValue = tab.Source2set[3],                                                     //電源設定レンジ("AUTO","2V")
                        SourceRangeUnit = GetSourceRangeUnit(tab.Source2set[0], tab.Constset),              //Const設定時の電源設定値単位("V","mA")
                        SourceLimit = utility.String2Double_Conv(tab.Source2set[5], tab.Source2set[6]),     //電源Limit設定値
                        SourceLimitUnit = tab.Source2set[7]                                                 //電源Limit設定レンジ
                    };
                }
                if (deviceDict.ContainsKey("SOURCE3") && tab.Source3set != null)
                {
                    deviceDict["SOURCE3"].TabSettings[tab.TabName] = new SourceSettings
                    {
                        SourceAct = tab.Source3set[0],                                                      //ACT:"Sweep","Constant1"
                        Function = tab.Source3set[1],                                                       //Func:"sweep","const"
                        Mode = tab.Source3set[2],                                                           //MODE:"VOLT","CURR"
                        SourceValue = GetSourceValue(tab.Source3set[0], tab.Constset),                      //電源設定値 Act="Constant1-4"でSourceValue場合分け
                        SourceRange = utility.GetRangeString(tab.Source3set[3], tab.Source3set[4]),         //コマンド送信用に電源設定レンジを数値文字列に変換(100mV→100E-3)
                        RangeValue = tab.Source3set[3],                                                     //電源設定レンジ("AUTO","2V")
                        SourceRangeUnit = GetSourceRangeUnit(tab.Source3set[0], tab.Constset),              //Const設定時の電源設定値単位("V","mA")
                        SourceLimit = utility.String2Double_Conv(tab.Source3set[5], tab.Source3set[6]),     //電源Limit設定値
                        SourceLimitUnit = tab.Source3set[7]                                                 //電源Limit設定レンジ
                    };
                }
                if (deviceDict.ContainsKey("SOURCE4") && tab.Source4set != null)
                {
                    deviceDict["SOURCE4"].TabSettings[tab.TabName] = new SourceSettings
                    {
                        SourceAct = tab.Source4set[0],                                                      //ACT:"Sweep","Constant1"
                        Function = tab.Source4set[1],                                                       //Func:"sweep","const"
                        Mode = tab.Source4set[2],                                                           //MODE:"VOLT","CURR"
                        SourceValue = GetSourceValue(tab.Source4set[0], tab.Constset),                      //電源設定値 Act="Constant1-4"でSourceValue場合分け
                        SourceRange = utility.GetRangeString(tab.Source4set[3], tab.Source4set[4]),         //コマンド送信用に電源設定レンジを数値文字列に変換(100mV→100E-3)
                        RangeValue = tab.Source4set[3],                                                     //電源設定レンジ("AUTO","2V")
                        SourceRangeUnit = GetSourceRangeUnit(tab.Source4set[0], tab.Constset),              //Const設定時の電源設定値単位("V","mA")
                        SourceLimit = utility.String2Double_Conv(tab.Source4set[5], tab.Source4set[6]),     //電源Limit設定値
                        SourceLimitUnit = tab.Source4set[7]                                                 //電源Limit設定レンジ
                    };
                }
                //*************************************
                //DMM1-4
                //*************************************
                if (deviceDict.ContainsKey("DMM1") && tab.DMM1set != null)
                {
                    deviceDict["DMM1"].TabSettings[tab.TabName] = new DmmSettings
                    {
                        Mode = tab.DMM1set[0],                                                              //MODE:"VOLT","CURR"
                        Plc = tab.DMM1set[3],                                                               //PLC:"10"
                        DisplayOn = tab.DMMDisp[0],                                                         //DisplayOnFlag
                        DmmRange = utility.GetRangeString(tab.DMM1set[1], tab.DMM1set[2]),                  //コマンド送信用にDMM設定レンジを数値文字列に変換(100mV→100E-3)
                        RangeValue = tab.DMM1set[1],                                                        //DMM設定レンジ("AUTO","10V")
                        TriggerSource = tab.DMMTrigSrc                                                      //トリガソース
                    };
                }
                if (deviceDict.ContainsKey("DMM2") && tab.DMM2set != null)
                {
                    deviceDict["DMM2"].TabSettings[tab.TabName] = new DmmSettings
                    {
                        Mode = tab.DMM2set[0],                                                              //MODE:"VOLT","CURR"
                        Plc = tab.DMM2set[3],                                                               //PLC:"10"
                        DisplayOn = tab.DMMDisp[1],                                                         //DisplayOnFlag
                        DmmRange = utility.GetRangeString(tab.DMM2set[1], tab.DMM2set[2]),                  //コマンド送信用にDMM設定レンジを数値文字列に変換(100mV→100E-3)
                        RangeValue = tab.DMM2set[1],                                                        //DMM設定レンジ("AUTO","10V")
                        TriggerSource = tab.DMMTrigSrc                                                      //トリガソース
                    };
                }
                if (deviceDict.ContainsKey("DMM3") && tab.DMM3set != null)
                {
                    deviceDict["DMM3"].TabSettings[tab.TabName] = new DmmSettings
                    {
                        Mode = tab.DMM3set[0],                                                              //MODE:"VOLT","CURR"
                        Plc = tab.DMM3set[3],                                                               //PLC:"10"
                        DisplayOn = tab.DMMDisp[2],                                                         //DisplayOnFlag
                        DmmRange = utility.GetRangeString(tab.DMM3set[1], tab.DMM3set[2]),                  //コマンド送信用にDMM設定レンジを数値文字列に変換(100mV→100E-3)
                        RangeValue = tab.DMM3set[1],                                                        //DMM設定レンジ("AUTO","10V")
                        TriggerSource = tab.DMMTrigSrc                                                      //トリガソース
                    };
                }
                if (deviceDict.ContainsKey("DMM4") && tab.DMM4set != null)
                {
                    deviceDict["DMM4"].TabSettings[tab.TabName] = new DmmSettings
                    {
                        Mode = tab.DMM4set[0],                                                              //MODE:"VOLT","CURR"
                        Plc = tab.DMM4set[3],                                                               //PLC:"10"
                        DisplayOn = tab.DMMDisp[3],                                                         //DisplayOnFlag
                        DmmRange = utility.GetRangeString(tab.DMM4set[1], tab.DMM4set[2]),                  //コマンド送信用にDMM設定レンジを数値文字列に変換(100mV→100E-3)
                        RangeValue = tab.DMM4set[1],                                                        //DMM設定レンジ("AUTO","10V")
                        TriggerSource = tab.DMMTrigSrc                                                      //トリガソース
                    };
                }
            }
            return deviceDict.Values.ToList();
        }

        //*************************************************
        //アクセス：private
        //戻り値：<float> SourceValue初期値
        //機能：SourceValue初期値判別用メソッド
        //引数1：sourceAct
        //説明：<string> 電源動作
        // "Sweep" or "Constant1"-"Constant3"
        //引数2：constSet
        //説明：<string> constant電源設定群
        // Constset[0]:constant電源1 value
        // Constset[1]:constant電源1 unit
        // Constset[2]:constant電源2 value
        // Constset[3]:constant電源2 unit
        // Constset[4]:constant電源3 value
        // Constset[5]:constant電源3 unit
        // Constset[9]:constant電源4 value
        // Constset[10]:constant電源4 unit
        //*************************************************
        private double GetSourceValue(string sourceAct, string[] constSet)
        {
            if (string.IsNullOrEmpty(sourceAct)) return 0.0f;

            string value = "";
            string unit = "";
            if (sourceAct == "Constant1")
            {
                value = constSet[0];
                unit = constSet[1];
            }
            else if (sourceAct == "Constant2")
            {
                value = constSet[2];
                unit = constSet[3];
            }
            else if (sourceAct == "Constant3")
            {
                value = constSet[4];
                unit = constSet[5];
            }
            else if (sourceAct == "Constant4")
            {
                value = constSet[9];
                unit = constSet[10];
            }
            else if (sourceAct == "NotUsed" || sourceAct == "Sweep")
                return 0.0f;

            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            {
                double scalingFactor = utility.Range_Conv(unit).scalingFactor;
                return result * scalingFactor;
            }
            return 0.0f;
        }
        //*************************************************
        //アクセス：private
        //戻り値：<string> 電源設定値単位SourceRangeUnit
        //機能：電源設定値単位SourceRangeUnit判別用メソッド
        //引数1：sourceAct
        //説明：<string> 電源動作
        // "constant1"-"constant4"
        //引数2：constSet
        //説明：<string> constant電源設定群
        // Constset[6]:constant電源1 RangeUnit
        // Constset[7]:constant電源2 RangeUnit
        // Constset[8]:constant電源3 RangeUnit
        // Constset[11]:constant電源4 RangeUnit
        //*************************************************
        private string GetSourceRangeUnit(string sourceAct, string[] constSet)
        {
            if (string.IsNullOrEmpty(sourceAct)) return "";

            string rangeUnit = "";
            if (sourceAct == "Constant1")
                rangeUnit = constSet[6];
            else if (sourceAct == "Constant2")
                rangeUnit = constSet[7];
            else if (sourceAct == "Constant3")
                rangeUnit = constSet[8];
            else if (sourceAct == "Constant4")
                rangeUnit = constSet[11];

            return rangeUnit;
        }

        //*************************************************
        //アクセス：public
        //戻り値：<string> VI結果
        //機能：VI動作
        //　　　各DMMの結果を返す
        //引数1：setting
        //説明：<var> 測定器設定＋VI条件
        // 各電源設定
        // constant電源設定
        // dmm設定
        // 検出復帰設定
        // 測定条件
        //コメント
        //*************************************************
        public async Task<List<string>> VIAction(
                                            List<(bool IsChecked, string UsbId, string InstName, string Identifier)> meas_inst,
                                            IEnumerable<VITabData> tabData,
                                            string[] tabNames,
                                            DebugOption debugOption,
                                            CancellationToken cancellationToken = default,
                                            List<Device>? preCombinedDevices = null)
        {
            cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
            //*********************
            //定義
            //*********************
            List<string> viData = new List<string>();                               //エラー発生時の入力用変数
            List<string> csvRows = new List<string>();                              //戻り値（測定データ） エラー発生時には#を付けたコメントが入る
            List<string> tabCsvRows = new List<string>();                           //各タブ用のデータ（中間データ）を初期化
            List<string> viRows = new List<string>();                               //VIデータ保持用
            Dictionary<string, List<string>> resultDataRowsByTab = new Dictionary<string, List<string>>();
                                                                                    //VI結果データを辞書形式で定義
            List<Device>? deviceList = null;                                        //測定器一覧
            string currentTabName = string.Empty;                                   //処理中タブの追跡用変数初期化
            //*********************
            //中間データ保存先取得
            //*********************
            string baseTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");        //タイムスタンプ生成
            string baseTempFilePath = utility.GetTempFilePath(baseTimestamp);
            //*********************
            //ヘッダー用
            //*********************
            List<string> deviceHeaderRows = new List<string>();                     //測定器ヘッダー
            Dictionary<string, List<string>> measHeaderRows = new Dictionary<string, List<string>>();       //タブごとの測定条件ヘッダーを辞書形式で定義

            try
            {
                //*********************
                //チェックされているUSBアドレスと
                //チェックされているTabから各測定器設定を取得し紐づけてリスト化
                //すでに紐づけている場合はそのまま（preCombinedDevices
                //*********************
                //deviceList = preCombinedDevices ?? CombineDeviceData(meas_inst, _viTab.GetMeasureOnTabData);
                deviceList = preCombinedDevices ?? CombineDeviceData(meas_inst, tabData);

                if (!deviceList.Any())
                {
                    viData.Add("チェックされた測定器無し");
                    csvRows.Add($"# {string.Join(" ", viData).Replace(Environment.NewLine, " ")}");
                    return csvRows;
                }
                //*********************
                //測定Tab名を取得
                //*********************
                ////以下TabViewModelに移動
                //string TabName = _viTab.CheckedTabNamesText.Trim();
                ////タブ名をカンマで分割＋文字列前後の空白を削除
                //string[] tabNames = TabName?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                //                            ?.Select(t => t.Trim())
                //                            ?.ToArray() ?? Array.Empty<string>();
                //if(tabNames.FirstOrDefault() == "対象なし")
                //{
                //    viData.Add("測定対象選択無し");
                //    csvRows.Add($"# {string.Join(" ", viData).Replace(Environment.NewLine, " ")}");
                //    return csvRows;
                //}
                if (tabNames.Length == 0 || tabNames[0] == "対象なし")
                {
                    viData.Add("測定対象選択無し");
                    csvRows.Add($"# {string.Join(" ", viData).Replace(Environment.NewLine, " ")}");
                    return csvRows;
                }
                //*********************
                //入力チェック
                //*********************
                (viData, bool isVerify) = await errCheck.VerifyVITabInputs(deviceList, tabNames);
                if (!isVerify)
                {
                    viData.Add("以下の箇所で入力に不備があります");
                    csvRows.Add($"# {string.Join(" ", viData).Replace(Environment.NewLine, " ")}");
                    return viData;
                }
                //*********************
                //マルチメーターAutoレンジ使用時注意メッセージ表示(OK,NG)
                //未実装
                //*********************
                //(viData, isVerify) = await errCheck.SourceRangeAuto(deviceList, tabNames);
                //if (!isVerify)
                //{
                //    bool confirmed = await confirmCallback();
                //    viData.Clear();
                //    if (!confirmed)
                //    {
                //        viData.Add("測定がユーザーによりキャンセルされました。");
                //        csvRows.Add($"# {string.Join(" ", viData).Replace(Environment.NewLine, " ")}");
                //        throw new OperationCanceledException("ユーザーによるキャンセル");
                //    }
                //}
                //*********************
                //測定器ヘッダーの生成
                //*********************
                await CreateInsHeaders(deviceHeaderRows, meas_inst,deviceList);
                //*********************
                //測定項目名(tabname)毎に測定条件と測定結果ヘッダーの生成しリスト化
                //*********************
                foreach (string tabname in tabNames)
                {
                    if (tabname == "対象なし")
                        continue;
                    List<string> tempMeasHeaders = new List<string>();
                    await CreateMeasHeaders(tempMeasHeaders, meas_inst, tabname, deviceList);
                    measHeaderRows[tabname] = tempMeasHeaders;
                }
                //*********************
                //測定項目名(tabname)毎に繰り返し
                //*********************
                foreach (string tabname in tabNames)
                {
                    cancellationToken.ThrowIfCancellationRequested();           //ループ中キャンセルチェック
                    if (tabname == "対象なし")
                        continue;                   //以降スキップ
                    //*********************
                    //データ保存用にtab名を取得
                    //*********************
                    currentTabName = tabname;
                    //*********************
                    //中間データファイル名変更
                    //*********************
                    string tabNameFilePath = utility.GetTabTempFilePath(tabname, baseTimestamp);
                    //*********************
                    //初期化
                    //*********************
                    tabCsvRows.Clear();                             //中間データ保持用tabCsvRowsをクリア
                    viRows.Clear();                                 //VI測定結果をクリア
                    resultDataRowsByTab[tabname] = new List<string>();
                    //*********************
                    //中間データ用に要素追加
                    //*********************
                    tabCsvRows.AddRange(deviceHeaderRows);          //中間データに測定器ヘッダー追加
                    tabCsvRows.AddRange(measHeaderRows[tabname]);   //中間データに測定条件と測定結果ヘッダー追加
                    //*********************
                    //中間ファイルにヘッダー保存
                    //*********************
                    await utility.WriteCsvFileAsync(tabNameFilePath, tabCsvRows, append: false, useShiftJis: true);
                    //*********************
                    //各測定器設定抽出
                    //*********************
                    List<Device>? sourceDevices = null;     //初期化
                    List<Device>? dmmDevices = null;        //初期化
                    (sourceDevices, bool success) = utility.FilterSettings(deviceList, "SOURCE", tabname);
                    (dmmDevices, bool dmmsuccess) = utility.FilterSettings(deviceList, "DMM", tabname);
                    //*********************
                    //VI設定抽出(measure standby)
                    //*********************
                    List<Device>? Settings = null;
                    (Settings, bool settingsuccess) = utility.FilterSettings(deviceList, "VI", tabname);
                    Device? device = Settings.FirstOrDefault();             //VI設定は各Tab内で一つだけなので最初のデータ群だけで問題なし
                    VISettings? viSettings = device.TabSettings[tabname] as VISettings ?? new VISettings();
                    float standbyTime = viSettings.StandbyTime;             //measure standby値
                    //********************* 
                    //Detect,Release条件抽出
                    //*********************
                    List<Device>? detectreleaseSettings = null;
                    (detectreleaseSettings, bool detrelsettingsuccess) = utility.FilterSettings(deviceList, "DETREL", tabname);
                    Device? detreldevice = detectreleaseSettings.FirstOrDefault();      //Detect,Releaseは共通なので最初のデータ群だけで問題なし
                    DetRelSettings? detrelSettings = detreldevice.TabSettings[tabname] as DetRelSettings ?? new DetRelSettings();
                    string? act = detrelSettings.Act;
                    float checkTime = detrelSettings.CheckTime;
                    double constValueA = 0.0;
                    double constValueB = 0.0;
                    double detrelValueA = detrelSettings.ValueA;
                    double detrelValueB = detrelSettings.ValueB;
                    bool isSpecial = act == "ActSpecial1";
                    //*********************
                    //Det/Rel動作電源を特定してdetrelDevicesとしてリスト化
                    //*********************
                    List<Device>? detrelDevices = null;
                    detrelDevices = sourceDevices.Where(d =>                                                //sourceDevicesの中で
                                                    d.TabSettings.ContainsKey(tabname) &&                   //測定項目名(tabname)が一致して
                                                    d.TabSettings[tabname] is SourceSettings settings &&    //TabSettings[tabname]がSourceSettings型で定義されていて
                                                    settings.Function == "const" &&                         //Functionが"const"になっている
                                                    (d.Identifier == detrelSettings.SourceA || d.Identifier == detrelSettings.SourceB) &&
                                                    d.Identifier != "SOURCEnull")                           //かつdetrelSettings.SourceAかdetrelSettings.SourceBにマッチする
                                                    .ToList();                                              //DeviceオブジェクトをList化しdetrelDevicesに渡す
                    if (isSpecial)
                    {
                        foreach (Device? detrelDevice in detrelDevices)
                        {
                            SourceSettings? detrelset = detrelDevice.TabSettings[tabname] as SourceSettings;
                            if(detrelDevice.Identifier == detrelSettings.SourceA)
                                constValueA = detrelset.SourceValue;
                            if(detrelDevice.Identifier == detrelSettings.SourceB)
                                constValueB = detrelset.SourceValue;
                        }
                    }
                    //*********************
                    //DMMTrig条件抽出
                    //*********************
                    Device? dmmDevice = dmmDevices.FirstOrDefault();      //DMMトリガ設定は共通なので最初のデータ群だけで問題なし
                    DmmSettings? dmmSettings = dmmDevice.TabSettings[tabname] as DmmSettings ?? new DmmSettings();
                    string dmmTrigSource = dmmSettings.TriggerSource;
                    //*********************
                    //CSV書き込みデータ更新
                    //*********************
                    List<string>? row = null;
                    row = new List<string> { tabname, "" };
                    for (int j = 1; j <= 4; j++)
                        if (deviceList.Any(d => d.Identifier == $"DMM{j}"))
                            row.Add("");
                    viRows.Add(string.Join(",", row));     //仮の行を追加
                    try
                    {
                        //*********************
                        //各測定器Initialize
                        //*********************
                        await commSOURCE.SOURCE_Initialize(sourceDevices, tabname, cancellationToken);
                        await commMEASURE.MEASURE_Initialize(dmmDevices, tabname, cancellationToken);   //初期化処理でDMM測定待機状態まで遷移
                        //*********************
                        //SourceON
                        //電源出力安定待ち時間(暫定20ms)
                        //*********************
                        await commSOURCE.SOURCE_OutputON(sourceDevices, tabname, cancellationToken);
                        await utility.Wait_Timer((int)(20), cancellationToken);
                        //*********************
                        //初期状態設定
                        //*********************
                        if (isSpecial)
                        {
                            //検出復帰で別電源を変化
                            foreach (Device detrelDevice in detrelDevices)
                            {
                                SourceSettings? detrelset = detrelDevice.TabSettings[tabname] as SourceSettings;
                                if (detrelDevice.Identifier == detrelSettings.SourceA)
                                    detrelset.SourceValue = Math.Round(detrelValueA, 8);
                                if (detrelDevice.Identifier == detrelSettings.SourceB)
                                    detrelset.SourceValue = Math.Round(detrelValueB, 8);
                            }
                            await commSOURCE.SOURCE_SetValue(detrelDevices, tabname, cancellationToken);
                        }
                        //*********************
                        //検出復帰wait
                        //*********************
                        await utility.Wait_Timer((int)(checkTime * 1000), cancellationToken);
                        if (isSpecial)
                        {
                            //検出復帰で別電源を変化させたものを元に戻す
                            foreach (Device detrelDevice in detrelDevices)
                            {
                                SourceSettings? detrelset = detrelDevice.TabSettings[tabname] as SourceSettings;
                                if (detrelDevice.Identifier == detrelSettings.SourceA)
                                    detrelset.SourceValue = constValueA;
                                if (detrelDevice.Identifier == detrelSettings.SourceB)
                                    detrelset.SourceValue = constValueB;
                            }
                            await commSOURCE.SOURCE_SetValue(detrelDevices, tabname, cancellationToken);
                        }
                        //*********************
                        //測定Stanby
                        //*********************
                        await utility.Wait_Timer((int)(standbyTime * 1000), cancellationToken);
                        //*********************
                        //測定スタート
                        //*********************
                        cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
                        StringBuilder dmmData = new StringBuilder();
                        //*********************
                        //測定待機状態に遷移している為BUSトリガ発生→測定
                        //*********************
                        if (dmmTrigSource == "BUS")
                            await commMEASURE.MEASURE_BusTrigger(dmmDevices, tabname, cancellationToken);
                        //*********************
                        //測定値取得
                        //*********************
                        if (dmmTrigSource == "BUS" || dmmTrigSource == "EXT")
                            dmmData = await commMEASURE.MEASURE_ReadData(dmmDevices, tabname, cancellationToken);
                        if (dmmTrigSource == "IMM")
                            dmmData = await commMEASURE.MEASURE_Data(dmmDevices, tabname, cancellationToken);
                        //*********************
                        //CSV書き込みデータ更新
                        //*********************
                        await UpdateCsvWithDmmData(viRows, dmmData, deviceList);
                        //*********************
                        //1条件(Tabname)終了時動作
                        //SourceOFF
                        //*********************
                        await commSOURCE.SOURCE_OutputOFF(sourceDevices, tabname, cancellationToken);
                        //*********************
                        //全測定器Remote解除
                        //*********************
                        await commSOURCE.SOURCE_RemoteOFF(sourceDevices);
                        await commMEASURE.MEASURE_RemoteOFF(dmmDevices);
                        //*********************
                        //データ格納
                        //*********************
                        tabCsvRows.AddRange(viRows);
                        resultDataRowsByTab[tabname].AddRange(viRows);
                        //*********************
                        //タブごとの中間データ最終保存
                        //*********************
                        if (tabCsvRows.Any() && tabCsvRows.Count > 1)
                            await utility.WriteCsvFileAsync(tabNameFilePath, tabCsvRows, append: false, useShiftJis: true);
                    }
                    catch (MeasFatalException ex)
                    {
                        viData.Add($"# VI動作中に致命レベルエラーが発生しました: {ex.Message}");
                        csvRows.Add($"# {string.Join(" ", viData).Replace(Environment.NewLine, " ")}");
                        //処理中のタブデータがあれば保存
                        if (tabCsvRows.Any() && tabCsvRows.Count > 1)
                        {
                            tabNameFilePath = baseTempFilePath.Replace("_VIData_", $"_ErrorVIData_{currentTabName}_");
                            await utility.WriteCsvFileAsync(tabNameFilePath, tabCsvRows, append: false, useShiftJis: true);
                        }
                        return csvRows;
                    }
                    catch (Exception ex)
                    {
                        viData.Add($"# VI動作中にエラーが発生しました: {ex.Message}");
                        csvRows.Add($"# {string.Join(" ", viData).Replace(Environment.NewLine, " ")}");
                        //処理中のタブデータがあれば保存
                        if (tabCsvRows.Any() && tabCsvRows.Count > 1)
                        {
                            tabNameFilePath = baseTempFilePath.Replace("_VIData_", $"_ErrorVIData_{currentTabName}_");
                            await utility.WriteCsvFileAsync(tabNameFilePath, tabCsvRows, append: false, useShiftJis: true);
                        }
                        //Warningで停止する場合
                        if (debugOption.StopOnWarning)
                            return csvRows;
                        return csvRows;
                    }
                }
                //*********************
                //戻り値用データ生成
                //*********************
                //csvRows.AddRange(deviceHeaderRows);
                foreach (string tab in tabNames)
                {
                    if (tab == "対象なし")
                        continue;
                    //csvRows.AddRange(measHeaderRows[tab]);
                    csvRows.AddRange(resultDataRowsByTab[tab]);
                }
                //*********************
                //最終データを戻す
                //*********************
                if (viData.Any())                                                                    //途中でエラーが発生していたら
                    csvRows.Add($"# {string.Join(" ", viData).Replace(Environment.NewLine, " ")}");  //戻り値csvRowsにエラーメッセージ追加
                return csvRows;
            }
            catch (OperationCanceledException)
            {
                viData.Add("# VI動作がキャンセルされました。");
                csvRows.Add($"# {string.Join(" ", viData).Replace(Environment.NewLine, " ")}");
                //処理中のタブデータがあれば保存
                if (tabCsvRows.Any() && tabCsvRows.Count > 1)
                {
                    string tabNameFilePath = baseTempFilePath.Replace("_VIData_", $"_AbortVIData_{currentTabName}_");
                    await utility.WriteCsvFileAsync(tabNameFilePath, tabCsvRows, append: false, useShiftJis: true);
                }
                throw;          //キャンセル要求を検知したら呼び出し元に通知
            }
            finally
            {
                //*********************
                //全測定器Remote解除
                //*********************
                if (deviceList != null)
                {
                    List<Device>? sourceDevices = deviceList.Where(d => d.Identifier.StartsWith("SOURCE", StringComparison.OrdinalIgnoreCase)).ToList();
                    List<Device>? dmmDevices = deviceList.Where(d => d.Identifier.StartsWith("DMM", StringComparison.OrdinalIgnoreCase)).ToList();
                    await commSOURCE.SOURCE_RemoteOFF(sourceDevices);
                    await commMEASURE.MEASURE_RemoteOFF(dmmDevices);

                }
            }
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし<Task>
        //機能：CSV書き込み補助
        //引数1：csvRows
        //説明：List<string> 書き込みデータ用変数
        //引数2：dmmData
        //説明：<StringBuilder> 測定データ群
        //引数3：deviceList
        //説明：List<Device> DMM設定リスト群
        //*************************************************
        private async Task UpdateCsvWithDmmData(List<string> csvRows, StringBuilder dmmData, List<Device> deviceList)
        {
            if (!csvRows.Any())
                return;

            string[] dmmValues = dmmData.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            string[] lastRow = csvRows[csvRows.Count - 1].Split(',');
            int dmmIndex = 0;
            for (int j = 1; j <= 4; j++)
            {
                if (deviceList.Any(d => d.Identifier == $"DMM{j}"))
                {
                    if (dmmIndex < dmmValues.Length)
                        lastRow[2 + dmmIndex] = dmmValues[dmmIndex].Trim();
                    dmmIndex++;
                }
            }
            csvRows[csvRows.Count - 1] = string.Join(",", lastRow);
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし<Task>
        //機能：ヘッダー作成1
        //引数1：csvRows
        //説明：List<string> 書き込みデータ用変数
        //引数2：dmmData
        //説明：List<var> 測定器リスト
        //引数3：deviceList
        //説明：List<Device> 測定器設定リスト群
        //*************************************************
        private async Task CreateInsHeaders(
            List<string> csvRows, List<(bool IsChecked, string UsbId, string InstName, string Identifier)> measInst, List<Device> deviceList)
        {
            //*********************
            //測定器ヘッダー
            //*********************
            csvRows.Add("DeviceList");
            foreach (var inst in measInst.Where(m => m.IsChecked))
            {
                var device = deviceList.FirstOrDefault(d =>
                    string.Equals(d.Identifier, inst.Identifier, StringComparison.OrdinalIgnoreCase));
                if (device == null)
                    continue;
                var row = new List<string>
                {
                    device.Identifier ?? "",
                    device.UsbId ?? "",
                    device.InstName ?? ""
                };
                csvRows.Add(string.Join(",", row));
            }
        }

        //*************************************************
        //アクセス：private
        //戻り値：なし<Task>
        //機能：ヘッダー作成
        //引数1：csvRows
        //説明：List<string> 書き込みデータ用変数
        //引数2：dmmData
        //説明：List<var> 測定器リスト
        //引数3：deviceList
        //説明：List<Device> 測定器設定リスト群
        //*************************************************
        private async Task CreateMeasHeaders(
            List<string> csvRows, 
            List<(bool IsChecked, string UsbId, string InstName, string Identifier)> meas_inst,
            string TabNamesText,
            List<Device> deviceList)
        {
            csvRows.Add("");                    //区切り行
            //*********************
            //VI条件ヘッダー
            //*********************
            csvRows.Add("VISettings");       //区切りコメント
            List<Device>? Settings = null;
            (Settings, bool settingsuccess) = utility.FilterSettings(deviceList, "VI", TabNamesText);
            Device? device = Settings.FirstOrDefault();      //VI設定は各Tab内で一つだけなので最初のデータ群だけで問題なし
            VISettings viSettings = device.TabSettings[TabNamesText] as VISettings ?? new VISettings();
            var row = new List<string>
            {
                TabNamesText,
                string.Join(",", new[]
                {
                    $"StandbyTime={viSettings.StandbyTime:F2}{viSettings.StandbyTimeUnit ?? ""}"
                })
            };
            csvRows.Add(string.Join(",", row));

            //*********************
            //Dataヘッダー
            //*********************
            csvRows.Add("Data");       //区切りコメント
            List<string> headers = new List<string> { "TabName", "VIValue" };
            List<int> dmmIndices = new List<int>();             //DMM1～4のインデックスを記録
            if (deviceList.Any(d => d.Identifier.StartsWith("DMM", StringComparison.OrdinalIgnoreCase)))
            {
                //DMM1～DMM4のヘッダーを追加
                for (int i = 1; i <= 4; i++)
                {
                    if (deviceList.Any(d => d.Identifier == $"DMM{i}"))
                    {
                        headers.Add($"DMM{i}_Value");
                        dmmIndices.Add(i);                      //DMM1, DMM3などのインデックスを保存
                    }
                }
            }
            csvRows.Add(string.Join(",", headers));                 //ヘッダーをCSV形式で追加

            //*********************
            //InstName行の追加
            //*********************
            List<string> instNameRow = new List<string>(new string[headers.Count]);     //ヘッダーと同じ列数で初期化
            //1列目: 空欄
            instNameRow[0] = "";
            instNameRow[1] = "";

            foreach (var i in dmmIndices)
            {
                (bool IsChecked, string UsbId, string InstName, string Identifier) dmmInst =
                    meas_inst.FirstOrDefault(m => m.IsChecked && m.Identifier == $"DMM{i}" && !string.IsNullOrWhiteSpace(m.InstName));
                //3列名以降DMM{i}_InstName
                instNameRow[headers.IndexOf($"DMM{i}_Value")] = dmmInst.InstName ?? "";
            }
            //InstNameが1つ以上あれば追加
            if (instNameRow.Any(s => !string.IsNullOrEmpty(s)))
            {
                csvRows.Add(string.Join(",", instNameRow));         //ヘッダーにCSV形式で追加
            }
        }
    }
}
