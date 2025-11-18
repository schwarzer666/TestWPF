namespace TemperatureCharacteristics.Models
{
    //*************************************************
    //定義
    // 名前空間の定義Combobox用Item
    //*************************************************
    public class ComboItem
    {
        public string? Name { get; set; }
        public string? Tag { get; set; }
        public override string? ToString() => Name;
    }
}