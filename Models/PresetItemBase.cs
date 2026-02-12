using System.Text.Json.Serialization;

namespace TemperatureCharacteristics.Models
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "Type")]
    [JsonDerivedType(typeof(PresetItemSweep), "Sweep")]
    [JsonDerivedType(typeof(PresetItemDelay), "Delay")]
    [JsonDerivedType(typeof(PresetItemVI), "VI")]
    public abstract class PresetItemBase
    {
        [JsonPropertyOrder(0)]
        [JsonIgnore]                                    //TypeプロパティをJSONシリアライズから除外
        public string Type { get; set; }                //JSON デシリアライズ用に型を識別(Sweep,Delay,VI)
        [JsonPropertyOrder(1)]
        public string Id { get; set; }                  //識別番号
        [JsonPropertyOrder(2)]
        public string Name { get; set; }                //プリセットボタン名
        [JsonPropertyOrder(3)]
        public string? BackgroundColor { get; set; }     //プリセットボタン背景色
        [JsonPropertyOrder(4)]
        public string ItemHeader { get; set; }          //Itemタブ入力欄
        [JsonPropertyOrder(5)]
        public bool MeasureOn { get; set; }             //測定ON/OFF CheckBox
    }
    public class SourceConfig
    {
        [JsonPropertyOrder(1)]
        [JsonPropertyName("SourceFunc1Index")]
        public int? SourceFunc1Index { get; set; }   //電源1の動作（初期値Sweep
        [JsonPropertyOrder(2)]
        [JsonPropertyName("SourceMode1Index")]
        public int? SourceMode1Index { get; set; }   //電源1の印加モード
        [JsonPropertyOrder(3)]
        [JsonPropertyName("SourceRang1Index")]
        public int? SourceRang1Index { get; set; }   //電源1のレンジ
        [JsonPropertyOrder(4)]
        [JsonPropertyName("SourceLimit1")]
        public string? SourceLimit1 { get; set; }    //電源1のLimit value
        [JsonPropertyOrder(5)]
        [JsonPropertyName("SourceLimit1Index")]
        public int? SourceLimit1Index { get; set; }  //電源1のLimitレンジ
        [JsonPropertyOrder(6)]
        [JsonPropertyName("SourceFunc2Index")]
        public int? SourceFunc2Index { get; set; }   //電源2の動作（初期値Constant1
        [JsonPropertyOrder(7)]
        [JsonPropertyName("SourceMode2Index")]
        public int? SourceMode2Index { get; set; }   //電源2の印加モード
        [JsonPropertyOrder(8)]
        [JsonPropertyName("SourceRang2Index")]
        public int? SourceRang2Index { get; set; }   //電源2のレンジ
        [JsonPropertyOrder(9)]
        [JsonPropertyName("SourceLimit2")]
        public string? SourceLimit2 { get; set; }    //電源2のLimit value
        [JsonPropertyOrder(10)]
        [JsonPropertyName("SourceLimit2Index")]
        public int? SourceLimit2Index { get; set; }  //電源2のLimitレンジ
        [JsonPropertyOrder(11)]
        [JsonPropertyName("SourceFunc3Index")]
        public int? SourceFunc3Index { get; set; }   //電源3の動作（初期値Constant2
        [JsonPropertyOrder(12)]
        [JsonPropertyName("SourceMode3Index")]
        public int? SourceMode3Index { get; set; }   //電源3の印加モード
        [JsonPropertyOrder(13)]
        [JsonPropertyName("SourceRang3Index")]
        public int? SourceRang3Index { get; set; }   //電源3のレンジ
        [JsonPropertyOrder(14)]
        [JsonPropertyName("SourceLimit3")]
        public string? SourceLimit3 { get; set; }    //電源3のLimit value
        [JsonPropertyOrder(15)]
        [JsonPropertyName("SourceLimit3Index")]
        public int? SourceLimit3Index { get; set; }  //電源3のLimitレンジ
        [JsonPropertyOrder(16)]
        [JsonPropertyName("SourceFunc4Index")]
        public int? SourceFunc4Index { get; set; }   //電源4の動作（初期値Constant3
        [JsonPropertyOrder(17)]
        [JsonPropertyName("SourceMode4Index")]
        public int? SourceMode4Index { get; set; }   //電源4の印加モード
        [JsonPropertyOrder(18)]
        [JsonPropertyName("SourceRang4Index")]
        public int? SourceRang4Index { get; set; }   //電源4のレンジ
        [JsonPropertyOrder(19)]
        [JsonPropertyName("SourceLimit4")]
        public string? SourceLimit4 { get; set; }    //電源4のLimit value
        [JsonPropertyOrder(20)]
        [JsonPropertyName("SourceLimit4Index")]
        public int? SourceLimit4Index { get; set; }  //電源4のLimitレンジ
    }
    public class ConstConfig
    {
        [JsonPropertyOrder(1)]
        [JsonPropertyName("Const1")]
        public string? Const1 { get; set; }          //constant電源1 value
        [JsonPropertyOrder(2)]
        [JsonPropertyName("Const1UnitIndex")]
        public int? Const1UnitIndex { get; set; }    //constant電源1 Unit
        [JsonPropertyOrder(3)]
        [JsonPropertyName("Const2")]
        public string? Const2 { get; set; }          //constant電源2 value
        [JsonPropertyOrder(4)]
        [JsonPropertyName("Const2UnitIndex")]
        public int? Const2UnitIndex { get; set; }    //constant電源2 Unit
        [JsonPropertyOrder(5)]
        [JsonPropertyName("Const3")]
        public string? Const3 { get; set; }          //constant電源3 value
        [JsonPropertyOrder(6)]
        [JsonPropertyName("Const3UnitIndex")]
        public int? Const3UnitIndex { get; set; }    //constant電源3 Unit
        [JsonPropertyOrder(7)]
        [JsonPropertyName("Const4")]
        public string? Const4 { get; set; }          //constant電源4 value
        [JsonPropertyOrder(8)]
        [JsonPropertyName("Const4UnitIndex")]
        public int? Const4UnitIndex { get; set; }    //constant電源4 Unit
        public bool HasConst4 => Const4 != null;
    }
    public class DetectReleaseConfig
    {
        [JsonPropertyOrder(1)]
        [JsonPropertyName("DetectReleaseIndex")]
        public int? DetectReleaseIndex { get; set; } //Sweep検出復帰の際の動作（normal→Sweep電源をStart値に戻す normal+α→別電源を変動させる（ラッチタイプでVMを0.3V程度に引き上げる想定
        [JsonPropertyOrder(2)]
        [JsonPropertyName("DetectSourceAIndex")]
        public int? DetectSourceAIndex { get; set; } //Sweep検出復帰の際にSweep電源をStart値に戻す＋別電源を追加で使用する場合の電源を指定A（VMを想定
        [JsonPropertyOrder(3)]
        [JsonPropertyName("DetectReleaseA")]
        public string? DetectReleaseA { get; set; }  //Sweep検出復帰の際にSweep電源をStart値に戻す＋別電源Aを追加で使用する場合の値
        [JsonPropertyOrder(4)]
        [JsonPropertyName("DetectUnitAIndex")]
        public int? DetectUnitAIndex { get; set; }   //上記検出復帰時に使用する電源AのUnit
        [JsonPropertyOrder(5)]
        [JsonPropertyName("DetectSourceBIndex")]
        public int? DetectSourceBIndex { get; set; } //Sweep検出復帰の際にSweep電源をStart値に戻す＋別電源を追加で使用する場合の電源を指定B（CSを想定
        [JsonPropertyOrder(6)]
        [JsonPropertyName("DetectReleaseB")]
        public string? DetectReleaseB { get; set; }  //Sweep検出復帰の際にSweep電源をStart値に戻す＋別電源Bを追加で使用する場合の値
        [JsonPropertyOrder(7)]
        [JsonPropertyName("DetectUnitBIndex")]
        public int? DetectUnitBIndex { get; set; }   //上記検出復帰時に使用する電源BのUnit
        [JsonPropertyOrder(8)]
        [JsonPropertyName("CheckTime")]
        public string? CheckTime { get; set; }       //Sweep検出復帰の際のwait value（ex.vdet1測定時tvrel1を指定
        [JsonPropertyOrder(9)]
        [JsonPropertyName("CheckTimeUnitIndex")]
        public int? CheckTimeUnitIndex { get; set; } //Sweep検出復帰の際のwait Unit
    }
    public class OscConfig
    {
        [JsonPropertyOrder(1)]
        [JsonPropertyName("OSCRang1Index")]
        public int? OSCRang1Index { get; set; }      //OSC Ch1 range
        [JsonPropertyOrder(2)]
        [JsonPropertyName("OSCPos1")]
        public string? OSCPos1 { get; set; }         //OSC Ch1 position
        [JsonPropertyOrder(3)]
        [JsonPropertyName("OSCRang2Index")]
        public int? OSCRang2Index { get; set; }      //OSC Ch2 range
        [JsonPropertyOrder(4)]
        [JsonPropertyName("OSCPos2")]
        public string? OSCPos2 { get; set; }         //OSC Ch2 position
        [JsonPropertyOrder(5)]
        [JsonPropertyName("OSCRang3Index")]
        public int? OSCRang3Index { get; set; }      //OSC Ch3 range
        [JsonPropertyOrder(6)]
        [JsonPropertyName("OSCPos3")]
        public string? OSCPos3 { get; set; }         //OSC Ch3 position
        [JsonPropertyOrder(7)]
        [JsonPropertyName("OSCRang4Index")]
        public int? OSCRang4Index { get; set; }      //OSC Ch4 range
        [JsonPropertyOrder(8)]
        [JsonPropertyName("OSCPos4")]
        public string? OSCPos4 { get; set; }         //OSC Ch4 position
        [JsonPropertyOrder(9)]
        [JsonPropertyName("TrigSourceIndex")]
        public int? TrigSourceIndex { get; set; }    //OSC Trig ソース
        [JsonPropertyOrder(10)]
        [JsonPropertyName("TrigDirectionalIndex")]
        public int TrigDirectionalIndex { get; set; }   //OSC Trig Rise/Fall
        [JsonPropertyOrder(11)]
        [JsonPropertyName("OSCTrigLevel")]
        public string? OSCTrigLevel { get; set; }    //OSC Trig level
        [JsonPropertyOrder(12)]
        [JsonPropertyName("LevelUnitIndex")]
        public int? LevelUnitIndex { get; set; }     //OSC Trig Unit
        [JsonPropertyOrder(13)]
        [JsonPropertyName("OSCTimeRangeIndex")]
        public int? OSCTimeRangeIndex { get; set; } //OSC TimeRange
        [JsonPropertyOrder(14)]
        [JsonPropertyName("OSCTimeRangeUnitIndex")]
        public int? OSCTimeRangeUnitIndex { get; set; } //OSC TimeRange Unit
        [JsonPropertyOrder(15)]
        [JsonPropertyName("OSCTimePos")]
        public string? OSCTimePos { get; set; }      //OSC Horizontial position
    }
    public class DmmConfig
    {
        [JsonPropertyOrder(1)]
        [JsonPropertyName("MeasMode1Index")]
        public int? MeasMode1Index { get; set; }     //DMM1の測定モード
        [JsonPropertyOrder(2)]
        [JsonPropertyName("MeasMode2Index")]
        public int? MeasMode2Index { get; set; }     //DMM2の測定モード
        [JsonPropertyOrder(3)]
        [JsonPropertyName("MeasMode3Index")]
        public int? MeasMode3Index { get; set; }     //DMM3の測定モード
        [JsonPropertyOrder(4)]
        [JsonPropertyName("MeasMode4Index")]
        public int? MeasMode4Index { get; set; }     //DMM4の測定モード
        [JsonPropertyOrder(5)]
        [JsonPropertyName("MeasRang1Index")]
        public int? MeasRang1Index { get; set; }     //DMM1のレンジ
        [JsonPropertyOrder(6)]
        [JsonPropertyName("MeasRang2Index")]
        public int? MeasRang2Index { get; set; }     //DMM2のレンジ
        [JsonPropertyOrder(7)]
        [JsonPropertyName("MeasRang3Index")]
        public int? MeasRang3Index { get; set; }     //DMM3のレンジ
        [JsonPropertyOrder(8)]
        [JsonPropertyName("MeasRang4Index")]
        public int? MeasRang4Index { get; set; }     //DMM4レンジ
        [JsonPropertyOrder(9)]
        [JsonPropertyName("MeasTrigIndex")]
        public int? MeasTrigIndex { get; set; }      //DMMのトリガソース
        [JsonPropertyOrder(10)]
        [JsonPropertyName("Meas1plc")]
        public string? Meas1plc { get; set; }        //DMM1のNPLC value
        [JsonPropertyOrder(11)]
        [JsonPropertyName("Meas2plc")]
        public string? Meas2plc { get; set; }        //DMM2のNPLC value
        [JsonPropertyOrder(12)]
        [JsonPropertyName("Meas3plc")]
        public string? Meas3plc { get; set; }        //DMM3のNPLC value
        [JsonPropertyOrder(13)]
        [JsonPropertyName("Meas4plc")]
        public string? Meas4plc { get; set; }        //DMM4のNPLC value
        [JsonPropertyOrder(14)]
        [JsonPropertyName("Meas1DispOn")]
        public bool? Meas1DispOn { get; set; }       //DMM1のDisplay ON/OFF CheckBox
        [JsonPropertyOrder(15)]
        [JsonPropertyName("Meas2DispOn")]
        public bool? Meas2DispOn { get; set; }       //DMM2のDisplay ON/OFF CheckBox
        [JsonPropertyOrder(16)]
        [JsonPropertyName("Meas3DispOn")]
        public bool? Meas3DispOn { get; set; }       //DMM3のDisplay ON/OFF CheckBox
        [JsonPropertyOrder(17)]
        [JsonPropertyName("Meas4DispOn")]
        public bool? Meas4DispOn { get; set; }       //DMM4のDisplay ON/OFF CheckBox
    }
    public class PulseGenConfig
    {
        [JsonPropertyOrder(1)]
        [JsonPropertyName("OutputChIndex")]
        public int? OutputChIndex { get; set; }      //PG 出力CH
        [JsonPropertyOrder(2)]
        [JsonPropertyName("LowValue")]
        public string? LowValue { get; set; }        //PG Low value
        [JsonPropertyOrder(3)]
        [JsonPropertyName("LowVUnitIndex")]
        public int? LowVUnitIndex { get; set; }      //PG LowValue Unit
        [JsonPropertyOrder(4)]
        [JsonPropertyName("HighValue")]
        public string? HighValue { get; set; }       //PG High value
        [JsonPropertyOrder(5)]
        [JsonPropertyName("HighVUnitIndex")]
        public int? HighVUnitIndex { get; set; }     //PG HighValue Unit
        [JsonPropertyOrder(6)]
        [JsonPropertyName("PolarityIndex")]
        public int? PolarityIndex { get; set; }      //PG 出力極性
        [JsonPropertyOrder(7)]
        [JsonPropertyName("PeriodValue")]
        public string? PeriodValue { get; set; }     //PG Period value
        [JsonPropertyOrder(8)]
        [JsonPropertyName("PeriodUnitIndex")]
        public int? PeriodUnitIndex { get; set; }    //PG PeriodValue Unit
        [JsonPropertyOrder(9)]
        [JsonPropertyName("WidthUnitIndex")]
        public int? WidthUnitIndex { get; set; }     //PG WidthValue Unit
        [JsonPropertyOrder(10)]
        [JsonPropertyName("WidthValue")]
        public string? WidthValue { get; set; }      //PG Width value
        [JsonPropertyOrder(11)]
        [JsonPropertyName("OutputLoadIndex")]
        public int? OutputLoadIndex { get; set; }    //PG 出力抵抗
        [JsonPropertyOrder(12)]
        [JsonPropertyName("TrigOutIndex")]
        public int TrigOutIndex { get; set; }      //PG TrigOut ON/OFF
    }
}
