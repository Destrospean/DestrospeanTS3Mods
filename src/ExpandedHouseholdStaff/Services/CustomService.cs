using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Abstracts.Destrospean.ExpandedHouseholdStaff;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Interfaces.Destrospean.ExpandedHouseholdStaff;
using Sims3.Gameplay.Objects.Beds;
using Sims3.Gameplay.Services;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.Utilities;
using Sims3.Gameplay.Destrospean.ExpandedHouseholdStaff.Situations;
using Sims3.Gameplay.Destrospean.Utils;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using System;
using System.Collections.Generic;
using System.Reflection;
using Destrospean.Misc;
using Destrospean.Utils;
using Destrospean.Utils.ExpandedHouseholdStaff;

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
                    List<Sim> simsAssignedToLot = Service.GetSimsAssignedToLot(actor.LotHome);
                    return Localization.LocalizeString(actor.IsFemale, DerivedType.GetLocalizationKey() + "/" + typeof(SetUnsetServiceBed).Name + (target.FindOwnedBed(simsAssignedToLot[0]) == target ? ":Unset" : ":Set") + "InteractionName", simsAssignedToLot[0].SimDescription, Service.Profile.Title);
                }

                public override bool Test(Sim actor, Bed target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    if (actor.LotHome != null)
                    {
                        if (target.LotCurrent != actor.LotHome)
                        {
                            return false;
                        }
                        List<Sim> simsAssignedToLot = Service.GetSimsAssignedToLot(actor.LotHome);
                        if (simsAssignedToLot.Count > 0)
                        {
                            Sim owner = simsAssignedToLot[0];
                            Bed bed = target.FindOwnedBed(owner);
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
                bool retVal;
                return !DebugUtils.TryDisplayScriptError(() =>
                    {
                        CustomService service = ((Definition)InteractionDefinition).Service;
                        Sim simActiveOnLot = service.GetSimActiveOnLot(Actor.LotHome);
                        if (simActiveOnLot != null)
                        {
                            Bed bed = Target.FindOwnedBed(simActiveOnLot);
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
                                        ServiceType currentServiceType = service.ServiceType;
                                        service.Profile.ServiceType = ServiceType.Butler;
                                        Target.ClaimOwnership(simActiveOnLot, value);
                                        service.Profile.ServiceType = currentServiceType;
                                        break;
                                    }
                                }
                            }
                        }
                        return true;
                    }, out retVal) && retVal;
            }
        }

        public float CheckTime
        {
            get
            {
                return Profile.CheckTime;
            }
        }

        public float DelayBeforeArriving
        {
            get
            {
                return Profile.DelayBeforeArriving;
            }
        }

        public float DelayBeforeLeaving
        {
            get
            {
                return Profile.DelayBeforeLeaving;
            }
        }

        public float DriveTime
        {
            get
            {
                return Profile.DriveTime;
            }
        }

        public float ExtraWaitTimeAfterSocializing
        {
            get
            {
                return Profile.ExtraWaitTimeAfterSocializing;
            }
        }

        public override bool IsPaidWeekly
        {
            get
            {
                return Profile.IsLiveInService;
            }
        }

        public override bool IsQuietAroundSleepingSims
        {
            get
            {
                return Profile.IsQuietAroundSleepingSims;
            }
        }

        /// <summary>
        /// Outputs for interactions and their target types that the service motive of the service is inserted into.
        /// </summary>
        public override List<ServiceUtils.CommodityChange> Outputs
        {
            get
            {
                return Profile.Outputs;
            }
        }

        public IServiceProfile Profile;

        public float RelationshipLevelForQuit
        {
            get
            {
                return Profile.RelationshipLevelForQuit;
            }
        }

        /// <summary>
        /// Gets the motive commodity kind for the service.
        /// </summary>
        public new CommodityKind ServiceMotive
        {
            get
            {
                return Profile.ServiceMotive;
            }
        }

        public override List<CommodityKind> ServiceMotives
        {
            get
            {
                return Profile.Motives;
            }
        }

        public override ServiceType ServiceType
        {
            get
            {
                return Profile?.ServiceType ?? ServiceType.Maid;
            }
        }

        public SetUnsetServiceBed.Definition SetUnsetServiceBedInstance;

        public float TimeWaitBeforePutawayLeftovers
        {
            get
            {
                return Profile.TimeWaitBeforePutawayLeftovers;
            }
        }

        public override ServiceTuning Tuning
        {
            get
            {
                return Profile.ServiceTuning;
            }
        }

        public float UseObjectInSameRoomAsSleeperMultiplier
        {
            get
            {
                return Profile.UseObjectInSameRoomAsSleeperMultiplier;
            }
        }

        public override bool WaitsBeforePuttingAwayLeftovers
        {
            get
            {
                return Profile.WaitsBeforePuttingAwayLeftovers;
            }
        }

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
            DebugUtils.TryDisplayScriptError(() => bed.AddInteraction(SetUnsetServiceBedInstance, true));
        }

        public override void AddOutputs()
        {
            foreach (ServiceUtils.CommodityChange output in Outputs)
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
                        CustomService service;
                        if (ServiceUtils.CustomInstances.TryGetValue(profile.Name, out service) && service != null)
                        {
                            service.PostLoadFixup();
                        }
                        else
                        {
                            new CustomService(profile);
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
                    CustomService service;
                    if (ServiceUtils.CustomInstances.TryGetValue(profile.Name, out service) && service != null)
                    {
                        if (!worldJustGotQuit)
                        {
                            if (profile.IsLiveInService)
                            {
                                foreach (Bed bed in Sims3.Gameplay.Queries.GetObjects<Bed>())
                                {
                                    service.RemoveInteractions(bed);
                                }
                            }
                            foreach (SimDescription simDescription in new List<SimDescription>(service.Pool))
                            {
                                if (simDescription != null && simDescription.CreatedSim != null)
                                {
                                    if (simDescription.CreatedSim == null)
                                    {
                                        service.RemoveSimFromPool(simDescription);
                                        continue;
                                    }
                                    CustomServiceSituation situation = ServiceSituation.FindServiceSituationInvolving(simDescription.CreatedSim) as CustomServiceSituation;
                                    if (situation == null)
                                    {
                                        service.RemoveSimFromPool(simDescription);
                                        continue;
                                    }
                                    situation.SetState(new CustomServiceSituation.LeaveLotAndEndService(situation));
                                }
                            }
                        }
                        service.RemoveOutputs();
                        if (MotiveTuning.sTuning.ContainsKey((int)profile.ServiceMotive))
                        {
                            MotiveTuning.sTuning.Remove((int)profile.ServiceMotive);
                        }
                        CommonUtils.RemoveEnumValue<CommodityKind>("Be" + profile.Name);
                        string activeTopic = profile.Name + " Service";
                        foreach (ServiceUtils.ActiveTopicAction action in profile.Actions)
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
                    string activeTopic = profile.Name + " Service";
                    if (!ActiveTopicData.Exists(activeTopic))
                    {
                        ActiveTopicData.Add(new ActiveTopicData(activeTopic, false, 1000, "", true, true, false, true, null, 0f, "", false));
                    }
                    foreach (ServiceUtils.ActiveTopicAction action in profile.Actions)
                    {
                        CommonUtils.AddActions(activeTopic, action.Grouping, action.IsActive, action.Name);
                    }
                    Create(profile);
                    CustomService service;
                    if (ServiceUtils.CustomInstances.TryGetValue(profile.Name, out service) && service != null)
                    {
                        if (profile.IsLiveInService)
                        {
                            foreach (Bed bed in Sims3.Gameplay.Queries.GetObjects<Bed>())
                            {
                                service.AddInteractions(bed);
                            }
                        }
                        World.OnObjectPlacedInLotEventHandler += (sender, e) => DebugUtils.TryDisplayScriptError(() =>
                            {
                                World.OnObjectPlacedInLotEventArgs onObjectPlacedInLotEventArgs = e as World.OnObjectPlacedInLotEventArgs;
                                if (onObjectPlacedInLotEventArgs != null)
                                {
                                    GameObject gameObject = GameObject.GetObject(onObjectPlacedInLotEventArgs.ObjectId);
                                    if (service.Profile.IsLiveInService)
                                    {
                                        Bed bed = gameObject as Bed;
                                        if (bed != null)
                                        {
                                            service.AddInteractions(bed);
                                        }
                                    }
                                }
                            });
                        service.AddOutputs();
                        IEnumerator<SimDescription> enumerator = service.Pool.GetEnumerator();
                        while (enumerator.MoveNext())
                        {
                            if (enumerator.Current != null && enumerator.Current.CreatedSim != null)
                            {
                                CommonUtils.UpdateMotiveTunings(enumerator.Current.CreatedSim, service.ServiceMotive);
                                (ServiceSituation.FindServiceSituationInvolving(enumerator.Current.CreatedSim) as CustomServiceSituation)?.SetMotivesAndCommodities();
                            }
                        }
                    }
                });
        }

        public override ServiceSituation InternalCreateSituation(Lot assignedLot, Sim createdSim, int cost, ObjectGuid requestingSim)
        {
            ServiceSituation retVal = null;
            DebugUtils.TryDisplayScriptError(() =>
                {
                    createdSim.SimDescription.ShowSocialsOnSim = true;
                    createdSim.CanBeFired = true;
                    retVal = new CustomServiceSituation(this, assignedLot, createdSim, cost);
                });
            return retVal;
        }

        public void RemoveInteractions(Bed bed)
        {
            DebugUtils.TryDisplayScriptError(() =>
                {
                    foreach (InteractionObjectPair interaction in new List<InteractionObjectPair>(bed.Interactions))
                    {
                        if ((interaction.InteractionDefinition as SetUnsetServiceBed.Definition)?.Service == this)
                        {
                            bed.RemoveInteraction(interaction);
                        }
                    }
                });
        }

        public override void RemoveOutputs()
        {
            foreach (ServiceUtils.CommodityChange output in Outputs)
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
            DebugUtils.TryDisplayScriptError(() =>
                {
                    foreach (TraitNames traitName in Profile.Traits)
                    {
                        simDescription.TraitManager.AddElement(traitName);
                    }
                    foreach (TraitNames traitName in Profile.HiddenTraits)
                    {
                        if (TraitManager.GetTraitFromDictionary(traitName).IsReward)
                        {
                            simDescription.TraitManager.AddElement(traitName);
                            continue;
                        }
                        simDescription.TraitManager.AddHiddenElement(traitName);
                    }
                    List<TraitNames> potentialTraits = new List<TraitNames>(Profile.PotentialTraits);
                    for (int i = 0; i < Math.Min(Profile.PotentialTraitCount, Profile.PotentialTraits.Count); i++)
                    {
                        TraitNames traitName = RandomUtil.GetRandomObjectFromList(potentialTraits);
                        simDescription.TraitManager.AddElement(traitName);
                        potentialTraits.Remove(traitName);
                    }
                });
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
                    foreach (SkillLevelPair skillLevelPair in Profile.Skills)
                    {
                        Skill skill = sim.SkillManager.AddElement(skillLevelPair.SkillName);
                        int level = skillLevelPair.SkillLevel < 0 ? skill.MaxSkillLevel : skillLevelPair.SkillLevel;
                        for (int i = 0; i < level && skill.SkillLevel < level; i++)
                        {
                            skill.ForceSkillLevelUp(level);
                        }
                    }
                    if (sim.Inventory != null)
                    {
                        // The following code corrects items with the wrong presets.
                        List<InventoryObjectCreationParameters> tempInventory = Profile.Inventory.ConvertAll(x => new InventoryObjectCreationParameters(x.InstanceName, x.InstanceId, x.GroupId, x.Count, x.Preset));
                        foreach (GameObject inventoryObject in sim.Inventory.FindAll<GameObject>(false))
                        {
                            for (int i = 0; i < tempInventory.Count; i++)
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
                        foreach (InventoryObjectCreationParameters inventoryObjectCreationParameters in Profile.Inventory)
                        {
                            IGameObject dummyItem = inventoryObjectCreationParameters.Instantiate();
                            foreach (IGameObject inventoryObject in inventoryObjectCreationParameters.InstantiateMany(inventoryObjectCreationParameters.Count - (sim.Inventory.mInventoryItems?.FindAll(x => (x.Object as GameObject)?.ObjectInstanceName == inventoryObjectCreationParameters.InstanceName).Count ?? 0)))
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
