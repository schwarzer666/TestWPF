using System.Windows;
using System.Windows.Controls;


namespace TemperatureCharacteristics.Views.Controls
{
    public partial class InstrumentListControl : UserControl
    {
        public InstrumentListControl()
        {
            InitializeComponent();
        }
        //************************************
        //動作
        // USB ID ListBoxのテキストを選択した時
        // クリップボードの内容をクリアし
        // クリップボードに選択した内容を張り付け
        //************************************
        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ListBox lb) return;
            if (lb.SelectedItem == null) return;
            Clipboard.Clear();
            Clipboard.SetText(lb.SelectedItem.ToString());
        }
    }
}
