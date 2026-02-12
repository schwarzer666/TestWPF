using System.Collections.ObjectModel;
using TemperatureCharacteristics.Models;

namespace TemperatureCharacteristics.ViewModels
{
    public class ResourceViewModel
    {
        public ObservableCollection<ComboItem> FuncItems { get; }
            = new ObservableCollection<ComboItem>(ComboItemMaster.FuncItems);

        public ObservableCollection<ComboItem> ModeItems { get; }
            = new ObservableCollection<ComboItem>(ComboItemMaster.ModeItems);

        public ObservableCollection<ComboItem> SourceRangeItems { get; }
            = new ObservableCollection<ComboItem>(ComboItemMaster.SourceRangeItems);

        public ObservableCollection<ComboItem> MeasureRangeItems { get; }
            = new ObservableCollection<ComboItem>(ComboItemMaster.MeasureRangeItems);

        public ObservableCollection<ComboItem> OscRangeItems { get; }
            = new ObservableCollection<ComboItem>(ComboItemMaster.OscRangeItems);

        public ObservableCollection<ComboItem> TrigSourceItems { get; }
            = new ObservableCollection<ComboItem>(ComboItemMaster.TrigSourceItems);

        public ObservableCollection<ComboItem> DmmTrigSourceItems { get; }
            = new ObservableCollection<ComboItem>(ComboItemMaster.DmmTrigSourceItems);

        public ObservableCollection<ComboItem> DirectionalItems { get; }
            = new ObservableCollection<ComboItem>(ComboItemMaster.DirectionalItems);

        public ObservableCollection<ComboItem> TrigDirectionalItems { get; }
            = new ObservableCollection<ComboItem>(ComboItemMaster.TrigDirectionalItems);

        public ObservableCollection<ComboItem> UnitsItems { get; }
            = new ObservableCollection<ComboItem>(ComboItemMaster.UnitsItems);

        public ObservableCollection<ComboItem> TimeUnits { get; }
            = new ObservableCollection<ComboItem>(ComboItemMaster.TimeUnits);

        public ObservableCollection<ComboItem> SourceItems { get; }
            = new ObservableCollection<ComboItem>(ComboItemMaster.SourceItems);

        public ObservableCollection<ComboItem> DetRelActItems { get; }
            = new ObservableCollection<ComboItem>(ComboItemMaster.DetRelActItems);

        public ObservableCollection<ComboItem> SourceLimitItems { get; }
            = new ObservableCollection<ComboItem>(ComboItemMaster.SourceLimitItems);

        public ObservableCollection<ComboItem> OutputCHItems { get; }
            = new ObservableCollection<ComboItem>(ComboItemMaster.OutputCHItems);

        public ObservableCollection<ComboItem> OutputZItems { get; }
            = new ObservableCollection<ComboItem>(ComboItemMaster.OutputZItems);

        public ObservableCollection<ComboItem> PolarityItems { get; }
            = new ObservableCollection<ComboItem>(ComboItemMaster.PolarityItems);

        public ObservableCollection<ComboItem> OutputOnOffItems { get; }
            = new ObservableCollection<ComboItem>(ComboItemMaster.OutputOnOffItems);

        public ObservableCollection<ComboItem> OscTimeRangeItems { get; }
            = new ObservableCollection<ComboItem>(ComboItemMaster.OscTimeRangeItems);

        public ObservableCollection<ComboItem> OscTDivUnitItems { get; }
            = new ObservableCollection<ComboItem>(ComboItemMaster.OscTDivUnitItems);

        public ObservableCollection<ComboItem> MeasureSourceItems { get; }
            = new ObservableCollection<ComboItem>(ComboItemMaster.MeasureSourceItems);
    }
}
