using System;
using System.Collections.Generic;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;

namespace Destrospean.OutfitAssignment.Interactions
{
    public class ConfigureOutfitAssignment : ImmediateInteraction<Sim, GameObject>
    {
        public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ConfigureOutfitAssignment>
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
                return Array.Exists(targetSim.GetSimDescription().GetAllOutfitAssignments(), x => true /*targetSim != null || x.SpecialOutfitKey.StartsWith(OutfitAssignmentUtils.OutfitAssignmentCategoryPrefix) || x.SpecialOutfitKey.StartsWith(actor.GetGlobalAssignedOutfitPrefix())*/);
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

        public const string sLocalizationKey = "/Interactions/ConfigureOutfitAssignment";

        public override bool Run()
        {
            var definition = (Definition)InteractionDefinition;
            return definition.IsGlobal ? AssignOutfitToInteraction.HandleGlobalOutfit(Actor, RunInternal) : RunInternal(definition.IsGlobal, false, Actor, Target);
        }

        public static bool RunInternal(bool isGlobal, bool isPartial, Sim actor, GameObject target)
        {
            var targetSim = isGlobal ? null : target as Sim ?? actor;
            if (InteractionInstanceTypeUtils.TryGetSelectedInteractionInstanceTypes(out var selectedInteractionInstanceTypes, Array.FindAll(InteractionInstanceTypeUtils.InteractionInstanceTypes, x => OutfitAssignmentUtils.OutfitAssignments.Exists(y => (y.SimDescription == null && targetSim.GetSimDescription() == null || y.SimDescription != null && targetSim.GetSimDescription() != null && y.SimDescription.SimDescriptionId == targetSim.GetSimDescription().SimDescriptionId) && (targetSim != null || y.SpecialOutfitKey.StartsWith(actor.GetGlobalAssignedOutfitPrefix())) && y.InteractionInstanceType == x.FullName))) && AssignOutfitToInteraction.TryGetEntryCallbackType(targetSim, actor, selectedInteractionInstanceTypes[0], out var entryCallbackType) && AssignOutfitToInteraction.TryGetExitCallbackType(targetSim, actor, selectedInteractionInstanceTypes[0], out var exitCallbackType))
            {
                BodyTypes[] partOverrides = null;
                var changePartOverrides = false;
                if (targetSim.GetSimDescription().TryGetOutfitAssignment(selectedInteractionInstanceTypes[0], out var firstOutfitAssignment, actor.SimDescription))
                {
                    if (OutfitAssignmentUtils.AssignedOutfits.TryGetValue(firstOutfitAssignment.SpecialOutfitKey, out _))
                    {
                        changePartOverrides = OutfitAssignmentUtils.ShowPartOverrideListDialog(OutfitAssignmentUtils.AssignedOutfits[firstOutfitAssignment.SpecialOutfitKey], out partOverrides);
                    }
                }
                foreach (var interactionInstanceType in selectedInteractionInstanceTypes)
                {
                    if (targetSim.GetSimDescription().TryGetOutfitAssignment(interactionInstanceType, out var outfitAssignment, actor.SimDescription))
                    {
                        targetSim.GetSimDescription().AssignOutfitToInteraction(outfitAssignment.SpecialOutfitKey, interactionInstanceType, entryCallbackType ?? default, exitCallbackType ?? default, actor.SimDescription);
                    }
                    if (changePartOverrides && partOverrides != null && OutfitAssignmentUtils.AssignedOutfits.TryGetValue(outfitAssignment.SpecialOutfitKey, out var assignedOutfit))
                    {
                        assignedOutfit.PartOverrides = new List<BodyTypes>(partOverrides);
                    }
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
