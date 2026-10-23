using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Utilities;
using Sims3.Gameplay.Destrospean.Utils;
using Sims3.SimIFace;
using Sims3.UI;
using System;
using System.Collections.Generic;
using Destrospean.Utils;

namespace Destrospean.ExpandedHouseholdStaff.Interactions
{
    public class ListInteractions : ImmediateInteraction<Sim, GameObject>
    {
        [DoesntRequireTuning]
        public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ListInteractions>
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
                return !isAutonomous && target.Interactions.Exists(x => x.Tuning != null);
            }
        }

        public static readonly string LocalizationKey = typeof(ListInteractions).GetLocalizationKey();

        public static InteractionDefinition Singleton = new Definition();

        public override bool Run()
        {
            DebugUtils.TryDisplayScriptError(() =>
                {
                    foreach (InteractionObjectPair interaction in Target.Interactions)
                    {
                        if (interaction.Tuning != null)
                        {
                            string interactionName = null;
                            Exception exception;
                            DebugUtils.TryGetException(() => interactionName = interaction.InteractionDefinition.GetInteractionName(Actor, Target, interaction), out exception);
                            StyledNotification.Show(new StyledNotification.Format(Localization.LocalizeString(LocalizationKey + "/Headers:ObjectName") + Target.GetLocalizedName() + "\n" + Localization.LocalizeString(LocalizationKey + "/Headers:InteractionName") + interactionName + "\n" + Localization.LocalizeString(LocalizationKey + "/Headers:InteractionDefinitionType") + interaction.Tuning.FullInteractionName + "\n" + Localization.LocalizeString(LocalizationKey + "/Headers:TargetType") + interaction.Tuning.FullObjectName, StyledNotification.NotificationStyle.kSystemMessage));
                        }
                    }
                });
            return true;
        }
    }
}
