using System.Configuration;
using System.Data;
using System.Text;
using System.Windows;

namespace TemperatureCharacteristics
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            //Shift-JISのコードページを登録
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            base.OnStartup(e);
        }
    }

}
