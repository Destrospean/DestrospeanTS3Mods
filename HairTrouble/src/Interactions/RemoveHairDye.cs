using Destrospean.HairTrouble;
using Destrospean.Utils;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Objects.Decorations;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;

namespace Sims3.Gameplay.Destrospean.HairTrouble.Interactions
{
    public class RemoveHairDye : Interaction<Sim, IMirror>
    {
        public class Definition : InteractionDefinition<Sim, IMirror, RemoveHairDye>
        {
            public override string GetInteractionName(Sim actor, IMirror target, InteractionObjectPair iop)
            {
                return Localization.LocalizeString(actor.IsFemale, LocalizationKey + ":Name");
            }

            public override string[] GetPath(bool isFemale)
            {
                return new[]
                {
                    Localization.LocalizeString(isFemale, LocalizationKey.Replace("/RemoveHairDye", ":Path"))
                };
            }

            public override bool Test(Sim actor, IMirror target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                if (actor.SimDescription == null || actor.SimDescription.HasOriginalOverallHairColors() || actor.CurrentOutfitCategory == OutfitCategories.Singed || actor.SimDescription.Age == CASAgeGenderFlags.Baby || actor.OccultManager.DisallowClothesChange())
                {
                    return false;
                }
                if (actor.BuffManager.HasTransformBuff())
                {
                    greyedOutTooltipCallback = CreateTooltipCallback(Localization.LocalizeString(actor.IsFemale, "Gameplay/Buffs/BuffTransformation:CantChangeClothesTooltip", actor));
                    return false;
                }
                return true;
            }
        }

        public static readonly string LocalizationKey = typeof(RemoveHairDye).GetLocalizationKey();

        public static InteractionDefinition Singleton = new Definition();

        public override bool Run()
        {
            if (!Target.Route(this))
            {
                return false;
            }
            StandardEntry();
            Mirror.EnterStateMachine(this);
            AnimateSim("ChangeAppearance");
            if (!Actor.IsSelectable)
            {
                StandardExit();
                return false;
            }
            Actor.SimDescription.ApplyOverallHairColorsToAllOutfits(Actor.SimDescription.GetOriginalBodyHairColor(), Actor.SimDescription.GetOriginalEyebrowColor(), Actor.SimDescription.GetOriginalFacialHairColors(), Actor.SimDescription.GetOriginalHairColors(), true);
            StandardExit();
            return true;
        }
    }
}
