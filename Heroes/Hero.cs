using Heroes_UnWelcomed.AnimationFolder;

namespace Heroes_UnWelcomed.Heroes
{
    public class Hero : Moving
    {

        public Hero(HeroData data) : base(data.Animation)
        {

        }
    }
}
