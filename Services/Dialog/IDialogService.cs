namespace TemperatureCharacteristics.Services.Dialog
{
    public interface IDialogService
    {
        void ShowMessage(string message, string title = "情報");
        void ShowError(string message, string title = "エラー");
        bool ShowConfirm(string message, string title = "確認");
    }

}
