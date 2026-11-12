using System;

namespace Destrospean.Enums.HairTrouble
{
    [Flags]
    public enum HairGrowthStates
    {
        Unknown = 0,
        Bald = 1,
        Shaved = 2,
        Short = 4,
        Medium = 8,
        Long = 16,
        VeryLong = 32,
        Any = 63
    }
}
