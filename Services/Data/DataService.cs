using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using TemperatureCharacteristics.Models;
using Microsoft.Win32;              //JSONファイルのRead/Writeに必要

namespace TemperatureCharacteristics.Services.Data
{
    //*********************
    //インターフェイス定義
    //*********************
    public interface IJsonDataService
    {
        (ObservableCollection<PresetItemBase>, string, string) LoadItems<T>(string initialFolder = null, string filePath = null) where T : PresetItemBase;
        (bool, string, string) SaveItems<T>(string initialFolder, string filePath = null, ObservableCollection<T> items = null) where T : PresetItemBase;
    }

    public class DataService : IJsonDataService
    {
        //*************************************************
        //JSON読み込みメソッド
        //*************************************************
        public (ObservableCollection<PresetItemBase>, string, string) LoadItems<T>(
                                                                            string initialFolder = null, 
                                                                            string? filePath = null) where T : PresetItemBase
        {
            var result = new ObservableCollection<PresetItemBase>();
            string log = string.Empty;
            string selectedPath = filePath;
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    var openFileDialog = new OpenFileDialog
                    {
                        Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                        DefaultExt = "json",
                        Title = "読み込むファイルを選択してください",
                        InitialDirectory = Directory.Exists(initialFolder)
                            ? initialFolder
                            : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                    };

                    if (openFileDialog.ShowDialog() == true)
                        selectedPath = openFileDialog.FileName;
                    else
                    {
                        log += "ファイル選択がキャンセルされました";
                        return (result, log, null);
                    }
                }
                if (!File.Exists(selectedPath))
                {
                    log += $"ファイル {selectedPath} が見つかりません";
                    return (result, log, null);
                }

                var json = File.ReadAllText(selectedPath);
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,             //JSONのプロパティ名の大文字/小文字を無視
                    Converters = { new JsonStringEnumConverter() }
                };

                //JSON ルートの "items" 配列を取得
                using var jsonDoc = JsonDocument.Parse(json);
                if (!jsonDoc.RootElement.TryGetProperty("items", out var itemsElement) || itemsElement.ValueKind != JsonValueKind.Array)
                {
                    log += "JSON 形式エラー: 'items' 配列が見つかりません";
                    return (result, log, null);
                }

                foreach (var item in itemsElement.EnumerateArray())
                {
                    if (item.TryGetProperty("Type", out var typeElement) && typeElement.ValueKind == JsonValueKind.String)
                    {
                        string type = typeElement.GetString();
                        PresetItemBase preset = type switch
                        {
                            "Sweep" => JsonSerializer.Deserialize<PresetItemSweep>(item.GetRawText(), jsonOptions),
                            "Delay" => JsonSerializer.Deserialize<PresetItemDelay>(item.GetRawText(), jsonOptions),
                            "VI" => JsonSerializer.Deserialize<PresetItemVI>(item.GetRawText(), jsonOptions),
                            _ => null
                        };

                        if (preset != null)
                            result.Add(preset);
                        else
                            log += $"無効なプリセット型: {type}";
                    }
                    else
                        log += "JSON 形式エラー: 'Type' プロパティが見つかりません";
                }
                log += $"設定を {result.Count} 件読み込みました (Path: {filePath})";
                return (result, log, selectedPath);
            }
            catch (JsonException ex)
            {
                log += $"JSON パースエラー: {ex.Message}";
                return (result, log, null);
            }
            catch (Exception ex)
            {
                log += $"エラー: 設定の読み込みに失敗しました ({ex.Message})";
                return (result, log, null);
            }
        }
        //*************************************************
        //JSON保存メソッド
        //*************************************************
        public (bool, string, string) SaveItems<T>(
                                                string initialFolder, 
                                                string fileName = null, 
                                                ObservableCollection<T> items = null) where T : PresetItemBase
        {
            string log = string.Empty;
            string selectedPath = null;
            try
            {
                if (items == null || !items.Any())
                {
                    log += "エラー: 保存するデータがありません";
                    return (false, log, null);
                }
                //SaveFileDialogを表示
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                    DefaultExt = "json",
                    FileName = fileName,
                    Title = "保存先を選択してください",
                    InitialDirectory = Directory.Exists(initialFolder)
                        ? initialFolder
                        : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };

                if (saveFileDialog.ShowDialog() == true)
                    selectedPath = saveFileDialog.FileName;
                else
                {
                    log += "保存がキャンセルされました";
                    return (false, log, null);
                }
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.Never,     //null値もJSONに含める
                    Converters = { new JsonStringEnumConverter() }
                };
                //書き込み前に読み取り専用解除
                if (File.Exists(selectedPath))
                {
                    File.SetAttributes(selectedPath, File.GetAttributes(selectedPath) & ~FileAttributes.ReadOnly);
                }
                var json = JsonSerializer.Serialize(new { items }, jsonOptions);
                File.WriteAllText(selectedPath, json);
                //書き込み後クリア
                json = null;
                log += $"設定を {selectedPath} に保存しました";
                return (true, log, selectedPath);
            }
            catch (Exception ex)
            {
                log += $"エラー: 設定の保存に失敗しました ({ex.Message})";
                return (false, log, null);
            }
        }
    }
}
