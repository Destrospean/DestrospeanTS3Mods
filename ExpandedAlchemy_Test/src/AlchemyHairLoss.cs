using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.PetSystems;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Destrospean.Utils;

namespace Sims3.Gameplay.Objects.Alchemy
{
    public class AlchemyHairLoss : AlchemyPotion
    {
        public class FreakOut : Interaction<Sim, Sim>
        {
            public class Definition : SoloSimInteractionDefinition<FreakOut>
            {
                public override string GetInteractionName(Sim actor, Sim target, InteractionObjectPair iop)
                {
                    return LocalizeString("InteractionName");
                }

                public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return true;
                }
            }

            public ReactionBroadcaster FreakOutBroadcast;

            public static ISoloInteractionDefinition Singleton = new Definition();

            public static string LocalizeString(string name, params object[] parameters)
            {
                return Localization.LocalizeString("Gameplay/ActorSystems/TraitFunctions/FreakOut:" + name, parameters);
            }

            public override void Cleanup()
            {
                DisposeBroadcaster();
                base.Cleanup();
            }

            public void DisposeBroadcaster()
            {
                if (FreakOutBroadcast != null)
                {
                    FreakOutBroadcast.Dispose();
                    FreakOutBroadcast = null;
                }
            }

            public static void OnWitnessFreakOut(Sim sim, ReactionBroadcaster reactionBroadcaster)
            {
                if (sim != reactionBroadcaster.BroadcastingObject)
                {
                    if (!sim.IsPet)
                    {
                        sim.PlayReaction(ReactionTypes.Shocked, reactionBroadcaster.BroadcastingObject as GameObject, ReactionSpeed.NowOrLater);
                    }
                    else
                    {
                        sim.InteractionQueue.Add(PetReactions.ReactNeutralPet.Singleton.CreateInstance(reactionBroadcaster.BroadcastingObject as GameObject, sim, new InteractionPriority(InteractionPriorityLevel.NonCriticalNPCBehavior), true, false));
                    }
                }
            }

            public override bool Run()
            {
                StandardEntry(false);
                EnterStateMachine("FreakOut", "Enter", "x");
                if (Actor.HasExitReason(ExitReason.Default))
                {
                    StandardExit(false, false);
                    return false;
                }
                BeginCommodityUpdates();
                AnimateSim("FreakOut");
                FreakOutBroadcast = new ReactionBroadcaster(Actor, TraitFunctions.FreakOut.kFreakOutBroadcasterParams, OnWitnessFreakOut);
                Actor.BuffManager.AddElement(BuffNames.Embarrassed, Origin.FromElixir);
                EventTracker.SendEvent(EventTypeId.kFreakedOut, Actor);
                AnimateSim("Exit");
                EndCommodityUpdates(true);
                StandardExit(false, false);
                DisposeBroadcaster();
                return true;
            }
        }

        public override bool DoDrinkBehavior(Sim actor, Sim target, InteractionInstance interaction)
        {
            base.DoDrinkBehavior(actor, target, interaction);
            target.SimDescription.ApplyToAllOutfits((simBuilder, outfitCategory, outfitIndex) =>
                {
                    SimOutfit outfit = target.SimDescription.GetOutfit(outfitCategory, outfitIndex);
                    ResourceKey partKey = new ResourceKey(ResourceUtils.HashString64(target.SimDescription.Toddler ? "puhairnone" : (target.SimDescription.Teen ? "a" : OutfitUtils.GetAgePrefix(target.SimDescription.Age, true)) + OutfitUtils.GetGenderPrefix(target.SimDescription.Gender) + "hairep08hairless"), 0x034AEECB, 0x00000000);
                    if (partKey == ResourceKey.kInvalidResourceKey)
                    {
                        return outfit;
                    }
                    simBuilder.PrepareForOutfit(outfit);
                    simBuilder.RemoveParts(BodyTypes.Hair);
                    simBuilder.AddPart(partKey);
                    return new SimOutfit(simBuilder.CacheOutfit(string.Format("AlchemyHairLoss_{0}_{1}_{2}", target.SimDescription.SimDescriptionId, outfitCategory, outfitIndex)));
                });
            target.InteractionQueue.Add(FreakOut.Singleton.CreateInstance(actor, target, new InteractionPriority(InteractionPriorityLevel.NonCriticalNPCBehavior), true, false));
            return true;
        }
    }
}
