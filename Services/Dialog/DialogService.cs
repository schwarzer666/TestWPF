using System.Windows;
using TemperatureCharacteristics.Services.Dialog;

public class DialogService : IDialogService
{
    public void ShowMessage(string message, string title = "情報")
    {
        MessageBox.Show(
            Application.Current.MainWindow,
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    public void ShowError(string message, string title = "エラー")
    {
        MessageBox.Show(
            Application.Current.MainWindow,
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    public bool ShowConfirm(string message, string title = "確認")
    {
        var result = MessageBox.Show(
            Application.Current.MainWindow,
            message,
            title,
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question);

        return result == MessageBoxResult.OK;
    }
}
