using Random = System.Random;

namespace Level.Generation.Support
{
    public class SeedManager
    {
        private readonly Random _rng;
        public int Seed { get; }
        
        public SeedManager(int seed)
        {
            Seed = seed;
            _rng = new Random(seed);
        }
        
        public Random GetRng() => _rng;
        
        public int NextInt() => _rng.Next();
        public int NextInt(int maxExclusive) => _rng.Next(maxExclusive);
        public int NextInt(int minInclusive, int maxExclusive) => _rng.Next(minInclusive, maxExclusive);
        public float NextFloat() => (float)_rng.NextDouble();
        public bool NextBool() => _rng.Next(2) == 1;
        public bool Chance(float probability) => NextFloat() < probability;
    }
}