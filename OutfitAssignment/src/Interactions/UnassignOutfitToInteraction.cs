using System;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.SimIFace;
using Queries = Sims3.Gameplay.Queries;

namespace Destrospean.OutfitAssignment.Interactions
{
    public class UnassignOutfitToInteraction : ImmediateInteraction<Sim, GameObject>
    {
        public class Definition : ImmediateInteractionDefinition<Sim, GameObject, UnassignOutfitToInteraction>
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
        
        private const string kLocalizationKey = "/Interactions/UnassignOutfitToInteraction";
        
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
            var targetSim = isGlobal ? null : target as Sim ?? actor;
            if (InteractionInstanceTypeUtils.TryGetSelectedInteractionInstanceTypes(out var selectedInteractionInstanceTypes, Array.FindAll(InteractionInstanceTypeUtils.InteractionInstanceTypes, x => OutfitAssignmentUtils.OutfitAssignments.Exists(y => (y.SimDescription == null && targetSim.GetSimDescription() == null || y.SimDescription != null && targetSim.GetSimDescription() != null && y.SimDescription.SimDescriptionId == targetSim.GetSimDescription().SimDescriptionId) && (targetSim != null || y.SpecialOutfitKey.StartsWith(actor.GetGlobalAssignedOutfitPrefix())) && y.InteractionInstanceType == x.FullName))))
            {
                foreach (var interactionInstanceType in selectedInteractionInstanceTypes)
                {
                    if (targetSim.GetSimDescription().TryGetOutfitAssignment(interactionInstanceType, out var outfitAssignment, actor.SimDescription) && Array.FindAll(targetSim.GetSimDescription().GetAllOutfitAssignments(), x => x.SpecialOutfitKey == outfitAssignment.SpecialOutfitKey).Length == 1)
                    {
                        if (targetSim == null)
                        {
                            foreach (var sim in Queries.GetObjects<Sim>())
                            {
                                if (sim.SimDescription != null && sim.SimDescription.HasSpecialOutfit(outfitAssignment.SpecialOutfitKey))
                                {
                                    sim.SimDescription.RemoveSpecialOutfit(outfitAssignment.SpecialOutfitKey);
                                }
                            }
                            OutfitAssignmentUtils.AssignedOutfits.Remove(outfitAssignment.SpecialOutfitKey);
                        }
                        else if (OutfitAssignmentUtils.AssignedOutfits.ContainsKey(outfitAssignment.SpecialOutfitKey))
                        {
                            if (Array.FindAll(targetSim.SimDescription.GetAllOutfitAssignments(), x => x.SpecialOutfitKey == outfitAssignment.SpecialOutfitKey).Length == 1)
                            {
                                OutfitAssignmentUtils.AssignedOutfits.Remove(outfitAssignment.SpecialOutfitKey);
                            }
                        }
                        else
                        {
                            targetSim.SimDescription.RemoveSpecialOutfit(outfitAssignment.SpecialOutfitKey);
                        }
                    }
                    if (targetSim == null)
                    {
                        actor.SimDescription.UnassignGlobalOutfitToInteraction(interactionInstanceType);
                    }
                    else
                    {
                        targetSim.SimDescription.UnassignOutfitToInteraction(interactionInstanceType);
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
