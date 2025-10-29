using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heroes_UnWelcomed.AnimationFolder
{
    public abstract class Moving : Animatable
    {
        public List<Vector2> MovePath = new List<Vector2>();
        public List<Vector2> DestinationPoints = new List<Vector2>();
        public Moving(string animationData): base(animationData)
        {

        }
        public void MoveEntity()
        {
            if (MovePath.Count > 0)
                HandlePathProgression();
            if (DestinationPoints.Count > 0)
            {   List<Vector2> destPoints = DestinationPoints.ToList(); 
                DestinationPoints.Clear();
                CreateMovePath(destPoints);}
                


        }

        private void CreateMovePath(List<Vector2> destPoints)
        {
            foreach (var dest in  destPoints)
            {

            }
        }

        private void HandlePathProgression()
        {
            throw new NotImplementedException();
        }
    }
}
