using TemperatureCharacteristics.ViewModels;
using TemperatureCharacteristics.ViewModels.Tabs.Delay;
using TemperatureCharacteristics.ViewModels.Tabs.Sweep;
using TemperatureCharacteristics.ViewModels.Tabs.VI;

namespace TemperatureCharacteristics.Services.UserConfig
{
    public interface ITabFactory
    {
        SweepTabViewModel CreateSweepTab();
        DelayTabViewModel CreateDelayTab();
        VITabViewModel CreateVITab();
    }

    public class TabFactory : ITabFactory
    {
        private readonly IUserConfigService _configService;
        private readonly ResourceViewModel _resources;

        public TabFactory(IUserConfigService configService, ResourceViewModel resources)
        {
            _configService = configService;
            _resources = resources;
        }

        public SweepTabViewModel CreateSweepTab() => new SweepTabViewModel(_configService, _resources);
        public DelayTabViewModel CreateDelayTab() => new DelayTabViewModel(_configService, _resources);
        public VITabViewModel CreateVITab() => new VITabViewModel(_configService, _resources);
    }
}
