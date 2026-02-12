using System.ComponentModel;
using System.Runtime.CompilerServices;

public abstract class BaseViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    //*************************************************
    //定義
    // プロパティが変更通知時に呼び出され値を更新
    // CallerMemberNameありだと引数を省略した場合、呼び出したプロパティ名が自動で渡される
    //*************************************************
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    //*************************************************
    //プロパティ変更時の無駄なOnPropertyChanged呼び出し防止用
    //*************************************************
    protected bool SetProperty<T>(
                                ref T field,
                                T value,
                                [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

}
