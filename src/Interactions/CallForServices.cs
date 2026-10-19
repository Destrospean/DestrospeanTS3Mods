using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces.Destrospean.ExpandedHouseholdStaff;
using Sims3.Gameplay.Moving;
using Sims3.Gameplay.Objects.Electronics;
using Sims3.Gameplay.Services;
using Sims3.Gameplay.Tutorial;
using Sims3.Gameplay.Utilities;
using Sims3.Gameplay.Destrospean.ExpandedHouseholdStaff.Services;
using Sims3.SimIFace;
using Sims3.UI;
using System;
using System.Collections.Generic;
using Destrospean.Utils;
using Destrospean.Utils.ExpandedHouseholdStaff;

namespace Sims3.Gameplay.Destrospean.ExpandedHouseholdStaff.Interactions
{
    /// <summary>
    /// Replacement interaction for calling for services.
    /// </summary>
    public class CallForServices : Phone.Call
    {
        public class ServicesModel : UI.ServicesModel, IServicesModel
        {
            class ServiceInfoEx : ServiceInfo
            {
                public Service Service;
            }

            List<ServiceInfo> IServicesModel.GetServices()
            {
                List<ServiceInfo> serviceInfoList = new List<ServiceInfo>();
                List<Service> services = new List<Service>
                    {
                        Babysitter.Instance,
                        Maid.Instance,
                        PizzaDelivery.Instance,
                        Repairman.Instance,
                        SocialWorkerAdoption.Instance,
                        Police.Instance,
                        Firefighter.Instance,
                        sNewspaperDeliveryService,
                        Butler.Instance,
                        BartenderService.Instance,
                        Magician.Instance,
                        DJ.Instance,
                        Singer.Instance,
                        PerformanceArtist.Instance
                    };
                services.AddRange(ServiceUtils.PredefinedInstances.Values);
                foreach (Service service in ServiceUtils.CustomInstances.Values)
                {
                    services.Add(service);
                }
                foreach (Service service in services)
                {
                    if (service != null && service.CanRequestServiceFromPhone(Lot))
                    {
                        serviceInfoList.Add(GetServiceInfo(this, service));
                    }
                }
                return serviceInfoList;
            }

            bool IServicesModel.MakeServiceRequest(ServiceInfo info)
            {
                if (info as ServiceInfoEx == null && info.mServiceType == 100)
                {
                    mLot.Household.AutoBabysitter = info.mActive;
                    return true;
                }
                Service service = GetServiceFromInfo(this, info);

                // The following code dismisses all workers of a service from a lot if said service is cancelled by phone from said lot.
                if (service != null && (service.IsServiceRequested(mLot) || service.IsAnySimAssignedToLot(mLot)))
                {
                    foreach (Sim sim in service.GetSimsAssignedToLot(mLot))
                    {
                        ServiceSituation.FindServiceSituationInvolving(sim)?.SetToLeave();
                    }
                }

                if (service != null && info.mActive != service.IsServiceRequested(mLot))
                {
                    service.MakeServiceRequest(mLot, info.mActive, SimGuid);
                    SimGuid = ObjectGuid.InvalidObjectGuid;
                    return true;
                }
                return false;
            }

            public static Service GetServiceFromInfo(UI.ServicesModel servicesModel, ServiceInfo info)
            {
                ServiceInfoEx serviceInfo = info as ServiceInfoEx;
                return serviceInfo == null ? servicesModel.GetServiceFromInfo(info) :  serviceInfo.Service;
            }

            public static ServiceInfo GetServiceInfo(UI.ServicesModel servicesModel, Service service)
            {
                if (!service.IsFromExpandedHouseholdStaff())
                {
                    return servicesModel.GetServiceInfo(service);
                }
                CustomService customService = service as CustomService;
                string entryKey = service.GetType().GetLocalizationKey();
                ServiceInfoEx serviceInfo = new ServiceInfoEx();
                serviceInfo.mName = customService?.Profile.Title ?? Localization.LocalizeString(entryKey + ":Title");
                serviceInfo.mAlreadyActiveToolTip = Localization.LocalizeString("Gameplay/UI/ServicesUIWindow:AlreadyActive");
                if (service.IsRecurrent())
                {
                    serviceInfo.mAlreadyActiveToolTip = Localization.LocalizeString("Gameplay/UI/ServicesUIWindow:RecurrentAlreadyActive");
                    bool isNonLiveInServiceActiveOnLot = service as IAmLiveInService == null && !(customService?.Profile.IsLiveInService ?? false) && service.IsAnySimAssignedToLot(servicesModel.Lot);
                    serviceInfo.mCancelledTns = customService == null ? Localization.LocalizeString(entryKey + ":ServiceCancelled" + (isNonLiveInServiceActiveOnLot ? "WhileActive" : "")) : isNonLiveInServiceActiveOnLot ? customService.Profile.CancelledWhileActiveMessage : customService.Profile.CancelledMessage;
                }
                serviceInfo.mRequestedTns = customService == null ? Localization.LocalizeString(entryKey + ":ServiceRequested") : customService.Profile.RequestedMessage;
                serviceInfo.mServiceType = (int)service.ServiceType;
                serviceInfo.mActive = service.IsServiceRequested(servicesModel.Lot) || service.IsAnySimAssignedToLot(servicesModel.Lot);
                serviceInfo.mPrice = service.IsEmergencyService ? 0 : service.Cost();
                serviceInfo.mRecurring = service.IsRecurrent();
                serviceInfo.mIsPaidWeekly = service.IsPaidWeekly;
                serviceInfo.mActiveButMissingRequirementToolTip = "";
                serviceInfo.Service = service;
                return serviceInfo;
            }

            public static new bool Show(Lot lot, ObjectGuid simGuid)
            {
                UI.Responder.Instance.mServicesModel = new ServicesModel();
                ServicesModel servicesModel = (ServicesModel)UI.Responder.Instance.ServicesModel;
                servicesModel.Lot = lot;
                servicesModel.SimGuid = simGuid;
                return ServicesController.Show(simGuid);
            }
        }

        [DoesntRequireTuning]
        public class Definition : CallDefinition<CallForServices>
        {
            public override string GetInteractionName(ref InteractionInstanceParameters parameters)
            {
                return LocalizeString("InteractionName");
            }

            public override string[] GetPath(bool isFemale)
            {
                return new string[]
                {
                    Phone.LocalizeString("Services") + Localization.Ellipsis
                };
            }

            public override bool Test(Sim actor, Phone target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                if (!target.IsUsableBy(actor))
                {
                    return false;
                }
                if (!actor.HouseholdOwnsResidentialLot(actor.LotCurrent))
                {
                    greyedOutTooltipCallback = () => LocalizeString("ServicesOnlyOnHomeLot");
                    return false;
                }
                return base.Test(actor, target, isAutonomous, ref greyedOutTooltipCallback);
            }
        }

        public static string LocalizeString(string name, params object[] parameters)
        {
            return Localization.LocalizeString("Gameplay/Objects/Electronics/Phone/CallForServices:" + name, parameters);
        }

        public override ConversationBehavior OnCallConnected()
        {
            if (!UIUtils.IsOkayToStartModalDialog())
            {
                return ConversationBehavior.JustHangUp;
            }
            if (Actor.LotHome == null)
            {
                return ConversationBehavior.ShakeHead;
            }
            if (Actor.Household.IsActive && !MovingSituation.MovingInProgress)
            {
                Tutorialette.TriggerLesson(Lessons.HiringServices, Actor);
                if (ServicesModel.Show(Actor.LotCurrent, Actor.ObjectId))
                {
                    return ConversationBehavior.TalkBriefly;
                }
            }
            return ConversationBehavior.JustHangUp;
        }
    }
}
