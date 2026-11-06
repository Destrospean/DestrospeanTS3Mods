extern alias NRaasTagger;
using System;
using System.Collections.Generic;
using System.Reflection;
using Destrospean.ExpandedHouseholdStaff.Interactions;
using Destrospean.Utils;
using Destrospean.Utils.ExpandedHouseholdStaff;
using Sims3.Gameplay;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.ActorSystems.Children;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Destrospean.ExpandedHouseholdStaff.Interactions;
using Sims3.Gameplay.Destrospean.ExpandedHouseholdStaff.Services;
using Sims3.Gameplay.Destrospean.Utils;
using Sims3.Gameplay.DreamsAndPromises;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Interfaces.Destrospean.ExpandedHouseholdStaff;
using Sims3.Gameplay.ObjectComponents;
using Sims3.Gameplay.Objects.Electronics;
using Sims3.Gameplay.Objects.Gardening;
using Sims3.Gameplay.Objects.Register;
using Sims3.Gameplay.Objects.Vehicles;
using Sims3.Gameplay.Services;
using Sims3.Gameplay.Situations;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.UI;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI.CAS;
using Aging = NRaasTagger::NRaas.CommonSpace.Helpers.Aging;
using Common = NRaasTagger::NRaas.Common;
using OccultTypeHelper = NRaasTagger::NRaas.CommonSpace.Helpers.OccultTypeHelper;
using Queries = Sims3.Gameplay.Queries;
using SimTypes = NRaasTagger::NRaas.CommonSpace.Helpers.SimTypes;
using TagDataHelper = NRaasTagger::NRaas.TaggerSpace.Helpers.TagDataHelper;
using Tagger = NRaasTagger::NRaas.Tagger;

namespace Destrospean.ExpandedHouseholdStaff
{
    public class Main
    {
        [Tunable]
        protected static bool kInstantiator;

        static Main()
        {
            CommonUtils.ReplaceMethod<Main, Inventory>(nameof(AddInternal_Original), nameof(AddInternal));
            CommonUtils.ReplaceMethod<Inventory, Main>(nameof(AddInternal));
            CommonUtils.ReplaceMethod<Main, CaregiverRoutingMonitor>(nameof(CountBabiesToddlersAndCaregivers_Original), nameof(CountBabiesToddlersAndCaregivers));
            CommonUtils.ReplaceMethod<CaregiverRoutingMonitor, Main>(nameof(CountBabiesToddlersAndCaregivers));
            CommonUtils.ReplaceMethod<Main, Plant>(nameof(GardenInteractionLotValidityTest_Original), nameof(GardenInteractionLotValidityTest));
            CommonUtils.ReplaceMethod<Plant, Main>(nameof(GardenInteractionLotValidityTest));
            CommonUtils.ReplaceMethod<Main, SocialComponent>(nameof(IsInServicePreventingSocialization_Original), nameof(IsInServicePreventingSocialization));
            CommonUtils.ReplaceMethod<SocialComponent, Main>(nameof(IsInServicePreventingSocialization));
            CommonUtils.ReplaceMethod<Main, SimDescription>(nameof(IsPerformingAnyServiceOfType_Original), nameof(IsPerformingAnyServiceOfType));
            CommonUtils.ReplaceMethod<SimDescription, Main>(nameof(IsPerformingAnyServiceOfType));
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                switch (assembly.GetName().Name)
                {
                    case "NRaasMasterController":
                        NRaasMasterControllerIntegration.Init();
                        break;
                    case "NRaasTagger":
                        IntegrateNRaasTagger();
                        break;
                }
            }
            InteractionObjectTypeUtils.InitTypes();
            //List<EventListener> listeners = new List<EventListener>();
            LoadSaveManager.ObjectGroupsPreLoad += () => Phone.CallForServices.Singleton = new CallForServices.Definition();
            World.OnObjectPlacedInLotEventHandler += (sender, e) => DebugUtils.TryDisplayScriptError(() =>
                {
                    if (e is World.OnObjectPlacedInLotEventArgs onObjectPlacedInLotEventArgs)
                    {
                        var gameObject = GameObject.GetObject(onObjectPlacedInLotEventArgs.ObjectId);
                        gameObject.AddInteraction(ListInteractions.Singleton, true);
                        gameObject.AddInteraction(AddInventoryObjectToService.Singleton, true);
                        (gameObject as Car)?.AddInteraction(AssignCarToService.Singleton, true);
                        (gameObject as Mailbox).AddServiceProfileInteractions();
                    }
                });
            World.sOnWorldLoadFinishedEventHandler += (sender, e) => DebugUtils.TryDisplayScriptError(() =>
                {
                    foreach (var gameObject in Queries.GetObjects<GameObject>())
                    {
                        gameObject.AddInteraction(ListInteractions.Singleton, true);
                        gameObject.AddInteraction(AddInventoryObjectToService.Singleton, true);
                        (gameObject as Car)?.AddInteraction(AssignCarToService.Singleton, true);
                        (gameObject as Mailbox).AddServiceProfileInteractions();
                    }
                    foreach (var profile in new List<IServiceProfile>(ServiceUtils.ServiceProfiles))
                    {
                        if (profile.IsImmutable)
                        {
                            profile.RemoveServiceFromSaveGame();
                            continue;
                        }
                        CustomService.Init(profile);
                        profile.FixUp();
                    }
                });
            World.sOnWorldQuitEventHandler += (sender, e) =>
                {
                    /*
                    foreach (EventListener listener in listeners)
                    {
                        EventTracker.RemoveListener(listener);
                    }
                    listeners.Clear();
                    */
                    foreach (var service in new List<CustomService>(ServiceUtils.CustomInstances.Values))
                    {
                        CustomService.Deinit(service.Profile, true);
                    }
                };
        }

        public InventoryItem AddInternal(IGameObject gameObject, uint stackNumber, InventoryStack stack, bool testPurge)
        {
            if ((((Inventory)(object)this).Owner as Sim)?.Service?.IsFromExpandedHouseholdStaff() ?? false)
            {
                testPurge = false;
            }
            return AddInternal_Original(gameObject, stackNumber, stack, testPurge);
        }

        public InventoryItem AddInternal_Original(IGameObject gameObject, uint stackNumber, InventoryStack stack, bool testPurge)
        {
            throw new NotImplementedException();
        }
        
        public static void CountBabiesToddlersAndCaregivers(Household household, Lot lot, Dictionary<Sim, Vector3> lastOnLotPositions, out int babyToddlerCount, out int caregiverCount, out int routingCaregiverCount, out Sim arbitraryBabyOrToddler, bool countServicesAsCaregiversEvenWithoutToddlers)
        {
            CountBabiesToddlersAndCaregivers_Original(household, lot, lastOnLotPositions, out babyToddlerCount, out caregiverCount, out routingCaregiverCount, out arbitraryBabyOrToddler, countServicesAsCaregiversEvenWithoutToddlers);
            var adjustedBabyToddlerCount = countServicesAsCaregiversEvenWithoutToddlers && babyToddlerCount < 1 ? 1 : babyToddlerCount;
            foreach (var service in ServiceUtils.CustomInstances.Values)
            {
                if (service.Profile.IsBabysittingService && (service.NumSimsAssignedToLot(lot) > 0 || service.IsServiceRequested(lot)))
                {
                    caregiverCount += adjustedBabyToddlerCount;
                }
            }
        }

        public static void CountBabiesToddlersAndCaregivers_Original(Household household, Lot lot, Dictionary<Sim, Vector3> lastOnLotPositions, out int babyToddlerCount, out int caregiverCount, out int routingCaregiverCount, out Sim arbitraryBabyOrToddler, bool countServicesAsCaregiversEvenWithoutToddlers)
        {
            throw new NotImplementedException();
        }

        public bool GardenInteractionLotValidityTest(Sim sim)
        {
            return (sim?.Service?.IsFromExpandedHouseholdStaff() ?? false) && ServiceSituation.FindServiceSituationInvolving(sim)?.Lot == ((Plant)(object)this).LotCurrent || GardenInteractionLotValidityTest_Original(sim);
        }

        public bool GardenInteractionLotValidityTest_Original(Sim sim)
        {
            throw new NotImplementedException();
        }

        public static string GetStatus(SimDescription sim)
        {
            var text = sim.FullName;
            var isServiceOrRole = false;
            if (sim.AssignedRole != null)
            {
                isServiceOrRole = true;
                if (sim.AssignedRole.RoleGivingObject is ShoppingRegister shoppingRegister)
                {
                    text += ", " + shoppingRegister.RegisterRoleName(sim.IsFemale);
                }
                else if (StringTable.GetLocalizedString(sim.AssignedRole.CareerTitleKey, out var localizedString))
                {
                    text += ", " + localizedString;
                }
                var roleHours = TagDataHelper.GetRoleHours(sim);
                if (!string.IsNullOrEmpty(roleHours))
                {
                    text += Common.NewLine + roleHours;
                }
            }
            else if (SimTypes.InServicePool(sim))
            {
                isServiceOrRole = true;
                if (sim.CreatedByService is CustomService customService)
                {
                    text += ", " + customService.Profile.Title;
                }
                else if (StringTable.GetLocalizedString("Ui/Caption/Services/Service:" + sim.CreatedByService.ServiceType, out var localizedString))
                {
                    text += ", " + localizedString;
                }
            }
            if (Tagger.Settings.mCustomSimTitles.TryGetValue(sim.SimDescriptionId, out var customTitles))
            {
                var perLine = isServiceOrRole ? 1 : 2;
                isServiceOrRole = true;
                foreach (var title in customTitles)
                {
                    if (perLine == 0)
                    {
                        text += Common.NewLine;
                        perLine = 3;
                    }
                    text += ", " + title;
                    perLine--;
                }
            }
            text = !isServiceOrRole && (!Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.Orientation] || !Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.LifeStage] || !Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.AgeInDays] && !Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.AgeInYears] && (!Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.DaysTillNextStage] || !sim.AgingEnabled)) ? text + " - " : text + Common.NewLine;
            if (!sim.IsPet)
            {
                if (sim.TeenOrAbove && Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.Orientation])
                {
                    text += Common.Localize("SimType:" + TagDataHelper.GetOrientation(sim));
                }
                if (Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.Zodiac] && sim.Zodiac != Zodiac.Unset)
                {
                    if (Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.Orientation])
                    {
                        text += " ";
                    }
                    text += Common.LocalizeEAString("Ui/Caption/HUD/KnownInfoDialog:" + sim.Zodiac);
                }
            }
            if (Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.LifeStage])
            {
                if (!sim.IsPet && Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.Orientation] || Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.Zodiac] && sim.Zodiac != Zodiac.Unset)
                {
                    text += " ";
                }
                text += sim.AgeLocalizedText;
            }
            if (Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.AgeInDays] || Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.AgeInYears] || Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.DaysTillNextStage])
            {
                text += " (";
            }
            if (Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.AgeInDays] || Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.AgeInYears])
            {
                text += Common.Localize("TagData:Age", sim.IsFemale);
            }
            if (Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.AgeInDays] && !Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.AgeInYears])
            {
                text += " " + Common.Localize("TagData:Days", sim.IsFemale, new object[]
                    {
                        Math.Round(Aging.GetCurrentAgeInDays(sim))
                    });
            }
            if (Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.AgeInYears])
            {
                text += " " + Common.Localize("TagData:Years", sim.IsFemale, new object[]
                    {
                        Math.Round(Aging.GetCurrentAgeInDays(sim) / Tagger.Settings.TagDataAgeInYearsLength) + 1.0
                    });
            }
            if (sim.AgingEnabled && Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.DaysTillNextStage])
            {
                text += ", " + Common.Localize("TagData:Birthday", sim.IsFemale, new object[]
                    {
                        (int)(AgingManager.Singleton.AgingYearsToSimDays(AgingManager.GetMaximumAgingStageLength(sim)) - AgingManager.Singleton.AgingYearsToSimDays(sim.AgingYearsSinceLastAgeTransition))
                    });
            }
            if (Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.AgeInDays] || Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.AgeInYears] || Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.DaysTillNextStage])
            {
                text += ")";
            }
            if (Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.Occult] && sim.OccultManager != null)
            {
                var occultTypes = OccultTypeHelper.CreateList(sim, false);
                if (occultTypes.Count > 0)
                {
                    text += Common.NewLine;
                    var listString = "";
                    foreach (var occultType in occultTypes)
                    {
                        listString += ", " + OccultTypeHelper.GetLocalizedName(occultType);
                    }
                    text += Common.Localize("TagData:OccultTag", sim.IsFemale, new object[]
                        {
                            listString.Remove(0, 2)
                        });
                }
            }
            var createdSim = sim.CreatedSim;
            if (Tagger.Settings.Debugging && createdSim != null)
            {
                text += Common.NewLine + "Autonomy: ";
                if (createdSim.Autonomy == null)
                {
                    text += "None";
                }
                else
                {
                    text = createdSim.Autonomy.AutonomyDisabled ? text + "Disabled" : AutonomyRestrictions.IsAnyAutonomyEnabled(createdSim) ? createdSim.Autonomy.IsRunningHighLODSimulation ? text + "High" : text + "Low" : text + "User Disabled";
                    if (createdSim.Autonomy.ShouldRunLocalAutonomy)
                    {
                        text += " Local";
                    }
                    if (createdSim.CanRunAutonomyImmediately())
                    {
                        text += " Ready";
                    }
                    else if (!createdSim.mLastInteractionWasAutonomous)
                    {
                        text += " Push";
                    }
                    else if (!createdSim.mLastInteractionSucceeded)
                    {
                        text += " Fail";
                    }
                    if (createdSim.Autonomy.InAutonomyManagerQueue)
                    {
                        text += " Queued";
                    }
                }
            }
            if (createdSim != null)
            {
                if (Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.Mood])
                {
                    text += Common.NewLine;
                    var moodFlavor = (int)createdSim.MoodManager.MoodFlavor;
                    text += Common.Localize("TagData:MoodTag", sim.IsFemale, new object[]
                        {
                            Common.LocalizeEAString(false, "Ui/Tooltip/HUD/SimDisplay:MoodFlavor" + moodFlavor)
                        }) + " ";
                }
                if (Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.MotiveInfo])
                {
                    if (!Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.Mood])
                    {
                        text += Common.NewLine;
                    }
                    var listString = ", ";
                    var motiveIndex = 0;
                    foreach (var motive in ((HudModel)Responder.Instance.HudModel).GetMotives(sim.CreatedSim))
                    {
                        if (sim.CreatedSim.Motives.HasMotive(motive))
                        {
                            if (motiveIndex > 5)
                            {
                                break;
                            }
                            listString += TagDataHelper.FetchMotiveLocalization(sim.Species, motive) + ": (" + sim.CreatedSim.Motives.GetValue(motive).ToString("0.") + ") ";
                        }
                        motiveIndex++;
                    }
                    if (sim.CreatedSim.Motives.HasMotive(CommodityKind.Temperature))
                    {
                        listString += TagDataHelper.FetchMotiveLocalization(sim.Species, CommodityKind.Temperature) + ": (" + sim.CreatedSim.Motives.GetValue(CommodityKind.Temperature).ToString("0.") + ") ";
                    }
                    text += Common.Localize("TagData:Motives", sim.IsFemale, new object[]
                        {
                            listString.Remove(0, 2)
                        });
                }
                if (Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.CurrentInteraction] && createdSim.CurrentInteraction != null)
                {
                    text += Common.NewLine;
                    try
                    {
                        text += createdSim.CurrentInteraction;
                    }
                    catch (Exception exception)
                    {
                        Common.DebugException(createdSim, exception);
                        text += createdSim.CurrentInteraction.GetType();
                    }
                    var currentTone = createdSim.CurrentInteraction.CurrentTone;
                    if (currentTone != null)
                    {
                        text += Common.NewLine + currentTone;
                    }
                    if (createdSim.CurrentInteraction is SocialInteractionBase socialInteractionBase && socialInteractionBase.Target != null)
                    {
                        text += " " + Common.Localize("TagData:With", sim.IsFemale, new object[]
                            {
                                socialInteractionBase.Target.Name
                            });
                    }
                    if (createdSim.CurrentInteraction is Terrain.GoHereWith)
                    {
                        var inviteToLotSituation = InviteToLotSituation.FindInviteToLotSituationInvolving(createdSim);
                        if (inviteToLotSituation != null)
                        {
                            if (inviteToLotSituation.SimA != createdSim)
                            {
                                text += " " + inviteToLotSituation.SimA.Name;
                            }
                            else if (inviteToLotSituation.SimB != createdSim)
                            {
                                text += inviteToLotSituation.SimB.Name;
                            }
                        }
                    }
                }
            }
            var getDebtAndNetWorth = (Common.MethodStore)typeof(TagDataHelper).GetField("sGetDebtAndNetworth", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            if (!SimTypes.IsSpecial(sim))
            {
                text += Common.NewLine + Common.Localize("TagData:CashTag", sim.IsFemale, new object[]
                    {
                        sim.FamilyFunds
                    });
                if ((Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.Debt] || Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.NetWorth]) && getDebtAndNetWorth.Valid)
                {
                    var netWorth = 0;
                    if (Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.Debt])
                    {
                        netWorth++;
                    }
                    if (Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.NetWorth])
                    {
                        netWorth += 4;
                    }
                    text += getDebtAndNetWorth.Invoke<string>(new object[]
                        {
                            sim,
                            netWorth
                        });
                }
                if (Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.LifetimeWant] && sim.HasLifetimeWish)
                {
                    var majorDream = DreamsAndPromisesManager.GetMajorDream(sim.LifetimeWish);
                    var completed = "";
                    if (sim.CreatedSim != null && sim.CreatedSim.DreamsAndPromisesManager != null)
                    {
                        var lifetimeWishNode = sim.CreatedSim.DreamsAndPromisesManager.LifetimeWishNode;
                        completed = " " + lifetimeWishNode.AmountCompleted;
                    }
                    if (majorDream != null)
                    {
                        text += Common.NewLine + Common.Localize("TagData:LifetimeWantTag", sim.IsFemale, new object[]
                            {
                                majorDream.GetMajorWishName(sim)
                            }) + completed;
                    }
                }
                if (Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.LifetimeHappiness] && sim.LifetimeHappiness != 0)
                {
                    text = Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.LifetimeWant] && (!Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.LifetimeWant] || sim.HasLifetimeWish) ? text + " " : text + Common.NewLine;
                    text += Common.Localize("TagData:LifetimeHappinessSpendable", sim.IsFemale, new object[]
                        {
                            sim.LifetimeHappiness,
                            sim.SpendableHappiness
                        });
                }
                if (Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.Job] && sim.Occupation != null)
                {
                    text += Common.NewLine;
                    text = sim.Occupation.OfficeLocation == null ? text + Common.Localize("TagData:JobTag", sim.IsFemale, new object[]
                        {
                            sim.Occupation.CurLevelJobTitle
                        }) : text + Common.Localize("TagData:JobAt", sim.IsFemale, new object[]
                            {
                                sim.Occupation.CurLevelJobTitle,
                                sim.Occupation.OfficeLocation.GetLocalizedName()
                            });
                }
                if (Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.PartnerInfo] && sim.Partner != null)
                {
                    var relationship = sim.GetRelationship(sim.Partner, false);
                    var happilyOrUnhappily = "Happily";
                    if (relationship != null && relationship.CurrentLTRLiking < -15f)
                    {
                        happilyOrUnhappily = "Unhappily";
                    }
                    text += Common.NewLine;
                    text = sim.IsMarried ? text + Common.Localize("TagData:Spouse", sim.IsFemale, new object[]
                        {
                            Common.Localize("TagData:" + happilyOrUnhappily),
                            sim.Partner
                        }) : text + Common.Localize("TagData:Partner", sim.IsFemale, new object[]
                            {
                                sim.Partner
                            });
                }
            }
            if (sim.IsPregnant && Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.PregnancyInfo])
            {
                var miniSimDescription = SimDescription.Find(sim.Pregnancy.DadDescriptionId) ?? (IMiniSimDescription)MiniSimDescription.Find(sim.Pregnancy.DadDescriptionId);
                text += Common.NewLine;
                if (miniSimDescription != null)
                {
                    if (sim.Partner != null && miniSimDescription != sim.Partner && !sim.IsPet)
                    {
                        text += Common.Localize("TagData:Pregnancy", sim.IsFemale, new object[]
                            {
                                miniSimDescription,
                                Common.Localize("TagData:Uhoh")
                            });
                    }
                    else
                    {
                        text += Common.Localize("TagData:Pregnancy", sim.IsFemale, new object[]
                            {
                                miniSimDescription
                            });
                    }
                }
                else
                {
                    text += Common.Localize("TagData:PregnancyUnknown", sim.IsFemale);
                }
            }
            var getClanInfo = (Common.MethodStore)typeof(TagDataHelper).GetField("sGetClanInfo", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            if (Tagger.Settings.mTagDataSettings[TagDataHelper.TagDataType.PersonalityInfo] && getClanInfo.Valid)
            {
                foreach (var info in getClanInfo.Invoke<List<string>>(new object[]
                    {
                        sim
                    }))
                {
                    text += Common.NewLine + info;
                }
            }
            return text;
        }

        public static void IntegrateNRaasTagger()
        {
            CommonUtils.ReplaceMethod<TagDataHelper, Main>(nameof(GetStatus));
        }

        public static bool IsInServicePreventingSocialization(Sim target)
        {
            return ((target.Service as CustomService)?.Profile.PreventSocialization ?? !(target.Service is IAmSociableService)) && IsInServicePreventingSocialization_Original(target);
        }

        public static bool IsInServicePreventingSocialization_Original(Sim target)
        {
            throw new NotImplementedException();
        }

        public bool IsPerformingAnyServiceOfType(ServiceType serviceType)
        {
            var customService = ((SimDescription)(object)this).Service as CustomService;
            return serviceType == ServiceType.Butler && (customService?.Profile.IsLiveInService ?? false) || serviceType == ServiceType.ChildCareServices && (customService?.Profile.IsBabysittingService ?? false) || IsPerformingAnyServiceOfType_Original(serviceType);
        }

        public bool IsPerformingAnyServiceOfType_Original(ServiceType serviceType)
        {
            throw new NotImplementedException();
        }

        [ScoringFunction]
        public static float PutAwayLeftOversScoringFunction_EHSPatch(Sim actor, InteractionObjectPair iop)
        {
            return DebugUtils.TryDisplayScriptError(() =>
                {
                    var customService = actor.Service as CustomService;
                    if (actor.IsPerformingAService && (customService?.WaitsBeforePuttingAwayLeftovers ?? false))
                    {
                        return iop.Target is IPreparedFood preparedFood && SimClock.ElapsedTime(TimeUnit.Minutes) - preparedFood.TimeOfCreation <= customService.TimeWaitBeforePutawayLeftovers ? 0f : 1f;
                    }
                    return CleanableComponent.PutAwayLeftOversScoringFunction(actor, iop);
                }, out var retVal) ? 1f : retVal;
        }
    }
}
