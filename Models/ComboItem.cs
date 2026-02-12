using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TemperatureCharacteristics.Models
{
    //*************************************************
    //定義
    // 名前空間の定義Combobox用Item
    //*************************************************
    public class ComboItem : INotifyPropertyChanged
    {
        private string? _name;
        public string? Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }
        private string? _tag;
        public string? Tag
        {
            get => _tag;
            set
            {
                if (_tag != value)
                {
                    _tag = value;
                    OnPropertyChanged();
                }
            }
        }
        public override string? ToString() => Name;
        //********************************
        //プロパティ変更通知
        //********************************
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}