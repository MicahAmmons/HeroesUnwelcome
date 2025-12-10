using Heroes_UnWelcomed.AnimationFolder;
using Heroes_UnWelcomed.Assets;
using Heroes_UnWelcomed.Buttons;
using Heroes_UnWelcomed.Cells;
using Heroes_UnWelcomed.Charges;
using Heroes_UnWelcomed.InputTracker;
using Heroes_UnWelcomed.Libraries;
using Heroes_UnWelcomed.Randomness;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heroes_UnWelcomed.Heroes
{
    internal static class HeroManager
    {
        private static List<Hero> _standbyHeroes = new List<Hero>();
        private static List<PartyController> _allParties = new List<PartyController> { };
        private static List<ChargeData> _chargeMasterList = new List<ChargeData>();

        private static int maxPartyCapacity = 1;
        public static void Initialize()
        {
            _standbyHeroes.Add(new Hero("Hero"));
            _standbyHeroes.Add(new Hero("Hero"));
            _standbyHeroes.Add(new Hero("Hero"));
            _standbyHeroes.Add(new Hero("Hero"));
            _standbyHeroes.Add(new Hero("Hero"));
            // _standbyHeroes.Add(new Hero("Goblin"));
            //  GenerateParty();
            CellManager.EncounterAdded += AddChargesData;
        }
        private static void AddChargesData(List<ChargeData> charges)
        {
            _chargeMasterList.AddRange(charges);
        }
        private static void GenerateParty()
        {
            List<Hero> newParty = new List<Hero>();
            for (int i = 0; i< maxPartyCapacity; i++)
            {
                Hero hero = _standbyHeroes[RandomHut.Next(0, _standbyHeroes.Count)];
                newParty.Add(hero);
                _standbyHeroes.Remove(hero);
            }
            PartyController party = new PartyController(newParty);
            party.SetStartingPositions();
            _allParties.Add(party);
        }


        public static void Update(float delta)
        {
            UpdateAllParties(delta);
            CheckForSpawn();
        }
        private static bool _started = false;
        private static void CheckForSpawn()
        {
            if (_started) return;
            bool pressed = TapTap.IsKeyDown(Keys.G);
            if (pressed)
            {
                _started = true;
                GenerateParty();
            }
        }
        private static void UpdateAllParties(float delta)
        {
            if (_allParties.Count <= 0) return;
            foreach (var contr in  _allParties)
            {
                contr.Update(delta);
            }
        }
        internal static void Draw(SpriteBatch s)
        {
            foreach (var contr in _allParties)
                contr.DrawHeroes(s);
        }
    }
}
