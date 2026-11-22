using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.SimIFace;

namespace Destrospean.Utils
{
    public static class MotiveUtils
    {
        public static void LoadMotive(string instanceName)
        {
            MotiveTuning.LoadTuning(Simulator.LoadXML(instanceName));
        }
        
        public static void UpdateMotiveTunings(Sim sim, CommodityKind commodityKind)
        {
            var allTunings = MotiveTuning.GetAllTunings(commodityKind);
            if (allTunings == null)
            {
                return;
            }
            MotiveTuning motiveTuning = null;
            var score = float.MinValue;
            foreach (var tuning in allTunings)
            {
                var tempScore = sim.ScoreMotiveTuning(tuning);
                if (tempScore > score)
                {
                    score = tempScore;
                    motiveTuning = tuning;
                }
            }
            if (motiveTuning != null)
            {
                sim.mMotiveTuning[(int)commodityKind] = motiveTuning;
            }
        }
    }
}
