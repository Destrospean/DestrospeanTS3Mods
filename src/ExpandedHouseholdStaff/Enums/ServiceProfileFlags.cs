using System;

namespace Destrospean.Enums.ExpandedHouseholdStaff
{
    [Flags]
    public enum ServiceProfileFlags : ulong
    {
        LiveInService = 0x1,
        QuietAroundSleepingSims = 0x2,
        ReportsFires = 0x4,
        ScaredOfBonehilda = 0x8,
        WaitsBeforePuttingAwayLeftovers = 0x10,
        Recurrent = 0x20,
        EmergencyService = 0x40,
        AlwaysTryToSendTheSameSim = 0x80,
        BabysittingService = 0x100,
        RequireBeInSameRoom = 0x200,
        PreventSocialization = 0x400
    }
}
