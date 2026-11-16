using System.Collections.Generic;
using Destrospean.Enums.HairTrouble;
using Sims3.SimIFace;

namespace Destrospean.HairTrouble
{
    public static class SimHairData
    {
        [PersistableStatic(true)]
        public static Dictionary<ulong, HairGrowthStates> GrowthStates = new Dictionary<ulong, HairGrowthStates>();
        
        [PersistableStatic(true)]
        public static Dictionary<ulong, bool> RootsShowing = new Dictionary<ulong, bool>();
    }
}
