using Destrospean.Utils;
using Destrospean.Utils.HairTrouble;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Destrospean.HairTrouble;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;

namespace Destrospean.HairTrouble.Interactions
{
    public class DeleteHairClassificationCollection : ImmediateInteraction<Sim, GameObject>
    {
        [DoesntRequireTuning]
        public class Definition : ImmediateInteractionDefinition<Sim, GameObject, DeleteHairClassificationCollection>
        {
            public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair iop)
            {
                return Localization.LocalizeString(actor.IsFemale, LocalizationKey + ":Name");
            }

            public override string[] GetPath(bool isFemale)
            {
                return new[]
                {
                    Localization.LocalizeString(isFemale, LocalizationKey.Replace("/DeleteHairClassificationCollection", ":Path"))
                };
            }

            public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                return Settings.kShowCheatInteractions && !isAutonomous;
            }
        }

        public static readonly string LocalizationKey = typeof(DeleteHairClassificationCollection).GetLocalizationKey();

        public static InteractionDefinition Singleton = new Definition();

        public override bool Run()
        {
            FileUtils.Singleton.DeleteFile();
            return true;
        }
    }
}
