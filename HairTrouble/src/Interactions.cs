using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Objects.Decorations;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Tuning = Sims3.Gameplay.Destrospean.HairTrouble.Settings;

namespace Destrospean.HairTrouble
{
    public class Interactions
    {
        private const string kLocalizationPath = "Destrospean/HairTrouble/Interactions";

        public class DecrementHairGrowthState : ImmediateInteraction<Sim, Sim>
        {
            public static InteractionDefinition Singleton = new Definition();

            public const string sLocalizationKey = kLocalizationPath + "/DecrementHairGrowthState";

            [DoesntRequireTuning]
            public class Definition : ImmediateInteractionDefinition<Sim, Sim, DecrementHairGrowthState>
            {
                public override string GetInteractionName(Sim actor, Sim target, InteractionObjectPair iop)
                {
                    return Localization.LocalizeString(target.IsFemale, sLocalizationKey + ":Name");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new[]
                    {
                        Localization.LocalizeString(isFemale, kLocalizationPath + ":Path")
                    };
                }

                public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return Tuning.kShowCheatInteractions;
                }
            }

            public override bool Run()
            {
                Target.SimDescription.DecrementHairGrowthState();
                return true;
            }
        }

        public class IncrementHairGrowthState : ImmediateInteraction<Sim, Sim>
        {
            public static InteractionDefinition Singleton = new Definition();

            public const string sLocalizationKey = kLocalizationPath + "/IncrementHairGrowthState";

            [DoesntRequireTuning]
            public class Definition : ImmediateInteractionDefinition<Sim, Sim, IncrementHairGrowthState>
            {
                public override string GetInteractionName(Sim actor, Sim target, InteractionObjectPair iop)
                {
                    return Localization.LocalizeString(target.IsFemale, sLocalizationKey + ":Name");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new[]
                    {
                        Localization.LocalizeString(isFemale, kLocalizationPath + ":Path")
                    };
                }

                public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return Tuning.kShowCheatInteractions;
                }
            }

            public override bool Run()
            {
                Target.SimDescription.IncrementHairGrowthState();
                return true;
            }
        }

        public class RemoveHairDye : Interaction<Sim, IMirror>
        {
            public static InteractionDefinition Singleton = new Definition();

            public const string sLocalizationKey = kLocalizationPath + "/RemoveHairDye";

            public class Definition : InteractionDefinition<Sim, IMirror, RemoveHairDye>
            {
                public override string GetInteractionName(Sim actor, IMirror target, InteractionObjectPair iop)
                {
                    return Localization.LocalizeString(actor.IsFemale, sLocalizationKey + ":Name");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new[]
                    {
                        Localization.LocalizeString(isFemale, kLocalizationPath + ":Path")
                    };
                }

                public override bool Test(Sim actor, IMirror target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    if (actor.SimDescription.HasOriginalOverallHairColors() || actor.CurrentOutfitCategory == OutfitCategories.Singed || actor.SimDescription.Age == CASAgeGenderFlags.Baby || actor.OccultManager.DisallowClothesChange())
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

        public class ResetOriginalHair : ImmediateInteraction<Sim, Sim>
        {
            public static InteractionDefinition Singleton = new Definition();

            public const string sLocalizationKey = kLocalizationPath + "/ResetOriginalHair";

            [DoesntRequireTuning]
            public class Definition : ImmediateInteractionDefinition<Sim, Sim, ResetOriginalHair>
            {
                public override string GetInteractionName(Sim actor, Sim target, InteractionObjectPair iop)
                {
                    return Localization.LocalizeString(target.IsFemale, sLocalizationKey + ":Name");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new[]
                    {
                        Localization.LocalizeString(isFemale, kLocalizationPath + ":Path")
                    };
                }

                public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return Tuning.kShowCheatInteractions;
                }
            }

            public override bool Run()
            {
                Target.SimDescription.ClearOriginalOverallHairColors();
                Target.SimDescription.InitOriginalOverallHairColors();
                return true;
            }
        }
    }
}
