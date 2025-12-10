using Heroes_UnWelcomed.Encounters;
using Heroes_UnWelcomed.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace Heroes_UnWelcomed.Data.SaveData
{
    public static class SaveStateLibrary
    {
        private static SaveState _saveState;
        public static Action EncounterSaveStateUpdated;

        public static void Initialize()
        {
            _saveState = JsonLoader.LoadSaveData();
        }
        private static void NotifyEncounterSaveStateUpdated()
        {
            EncounterSaveStateUpdated?.Invoke();
        }
        public static bool IsEncounterUnlocked(string name)
        {
            if (_saveState?.Encounters == null)
                throw new InvalidOperationException("Save state not initialized or invalid.");

            EncounterSaveData encounter = _saveState.Encounters;

            foreach (var dict in new[]
            {
             encounter.Combat,
             encounter.Puzzle,
             encounter.Trap,
             encounter.LockedDoor,
             encounter.SpawnIn
            })
            {
                if (dict != null && dict.TryGetValue(name, out bool unlocked))
                    return unlocked;
            }

            throw new KeyNotFoundException($"Encounter '{name}' not found in any save dictionary.");
        }
        public static void UnlockEncounter(string name)
        {
            var encounter = _saveState.Encounters;
            var dict = new[]{
            encounter.Combat,
            encounter.Puzzle,
            encounter.Trap,
            encounter.LockedDoor };
            foreach (var d in dict)
            {
                if (d.ContainsKey(name))
                {
                    d[name] = true;
                    UpdateSaveFile();
                    NotifyEncounterSaveStateUpdated();
                    return;
                }
            }
        }
        private static void UpdateSaveFile()
        {
            JsonLoader.SaveSaveData(_saveState);
        }
        public static List<EncounterType> ReturnUnlockedEncounterCategories()
        {
            var categories = new List<EncounterType>();
            var encounter = _saveState.Encounters;

            var dicts = new (Dictionary<string, bool> Dict, EncounterType Type)[]
            {
    (encounter.Combat, EncounterType.Combat),
    (encounter.Puzzle, EncounterType.Puzzle),
    (encounter.Trap, EncounterType.Trap),
    (encounter.LockedDoor, EncounterType.LockedDoor),
    (encounter.SpawnIn, EncounterType.SpawnIn),
            };

            foreach (var (dict, name) in dicts)
            {
                if (dict != null && dict.Any(kvp => kvp.Value))
                {
                    categories.Add(name);
                }
            }

            return categories;
        }

    }
}
