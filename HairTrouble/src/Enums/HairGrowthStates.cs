using System;

namespace Destrospean.Enums.HairTrouble
{
    [Flags]
    public enum HairGrowthStates
    {
        Unknown,
        Any = 0x3F,
        Bald = 0x1,
        Shaved,
        Short = 0x4,
        ShortOrAbove = 0x3C,
        Medium = 0x8,
        MediumOrAbove = 0x38,
        Long = 0x10,
        LongOrAbove = 0x30,
        VeryLong = 0x20
    }
}
