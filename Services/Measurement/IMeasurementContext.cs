using TemperatureCharacteristics.Services.Dialog;
using TemperatureCharacteristics.ViewModels.Devices;
using TemperatureCharacteristics.ViewModels.Tabs.Delay;
using TemperatureCharacteristics.ViewModels.Tabs.Sweep;
using TemperatureCharacteristics.ViewModels.Tabs.VI;

namespace TemperatureCharacteristics.Services.Measurement
{
    public interface IMeasurementContext
    {
        //*************************************************
        //UI状態
        //*************************************************
        string MeasurementStatus { get; set; }
        bool MultiTemperature { get; }
        bool IsRunning { get; set; }
        //*************************************************
        //MeasurementManagerで使うため中継
        //*************************************************
        SweepTabGroupViewModel SweepTabs { get; }
        DelayTabGroupViewModel DelayTabs { get; }
        VITabGroupViewModel VITabs { get; }
        //*************************************************
        //測定器・環境情報
        //*************************************************
        List<float> Temperatures { get; }
        List<InstrumentViewModel> MeasInst { get; }
        //*************************************************
        //メッセージ表示
        //*************************************************
        IDialogService DialogService { get; }
        //ログ
        void LogDebug(string message);
        //*************************************************
        //結果ファイル名
        //*************************************************
        string FinalFileName { get; }

        //ピボット処理
        List<string> CreatePivotRows(
                            List<string> dataRows,
                            bool multiTemperature,
                            bool multiSample);

        //フッター生成
        string BuildSettingsFooter(List<(bool IsChecked, string UsbId, string InstName, string Identifier)> measInstData);
    }
}

