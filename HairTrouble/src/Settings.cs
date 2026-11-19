using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;

namespace Sims3.Gameplay.Destrospean.HairTrouble
{
    public class Settings
    {
        [Tunable]
        public static bool kEnableHairGrowthForInactiveSims = true;
        
        [Tunable]
        public static bool kEnforceHairClassificationsInCAS = true;
        
        [Tunable]
        public static bool kExportHairClassificationCollectionsAsXMLs;
        
        [Tunable]
        public static float kHairGrowthInterval = 1f;
        
        [Tunable]
        public static TimeUnit kHairGrowthIntervalUnit = TimeUnit.Days;
        
        [Tunable]
        public static bool kShowCheatInteractions;
    }
}
