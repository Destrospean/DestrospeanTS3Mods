using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Abstracts.Destrospean.ExpandedHouseholdStaff;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Objects;
using Sims3.Gameplay.Objects.HobbiesSkills;
using Sims3.Gameplay.Objects.Vehicles;
using Sims3.Gameplay.Services;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.Utilities;
using Sims3.Gameplay.Destrospean.ExpandedHouseholdStaff.Services;
using Sims3.Gameplay.Destrospean.Utils;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using System;
using System.Collections.Generic;
using Destrospean.Utils;
using Destrospean.Utils.ExpandedHouseholdStaff;

namespace Sims3.Gameplay.Destrospean.ExpandedHouseholdStaff.Situations
{
    public class CustomServiceSituation : ServiceSituation<CustomServiceSituation>
    {
        public class HangAroundBeforeLeaving : ChildSituation<CustomServiceSituation>
        {
            AlarmHandle mAlarmHandle;

            public HangAroundBeforeLeaving()
            {
            }

            public HangAroundBeforeLeaving(CustomServiceSituation parent) : base(parent)
            {
            }

            public override void CleanUp()
            {
                DebugUtils.TryDisplayScriptError(() =>
                    {
                        AlarmManager.RemoveAlarm(mAlarmHandle);
                        base.CleanUp();
                    });
            }

            public override void Init(CustomServiceSituation parent)
            {
                parent.MoveAllPossibleToTargetInventory();
                CustomService service = (CustomService)parent.Service;
                DebugUtils.TryDisplayScriptError(() => mAlarmHandle = AlarmManager.AddAlarm(service.DelayBeforeLeaving, TimeUnit.Minutes, TimeToRoute, service.Profile.Name + " waiting to leave", AlarmType.DeleteOnReset, parent.Worker));
            }

            public override void OnSocializedWith(Sim sim)
            {
                DebugUtils.TryDisplayScriptError(() =>
                    {
                        CustomService service = (CustomService)Parent.Service;
                        float timeLeft = AlarmManager.GetTimeLeft(mAlarmHandle, TimeUnit.Minutes);
                        if (timeLeft < service.ExtraWaitTimeAfterSocializing)
                        {
                            AlarmManager.UpdateAlarmTime(mAlarmHandle, service.ExtraWaitTimeAfterSocializing - timeLeft, TimeUnit.Minutes);
                        }
                    });
            }

            public void TimeToRoute()
            {
                DebugUtils.TryDisplayScriptError(() =>
                    {
                        Parent.NPCLeavingMessage(LeavingReason.NPCJobDone);
                        Parent.SetState(new LeaveLot<CustomServiceSituation>(Parent));
                    });
            }
        }

        public class LeaveLotAndEndService : ChildSituation<CustomServiceSituation>
        {
            public LeaveLotAndEndService()
            {
            }

            public LeaveLotAndEndService(CustomServiceSituation parent) : base(parent)
            {
            }

            public override void Init(CustomServiceSituation parent)
            {
                DebugUtils.TryDisplayScriptError(() =>
                    {
                        parent.Worker.InteractionQueue.CancelAllInteractions();
                        parent.NPCLeavingMessage(LeavingReason.NPCDismissed);
                        parent.UnsetServiceBed();
                        parent.ChargeForService((s, x) => ForceSituationSpecificInteraction(parent.Lot, parent.Worker, new DriveAwayInServiceCar.Definition(parent.Car), null, OnFinished, OnFinished));
                    });
            }

            public void OnFinished(Sim actor, float x)
            {
                actor.Service.ClearServiceForLot(Lot);
                actor.Service.EndService(actor.SimDescription);
            }
        }

        public new class NPCIsFired : ServiceSituation<CustomServiceSituation>.NPCIsFired
        {
            public NPCIsFired()
            {
            }

            public NPCIsFired(Sim firer, CustomServiceSituation parent) : base(firer, parent)
            {
            }

            public override void OnFinished(Sim actor, float x)
            {
                DebugUtils.TryDisplayScriptError(() =>
                    {
                        Parent.NPCLeavingMessage(LeavingReason.NPCFired);
                        Parent.SetState(new LeaveLot<ServiceSituation<CustomServiceSituation>>(Parent));
                        Parent.Service.FireSim(actor);
                    });
            }
        }

        public class PerformDuties : ChildSituation<CustomServiceSituation>
        {
            AlarmHandle mAlarmHandle = AlarmHandle.kInvalidHandle;

            public bool HasDuties
            {
                get
                {
                    bool retVal;
                    return !DebugUtils.TryDisplayScriptError(() =>
                        {
                            Parent.MoveAllPossibleToTargetInventory();
                            CustomService service = Parent.Service as CustomService;
                            if (service?.Profile.IsScaredOfBonehilda ?? false)
                            {
                                foreach (Sim sim in Lot.GetObjects<Sim>())
                                {
                                    if (sim.SimDescription.IsBonehilda && sim.RoomId == Parent.Worker.RoomId)
                                    {
                                        Parent.Worker.BuffManager.AddElement(BuffNames.Scared, Origin.FromSeeingBonehilda);
                                        Parent.SetState(new QuitCauseOfBonehilda(Parent));
                                        return false;
                                    }
                                }
                            }
                            if (Parent.Worker.CurrentInteraction == null)
                            {
                                PushSwitchToServiceUniform();
                            }
                            else if (Parent.Worker.CurrentInteraction.GetPriority().Level <= InteractionPriorityLevel.Autonomous)
                            {
                                InteractionInstance interactionInstance = AutonomyUtils.FindBestAction(Parent.Worker.Autonomy);
                                if (interactionInstance != null && Parent.IsInteractionBetterThanCurrent(interactionInstance))
                                {
                                    Parent.Worker.AddExitReason(ExitReason.CanceledByScript);
                                    return false;
                                }
                            }
                            if (Parent.Worker.InteractionQueue != null)
                            {
                                InteractionInstance headInteraction = Parent.Worker.InteractionQueue.GetHeadInteraction();
                                if (headInteraction != null && service != null && headInteraction.SatisfiesCommodity(service.ServiceMotive))
                                {
                                    return true;
                                }
                            }
                            return true;
                        }, out retVal) && retVal;
                }
            }

            public PerformDuties()
            {
            }

            public PerformDuties(CustomServiceSituation parent) : base(parent)
            {
            }

            public void CheckForChild()
            {
                if (Parent.ServiceTerminated || !Parent.IsBabysittingService)
                {
                    return;
                }
                bool attendingToChild = false;
                bool currentInteractionSatisfiesHunger = false;
                List<Sim> extendedHouseholdSims = Parent.GetExtendedHouseholdSims();
                if (Parent.Worker.CurrentInteraction != null)
                {
                    if (Parent.Worker.CurrentInteraction.SatisfiesCommodity(CommodityKind.Hunger))
                    {
                        currentInteractionSatisfiesHunger = true;
                    }
                    Sim sim = Parent.Worker.CurrentInteraction.Target as Sim;
                    if (sim == null || !sim.SimDescription.ChildOrBelow || !extendedHouseholdSims.Contains(sim))
                    {
                        GameObject gameObject = Parent.Worker.CurrentInteraction.Target as GameObject;
                        if (gameObject != null)
                        {
                            foreach (Sim item in gameObject.ActorsUsingMe)
                            {
                                if (extendedHouseholdSims.Contains(item) && item.SimDescription.ChildOrBelow)
                                {
                                    attendingToChild = true;
                                    break;
                                }
                            }
                        }
                        if (Parent.Worker.CurrentInteraction.GetPriority().Level <= InteractionPriorityLevel.Autonomous)
                        {
                            InteractionInstance interactionInstance = Parent.Worker.Autonomy.FindBestAction();
                            if (interactionInstance != null && Parent.IsInteractionBetterThanCurrent(interactionInstance))
                            {
                                Parent.Worker.AddExitReason(ExitReason.CanceledByScript);
                                return;
                            }
                        }
                    }
                    else
                    {
                        attendingToChild = true;
                    }
                }
                bool isInSameRoom = false;
                foreach (Sim sim in extendedHouseholdSims)
                {
                    if (sim.IsAtHome && sim.SimDescription.ChildOrBelow && sim.RoomId == Parent.Worker.RoomId)
                    {
                        isInSameRoom = true;
                    }
                    if (Babysitter.CanAttendToSim(sim) && Babysitter.AttendToNeedsOfSim(Parent.Worker, sim, attendingToChild, currentInteractionSatisfiesHunger))
                    {
                        return;
                    }
                }
                if (Parent.RequireBeInSameRoom && !isInSameRoom && !currentInteractionSatisfiesHunger && Parent.Worker.CurrentInteraction != null)
                {
                    if (Parent.Worker.CurrentInteraction.Id == Parent.LastInteractionId)
                    {
                        Parent.Worker.AddExitReason(ExitReason.Finished);
                    }
                    Parent.LastInteractionId = Parent.Worker.CurrentInteraction.Id;
                }
            }

            public void CheckForDuties()
            {
                DebugUtils.TryDisplayScriptError(() =>
                    {
                        if (!Parent.ServiceTerminated && !HasDuties && !Parent.IsLiveInService && (!Parent.Worker.BuffManager.HasElement(BuffNames.Scared) || Parent.Worker.BuffManager.GetElement(BuffNames.Scared).BuffOrigin != Origin.FromSeeingBonehilda))
                        {
                            Parent.SetState(new HangAroundBeforeLeaving(Parent));
                        }
                        CheckForChild();
                    });
            }

            public override void CleanUp()
            {
                DebugUtils.TryDisplayScriptError(() =>
                    {
                        Parent.Worker.WorkMotive = CommodityKind.None;
                        foreach (CommodityKind motive in Parent.Service?.ServiceMotives ?? new List<CommodityKind>())
                        {
                            Parent.Worker.Autonomy.Motives.RemoveMotive(motive);
                        }
                        AlarmManager.RemoveAlarm(mAlarmHandle);
                        base.CleanUp();
                    });
            }

            public override void Init(CustomServiceSituation parent)
            {
                CustomService service = (CustomService)parent.Service;
                parent.Worker.GreetSimOnLot(parent.Lot);
                DebugUtils.TryDisplayScriptError(() => mAlarmHandle = parent.Worker.AddAlarmRepeating(service.CheckTime, TimeUnit.Minutes, CheckForDuties, service.CheckTime, TimeUnit.Minutes, "Time for " + service.Profile.Name + " to check if everything is done", AlarmType.AlwaysPersisted));
            }

            public void PushSwitchToServiceUniform()
            {
                CustomService service = Parent.Service as CustomService;
                OutfitAssignmentUtils.OutfitAssignment outfitAssignment;
                if (service?.Profile != null && OutfitAssignmentUtils.TryGetGlobalOutfitAssignment(Parent.Worker.SimDescription, service.Profile, out outfitAssignment) && Parent.Worker.CurrentOutfitCategory != OutfitCategories.Career)
                {
                    Parent.Worker.PushSwitchToOutfitInteraction(Sim.ClothesChangeReason.Force, OutfitCategories.Career, new InteractionPriority(InteractionPriorityLevel.Autonomous, 50f));
                }
            }
        }

        public class QuitCauseOfBonehilda : ChildSituation<CustomServiceSituation>
        {
            public QuitCauseOfBonehilda()
            {
            }

            public QuitCauseOfBonehilda(CustomServiceSituation parent) : base(parent)
            {
            }

            public override void Init(CustomServiceSituation parent)
            {
                DebugUtils.TryDisplayScriptError(() =>
                    {
                        parent.Worker.InteractionQueue.CancelAllInteractions();
                        RequestWalkStyle(parent.Worker, Sim.WalkStyle.Run);
                        ForceSituationSpecificInteraction(parent.Lot, parent.Worker, new Maid.QuitBecauseOfBonehilda.Definition(), null, (s, x) => s.RequestWalkStyle(Sim.WalkStyle.OnFire), null);
                        ForceSituationSpecificInteraction(parent.Lot, parent.Worker, new DriveAwayInServiceCar.Definition(parent.Car), null, OnFinished, OnFinished);
                    });
            }

            public void OnFinished(Sim actor, float x)
            {
                actor.UnrequestWalkStyle(Sim.WalkStyle.OnFire);
                actor.Service.ClearServiceForLot(Lot);
                actor.Service.EndService(actor.SimDescription);
            }
        }

        public class StartPerformingDuties : ChildSituation<CustomServiceSituation>
        {
            public StartPerformingDuties()
            {
            }

            public StartPerformingDuties(CustomServiceSituation parent) : base(parent)
            {
            }

            public override void Init(CustomServiceSituation parent)
            {
                DebugUtils.TryDisplayScriptError(() =>
                    {
                        parent.OnArriveOnLot();
                        parent.SetMotivesAndCommodities();
                        parent.SetState(new PerformDuties(parent));
                    });
            }
        }

        public new class WaitToRoute : ChildSituation<CustomServiceSituation>
        {
            AlarmHandle mAlarmHandle;

            public WaitToRoute()
            {
            }

            public WaitToRoute(CustomServiceSituation parent) : base(parent)
            {
            }

            public override void CleanUp()
            {
                DebugUtils.TryDisplayScriptError(() =>
                    {
                        AlarmManager.RemoveAlarm(mAlarmHandle);
                        base.CleanUp();
                    });
            }

            public override void Init(CustomServiceSituation parent)
            {
                CustomService service = (CustomService)parent.Service;
                DebugUtils.TryDisplayScriptError(() => mAlarmHandle = AlarmManager.AddAlarm(service.DelayBeforeArriving, TimeUnit.Minutes, TimeToRoute, service.Profile.Name + " waiting to route", AlarmType.DeleteOnReset, parent.Worker));
            }

            public void TimeToRoute()
            {
                DebugUtils.TryDisplayScriptError(() =>
                    {
                        Parent.mProtectedInventory = Parent.Worker.Inventory.FindAll<IGameObject>(false);
                        Parent.OnServiceStarting();
                        CustomService service = (CustomService)Parent.Service;
                        RouteToLot<CustomServiceSituation, StartPerformingDuties> routeToLot = string.IsNullOrEmpty(service.Profile.CarInstanceName) ? new WalkToLot<CustomServiceSituation, StartPerformingDuties>(Parent) : new RouteToLot<CustomServiceSituation, StartPerformingDuties>(Parent);
                        routeToLot.SetRouteTime(service.DriveTime);
                        Parent.SetState(routeToLot);
                    });
            }
        }

        public new class WalkToLot<ParentSituation, NextSituation> : RouteToLot<ParentSituation, NextSituation> where ParentSituation : CustomServiceSituation where NextSituation : ChildSituation<ParentSituation>
        {
            public class DoWalkToLot : Interaction<Sim, Lot>
            {
                [DoesntRequireTuning]
                public class Definition : InteractionDefinition<Sim, Lot, DoWalkToLot>
                {
                    public override string GetInteractionName(Sim actor, Lot target, InteractionObjectPair iop)
                    {
                        return string.IsNullOrEmpty(target.Name) ? LocalizeString(actor.IsFemale, target.GetMetaAutonomyVenueType().ToString(), new object[0]) : LocalizeString(actor.IsFemale, "VisitNamedLot", new object[]
                            {
                                target.Name
                            });
                    }

                    public override bool Test(Sim actor, Lot target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                    {
                        return true;
                    }
                }

                public static InteractionDefinition Singleton = new Definition();

                public static string LocalizeString(bool isFemale, string name, params object[] parameters)
                {
                    return Localization.LocalizeString(isFemale, "Gameplay/Core/VisitCommunityLot:" + name, parameters);
                }

                public override bool Run()
                {
                    Route route = Actor.CreateRoute();
                    //route.SetOption(Route.RouteOption.EnablePlanningAsCar, false);
                    Target.PlanToLot(route);
                    return Actor.DoRoute(route);
                }
            }

            public WalkToLot()
            {
            }

            public WalkToLot(ParentSituation parent) : base(parent)
            {
            }

            public override void Init(ParentSituation parent)
            {
                ForceSituationSpecificInteraction(Lot, parent.Worker, DoWalkToLot.Singleton, null, OnRouteSucceeded, OnRouteFailed);
            }
        }

        List<IGameObject> mProtectedInventory;

        public bool IsBabysittingService
        {
            get
            {
                return (Service as CustomService)?.Profile.IsBabysittingService ?? false;
            }
        }

        public override bool IsLiveInService
        {
            get
            {
                return (Service as CustomService)?.Profile.IsLiveInService ?? false;
            }
        }

        public override bool ReportsFires
        {
            get
            {
                return (Service as CustomService)?.Profile.ReportsFires ?? false;
            }
        }

        public bool RequireBeInSameRoom
        {
            get
            {
                return (Service as CustomService)?.Profile.RequireBeInSameRoom ?? false;
            }
        }

        public override bool ServiceTerminated
        {
            get
            {
                if (Lot.EffectiveHousehold == null)
                {
                    return true;
                }
                foreach (Sim sim in Lot.EffectiveHousehold.Sims)
                {
                    if (sim.SimDescription.YoungAdultOrAbove)
                    {
                        Relationship relationship = Relationship.Get(Worker, sim, true);
                        if (relationship.LTR.Liking < ((CustomService)Service).RelationshipLevelForQuit)
                        {
                            SetToFire(Worker, Worker);
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public override float TimeToSpendWorking
        {
            get
            {
                return (Service as CustomService)?.Profile.TimeToSpendWorking ?? 0f;
            }
        }

        public CustomServiceSituation()
        {
        }

        public CustomServiceSituation(Service<CustomService> service, Lot lot, Sim worker, int cost) : base(service, lot, worker, cost)
        {
        }

        public bool AttendToNeedsOfSim(Sim sim, bool attendingToChild, bool preparingFood)
        {
            if (!preparingFood && !sim.SimDescription.ToddlerOrBelow && ShouldStartPreparingFoodFor(sim) && Babysitter.NumFoodOnLot(Worker, Quality.Neutral) < Babysitter.ValidHungryHouseholdMembers(Worker).Count && Babysitter.PrepareFood(Worker, true))
            {
                return true;
            }
            if (sim.Motives.IsLonely() && !attendingToChild && !preparingFood && (sim.Conversation == null || !sim.Conversation.ContainsSim(Worker)))
            {
                Worker.AddExitReason(ExitReason.Finished);
                Worker.InteractionQueue.AddNext(new SocialInteractionA.Definition("Chat", new string[0], null, false).CreateInstance(sim, Worker, new InteractionPriority(InteractionPriorityLevel.NonCriticalNPCBehavior, GetNewInteractionPriorityValue()), true, true));
                return true;
            }
            return false;
        }

        public override void ChargeForService(Callback callbackOnCompletion)
        {
            int totalCost = CostTotal();
            if (totalCost > 0 && Lot.EffectiveHousehold != null && !mbHasCharged && Lot.EffectiveHousehold.IsActive)
            {
                if (Lot.EffectiveHousehold.FamilyFunds < totalCost)
                {
                    string entryKey = NotEnoughFundsMessage();
                    if (entryKey != null)
                    {
                        StyledNotification.Show(new StyledNotification.Format(Localization.LocalizeString(entryKey, (Service as CustomService)?.Profile.Title), StyledNotification.NotificationStyle.kGameMessageNegative));
                    }
                    mNumStealAttempts = 0;
                    mObjectsFailedToSteal = new List<GameObject>();
                    mCallbackOnStealCompletion = callbackOnCompletion;
                    ForceSituationSpecificInteraction(Worker, Worker, new ServiceNPCSteal.Definition(this), null, mCallbackOnStealCompletion, StealFailed, new InteractionPriority(InteractionPriorityLevel.High));
                    return;
                }
                Lot.EffectiveHousehold.ModifyFamilyFunds(-totalCost);
            }
            MoveAllPossibleToTargetInventory();
            mbHasCharged = true;
            if (callbackOnCompletion != null)
            {
                callbackOnCompletion(Worker, 0f);
            }
        }

        public override CarService CreateServiceCar()
        {
            CustomService service = (CustomService)Service;
            CarService car = GlobalFunctions.CreateObjectOutOfWorld(new ResourceKey(service.Profile.CarInstanceId, 0x319E4F1D, service.Profile.CarGroupId), typeof(CarServiceMaidVan).FullName, null) as CarService;
            if (!string.IsNullOrEmpty(service.Profile.CarPreset))
            {
                car.ApplyPreset(service.Profile.CarPreset);
            }
            return car;
        }

        public static Inventory FindTargetSimInventory(Household household)
        {
            foreach (Sim sim in household.Sims)
            {
                if (sim.SimDescription.TeenOrAbove && sim.HasInventory)
                {
                    return sim.Inventory;
                }
            }
            return null;
        }

        public override void FreezeMotives()
        {
            Worker.Autonomy.Motives.MaxEverything();
            Worker.Autonomy.Motives.FreezeDecayEverythingExcept(CommodityKind.Energy, CommodityKind.Hygiene);
        }

        public List<Sim> GetExtendedHouseholdSims()
        {
            return Babysitter.GetExtendedHouseholdSims(Lot);
        }

        public override string GetUniformName(SimDescription simDescription)
        {
            ServiceUtils.ServiceProfile profile = (Service as CustomService)?.Profile as ServiceUtils.ServiceProfile;
            return profile?.GetUniformNameCallback == null ? base.GetUniformName(Worker.SimDescription) : profile.GetUniformNameCallback(Worker.SimDescription);
        }

        /*
        public override void OnArriveOnLot()
        {
            Tutorialette.TriggerLesson(Lessons.Maid, null);
            base.OnArriveOnLot();
        }
        */

        public void MoveAllPossibleToTargetInventory()
        {
            if (mProtectedInventory == null)
            {
                mProtectedInventory = Worker.Inventory.FindAll<IGameObject>(false);
            }
            Worker.Inventory.FindAll<IGameObject>(true, (gameObject, customData) => !mProtectedInventory.Contains(gameObject) && gameObject.ObjectOwnerComponent?.Thief != Worker && !Lot.EffectiveHousehold.Sims.Contains(gameObject.ObjectOwnerComponent?.GameObjectStolenFrom as Sim) && gameObject.ObjectOwnerComponent?.GameObjectStolenFrom != Lot && !((gameObject as MusicalInstrument)?.IsInBeingPlayedInteraction ?? false)).ForEach(x => TryMoveToTargetInventory(x));
        }

        public override void SetMotivesAndCommodities()
        {
            CustomService service = (CustomService)Service;
            CommonUtils.UpdateMotiveTunings(Worker, service.ServiceMotive);
            Worker.WorkMotive = service.ServiceMotive;
            foreach (CommodityKind motive in service.ServiceMotives)
            {
                Worker.Motives.CreateMotive(motive);
            }
        }

        public override void SetToFire(Sim serviceSim, Sim firer)
        {
            if (serviceSim == firer)
            {
                EventTracker.SendEvent(EventTypeId.kServiceNPCFired, firer, serviceSim);
                NPCLeavingMessage(LeavingReason.NPCSelfTerminated);
                UnsetServiceBed();
                SetState(new LeaveLot<CustomServiceSituation>(this));
                Service.FireSim(Worker);
            }
            else
            {
                base.SetToFire(serviceSim, firer);
            }
        }

        public virtual bool ShouldStartPreparingFoodFor(Sim sim)
        {
            return sim.Motives.IsHungry();
        }

        public override void SwitchWorkerToServiceOutfit()
        {
            DebugUtils.TryDisplayScriptError(() =>
                {
                    CustomService service = (CustomService)Service;
                    OutfitAssignmentUtils.OutfitAssignment outfitAssignment;
                    if (Worker.SimDescription.TryGetGlobalOutfitAssignment(service.Profile, out outfitAssignment) && Worker.AddAssignedOutfit(outfitAssignment.SpecialOutfitKey))
                    {
                        Worker.SimDescription.AddOutfit(new SimOutfit(Worker.SimDescription.GetSpecialOutfit(outfitAssignment.SpecialOutfitKey).Key), OutfitCategories.Career, true);
                        for (int i = Worker.SimDescription.GetOutfitCount(OutfitCategories.Career) - 1; i > 0; i--)
                        {
                            Worker.SimDescription.RemoveOutfit(OutfitCategories.Career, i, true);
                        }
                        if (!Worker.SimDescription.IsRobot)
                        {
                            Worker.SwitchToOutfitWithoutSpin(OutfitCategories.Career);
                        }
                        return;
                    }
                    if ((service.Profile as ServiceUtils.ServiceProfile)?.GetUniformFromName ?? false)
                    {
                        base.SwitchWorkerToServiceOutfit();
                        return;
                    }
                    Worker.SimDescription.AddOutfit(new SimOutfit(Worker.SimDescription.GetOutfit(OutfitCategories.Everyday, 0).Key), OutfitCategories.Career, true);
                    for (int i = Worker.SimDescription.GetOutfitCount(OutfitCategories.Career) - 1; i > 0; i--)
                    {
                        Worker.SimDescription.RemoveOutfit(OutfitCategories.Career, i, true);
                    }
                    if (!Worker.SimDescription.IsRobot)
                    {
                        Worker.SwitchToOutfitWithoutSpin(OutfitCategories.Career);
                    }
                });
        }

        public bool TryMoveToTargetInventory(IGameObject gameObject)
        {
            if (Lot.EffectiveHousehold != null && !(FindTargetSimInventory(Lot.EffectiveHousehold)?.TryToMove(gameObject) ?? false))
            {
                if (!(Lot.EffectiveHousehold.SharedFridgeInventory?.Inventory?.TryToMove(gameObject) ?? false))
                {
                    return Lot.EffectiveHousehold.SharedFamilyInventory?.Inventory?.TryToMove(gameObject) ?? false;
                }
            }
            return true;
        }
    }
}
