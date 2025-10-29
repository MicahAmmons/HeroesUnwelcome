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
    }
}
