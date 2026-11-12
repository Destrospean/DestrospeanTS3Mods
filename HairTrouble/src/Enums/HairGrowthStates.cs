using System;

namespace Destrospean.Enums.HairTrouble
{
    [Flags]
    public enum HairGrowthStates
    {
        Unknown        = 0b000000,
        Any            = 0b111111,
        Bald           = 0b000001,
        Shaved         = 0b000010,
        ShavedOrShort  = 0b000110,
        ShavedToMedium = 0b001110,
        ShavedOrAbove  = 0b111110,
        Short          = 0b000100,
        ShortOrMedium  = 0b001100,
        ShortOrAbove   = 0b111100,
        Medium         = 0b001000,
        MediumOrLong   = 0b011000,
        MediumOrAbove  = 0b111000,
        Long           = 0b010000,
        LongOrAbove    = 0b110000,
        VeryLong       = 0b100000
    }
}
