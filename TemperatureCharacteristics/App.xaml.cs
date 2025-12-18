using System.Reflection;
using System.Text;
using System.Windows;
using System.IO;

namespace TemperatureCharacteristics
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        //public App()
        //{
        //    //アプリ立ち上げ時Librariesフォルダ読み込み
        //    AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        //}
        protected override void OnStartup(StartupEventArgs e)
        {
            //Shift-JISのコードページを登録
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            base.OnStartup(e);
        }
        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string assemblyName = new AssemblyName(args.Name).Name + ".dll";
            string assemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Libraries", assemblyName);

            if (File.Exists(assemblyPath))
                return Assembly.LoadFrom(assemblyPath);
            return null;
        }
    }

}
