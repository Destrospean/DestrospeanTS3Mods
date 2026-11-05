using System;
using System.Collections.Generic;
using Destrospean.Misc;
using Destrospean.Utils;
using Destrospean.Utils.ExpandedHouseholdStaff;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Abstracts.Destrospean.ExpandedHouseholdStaff;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Destrospean.ExpandedHouseholdStaff.Situations;
using Sims3.Gameplay.Destrospean.Utils;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces.Destrospean.ExpandedHouseholdStaff;
using Sims3.Gameplay.Objects.Beds;
using Sims3.Gameplay.Services;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;

namespace Sims3.Gameplay.Destrospean.ExpandedHouseholdStaff.Services
{
    [Persistable]
    public class CustomService : Service<CustomService>, IAmSociableService
    {
        public new class SetUnsetServiceBed : ImmediateInteraction<Sim, Bed>
        {
            [DoesntRequireTuning]
            [Persistable]
            public class Definition : InteractionDefinition<Sim, Bed, SetUnsetServiceBed>
            {
                public CustomService Service;

                public Definition()
                {
                }

                public Definition(CustomService service)
                {
                    Service = service;
                }

                public override string GetInteractionName(Sim actor, Bed target, InteractionObjectPair iop)
                {
                    var simsAssignedToLot = Service.GetSimsAssignedToLot(actor.LotHome);
                    return Localization.LocalizeString(actor.IsFemale, DerivedType.GetLocalizationKey() + "/" + nameof(SetUnsetServiceBed) + (target.FindOwnedBed(simsAssignedToLot[0]) == target ? ":Unset" : ":Set") + "InteractionName", simsAssignedToLot[0].SimDescription, Service.Profile.Title);
                }

                public override bool Test(Sim actor, Bed target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    if (actor.LotHome != null)
                    {
                        if (target.LotCurrent != actor.LotHome)
                        {
                            return false;
                        }
                        var simsAssignedToLot = Service.GetSimsAssignedToLot(actor.LotHome);
                        if (simsAssignedToLot.Count > 0)
                        {
                            var owner = simsAssignedToLot[0];
                            var bed = target.FindOwnedBed(owner);
                            if (bed == null || bed == target)
                            {
                                return target.CanBeUsedAsBed;
                            }
                        }
                    }
                    return false;
                }
            }

            public override bool Run()
            {
                return !DebugUtils.TryDisplayScriptError(() =>
                    {
                        var service = ((Definition)InteractionDefinition).Service;
                        var simActiveOnLot = service.GetSimActiveOnLot(Actor.LotHome);
                        if (simActiveOnLot != null)
                        {
                            var bed = Target.FindOwnedBed(simActiveOnLot);
                            if (bed == Target)
                            {
                                Target.RelinquishOwnership(simActiveOnLot);
                                return true;
                            }
                            if (Target.PartComponent != null && Target.PartComponent.PartDataList != null)
                            {
                                foreach (BedData value in Target.PartComponent.PartDataList.Values)
                                {
                                    if (value != null)
                                    {
                                        var currentServiceType = service.ServiceType;
                                        service.Profile.ServiceType = ServiceType.Butler;
                                        Target.ClaimOwnership(simActiveOnLot, value);
                                        service.Profile.ServiceType = currentServiceType;
                                        break;
                                    }
                                }
                            }
                        }
                        return true;
                    }, out var retVal) && retVal;
            }
        }

        public float CheckTime => Profile.CheckTime;

        public float DelayBeforeArriving => Profile.DelayBeforeArriving;

        public float DelayBeforeLeaving => Profile.DelayBeforeLeaving;

        public float DriveTime => Profile.DriveTime;

        public float ExtraWaitTimeAfterSocializing => Profile.ExtraWaitTimeAfterSocializing;

        public override bool IsPaidWeekly => Profile.IsLiveInService;

        public override bool IsQuietAroundSleepingSims => Profile.IsQuietAroundSleepingSims;

        /// <summary>
        /// Outputs for interactions and their target types that the service motive of the service is inserted into.
        /// </summary>
        public override List<ServiceUtils.CommodityChange> Outputs => Profile.Outputs;

        public IServiceProfile Profile;

        public float RelationshipLevelForQuit => Profile.RelationshipLevelForQuit;

        /// <summary>
        /// Gets the motive commodity kind for the service.
        /// </summary>
        public new CommodityKind ServiceMotive => Profile.ServiceMotive;

        public override List<CommodityKind> ServiceMotives => Profile.Motives;

        public override ServiceType ServiceType => Profile?.ServiceType ?? ServiceType.Maid;

        public SetUnsetServiceBed.Definition SetUnsetServiceBedInstance;

        public float TimeWaitBeforePutawayLeftovers => Profile.TimeWaitBeforePutawayLeftovers;

        public override ServiceTuning Tuning => Profile.ServiceTuning;

        public float UseObjectInSameRoomAsSleeperMultiplier => Profile.UseObjectInSameRoomAsSleeperMultiplier;

        public override bool WaitsBeforePuttingAwayLeftovers => Profile.WaitsBeforePuttingAwayLeftovers;

        public CustomService()
        {
        }

        public CustomService(IServiceProfile profile)
        {
            Profile = profile;
            ServiceUtils.CustomInstances[profile.Name] = this;
            SetUnsetServiceBedInstance = new SetUnsetServiceBed.Definition(this);
        }

        public new void AddInteractions(Bed bed)
        {
            bed.AddInteraction(SetUnsetServiceBedInstance, true);
        }

        public override void AddOutputs()
        {
            foreach (var output in Outputs)
            {
                ServiceMotive.AddAsOutput(output.InteractionDefinitionType, output.TargetType, output.ConstantChange, output.Locked, output.ActualValue, output.UpdateType, output.TimeDependsOnCommodityFilling, output.UpdateEvenOnFailure, output.UpdateAboveAndBelowZero);
            }
        }

        public static void Create(IServiceProfile profile)
        {
            DebugUtils.TryDisplayScriptError(() =>
                {
                    if (ServiceNPCSpecifications.ValidForCurrentWorld(profile.ServiceType))
                    {
                        if (ServiceUtils.CustomInstances.TryGetValue(profile.Name, out var service) && service != null)
                        {
                            service.PostLoadFixup();
                        }
                        else
                        {
                            _ = new CustomService(profile);
                        }
                    }
                    else if (ServiceUtils.CustomInstances.ContainsKey(profile.Name))
                    {
                        Destroy(ServiceUtils.CustomInstances[profile.Name]);
                        ServiceUtils.CustomInstances.Remove(profile.Name);
                    }
                });
        }

        /// <summary>
        /// Deinitializes everything related to the service of the specified profile.
        /// </summary>
        public static void Deinit(IServiceProfile profile, bool worldJustGotQuit = false)
        {
            DebugUtils.TryDisplayScriptError(() =>
                {
                    if (ServiceUtils.CustomInstances.TryGetValue(profile.Name, out var service) && service != null)
                    {
                        if (!worldJustGotQuit)
                        {
                            if (profile.IsLiveInService)
                            {
                                foreach (var bed in Queries.GetObjects<Bed>())
                                {
                                    service.RemoveInteractions(bed);
                                }
                            }
                            foreach (var simDescription in new List<SimDescription>(service.Pool))
                            {
                                if (simDescription != null && simDescription.CreatedSim != null)
                                {
                                    if (simDescription.CreatedSim == null)
                                    {
                                        service.RemoveSimFromPool(simDescription);
                                        continue;
                                    }
                                    if (ServiceSituation.FindServiceSituationInvolving(simDescription.CreatedSim) is CustomServiceSituation situation)
                                    {
                                        situation.SetState(new CustomServiceSituation.LeaveLotAndEndService(situation));
                                        continue;
                                    }
                                    service.RemoveSimFromPool(simDescription);
                                }
                            }
                        }
                        service.RemoveOutputs();
                        if (MotiveTuning.sTuning.ContainsKey((int)profile.ServiceMotive))
                        {
                            MotiveTuning.sTuning.Remove((int)profile.ServiceMotive);
                        }
                        CommonUtils.RemoveEnumValue<CommodityKind>("Be" + profile.Name);
                        var activeTopic = profile.Name + " Service";
                        foreach (var action in profile.Actions)
                        {
                            CommonUtils.RemoveActions(activeTopic, action.Grouping, action.IsActive, action.Name);
                        }
                        if (ActiveTopicData.Exists(activeTopic))
                        {
                            ActiveTopicData.sData.Remove(activeTopic);
                        }
                    }
                });
        }

        public override CASAgeGenderFlags GetGenderForNewNpc(Lot lot)
        {
            return Profile.ValidGenders;
        }

        public override string GetServiceTopic(Sim serviceSim)
        {
            return Profile.Name + " Service";
        }

        /// <summary>
        /// Initializes everything related to the service of the specified profile.
        /// </summary>
        public static void Init(IServiceProfile profile)
        {
            DebugUtils.TryDisplayScriptError(() =>
                {
                    CommonUtils.AddEnumValue<CommodityKind>("Be" + profile.Name, profile.ServiceMotive);
                    LoadServiceMotive(profile.ServiceMotive);
                    var activeTopic = profile.Name + " Service";
                    if (!ActiveTopicData.Exists(activeTopic))
                    {
                        ActiveTopicData.Add(new ActiveTopicData(activeTopic, false, 1000, "", true, true, false, true, null, 0f, "", false));
                    }
                    foreach (var action in profile.Actions)
                    {
                        CommonUtils.AddActions(activeTopic, action.Grouping, action.IsActive, action.Name);
                    }
                    Create(profile);
                    if (ServiceUtils.CustomInstances.TryGetValue(profile.Name, out var service) && service != null)
                    {
                        if (profile.IsLiveInService)
                        {
                            foreach (var bed in Queries.GetObjects<Bed>())
                            {
                                service.AddInteractions(bed);
                            }
                        }
                        World.OnObjectPlacedInLotEventHandler += (sender, e) => DebugUtils.TryDisplayScriptError(() =>
                            {
                                if (e is World.OnObjectPlacedInLotEventArgs onObjectPlacedInLotEventArgs)
                                {
                                    var gameObject = GameObject.GetObject(onObjectPlacedInLotEventArgs.ObjectId);
                                    if (service.Profile.IsLiveInService)
                                    {
                                        if (gameObject is Bed bed)
                                        {
                                            service.AddInteractions(bed);
                                        }
                                    }
                                }
                            });
                        service.AddOutputs();
                        using (var enumerator = service.Pool.GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                if (enumerator.Current != null && enumerator.Current.CreatedSim != null)
                                {
                                    CommonUtils.UpdateMotiveTunings(enumerator.Current.CreatedSim, service.ServiceMotive);
                                    (ServiceSituation.FindServiceSituationInvolving(enumerator.Current.CreatedSim) as CustomServiceSituation)?.SetMotivesAndCommodities();
                                }
                            }
                        }
                    }
                });
        }

        public override ServiceSituation InternalCreateSituation(Lot assignedLot, Sim createdSim, int cost, ObjectGuid requestingSim)
        {
            createdSim.SimDescription.ShowSocialsOnSim = true;
            createdSim.CanBeFired = true;
            return new CustomServiceSituation(this, assignedLot, createdSim, cost);
        }

        public override bool NeedsAssignment(Lot lot)
        {
            if (Profile.IsLiveInService)
            {
                return IsServiceRequested(lot) && !IsAnySimAssignedToLot(lot);
            }
            if (SimClock.ElapsedCalendarDays() - WhenLastAssignedToLot(lot) > 0)
            {
                var hours = SimClock.Hours24;
                if (hours >= Profile.TimeToStartWorking && (hours <= Profile.TimeToStopWorking || Profile.TimeToStartWorking >= Profile.TimeToStopWorking) && IsServiceRequested(lot) && !IsAnySimAssignedToLot(lot))
                {
                    return true;
                }
            }
            return false;
        }

        public void RemoveInteractions(Bed bed)
        {
            foreach (var interaction in new List<InteractionObjectPair>(bed.Interactions))
            {
                if ((interaction.InteractionDefinition as SetUnsetServiceBed.Definition)?.Service == this)
                {
                    bed.RemoveInteraction(interaction);
                }
            }
        }

        public override void RemoveOutputs()
        {
            foreach (var output in Outputs)
            {
                ServiceMotive.RemoveAsOutput(output.InteractionDefinitionType, output.TargetType, output.ConstantChange, output.Locked, output.ActualValue, output.UpdateType, output.TimeDependsOnCommodityFilling, output.UpdateEvenOnFailure, output.UpdateAboveAndBelowZero);
            }
        }

        public override void SetServiceNPCProperties(SimDescription simDescription)
        {
            simDescription.CanBeKilledOnJob = true;
            simDescription.Marryable = true;
            simDescription.Contactable = true;
            simDescription.ShowSocialsOnSim = true;
        }

        public override void SetTraits(SimDescription simDescription)
        {
            foreach (var traitName in Profile.Traits)
            {
                simDescription.TraitManager.AddElement(traitName);
            }
            foreach (var traitName in Profile.HiddenTraits)
            {
                if (TraitManager.GetTraitFromDictionary(traitName).IsReward)
                {
                    simDescription.TraitManager.AddElement(traitName);
                    continue;
                }
                simDescription.TraitManager.AddHiddenElement(traitName);
            }
            var potentialTraits = new List<TraitNames>(Profile.PotentialTraits);
            for (var i = 0; i < Math.Min(Profile.PotentialTraitCount, Profile.PotentialTraits.Count); i++)
            {
                var traitName = RandomUtil.GetRandomObjectFromList(potentialTraits);
                simDescription.TraitManager.AddElement(traitName);
                potentialTraits.Remove(traitName);
            }
        }

        public override bool ShouldSimBeRemovedFromService(SimDescription sim)
        {
            if (AgingManager.NumDaysBeforeAging(sim) <= kNumDaysBeforeBirthdayToRemoveSim)
            {
                return true;
            }
            if ((sim.AgeAfterInstantiation & Profile.ValidAges) == 0)
            {
                return true;
            }
            if (sim.Household == null || !sim.Household.IsServiceNpcHousehold)
            {
                return true;
            }
            if (sim.DeathStyle != 0)
            {
                return true;
            }
            return false;
        }

        public override void UpdateCreatedSim(Sim sim)
        {
            DebugUtils.TryDisplayScriptError(() =>
                {
                    foreach (var skillLevelPair in Profile.Skills)
                    {
                        var skill = sim.SkillManager.AddElement(skillLevelPair.SkillName);
                        var level = skillLevelPair.SkillLevel < 0 ? skill.MaxSkillLevel : skillLevelPair.SkillLevel;
                        for (var i = 0; i < level && skill.SkillLevel < level; i++)
                        {
                            skill.ForceSkillLevelUp(level);
                        }
                    }
                    if (sim.Inventory != null)
                    {
                        // The following code corrects items with the wrong presets.
                        var tempInventory = Profile.Inventory.ConvertAll(x => new InventoryObjectCreationParameters(x.InstanceName, x.InstanceId, x.GroupId, x.Count, x.Preset));
                        foreach (var inventoryObject in sim.Inventory.FindAll<GameObject>(false))
                        {
                            for (var i = 0; i < tempInventory.Count; i++)
                            {
                                if (!string.IsNullOrEmpty(tempInventory[i].Preset) && tempInventory[i].Count > 0 && tempInventory[i].InstanceId == inventoryObject.Product?.ProductResourceKey.InstanceId && tempInventory[i].GroupId == inventoryObject.Product?.ProductResourceKey.GroupId && sim.Inventory.FindAll<GameObject>(false).FindAll(x => x.GetCurrentPreset() == tempInventory[i].Preset).Count < Profile.Inventory[i].Count)
                                {
                                    inventoryObject.ApplyPreset(tempInventory[i].Preset);
                                    tempInventory[i].Count--;
                                    break;
                                }
                            }
                        }

                        // The following code adds items in the inventory when they do not exist.
                        foreach (var inventoryObjectCreationParameters in Profile.Inventory)
                        {
                            var dummyItem = inventoryObjectCreationParameters.Instantiate();
                            foreach (var inventoryObject in inventoryObjectCreationParameters.InstantiateMany(inventoryObjectCreationParameters.Count - (sim.Inventory.mInventoryItems?.FindAll(x => (x.Object as GameObject)?.ObjectInstanceName == inventoryObjectCreationParameters.InstanceName).Count ?? 0)))
                            {
                                if (sim.Inventory.TryToAdd(inventoryObject))
                                {
                                    if (!string.IsNullOrEmpty(inventoryObjectCreationParameters.Preset))
                                    {
                                        inventoryObject.ApplyPreset(inventoryObjectCreationParameters.Preset);
                                    }
                                    continue;
                                }
                                inventoryObject.Destroy();
                            }
                            dummyItem.Destroy();
                        }
                    }
                });
        }
    }
}
