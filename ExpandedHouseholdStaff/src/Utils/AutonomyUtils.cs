using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.ChildAndTeenUpdates;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.ObjectComponents;
using Sims3.Gameplay.Seasons;
using Sims3.Gameplay.Services;
using Sims3.Gameplay.Situations;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.TuningValues;
using Sims3.Gameplay.UI;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.Enums;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Destrospean.Utils.ExpandedHouseholdStaff
{
    /// <summary>
    /// Alternative methods for autonomy-related things. Use these methods instead of the originals for services and service situations from the Expanded Household Staff mod.
    /// </summary>
    public class AutonomyUtils
    {
        static InteractionInstance FindBestAction(Autonomy autonomy, CommodityKind commodityKind, bool metaAutonomy)
        {
            autonomy.ClearScoring();
            autonomy.mHasBeenScored = Autonomy.InteractionCheckTable.Allocate();
            autonomy.AddDecisionSnapshot();
            ScoreInteractionsOnObjects(autonomy, commodityKind, metaAutonomy);
            InteractionInstance result = autonomy.AssignProbabilitiesAndChooseInteraction();
            autonomy.ClearScoring();
            LiveDragHelperModel.ClearCachedTopDraggedObject();
            return result;
        }

        static void ScoreInteractionsForLocalAutonomy(Autonomy autonomy, CommodityKind commodityKind)
        {
            Lot lot = autonomy.mActor.LotCurrent;
            if (autonomy.mOverriddenLocalAutonomyLot != null)
            {
                lot = autonomy.mOverriddenLocalAutonomyLot;
            }
            else if (lot.IsWorldLot)
            {
                lot = autonomy.FindClosestLotForLocalAutonomy();
            }
            if (lot != null)
            {
                autonomy.ScoreInteractions(lot.Map, commodityKind);
                autonomy.ScoreInteractions(autonomy.mActor.SimCommodityInteractionMap, commodityKind);
                if (!lot.IsWorldLot || autonomy.mOverriddenLocalAutonomyLot != null)
                {
                    foreach (Sim sim in lot.GetObjects<Sim>())
                    {
                        if (sim != autonomy.mActor)
                        {
                            autonomy.ScoreInteractions(sim.SimCommodityInteractionMap, commodityKind);
                        }
                    }
                }
            }
            if (!autonomy.mActor.IsNPC || !autonomy.mActor.LotCurrent.IsWorldLot)
            {
                autonomy.ScoreInteractions(autonomy.mActor.Inventory.CommodityInteractionMap, commodityKind);
            }
            if (commodityKind == CommodityKind.None)
            {
                ScoreInteractionsForSituations(autonomy);
            }
            if (commodityKind == CommodityKind.None || commodityKind == CommodityKind.AlienBrainPower || autonomy.mForceScoreSoloInteractions)
            {
                autonomy.ScoreSoloSimInteractions(commodityKind);
            }
        }

        static void ScoreInteractionsForSituations(Autonomy autonomy)
        {
            foreach (Situation situation in autonomy.mSituationComponent.Situations)
            {
                foreach (Sim sim in situation.SimsWithInteractions)
                {
                    if (!sim.HasBeenDestroyed)
                    {
                        foreach (InteractionObjectPair interaction in sim.GetSituationSpecificInteractionsForActor(autonomy.mActor) ?? new List<InteractionObjectPair>())
                        {
                            CalculateScoreAndAddToCandidates(autonomy, interaction);
                        }
                    }
                }
            }
        }

        static Autonomy.YieldResult ScoreInteractionsOnObjects(Autonomy autonomy, CommodityKind commodityKind, bool metaAutonomy)
        {
            Autonomy.YieldResult result = Autonomy.YieldResult.Continue;
            autonomy.mUseCachedValues = true;
            try
            {
                autonomy.CacheValuesToOptimizeTestFunctions(autonomy.mActor);
                if (autonomy.ShouldRunLocalAutonomy)
                {
                    ScoreInteractionsForLocalAutonomy(autonomy, commodityKind);
                }
                if (metaAutonomy)
                {
                    return autonomy.ScoreInteractionsForMetaAutonomy(commodityKind);
                }
                return result;
            }
            finally
            {
                autonomy.mUseCachedValues = false;
            }
        }

        public static float CalculateScore(Autonomy autonomy, InteractionObjectPair iop)
        {
            return TestAndCalculateScore(iop, autonomy, autonomy.CurrentSearchType, autonomy.GetInteractionFlagsForCurrentSearch());
        }

        public static void CalculateScoreAndAddToCandidates(Autonomy autonomy, InteractionObjectPair iop)
        {
            if (autonomy.mHasBeenScored == null)
            {
                autonomy.mHasBeenScored = Autonomy.InteractionCheckTable.Allocate();
            }
            if (autonomy.mHasBeenScored.TryAdd(iop))
            {
                float score = CalculateScore(autonomy, iop);
                autonomy.mBestScores.Add(iop, score);
            }
        }

        public static float CalculateScoreForObjectInteraction(Autonomy autonomy, InteractionObjectPair iop)
        {
            ScoreDebugInfo tradeoffScore;
            return CalculateScoreForObjectInteraction(autonomy, iop, out tradeoffScore);
        }

        public static float CalculateScoreForObjectInteraction(Autonomy autonomy, InteractionObjectPair iop, out ScoreDebugInfo tradeoffScore)
        {
            tradeoffScore = null;
            if (iop.Tradeoff == null)
            {
                autonomy.AddSnapshotScore(iop, "[No tradeoff] " + iop.InteractionDefinition.ToString(), iop.Target.ToString());
                return 0f;
            }
            float initialMultiplier = 1f;
            if (iop.Tuning.ScoringFunction != null)
            {
                initialMultiplier = iop.Tuning.ScoringFunction(autonomy.mActor, iop);
            }
            CommodityKind mostValuedCommodity = CommodityKind.None;
            float metaOutputScore = 0f;
            IMetaInteractionDefinition metaInteractionDefinition = iop.InteractionDefinition as IMetaInteractionDefinition;
            if (metaInteractionDefinition != null)
            {
                metaOutputScore = metaInteractionDefinition.MetaAdScore(autonomy.mActor, iop.Target, autonomy.mInteractionScorer, ref mostValuedCommodity);
            }
            Sim actor = null;
            if (autonomy.ActorsGroupSituation != null)
            {
                actor = autonomy.ActorsGroupSituation.Leader;
            }
            if (actor == null)
            {
                actor = autonomy.mActor;
            }
            float score = iop.Tradeoff.GetScore(actor, autonomy.mInteractionScorer, iop.Target, iop, metaOutputScore, out tradeoffScore, initialMultiplier, iop.Tuning.ScoringFunctionOnlyAppliesToSpecificCommodity);
            float multiplier = 1f;
            GameObject target = iop.Target as GameObject;
            Sim targetSim = iop.Target as Sim;
            bool isOutside = iop.Target.IsOutside;
            if (isOutside)
            {
                if (autonomy.mActor.TraitManager.HasElement(TraitNames.HatesOutdoors))
                {
                    multiplier -= TraitTuning.HatesOutdoorsTraitAdvertisingMultiplier;
                }
                else if (autonomy.mActor.TraitManager.HasElement(TraitNames.LovesTheOutdoors))
                {
                    multiplier += TraitTuning.LovesOutdoorsTraitAdvertisingMultiplier;
                }
            }
            if (autonomy.mActor.IsADogSpecies && autonomy.mActor.TraitManager.HasElement(TraitNames.LoyalPet) && autonomy.Actor != targetSim)
            {
                Sim bffOrSimWithHighestLikingForPet = Relationship.GetBFFOrSimWithHighestLikingForPet(autonomy.mActor);
                if (bffOrSimWithHighestLikingForPet != null)
                {
                    if (targetSim == bffOrSimWithHighestLikingForPet)
                    {
                        multiplier *= TraitTuning.LoyalDogTraitAdvertisingMultiplierForFavoriteSim;
                    }
                    else if (bffOrSimWithHighestLikingForPet.LotCurrent == autonomy.Actor.LotCurrent && bffOrSimWithHighestLikingForPet.RoomId == iop.Target.RoomId)
                    {
                        multiplier *= TraitTuning.LoyalDogTraitAdvertisingMultiplier;
                    }
                }
            }
            if (autonomy.mActor.TraitManager.HasElement(TraitNames.Loner))
            {
                if (autonomy.mActor.Bed != null && autonomy.mActor.Bed.LotCurrent == iop.Target.LotCurrent && autonomy.mActor.Bed.RoomId == iop.Target.RoomId)
                {
                    multiplier += TraitTuning.LonerTraitAdvertisingMultiplier;
                }
                int simsInSameRoomCount = 0;
                if (iop.Target.LotCurrent != null)
                {
                    foreach (Sim sim in iop.Target.LotCurrent.GetAllActors())
                    {
                        if (sim != autonomy.mActor && sim.RoomId == autonomy.mActor.RoomId)
                        {
                            simsInSameRoomCount++;
                        }
                    }
                }
                multiplier -= Math.Min(simsInSameRoomCount * TraitTuning.LonerTraitAdvertisingMultiplierForSims, TraitTuning.LonerTraitAdvertisingMultiplierForSimsMax);
                multiplier = Math.Max(0f, multiplier);
            }
            if (autonomy.IsGuestAtAParty)
            {
                Party partyIAmGuestAt = Party.GetPartyIAmGuestAt(autonomy.mActor);
                if (partyIAmGuestAt != null && autonomy.mActor.LotCurrent == partyIAmGuestAt.Host.LotCurrent && iop.Target.RoomId == partyIAmGuestAt.Host.RoomId)
                {
                    multiplier += Party.ScoringMultiplierForNearHostAtParty;
                }
            }
            if (autonomy.ActorsGroupSituation != null && (iop.Tradeoff.SatisfiesCommodity(CommodityKind.Fun) || iop.Tradeoff.SatisfiesCommodity(CommodityKind.Social)))
            {
                multiplier += GroupingSituation.kScoringMulitplierForGroup;
            }
            if (autonomy.mActor.SimDescription.Teen && target.ActorsUsingMe.Count > 0)
            {
                foreach (Sim sim in target.ActorsUsingMe)
                {
                    Relationship relationship = Relationship.Get(sim, autonomy.mActor, false);
                    if (relationship != null && (relationship.LTR.LTRInteractionBits & LongTermRelationship.InteractionBits.BFF) != 0)
                    {
                        multiplier += SocialComponent.BFFAutonomyMultiplier;
                        break;
                    }
                }
            }
            if (autonomy.mActor.Service != null)
            {
                Type serviceDataType = autonomy.mActor.Service.GetType();
                PropertyInfo useObjectInSameRoomAsSleeperMultiplierProperty = serviceDataType.GetProperty("UseObjectInSameRoomAsSleeperMultiplier");
                if (autonomy.mActor.Service.ServiceType == ServiceType.Babysitter)
                {
                    if (target.ActorsUsingMe.Count > 0)
                    {
                        foreach (Sim sim in target.ActorsUsingMe)
                        {
                            if (sim.Service == null && sim.SimDescription.ChildOrBelow && score != 0f)
                            {
                                score *= Babysitter.UseObjectInSameRoomAsChildMultiplier;
                            }
                        }
                    }
                    Household household = autonomy.mActor.LotCurrent.Household;
                    if (household != null && !iop.Tradeoff.SatisfiesCommodity(CommodityKind.LookAfterBabyOrToddler))
                    {
                        foreach (Sim sim in household.Sims)
                        {
                            if (autonomy.mActor.RoomId != sim.RoomId && sim.SimDescription.ChildOrBelow && sim.RoomId == iop.Target.RoomId && score != 0f)
                            {
                                score += Babysitter.UseObjectInSameRoomAsChildBaseScore;
                            }
                        }
                    }
                }
                else if (ServiceUtils.IsFromExpandedHouseholdStaff(serviceDataType) && (bool)serviceDataType.GetProperty("IsQuietAroundSleepingSims").GetValue(autonomy.mActor.Service, null) && useObjectInSameRoomAsSleeperMultiplierProperty != null && useObjectInSameRoomAsSleeperMultiplierProperty.PropertyType == typeof(float) && iop.Target as IBed == null)
                {
                    int roomId = iop.Target.RoomId;
                    if (autonomy.mActor.LotCurrent.Household != null)
                    {
                        foreach (Sim sim in autonomy.mActor.LotCurrent.Household.Sims)
                        {
                            if (sim.IsSleeping && sim.RoomId == roomId)
                            {
                                score *= (float)useObjectInSameRoomAsSleeperMultiplierProperty.GetValue(null, null);
                            }
                        }
                    }
                }
            }
            if (iop.Tuning.ScoringFunction != null && iop.Tuning.ScoringFunctionOnlyAppliesToSpecificCommodity == CommodityKind.None)
            {
                multiplier *= initialMultiplier;
            }
            if (iop.Target != null)
            {
                multiplier *= iop.Target.GetBlockingScore();
            }
            if (iop.Target.IsInPublicResidentialRoom && (iop.Target.LotCurrent != autonomy.mActor.LotCurrent || !autonomy.mActor.IsInPublicResidentialRoom))
            {
                multiplier *= Autonomy.AutonomyPublicAreaPenaltyMultiplier;
            }
            for (Posture posture = autonomy.mActor.Posture; posture != null; posture = posture.PreviousPosture)
            {
                multiplier *= posture.GetAutonomyScoreMultiplierForInteraction(autonomy.mActor, iop.Target, iop.Tuning.PosturePreconditions);
            }
            if (isOutside && autonomy.mActor.SimDescription.IsVampire && !SimClock.IsNightTime())
            {
                multiplier *= Autonomy.kVampireDaylightOutdoorMultiplier;
            }
            if (targetSim != null && targetSim.Posture is IInBoxStallPosture && iop.InteractionDefinition as IBoxStallAllowedInteractionDefinition == null && (autonomy.mActor != targetSim || autonomy.mCurrentSearchType != AutonomySearchType.BuffAutoSolve))
            {
                return 0f;
            }
            if (autonomy.mActor.mSnubManager != null && autonomy.mActor.mSnubManager.ListOfSnubTargets != null)
            {
                foreach (ulong simDescriptionId in autonomy.mActor.mSnubManager.ListOfSnubTargets)
                {
                    SimDescription simDescription = SimDescription.Find(simDescriptionId);
                    if (simDescription == null)
                    {
                        continue;
                    }
                    Sim createdSim = simDescription.CreatedSim;
                    if (createdSim != null)
                    {
                        if ((iop.Target.Position - createdSim.Position).LengthSqr() <= (float)SnubManager.kAutonomousInteractionLimiterDistanceSqr)
                        {
                            multiplier *= SnubManager.kAutonomousInteractionLimiterMultiplier;
                        }
                    }
                }
            }
            if (iop.Tuning.Availability.HasFlags(Availability.FlagField.AllowEvenIfNotAllowedInRoomAutonomous) && !autonomy.mActor.IsAllowedInRoom(iop.Target.RoomId))
            {
                multiplier *= Autonomy.kAllowEvenIfNotAllowedInRoomAutonomousMultiplier;
            }
            if (SeasonsManager.Enabled && (SeasonsManager.CurrentWeather == Weather.Rain || SeasonsManager.CurrentWeather == Weather.Hail) && autonomy.mCurrentSearchType == AutonomySearchType.Autonomy)
            {
                if (isOutside && targetSim == null && !SeasonsManager.IsShelteredFromPrecipitation(iop.Target, iop.InteractionDefinition.CreateInstance(iop.Target, autonomy.Actor, new InteractionPriority(InteractionPriorityLevel.Autonomous), true, true)))
                {
                    multiplier *= SeasonsManager.kRainHailObjectAutonomyScoringMultiplier;
                }
                if (iop.Target.InInventory)
                {
                    if (iop.Target.ItemComp != null && autonomy.mActor.Inventory != null && iop.Target.ItemComp.InventoryParent == autonomy.mActor.Inventory && !SeasonsManager.IsShelteredFromPrecipitation(autonomy.mActor))
                    {
                        multiplier = iop.Target is IDontUseAutonomouslyOutsideInRainObject ? 0f : multiplier * SeasonsManager.kRainHailObjectAutonomyScoringMultiplier;
                    }
                }
            }
            score *= multiplier;
            float scoreBonus = 0f;
            if (autonomy.Actor.CurrentInteraction as OccultImaginaryFriend.ImaginaryFriendCleanHouse == null)
            {
                scoreBonus = autonomy.ComputeCheckScore(iop.Checks, tradeoffScore, iop.Tradeoff, autonomy.mActor, iop.Target);
            }
            float finalScore = score + scoreBonus;
            if (autonomy.mInteractionScorer.InteractionModifier != null)
            {
                finalScore *= autonomy.mInteractionScorer.InteractionModifier(iop);
            }
            float inUseAutonomyMultiplier = iop.Target.GetInUseAutonomyMultiplier(autonomy.mActor);
            finalScore *= inUseAutonomyMultiplier;
            multiplier *= inUseAutonomyMultiplier;
            return finalScore;
        }

        /// <summary>
        /// Finds the best available interaction. Use this instead of the original for service situations from the Expanded Household Staff mod.
        /// </summary>
        public static InteractionInstance FindBestAction(Autonomy autonomy)
        {
            autonomy.SetSearchType(AutonomySearchType.Generic);
            InteractionInstance result = FindBestAction(autonomy, CommodityKind.None, false);
            autonomy.ClearSearchType(AutonomySearchType.Generic);
            return result;
        }

        public static float TestAndCalculateScore(InteractionObjectPair iop, Autonomy autonomy, AutonomySearchType autonomySearchType, InteractionFlags flags)
        {
            if (autonomy.OverrideInteractionParameters.HasValue)
            {
                flags |= autonomy.OverrideInteractionParameters.Value.Flags;
            }
            InteractionInstanceParameters parameters = new InteractionInstanceParameters(iop, autonomy.Actor, new InteractionPriority(InteractionPriorityLevel.Autonomous), autonomySearchType, flags);
            GreyedOutTooltipCallback greyedOutTooltipCallback = null;
            return iop.InteractionDefinition.Test(ref parameters, ref greyedOutTooltipCallback) == 0 ? (iop.mInteraction as IOverridesCalculateScore)?.CalculateScore(iop, autonomy) ?? CalculateScoreForObjectInteraction(autonomy, iop) : 0f;
        }
    }
}
