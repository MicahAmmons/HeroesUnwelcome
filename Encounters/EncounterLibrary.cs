using Heroes_UnWelcomed.Data.SaveData;
using Heroes_UnWelcomed.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heroes_UnWelcomed.Encounters
{
    public static class EncounterLibrary
    {
        private static Dictionary<string, EncounterData> _data = new Dictionary<string, EncounterData>();

        public static void Initialize()
        {
            _data = JsonLoader.LoadEncounterData();
        }
        public static Dictionary<string, EncounterData> ReturnUnlockedDictionary()
        {
            var unlocked = new Dictionary<string, EncounterData>();

            foreach (var kvp in _data)
            {
                try
                {
                    if (SaveStateLibrary.IsEncounterUnlocked(kvp.Key))
                        unlocked.Add(kvp.Key, kvp.Value);
                }
                catch (KeyNotFoundException)
                {
                }
            }

            return unlocked;
        }
        public static EncounterData GetEncounterData(string name)
        {
            return _data[name];
        }
        internal static List<string> ReturnUnlockedCategories()
        {
            var unlocked = new List<string>();

            foreach (var kvp in _data)
            {
                if (SaveStateLibrary.IsEncounterUnlocked(kvp.Key))
                {
                    string cat = kvp.Value.Category.ToString();
                    if (!unlocked.Contains(cat))
                    {
                        unlocked.Add(cat);
                    }
                }
            }
            return unlocked;
        }
    }
}
