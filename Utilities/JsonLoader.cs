using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using Heroes_UnWelcomed.Heroes;
using Heroes_UnWelcomed.AnimationFolder;
using Heroes_UnWelcomed.Encounters;
using Heroes_UnWelcomed.Data.SaveData;

namespace Heroes_UnWelcomed.Utilities
{
    internal class JsonLoader
    {
        public static string GetDataPath(params string[] parts)
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(new[] { basePath, "Data" }.Concat(parts).ToArray());
        }

        private static readonly string HeroDataPath = GetDataPath("Heroes", "HeroData.json");
        public static Dictionary<string, HeroData> LoadHeroData()
        {
            string json = File.ReadAllText(HeroDataPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
            return JsonSerializer.Deserialize<Dictionary<string, HeroData>>(json, options);
        }

        private static readonly string AnimationDataPath = GetDataPath("Animations", "AnimationData.json");
        internal static Dictionary<string, Dictionary<AnimationType, List<SpecificAnimationData>>> LoadAnimationData()
        {
            string json = File.ReadAllText(AnimationDataPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
            return JsonSerializer.Deserialize<Dictionary<string, Dictionary<AnimationType, List<SpecificAnimationData>>>>(json, options);
        }
        private static readonly string EncounterDataPath = GetDataPath("Encounters", "EncounterData.json");
        internal static Dictionary<string, EncounterData> LoadEncounterData()
        {
            string json = File.ReadAllText(EncounterDataPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
            return JsonSerializer.Deserialize<Dictionary<string, EncounterData>>(json, options);
        }
        private static readonly string SaveDataPath = GetDataPath("SaveData", "saveState.json");
        internal static SaveState LoadSaveData()
        {
            string json = File.ReadAllText(SaveDataPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
            return JsonSerializer.Deserialize<SaveState>(json, options);
        }
        internal static void SaveSaveData(SaveState saveState)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            };

            string json = JsonSerializer.Serialize(saveState, options);
            File.WriteAllText(SaveDataPath, json);
        }
    }
}
