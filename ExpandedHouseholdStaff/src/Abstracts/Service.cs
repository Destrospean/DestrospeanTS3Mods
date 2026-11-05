using System;
using System.Collections.Generic;
using System.Xml;
using Destrospean.Enums;
using Destrospean.Utils;
using Destrospean.Utils.ExpandedHouseholdStaff;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Destrospean.ExpandedHouseholdStaff.Services;
using Sims3.Gameplay.Destrospean.Utils;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces.Destrospean.ExpandedHouseholdStaff;
using Sims3.Gameplay.Objects.Beds;
using Sims3.Gameplay.Services;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI.Controller;

namespace Sims3.Gameplay.Abstracts.Destrospean.ExpandedHouseholdStaff
{
    /// <summary>
    /// Service base class from which to derive all services for the Expanded Household Staff mod.
    /// </summary>
    [Persistable]
    public abstract class Service<T> : Service, IService where T : Service<T>
    {
        public class SetUnsetServiceBed : ImmediateInteraction<Sim, Bed>
        {
            [DoesntRequireTuning]
            public class Definition : InteractionDefinition<Sim, Bed, SetUnsetServiceBed>
            {
                public override string GetInteractionName(Sim actor, Bed target, InteractionObjectPair iop)
                {
                    if (Instance == null)
                    {
                        return StringTable.GetLocalizedString(DerivedType.GetLocalizationKey() + ":Title");
                    }
                    var simsAssignedToLot = Instance.GetSimsAssignedToLot(actor.LotHome);
                    return Localization.LocalizeString(actor.IsFemale, DerivedType.GetLocalizationKey() + "/" + nameof(SetUnsetServiceBed) + (target.FindOwnedBed(simsAssignedToLot[0]) == target ? ":Unset" : ":Set") + "InteractionName", simsAssignedToLot[0].SimDescription);
                }

                public override bool Test(Sim actor, Bed target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    if (actor.LotHome != null)
                    {
                        if (target.LotCurrent != actor.LotHome)
                        {
                            return false;
                        }
                        if (Instance != null)
                        {
                            var simsAssignedToLot = Instance.GetSimsAssignedToLot(actor.LotHome);
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
                    }
                    return false;
                }
            }

            public static InteractionDefinition Singleton = new Definition();

            public override bool Run()
            {
                if (Instance != null)
                {
                    var simActiveOnLot = Instance.GetSimActiveOnLot(Actor.LotHome);
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
                                    Instance.mServiceType = ServiceType.Butler;
                                    Target.ClaimOwnership(simActiveOnLot, value);
                                    Instance.mServiceType = default(ServiceType);
                                    break;
                                }
                            }
                        }
                    }
                }
                return true;
            }
        }

        private readonly List<ServiceUtils.CommodityChange> mOutputs = new List<ServiceUtils.CommodityChange>();

        private ServiceType mServiceType;

        public static Type DerivedType => typeof(T);

        public static Service<T> Instance
        {
            get => ServiceUtils.PredefinedInstances.TryGetValue(DerivedType.Name, out var service) ? (Service<T>)service : null;
            set => ServiceUtils.PredefinedInstances[DerivedType.Name] = value;
        }

        public virtual bool IsHomelessService => true;

        public override bool IsPaidWeekly => this is IAmLiveInService;

        public virtual bool IsQuietAroundSleepingSims => false;

        /// <summary>
        /// Outputs for interactions and their target types that the service motive of the service is inserted into.
        /// </summary>
        public virtual List<ServiceUtils.CommodityChange> Outputs => mOutputs;

        /// <summary>
        /// Gets the motive commodity kind for the service.
        /// </summary>
        public static CommodityKind ServiceMotive
        {
            get
            {
                if (!ServiceUtils.ServiceMotives.TryGetValue(DerivedType, out var serviceMotive))
                {
                    serviceMotive = CommonUtils.GetCommodityKind("Be" + DerivedType.Name, CommodityKindType.Motive);
                }
                return serviceMotive;
            }
        }

        public override ServiceType ServiceType => mServiceType == default(ServiceType) ? (ServiceType)(DerivedType.GetProperty("ServiceTypeStatic")?.GetValue(null, null) ?? ServiceTypeStatic) : mServiceType;

        public static ServiceType ServiceTypeStatic => ServiceType.None;

        public virtual bool WaitsBeforePuttingAwayLeftovers => false;

        public static void AddInteractions(Bed bed)
        {
            bed.AddInteraction(SetUnsetServiceBed.Singleton, true);
        }

        /// <summary>
        /// Loads a motive tuning but (optionally) with a different commodity kind from the one specified in XML.
        /// </summary>
        private static void LoadMotive(XmlDocument xmlDocument, CommodityKind commodityKind = CommodityKind.None)
        {
            var elementsByTagName = xmlDocument.GetElementsByTagName("Motive");
            foreach (XmlElement motiveElement in elementsByTagName)
            {
                var tuningElement = (XmlElement)motiveElement.GetElementsByTagName("Tuning")[0];
                var addIfUniversalOnLoadFixUp = false;
                if (tuningElement.HasAttribute("codeVersions") && ParserFunctions.TryParseCommaSeparatedList(tuningElement.GetAttribute("codeVersions"), out var codeVersions, ProductVersion.Undefined))
                {
                    addIfUniversalOnLoadFixUp = true;
                    var hasProduct = false;
                    foreach (var codeVersion in codeVersions)
                    {
                        if (GameUtils.IsInstalled(codeVersion))
                        {
                            hasProduct = true;
                            break;
                        }
                    }
                    if (!hasProduct)
                    {
                        continue;
                    }
                }
                var commodityKindAttribute = tuningElement.GetAttribute("kind");
                if (commodityKind != CommodityKind.None || ParserFunctions.TryParseEnum(commodityKindAttribute, out commodityKind, CommodityKind.None))
                {
                    var universalAttribute = tuningElement.GetAttribute("universal");
                    var universal = true;
                    if (universalAttribute != "")
                    {
                        universal = ParserFunctions.ParseBool(universalAttribute);
                    }
                    var insatiableAttribute = tuningElement.GetAttribute("insatiable");
                    var insatiable = false;
                    if (insatiableAttribute != "")
                    {
                        insatiable = ParserFunctions.ParseBool(insatiableAttribute);
                    }
                    var ageSpecificityAttribute = tuningElement.GetAttribute("ageSpecificity");
                    var ageSpeciesSpecificity = CASAgeGenderFlags.AgeMask;
                    if (ageSpecificityAttribute != "" && ageSpecificityAttribute != "All")
                    {
                        ParserFunctions.TryParseEnum(ageSpecificityAttribute, out ageSpeciesSpecificity, CASAgeGenderFlags.AgeMask);
                    }
                    var speciesSpecifityAttribute = tuningElement.GetAttribute("speciesSpecificity");
                    if (speciesSpecifityAttribute != "" && ParserFunctions.TryParseEnum(speciesSpecifityAttribute, out var speciesSpecifity, CASAgeGenderFlags.None))
                    {
                        ageSpeciesSpecificity |= speciesSpecifity;
                    }
                    ParserFunctions.TryParseEnum(tuningElement.GetAttribute("worldSpecificityType"), out var worldRestrictionType, WorldRestrictionType.None);
                    ParserFunctions.TryParseCommaSeparatedList(tuningElement.GetAttribute("worldSpecificityWorldTypes"), out var worldRestrictionWorldTypes, WorldType.Undefined);
                    ParserFunctions.TryParseCommaSeparatedList(tuningElement.GetAttribute("worldSpecificityWorldNames"), out var worldRestrictionWorldNames, WorldName.Undefined);
                    var traitSpecificityAttribute = tuningElement.GetAttribute("traitSpecificity");
                    List<TraitNames> traitSpecificity = null;
                    if (traitSpecificityAttribute != "")
                    {
                        ParserFunctions.TryParseCommaSeparatedList(traitSpecificityAttribute, out var traitNames, TraitNames.Unknown);
                        foreach (var traitName in traitNames)
                        {
                            if (traitName != TraitNames.Unknown)
                            {
                                Lazy.Add(ref traitSpecificity, traitName);
                            }
                        }
                    }
                    ParserFunctions.TryParseEnum(tuningElement.GetAttribute("decayType"), out var decayType, DecayType.DecayFromAutoSatisfy);
                    var decayValue = ParserFunctions.ParseFloat(tuningElement.GetAttribute("decayValue"), 0f);
                    decayValue = MotiveTuning.HackToFixupCertainMotiveDecayRates(commodityKind, decayValue);
                    var initialMin = ParserFunctions.ParseFloat(tuningElement.GetAttribute("initialMin"), -100f);
                    var initialMax = ParserFunctions.ParseFloat(tuningElement.GetAttribute("initialMax"), 100f);
                    var timeRandomness = ParserFunctions.ParseFloat(tuningElement.GetAttribute("timeRandomness"), 0f);
                    var hasDefaultValue = ParserFunctions.ParseBool(tuningElement.GetAttribute("hasDefaultValue"));
                    var intensity = ParserFunctions.ParseInt(tuningElement.GetAttribute("intensity"), 1);
                    var intensityElements = motiveElement.GetElementsByTagName("Intensity");
                    var pointElements = ((XmlElement)intensityElements[0]).GetElementsByTagName("Point");
                    var count = pointElements.Count;
                    var coordinates = new Vector2[count];
                    var index = 0;
                    foreach (XmlElement pointElement in pointElements)
                    {
                        var x = ParserFunctions.ParseFloat(pointElement.GetAttribute("x"), 0f);
                        var y = ParserFunctions.ParseFloat(pointElement.GetAttribute("y"), 0f) * intensity;
                        var vector = new Vector2(x, y);
                        coordinates[index] = vector;
                        index++;
                    }
                    var curve = new DesireCurve(coordinates);
                    var motiveSatisfactionCurve = new MotiveSatisfactionCurve();
                    motiveSatisfactionCurve.Loops = true;
                    Curve autoSatisfyCurve = motiveSatisfactionCurve;
                    MotiveTuning.ParseCurve(motiveElement, "AutoSatisfy", autoSatisfyCurve);
                    var motiveDecayCurve = motiveSatisfactionCurve.GetMotiveDecayCurve();
                    var moodContributionCurve = new Curve();
                    MotiveTuning.ParseCurve(motiveElement, "MoodContribution", moodContributionCurve);
                    var buffTriggers = new List<MotiveTuning.MotiveBuffTrigger>();
                    foreach (XmlElement buffTriggerElement in  ((XmlElement)motiveElement.GetElementsByTagName("MotiveBuffs")[0]).GetElementsByTagName("BuffTrigger"))
                    {
                        var motiveBuffTrigger = new MotiveTuning.MotiveBuffTrigger();
                        motiveBuffTrigger.mTriggerValueStart = ParserFunctions.ParseFloat(buffTriggerElement.GetAttribute("TriggerValueStart"), -1000f);
                        motiveBuffTrigger.mTriggerValueEnd = ParserFunctions.ParseFloat(buffTriggerElement.GetAttribute("TriggerValueEnd"), -1000f);
                        motiveBuffTrigger.mDecay = ParserFunctions.ParseFloat(buffTriggerElement.GetAttribute("Decay"), 0f);
                        motiveBuffTrigger.mDecay = MotiveTuning.HackToFixupCertainMotiveDecayRates(commodityKind, motiveBuffTrigger.mDecay);
                        ParserFunctions.TryParseEnum(buffTriggerElement.GetAttribute("AddBuff"), out motiveBuffTrigger.mAddBuff, BuffNames.Undefined);
                        ParserFunctions.TryParseCommaSeparatedList(buffTriggerElement.GetAttribute("RemoveBuff"), out motiveBuffTrigger.mRemoveBuff, BuffNames.Undefined);
                        var customClassAttribute = buffTriggerElement.GetAttribute("CustomClass");
                        motiveBuffTrigger.mCustomClass = customClassAttribute.Length > 0 ? typeof(Motive).GetMethod(customClassAttribute) : null;
                        buffTriggers.Add(motiveBuffTrigger);
                    }
                    var motiveTuning = new MotiveTuning(commodityKind, universal, insatiable, ageSpeciesSpecificity, worldRestrictionType, worldRestrictionWorldTypes, worldRestrictionWorldNames, traitSpecificity, curve, decayType, decayValue, motiveSatisfactionCurve, motiveDecayCurve, moodContributionCurve, hasDefaultValue, initialMin, initialMax, timeRandomness, buffTriggers, addIfUniversalOnLoadFixUp);
                    if (!MotiveTuning.sTuning.TryGetValue((int)commodityKind, out var motiveTunings))
                    {
                        motiveTunings = MotiveTuning.sTuning[(int)commodityKind] = new List<MotiveTuning>();
                    }
                    motiveTunings.Add(motiveTuning);
                    Commodities.NewType(commodityKind, 1, motiveTuning.Min, motiveTuning.Max, 0f, true, -100f, 100f);
                }
            }
        }

        public virtual void AddOutputs()
        {
            foreach (var output in Outputs)
            {
                ServiceMotive.AddAsOutput(output.InteractionDefinitionType, output.TargetType, output.ConstantChange, output.Locked, output.ActualValue, output.UpdateType, output.TimeDependsOnCommodityFilling, output.UpdateEvenOnFailure, output.UpdateAboveAndBelowZero);
            }
        }

        public static void Create()
        {
            DebugUtils.TryDisplayScriptError(() =>
                {
                    if (ServiceNPCSpecifications.ValidForCurrentWorld((ServiceType)(DerivedType.GetProperty("ServiceTypeStatic")?.GetValue(null, null) ?? ServiceTypeStatic)))
                    {
                        if (Instance == null)
                        {
                            Activator.CreateInstance(DerivedType);
                        }
                        else
                        {
                            Instance.PostLoadFixup();
                        }
                    }
                    else
                    {
                        Destroy();
                    }
                });
        }

        public override SimDescription CreateNewNPCForPool(Lot lot)
        {
            return CreateOrUpdateServiceNpc(null, lot);
        }

        public new SimDescription CreateOrUpdateServiceNpc(SimDescription preCreatedSim, Lot lot)
        {
            var simDescription = preCreatedSim;
            DebugUtils.TryDisplayScriptError(() =>
                {
                    var shouldUsePlumbot = false;
                    if (simDescription == null)
                    {
                        if (GameUtils.IsInstalled(ProductVersion.EP11))
                        {
                            shouldUsePlumbot = ServiceNPCSpecifications.ShouldUseServobot(ServiceType.ToString());
                        }
                        simDescription = shouldUsePlumbot ? CreateRobotSim(this) : CreateSimDescription(this, this is CustomService customService ? ServiceNPCSpecifications.ChooseRandomAge(customService.Profile.ValidAges) : ServiceNPCSpecifications.GetAge(ServiceType.ToString()), GetGenderForNewNpc(lot));
                        simDescription.FindSuitableVirtualHome();
                    }
                    else
                    {
                        simDescription.CreatedByService = this;
                    }
                    if (simDescription != null)
                    {
                        ClearAllTraits(simDescription);
                        SetServiceNPCProperties(simDescription);
                        if (!shouldUsePlumbot)
                        {
                            SetTraits(simDescription);
                            SetRandomTraits(simDescription);
                        }
                    }
                });
            return simDescription;
        }

        public static SimDescription CreateSimDescription(Service<T> service, CASAgeGenderFlags ageIfRandom, CASAgeGenderFlags genderIfRandom)
        {
            return DebugUtils.TryDisplayScriptError(() =>
                {
                    var currentWorld = GameUtils.GetCurrentWorld();
                    var simDescription = CreateSimDescriptionInternal(service, null, ageIfRandom, genderIfRandom, currentWorld, out _);
                    service.GetNameForNewNpc(simDescription.IsMale, currentWorld, out var firstName, out var lastName);
                    simDescription.FirstName = firstName;
                    simDescription.LastName = lastName;
                    return simDescription;
                }, out var retVal) ? null : retVal;
        }

        public static SimDescription CreateSimDescriptionInternal(Service<T> service, string outfitName, CASAgeGenderFlags ageIfRandom, CASAgeGenderFlags genderIfRandom, WorldName homeWorld, out bool randomlyCreated)
        {
            SimDescription simDescription = null;
            var tempRandomlyCreated = false;
            DebugUtils.TryDisplayScriptError(() =>
                {
                    tempRandomlyCreated = false;
                    if ((service.IsHomelessService || LotManager.SelectRandomLotForNPCMoveIn(x => x.Household == null) == null) && Household.NpcHousehold == null)
                    {
                        simDescription = null;
                        return;
                    }
                    var outfitIsInvalid = false;
                    if (outfitName != null)
                    {
                        var simOutfit = new SimOutfit(ResourceKey.CreateOutfitKey(outfitName, 0));
                        if (simOutfit.IsValid)
                        {
                            var simBuilder = new SimBuilder
                                {
                                    UseCompression = true
                                };
                            OutfitUtils.SetOutfit(simBuilder, simOutfit, null);
                            var outfit = new SimOutfit(simBuilder.CacheOutfit("Service_" + outfitName));
                            simDescription = new SimDescription(outfit);
                            simDescription.AddOutfit(outfit, OutfitCategories.Everyday, true);
                        }
                        else
                        {
                            outfitIsInvalid = true;
                        }
                    }
                    if (outfitName == null || outfitIsInvalid)
                    {
                        simDescription = genderIfRandom == CASAgeGenderFlags.None ? Genetics.MakeSim(ageIfRandom) : Genetics.MakeSim(ageIfRandom, genderIfRandom, homeWorld, uint.MaxValue);
                        tempRandomlyCreated = true;
                    }
                    var lot = service.IsHomelessService ? null : LotManager.SelectRandomLotForNPCMoveIn(x => x.Household == null);
                    if (lot == null)
                    {
                        InitialServiceNpcSetup(service, simDescription);
                        return;
                    }
                    var household = new SimUtils.HouseholdCreationSpec().Instantiate();
                    household.Add(simDescription);
                    lot.MoveIn(household);
                    simDescription.CreatedByService = service;
                });
            randomlyCreated = tempRandomlyCreated;
            return simDescription;
        }

        public static void Destroy()
        {
            Destroy(Instance);
            Instance = null;
        }

        public override SimDescription FindSimForAssignment(Lot lot)
        {
            return DebugUtils.TryDisplayScriptError(() =>
                {
                    var shouldUseServobot = false;
                    if (GameUtils.IsInstalled(ProductVersion.EP11))
                    {
                        shouldUseServobot = ServiceNPCSpecifications.ShouldUseServobot(ServiceType.ToString());
                    }
                    var pool = new List<SimDescription>();
                    foreach (var simDescription in mPool)
                    {
                        if (!IsSimAssignedTask(simDescription) && CanSimBeAssignedToLot(simDescription, lot) && (shouldUseServobot && simDescription.IsEP11Bot || !shouldUseServobot && !simDescription.IsEP11Bot) && simDescription.CreatedSim == null)
                        {
                            pool.Add(simDescription);
                        }
                    }
                    if (pool.Count == 0)
                    {
                        var simDescription = CreateNewNPCForPool(lot);
                        if (simDescription != null)
                        {
                            AddSimToPool(simDescription);
                        }
                        return simDescription;
                    }
                    var randomSimDescription = RandomUtil.GetRandomObjectFromList(pool);
                    if (AlwaysTryToSendSameSim && lot.Household != null)
                    {
                        if (mPreferredServiceNpc.TryGetValue(lot.Household.HouseholdId, out var simDescription))
                        {
                            if (pool.Contains(simDescription))
                            {
                                return simDescription;
                            }
                        }
                        else
                        {
                            mPreferredServiceNpc[lot.Household.HouseholdId] = randomSimDescription;
                        }
                    }
                    return randomSimDescription;
                }, out var retVal) ? null : retVal;
        }

        public override string GetServiceTopic(Sim serviceSim)
        {
            return DerivedType.Name + " Service";
        }

        /// <summary>
        /// Call this method for every class derived from this one within its static constructor.
        /// </summary>
        public static void Init()
        {
            World.OnObjectPlacedInLotEventHandler += (sender, e) => DebugUtils.TryDisplayScriptError(() =>
                {
                    if (e is World.OnObjectPlacedInLotEventArgs onObjectPlacedInLotEventArgs)
                    {
                        var gameObject = GameObject.GetObject(onObjectPlacedInLotEventArgs.ObjectId);
                        if (typeof(IAmLiveInService).IsAssignableFrom(DerivedType))
                        {
                            if (gameObject is Bed bed)
                            {
                                AddInteractions(bed);
                            }
                        }
                    }
                });
            World.sOnWorldLoadFinishedEventHandler += (sender, e) => DebugUtils.TryDisplayScriptError(() =>
                {
                    if (!ServiceUtils.LoadedTypes.Contains(DerivedType))
                    {
                        CommonUtils.AddEnumValue<CommodityKind>("Be" + DerivedType.Name, ServiceMotive);
                        LoadServiceMotive();
                        var activeTopic = DerivedType.Name + " Service";
                        if (!ActiveTopicData.Exists(activeTopic))
                        {
                            ActiveTopicData.Add(new ActiveTopicData(activeTopic, false, 1000, "", true, true, false, true, null, 0f, "", false));
                        }
                        CommonUtils.AddActions(activeTopic, LongTermRelationshipTypes.Default, false, "Dismiss", "Fire");
                        ServiceUtils.LoadedTypes.Add(DerivedType);
                    }
                    var createMethod = DerivedType.GetMethod("Create");
                    if (createMethod == null)
                    {
                        Create();
                    }
                    else
                    {
                        createMethod.Invoke(null, null);
                    }
                    if (Instance == null)
                    {
                        return;
                    }
                    Instance.AddOutputs();
                    using (var enumerator = Instance.Pool.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            if (enumerator.Current != null && enumerator.Current.CreatedSim != null)
                            {
                                CommonUtils.UpdateMotiveTunings(enumerator.Current.CreatedSim, ServiceMotive);
                            }
                        }

                        if (typeof(IAmLiveInService).IsAssignableFrom(DerivedType))
                        {
                            foreach (var bed in Queries.GetObjects<Bed>())
                            {
                                AddInteractions(bed);
                            }
                        }
                    }
                });
            World.sOnWorldQuitEventHandler += (sender, e) =>
                {
                    if (Instance != null)
                    {
                        Instance = null;
                    }
                };
        }

        public static void LoadServiceMotive(CommodityKind? serviceMotive = null)
        {
            LoadMotive(Simulator.LoadXML("ExpandedHouseholdStaff_ServiceMotive"), serviceMotive ?? ServiceMotive);
        }

        public override bool NeedsAssignment(Lot lot)
        {
            return IsServiceRequested(lot) && !IsAnySimAssignedToLot(lot);
        }

        public virtual void RemoveOutputs()
        {
            foreach (var output in Outputs)
            {
                ServiceMotive.RemoveAsOutput(output.InteractionDefinitionType, output.TargetType, output.ConstantChange, output.Locked, output.ActualValue, output.UpdateType, output.TimeDependsOnCommodityFilling, output.UpdateEvenOnFailure, output.UpdateAboveAndBelowZero);
            }
        }
    }
}
