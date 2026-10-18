using Sims3.Gameplay;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Interfaces.Destrospean.ExpandedHouseholdStaff;
using Sims3.Gameplay.ObjectComponents;
using Sims3.Gameplay.Objects.Electronics;
using Sims3.Gameplay.Objects.Gardening;
using Sims3.Gameplay.Objects.Vehicles;
using Sims3.Gameplay.Services;
using Sims3.Gameplay.Situations;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.Utilities;
using Sims3.Gameplay.Destrospean.ExpandedHouseholdStaff;
using Sims3.Gameplay.Destrospean.ExpandedHouseholdStaff.Interactions;
using Sims3.Gameplay.Destrospean.ExpandedHouseholdStaff.Services;
using Sims3.Gameplay.Destrospean.Utils;
using Sims3.SimIFace;
using System;
using System.Collections.Generic;
using Destrospean.ExpandedHouseholdStaff.Interactions;
using Destrospean.Utils;
using Destrospean.Utils.ExpandedHouseholdStaff;

// Temporary
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.ActiveCareer.ActiveCareers;
using Sims3.Gameplay.Careers;
using Sims3.Gameplay.TuningValues;
using Sims3.SimIFace.Enums;
using Sims3.Gameplay.Seasons;
using Sims3.Gameplay.Objects.Island;
using Sims3.Gameplay.Objects;
using Sims3.Gameplay.Pools;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Academics;
using Sims3.Gameplay.Objects.FoodObjects;
using Sims3.UI.Hud;
using Sims3.Gameplay.Controllers;
using Sims3.UI;

namespace Destrospean.ExpandedHouseholdStaff
{
    public class Main
    {
        [Tunable]
        protected static bool kInstantiator;

        static Main()
        {
            CommonUtils.ReplaceMethod<Main, Inventory>("AddInternal_Original", "AddInternal");
            CommonUtils.ReplaceMethod<Inventory, Main>("AddInternal");
            CommonUtils.ReplaceMethod<Main, Plant>("GardenInteractionLotValidityTest_Original", "GardenInteractionLotValidityTest");
            CommonUtils.ReplaceMethod<Plant, Main>("GardenInteractionLotValidityTest");
            CommonUtils.ReplaceMethod<Main, SocialComponent>("IsInServicePreventingSocialization_Original", "IsInServicePreventingSocialization");
            CommonUtils.ReplaceMethod<SocialComponent, Main>("IsInServicePreventingSocialization");
            CommonUtils.ReplaceMethod<Main, SimDescription>("IsPerformingAnyServiceOfType_Original", "IsPerformingAnyServiceOfType");
            CommonUtils.ReplaceMethod<SimDescription, Main>("IsPerformingAnyServiceOfType");
            CommonUtils.ReplaceMethod<Motive, Main>("UpdateMotiveWithDecay");
            InteractionObjectTypeUtils.InitTypes();
            if (Array.Exists(AppDomain.CurrentDomain.GetAssemblies(), x => x.GetName().Name == "NRaasMasterController"))
            {
                NRaasMasterControllerIntegration.Init();
            }
            List<EventListener> listeners = new List<EventListener>();
            LoadSaveManager.ObjectGroupsPreLoad += () => Phone.CallForServices.Singleton = CallForServices.Singleton;
            World.OnObjectPlacedInLotEventHandler += (sender, e) => DebugUtils.TryDisplayScriptError(() =>
                {
                    World.OnObjectPlacedInLotEventArgs onObjectPlacedInLotEventArgs = e as World.OnObjectPlacedInLotEventArgs;
                    if (onObjectPlacedInLotEventArgs != null)
                    {
                        GameObject gameObject = GameObject.GetObject(onObjectPlacedInLotEventArgs.ObjectId);
                        gameObject.AddInteraction(ListInteractions.Singleton, true);
                        gameObject.AddInteraction(AddInventoryObjectToService.Singleton, true);
                        (gameObject as Car)?.AddInteraction(AssignCarToService.Singleton, true);
                        (gameObject as Mailbox).AddServiceProfileInteractions();
                    }
                });
            World.sOnWorldLoadFinishedEventHandler += (sender, e) => DebugUtils.TryDisplayScriptError(() =>
                {
                    foreach (GameObject gameObject in Sims3.Gameplay.Queries.GetObjects<GameObject>())
                    {
                        gameObject.AddInteraction(ListInteractions.Singleton, true);
                        gameObject.AddInteraction(AddInventoryObjectToService.Singleton, true);
                        (gameObject as Car)?.AddInteraction(AssignCarToService.Singleton, true);
                        (gameObject as Mailbox).AddServiceProfileInteractions();
                    }
                    foreach (IServiceProfile profile in new List<IServiceProfile>(ServiceUtils.ServiceProfiles))
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
                    foreach (EventListener listener in listeners)
                    {
                        EventTracker.RemoveListener(listener);
                    }
                    listeners.Clear();
                    foreach (CustomService service in new List<CustomService>(ServiceUtils.CustomInstances.Values))
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

        public bool GardenInteractionLotValidityTest(Sim sim)
        {
            return (sim?.Service?.IsFromExpandedHouseholdStaff() ?? false) && ServiceSituation.FindServiceSituationInvolving(sim)?.Lot == ((Plant)(object)this).LotCurrent || GardenInteractionLotValidityTest_Original(sim);
        }

        public bool GardenInteractionLotValidityTest_Original(Sim sim)
        {
            throw new NotImplementedException();
        }

        public static bool IsInServicePreventingSocialization(Sim target)
        {
            return ((target.Service as CustomService)?.Profile.PreventSocialization ?? target.Service as IAmSociableService == null) && IsInServicePreventingSocialization_Original(target);
        }

        public static bool IsInServicePreventingSocialization_Original(Sim target)
        {
            throw new NotImplementedException();
        }

        public bool IsPerformingAnyServiceOfType(ServiceType serviceType)
        {
            CustomService customService = ((SimDescription)(object)this).Service as CustomService;
            return serviceType == ServiceType.Butler && (customService?.Profile.IsLiveInService ?? false) || serviceType == ServiceType.ChildCareServices && (customService?.Profile.IsBabysittingService ?? false) || IsPerformingAnyServiceOfType_Original(serviceType);
        }

        public bool IsPerformingAnyServiceOfType_Original(ServiceType serviceType)
        {
            throw new NotImplementedException();
        }

        [ScoringFunction]
        public static float PutAwayLeftOversScoringFunction_EHSPatch(Sim actor, InteractionObjectPair iop)
        {
            float retVal = 1f;
            DebugUtils.TryDisplayScriptError(() =>
                {
                    CustomService customService = actor.Service as CustomService;
                    if (actor.IsPerformingAService && (customService?.WaitsBeforePuttingAwayLeftovers ?? false))
                    {
                        IPreparedFood preparedFood = iop.Target as IPreparedFood;
                        return preparedFood != null && SimClock.ElapsedTime(TimeUnit.Minutes) - preparedFood.TimeOfCreation <= customService.TimeWaitBeforePutawayLeftovers ? 0f : 1f;
                    }
                    return CleanableComponent.PutAwayLeftOversScoringFunction(actor, iop);
                }, out retVal);
            return retVal;
        }

        public float UpdateMotiveWithDecay(float hoursPassed, float currentHours24, Sim sim)
        {
            int errorNumber = 0;
            try
            {
                Motive self = (Motive)(object)this;
                errorNumber = 1;
                float currentValue = self.Value;
                errorNumber = 2;
                bool flag = false;
                errorNumber = 3;
                if (self.Commodity == CommodityKind.Temperature && sim.BuffManager.HasElement(BuffNames.FrostyFace))
                {
                    errorNumber = 4;
                    flag = true;
                    errorNumber = 5;
                    currentValue += SimTemperature.kTemperatureDeltaFrostyFace;
                }
                errorNumber = 6;
                InteractionInstance currentInteraction = sim.CurrentInteraction;
                errorNumber = 7;
                float num = 0f;
                errorNumber = 8;
                if (sim.Posture != null)
                {
                    errorNumber = 9;
                    num += sim.Posture.GetPostureMotiveDecayUpdate(currentValue, self.Commodity, hoursPassed);
                }
                errorNumber = 10;
                DaycareSituation daycareSituationForChild = DaycareSituation.GetDaycareSituationForChild(sim);
                errorNumber = 11;
                if (daycareSituationForChild != null)
                {
                    errorNumber = 12;
                    Daycare daycareActiveCareer = Daycare.GetDaycareActiveCareer();
                    errorNumber = 13;
                    if (daycareActiveCareer != null)
                    {
                        errorNumber = 14;
                        num += daycareActiveCareer.GetMotiveDecayUpdate(currentValue, self.Commodity, hoursPassed, sim);
                    }
                }
                errorNumber = 15;
                BuffManager buffManager = sim.BuffManager;
                errorNumber = 16;
                if (self.Commodity == CommodityKind.Fun)
                {
                    errorNumber = 17;
                    if (sim.Occupation != null && buffManager.HasElement(BuffNames.MissingWork))
                    {
                        errorNumber = 18;
                        num += (float)Career.WorkaholicTraitMissingWorkStressPerHour * hoursPassed;
                    }
                    errorNumber = 19;
                    if (buffManager.HasElement(BuffNames.TooManyPeople))
                    {
                        errorNumber = 20;
                        num += BuffTooManyPeople.TooManyPeopleFunPenalty * hoursPassed;
                    }
                    errorNumber = 21;
                    if (buffManager.HasElement(BuffNames.GoingWild))
                    {
                        errorNumber = 22;
                        num += BuffGoingWild.kFunMultiplier * hoursPassed;
                    }
                    errorNumber = 23;
                    if (buffManager.HasElement(BuffNames.CryingBaby))
                    {
                        errorNumber = 24;
                        num += BuffCryingBaby.StressPerHour * hoursPassed;
                    }
                    errorNumber = 25;
                    if (sim.Posture != null && currentValue < 0f && (sim.Posture.Satisfies(CommodityKind.Relaxing, sim.Posture.Container) || sim.Posture.Satisfies(CommodityKind.Lounging, sim.Posture.Container)))
                    {
                        errorNumber = 26;
                        num += Motive.kRelaxingDestressAmount * hoursPassed;
                    }
                    errorNumber = 27;
                    if (sim.Posture != null && sim.Posture.Satisfies(CommodityKind.SwimmingInPool, sim.Posture.Container))
                    {
                        errorNumber = 28;
                        num = ((!sim.HasTrait(TraitNames.Hydrophobic)) ? (num + Motive.kSwimmingInPoolFunAmount * hoursPassed) : (num - Motive.kSwimmingInPoolFunAmount * hoursPassed));
                    }
                    errorNumber = 29;
                    if (sim.BuffManager.HasElement(BuffNames.SoothingScenery) && self.Value < Motive.kSoothingSceneryFunGainDisablePoint)
                    {
                        errorNumber = 30;
                        num += Motive.kSoothingSceneryFunAmount * hoursPassed;
                    }
                    errorNumber = 31;
                    if (sim.HasTrait(TraitNames.Carefree) && num > 0f)
                    {
                        errorNumber = 32;
                        num *= TraitTuning.CarefreeFunGainMultiplier;
                    }
                }
                errorNumber = 33;
                if (self.Commodity == CommodityKind.Hygiene && buffManager.HasElement(BuffNames.PestilencePlague))
                {
                    errorNumber = 34;
                    num += BuffPestilencePlague.ExtraHygieneMotiveDecayPerHour * hoursPassed;
                }
                errorNumber = 35;
                if (self.Commodity == CommodityKind.MermaidDermalHydration && sim.TraitManager.HasElement(TraitNames.MermaidHiddenTrait))
                {
                    errorNumber = 36;
                    PrecipitationIntensity intensity;
                    errorNumber = 37;
                    if (sim.Posture != null && (sim.Posture.Satisfies(CommodityKind.SwimmingInPool, sim.Posture.Container) || sim.Posture.Satisfies(CommodityKind.ScubaDiving, sim.Posture.Container)))
                    {
                        errorNumber = 38;
                        num += OccultMermaid.kSwimmingInPoolHydrationAmount * hoursPassed;
                    }
                    else if (SeasonsManager.Enabled && sim.IsOutside && SeasonsManager.IsRaining(out intensity))
                    {
                        errorNumber = 39;
                        BeachTowel beachTowel = sim.Posture.Container as BeachTowel;
                        errorNumber = 40;
                        if ((beachTowel != null && !beachTowel.UmbrellaIsUp) || Umbrella.SearchForHoldingUmbrellaPosture(sim) == null)
                        {
                            errorNumber = 41;
                            switch (intensity)
                            {
                                case PrecipitationIntensity.Light:
                                    errorNumber = 42;
                                    num += OccultMermaid.kRainHydrationAmount[0] * hoursPassed;
                                    break;
                                case PrecipitationIntensity.Moderate:
                                    errorNumber = 43;
                                    num += OccultMermaid.kRainHydrationAmount[1] * hoursPassed;
                                    break;
                                case PrecipitationIntensity.Heavy:
                                    errorNumber = 44;
                                    num += OccultMermaid.kRainHydrationAmount[2] * hoursPassed;
                                    break;
                            }
                        }
                    }
                    errorNumber = 45;
                    if (buffManager.HasElement(BuffNames.DryingUp))
                    {
                        errorNumber = 46;
                        num += OccultMermaid.kDryingUpDehydrationAmount * hoursPassed;
                    }
                }
                errorNumber = 47;
                if (self.Commodity == CommodityKind.Energy && buffManager.HasElement(BuffNames.PestilencePlague))
                {
                    errorNumber = 48;
                    num += BuffPestilencePlague.ExtraEnergyMotiveDecayPerHour * hoursPassed;
                }
                errorNumber = 49;
                if (self.Commodity == CommodityKind.VampireThirst && buffManager.HasElement(BuffNames.TooMuchSun))
                {
                    errorNumber = 50;
                    num += BuffTooMuchSun.TooMuchSunHungerPerHour * hoursPassed;
                }
                errorNumber = 51;
                if (self.Commodity == CommodityKind.Fatigue && !sim.SimDescription.IsMermaid && sim.Posture != null && sim.Posture.Satisfies(CommodityKind.SwimmingInPool, sim.Posture.Container) && sim.Posture.Container is ISwimLocation)
                {
                    errorNumber = 52;
                    float num2 = Motive.kSwimmingInPoolFatigueAmount * hoursPassed;
                    errorNumber = 53;
                    SwimmingInPool swimmingInPool = sim.Posture as SwimmingInPool;
                    errorNumber = 54;
                    if (swimmingInPool != null && swimmingInPool.ContainerIsOcean)
                    {
                        errorNumber = 55;
                        num2 = Motive.kSwimmingInOceanFatigueAmount * hoursPassed;
                    }
                    errorNumber = 56;
                    if (sim.TraitManager.HasElement(TraitNames.LovesToSwim))
                    {
                        errorNumber = 57;
                        num2 *= TraitTuning.kLovesToSwimFatigueMultiplier;
                    }
                    errorNumber = 58;
                    num += num2;
                }
                errorNumber = 59;
                if (self.Commodity == CommodityKind.Hygiene && sim.Posture != null && sim.Posture.Satisfies(CommodityKind.SwimmingInPool, sim.Posture.Container))
                {
                    errorNumber = 60;
                    Pool pool = sim.Posture.Container as Pool;
                    errorNumber = 61;
                    if (pool != null && pool.LotCurrent.CommercialLotSubType == CommercialLotSubType.kEP10_Resort && !pool.IsChlorinated)
                    {
                        errorNumber = 62;
                        num += Motive.kSwimmingInUnchlorinatedResortPoolHygieneAmount * hoursPassed;
                    }
                }
                errorNumber = 63;
                if (self.Commodity == CommodityKind.Social && buffManager.HasAnyElement(BuffNames.FoalAwayFromMother, BuffNames.FoalMissingMother, BuffNames.MotherAwayFromFoal))
                {
                    errorNumber = 64;
                    num += HorseManager.kFoalAwayFromMotherSocialDecayPerHour * hoursPassed;
                }
                errorNumber = 65;
                if (self.Commodity == CommodityKind.Social && sim.TraitManager.HasElement(TraitNames.SocialButterfly))
                {
                    errorNumber = 66;
                    num -= TraitTuning.kSocialButterflySocialDecayIncrease * hoursPassed;
                }
                errorNumber = 67;
                if (self.Commodity == CommodityKind.AuraPower)
                {
                    errorNumber = 68;
                    float num3 = 1f;
                    errorNumber = 69;
                    int skillLevel = sim.SkillManager.GetSkillLevel(SkillNames.FairyMagic);
                    errorNumber = 70;
                    int num4 = BuffFairyAuraProjecting.kFairySkillAuraPowerCostMultiplier.Length - 1;
                    errorNumber = 71;
                    if (skillLevel >= 0 && num4 >= 0)
                    {
                        errorNumber = 72;
                        num3 = ((skillLevel > num4) ? BuffFairyAuraProjecting.kFairySkillAuraPowerCostMultiplier[num4] : BuffFairyAuraProjecting.kFairySkillAuraPowerCostMultiplier[skillLevel]);
                    }
                    errorNumber = 73;
                    if (sim.TraitManager.HasElement(TraitNames.FairyQueen))
                    {
                        errorNumber = 74;
                        num3 *= OccultFairy.kFairyPowerDrainScalerForQueen;
                    }
                    errorNumber = 75;
                    if (buffManager.HasElement(BuffNames.FairyAuraProjectingSoothe))
                    {
                        errorNumber = 76;
                        num -= BuffFairyAuraProjecting.BuffInstanceSootheAura.kAuraPowerCostPerHour * num3 * hoursPassed;
                    }
                    errorNumber = 77;
                    if (buffManager.HasElement(BuffNames.FairyAuraProjectingCreative))
                    {
                        errorNumber = 78;
                        num -= BuffFairyAuraProjecting.BuffInstanceCreativeAura.kAuraPowerCostPerHour * num3 * hoursPassed;
                    }
                    errorNumber = 79;
                    if (buffManager.HasElement(BuffNames.FairyAuraProjectingMind))
                    {
                        errorNumber = 80;
                        num -= BuffFairyAuraProjecting.BuffInstanceMindAura.kAuraPowerCostPerHour * num3 * hoursPassed;
                    }
                }
                errorNumber = 81;
                if (self.Commodity == CommodityKind.Temperature && GameUtils.IsInstalled(ProductVersion.EP8))
                {
                    errorNumber = 82;
                    float num5 = SimTemperature.CalculateSimTemperatureChangeAndBuffs(sim, hoursPassed, currentValue);
                    errorNumber = 83;
                    num += num5 * hoursPassed;
                }
                errorNumber = 84;
                if (self.Commodity == CommodityKind.AcademicPerformance && sim.Autonomy.SituationComponent.InSituationOfType(typeof(AcademicSituationBase)))
                {
                    errorNumber = 85;
                    num += AcademicSituationBase.kAcademicPerformancePerHour * hoursPassed * AcademicCourse.kCourseLoadMultipliersToAcademicPerformance[sim.OccupationAsAcademicCareer.NumberOfCoursesPerDay - 1] * AcademicCourse.kTermLengthMultipliersToAcademicPerformance[(uint)(AcademicCareer.sTermLengthEx - 1)];
                }
                if (self.Commodity == CommodityKind.BatteryPower && sim.BuffManager.HasElement(BuffNames.AbsorbingSunlight))
                {
                    errorNumber = 86;
                    num += TraitChip.kAbsorbingSunlightBatteryPowerMotivePerHour * hoursPassed;
                }
                errorNumber = 87;
                bool flag2 = false;
                errorNumber = 88;
                if (currentInteraction != null)
                {
                    errorNumber = 89;
                    foreach (MotiveDelta motiveDelta in currentInteraction.MotiveDeltas)
                    {
                        errorNumber = 90;
                        if (motiveDelta.Motive != self.Commodity)
                        {
                            errorNumber = 91;
                            continue;
                        }
                        errorNumber = 92;
                        flag2 = true;
                        errorNumber = 93;
                        if (motiveDelta.Enabled)
                        {
                            errorNumber = 94;
                            if (motiveDelta.EnabledAboveZero && currentValue > 0f)
                            {
                                errorNumber = 95;
                                num += motiveDelta.ChangePerHour * motiveDelta.Multiplier * hoursPassed;
                                errorNumber = 96;
                                if (self.Tuning.DecayToZero && !motiveDelta.EnabledBelowZero)
                                {
                                    errorNumber = 97;
                                    num = Math.Max(num, 0f - currentValue);
                                }
                            }
                            else if (motiveDelta.EnabledBelowZero && currentValue <= 0f)
                            {
                                errorNumber = 98;
                                num += motiveDelta.ChangePerHour * motiveDelta.Multiplier * hoursPassed;
                                errorNumber = 99;
                                if (self.Tuning.DecayToZero && !motiveDelta.EnabledAboveZero)
                                {
                                    errorNumber = 100;
                                    num = Math.Min(num, 0f - currentValue);
                                }
                            }
                        }
                        errorNumber = 101;
                        if (self.Commodity == CommodityKind.Fun)
                        {
                            errorNumber = 102;
                            if (num > 0f && sim.HasTrait(TraitNames.Carefree))
                            {
                                errorNumber = 103;
                                num *= TraitTuning.CarefreeFunGainMultiplier;
                            }
                            else if (num < 0f && buffManager.HasElement(BuffNames.LaidbackLemon))
                            {
                                errorNumber = 104;
                                num *= BuffLaidbackLemon.LaidbackLemonFunLossMultiplier;
                            }
                        }
                        errorNumber = 105;
                        if (self.Commodity == CommodityKind.BatteryPower && num > 0f && sim.HasTrait(TraitNames.EfficientChip))
                        {
                            errorNumber = 106;
                            num *= TraitTuning.EfficientChipBatteryPowerGainMultiplier;
                        }
                    }
                }
                errorNumber = 107;
                if (sim.IsHorse)
                {
                    errorNumber = 108;
                    if (self.Commodity == CommodityKind.HorseExercise && sim.IsRouting)
                    {
                        errorNumber = 109;
                        switch (sim.SimRoutingComponent.CurrentWalkStyle)
                        {
                            case Sim.WalkStyle.GhostWalk:
                            case Sim.WalkStyle.PetWalk:
                                errorNumber = 110;
                                num += Motive.kHorseExerciseIncreaseWalking;
                                break;
                            case Sim.WalkStyle.PetTrot:
                                errorNumber = 111;
                                num += Motive.kHorseExerciseIncreaseTrotting;
                                break;
                            default:
                                errorNumber = 112;
                                num += Motive.kHorseExerciseIncreaseGalloping;
                                break;
                            case Sim.WalkStyle.Speedboat:
                            case Sim.WalkStyle.SpeedboatFast:
                                errorNumber = 113;
                                break;
                        }
                        errorNumber = 114;
                        if (sim.TraitManager.HasElement(TraitNames.EquineZenPet))
                        {
                            errorNumber = 115;
                            num *= TraitTuning.EquineZenMultiplier;
                        }
                    }
                    errorNumber = 116;
                    if (sim.BeingRiddenPosture != null)
                    {
                        errorNumber = 117;
                        for (int i = 0; i < BeingRiddenPosture.kBenefitHorseCommoditiesToUpdate.Length; i++)
                        {
                            errorNumber = 118;
                            if (BeingRiddenPosture.kBenefitHorseCommoditiesToUpdate[i] == self.Commodity)
                            {
                                errorNumber = 119;
                                num += BeingRiddenPosture.kBenefitHorseCommoditiesToUpdateDeltas[i];
                                break;
                            }
                        }
                    }
                }
                errorNumber = 120;
                if (sim.RidingPosture != null)
                {
                    errorNumber = 121;
                    for (int j = 0; j < BeingRiddenPosture.kBenefitSimCommoditiesToUpdate.Length; j++)
                    {
                        errorNumber = 122;
                        if (BeingRiddenPosture.kBenefitSimCommoditiesToUpdate[j] == self.Commodity)
                        {
                            errorNumber = 123;
                            num += BeingRiddenPosture.kBenefitSimCommoditiesToUpdateDeltas[j];
                            break;
                        }
                    }
                }
                errorNumber = 124;
                if (SeasonsManager.Enabled && self.Commodity == CommodityKind.Temperature)
                {
                    errorNumber = 125;
                    TraitManager traitManager = sim.TraitManager;
                    errorNumber = 126;
                    if (currentValue <= 0f && num < 0f && traitManager.HasElement(TraitNames.LovesTheCold))
                    {
                        errorNumber = 127;
                        num *= SimTemperature.kTempMultiplierForLovesTheColdTrait;
                    }
                    errorNumber = 128;
                    if (currentValue >= 0f && num > 0f && traitManager.HasElement(TraitNames.LovesTheHeat))
                    {
                        errorNumber = 129;
                        num *= SimTemperature.kTempMultiplierForLovesTheHeatTrait;
                    }
                }
                errorNumber = 130;
                if (num != 0f || flag2)
                {
                    errorNumber = 131;
                    currentValue += num;
                }
                else if ((self.Commodity != CommodityKind.Bladder || (!sim.LotCurrent.IsWorldLot && (!sim.LotCurrent.IsCommunityLot || sim.LotCurrent.SatisfiesCommodity(CommodityKind.Bladder)) && !TombRoomManager.IsObjectInATombRoom(sim))) && (self.Commodity != CommodityKind.Social || sim.Conversation == null))
                {
                    errorNumber = 132;
                    float num6 = 1f;
                    errorNumber = 133;
                    float num7 = 1f;
                    errorNumber = 134;
                    float num8 = 1f;
                    errorNumber = 135;
                    float num9 = 1f;
                    errorNumber = 136;
                    switch (self.Commodity)
                    {
                        case CommodityKind.Bladder:
                            errorNumber = 137;
                            if (sim.IsSleeping)
                            {
                                errorNumber = 138;
                                num6 *= Motive.kBladderMultiplierWhileSleeping;
                            }
                            errorNumber = 139;
                            if (sim.Motives.SimIsAWereWolf())
                            {
                                errorNumber = 140;
                                num8 = TraitTuning.LycanthropyWerewolfSlowingBladderMotives;
                            }
                            break;
                        case CommodityKind.Hunger:
                            errorNumber = 141;
                            if (sim.IsSleeping)
                            {
                                errorNumber = 142;
                                num6 *= Motive.kHungerMultiplierWhileSleeping;
                            }
                            errorNumber = 143;
                            if (sim.HasTrait(TraitNames.HardlyHungry))
                            {
                                errorNumber = 144;
                                num7 = TraitTuning.HardlyHungryHungerDecayMultiplier;
                            }
                            errorNumber = 145;
                            if (sim.HasTrait(TraitNames.PiggyPet))
                            {
                                errorNumber = 146;
                                num7 *= TraitTuning.PiggyPetHungerDecayMultiplier;
                            }
                            errorNumber = 147;
                            if (sim.BuffManager.HasElement(BuffNames.HerbLicorice))
                            {
                                errorNumber = 148;
                                num7 *= Licorice.HungerDecayMultiplier;
                            }
                            errorNumber = 149;
                            if (sim.BuffManager.HasElement(BuffNames.HungerResolved))
                            {
                                errorNumber = 150;
                                num7 *= BuffHungerResolved.kHungerMultiplier;
                            }
                            errorNumber = 151;
                            break;
                        case CommodityKind.Fun:
                            errorNumber = 152;
                            if (sim.IsSleeping)
                            {
                                errorNumber = 153;
                                num6 *= Motive.kFunMultiplierWhileSleeping;
                            }
                            errorNumber = 154;
                            if (sim.HasTrait(TraitNames.PlayfulPet))
                            {
                                errorNumber = 155;
                                num7 *= TraitTuning.PlayfulPetFunDecayMultiplier;
                            }
                            errorNumber = 156;
                            if (sim.Motives.SimIsAWereWolf())
                            {
                                errorNumber = 157;
                                num8 = TraitTuning.LycanthropyWerewolfSlowingFunMotives;
                            }
                            errorNumber = 158;
                            break;
                        case CommodityKind.VampireThirst:
                            errorNumber = 159;
                            if (sim.HasTrait(TraitNames.HardlyHungry))
                            {
                                errorNumber = 160;
                                num7 = TraitTuning.HardlyHungryVampireThirstDecayMultiplier;
                            }
                            errorNumber = 161;
                            if (sim.HasTrait(TraitNames.SuperVampire))
                            {
                                errorNumber = 162;
                                num7 = OccultVampire.kSuperVampireThirstDecayMultiplier;
                            }
                            errorNumber = 163;
                            break;
                        case CommodityKind.Hygiene:
                            errorNumber = 164;
                            if (sim.HasTrait(TraitNames.PermaClean))
                            {
                                errorNumber = 165;
                                num7 = TraitTuning.PermaCleanHygieneDecayMultiplier;
                            }
                            errorNumber = 166;
                            break;
                        case CommodityKind.PetSkillFatigue:
                            {
                                errorNumber = 167;
                                bool flag3 = sim.TraitManager.HasAnyElement(TraitTuning.TraitsThatAffectPetSkillFatigueNegatively);
                                errorNumber = 168;
                                if (flag3)
                                {
                                    errorNumber = 169;
                                    num7 = TraitTuning.PetSkillFatigueNegativeEffectMultiplier;
                                }
                                errorNumber = 170;
                                bool flag4 = sim.TraitManager.HasAnyElement(TraitTuning.TraitsThatAffectPetSkillFatiguePositively);
                                errorNumber = 171;
                                if (flag4)
                                {
                                    errorNumber = 172;
                                    num7 = TraitTuning.PetSkillFatiguePositiveEffectMultiplier;
                                }
                                errorNumber = 173;
                                if (flag3 && flag4)
                                {
                                    errorNumber = 174;
                                    num7 = 1f;
                                }
                                errorNumber = 175;
                                break;
                            }
                        case CommodityKind.HorseExercise:
                            errorNumber = 176;
                            if (sim.HasTrait(TraitNames.AgilePet))
                            {
                                errorNumber = 177;
                                num7 *= TraitTuning.AgilePetExerciseDecayMultiplier;
                            }
                            errorNumber = 178;
                            if (sim.HasTrait(TraitNames.HatesJumpingPet))
                            {
                                errorNumber = 179;
                                num7 *= TraitTuning.HatesJumpingExerciseDecayMultiplier;
                            }
                            errorNumber = 180;
                            if (sim.HasTrait(TraitNames.FastPet))
                            {
                                errorNumber = 181;
                                num7 *= TraitTuning.FastPetRacingSkillMultiplier;
                            }
                            errorNumber = 182;
                            if (sim.HasTrait(TraitNames.PlayfulPet))
                            {
                                errorNumber = 183;
                                num7 *= TraitTuning.PlayfulHorseExerciseDecayMultiplier;
                            }
                            errorNumber = 184;
                            break;
                        case CommodityKind.Social:
                            errorNumber = 185;
                            if (sim.IsHorse && sim.HasTrait(TraitNames.PlayfulPet))
                            {
                                errorNumber = 186;
                                num7 *= TraitTuning.PlayfulHorseSocialDecayMultiplier;
                            }
                            errorNumber = 187;
                            if (sim.HasTrait(TraitNames.IndependentPet))
                            {
                                errorNumber = 188;
                                num7 *= TraitTuning.IndependentPetSocialDecayMultiplier;
                            }
                            errorNumber = 189;
                            if (buffManager.HasElement(BuffNames.TotalBeliever))
                            {
                                errorNumber = 190;
                                num7 *= BuffTotalBeliever.SocialLossMultiplier;
                            }
                            errorNumber = 191;
                            break;
                        case CommodityKind.HorseThirst:
                            errorNumber = 192;
                            if (sim.TraitManager.HasElement(TraitNames.DesertPonyPet))
                            {
                                errorNumber = 193;
                                num7 *= TraitTuning.kDesertPonyHorseThirstMultiplier;
                            }
                            errorNumber = 194;
                            break;
                        case CommodityKind.SkillInfluenceNerd:
                            errorNumber = 195;
                            if (sim.TraitManager.HasElement(TraitNames.InfluenceNerd))
                            {
                                errorNumber = 196;
                                Skill element = sim.SkillManager.GetElement(SkillNames.InfluenceNerd);
                                errorNumber = 197;
                                if (element != null)
                                {
                                    errorNumber = 198;
                                    num7 *= element.GetCommodityMod();
                                }
                            }
                            errorNumber = 199;
                            break;
                        case CommodityKind.SkillInfluenceRebel:
                            errorNumber = 200;
                            if (sim.TraitManager.HasElement(TraitNames.InfluenceRebel))
                            {
                                errorNumber = 201;
                                Skill element3 = sim.SkillManager.GetElement(SkillNames.InfluenceRebel);
                                errorNumber = 202;
                                if (element3 != null)
                                {
                                    errorNumber = 203;
                                    num7 *= element3.GetCommodityMod();
                                }
                            }
                            errorNumber = 204;
                            break;
                        case CommodityKind.SkillInfluenceSocialite:
                            errorNumber = 205;
                            if (sim.TraitManager.HasElement(TraitNames.InfluenceSocialite))
                            {
                                errorNumber = 206;
                                Skill element2 = sim.SkillManager.GetElement(SkillNames.InfluenceSocialite);
                                errorNumber = 207;
                                if (element2 != null)
                                {
                                    errorNumber = 208;
                                    num7 *= element2.GetCommodityMod();
                                }
                            }
                            errorNumber = 209;
                            break;
                        case CommodityKind.BatteryPower:
                            {
                                errorNumber = 210;
                                CASRobotData cASRobotData2 = sim.SimDescription.SupernaturalData as CASRobotData;
                                errorNumber = 211;
                                if (cASRobotData2 != null && cASRobotData2.BotQualityLevel >= 0 && cASRobotData2.BotQualityLevel <= 10)
                                {
                                    errorNumber = 212;
                                    num7 *= OccultRobot.kQualityModifiersBatteryPower[cASRobotData2.BotQualityLevel];
                                }
                                errorNumber = 213;
                                break;
                            }
                        case CommodityKind.Maintenence:
                            {
                                errorNumber = 214;
                                CASRobotData cASRobotData = sim.SimDescription.SupernaturalData as CASRobotData;
                                errorNumber = 215;
                                if (cASRobotData != null && cASRobotData.BotQualityLevel >= 0 && cASRobotData.BotQualityLevel <= 10)
                                {
                                    errorNumber = 216;
                                    num7 *= OccultRobot.kQualityModifiersMaintenance[cASRobotData.BotQualityLevel];
                                }
                                errorNumber = 217;
                                if (sim.TraitManager.HasElement(TraitNames.EfficientChip))
                                {
                                    errorNumber = 218;
                                    num7 *= TraitTuning.EfficientChipMaintenenceDecayMultiplier;
                                }
                                errorNumber = 219;
                                break;
                            }
                    }
                    errorNumber = 220;
                    if (self.Commodity == CommodityKind.Energy && !sim.Motives.IsSleepy())
                    {
                        errorNumber = 221;
                        if (sim.Motives.EnergyGainRate == float.MinValue)
                        {
                            errorNumber = 222;
                            InteractionTuning value;
                            errorNumber = 223;
                            InteractionTuning.sAllTunings.TryGetValue(Motives.kBedSleepTuningSignature, out value);
                            errorNumber = 224;
                            if (value != null)
                            {
                                errorNumber = 225;
                                foreach (CommodityChange mOutput in value.mTradeoff.mOutputs)
                                {
                                    errorNumber = 226;
                                    if (mOutput.Commodity == CommodityKind.Energy)
                                    {
                                        errorNumber = 227;
                                        sim.Motives.EnergyGainRate = mOutput.mActualValue * mOutput.mMultiplier;
                                    }
                                }
                            }
                        }
                        errorNumber = 228;
                        if (sim.Motives.SimIsAWereWolf())
                        {
                            errorNumber = 229;
                            num8 = TraitTuning.LycanthropyWerewolfSlowingEnergyMotives;
                        }
                        errorNumber = 230;
                        if (SimClock.IsFullMoon() && (sim.SimDescription.IsFairy || sim.SimDescription.IsWitch || sim.SimDescription.IsVampire || sim.SimDescription.IsWerewolf || sim.SimDescription.IsZombie))
                        {
                            errorNumber = 231;
                            num9 = TraitTuning.kOccultSlowingEnergyMotives;
                        }
                        errorNumber = 232;
                        if (sim.HasTrait(TraitNames.LazyPet) || sim.HasTrait(TraitNames.LazyHorse))
                        {
                            errorNumber = 233;
                            num7 = TraitTuning.LazyPetEnergyDecayMultiplier;
                        }
                        errorNumber = 234;
                        if (sim.HasTrait(TraitNames.HyperPet))
                        {
                            errorNumber = 235;
                            num7 = TraitTuning.HyperPetEnergyDecayMultiplier;
                        }
                        errorNumber = 236;
                        num7 *= self.GetCommodityDecayMultiplier(sim);
                        errorNumber = 237;
                        float energyGainRate = sim.Motives.EnergyGainRate;
                        errorNumber = 238;
                        float num10 = sim.Motives.EnergyFromFullToSleepy();
                        errorNumber = 239;
                        float num11 = num10 / (Motives.kEnergyHoursInDay - num10 / energyGainRate);
                        errorNumber = 240;
                        self.Decay = 0f - num11;
                        errorNumber = 241;
                        currentValue -= num11 * hoursPassed * num7 * num8 * num9;
                        errorNumber = 242;
                    }
                    else
                    {
                        errorNumber = 243;
                        num7 *= self.GetCommodityDecayMultiplier(sim);
                        errorNumber = 244;
                        switch (self.Tuning.DecayType)
                        {
                            case DecayType.DecayToZero:
                                errorNumber = 245;
                                self.UpdateDecayToZero(self.Commodity, sim, ref currentValue, currentHours24, hoursPassed, num6, num7);
                                errorNumber = 246;
                                break;
                            case DecayType.NoDecay:
                                errorNumber = 247;
                                self.Decay = 0f;
                                errorNumber = 248;
                                break;
                            case DecayType.FixedDecay:
                                {
                                    errorNumber = 249;
                                    if (self.Tuning.DecayValue == 0f)
                                    {
                                        errorNumber = 250;
                                        break;
                                    }
                                    errorNumber = 251;
                                    float num14 = self.Tuning.DecayValue;
                                    errorNumber = 252;
                                    if (self.Commodity == CommodityKind.PetSkillFatigue && sim.SkillManager.IsGainingAnyVisibleNonMaxedSkill())
                                    {
                                        errorNumber = 253;
                                        float petSkillFatigueRate = sim.SkillManager.GetPetSkillFatigueRate();
                                        errorNumber = 254;
                                        if (petSkillFatigueRate != 0f)
                                        {
                                            errorNumber = 255;
                                            num14 = petSkillFatigueRate;
                                        }
                                    }
                                    errorNumber = 256;
                                    if (self.Commodity == CommodityKind.Hygiene && buffManager.HasElement(BuffNames.UnnaturallyClean))
                                    {
                                        errorNumber = 257;
                                        num14 *= Motive.kUnaturallyCleanDecayMultiplier;
                                    }
                                    errorNumber = 258;
                                    float num15 = hoursPassed / num14;
                                    errorNumber = 259;
                                    if (num15 > 1f)
                                    {
                                        errorNumber = 260;
                                        num15 = 1f;
                                    }
                                    errorNumber = 261;
                                    if (num15 < 0f && num14 > 0f)
                                    {
                                        errorNumber = 262;
                                        num15 = 0f;
                                    }
                                    errorNumber = 263;
                                    self.Decay = (0f - (self.Tuning.Max - self.Tuning.Min)) / num14;
                                    errorNumber = 264;
                                    float num16 = (self.Tuning.Max - self.Tuning.Min) * num15;
                                    errorNumber = 265;
                                    self.Decay *= num6 * num7;
                                    errorNumber = 266;
                                    currentValue -= num16 * num6 * num7;
                                    errorNumber = 267;
                                    break;
                                }
                            case DecayType.DecayFromAutoSatisfy:
                                {
                                    errorNumber = 268;
                                    self.Decay = self.Tuning.GetDecay(currentHours24);
                                    errorNumber = 269;
                                    float num13 = self.Decay * hoursPassed;
                                    errorNumber = 270;
                                    currentValue += num13 * num6 * num7;
                                    errorNumber = 271;
                                    break;
                                }
                            case DecayType.VariableDecayByMoodlet:
                                {
                                    errorNumber = 272;
                                    self.Decay = self.CalculateDecayByMoodlet(currentValue);
                                    errorNumber = 273;
                                    float num12 = self.Decay * hoursPassed;
                                    errorNumber = 274;
                                    if (self.PotionBladderDecayOverride && self.Commodity == CommodityKind.Bladder)
                                    {
                                        errorNumber = 275;
                                        num12 *= Motive.kPotionBladderDecayMultiplier;
                                    }
                                    errorNumber = 276;
                                    if (sim.BuffManager.HasElement(BuffNames.OrganicOldJoeBuzz))
                                    {
                                        errorNumber = 277;
                                        BuffOrganicOldJoeBuzz.BuffInstanceOrganicOldJoeBuzz buffInstanceOrganicOldJoeBuzz = sim.BuffManager.GetElement(BuffNames.OrganicOldJoeBuzz) as BuffOrganicOldJoeBuzz.BuffInstanceOrganicOldJoeBuzz;
                                        errorNumber = 278;
                                        if (buffInstanceOrganicOldJoeBuzz != null)
                                        {
                                            errorNumber = 279;
                                            num12 *= buffInstanceOrganicOldJoeBuzz.GetBladderMultiplier();
                                        }
                                    }
                                    errorNumber = 280;
                                    if (self.PregnantMotiveDecay && sim.IsPet)
                                    {
                                        errorNumber = 281;
                                        if (self.Commodity == CommodityKind.Bladder)
                                        {
                                            errorNumber = 282;
                                            num12 *= Motive.kPetPregnancyBladderDecayMultiplier;
                                        }
                                        else if (self.Commodity == CommodityKind.Hunger)
                                        {
                                            errorNumber = 283;
                                            num12 *= Motive.kPetPregnancyHungerDecayMultiplier;
                                        }
                                    }
                                    errorNumber = 284;
                                    currentValue += num12 * num6 * num7;
                                    errorNumber = 285;
                                    break;
                                }
                        }
                    }
                }
                errorNumber = 286;
                if (flag)
                {
                    errorNumber = 287;
                    currentValue -= SimTemperature.kTemperatureDeltaFrostyFace;
                }
                errorNumber = 288;
                float minValue = self.Tuning.Min;
                errorNumber = 289;
                float maxValue = self.Tuning.Max;
                errorNumber = 290;
                if (self.Commodity == CommodityKind.Temperature && SeasonsManager.Enabled)
                {
                    errorNumber = 291;
                    if (sim.OccultManager != null)
                    {
                        errorNumber = 292;
                        OccultTypes currentOccultTypes = sim.OccultManager.CurrentOccultTypes;
                        errorNumber = 293;
                        bool flag5 = (currentOccultTypes | SimTemperature.kOccultsThatCanHaveSimTemperature) != SimTemperature.kOccultsThatCanHaveSimTemperature;
                        errorNumber = 294;
                        OccultImaginaryFriend occult;
                        errorNumber = 295;
                        if (flag5 && OccultImaginaryFriend.TryGetOccultFromSim(sim, out occult) && !occult.IsWearingSpecialOutfit)
                        {
                            errorNumber = 296;
                            flag5 = false;
                        }
                        errorNumber = 297;
                        if (flag5)
                        {
                            errorNumber = 298;
                            minValue = SimTemperature.kOccultTemperatureRangeCaps[0];
                            errorNumber = 299;
                            maxValue = SimTemperature.kOccultTemperatureRangeCaps[1];
                        }
                        else if (currentOccultTypes == OccultTypes.Vampire)
                        {
                            errorNumber = 300;
                            minValue = SimTemperature.kOccultTemperatureRangeCaps[0];
                        }
                    }
                    errorNumber = 301;
                    if (sim.TraitManager.HasElement(TraitNames.ImmuneToCold))
                    {
                        errorNumber = 302;
                        minValue = SimTemperature.kTempCapImmuneToCold;
                    }
                    errorNumber = 303;
                    if (sim.TraitManager.HasElement(TraitNames.ImmuneToHeat))
                    {
                        errorNumber = 304;
                        maxValue = SimTemperature.kTempCapImmuneToHeat;
                    }
                    errorNumber = 305;
                    if (buffManager.HasElement(BuffNames.PolarBearClub))
                    {
                        errorNumber = 306;
                        minValue = 0f;
                    }
                }
                errorNumber = 307;
                return MathUtils.Clamp(currentValue, minValue, maxValue);
            }
            catch (Exception ex)
            {
                SimpleMessageDialog.Show("ERROR " + errorNumber, "Say it's \"ERROR " + errorNumber + "\" to Destrospean on MTS");
                throw ex;
            }
        }
    }
}
