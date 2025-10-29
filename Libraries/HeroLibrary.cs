using Heroes_UnWelcomed.Heroes;
using Heroes_UnWelcomed.Utilities;
using System.Collections.Generic;

namespace Heroes_UnWelcomed.Libraries
{
    public static class HeroLibrary
    {
        private static Dictionary<string, HeroData> _heroLibrary = new Dictionary<string, HeroData>();
        
        internal static void Initialize()
        {
            _heroLibrary = JsonLoader.LoadHeroData();
        }
        public static HeroData GetHeroData(string name)
        {
            return _heroLibrary[name];
        }
    }
}
