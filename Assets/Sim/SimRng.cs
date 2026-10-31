// The rules' dice, bit for bit the web game's src/game/simRng.ts (Mulberry32 on a uint state). Every random
// roll the Hatchfield rules make comes from here; uint arithmetic wraps exactly like JavaScript's Math.imul and >>> 0.
namespace Hatchfield.Sim
{
    public sealed class SimRng
    {
        public uint State;

        public SimRng(uint seed) { State = seed; }

        /// <summary>A number in [0, 1), the same sequence as the web game for the same seed.</summary>
        public double Next()
        {
            unchecked
            {
                State += 0x6D2B79F5u;
                uint t = State;
                t = (t ^ (t >> 15)) * (t | 1u);
                t ^= t + (t ^ (t >> 7)) * (t | 61u);
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        }
    }
}
