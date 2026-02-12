using InputCheck;                   //ErrCheck.cs
using MEASUREcommunication;         //CommMEASURE.cs
using OSCcommunication;             //CommOSC.cs
using PGcommunication;              //CommPG.cs
using SOURCEcommunication;          //CommSOURCE.cs
using System.Globalization;         //NumberStyles.Float, CultureInfo.InvariantCultureを使用するのに必要
using System.Text;
using TemperatureCharacteristics.Exceptions;
using TemperatureCharacteristics.Models;
using TemperatureCharacteristics.Models.TabData;
using UTility;                      //Utility.cs

namespace TemperatureCharacteristics.Services.Actions
{
    public class Sweep : IDeviceCombinable<SweepTabData>
    {
        private static Sweep? instance;             //インスタンスをnull許容型(xxx?)として宣言し初期値がnullでも問題ないと表示
        private readonly SOURCEcomm commSOURCE;     //フィールド変数commSOURCE
        private readonly OSCcomm commOSC;           //フィールド変数commOSC
        private readonly MEASUREcomm commMEASURE;   //フィールド変数commMEASURE
        private readonly PGcomm commPG;             //フィールド変数commPG
        private readonly UT utility;                //フィールド変数utility
        private readonly InpCheck errCheck;         //フィールド変数errCheck

        private Sweep()
        {
            //初期化する変数
            utility = UT.Instance;                  //インスタンス生成(=初期化)を別クラス内で実行
            commSOURCE = SOURCEcomm.Instance;       //インスタンス生成(=初期化)を別クラス内で実行
            commOSC = OSCcomm.Instance;             //インスタンス生成(=初期化)を別クラス内で実行
            commMEASURE = MEASUREcomm.Instance;     //インスタンス生成(=初期化)を別クラス内で実行
            commPG = PGcomm.Instance;               //インスタンス生成(=初期化)を別クラス内で実行
            errCheck = InpCheck.Instance;           //インスタンス生成(=初期化)を別クラス内で実行
        }
        public static Sweep Instance     //外部からのアクセス用
        {
            get
            {
                if (instance == null)
                    instance = new Sweep(); // クラス内でインスタンスを生成
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
                                        IEnumerable<SweepTabData> tabData)
        {
            if (tabData == null || !tabData.Any())
                return new List<Device>();
            //*************************************
            //checkboxがチェックされている測定器の
            //識別・USBアドレス・信号名をリスト化
            //+ sweepSettings用のidentifier追加
            //+ Detect/Release用のidentifier追加
            //*************************************
            var activeDevices = utility.GetActiveUSBAddr(meas_inst);
            if (!activeDevices.Any())
                return new List<Device>();          //測定器のチェックボックスに一つもチェックが入っていない場合CombineDeviceDataを抜ける
            activeDevices.Add(("SWEEP","",""));
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
                //Sweep条件
                //*************************************
                if (tab.Sweepset != null && tab.Sweepset.Any())
                {
                    SweepSettings sweepSettings = GetSweepSet(tab.NormalSweepCheck, tab.Sweepset, tab.TabName);
                    deviceDict["SWEEP"].TabSettings[tab.TabName] = sweepSettings;
                }
                //*************************************
                //Detect/Release条件
                //*************************************
                if (tab.Detrelact != null && tab.Detrelact.Any())
                {
                    deviceDict["DETREL"].TabSettings[tab.TabName] = new DetRelSettings
                    {
                        Act = tab.Detrelact,                                                                //ACT:"ActNormal","ActSpecial1"
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
                        SourceValue = GetSourceValue(tab.Source1set[0], tab.Sweepset, tab.Constset),        //電源設定値 Act="Sweep","Constant1-4"でSourceValue場合分け
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
                        SourceValue = GetSourceValue(tab.Source2set[0], tab.Sweepset, tab.Constset),        //電源設定値 Act="Sweep","Constant1-4"でSourceValue場合分け
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
                        SourceValue = GetSourceValue(tab.Source3set[0], tab.Sweepset, tab.Constset),        //電源設定値 Act="Sweep","Constant1-4"でSourceValue場合分け
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
                        SourceValue = GetSourceValue(tab.Source4set[0], tab.Sweepset, tab.Constset),        //電源設定値 Act="Sweep","Constant1-4"でSourceValue場合分け
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
                //*************************************
                //OSC
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
                            utility.GetRangeString("50us","range3"),"50.0"                              //sweepTabでは時間軸固定 50us/div,50.0%
                        },
                    };
                }
                //*************************************
                //PulseGenerator
                //*************************************
                if (deviceDict.ContainsKey("PULSE") && tab.PGset != null)
                {
                    deviceDict["PULSE"].TabSettings[tab.TabName] = new PGSettings
                    {
                        PGCheck = tab.PulseGenUseCheck,
                        Function = "pulse",                                                             //電源functionに合わせて小文字
                        LowLevelValue = utility.String2Double_Conv(tab.PGset[1], tab.PGset[2]),
                        HighLevelValue = utility.String2Double_Conv(tab.PGset[3], tab.PGset[4]),
                        OutputCH = tab.PGset[0],
                        Polarity = tab.PGset[5],
                        PeriodValue = utility.String2Double_Conv(tab.PGset[6], tab.PGset[7]),
                        WidthValue = utility.String2Double_Conv(tab.PGset[8], tab.PGset[9]),
                        OutputZ = tab.PGset[10],
                        TrigOut = "ON"
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
        //引数2：sweepSet
        //説明：<string> sweep電源設定群
        // Sweepset[0]:minValue
        // Sweepset[1]:minValue unit
        // Sweepset[2]:maxValue
        // Sweepset[3]:maxValue unit
        // Sweepset[4]:Directional
        // Sweepset[5]以降当メソッドでは未使用
        //引数3：constSet
        //説明：<string> constant電源設定群
        // Constset[0]:constant電源1 value
        // Constset[1]:constant電源1 unit
        // Constset[2]:constant電源2 value
        // Constset[3]:constant電源2 unit
        // Constset[4]:constant電源3 value
        // Constset[5]:constant電源3 unit
        //*************************************************
        private double GetSourceValue(string sourceAct, string[] sweepSet, string[] constSet)
        {
            if (string.IsNullOrEmpty(sourceAct)) return 0.0f;

            string value = "";
            string unit = "";
            if (sourceAct == "Sweep")
            {
                //rise or risefall→minValue(Sweepset[0])+単位(Sweepset[1])
                //else→maxValue(Sweepset[2])+単位(Sweepset[3])
                value = sweepSet[4] == "rise" || sweepSet[4] == "risefall" ? sweepSet[0] : sweepSet[2];
                unit = sweepSet[4] == "rise" || sweepSet[4] == "risefall" ? sweepSet[1] : sweepSet[3];
            }
            else if (sourceAct == "Constant1")
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
            else if(sourceAct == "NotUsed")
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
        //戻り値：List<SweepSettings> Sweep設定群
        //機能：Sweep設定抜き出し
        //引数1：sweepSet
        //説明：<string> sweep電源設定群
        // Sweepset[0]:minValue
        // Sweepset[1]:minValue unit (range)
        // Sweepset[2]:maxValue
        // Sweepset[3]:maxValue unit (range)
        // Sweepset[4]:Directional
        // Sweepset[5]:StepTime
        // Sweepset[6]:StepTimeUnit (range)
        // Sweepset[7]:StepValue
        // Sweepset[8]:StepValueUnit (range)
        // Sweepset[9]:StanbyTime
        // Sweepset[10]:StanbyTimeUnit
        // Sweepset[11]:minValue unit (string)
        // Sweepset[12]:maxValue unit (string)
        // Sweepset[13]:stepValue unit (string)
        //*************************************************
        private SweepSettings GetSweepSet(bool normalSweep, string[] sweepSet, string tabName)
        {
            string minV = sweepSet[0];
            string minVunit = sweepSet[1];
            string maxV = sweepSet[2];
            string maxVunit = sweepSet[3];
            string stepT = sweepSet[5];
            string stepTunit = sweepSet[6];
            string stepV = sweepSet[7];
            string stepVunit = sweepSet[8];
            string standbyT = sweepSet[9];
            string standbyTunit = sweepSet[10];
            string minVunitString = sweepSet[11];
            string maxVunitString = sweepSet[12];
            string stepVunitString = sweepSet[13];

            SweepSettings Settings = new SweepSettings
            {
                Normalsweep = normalSweep,
                MinValue = utility.String2Double_Conv(minV, minVunit),
                MaxValue = utility.String2Double_Conv(maxV, maxVunit),
                StepTime = utility.String2Float_Conv(stepT, stepTunit),
                StepValue = utility.String2Double_Conv(stepV, stepVunit),
                StandbyTime = utility.String2Float_Conv(standbyT, standbyTunit),
                MinValueUnit = minVunitString,
                MaxValueUnit = maxVunitString,
                StepValueUnit = stepVunitString,
                Directional = sweepSet[4],
            };
            return Settings;
        }

        //*************************************************
        //アクセス：public
        //戻り値：<string> Sweep結果
        //機能：Sweep動作
        //　　　（normal sweep時）
        //　　　DMMにチェックが入っていれば各DMMの結果を返す
        //引数1：setting
        //説明：<var> 測定器設定＋sweep条件
        // sweep電源設定
        // constant電源設定
        // osc設定
        // dmm設定
        // 検出復帰設定
        // sweep条件
        //コメント
        //*************************************************
        public async Task<List<string>> SWEEPAction(
                                            List<(bool IsChecked, string UsbId, string InstName, string Identifier)> meas_inst,
                                            IEnumerable<SweepTabData> tabData,
                                            string[] tabNames,
                                            DebugOption debugOption,
                                            CancellationToken cancellationToken = default,
                                            List<Device>? preCombinedDevices = null)
        {
            cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
            //*********************
            //定義
            //*********************
            List<string> sweepData = new List<string>();                            //エラー発生時の入力用変数
            List<string> csvRows = new List<string>();                              //戻り値（測定データ） エラー発生時には#を付けたコメントが入る
            List<string> tabCsvRows = new List<string>();                           //各タブ用のデータ（中間データ）を初期化
            List<string> sweepRows = new List<string>();                            //normalsweepデータ保持用
            List<string> triggerRows = new List<string>();                          //検出復帰の全データ保持用
            List<string> finalTriggerRows = new List<string>();                     //検出復帰の最終桁データ保持用
            Dictionary<string, List<string>> resultDataRowsByTab = new Dictionary<string, List<string>>();
                                                                                    //sweep結果データを辞書形式で定義
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
            List<string> deviceHeaderRows = new List<string>();                     //中間データ用測定器ヘッダー
            Dictionary<string, List<string>> measHeaderRows = new Dictionary<string, List<string>>();       //タブごとの測定条件ヘッダーを辞書形式で定義

            try
            {
                //*********************
                //チェックされているUSBアドレスと
                //チェックされているTabから各測定器設定を取得し紐づけてリスト化
                //すでに紐づけている場合はそのまま（preCombinedDevices
                //*********************
                //deviceList = preCombinedDevices ?? CombineDeviceData(meas_inst, sweepTab.GetMeasureOnTabData);
                deviceList = preCombinedDevices ?? CombineDeviceData(meas_inst, tabData);
                if (!deviceList.Any())
                {
                    sweepData.Add("チェックされた測定器無し");
                    csvRows.Add($"# {string.Join(" ", sweepData).Replace(Environment.NewLine, " ")}");
                    return csvRows;
                }
                //*********************
                //測定Tab名を取得
                //*********************
                //string TabName = sweepTab.CheckedTabNamesText.Trim();
                ////タブ名をカンマで分割＋文字列前後の空白を削除
                //string[] tabNames = TabName?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                //                            ?.Select(t => t.Trim())
                //                            ?.ToArray() ?? Array.Empty<string>();
                if(tabNames.Length == 0 || tabNames[0] == "対象なし")
                {
                    sweepData.Add("測定対象選択無し");
                    csvRows.Add($"# {string.Join(" ", sweepData).Replace(Environment.NewLine, " ")}");
                    return csvRows;
                }
                //*********************
                //入力チェック
                //*********************
                (sweepData, bool isVerify) = await errCheck.VerifySweepTabInputs(deviceList, tabNames);
                if (!isVerify)
                {
                    sweepData.Add("以下の箇所で入力に不備があります");
                    csvRows.Add($"# {string.Join(" ", sweepData).Replace(Environment.NewLine, " ")}");
                    return sweepData;
                }
                //*********************
                //normalsweep以外(検出復帰測定)でrisefall/fallriseを使っている場合
                //rise/fallのみ実行する注意メッセージ表示(OK,NG)
                //コールバックする？
                //*********************
                //(sweepData, isVerify) = await errCheck.SweepDirectional(deviceList, tabNames);
                //if (!isVerify)
                //{
                //    bool confirmed = await confirmCallback();
                //    sweepData.Clear();
                //    if (!confirmed)
                //    {
                //        sweepData.Add("測定がユーザーによりキャンセルされました。");
                //        csvRows.Add($"# {string.Join(" ", sweepData).Replace(Environment.NewLine, " ")}");
                //        throw new OperationCanceledException("ユーザーによるキャンセル");
                //    }
                //}
                //*********************
                //中間データ用測定器ヘッダーの生成
                //*********************
                await CreateInsHeaders(deviceHeaderRows, meas_inst, deviceList);
                //*********************
                //測定項目名(tabname)毎に測定条件と測定結果ヘッダーを生成しリスト化
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
                    sweepRows.Clear();                              //normalsweepデータ保持用sweepRowsをクリア
                    triggerRows.Clear();                            //検出復帰の全データ保持用triggerRowsをクリア
                    finalTriggerRows.Clear();                       //検出復帰の最終桁データ保持用finalTriggerRowsをクリア
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
                    //対象測定器チェック
                    //*********************
                    bool enOsc = deviceList.Any(d => d.Identifier.Equals("OSC", StringComparison.OrdinalIgnoreCase));       //一致検索 OrdinalIgnoreCaseで大文字小文字無視
                    bool enDmm = deviceList.Any(d => d.Identifier.StartsWith("DMM", StringComparison.OrdinalIgnoreCase));   //前方一致 OrdinalIgnoreCaseで大文字小文字無視
                    bool enPg = deviceList.Any(d => d.Identifier.Equals("PULSE", StringComparison.OrdinalIgnoreCase));   //一致検索 OrdinalIgnoreCaseで大文字小文字無視
                    //*********************
                    //各測定器設定抽出
                    //*********************
                    List<Device>? sourceDevices = null;     //初期化
                    List<Device>? oscDevices = null;        //初期化
                    List<Device>? dmmDevices = null;        //初期化
                    List<Device>? pulseDevices = null;      //初期化
                    (sourceDevices, bool success) = utility.FilterSettings(deviceList, "SOURCE", tabname);
                    if (enOsc)
                        (oscDevices, bool oscsuccess) = utility.FilterSettings(deviceList, "OSC", tabname);
                    if (enDmm)
                        (dmmDevices, bool dmmsuccess) = utility.FilterSettings(deviceList, "DMM", tabname);
                    if (enPg)
                        (pulseDevices, bool pulsesuccess) = utility.FilterSettings(deviceList, "PULSE", tabname);
                    //*********************
                    //Sweep設定抽出
                    //*********************
                    List<Device>? Settings = null;
                    (Settings, bool settingsuccess) = utility.FilterSettings(deviceList, "SWEEP", tabname);
                    Device? device = Settings.FirstOrDefault();      //Sweep設定はsweep電源で共通なので最初のデータ群だけで問題なし
                    SweepSettings? sweepSettings = device.TabSettings[tabname] as SweepSettings ?? new SweepSettings();
                    bool normalSweep = sweepSettings.Normalsweep;           //normalSweep check
                    double minValue = sweepSettings.MinValue;               //sweep最小値
                    double maxValue = sweepSettings.MaxValue;               //sweep最大値
                    double stepValue = sweepSettings.StepValue;             //sweep step値(normalSweep時のみ使用
                    string? directional = sweepSettings.Directional;        //Sweep方向
                    string? sweepDirectional = directional.Substring(0, 4); //Sweep方向の先頭4文字抜き出し
                    float standbyTime = sweepSettings.StandbyTime;          //sweep standby値(normalSweep時のみ使用
                    float stepTime = sweepSettings.StepTime;                //sweep step時間

                    long minValue_n = (long)(minValue * 1_000_000_000);     //100nVまで扱うためminValueを整数型に変換
                    long maxValue_n = (long)(maxValue * 1_000_000_000);     //100nVまで扱うためmaxValueを整数型に変換
                    long stepValue_n = (long)(stepValue * 1_000_000_000);   //100nVまで扱うためstepValueを整数型に変換(normalSweep時のみ使用
                    int loopCount = (int)((maxValue_n - minValue_n) / stepValue_n); //sweep loop回数(normalSweep時のみ使用
                    //*********************
                    //PGCheckがONの場合
                    //Sweep設定の一部を更新
                    //*********************
                    bool enPulse = false;
                    if (enPg)
                    {
                        Device? pulseDevice = pulseDevices.FirstOrDefault();    //Pulse設定は共通なので最初のデータ群だけで問題なし
                        PGSettings? pgSettings = pulseDevice.TabSettings[tabname] as PGSettings ?? new PGSettings();
                        enPulse = pgSettings.PGCheck;
                    }
                    if (enPulse)
                    {
                        Device? pulseDevice = pulseDevices.FirstOrDefault();      //Pulse設定は共通なので最初のデータ群だけで問題なし
                        PGSettings? pgSettings = pulseDevice.TabSettings[tabname] as PGSettings ?? new PGSettings();
                        minValue = pgSettings.LowLevelValue;
                        maxValue = pgSettings.HighLevelValue;
                        string? polarity = pgSettings.Polarity;
                        sweepDirectional = polarity == "NORM" ? "rise" : "fall";    //polarity == "NORM"の時sweepDirectional=="rise"、それ以外の時sweepDirectional=="fall"
                        minValue_n = (long)(minValue * 1_000_000_000);     //100nVまで扱うためminValueを整数型に変換
                        maxValue_n = (long)(maxValue * 1_000_000_000);     //100nVまで扱うためmaxValueを整数型に変換
                    }
                    //*********************
                    //Sweep動作電源を特定してsweepDevicesとしてリスト化
                    //*********************
                    List<Device>? sweepDevices = null;
                    sweepDevices = sourceDevices.Where(d =>                                                 //sourceDevicesの中で
                                                    d.TabSettings.ContainsKey(tabname) &&                   //測定項目名(tabname)が一致して
                                                    d.TabSettings[tabname] is SourceSettings settings &&    //TabSettings[tabname]がSourceSettings型で定義されていて
                                                    settings.Function == "sweep")                           //Functionが"sweep"になっている
                                                    .ToList();                                              //DeviceオブジェクトをList化しsweepDevicesに渡す
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
                    string? dmmTrigSource = string.Empty;
                    if (enDmm)
                    {
                        Device? dmmDevice = dmmDevices.FirstOrDefault();      //DMMトリガ設定は共通なので最初のデータ群だけで問題なし
                        DmmSettings? dmmSettings = dmmDevice.TabSettings[tabname] as DmmSettings ?? new DmmSettings();
                        dmmTrigSource = dmmSettings.TriggerSource;
                    }
                    //*********************
                    //各測定器Initialize
                    //*********************
                    await commSOURCE.SOURCE_Initialize(sourceDevices, tabname, cancellationToken);
                    if (enOsc)
                    {
                        await commOSC.OSC_Initialize(oscDevices, tabname, cancellationToken);
                        if (debugOption.Use8chOSC)
                            await commOSC.OSCUnusedChOFF(oscDevices, tabname, cancellationToken);
                    }
                    if (enDmm)
                        await commMEASURE.MEASURE_Initialize(dmmDevices, tabname, cancellationToken);
                    if (enPulse)
                        await commPG.PG_Initialize(pulseDevices, tabname, cancellationToken);
                    //*********************
                    //SourceON
                    //電源出力安定待ち時間(暫定20ms)
                    //*********************
                    await commSOURCE.SOURCE_OutputON(sourceDevices, tabname, cancellationToken);
                    if (enPulse)
                        await commPG.PG_OutputON(pulseDevices, tabname, cancellationToken);
                    await utility.Wait_Timer((int)(20), cancellationToken);
                    //*********************
                    //測定Stanby
                    //*********************
                    await utility.Wait_Timer((int)(standbyTime * 1000), cancellationToken);
                    //*********************
                    //測定ループスタート
                    //*********************
                    //normalSweep
                    if (normalSweep)
                    {
                        //*********************
                        //normalSweepでPulseが選択されている場合以降スキップ
                        //*********************
                        if (enPulse)
                            continue;
                        //*********************
                        //sweep方向がriseもしくはfall
                        //*********************
                        for (int i = 0; i <= loopCount; i++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();           //ループ中キャンセルチェック
                            long sweepValue_n = 0;
                            //*********************
                            //sweep方向がriseの場合(minValueからstart
                            //*********************
                            if (sweepDirectional == "rise")
                                sweepValue_n = (minValue_n + stepValue_n * i);
                            //*********************
                            //sweep方向がfallの場合(maxValueからstart
                            //*********************
                            if (sweepDirectional == "fall")
                                sweepValue_n = (maxValue_n - stepValue_n * i);
                            //*********************
                            //SourceVariable
                            //sweepDevicesのSourceValueを更新
                            //*********************
                            double sweepValue = sweepValue_n / 1_000_000_000.0;         //整数型で計算した電源出力設定値をdouble型に変換
                            List<string>? row = null;                                    //CSV追記用変数をループ外初期化
                            foreach (Device sweepDevice in sweepDevices)
                            {
                                SourceSettings? sweepset = sweepDevice.TabSettings[tabname] as SourceSettings;
                                sweepset.SourceValue = Math.Round(sweepValue, 8);                       //SourceValueを更新
                                //*********************
                                //CSV書き込みデータ更新
                                //出力設定値を変数に追記
                                //*********************
                                if (row == null)
                                {
                                    row = new List<string> { tabname, sweepset.SourceValue.ToString(CultureInfo.InvariantCulture) };
                                    if (enDmm)
                                        for (int j = 1; j <= 4; j++)
                                            if (deviceList.Any(d => d.Identifier == $"DMM{j}"))
                                                row.Add("");
                                }
                            }
                            if (row != null)
                                sweepRows.Add(string.Join(",", row));     //仮の行を追加
                            //*********************
                            //電源出力値設定(出力値変更実行
                            //電源出力安定待ち時間(暫定20ms)
                            //*********************
                            await commSOURCE.SOURCE_SetValue(sweepDevices, tabname, cancellationToken);
                            await utility.Wait_Timer((int)(20), cancellationToken);
                            //DMMで測定
                            if (enDmm)
                            {
                                cancellationToken.ThrowIfCancellationRequested();           //測定前キャンセルチェック
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
                                await UpdateCsvWithDmmData(sweepRows, dmmData, deviceList);
                                //*********************
                                //中間データ追記
                                //ファイルのアクセス回数を減らすため
                                //20step毎
                                //*********************
                                if (i % 20 == 0)
                                    await utility.WriteCsvFileAsync(tabNameFilePath, sweepRows, append: false, useShiftJis: true);
                                //*********************
                                //測定スタンバイ状態に遷移
                                //*********************
                                await commMEASURE.MEASURE_SetStandby(dmmDevices, tabname, cancellationToken);
                                //*********************
                                //ディスプレイ表示更新
                                //*********************
                                await commMEASURE.MEASURE_Disp(dmmDevices, tabname, cancellationToken);
                            }
                            //*********************
                            //測定wait
                            //*********************
                            await utility.Wait_Timer((int)(stepTime * 1000), cancellationToken);
                        }

                        //*********************
                        //sweep方向がrisefallもしくはfallrise
                        //*********************
                        if (directional.Length > 5)
                        {
                            for (int i = 0; i <= loopCount; i++)
                            {
                                cancellationToken.ThrowIfCancellationRequested();           //ループ中キャンセルチェック
                                long sweepValue_n = 0;
                                //*********************
                                //sweep方向がrisefallの場合(riseが完了してるのでmaxValueからstart
                                //*********************
                                if (sweepDirectional == "rise")
                                    sweepValue_n = (maxValue_n - stepValue_n * i);
                                //*********************
                                //sweep方向がfallriseの場合(fallが完了しているのminValueからstart
                                //*********************
                                if (sweepDirectional == "fall")
                                    sweepValue_n = (minValue_n + stepValue_n * i);
                                //*********************
                                //SourceVariable
                                //sweepDevicesのSourceValueを更新
                                //*********************
                                double sweepValue = sweepValue_n / 1_000_000_000.0;              //整数型で計算した電源出力設定値をdouble型に変換
                                List<string>? row = null;                                        //CSV追記用変数をループ外初期化
                                foreach (Device sweepDevice in sweepDevices)
                                {
                                    SourceSettings? sweepset = sweepDevice.TabSettings[tabname] as SourceSettings;
                                    sweepset.SourceValue = Math.Round(sweepValue, 8);           //SourceValueを更新
                                    //*********************
                                    //CSV書き込みデータ更新
                                    //出力設定値を追記
                                    //*********************
                                    if (row == null)
                                    {
                                        row = new List<string> { tabname, sweepset.SourceValue.ToString(CultureInfo.InvariantCulture) };
                                        if (enDmm)
                                            for (int j = 1; j <= 4; j++)
                                                if (deviceList.Any(d => d.Identifier == $"DMM{j}"))
                                                    row.Add("");
                                    }
                                }
                                if (row != null)
                                    sweepRows.Add(string.Join(",", row));     //仮の行を追加
                                //*********************
                                //電源出力値設定(出力値変更実行
                                //電源出力安定待ち時間(暫定20ms)
                                //*********************
                                await commSOURCE.SOURCE_SetValue(sweepDevices, tabname, cancellationToken);
                                await utility.Wait_Timer((int)(20), cancellationToken);
                                //DMMで測定
                                if (enDmm)
                                {
                                    cancellationToken.ThrowIfCancellationRequested();           //測定前キャンセルチェック
                                    StringBuilder dmmData = new StringBuilder();
                                    //*********************
                                    //測定待機状態に遷移している為BUSトリガ発生→測定
                                    //*********************
                                    if(dmmTrigSource == "BUS")
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
                                    await UpdateCsvWithDmmData(sweepRows, dmmData, deviceList);
                                    //*********************
                                    //中間データ追記
                                    //ファイルのアクセス回数を減らすため
                                    //20step毎
                                    //*********************
                                    if (i % 20 == 0)
                                        await utility.WriteCsvFileAsync(tabNameFilePath, sweepRows, append: true, useShiftJis: true);
                                    //*********************
                                    //測定スタンバイ状態に遷移
                                    //*********************
                                    await commMEASURE.MEASURE_SetStandby(dmmDevices, tabname, cancellationToken);
                                    //*********************
                                    //ディスプレイ表示更新
                                    //*********************
                                    await commMEASURE.MEASURE_Disp(dmmDevices, tabname, cancellationToken);
                                }
                                //*********************
                                //測定wait
                                //*********************
                                await utility.Wait_Timer((int)(stepTime * 1000), cancellationToken);
                            }
                        }
                    }
                    //検出,復帰sweep
                    else
                    {
                        cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
                        //*********************
                        //OscSingleRun
                        //*********************
                        if (enOsc)
                            await commOSC.OSCsingleRUN(oscDevices, tabname, cancellationToken);
                        //*********************
                        //PulseGenerator CH表示切替(Initialと同時にするとタイミングの関係上切り替わらない
                        //*********************
                        if (enPulse)
                            await commPG.PG_DispCH(pulseDevices, tabname, cancellationToken);
                        //*********************
                        //Sweep方向に基づいて初期値を設定
                        //*********************
                        bool isRise = sweepDirectional == "rise" || sweepDirectional == "risefall";
                        long initialValue_n = isRise ? minValue_n : maxValue_n; //開始電圧
                        long limitValue_n = isRise ? maxValue_n : minValue_n;   //Sweep上限/下限
                        long currentValue_n = initialValue_n;                   //現在の電圧
                        double currentValue = currentValue_n / 1_000_000_000.0; //double型に変換
                        long baseValue_n = initialValue_n;                      //上位桁のトリガ検出値を保持
                        long previousStepSize_n = 0;                            //前の桁のステップサイズを保持
                        if(enPulse)
                            initialValue_n = isRise ? 2_000_000 : -2_000_000; //Pulse選択時initial値=2mV@rise,-2mV@fall
                        //*********************
                        //電源レンジを取得
                        //*********************
                        double sourceRange = double.MaxValue;
                        if (sweepDevices.Any())
                        {
                            SourceSettings? sweepset = sweepDevices.FirstOrDefault().TabSettings[tabname] as SourceSettings;
                            sourceRange = double.Parse(sweepset.SourceRange);
                        }
                        long sourceRange_n = (long)(sourceRange * 1_000_000_000);       //100nV単位に変換
                        //*********************
                        //桁リスト（10V, 1V, 100mV, 10mV, 1mV, 100uV, 10uV, 1uV, 100nV）
                        //*********************
                        long[] stepSizes_n = new long[] {
                                                            100_000_000_000, //10V
                                                            10_000_000_000,  //1V
                                                            1_000_000_000,   //100mV
                                                            100_000_000,     //10mV
                                                            10_000_000,      //1mV
                                                            1_000_000,       //100uV
                                                            100_000,         //10uV
                                                            10_000,          //1uV
                                                            1_000            //100nV
                                                        };
                        //*********************
                        //電源レンジに基づく最小桁を設定
                        //*********************
                        long minStepSize_n;
                        if (sourceRange_n >= 100_000_000_000)       //10Vレンジ
                            minStepSize_n = 1_000_000;              //100uVまで
                        else if (sourceRange_n >= 10_000_000_000)   //1Vレンジ
                            minStepSize_n = 100_000;                //10uVまで
                        else if (sourceRange_n >= 1_000_000_000)    //100mVレンジ
                            minStepSize_n = 10_000;                 //1uVまで
                        else if (sourceRange_n >= 100_000_000)      //10mVレンジ
                            minStepSize_n = 1_000;                  //100nVまで
                        else
                            minStepSize_n = 1_000;                  //default:100nV
                        //PG使用時
                        if (enPulse)
                            minStepSize_n = 10_000;                 //1uVまで
                        //*********************
                        //レンジとSweep範囲に基づき桁リストをフィルタリング
                        //*********************
                        long sweepRange_n = Math.Abs(maxValue_n - minValue_n);
                        stepSizes_n = stepSizes_n.Where(step => step <= sourceRange_n && step <= sweepRange_n && step >= minStepSize_n).ToArray();
                        if (!stepSizes_n.Any())
                        {
                            csvRows.Add($"# {tabname}: 電源レンジ({sourceRange}V)またはSweep範囲({sweepRange_n / 1_000_000_000.0}V)で有効な桁がありません");
                            return csvRows;
                        }
                        //*********************
                        //各桁でSweep Loop
                        //*********************
                        foreach (long stepSize_n in stepSizes_n)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            bool triggerDetected = false;
                            long stepDirection_n = isRise ? stepSize_n : -stepSize_n;   //Sweep方向
                            //*********************
                            //開始電圧を計算
                            //*********************
                            long offset_n;
                            if (stepSize_n >= 1_000_000) //1V～100uV
                            {
                                //初回（10V桁）は中央値を使用
                                if (previousStepSize_n == 0)
                                {
                                    long rangeStart_n = Math.Max(minValue_n, baseValue_n);
                                    long rangeEnd_n = Math.Min(maxValue_n, limitValue_n);
                                    currentValue_n = rangeStart_n + (rangeEnd_n - rangeStart_n) / 2;    //中央値
                                }
                                else
                                {
                                    //前の桁のステップサイズ分ずらす
                                    offset_n = previousStepSize_n;          //例: 1Vの次は100mV、100mVの次は10mV
                                    currentValue_n = isRise ? baseValue_n - offset_n : baseValue_n + offset_n;
                                }
                            }
                            else //10uV以下の桁
                            {
                                //前の桁のステップサイズの3倍ずらす(暫定)
                                offset_n = previousStepSize_n > 0 ? previousStepSize_n * 3 : stepSize_n * 3;
                                currentValue_n = isRise ? baseValue_n - offset_n : baseValue_n + offset_n;
                            }
                            //*********************
                            //開始電圧を範囲内に制限
                            //*********************
                            currentValue_n = Math.Max(minValue_n, Math.Min(maxValue_n, Math.Min(sourceRange_n, Math.Max(-sourceRange_n, currentValue_n))));
                            if ((isRise && currentValue_n > limitValue_n) || (!isRise && currentValue_n < limitValue_n))
                            {
                                csvRows.Add($"# {tabname}: 開始電圧({currentValue_n / 1_000_000_000.0}V)がSweep範囲または電源レンジ({sourceRange}V)を超えています");
                                continue;
                            }

                            cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
                            //*********************
                            //初期状態へ遷移(復帰状態もしくは検出状態)
                            //*********************
                            currentValue = initialValue_n / 1_000_000_000.0;
                            if(enPulse)
                                await commPG.PG_RangeAutoHold(pulseDevices, tabname, "OFF", cancellationToken);     //PGのAutoRangeHold=OFF
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
                            //Sweep電源初期設定
                            //*********************
                            triggerDetected = await TrySetVoltageAndCheckTrigger(
                                                                    sweepDevices, oscDevices, pulseDevices, tabname, currentValue, isRise,
                                                                    enOsc, enPulse, checkTime, cancellationToken, 
                                                                    csvRows, triggerRows, null, false);
                            if (enPulse)
                                await commPG.PG_RangeAutoHold(pulseDevices, tabname, "ON", cancellationToken);     //PGのAutoRangeHold=ON
                            //初期状態遷移でトリガが検出された場合
                            if (triggerDetected)
                            {
                                csvRows.Add($"# {tabname}: 初期状態遷移で検出もしくは復帰エラー({currentValue}V)");
                                break;
                            }
                            //*********************
                            //Sweep開始
                            //*********************
                            while (isRise ? currentValue_n <= limitValue_n : currentValue_n >= limitValue_n)
                            {
                                cancellationToken.ThrowIfCancellationRequested();       //キャンセルチェック
                                //*********************
                                //電圧値変化→wait→OSCトリガ確認
                                //*********************
                                currentValue = currentValue_n / 1_000_000_000.0;
                                bool triggerDigitFinal = (stepSize_n == stepSizes_n.Last());
                                triggerDetected = await TrySetVoltageAndCheckTrigger(
                                                                    sweepDevices, oscDevices, pulseDevices, tabname, currentValue, isRise,
                                                                    enOsc, enPulse, stepTime, cancellationToken, 
                                                                    csvRows, triggerRows, finalTriggerRows, triggerDigitFinal);
                                //*********************
                                //トリガが検出された場合
                                //*********************
                                if (triggerDetected)
                                {
                                    //トリガ検出値を更新
                                    baseValue_n = currentValue_n;
                                    break;          //ループから抜ける
                                }
                                currentValue_n += stepDirection_n;      //次の電圧
                            }
                            //*********************
                            //stepSize_nの桁でSweepしてトリガ検出後の挙動
                            //検出電圧Sweep時→復帰動作　復帰電圧Sweep時→検出動作
                            //*********************
                            if (triggerDetected)
                            {
                                //電圧を開始値にリセット
                                currentValue_n = initialValue_n;
                                currentValue = currentValue_n / 1_000_000_000.0;
                                foreach (Device sweepDevice in sweepDevices)
                                {
                                    SourceSettings? sweepset = sweepDevice.TabSettings[tabname] as SourceSettings;
                                    sweepset.SourceValue = Math.Round(currentValue, 8);
                                }
                                try
                                {
                                    //*********************
                                    //電源出力値設定(出力値変更実行
                                    //電源出力安定待ち時間(暫定20ms)
                                    //*********************
                                    await commSOURCE.SOURCE_SetValue(sweepDevices, tabname, cancellationToken);
                                    await utility.Wait_Timer((int)(20), cancellationToken);
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
                                    if(isSpecial)
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
                                }
                                catch (MeasFatalException ex)
                                {
                                    sweepData.Add($"# Sweep動作中に致命レベルエラーが発生しました: {ex.Message}");
                                    csvRows.Add($"# {string.Join(" ", sweepData).Replace(Environment.NewLine, " ")}");
                                    //処理中のタブデータがあれば保存
                                    if (tabCsvRows.Any() && tabCsvRows.Count > 1)
                                    {
                                        tabNameFilePath = baseTempFilePath.Replace("_SweepData_", $"_ErrorSweepData_{currentTabName}_");
                                        await utility.WriteCsvFileAsync(tabNameFilePath, tabCsvRows, append: false, useShiftJis: true);
                                    }
                                    return csvRows;
                                }
                                catch (Exception ex)
                                {
                                    //WarningやExceptionエラー発生なら継続
                                    csvRows.Add($"# {tabname}: 検出もしくは復帰エラー({currentValue}V): {ex.Message}");
                                    if (tabCsvRows.Any() && tabCsvRows.Count > 1)
                                    {
                                        tabNameFilePath = baseTempFilePath.Replace("_SweepData_", $"_ErrorSweepData_{currentTabName}_");
                                        await utility.WriteCsvFileAsync(tabNameFilePath, tabCsvRows, append: false, useShiftJis: true);
                                    }
                                    //Warningで停止する場合
                                    if (debugOption.StopOnWarning)
                                        return csvRows;
                                    continue;
                                }

                                //*********************
                                //OscSingleRun
                                //*********************
                                if (enOsc)
                                    await commOSC.OSCsingleRUN(oscDevices, tabname, cancellationToken);
                                //*********************
                                //次の桁のために前の桁のステップサイズを保存
                                //*********************
                                previousStepSize_n = stepSize_n;
                            }
                            //*********************
                            //stepSize_nの桁でSweepしてトリガ未検出の場合
                            //*********************
                            else
                            {
                                if (previousStepSize_n != 0)
                                {
                                    //この桁(stepSize_n)のスイープを終了
                                    csvRows.Add($"# {tabname}: トリガ検出なし (桁: {stepSize_n / 1_000_000_000.0}V)");
                                    continue;
                                }

                            }
                        }
                    }
                    //*********************
                    //1条件(Tabname)終了時動作
                    //SourceOFF
                    //*********************
                    await commSOURCE.SOURCE_OutputOFF(sourceDevices, tabname, cancellationToken);
                    if (enPulse)
                        await commPG.PG_OutputOFF(pulseDevices, tabname, cancellationToken);
                    //*********************
                    //OSC Stop
                    //*********************
                    if (enOsc)
                        await commOSC.OSCSTOP(oscDevices, tabname, cancellationToken);
                    //*********************
                    //全測定器Remote解除
                    //*********************
                    await commSOURCE.SOURCE_RemoteOFF(sourceDevices);
                    if (enOsc)
                        await commOSC.OSC_RemoteOFF(oscDevices);
                    if (enDmm)
                        await commMEASURE.MEASURE_RemoteOFF(dmmDevices);
                    if(enPulse)
                        await commPG.PG_RemoteOFF(pulseDevices);
                    //*********************
                    //データ格納
                    //*********************
                    if (normalSweep)
                    {
                        tabCsvRows.AddRange(sweepRows);
                        resultDataRowsByTab[tabname].AddRange("normalsweep");
                        resultDataRowsByTab[tabname].AddRange(sweepRows);
                    }
                    else
                    {
                        tabCsvRows.AddRange(triggerRows);
                        resultDataRowsByTab[tabname].AddRange(finalTriggerRows);
                    }
                    //*********************
                    //タブごとの中間データ最終保存
                    //*********************
                    if (tabCsvRows.Any() && tabCsvRows.Count > 1)
                        await utility.WriteCsvFileAsync(tabNameFilePath, tabCsvRows, append: false, useShiftJis: true);
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
                if (sweepData.Any())                                                                    //途中でエラーが発生していたら
                    csvRows.Add($"# {string.Join(" ", sweepData).Replace(Environment.NewLine, " ")}");  //戻り値csvRowsにエラーメッセージ追加
                return csvRows;
            }
            catch (OperationCanceledException)
            {
                sweepData.Add("# Sweep動作がキャンセルされました。");
                csvRows.Add($"# {string.Join(" ", sweepData).Replace(Environment.NewLine, " ")}");
                //処理中のタブデータがあれば保存
                if (tabCsvRows.Any() && tabCsvRows.Count > 1)
                {
                    string tabNameFilePath = baseTempFilePath.Replace("_SweepData_", $"_AbortSweepData_{currentTabName}_");
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
                    List<Device>? dmmDevices = deviceList.Where(d => d.Identifier.StartsWith("DMM", StringComparison.OrdinalIgnoreCase)).ToList();
                    List<Device>? pulseDevices = deviceList.Where(d => d.Identifier.Equals("PULSE", StringComparison.OrdinalIgnoreCase)).ToList();

                    await commSOURCE.SOURCE_RemoteOFF(sourceDevices);
                    if (oscDevices.Any())
                        await commOSC.OSC_RemoteOFF(oscDevices);
                    if (dmmDevices.Any())
                        await commMEASURE.MEASURE_RemoteOFF(dmmDevices);
                    if(pulseDevices.Any())
                        await commPG.PG_RemoteOFF(pulseDevices);
                }
            }
        }
        //*************************************************
        //アクセス：private
        //戻り値：なし<Task>
        //機能：電圧設定とOSCトリガ確認
        //引数1：sweepDevices
        //説明：List<string> 書き込みデータ用変数
        //引数2：oscDevices
        //説明：<StringBuilder> 測定データ群
        //引数3：tabname
        //説明：List<Device> DMM設定リスト群
        //*************************************************
        private async Task<bool> TrySetVoltageAndCheckTrigger(
                                                                List<Device> sweepDevices,
                                                                List<Device> oscDevices,
                                                                List<Device> pulseDevices,
                                                                string? tabname,
                                                                double currentValue,
                                                                bool isRise,
                                                                bool enOsc,
                                                                bool enPulse,
                                                                float stepTime,
                                                                CancellationToken cancellationToken,
                                                                List<string>? csvRows,
                                                                List<string>? triggerRows,
                                                                List<string>? finalTriggerRows,
                                                                bool triggerDigitFinal)
        {
            foreach (Device? sweepDevice in sweepDevices)
            {
                SourceSettings? sweepset = sweepDevice.TabSettings[tabname] as SourceSettings;
                sweepset.SourceValue = Math.Round(currentValue, 8); //SWEEP設定のSourceValueを更新
            }
            if (enPulse)
            {
                Device? pulseDevice = pulseDevices.FirstOrDefault();
                PGSettings? pgSettings = pulseDevice.TabSettings[tabname] as PGSettings;
                if(isRise)
                    pgSettings.HighLevelValue= Math.Round(currentValue, 8); //SWEEP設定のSourceValueを更新
                else
                    pgSettings.LowLevelValue = Math.Round(currentValue, 8); //SWEEP設定のSourceValueを更新
            }

            try
            {
                //*********************
                //電源出力値設定(出力値変更実行
                //電源出力安定待ち時間(暫定20ms)
                //*********************
                if(enPulse)
                {
                    await commPG.PG_SetHighLow(pulseDevices, tabname, cancellationToken);
                    await commPG.PG_BusTrigger(pulseDevices, tabname, cancellationToken);       //BUSトリガ発生(問題あればCHトリガに変更
                }
                else
                    await commSOURCE.SOURCE_SetValue(sweepDevices, tabname, cancellationToken);

                await utility.Wait_Timer((int)(20), cancellationToken);
            }
            catch (MeasFatalException)
            {
                throw;
            }
            catch (Exception ex)
            {
                //WarningExceptionとExceptionの場合
                csvRows.Add($"# {tabname}: 電源設定エラー({currentValue}V): {ex.Message}");
                return false;
            }

            if (enOsc)
            {
                //*********************
                //測定wait
                //*********************
                await utility.Wait_Timer((int)(stepTime * 1000), cancellationToken);
                //*********************
                //OSCトリガ確認
                //*********************
                bool isTriggered = await commOSC.OSCTriggeredCheck(oscDevices, tabname, cancellationToken);
                //トリガ検出後csvRowsに結果を追加
                if (isTriggered)
                {
                    List<string> row = new List<string> {
                                                            tabname,
                                                            currentValue.ToString(CultureInfo.InvariantCulture)
                                                        };
                    string rowString = (string.Join(",", row));
                    triggerRows.Add(rowString);                     //中間データ用に追加
                    if (triggerDigitFinal)
                        finalTriggerRows.Add(rowString);            //最終桁の場合に追加
                }
                return isTriggered;
            }
            return false;
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
            //Sweep条件ヘッダー
            //*********************
            csvRows.Add("SweepSettings");       //区切りコメント
            List<Device>? Settings = null;
            (Settings, bool settingsuccess) = utility.FilterSettings(deviceList, "SWEEP", TabNamesText);
            Device? device = Settings.FirstOrDefault();      //Sweep設定はsweep電源で共通なので最初のデータ群だけで問題なし
            SweepSettings sweepSettings = device.TabSettings[TabNamesText] as SweepSettings ?? new SweepSettings();
            var row = new List<string>
            {
                TabNamesText,
                string.Join(",", new[]
                {
                    $"MinValue={sweepSettings.MinValue:F8}",
                    $"MaxValue={sweepSettings.MaxValue:F8}",
                    $"StepValue={sweepSettings.StepValue:F8}",
                    $"StepTime={sweepSettings.StepTime:F2}{sweepSettings.StepTimeUnit ?? ""}",
                    $"StandbyTime={sweepSettings.StandbyTime:F2}{sweepSettings.StandbyTimeUnit ?? ""}",
                    $"Directional={sweepSettings.Directional ?? "N/A"}",
                    $"Normalsweep={sweepSettings.Normalsweep}"
                })
            };
            csvRows.Add(string.Join(",", row));

            //*********************
            //Dataヘッダー
            //*********************
            csvRows.Add("Data");       //区切りコメント
            List<string> headers = new List<string> { "TabName", "SweepValue" };
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

            List<Device>? sourceDevices = null;     //初期化
            (sourceDevices, bool success) = utility.FilterSettings(deviceList, "SOURCE", TabNamesText);
            //*********************
            //Sweep動作電源を特定してsweepDevicesとしてリスト化
            //*********************
            List<Device>? sweepDevices = null;
            sweepDevices = sourceDevices.Where(d =>                         //sourceDevicesの中で
                d.TabSettings.ContainsKey(TabNamesText) &&                  //測定項目名(TabNamesText)が一致して
                d.TabSettings[TabNamesText] is SourceSettings settings &&   //TabSettings[TabNamesText]がSourceSettings型で定義されていて
                settings.Function == "sweep")                               //Functionが"sweep"になっている
                .ToList();                                                  //DeviceオブジェクトをList化しsweepDevicesに渡す
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
