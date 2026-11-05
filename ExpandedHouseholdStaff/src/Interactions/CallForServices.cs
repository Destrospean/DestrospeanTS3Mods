using System.Collections.Generic;
using Destrospean.Utils;
using Destrospean.Utils.ExpandedHouseholdStaff;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Destrospean.ExpandedHouseholdStaff.Services;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces.Destrospean.ExpandedHouseholdStaff;
using Sims3.Gameplay.Moving;
using Sims3.Gameplay.Objects.Electronics;
using Sims3.Gameplay.Services;
using Sims3.Gameplay.Tutorial;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;

namespace Sims3.Gameplay.Destrospean.ExpandedHouseholdStaff.Interactions
{
    /// <summary>
    /// Replacement interaction for calling for services.
    /// </summary>
    public class CallForServices : Phone.Call
    {
        public class ServicesModel : UI.ServicesModel, IServicesModel
        {
            private class ServiceInfoEx : ServiceInfo
            {
                public Service Service;
            }

            List<ServiceInfo> IServicesModel.GetServices()
            {
                var serviceInfoList = new List<ServiceInfo>();
                var services = new List<Service>
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
                foreach (var service in ServiceUtils.CustomInstances.Values)
                {
                    services.Add(service);
                }
                foreach (var service in services)
                {
                    if (service != null && (service.CanRequestServiceFromPhone(Lot) || Settings.kAllowAllServicesOnCommunityLots))
                    {
                        serviceInfoList.Add(GetServiceInfo(this, service));
                    }
                }
                return serviceInfoList;
            }

            bool IServicesModel.MakeServiceRequest(ServiceInfo info)
            {
                if (!(info is ServiceInfoEx) && info.mServiceType == 100)
                {
                    mLot.Household.AutoBabysitter = info.mActive;
                    return true;
                }
                var service = GetServiceFromInfo(this, info);

                // The following code dismisses all workers of a service from a lot if said service is cancelled by phone from said lot.
                if (service != null && (service.IsServiceRequested(mLot) || service.IsAnySimAssignedToLot(mLot)))
                {
                    foreach (var sim in service.GetSimsAssignedToLot(mLot))
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
                return info is ServiceInfoEx serviceInfo ? serviceInfo.Service : servicesModel.GetServiceFromInfo(info);
            }

            public static ServiceInfo GetServiceInfo(UI.ServicesModel servicesModel, Service service)
            {
                if (!service.IsFromExpandedHouseholdStaff())
                {
                    return servicesModel.GetServiceInfo(service);
                }
                var entryKey = service.GetType().GetLocalizationKey();
                var serviceInfo = new ServiceInfoEx();
                var customService = service as CustomService;
                serviceInfo.mName = customService?.Profile.Title ?? Localization.LocalizeString(entryKey + ":Title");
                serviceInfo.mAlreadyActiveToolTip = Localization.LocalizeString("Gameplay/UI/ServicesUIWindow:AlreadyActive");
                if (service.IsRecurrent())
                {
                    serviceInfo.mAlreadyActiveToolTip = Localization.LocalizeString("Gameplay/UI/ServicesUIWindow:RecurrentAlreadyActive");
                    var isNonLiveInServiceActiveOnLot = !(service is IAmLiveInService) && !(customService?.Profile.IsLiveInService ?? false) && service.IsAnySimAssignedToLot(servicesModel.Lot);
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

            public new static bool Show(Lot lot, ObjectGuid simGuid)
            {
                UI.Responder.Instance.mServicesModel = new ServicesModel();
                var servicesModel = (ServicesModel)UI.Responder.Instance.ServicesModel;
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
                return new[]
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
                if (!actor.HouseholdOwnsResidentialLot(actor.LotCurrent) && !Settings.kAllowAllServicesOnCommunityLots)
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
