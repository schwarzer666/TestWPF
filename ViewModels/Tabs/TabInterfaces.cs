namespace TemperatureCharacteristics.ViewModels.Tabs
{
    public interface ITabItemViewModel
    {
        string TabId { get; set; }
        string ItemHeader { get; set; }
        bool MeasureOn { get; set; }
        bool IsTabContentEnabled { get; set;  }
        //*************************************
        //検出復帰動作もしくは初期電圧設定でPGを選択した場合イベント発生
        //*************************************
        event EventHandler? DetectSourceIndexPGSelected;
    }
    //*************************************************
    //タブグループ（Sweep / Delay / VI）の共通インターフェイス
    //個々のタブ（ITabItemViewModel）をまとめて管理し、
    //測定処理に必要な情報（DTO やチェック状態）を提供する
    //*************************************************
    public interface ITabGroupViewModel
    {
        //*************************************
        // 子タブ（Item1, Item2, ...）の一覧
        //*************************************
        IReadOnlyList<ITabItemViewModel> Tabs { get; }
        //*************************************
        // 測定対象タブの名前一覧（"Item1", "Item3" など）
        //*************************************
        string[] CheckedTabNames { get; }
        //*************************************
        // 子タブ UI の有効/無効を一括制御するフラグ
        // 測定中、子タブの要素を触れなくる
        // MainViewModel側からの呼び出しで各タブに反映
        //*************************************
        void UpdateTabContentEnabled(bool enabled);
    }
}
