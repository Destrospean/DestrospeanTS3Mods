using System;
using System.Collections.Generic;
using Destrospean.OutfitAssignment.UI.Dialogs;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;

namespace Destrospean.OutfitAssignment.Interactions
{
    public class AssignOutfitCategoryToInteraction : ImmediateInteraction<Sim, GameObject>
    {
        public class Definition : ImmediateInteractionDefinition<Sim, GameObject, AssignOutfitCategoryToInteraction>
        {
            public bool IsGlobal;

            public bool TargetIsSim;

            public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair iop)
            {
                var targetSim = IsGlobal ? null : target as Sim ?? actor;
                return Common.Localize(targetSim != null && targetSim.IsFemale, kLocalizationKey + ":Name");
            }

            public override string[] GetPath(bool isFemale)
            {
                var basePath = Common.Localize(isFemale, kLocalizationKey + "/Paths:Base");
                return TargetIsSim ? new[]
                {
                    basePath
                } : new[]
                {
                    basePath,
                    Common.Localize(isFemale, kLocalizationKey + "/Paths:" + (IsGlobal ? "Global" : "Individual"))
                };
            }

            public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                var targetSim = target as Sim;
                return targetSim == null || targetSim.SimDescription != null && targetSim.IsHuman;
            }
        }

        private const string kLocalizationKey = "/Interactions/AssignOutfitCategoryToInteraction";
        
        public static InteractionDefinition GlobalOutfitSingleton = new Definition
            {
                IsGlobal = true
            };

        public static InteractionDefinition SimSingleton = new Definition
            {
                TargetIsSim = true
            };

        public static InteractionDefinition Singleton = new Definition();
        
        public override bool Run()
        {
            var definition = (Definition)InteractionDefinition;
            return definition.IsGlobal ? AssignOutfitToInteraction.HandleGlobalOutfit(Actor, RunInternal, false) : RunInternal(definition.IsGlobal, false, Actor, Target);
        }

        public static bool RunInternal(bool isGlobal, bool isPartial, Sim actor, GameObject target)
        {
            var targetSim = isGlobal ? null : target as Sim ?? actor;
            if (InteractionInstanceTypeUtils.TryGetSelectedInteractionInstanceTypes(out var selectedInteractionInstanceTypes) && AssignOutfitToInteraction.TryGetEntryCallbackType(targetSim, actor, selectedInteractionInstanceTypes[0], out var entryCallbackType) && AssignOutfitToInteraction.TryGetExitCallbackType(targetSim, actor, selectedInteractionInstanceTypes[0], out var exitCallbackType) && TryGetOutfitCategory(targetSim, actor, selectedInteractionInstanceTypes[0], out var outfitCategory))
            {
                foreach (var interactionInstanceType in selectedInteractionInstanceTypes)
                {
                    targetSim.GetSimDescription().AssignOutfitToInteraction(targetSim == null ? actor.GetGlobalAssignedOutfitPrefix(true) + outfitCategory : OutfitAssignmentUtils.OutfitAssignmentCategoryPrefix + outfitCategory, interactionInstanceType, entryCallbackType.Value, exitCallbackType.Value, actor.SimDescription);
                }
            }
            else if (isGlobal)
            {
                AssignOutfitToInteraction.HandleGlobalOutfit(Sim.ActiveActor, RunInternal, false);
            }
            return true;
        }

        public static bool TryGetOutfitCategory(Sim sim, Sim fallbackSim, Type interactionInstanceType, out OutfitCategories outfitCategory)
        {
            const string localizationKey = "/Dialogs/OutfitCategoryDialog";
            if (ComboSelectionDialog.Show(Common.Localize(sim != null && sim.IsFemale, localizationKey + ":Title"), new SortedDictionary<string, object>(new AssignOutfitToInteraction.DummyComparer())
                {
                    {
                        Common.Localize(sim != null && sim.IsFemale, localizationKey + "/Options:None"),
                        nameof(OutfitCategories.None)
                    },
                    {
                        Common.Localize(sim != null && sim.IsFemale, localizationKey + "/Options:Everyday"),
                        nameof(OutfitCategories.Everyday)
                    },
                    {
                        Common.Localize(sim != null && sim.IsFemale, localizationKey + "/Options:Formalwear"),
                        nameof(OutfitCategories.Formalwear)
                    },
                    {
                        Common.Localize(sim != null && sim.IsFemale, localizationKey + "/Options:Sleepwear"),
                        nameof(OutfitCategories.Sleepwear)
                    },
                    {
                        Common.Localize(sim != null && sim.IsFemale, localizationKey + "/Options:Swimwear"),
                        nameof(OutfitCategories.Swimwear)
                    },
                    {
                        Common.Localize(sim != null && sim.IsFemale, localizationKey + "/Options:Athletic"),
                        nameof(OutfitCategories.Athletic)
                    },
                    {
                        Common.Localize(sim != null && sim.IsFemale, localizationKey + "/Options:Career"),
                        nameof(OutfitCategories.Career)
                    },
                    {
                        Common.Localize(sim != null && sim.IsFemale, localizationKey + "/Options:Outerwear"),
                        nameof(OutfitCategories.Outerwear)
                    },
                    {
                        Common.Localize(sim != null && sim.IsFemale, localizationKey + "/Options:MartialArts"),
                        nameof(OutfitCategories.MartialArts)
                    },
                    {
                        Common.Localize(sim != null && sim.IsFemale, localizationKey + "/Options:Naked"),
                        nameof(OutfitCategories.Naked)
                    },
                    {
                        Common.Localize(sim != null && sim.IsFemale, localizationKey + "/Options:Singed"),
                        nameof(OutfitCategories.Singed)
                    },
                    {
                        Common.Localize(sim != null && sim.IsFemale, localizationKey + "/Options:SkinnyDippingTowel"),
                        nameof(OutfitCategories.SkinnyDippingTowel)
                    }
                }, sim.GetSimDescription().TryGetOutfitAssignment(interactionInstanceType, out var outfitAssignment, fallbackSim.SimDescription) && outfitAssignment.SpecialOutfitKey.StartsWith(OutfitAssignmentUtils.OutfitAssignmentCategoryPrefix) ? outfitAssignment.SpecialOutfitKey.Substring(OutfitAssignmentUtils.OutfitAssignmentCategoryPrefix.Length) : nameof(OutfitCategories.Everyday)) is string text)
            {
                outfitCategory = (OutfitCategories)Enum.Parse(typeof(OutfitCategories), text);
                return true;
            }
            outfitCategory = OutfitCategories.None;
            return false;
        }
    }
}
