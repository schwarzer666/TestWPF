using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using TemperatureCharacteristics.Services.Data;
using TemperatureCharacteristics.Services.UserConfig;

namespace TemperatureCharacteristics
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IUserConfigService ConfigService { get; private set; }
        public App()
        {
            //JSONサービス
            IJsonDataService jsonService = new DataService();
            //デフォルト保存フォルダ
            string saveloadDefaultPath = Path.Combine(
                                                    AppDomain.CurrentDomain.BaseDirectory,
                                                    "Resources",
                                                    "Settings");
            if (!Directory.Exists(saveloadDefaultPath))
                Directory.CreateDirectory(saveloadDefaultPath);
            //ログ出力
            void LogDebug(string message)
            {
                //初期化時のみ出力ウィンドウにログを表示
                System.Diagnostics.Debug.WriteLine(message);
            }
            //プリセット管理
            var presetManager = new PresetManager(LogDebug);
            ConfigService = new UserConfigService(
                                                jsonService,
                                                presetManager,
                                                LogDebug);
        }
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
