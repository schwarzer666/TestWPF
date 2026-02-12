using InputCheck;                   //ErrCheck.cs
using MEASUREcommunication;         //CommMEASURE.cs
using OSCcommunication;             //CommOSC.cs
using PGcommunication;              //CommPG.cs
using SOURCEcommunication;          //CommSOURCE.cs
using System.Globalization;         //NumberStyles.Float, CultureInfo.InvariantCultureを使用するのに必要
using TemperatureCharacteristics.Exceptions;
using TemperatureCharacteristics.Models;
using TemperatureCharacteristics.Models.TabData;
using UTility;                      //Utility.cs

namespace TemperatureCharacteristics.Services.Actions
{
    public class Delay : IDeviceCombinable<DelayTabData>
    {
        private static Delay? instance;             //インスタンスをnull許容型(xxx?)として宣言し初期値がnullでも問題ないと表示
        private readonly SOURCEcomm commSOURCE;     //フィールド変数commSOURCE
        private readonly OSCcomm commOSC;           //フィールド変数commOSC
        private readonly MEASUREcomm commMEASURE;   //フィールド変数commMEASURE
        private readonly PGcomm commPG;             //フィールド変数commPG
        private readonly UT utility;                //フィールド変数utility
        private readonly InpCheck errCheck;         //フィールド変数errCheck

        private Delay()
        {
            //初期化する変数
            utility = UT.Instance;                  //インスタンス生成(=初期化)を別クラス内で実行
            commSOURCE = SOURCEcomm.Instance;       //インスタンス生成(=初期化)を別クラス内で実行
            commOSC = OSCcomm.Instance;             //インスタンス生成(=初期化)を別クラス内で実行
            commMEASURE = MEASUREcomm.Instance;     //インスタンス生成(=初期化)を別クラス内で実行
            commPG = PGcomm.Instance;               //インスタンス生成(=初期化)を別クラス内で実行
            errCheck = InpCheck.Instance;           //インスタンス生成(=初期化)を別クラス内で実行
        }
        public static Delay Instance     //外部からのアクセス用
        {
            get
            {
                if (instance == null)
                    instance = new Delay(); // クラス内でインスタンスを生成
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
                                        IEnumerable<DelayTabData> tabData)
        {
            if (tabData == null || !tabData.Any())
                return new List<Device>();
            //*************************************
            //checkboxがチェックされている測定器の
            //識別・USBアドレス・信号名をリスト化
            //+ delaySettings用のidentifier追加
            //+ Detect/Release用のidentifier追加
            //*************************************
            var activeDevices = utility.GetActiveUSBAddr(meas_inst);
            if (!activeDevices.Any())
                return new List<Device>();          //測定器のチェックボックスに一つもチェックが入っていない場合CombineDeviceDataを抜ける
            activeDevices.Add(("DELAY","",""));
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
                //SOURCE1-4設定
                //*************************************
                if (deviceDict.ContainsKey("SOURCE1") && tab.Source1set != null)
                {
                    deviceDict["SOURCE1"].TabSettings[tab.TabName] = new SourceSettings
                    {
                        SourceAct = tab.Source1set[0],                                                      //ACT:"Sweep","Constant1"
                        Function = tab.Source1set[1],                                                       //Func:"sweep","const"
                        Mode = tab.Source1set[2],                                                           //MODE:"VOLT","CURR"
                        SourceValue = GetSourceValue(tab.Source1set[0], tab.Constset),                      //電源設定値 Act="Constant1-3"でSourceValue場合分け
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
                        SourceValue = GetSourceValue(tab.Source2set[0], tab.Constset),                      //電源設定値 Act="Constant1-3"でSourceValue場合分け
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
                        SourceValue = GetSourceValue(tab.Source3set[0], tab.Constset),                      //電源設定値 Act="Constant1-3"でSourceValue場合分け
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
                        SourceValue = GetSourceValue(tab.Source4set[0], tab.Constset),                      //電源設定値 Act="Constant1-3"でSourceValue場合分け
                        SourceRange = utility.GetRangeString(tab.Source4set[3], tab.Source4set[4]),         //コマンド送信用に電源設定レンジを数値文字列に変換(100mV→100E-3)
                        RangeValue = tab.Source4set[3],                                                     //電源設定レンジ("AUTO","2V")
                        SourceRangeUnit = GetSourceRangeUnit(tab.Source4set[0], tab.Constset),              //Const設定時の電源設定値単位("V","mA")
                        SourceLimit = utility.String2Double_Conv(tab.Source4set[5], tab.Source4set[6]),     //電源Limit設定値
                        SourceLimitUnit = tab.Source4set[7]                                                 //電源Limit設定レンジ
                    };
                }
                //*************************************
                //OSC設定 + Delay条件
                //*************************************
                if (deviceDict.ContainsKey("OSC") && tab.OSCset != null)
                {
                    deviceDict["OSC"].TabSettings[tab.TabName] = new OscSettings
                    {
                        ChannelSettings = new[] {
                            utility.GetRangeString(tab.OSCset[0], tab.OSCset[1]), tab.OSCset[2],        //CH1:Range文字列,POS
                            utility.GetRangeString(tab.OSCset[3], tab.OSCset[4]), tab.OSCset[5],        //CH2:Range文字列,POS
                            utility.GetRangeString(tab.OSCset[6], tab.OSCset[7]), tab.OSCset[8],        //CH3:Range文字列,POS
                            utility.GetRangeString(tab.OSCset[9], tab.OSCset[10]), tab.OSCset[11],      //CH4:Range文字列,POS
                        },
                        TriggerSource = tab.OSCset[12],                                                 //TrigSource:"1","EXT"
                        TriggerDirection = tab.OSCset[13],                                              //TrigDirection:"RISE","FALL"
                        TriggerLevel = utility.GetRangeString(tab.OSCset[14], tab.OSCset[15]),          //コマンド送信用にTrigLevelを数値文字列に変換(100mV→100E-3)
                        TimeSettings = new[]{
                            utility.GetRangeString(tab.OSCset[16],tab.OSCset[17]),tab.OSCset[18]        //時間軸,Horizontal pos
                        },
                        DelaySetupCh = tab.Delayset[0],                                                 //Delay測定対象CH
                        Polarity = tab.Delayset[1],                                                     //Delay測定対象CH極性
                        RefCh = tab.OSCset[12],                                                         //Delay測定用RefCH
                        TRange1 = GetTRange(tab.OSCset[18]),                                            //Measure測定範囲1
                        TRange2 = 5.0f                                                                  //Measure測定範囲2
                    };
                }
                //*************************************
                //PulseGenerator設定
                //*************************************
                if (deviceDict.ContainsKey("PULSE") && tab.PGset != null)
                {
                    deviceDict["PULSE"].TabSettings[tab.TabName] = new PGSettings
                    {
                        PGCheck = true,
                        Function = "pulse",                                                             //電源functionに合わせて小文字
                        LowLevelValue = utility.String2Double_Conv(tab.PGset[1], tab.PGset[2]),
                        HighLevelValue = utility.String2Double_Conv(tab.PGset[3], tab.PGset[4]),
                        OutputCH = tab.PGset[0],
                        Polarity = tab.PGset[5],
                        PeriodValue = utility.String2Double_Conv(tab.PGset[6], tab.PGset[7]),
                        WidthValue = utility.String2Double_Conv(tab.PGset[8], tab.PGset[9]),
                        OutputZ = tab.PGset[10],
                        TrigOut = tab.PGset[11]
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
        // "Constant1"-"Constant3"
        //引数2：constSet
        //説明：<string> constant電源設定群
        // Constset[0]:constant電源1 value
        // Constset[1]:constant電源1 unit
        // Constset[2]:constant電源2 value
        // Constset[3]:constant電源2 unit
        // Constset[4]:constant電源3 value
        // Constset[5]:constant電源3 unit
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
        // "Constant1"-"Constant3"
        //引数2：constSet
        //説明：<string> constant電源設定群
        // Constset[6]:constant電源1 RangeUnit
        // Constset[7]:constant電源2 RangeUnit
        // Constset[8]:constant電源3 RangeUnit
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

            return rangeUnit;
        }

        //*************************************************
        //アクセス：private
        //戻り値：<float> TRange
        //機能：OSC Horizontal posをDelay測定用にTRangeに変換
        //引数1：Tpos
        //説明：<string> OSC Horizontal postion
        //*************************************************
        private float GetTRange(string Tpos)
        {
            if (string.IsNullOrEmpty(Tpos)) return 0.0f;
            float TRange = (float.Parse(Tpos) - 50.0f) / 10.0f;
            return TRange;
        }

        //*************************************************
        //アクセス：public
        //戻り値：<string> Delay結果
        //機能：Delay動作
        //引数1：setting
        //説明：<var> 測定器設定＋delay条件
        // 電源設定
        // constant電源設定
        // osc設定
        // 検出復帰設定
        // delay条件
        //コメント
        //*************************************************
        public async Task<List<string>> DELAYAction(
                                            List<(bool IsChecked, string UsbId, string InstName, string Identifier)> meas_inst,
                                            IEnumerable<DelayTabData> tabData,
                                            string[] tabNames,
                                            DebugOption debugOption,
                                            CancellationToken cancellationToken = default,
                                            List<Device>? preCombinedDevices = null)
        {
            cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
            //*********************
            //定義
            //*********************
            List<string> delayData = new List<string>();                            //エラー発生時の入力用変数
            List<string> csvRows = new List<string>();                              //戻り値（測定データ） エラー発生時には#を付けたコメントが入る
            List<string> delayRows = new List<string>();                            //delay測定データ保持用
            List<string> tabCsvRows = new List<string>();                           //各タブ用のデータ（中間データ）を初期化
            Dictionary<string, List<string>> resultDataRowsByTab = new Dictionary<string, List<string>>();
                                                                                    //delay結果データを辞書形式で定義
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
                //deviceList = preCombinedDevices ?? CombineDeviceData(meas_inst, delayTab.GetMeasureOnTabData);
                deviceList = preCombinedDevices ?? CombineDeviceData(meas_inst, tabData);
                if (!deviceList.Any())
                {
                    delayData.Add("チェックされた測定器無し");
                    csvRows.Add($"# {string.Join(" ", delayData).Replace(Environment.NewLine, " ")}");
                    return csvRows;
                }
                //*********************
                //測定Tab名を取得
                //*********************
                //string TabName = delayTab.CheckedTabNamesText.Trim();
                ////タブ名をカンマで分割＋文字列前後の空白を削除
                //string[] tabNames = TabName?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                //                            ?.Select(t => t.Trim())
                //                            ?.ToArray() ?? Array.Empty<string>();
                if(tabNames.Length == 0 || tabNames[0] == "対象なし")
                {
                    delayData.Add("測定対象選択無し");
                    csvRows.Add($"# {string.Join(" ", delayData).Replace(Environment.NewLine, " ")}");
                    return csvRows;
                }
                //*********************
                //入力チェック
                //*********************
                (delayData, bool isVerify) = await errCheck.VerifyDelayTabInputs(deviceList, tabNames);
                if (!isVerify)
                {
                    delayData.Add("以下の箇所で入力に不備があります");
                    csvRows.Add($"# {string.Join(" ", delayData).Replace(Environment.NewLine, " ")}");
                    return delayData;
                }
                //*********************
                //測定器ヘッダーの生成
                //*********************
                await CreateInsHeaders(deviceHeaderRows, meas_inst, deviceList);
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
                    delayRows.Clear();                              //遅延時間測定結果をクリア
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
                    List<Device>? oscDevices = null;        //初期化
                    List<Device>? pulseDevices = null;      //初期化
                    (sourceDevices, bool success) = utility.FilterSettings(deviceList, "SOURCE", tabname);
                    (oscDevices, bool oscsuccess) = utility.FilterSettings(deviceList, "OSC", tabname);
                    (pulseDevices, bool pulsesuccess) = utility.FilterSettings(deviceList, "PULSE", tabname);
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
                    //Wait時間抽出
                    //*********************
                    Device? oscDevice = oscDevices.FirstOrDefault();      //Detect,Releaseは共通なので最初のデータ群だけで問題なし
                    OscSettings oscSettings = oscDevice.TabSettings[tabname] as OscSettings ?? new OscSettings();
                    string[]? oscTime = oscSettings.TimeSettings;
                    string? oscTrange = oscTime[0];
                    string oscTpos = oscTime[1];
                    float measureWaitTime = (float.Parse(oscTrange) * ((100.0f - float.Parse(oscTpos)) / 100)) * 10.0f; //(oscTrange(1division) * oscTpos(%))*10division
                    try
                    {
                        //*********************
                        //各測定器Initialize
                        //*********************
                        await commSOURCE.SOURCE_Initialize(sourceDevices, tabname, cancellationToken);
                        await commOSC.OSC_Initialize(oscDevices, tabname, cancellationToken);
                        if (debugOption.Use8chOSC)
                            await commOSC.OSCUnusedChOFF(oscDevices, tabname, cancellationToken);
                        await commOSC.OSCmeasureSet(oscDevices, tabname, cancellationToken);
                        await commPG.PG_Initialize(pulseDevices, tabname, cancellationToken);
                        //*********************
                        //SourceON,PulseGeneratorON
                        //電源出力安定待ち時間(暫定20ms)
                        //*********************
                        await commSOURCE.SOURCE_OutputON(sourceDevices, tabname, cancellationToken);
                        await commPG.PG_OutputON(pulseDevices, tabname, cancellationToken);
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
                        //OscSingleRun
                        //*********************
                        await commOSC.OSCsingleRUN(oscDevices, tabname, cancellationToken);
                        //*********************
                        //PulseGenerator CH表示切替(Initialと同時にするとタイミングの関係上切り替わらない
                        //*********************
                        await commPG.PG_DispCH(pulseDevices, tabname, cancellationToken);
                        //*********************
                        //PulseGenerator Trig発生
                        //*********************
                        await commPG.PG_BusTrigger(pulseDevices, tabname, cancellationToken);   //問題あればCHトリガ
                        //*********************
                        //検出復帰遅延時間wait
                        //*********************
                        await utility.Wait_Timer((int)(measureWaitTime * 1000), cancellationToken);
                        //*********************
                        //OSC Delay測定
                        //*********************
                        string delayResult = await commOSC.OSCmeasureDelay(oscDevices, tabname, cancellationToken);
                        //*********************
                        //CSV書き込みデータ更新
                        //*********************
                        delayRows.Add(string.Join(",", tabname, delayResult));  //区切り文字カンマを使用して各要素を追加
                        //*********************
                        //1条件(Tabname)終了時動作
                        //SourceOFF,PulseGeneratorOFF
                        //*********************
                        await commSOURCE.SOURCE_OutputOFF(sourceDevices, tabname, cancellationToken);
                        await commPG.PG_OutputOFF(pulseDevices, tabname, cancellationToken);
                        //*********************
                        //全測定器Remote解除
                        //*********************
                        await commSOURCE.SOURCE_RemoteOFF(sourceDevices);
                        await commOSC.OSC_RemoteOFF(oscDevices);
                        await commPG.PG_RemoteOFF(pulseDevices);
                        //*********************
                        //データ格納
                        //*********************
                        tabCsvRows.AddRange(delayRows);
                        resultDataRowsByTab[tabname].AddRange(delayRows);
                        //*********************
                        //タブごとの中間データ最終保存
                        //*********************
                        if (tabCsvRows.Any() && tabCsvRows.Count > 1)
                            await utility.WriteCsvFileAsync(tabNameFilePath, tabCsvRows, append: false, useShiftJis: true);
                    }
                    catch (MeasFatalException ex)
                    {
                        delayData.Add($"# Delay動作中に致命レベルエラーが発生しました: {ex.Message}");
                        csvRows.Add($"# {string.Join(" ", delayData).Replace(Environment.NewLine, " ")}");
                        //処理中のタブデータがあれば保存
                        if (tabCsvRows.Any() && tabCsvRows.Count > 1)
                        {
                            tabNameFilePath = baseTempFilePath.Replace("_DelayData_", $"_ErrorDelayData_{currentTabName}_");
                            await utility.WriteCsvFileAsync(tabNameFilePath, tabCsvRows, append: false, useShiftJis: true);
                        }
                        return csvRows;
                    }
                    catch (Exception ex)
                    {
                        delayData.Add($"# Delay動作中にエラーが発生しました: {ex.Message}");
                        csvRows.Add($"# {string.Join(" ", delayData).Replace(Environment.NewLine, " ")}");
                        //処理中のタブデータがあれば保存
                        if (tabCsvRows.Any() && tabCsvRows.Count > 1)
                        {
                            tabNameFilePath = baseTempFilePath.Replace("_DelayData_", $"_ErrorDelayData_{currentTabName}_");
                            await utility.WriteCsvFileAsync(tabNameFilePath, tabCsvRows, append: false, useShiftJis: true);
                        }
                        //Warningで停止する場合
                        if (debugOption.StopOnWarning)
                            return csvRows;
                        continue;
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
                if (delayData.Any())                                                                    //途中でエラーが発生していたら
                    csvRows.Add($"# {string.Join(" ", delayData).Replace(Environment.NewLine, " ")}");  //戻り値csvRowsにエラーメッセージ追加
                return csvRows;
            }
            catch (OperationCanceledException)
            {
                delayData.Add("# Delay動作がキャンセルされました。");
                csvRows.Add($"# {string.Join(" ", delayData).Replace(Environment.NewLine, " ")}");
                //処理中のタブデータがあれば保存
                if (tabCsvRows.Any() && tabCsvRows.Count > 1)
                {
                    string tabNameFilePath = baseTempFilePath.Replace("_DelayData_", $"_AbortDelayData_{currentTabName}_");
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
                    List<Device>? oscDevices = deviceList.Where(d => d.Identifier.Equals("OSC", StringComparison.OrdinalIgnoreCase)).ToList();
                    List<Device>? pulseDevices = deviceList.Where(d => d.Identifier.Equals("PULSE", StringComparison.OrdinalIgnoreCase)).ToList();

                    await commSOURCE.SOURCE_RemoteOFF(sourceDevices);
                    await commOSC.OSC_RemoteOFF(oscDevices);
                    await commPG.PG_RemoteOFF(pulseDevices);
                }
            }
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
            //Delay条件ヘッダー
            //*********************
            csvRows.Add("DelaySettings");       //区切りコメント
            List<Device>? Settings = null;
            (Settings, bool settingsuccess) = utility.FilterSettings(deviceList, "OSC", TabNamesText);
            Device? device = Settings.FirstOrDefault();      //最初のデータ群のみ
            OscSettings delaySettings = device.TabSettings[TabNamesText] as OscSettings ?? new OscSettings();
            var row = new List<string>
            {
                TabNamesText,
                string.Join(",", new[]
                {
                    $"RefCH={delaySettings.RefCh}",
                    $"DelayCH={delaySettings.DelaySetupCh}",
                    $"Polarity={delaySettings.Polarity}",
                    $"MeasureTimeRange={delaySettings.TRange1} {delaySettings.TRange2}"
                })
            };
            csvRows.Add(string.Join(",", row));

            //*********************
            //Dataヘッダー
            //*********************
            csvRows.Add("Data");       //区切りコメント
            List<string> headers = new List<string> { "TabName", "DelayValue" };
            csvRows.Add(string.Join(",", headers));                 //ヘッダーをCSV形式で追加
            //*********************
            //InstName行の追加
            //*********************
            List<string> instNameRow = new List<string>(new string[headers.Count]);     //ヘッダーと同じ列数で初期化
            //1列目: 空欄
            instNameRow[0] = "";
            instNameRow[1] = "";
        }
    }
}
