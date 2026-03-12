using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace TycoonGame.Scripts
{
    public static class SaveSystem
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public static string SavesDirectory
            => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TycoonGame", "Saves");

        public static string GetSlotPath(int slot) => Path.Combine(SavesDirectory, $"slot{slot}.json");

        public static void SaveSlot(int slot, GameSaveData data)
        {
            Directory.CreateDirectory(SavesDirectory);
            data.SavedAtUtc = DateTime.UtcNow;
            File.WriteAllText(GetSlotPath(slot), JsonSerializer.Serialize(data, JsonOptions));
        }

        public static GameSaveData? LoadSlot(int slot)
        {
            var path = GetSlotPath(slot);
            if (!File.Exists(path)) return null;

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<GameSaveData>(json);
        }

        public static bool SlotExists(int slot) => File.Exists(GetSlotPath(slot));

        public static void DeleteSlot(int slot)
        {
            var path = GetSlotPath(slot);
            if (File.Exists(path)) File.Delete(path);
        }
    }

    public sealed class GameSaveData
    {
        public string SaveName { get; set; } = "NoName";
        public decimal Money { get; set; }
        public int Population { get; set; }
        public List<LotSaveData> Lots { get; set; } = new();
        public DateTime SavedAtUtc { get; set; }
    }

    public sealed class LotSaveData
    {
        public string BuildingKey { get; set; } = string.Empty;
        public string BuildingName { get; set; } = string.Empty;
        public int Level { get; set; } = 1;
        public decimal IncomePerSec { get; set; }
        public int PopulationGain { get; set; }
        public string Sprite { get; set; } = string.Empty;
    }
}
