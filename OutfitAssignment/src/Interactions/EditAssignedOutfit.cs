using System;
using Destrospean.Utils;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.SimIFace;

namespace Destrospean.OutfitAssignment.Interactions
{
    public class EditAssignedOutfit : ImmediateInteraction<Sim, GameObject>
    {
        public class Definition : ImmediateInteractionDefinition<Sim, GameObject, EditAssignedOutfit>
        {
            public bool IsGlobal;

            public bool TargetIsSim;

            public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair iop)
            {
                var targetSim = IsGlobal ? null : target as Sim ?? actor;
                return Common.Localize(targetSim != null && targetSim.IsFemale, sLocalizationKey + ":Name");
            }

            public override string[] GetPath(bool isFemale)
            {
                var basePath = Common.Localize(isFemale, sLocalizationKey + "/Paths:Base");
                return TargetIsSim ? new[]
                {
                    basePath
                } : new[]
                {
                    basePath,
                    Common.Localize(isFemale, sLocalizationKey + "/Paths:" + (IsGlobal ? "Global" : "Individual"))
                };
            }

            public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                var targetSim = IsGlobal ? null : target as Sim ?? actor;
                return Array.Exists(targetSim.GetSimDescription().GetAllOutfitAssignments(), x => !x.SpecialOutfitKey.StartsWith(OutfitAssignmentUtils.OutfitAssignmentCategoryPrefix) /*&& (targetSim != null || x.SpecialOutfitKey.StartsWith(actor.GetGlobalAssignedOutfitPrefix()))*/);
            }
        }
        
        public static InteractionDefinition GlobalOutfitSingleton = new Definition
            {
                IsGlobal = true
            };

        public static InteractionDefinition SimSingleton = new Definition
            {
                TargetIsSim = true
            };

        public static InteractionDefinition Singleton = new Definition();

        public const string sLocalizationKey = "/Interactions/EditAssignedOutfit";

        public override bool Run()
        {
            var definition = (Definition)InteractionDefinition;
            return definition.IsGlobal ? AssignOutfitToInteraction.HandleGlobalOutfit(Actor, RunInternal) : RunInternal(definition.IsGlobal, false, Actor, Target);
        }

        public static bool RunInternal(bool isGlobal, bool isPartial, Sim actor, GameObject target)
        {
            var targetSim = isGlobal ? null : target as Sim ?? actor;
            var targetOrActor = targetSim ?? actor;
            if (InteractionInstanceTypeUtils.TryGetSelectedInteractionInstanceTypes(out var selectedInteractionInstanceTypes, Array.ConvertAll(Array.FindAll(targetSim.GetSimDescription().GetAllOutfitAssignments(), x => (targetSim != null || x.SpecialOutfitKey.StartsWith(actor.GetGlobalAssignedOutfitPrefix())) && !x.SpecialOutfitKey.StartsWith(OutfitAssignmentUtils.OutfitAssignmentCategoryPrefix)), x => Array.Find(InteractionInstanceTypeUtils.InteractionInstanceTypes, y => y.FullName == x.InteractionInstanceType))))
            {
                OutfitAssignmentUtils.OutfitAssignment outfitAssignment = null;
                var specialOutfitKey = "";
                foreach (var interactionInstanceType in selectedInteractionInstanceTypes)
                {
                    if (targetSim.GetSimDescription().TryGetOutfitAssignment(interactionInstanceType, out var tempOutfitAssignment, actor.SimDescription) && specialOutfitKey != tempOutfitAssignment.SpecialOutfitKey)
                    {
                        if (!string.IsNullOrEmpty(specialOutfitKey))
                        {
                            return AssignOutfitToInteraction.NotifyMultipleOutfitsFound(targetSim, targetOrActor, isGlobal);
                        }
                        outfitAssignment = tempOutfitAssignment;
                        specialOutfitKey = tempOutfitAssignment.SpecialOutfitKey;
                    }
                }
                isPartial = targetSim != null && OutfitAssignmentUtils.AssignedOutfits.ContainsKey(outfitAssignment?.SpecialOutfitKey ?? "");
                if (targetSim == null && actor.SimDescription.HasSpecialOutfit(specialOutfitKey) || isPartial && targetSim.SimDescription.HasSpecialOutfit(specialOutfitKey))
                {
                    targetOrActor.SimDescription.RemoveSpecialOutfit(specialOutfitKey);
                }
                if ((targetSim == null || isPartial) && OutfitAssignmentUtils.AssignedOutfits.ContainsKey(specialOutfitKey))
                {
                    targetOrActor.AddAssignedOutfit(specialOutfitKey);
                }
                if (OutfitExtensions.EditSpecialOutfit(targetOrActor, specialOutfitKey))
                {
                    foreach (var interactionInstanceType in selectedInteractionInstanceTypes)
                    {
                        targetSim.GetSimDescription().AssignOutfitToInteraction(specialOutfitKey, interactionInstanceType, outfitAssignment?.EntryCallbackType ?? default, outfitAssignment?.ExitCallbackType ?? default, actor.SimDescription);
                    }
                    if (targetSim == null || isPartial)
                    {
                        OutfitAssignmentUtils.AssignedOutfits[specialOutfitKey] = new OutfitAssignmentUtils.AssignedOutfit(targetOrActor.SimDescription.GetSpecialOutfit(specialOutfitKey))
                            {
                                PartOverrides = OutfitAssignmentUtils.AssignedOutfits[specialOutfitKey].PartOverrides
                            };
                    }
                }
                if ((targetSim == null || isPartial) && targetOrActor.SimDescription.HasSpecialOutfit(specialOutfitKey))
                {
                    targetOrActor.SimDescription.RemoveSpecialOutfit(specialOutfitKey);
                }
            }
            else if (isGlobal)
            {
                AssignOutfitToInteraction.HandleGlobalOutfit(Sim.ActiveActor, RunInternal);
            }
            return true;
        }
    }
}
