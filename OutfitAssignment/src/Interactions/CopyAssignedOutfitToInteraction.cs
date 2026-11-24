using System;
using System.Collections.Generic;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.SimIFace.CustomContent;

namespace Destrospean.OutfitAssignment.Interactions
{
    public class CopyAssignedOutfitToInteraction : ImmediateInteraction<Sim, GameObject>
    {
        public class Definition : ImmediateInteractionDefinition<Sim, GameObject, CopyAssignedOutfitToInteraction>
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

        public const string sLocalizationKey = "/Interactions/CopyAssignedOutfitToInteraction";

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
                var sourceSpecialOutfitKey = "";
                if (!InteractionInstanceTypeUtils.TryGetSelectedInteractionInstanceTypes(out var sourceInteractionInstanceTypes, Array.ConvertAll(Array.FindAll(targetSim.GetSimDescription().GetAllOutfitAssignments(), x => (targetSim != null || x.SpecialOutfitKey.StartsWith(actor.GetGlobalAssignedOutfitPrefix())) && !x.SpecialOutfitKey.StartsWith(OutfitAssignmentUtils.OutfitAssignmentCategoryPrefix)), x => Array.Find(InteractionInstanceTypeUtils.InteractionInstanceTypes, y => y.FullName == x.InteractionInstanceType)), Common.Localize(sLocalizationKey + "/Miscellaneous:SelectSourceNamespace"), Common.Localize(sLocalizationKey + "/Miscellaneous:SelectSourceInteraction")))
                {
                    if (isGlobal)
                    {
                        AssignOutfitToInteraction.HandleGlobalOutfit(Sim.ActiveActor, RunInternal);
                    }
                    return true;
                }
                foreach (var interactionInstanceType in sourceInteractionInstanceTypes)
                {
                    if (targetSim.GetSimDescription().TryGetOutfitAssignment(interactionInstanceType, out var outfitAssignment, actor.SimDescription) && sourceSpecialOutfitKey != outfitAssignment.SpecialOutfitKey)
                    {
                        if (!string.IsNullOrEmpty(sourceSpecialOutfitKey))
                        {
                            return AssignOutfitToInteraction.NotifyMultipleOutfitsFound(targetSim, targetSim ?? actor, isGlobal);
                        }
                        sourceSpecialOutfitKey = outfitAssignment.SpecialOutfitKey;
                    }
                }
                if (InteractionInstanceTypeUtils.TryGetSelectedInteractionInstanceTypes(out var destinationInteractionInstanceTypes, null, Common.Localize(sLocalizationKey + "/Miscellaneous:SelectDestinationNamespace"), Common.Localize(sLocalizationKey + "/Miscellaneous:SelectDestinationInteraction")) && AssignOutfitToInteraction.TryGetEntryCallbackType(targetSim, actor, sourceInteractionInstanceTypes[0], out var entryCallbackType) && AssignOutfitToInteraction.TryGetExitCallbackType(targetSim, actor, sourceInteractionInstanceTypes[0], out var exitCallbackType))
                {
                    var destinationSpecialOutfitKey = (targetSim == null ? actor.GetGlobalAssignedOutfitPrefix() : "OutfitAssignment_") + DownloadContent.GenerateGUID();
                    if (OutfitAssignmentUtils.AssignedOutfits.ContainsKey(sourceSpecialOutfitKey))
                    {
                        if (OutfitAssignmentUtils.ShowPartOverrideListDialog(OutfitAssignmentUtils.AssignedOutfits[destinationSpecialOutfitKey] = new OutfitAssignmentUtils.AssignedOutfit(OutfitAssignmentUtils.AssignedOutfits[sourceSpecialOutfitKey]), out var partOverrides))
                        {
                            OutfitAssignmentUtils.AssignedOutfits[destinationSpecialOutfitKey].PartOverrides = new List<BodyTypes>(partOverrides);
                        }
                    }
                    else
                    {
                        targetSim?.SimDescription.AddSpecialOutfit(new SimOutfit(targetSim.SimDescription.GetSpecialOutfit(sourceSpecialOutfitKey).Key), destinationSpecialOutfitKey);
                    }
                    foreach (var interactionInstanceType in destinationInteractionInstanceTypes)
                    {
                        targetSim.GetSimDescription().AssignOutfitToInteraction(destinationSpecialOutfitKey, interactionInstanceType, entryCallbackType ?? default, exitCallbackType ?? default, actor.SimDescription);
                    }
                    return true;
                }
            }
        }
    }
}
