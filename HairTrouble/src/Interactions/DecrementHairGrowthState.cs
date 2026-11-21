using Destrospean.Utils;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Destrospean.HairTrouble;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;

namespace Destrospean.HairTrouble.Interactions
{
    public class DecrementHairGrowthState : ImmediateInteraction<Sim, Sim>
    {
        [DoesntRequireTuning]
        public class Definition : ImmediateInteractionDefinition<Sim, Sim, DecrementHairGrowthState>
        {
            public override string GetInteractionName(Sim actor, Sim target, InteractionObjectPair iop)
            {
                return Localization.LocalizeString(target.IsFemale, LocalizationKey + ":Name");
            }

            public override string[] GetPath(bool isFemale)
            {
                return new[]
                {
                    Localization.LocalizeString(isFemale, LocalizationKey.Replace("/DecrementHairGrowthState", ":Path"))
                };
            }

            public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                return Settings.kShowCheatInteractions && !isAutonomous;
            }
        }

        public static readonly string LocalizationKey = typeof(DecrementHairGrowthState).GetLocalizationKey();

        public static InteractionDefinition Singleton = new Definition();

        public override bool Run()
        {
            Target.SimDescription.DecrementHairGrowthState();
            return true;
        }
    }
}
