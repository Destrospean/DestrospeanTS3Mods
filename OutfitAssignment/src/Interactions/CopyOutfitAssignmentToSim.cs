using System;
using System.Collections.Generic;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Interactions;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.SimIFace.CustomContent;

namespace Destrospean.OutfitAssignment.Interactions
{
    public class CopyOutfitAssignmentToSim : ImmediateInteraction<Sim, GameObject>
    {
        public class Definition : ImmediateInteractionDefinition<Sim, GameObject, CopyOutfitAssignmentToSim>
        {
            public bool TargetIsSim;

            public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair iop)
            {
                var targetSim = target as Sim ?? actor;
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
                    Common.Localize(isFemale, sLocalizationKey + "/Paths:Individual")
                };
            }

            public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                var targetSim = target as Sim ?? actor;
                return targetSim != null && Array.Exists(targetSim.GetSimDescription().GetAllOutfitAssignments(), x => !x.SpecialOutfitKey.StartsWith(OutfitAssignmentUtils.OutfitAssignmentCategoryPrefix));
            }
        }
        
        public static InteractionDefinition SimSingleton = new Definition
            {
                TargetIsSim = true
            };

        public static InteractionDefinition Singleton = new Definition();

        public const string sLocalizationKey = "/Interactions/CopyOutfitAssignmentToSim";

        public override bool Run()
        {
            var sourceSim = Target as Sim ?? Actor;
            var sourceSpecialOutfitKey = "";
            if (!Common.ShowSimListDialog(out var destinationSimDescriptions, x => x != sourceSim.SimDescription && OutfitUtils.GetAgePrefix(x.Age, true) == OutfitUtils.GetAgePrefix(sourceSim.SimDescription.Age, true) && x.Gender == sourceSim.SimDescription.Gender && x.Species == sourceSim.SimDescription.Species) || !InteractionInstanceTypeUtils.TryGetSelectedInteractionInstanceTypes(out var selectedInteractionInstanceTypes, Array.ConvertAll(Array.FindAll(sourceSim.SimDescription.GetAllOutfitAssignments(), x => !x.SpecialOutfitKey.StartsWith(OutfitAssignmentUtils.OutfitAssignmentCategoryPrefix)), x => Array.Find(InteractionInstanceTypeUtils.InteractionInstanceTypes, y => y.FullName == x.InteractionInstanceType))))
            {
                return true;
            }
            foreach (var interactionInstanceType in selectedInteractionInstanceTypes)
            {
                if (sourceSim.SimDescription.TryGetOutfitAssignment(interactionInstanceType, out var outfitAssignment, Actor.SimDescription) && sourceSpecialOutfitKey != outfitAssignment.SpecialOutfitKey)
                {
                    if (!string.IsNullOrEmpty(sourceSpecialOutfitKey))
                    {
                        return AssignOutfitToInteraction.NotifyMultipleOutfitsFound(sourceSim, sourceSim, false);
                    }
                    sourceSpecialOutfitKey = outfitAssignment.SpecialOutfitKey;
                }
            }
            if (AssignOutfitToInteraction.TryGetEntryCallbackType(sourceSim, Actor, selectedInteractionInstanceTypes[0], out var entryCallbackType) && AssignOutfitToInteraction.TryGetExitCallbackType(sourceSim, Actor, selectedInteractionInstanceTypes[0], out var exitCallbackType))
            {
                foreach (var destinationSimDescription in destinationSimDescriptions)
                {
                    var destinationSpecialOutfitKey = "OutfitAssignment_" + DownloadContent.GenerateGUID();
                    if (OutfitAssignmentUtils.AssignedOutfits.ContainsKey(sourceSpecialOutfitKey))
                    {
                        if (OutfitAssignmentUtils.ShowPartOverrideListDialog(OutfitAssignmentUtils.AssignedOutfits[destinationSpecialOutfitKey] = new OutfitAssignmentUtils.AssignedOutfit(OutfitAssignmentUtils.AssignedOutfits[sourceSpecialOutfitKey]), out var partOverrides))
                        {
                            OutfitAssignmentUtils.AssignedOutfits[destinationSpecialOutfitKey].PartOverrides = new List<BodyTypes>(partOverrides);
                        }
                    }
                    else
                    {
                        destinationSimDescription.AddAssignedOutfit(new OutfitAssignmentUtils.AssignedOutfit(sourceSim.SimDescription.GetSpecialOutfit(sourceSpecialOutfitKey)), destinationSpecialOutfitKey);
                    }
                    foreach (var interactionInstanceType in selectedInteractionInstanceTypes)
                    {
                        destinationSimDescription.AssignOutfitToInteraction(destinationSpecialOutfitKey, interactionInstanceType, entryCallbackType.Value, exitCallbackType.Value, Actor.SimDescription);
                    }
                }
            }
            return true;
        }
    }
}
