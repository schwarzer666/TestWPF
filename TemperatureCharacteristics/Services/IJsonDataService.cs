using System.Collections.ObjectModel;
using TemperatureCharacteristics.Models;

namespace TemperatureCharacteristics.Services
{
    //*********************
    //インターフェイス定義
    //*********************
    public interface IJsonDataService
    {
        (ObservableCollection<PresetItemBase>, string, string) LoadItems<T>(string filePath = null) where T : PresetItemBase;
        (bool, string, string) SaveItems<T>(string filePath = null, ObservableCollection<T> items = null) where T : PresetItemBase;
    }
}
