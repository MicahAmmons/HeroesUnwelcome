using Heroes_UnWelcomed.AnimationFolder;
using Heroes_UnWelcomed.Charges;
using Heroes_UnWelcomed.Encounters;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Heroes_UnWelcomed.Encounters
{
    public abstract class Encounter : Animatable
    {
        public EncounterType Category { get; set; }
        public List<ChargeData> OrderOfCharges { get; set; } = new List<ChargeData> { };
        public string Name { get; set; }
        public const float GroundFloor = .8f;
        public Encounter(string enc):base(enc) // this serves to generate the animcontr
        {
            Name = enc;
        }
        public virtual void SetCurrentPositon(Rectangle cellRect)
        {
            CurrentPosition = new Vector2(cellRect.X, cellRect.Y);
        }
        public virtual void Draw(SpriteBatch s)
        {
            AnimContr?.Draw(s, CurrentPosition);
        }
    }
    public class CombatEncounter : Encounter
    {
        public int AttackPower { get; set; }
        public CombatEncounter(EncounterData data, string encounter, Rectangle destRect) : base(encounter)
        {
            Category = EncounterType.Combat;
            Name = encounter;
            AttackPower = data.AttackPower;
            SetCurrentPositon(destRect);
        }
    }
    public class HallwayEncounter : Encounter
    {
        public HallwayEncounter(EncounterData data, string name, Rectangle destRect):base(name)
        {
                SetCurrentPositon(destRect);
        }
    }
}
    public class EncounterBunch
    {
        public List<Encounter> Encounters { get; set; } = new List<Encounter> { };
        public EncounterBunch(EncounterData data, string key, Rectangle dest)
        {
            foreach (var enc in data.EncounterOrder)
            {
                Rectangle cellRect = dest;
                switch (enc)
                {
                    case EncounterType.Combat:
                        Encounters.Add(new CombatEncounter(EncounterLibrary.GetEncounterData(key), key, dest));
                        break;
                    case EncounterType.Hallway:
                        Encounters.Add(new HallwayEncounter(data, "Hallway", dest));
                        break;
                }

            }
        }
    internal void Draw(SpriteBatch s)
    {
        foreach (var enc in Encounters)
        {
            enc.DrawAnimatable(s);
        }
    }
    internal void UpdateEncounters(float delta)
    {
        foreach (var enc in Encounters)
        {
            enc.UpdateAnimatable(delta);
        }
    }
}

