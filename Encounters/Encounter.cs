using Heroes_UnWelcomed.AnimationFolder;
using Heroes_UnWelcomed.Cells;
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
        public const float DoorX = .5f; // of cell width
        public Encounter(string enc):base(enc) // this serves to generate the animcontr
        {
            Name = enc;
            GenerateChargeData();
        }
        public abstract void GenerateChargeData();
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
        public override void GenerateChargeData()
        {
            OrderOfCharges.Add(new DoorChargeData());
            OrderOfCharges.Add(new CombatChargeData());
            OrderOfCharges.Add(new ExitChargeData());
        }
    }
    public class LockedDoorEncounter : Encounter
    {
        public LockedDoorEncounter(EncounterData data, string name, Rectangle destRect) : base(name)
        {
            SetCurrentPositon(destRect);
        }

        public override void GenerateChargeData()
        {
            float x = CurrentPosition.X;
            float y = CurrentPosition.Y * (1 + GroundFloor);
            OrderOfCharges.Add(new TravelChargeData(new Vector2(x, y), new Vector2(x + (Cell.Width * DoorX), y)));
        }
    }
    public class HallwayEncounter : Encounter
    {
        public HallwayEncounter(EncounterData data, string name, Rectangle destRect):base(name)
        {
                SetCurrentPositon(destRect);
        }

        public override void GenerateChargeData()
        {
            float x = CurrentPosition.X;
            float y = CurrentPosition.Y + (Cell.Height * GroundFloor);
            OrderOfCharges.Add(new TravelChargeData(new Vector2(x,y), new Vector2(x + (Cell.Width * DoorX), y)));
        }
    }
}
    public class EncounterBunch
    {
        public List<ChargeData> EncCharges;
        public List<Encounter> Encounters { get; set; } = new List<Encounter> { };

        public EncounterBunch(EncounterData data, string key, Rectangle dest)
        {
        List<ChargeData> cellCharges = new List<ChargeData>();
            foreach (var enc in data.EncounterOrder)
            {
                Rectangle cellRect = dest;
                switch (enc)
                {
                    case EncounterType.Combat:
                    CombatEncounter combatEnc = new CombatEncounter(EncounterLibrary.GetEncounterData(key), key, dest);
                    cellCharges.AddRange(combatEnc.OrderOfCharges);
                    Encounters.Add(combatEnc);
                        break;
                    case EncounterType.Hallway:
                    HallwayEncounter hallwayEnc = new HallwayEncounter(data, "Hallway", dest);
                    cellCharges.AddRange(hallwayEnc.OrderOfCharges);
                    Encounters.Add(hallwayEnc);
                        break;
                    //case EncounterType.LockedDoor:
                    //LockedDoorEncounter lockedDoor = new LockedDoorEncounter(data, key, dest);
                    //    break;
                }
            }
        EncCharges = new List<ChargeData>(cellCharges);
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

