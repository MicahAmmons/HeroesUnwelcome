using System;

namespace Heroes_UnWelcomed.Randomness
{
    internal static class RandomHut
    {
        private static readonly Random _rng = new Random();
        public static Random Rng => _rng;

        public static int Next(int min, int max) => _rng.Next(min, max);
        public static int Next(int max) => _rng.Next(0, max);
        public static double NextDouble() => _rng.NextDouble();
    }
}
