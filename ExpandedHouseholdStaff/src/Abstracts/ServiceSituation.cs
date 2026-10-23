using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Interfaces.Destrospean.ExpandedHouseholdStaff;
using Sims3.Gameplay.Objects;
using Sims3.Gameplay.Services;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.Utilities;
using Sims3.Gameplay.Destrospean.ExpandedHouseholdStaff;
using Sims3.Gameplay.Destrospean.ExpandedHouseholdStaff.Situations;
using Sims3.Gameplay.Destrospean.Utils;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using System;
using System.Collections.Generic;
using Destrospean.Enums;
using Destrospean.Utils;

namespace Sims3.Gameplay.Abstracts.Destrospean.ExpandedHouseholdStaff
{
    /// <summary>
    /// Service situation base class from which to derive all service situations for the Expanded Household Staff mod.
    /// </summary>
    public abstract class ServiceSituation<T> : ServiceSituation where T : ServiceSituation<T>
    {
        public class DummySituation : ChildSituation<ServiceSituation<T>>
        {
            public DummySituation()
            {
            }

            public DummySituation(ServiceSituation<T> parent) : base(parent)
            {
            }

            public override void Init(ServiceSituation<T> parent)
            {
                parent.OnArriveOnLot();
                parent.SetMotivesAndCommodities();
            }
        }

        public class NPCIsFired : ChildSituation<ServiceSituation<T>>
        {
            Sim mFirer;

            public NPCIsFired()
            {
            }

            public NPCIsFired(Sim firer, ServiceSituation<T> parent) : base(parent)
            {
                mFirer = firer;
            }

            public override void Init(ServiceSituation<T> parent)
            {
                DebugUtils.TryDisplayScriptError(() =>
                    {
                        Relationship relationship = Relationship.Get(parent.Worker, mFirer, true);
                        if (relationship.LTR.Liking <= 20)
                        {
                            ForceSituationSpecificInteraction(mFirer, parent.Worker, new SituationSocial.Definition("Insult", new string[0], null, false), null, OnFinished, OnFinished);
                        }
                        else if (relationship.LTR.Liking >= 50 || LTRData.Get(relationship.LTR.CurrentLTR).IsRomantic)
                        {
                            ForceSituationSpecificInteraction(mFirer, parent.Worker, new SituationSocial.Definition("Cry on Shoulder", new string[0], null, false), null, OnFinished, OnFinished);
                        }
                        else
                        {
                            ForceSituationSpecificInteraction(mFirer, parent.Worker, new SituationSocial.Definition("Chat", new string[0], null, false), null, OnFinished, OnFinished);
                        } 
                    });
            }

            public virtual void OnFinished(Sim actor, float x)
            {
                DebugUtils.TryDisplayScriptError(() =>
                    {
                        Parent.SetState(new LeaveLot<ServiceSituation<T>>(Parent));
                        Parent.Service.FireSim(actor);
                    });
            }
        }

        public class WaitToRoute : ChildSituation<ServiceSituation<T>>
        {
            AlarmHandle mAlarmHandle;

            public WaitToRoute()
            {
            }

            public WaitToRoute(ServiceSituation<T> parent) : base(parent)
            {
            }

            public override void Init(ServiceSituation<T> parent)
            {
                mAlarmHandle = AlarmManager.AddAlarm(parent.DelayBeforeArriving, TimeUnit.Hours, TimeToRoute, Parent.Service.GetType().Name + " waiting to route", AlarmType.DeleteOnReset, parent.Worker);
            }

            public void TimeToRoute()
            {
                RouteToLot<ServiceSituation<T>, DummySituation> routeToLot = new RouteToLot<ServiceSituation<T>, DummySituation>(Parent);
                routeToLot.SetRouteTime(Babysitter.DriveTime);
                Parent.SetState(routeToLot);
            }

            public override void CleanUp()
            {
                AlarmManager.RemoveAlarm(mAlarmHandle);
                base.CleanUp();
            }
        }

        AlarmHandle mCheckForFireAlarmHandle = AlarmHandle.kInvalidHandle;

        int mDateLastPaid;

        bool mInformedFireDepartment;

        AlarmHandle mPayLiveInServiceAlarmHandle = AlarmHandle.kInvalidHandle;

        AlarmHandle mTimeToFinishAlarmHandle = AlarmHandle.kInvalidHandle;

        public virtual float DelayBeforeArriving
        {
            get
            {
                return (float)(Service.GetType().GetProperty("DelayBeforeArriving")?.GetValue(null, null) ?? Babysitter.DelayBeforeArriving);
            }
        }

        public static Type DerivedType
        {
            get
            {
                return typeof(T);
            }
        }

        public virtual bool IsLiveInService
        {
            get
            {
                return Service is IAmLiveInService;
            }
        }

        public ulong LastInteractionId;

        public int NumDaysSinceLastPayment
        {
            get
            {
                int numDaysSinceLastPayment = SimClock.ElapsedCalendarDays() - mDateLastPaid;
                return numDaysSinceLastPayment > 0 ? numDaysSinceLastPayment : 1;
            }
        }

        public virtual bool ReportsFires
        {
            get
            {
                return false;
            }
        }

        public virtual bool ServiceTerminated
        {
            get
            {
                if (IsLiveInService)
                {
                    return false;
                }
                SetToLeave();
                return true;
            }
        }

        public abstract float TimeToSpendWorking
        {
            get;
        }

        public ServiceSituation()
        {
        }

        public ServiceSituation(Service service, Lot lot, Sim worker, int cost) : base(service, lot, worker, cost)
        {
            DebugUtils.TryDisplayScriptError(() =>
                {
                    if (IsLiveInService)
                    {
                        mDateLastPaid = SimClock.ElapsedCalendarDays();
                    }
                    worker.AssignRole(this);
                    worker.Autonomy.AllowedToRunMetaAutonomy = false;
                    FreezeMotives();
                    Type waitToRouteType = DerivedType.GetNestedType("WaitToRoute");
                    SetState(waitToRouteType == null ? new WaitToRoute(this) : (Situation)Activator.CreateInstance(waitToRouteType, this));
                    ScheduleSwitchWorkerToServiceOutfit();
                    AddCheckForFireAlarm();
                });
        }

        public ServiceSituation(DummyEnum dummyArgForBaseOfBase, Service service, Lot lot, Sim worker, int cost) : base(service, lot, worker, cost)
        {
        }

        public void AddCheckForFireAlarm()
        {
            mCheckForFireAlarmHandle = Worker.AddAlarmRepeating(Babysitter.BabysitterCheckForChildTime, TimeUnit.Minutes, CheckForFire, Babysitter.BabysitterCheckForChildTime, TimeUnit.Minutes, Service.GetType().Name + ": Check for Fire", AlarmType.DeleteOnReset);
        }

        public virtual bool ChargeForServiceWhileActive()
        {
            int totalCost = CostTotal();
            if (totalCost > 0)
            {
                if (Lot.Household.FamilyFunds < totalCost)
                {
                    SetToFire(Worker, Worker);
                    return false;
                }
                Lot.Household.ModifyFamilyFunds(-totalCost);
                StyledNotification.Show(new StyledNotification.Format(Localization.LocalizeString(Service.GetType().GetLocalizationKey() + ":WeeklyPayment", totalCost), Worker.ObjectId, StyledNotification.NotificationStyle.kSimTalking));
            }
            return true;
        }

        public void CheckForFire()
        {
            if (ServiceTerminated || !ReportsFires)
            {
                return;
            }
            bool isFireOnLot = Lot.IsFireOnLot();
            if (isFireOnLot && !mInformedFireDepartment)
            {
                mInformedFireDepartment = true;
                Firefighter firefighter = Firefighter.Instance;
                if (firefighter != null)
                {
                    firefighter.MakeServiceRequest(Lot, true, Worker.ObjectId);
                    StyledNotification.Show(new StyledNotification.Format(Localization.LocalizeString("Gameplay/Services/Babysitter:CalledFireDept"), Worker.ObjectId, StyledNotification.NotificationStyle.kSimTalking));
                }
                return;
            }
            if (!isFireOnLot)
            {
                mInformedFireDepartment = false;
            }
        }

        public override int CostTotal()
        {
            return IsLiveInService ? Cost * NumDaysSinceLastPayment / 7 : base.CostTotal();
        }

        public override void EndService()
        {
            Worker.RemoveAlarm(IsLiveInService ? mPayLiveInServiceAlarmHandle : mTimeToFinishAlarmHandle);
            Worker.RemoveAlarm(mCheckForFireAlarmHandle);
            RestoreMotives();
            Service = null;
            mDestroyWorkerOnExit = false;
            Exit();
        }

        public virtual void FreezeMotives()
        {
            Worker.Autonomy.Motives.MaxEverything();
            Worker.Autonomy.Motives.FreezeDecayEverythingExcept(CommodityKind.Fun, CommodityKind.Social);
        }

        public virtual string GetUniformName(SimDescription simDescription)
        {
            ResourceKey uniform;
            string uniformName;
            return ServiceNPCSpecifications.TryGetUniform(simDescription.Service.ServiceType.ToString(), simDescription.Gender, out uniform, out uniformName) ? uniformName : null;
        }

        public float GetNewInteractionPriorityValue()
        {
            InteractionPriority interactionPriority = new InteractionPriority(InteractionPriorityLevel.Zero, 0);
            if (Worker.CurrentInteraction != null)
            {
                interactionPriority = Worker.CurrentInteraction.GetPriority();
            }
            if (interactionPriority.Level == InteractionPriorityLevel.Zero)
            {
                return 1;
            }
            return interactionPriority.Value + 1;
        }

        public virtual bool IsInteractionBetterThanCurrent(InteractionInstance ii)
        {
            return ii.ScoreIsConsiderablyHigher(Worker.CurrentInteraction.GetPriority().Value) && Worker.CurrentInteraction.Autonomous;
        }

        public override string NotEnoughFundsMessage()
        {
            return Service.GetType().GetLocalizationKey() + ":NotEnoughFunds";
        }

        public virtual void OnArriveOnLot()
        {
            if (IsLiveInService)
            {
                mDateLastPaid = SimClock.ElapsedCalendarDays();
                mPayLiveInServiceAlarmHandle = AlarmManager.AddAlarmRepeating(1, TimeUnit.Weeks, PayLiveInService, 1, TimeUnit.Weeks, Service.GetType().Name + " weekly payment Alarm", AlarmType.AlwaysPersisted, Worker);
            }
            else
            {
                mTimeToFinishAlarmHandle = AlarmManager.AddAlarm(TimeToSpendWorking, TimeUnit.Hours, SetToJobDone, Service.GetType().Name + " finishing", AlarmType.DeleteOnReset, Worker);
            }
        }

        public void PayLiveInService()
        {
            if (ChargeForServiceWhileActive())
            {
                mDateLastPaid = SimClock.ElapsedCalendarDays();
            }
        }

        public virtual void SetMotivesAndCommodities()
        {
            Worker.Motives.MaxEverything();
            Worker.Motives.SetValue(CommodityKind.Fun, 95);
            Worker.Motives.SetValue(CommodityKind.Social, 0);
        }

        public override void SetToFire(Sim serviceSim, Sim firer)
        {
            base.SetToFire(serviceSim, firer);
            NPCLeavingMessage(LeavingReason.NPCFired);
            UnsetServiceBed();
            Type npcIsFiredType = DerivedType.GetNestedType("NPCIsFired");
            SetState(npcIsFiredType == null ? new NPCIsFired(firer, this) : (Situation)Activator.CreateInstance(npcIsFiredType, firer, this));
        }

        public virtual void SetToJobDone()
        {
            NPCLeavingMessage(LeavingReason.NPCJobDone);
            UnsetServiceBed();
            SetState(new LeaveLot<ServiceSituation<T>>(this));
        }

        public override void SetToLeave()
        {
            NPCLeavingMessage(LeavingReason.NPCDismissed);
            UnsetServiceBed();
            SetState(new LeaveLot<ServiceSituation<T>>(this));
        }

        public override void SwitchWorkerToServiceOutfit()
        {
            DebugUtils.TryDisplayScriptError(() =>
                {
                    string uniformName = GetUniformName(Worker.SimDescription);
                    if (uniformName != null)
                    {
                        SimOutfit resultOutfit;
                        if (OutfitUtils.TryApplyUniformToOutfit(Worker.SimDescription.GetOutfit(OutfitCategories.Everyday, 0), new SimOutfit(ResourceKey.CreateOutfitKeyFromProductVersion(uniformName, ProductVersion.BaseGame)), Worker.SimDescription, DerivedType.Name + ".SwitchWorkerToServiceOutfit", out resultOutfit))
                        {
                            Worker.SimDescription.AddOutfit(resultOutfit, OutfitCategories.Career, true);
                            for (int i = Worker.SimDescription.GetOutfitCount(OutfitCategories.Career) - 1; i > 0; i--)
                            {
                                Worker.SimDescription.RemoveOutfit(OutfitCategories.Career, i, true);
                            }
                        }
                    }
                    base.SwitchWorkerToServiceOutfit();
                });
        }

        public void UnsetServiceBed()
        {
            IBed bed = Worker.Bed as IBed;
            if (bed != null && bed.LotCurrent == Lot)
            {
                bed.RelinquishOwnership(Worker, null);
            }
        }
    }
}
