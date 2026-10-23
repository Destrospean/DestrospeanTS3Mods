using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces.Destrospean.ExpandedHouseholdStaff;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Destrospean.Utils;
using Destrospean.Utils.ExpandedHouseholdStaff;

namespace Destrospean.ExpandedHouseholdStaff.Interactions
{
    public class AddAutonomousInteraction : ImmediateInteraction<Sim, GameObject>
    {
        [DoesntRequireTuning]
        public class Definition : ImmediateInteractionDefinition<Sim, GameObject, AddAutonomousInteraction>
        {
            public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair iop)
            {
                return Localization.LocalizeString(actor.IsFemale, LocalizationKey + ":Name");
            }

            public override string[] GetPath(bool isFemale)
            {
                return new[]
                {
                    Localization.LocalizeString(isFemale, LocalizationKey + ":Path")
                };
            }

            public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                return !isAutonomous && ServiceUtils.ServiceProfiles.Exists(x => !x.IsImmutable);
            }
        }

        public static readonly string LocalizationKey = typeof(AddAutonomousInteraction).GetLocalizationKey();

        public static InteractionDefinition Singleton = new Definition();

        public override bool Run()
        {
            IServiceProfile[] profiles;
            if (ServiceUtils.TryUIGetSelectedServiceProfiles(out profiles, ServiceUtils.ServiceProfiles.FindAll(x => !x.IsImmutable).ToArray(), Localization.LocalizeString(LocalizationKey + ":Name"), 1))
            {
                profiles[0].TryUIAddOutput();
            }
            return true;
        }
    }
}
