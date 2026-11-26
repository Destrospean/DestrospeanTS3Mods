using System;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.SimIFace;

namespace Destrospean.OutfitAssignment.Interactions
{
    public class ExtendAssignedOutfitToInteraction : ImmediateInteraction<Sim, GameObject>
    {
        public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ExtendAssignedOutfitToInteraction>
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
                var targetSim = IsGlobal ? null : target as Sim ?? actor;
                return Array.Exists(targetSim.GetSimDescription().GetAllOutfitAssignments(), x => true /*targetSim != null || x.SpecialOutfitKey.StartsWith(OutfitAssignmentUtils.OutfitAssignmentCategoryPrefix) || x.SpecialOutfitKey.StartsWith(actor.GetGlobalAssignedOutfitPrefix())*/);
            }
        }
        
        private const string kCopyAssignedOutfitMiscellaneousLocalizationKey = "/Interactions/CopyAssignedOutfitToInteraction/Miscellaneous";
        
        private const string kLocalizationKey = "/Interactions/ExtendAssignedOutfitToInteraction";
        
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
            return definition.IsGlobal ? AssignOutfitToInteraction.HandleGlobalOutfit(Actor, RunInternal) : RunInternal(definition.IsGlobal, false, Actor, Target);
        }

        public static bool RunInternal(bool isGlobal, bool isPartial, Sim actor, GameObject target)
        {
            while (true)
            {
                var targetSim = isGlobal ? null : target as Sim ?? actor;
                if (!InteractionInstanceTypeUtils.TryGetSelectedInteractionInstanceTypes(out var sourceInteractionInstanceTypes, Array.FindAll(InteractionInstanceTypeUtils.InteractionInstanceTypes, x => OutfitAssignmentUtils.OutfitAssignments.Exists(y => (y.SimDescription == null && targetSim.GetSimDescription() == null || y.SimDescription != null && targetSim.GetSimDescription() != null && y.SimDescription.SimDescriptionId == targetSim.GetSimDescription().SimDescriptionId) && (targetSim != null || y.SpecialOutfitKey.StartsWith(actor.GetGlobalAssignedOutfitPrefix())) && y.InteractionInstanceType == x.FullName)), Common.Localize(kCopyAssignedOutfitMiscellaneousLocalizationKey + ":SelectSourceNamespace"), Common.Localize(kCopyAssignedOutfitMiscellaneousLocalizationKey + ":SelectSourceInteraction")))
                {
                    if (isGlobal)
                    {
                        AssignOutfitToInteraction.HandleGlobalOutfit(Sim.ActiveActor, RunInternal);
                    }
                    return true;
                }
                var specialOutfitKey = "";
                foreach (var interactionInstanceType in sourceInteractionInstanceTypes)
                {
                    if (targetSim.GetSimDescription().TryGetOutfitAssignment(interactionInstanceType, out var outfitAssignment, actor.SimDescription) && specialOutfitKey != outfitAssignment.SpecialOutfitKey)
                    {
                        if (!string.IsNullOrEmpty(specialOutfitKey))
                        {
                            return AssignOutfitToInteraction.NotifyMultipleOutfitsFound(targetSim, targetSim ?? actor, isGlobal);
                        }
                        specialOutfitKey = outfitAssignment.SpecialOutfitKey;
                    }
                }

                if (InteractionInstanceTypeUtils.TryGetSelectedInteractionInstanceTypes(out var destinationInteractionInstanceTypes, null, Common.Localize(kCopyAssignedOutfitMiscellaneousLocalizationKey + ":SelectDestinationNamespace"), Common.Localize(kCopyAssignedOutfitMiscellaneousLocalizationKey + ":SelectDestinationInteraction")) && AssignOutfitToInteraction.TryGetEntryCallbackType(targetSim, actor, sourceInteractionInstanceTypes[0], out var entryCallbackType) && AssignOutfitToInteraction.TryGetExitCallbackType(targetSim, actor, sourceInteractionInstanceTypes[0], out var exitCallbackType))
                {
                    foreach (var interactionInstanceType in destinationInteractionInstanceTypes)
                    {
                        targetSim.GetSimDescription().AssignOutfitToInteraction(specialOutfitKey, interactionInstanceType, entryCallbackType ?? default, exitCallbackType ?? default, actor.SimDescription);
                    }
                    return true;
                }
            }
        }
    }
}
