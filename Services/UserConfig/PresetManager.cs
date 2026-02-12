using System.Collections.ObjectModel;
using TemperatureCharacteristics.Models;
using TemperatureCharacteristics.Services.Utility;
using TemperatureCharacteristics.ViewModels.Tabs;
using TemperatureCharacteristics.ViewModels.Tabs.Delay;
using TemperatureCharacteristics.ViewModels.Tabs.Sweep;
using TemperatureCharacteristics.ViewModels.Tabs.VI;

namespace TemperatureCharacteristics.Services.UserConfig
{
    public class PresetManager
    {
        //*************************************************
        //呼び出し元ログ伝搬用
        //*************************************************
        private readonly Action<string> _log;
        public ITabFactory? TabFactory { get; set; }
        public PresetManager(Action<string> logAction)
        {
            _log = logAction;
        }

        //*************************************************
        //プリセット適用時に上書きしてはいけないプロパティを共通化
        //*************************************************
        private static readonly string[] IgnoreList = new[]
        {
            "Id",          // プリセットID
            "Type",        // Sweep/Delay/VI の種別
            "TabId",       // タブ固有の識別子
            "MeasureOn",   // UI 状態（チェックON/OFF）
        };
        //*************************************************
        // タブ（ViewModel） → プリセット（PresetItem）を生成
        //*************************************************
        public PresetItemBase? CreatePresetFromTab(
                                        object tabViewModel,
                                        ObservableCollection<PresetItemBase> existingConfigs)
        {
            if (tabViewModel == null)
            {
                _log("エラー: parameter is null");
                return null;
            }
            existingConfigs ??= new ObservableCollection<PresetItemBase>();

            string nextId = GenerateNextId(existingConfigs, tabViewModel);

            switch (tabViewModel)
            {
                case SweepTabViewModel sweep:
                    var sweepConfig = new PresetItemSweep
                    {
                        Id = nextId,
                        Name = sweep.ItemHeader ?? "Item",
                        BackgroundColor = "#FFFFFF",
                        Type = "Sweep"
                    };
                    PropertyCopyUtil.CopyPropertiesRecursive(sweep, sweepConfig, IgnoreList);
                    return sweepConfig;

                case DelayTabViewModel delay:
                    var delayConfig = new PresetItemDelay
                    {
                        Id = nextId,
                        Name = delay.ItemHeader ?? "Item",
                        BackgroundColor = "#FFFFFF",
                        Type = "Delay"
                    };
                    PropertyCopyUtil.CopyPropertiesRecursive(delay, delayConfig, IgnoreList);
                    return delayConfig;

                case VITabViewModel vi:
                    var viConfig = new PresetItemVI
                    {
                        Id = nextId,
                        Name = vi.ItemHeader ?? "Item",
                        BackgroundColor = "#FFFFFF",
                        Type = "VI"
                    };
                    PropertyCopyUtil.CopyPropertiesRecursive(vi, viConfig, IgnoreList);
                    return viConfig;

                default:
                    _log($"無効なタブ型: {tabViewModel.GetType().Name}");
                    return null;
            }
        }

        //*************************************************
        // プリセット → タブへプロパティをコピーして適用
        //*************************************************
        public void ApplyPresetToTab(PresetItemBase preset, object tab)
        {
            if (preset == null || tab == null)
                return;

            PropertyCopyUtil.CopyPropertiesRecursive(
                source: preset,
                target: tab,
                ignoreList: IgnoreList
            );
        }

        //*************************************************
        // ID 採番
        //*************************************************
        private string GenerateNextId(ObservableCollection<PresetItemBase> configs, object tab)
        {
            int baseId = tab switch
            {
                SweepTabViewModel => 300,
                DelayTabViewModel => 400,
                VITabViewModel => 500,
                _ => 999
            };

            if (!configs.Any())
                return baseId.ToString();

            int maxId = configs
                .Select(c => int.TryParse(c.Id, out int id) ? id : baseId)
                .Max();

            return (maxId + 1).ToString();
        }

        //*************************************************
        // プリセット（PresetItem） → タブ（ViewModel）を生成
        //*************************************************
        public ITabItemViewModel? CreateTabFromPreset(PresetItemBase preset)
        {
            if (TabFactory == null)
                throw new InvalidOperationException("TabFactory が設定されていません");
            switch (preset)
            {
                case PresetItemSweep s:
                    var sweep = TabFactory.CreateSweepTab();
                    ApplyPresetToTab(s, sweep);
                    return sweep;

                case PresetItemDelay d:
                    var delay = TabFactory.CreateDelayTab();
                    ApplyPresetToTab(d, delay);
                    return delay;

                case PresetItemVI v:
                    var vi = TabFactory.CreateVITab();
                    ApplyPresetToTab(v, vi);
                    return vi;
            }
            return null;
        }

        //*************************************************
        // タブ（ViewModel）の内容を置き換え
        //*************************************************
        public void ReplaceTab(object targetTab, PresetItemBase preset)
        {
            if (targetTab == null || preset == null)
                return;

            switch (preset)
            {
                case PresetItemSweep sweepPreset when targetTab is SweepTabViewModel sweepTab:
                    ApplyPresetToTab(sweepPreset, sweepTab);
                    break;

                case PresetItemDelay delayPreset when targetTab is DelayTabViewModel delayTab:
                    ApplyPresetToTab(delayPreset, delayTab);
                    break;

                case PresetItemVI viPreset when targetTab is VITabViewModel viTab:
                    ApplyPresetToTab(viPreset, viTab);
                    break;

                default:
                    _log($"ReplaceTab: 型が一致しません preset={preset.GetType().Name}, tab={targetTab.GetType().Name}");
                    break;
            }
        }

    }
}
