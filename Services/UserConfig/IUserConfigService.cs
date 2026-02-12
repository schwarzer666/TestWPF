using TemperatureCharacteristics.Models;
using TemperatureCharacteristics.ViewModels.Tabs;

namespace TemperatureCharacteristics.Services.UserConfig
{
    public interface IUserConfigService
    {
        string SweepAllFileName { get; }
        string DelayAllFileName { get; }
        string VIAllFileName { get; }
        //単一タブ置き換え用
        public Action<string, ITabItemViewModel>? ReplaceTabCallback { get; set; }
        //タブ単体からタブグループに変換
        public Func<ITabItemViewModel, ITabGroupViewModel?>? ResolveGroupFromTab { get; set; }
        //初期フォルダ受け渡し用
        public string DefaultFolder { get; set; }
        //現在開いているTabViewModel受け渡し用
        ITabItemViewModel? CurrentTabViewModel { get; set; }
        //単一タブ保存用
        Task SaveAsync(ITabItemViewModel tab);
        //全タブ保存用
        Task SaveAllAsync(ITabGroupViewModel tabGroup, string fileName);
        //単一タブ読込用
        Task LoadAsync(ITabItemViewModel tab, Action<string, ITabItemViewModel> replaceTab);
        //全タブ読込用
        Task<(bool success, string? errorMessage)> LoadAllAsync(ITabGroupViewModel tabGroup, Action<string, ITabItemViewModel> replaceTab);
        //プリセット適用
        Task ApplyPresetAsync(PresetItemBase preset);
        //将来的な実装展開用（Sweep,Delay,VI全タブ保存用）
        //Task SaveAllGroupsAsync(IEnumerable<ITabGroupViewModel> groups, string fileName);
    }
}
