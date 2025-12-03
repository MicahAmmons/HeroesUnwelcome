using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Heroes_UnWelcomed.Charges
{
    public abstract class ChargeData
    {
        public ChargeType Type { get; set; }
        public List<Vector2> TravelPath = new List<Vector2>();
        public virtual List<Vector2> GeneratePath(Vector2 start, Vector2 end)
        {
            int steps = 50;
            List<Vector2> path = new List<Vector2>(steps);

            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector2 point = Vector2.Lerp(start, end, t);
                path.Add(point);
            }

            return path;
        }
    }
    public class CombatChargeData : ChargeData
    { 

    }
    public class TravelChargeData : ChargeData
    {

        public TravelChargeData(Vector2 start, Vector2 end)
        {
            Type = ChargeType.Travel;
            TravelPath = GeneratePath(start, end);
        }
    }
    public class ExitChargeData : ChargeData
    {

    }
    public class  DoorChargeData : ChargeData
    {
        
    }
}


