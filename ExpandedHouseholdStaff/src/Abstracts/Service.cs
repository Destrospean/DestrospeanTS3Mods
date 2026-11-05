using System;
using System.Collections.Generic;
using System.Reflection;
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
                    List<Sim> simsAssignedToLot = Instance.GetSimsAssignedToLot(actor.LotHome);
                    return Localization.LocalizeString(actor.IsFemale, DerivedType.GetLocalizationKey() + "/" + typeof(SetUnsetServiceBed).Name + (target.FindOwnedBed(simsAssignedToLot[0]) == target ? ":Unset" : ":Set") + "InteractionName", simsAssignedToLot[0].SimDescription);
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
                            List<Sim> simsAssignedToLot = Instance.GetSimsAssignedToLot(actor.LotHome);
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
                    }
                    return false;
                }
            }

            public static InteractionDefinition Singleton = new Definition();

            public override bool Run()
            {
                if (Instance != null)
                {
                    Sim simActiveOnLot = Instance.GetSimActiveOnLot(Actor.LotHome);
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

        readonly List<ServiceUtils.CommodityChange> mOutputs = new List<ServiceUtils.CommodityChange>();

        ServiceType mServiceType;

        public static Type DerivedType
        {
            get
            {
                return typeof(T);
            }
        }

        public static Service<T> Instance
        {
            get
            {
                return ServiceUtils.PredefinedInstances.TryGetValue(DerivedType.Name, out Service service) ? (Service<T>)service : null;
            }
            set
            {
                ServiceUtils.PredefinedInstances[DerivedType.Name] = value;
            }
        }

        public virtual bool IsHomelessService
        {
            get
            {
                return true;
            }
        }

        public override bool IsPaidWeekly
        {
            get
            {
                return this is IAmLiveInService;
            }
        }

        public virtual bool IsQuietAroundSleepingSims
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Outputs for interactions and their target types that the service motive of the service is inserted into.
        /// </summary>
        public virtual List<ServiceUtils.CommodityChange> Outputs
        {
            get
            {
                return mOutputs;
            }
        }

        /// <summary>
        /// Gets the motive commodity kind for the service.
        /// </summary>
        public static CommodityKind ServiceMotive
        {
            get
            {
                if (!ServiceUtils.ServiceMotives.TryGetValue(DerivedType, out CommodityKind serviceMotive))
                {
                    serviceMotive = CommonUtils.GetCommodityKind("Be" + DerivedType.Name, CommodityKindType.Motive);
                }
                return serviceMotive;
            }
        }

        public override ServiceType ServiceType
        {
            get
            {
                return mServiceType == default(ServiceType) ? (ServiceType)(DerivedType.GetProperty("ServiceTypeStatic")?.GetValue(null, null) ?? ServiceTypeStatic) : mServiceType;
            }
        }

        public static ServiceType ServiceTypeStatic
        {
            get
            {
                return ServiceType.None;
            }
        }

        public virtual bool WaitsBeforePuttingAwayLeftovers
        {
            get
            {
                return false;
            }
        }

        public static void AddInteractions(Bed bed)
        {
            bed.AddInteraction(SetUnsetServiceBed.Singleton, true);
        }

        /// <summary>
        /// Loads a motive tuning but (optionally) with a different commodity kind from the one specified in XML.
        /// </summary>
        static void LoadMotive(XmlDocument xmlDocument, CommodityKind commodityKind = CommodityKind.None)
        {
            XmlNodeList elementsByTagName = xmlDocument.GetElementsByTagName("Motive");
            foreach (XmlElement motiveElement in elementsByTagName)
            {
                XmlElement tuningElement = (XmlElement)motiveElement.GetElementsByTagName("Tuning")[0];
                bool addIfUniversalOnLoadFixUp = false;
                if (tuningElement.HasAttribute("codeVersions") && ParserFunctions.TryParseCommaSeparatedList<ProductVersion>(tuningElement.GetAttribute("codeVersions"), out List<ProductVersion> codeVersions, ProductVersion.Undefined))
                {
                    addIfUniversalOnLoadFixUp = true;
                    bool hasProduct = false;
                    foreach (ProductVersion codeVersion in codeVersions)
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
                string commodityKindAttribute = tuningElement.GetAttribute("kind");
                if (commodityKind != CommodityKind.None || ParserFunctions.TryParseEnum<CommodityKind>(commodityKindAttribute, out commodityKind, CommodityKind.None))
                {
                    string universalAttribute = tuningElement.GetAttribute("universal");
                    bool universal = true;
                    if (universalAttribute != "")
                    {
                        universal = ParserFunctions.ParseBool(universalAttribute);
                    }
                    string insatiableAttribute = tuningElement.GetAttribute("insatiable");
                    bool insatiable = false;
                    if (insatiableAttribute != "")
                    {
                        insatiable = ParserFunctions.ParseBool(insatiableAttribute);
                    }
                    string ageSpecificityAttribute = tuningElement.GetAttribute("ageSpecificity");
                    CASAgeGenderFlags ageSpeciesSpecificity = CASAgeGenderFlags.AgeMask;
                    if (ageSpecificityAttribute != "" && ageSpecificityAttribute != "All")
                    {
                        ParserFunctions.TryParseEnum<CASAgeGenderFlags>(ageSpecificityAttribute, out ageSpeciesSpecificity, CASAgeGenderFlags.AgeMask);
                    }
                    string speciesSpecifityAttribute = tuningElement.GetAttribute("speciesSpecificity");
                    if (speciesSpecifityAttribute != "" && ParserFunctions.TryParseEnum<CASAgeGenderFlags>(speciesSpecifityAttribute, out CASAgeGenderFlags speciesSpecifity, CASAgeGenderFlags.None))
                    {
                        ageSpeciesSpecificity |= speciesSpecifity;
                    }
                    ParserFunctions.TryParseEnum<WorldRestrictionType>(tuningElement.GetAttribute("worldSpecificityType"), out WorldRestrictionType worldRestrictionType, WorldRestrictionType.None);
                    ParserFunctions.TryParseCommaSeparatedList<WorldType>(tuningElement.GetAttribute("worldSpecificityWorldTypes"), out List<WorldType> worldRestrictionWorldTypes, WorldType.Undefined);
                    ParserFunctions.TryParseCommaSeparatedList<WorldName>(tuningElement.GetAttribute("worldSpecificityWorldNames"), out List<WorldName> worldRestrictionWorldNames, WorldName.Undefined);
                    string traitSpecificityAttribute = tuningElement.GetAttribute("traitSpecificity");
                    List<TraitNames> traitSpecificity = null;
                    if (traitSpecificityAttribute != "")
                    {
                        ParserFunctions.TryParseCommaSeparatedList<TraitNames>(traitSpecificityAttribute, out List<TraitNames> traitNames, TraitNames.Unknown);
                        foreach (TraitNames traitName in traitNames)
                        {
                            if (traitName != TraitNames.Unknown)
                            {
                                Lazy.Add<List<TraitNames>, TraitNames>(ref traitSpecificity, traitName);
                            }
                        }
                    }
                    ParserFunctions.TryParseEnum<DecayType>(tuningElement.GetAttribute("decayType"), out DecayType decayType, DecayType.DecayFromAutoSatisfy);
                    float decayValue = ParserFunctions.ParseFloat(tuningElement.GetAttribute("decayValue"), 0f);
                    decayValue = MotiveTuning.HackToFixupCertainMotiveDecayRates(commodityKind, decayValue);
                    float initialMin = ParserFunctions.ParseFloat(tuningElement.GetAttribute("initialMin"), -100f);
                    float initialMax = ParserFunctions.ParseFloat(tuningElement.GetAttribute("initialMax"), 100f);
                    float timeRandomness = ParserFunctions.ParseFloat(tuningElement.GetAttribute("timeRandomness"), 0f);
                    bool hasDefaultValue = ParserFunctions.ParseBool(tuningElement.GetAttribute("hasDefaultValue"));
                    int intensity = ParserFunctions.ParseInt(tuningElement.GetAttribute("intensity"), 1);
                    XmlNodeList intensityElements = motiveElement.GetElementsByTagName("Intensity");
                    XmlNodeList pointElements = ((XmlElement)intensityElements[0]).GetElementsByTagName("Point");
                    int count = pointElements.Count;
                    Vector2[] coordinates = new Vector2[count];
                    int index = 0;
                    foreach (XmlElement pointElement in pointElements)
                    {
                        float x = ParserFunctions.ParseFloat(pointElement.GetAttribute("x"), 0f);
                        float y = ParserFunctions.ParseFloat(pointElement.GetAttribute("y"), 0f) * intensity;
                        Vector2 vector = new Vector2(x, y);
                        coordinates[index] = vector;
                        index++;
                    }
                    DesireCurve curve = new DesireCurve(coordinates);
                    MotiveSatisfactionCurve motiveSatisfactionCurve = new MotiveSatisfactionCurve();
                    motiveSatisfactionCurve.Loops = true;
                    Curve autoSatisfyCurve = motiveSatisfactionCurve;
                    MotiveTuning.ParseCurve(motiveElement, "AutoSatisfy", autoSatisfyCurve);
                    Curve motiveDecayCurve = motiveSatisfactionCurve.GetMotiveDecayCurve();
                    Curve moodContributionCurve = new Curve();
                    MotiveTuning.ParseCurve(motiveElement, "MoodContribution", moodContributionCurve);
                    List<MotiveTuning.MotiveBuffTrigger> buffTriggers = new List<MotiveTuning.MotiveBuffTrigger>();
                    foreach (XmlElement buffTriggerElement in  ((XmlElement)motiveElement.GetElementsByTagName("MotiveBuffs")[0]).GetElementsByTagName("BuffTrigger"))
                    {
                        MotiveTuning.MotiveBuffTrigger motiveBuffTrigger = new MotiveTuning.MotiveBuffTrigger();
                        motiveBuffTrigger.mTriggerValueStart = ParserFunctions.ParseFloat(buffTriggerElement.GetAttribute("TriggerValueStart"), -1000f);
                        motiveBuffTrigger.mTriggerValueEnd = ParserFunctions.ParseFloat(buffTriggerElement.GetAttribute("TriggerValueEnd"), -1000f);
                        motiveBuffTrigger.mDecay = ParserFunctions.ParseFloat(buffTriggerElement.GetAttribute("Decay"), 0f);
                        motiveBuffTrigger.mDecay = MotiveTuning.HackToFixupCertainMotiveDecayRates(commodityKind, motiveBuffTrigger.mDecay);
                        ParserFunctions.TryParseEnum<BuffNames>(buffTriggerElement.GetAttribute("AddBuff"), out motiveBuffTrigger.mAddBuff, BuffNames.Undefined);
                        ParserFunctions.TryParseCommaSeparatedList<BuffNames>(buffTriggerElement.GetAttribute("RemoveBuff"), out motiveBuffTrigger.mRemoveBuff, BuffNames.Undefined);
                        string customClassAttribute = buffTriggerElement.GetAttribute("CustomClass");
                        motiveBuffTrigger.mCustomClass = customClassAttribute.Length > 0 ? typeof(Motive).GetMethod(customClassAttribute) : null;
                        buffTriggers.Add(motiveBuffTrigger);
                    }
                    MotiveTuning motiveTuning = new MotiveTuning(commodityKind, universal, insatiable, ageSpeciesSpecificity, worldRestrictionType, worldRestrictionWorldTypes, worldRestrictionWorldNames, traitSpecificity, curve, decayType, decayValue, motiveSatisfactionCurve, motiveDecayCurve, moodContributionCurve, hasDefaultValue, initialMin, initialMax, timeRandomness, buffTriggers, addIfUniversalOnLoadFixUp);
                    if (!MotiveTuning.sTuning.TryGetValue((int)commodityKind, out List<MotiveTuning> motiveTunings))
                    {
                        motiveTunings = (MotiveTuning.sTuning[(int)commodityKind] = new List<MotiveTuning>());
                    }
                    motiveTunings.Add(motiveTuning);
                    Commodities.NewType(commodityKind, 1, motiveTuning.Min, motiveTuning.Max, 0f, true, -100f, 100f);
                }
            }
        }

        public virtual void AddOutputs()
        {
            foreach (ServiceUtils.CommodityChange output in Outputs)
            {
                ServiceMotive.AddAsOutput(output.InteractionDefinitionType, output.TargetType, output.ConstantChange, output.Locked, output.ActualValue, output.UpdateType, output.TimeDependsOnCommodityFilling, output.UpdateEvenOnFailure, output.UpdateAboveAndBelowZero);
            }
        }

        public static void Create()
        {
            DebugUtils.TryDisplayScriptError(() =>
                {
                    if (ServiceNPCSpecifications.ValidForCurrentWorld((ServiceType)(DerivedType.GetProperty("ServiceTypeStatic").GetValue(null, null) ?? ServiceTypeStatic)))
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
            SimDescription simDescription = preCreatedSim;
            DebugUtils.TryDisplayScriptError(() =>
                {
                    bool shouldUsePlumbot = false;
                    if (simDescription == null)
                    {
                        if (GameUtils.IsInstalled(ProductVersion.EP11))
                        {
                            shouldUsePlumbot = ServiceNPCSpecifications.ShouldUseServobot(ServiceType.ToString());
                        }
                        if (shouldUsePlumbot)
                        {
                            simDescription = CreateRobotSim(this);
                        }
                        else
                        {
                            simDescription = CreateSimDescription(this, this is CustomService customService ? ServiceNPCSpecifications.ChooseRandomAge(customService.Profile.ValidAges) : ServiceNPCSpecifications.GetAge(ServiceType.ToString()), GetGenderForNewNpc(lot));
                        }
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
                    WorldName currentWorld = GameUtils.GetCurrentWorld();
                    SimDescription simDescription = CreateSimDescriptionInternal(service, null, ageIfRandom, genderIfRandom, currentWorld, out bool randomlyCreated);
                    service.GetNameForNewNpc(simDescription.IsMale, currentWorld, out string firstName, out string lastName);
                    simDescription.FirstName = firstName;
                    simDescription.LastName = lastName;
                    return simDescription;
                }, out SimDescription retVal) ? null : retVal;
        }

        public static SimDescription CreateSimDescriptionInternal(Service<T> service, string outfitName, CASAgeGenderFlags ageIfRandom, CASAgeGenderFlags genderIfRandom, WorldName homeWorld, out bool randomlyCreated)
        {
            SimDescription simDescription = null;
            bool tempRandomlyCreated = false;
            DebugUtils.TryDisplayScriptError(() =>
                {
                    tempRandomlyCreated = false;
                    if ((service.IsHomelessService || LotManager.SelectRandomLotForNPCMoveIn(x => x.Household == null) == null) && Household.NpcHousehold == null)
                    {
                        simDescription = null;
                        return;
                    }
                    bool outfitIsInvalid = false;
                    if (outfitName != null)
                    {
                        SimOutfit simOutfit = new SimOutfit(ResourceKey.CreateOutfitKey(outfitName, 0));
                        if (simOutfit.IsValid)
                        {
                            SimBuilder simBuilder = new SimBuilder
                                {
                                    UseCompression = true
                                };
                            OutfitUtils.SetOutfit(simBuilder, simOutfit, null);
                            SimOutfit outfit = new SimOutfit(simBuilder.CacheOutfit("Service_" + outfitName));
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
                    Lot lot = service.IsHomelessService ? null : LotManager.SelectRandomLotForNPCMoveIn(x => x.Household == null);
                    if (lot == null)
                    {
                        InitialServiceNpcSetup(service, simDescription);
                        return;
                    }
                    Household household = new SimUtils.HouseholdCreationSpec().Instantiate();
                    household.Add(simDescription);
                    lot.MoveIn(household);
                    simDescription.CreatedByService = service;
                    return;
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
                    bool shouldUseServobot = false;
                    if (GameUtils.IsInstalled(ProductVersion.EP11))
                    {
                        shouldUseServobot = ServiceNPCSpecifications.ShouldUseServobot(ServiceType.ToString());
                    }
                    List<SimDescription> pool = new List<SimDescription>();
                    foreach (SimDescription simDescription in mPool)
                    {
                        if (!IsSimAssignedTask(simDescription) && CanSimBeAssignedToLot(simDescription, lot) && (shouldUseServobot && simDescription.IsEP11Bot || !shouldUseServobot && !simDescription.IsEP11Bot) && simDescription.CreatedSim == null)
                        {
                            pool.Add(simDescription);
                        }
                    }
                    if (pool.Count == 0)
                    {
                        SimDescription simDescription = CreateNewNPCForPool(lot);
                        if (simDescription != null)
                        {
                            AddSimToPool(simDescription);
                        }
                        return simDescription;
                    }
                    SimDescription randomSimDescription = RandomUtil.GetRandomObjectFromList<SimDescription>(pool);
                    if (AlwaysTryToSendSameSim && lot.Household != null)
                    {
                        if (mPreferredServiceNpc.TryGetValue(lot.Household.HouseholdId, out SimDescription simDescription))
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
                }, out SimDescription retVal) ? null : retVal;
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
                        GameObject gameObject = GameObject.GetObject(onObjectPlacedInLotEventArgs.ObjectId);
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
                        string activeTopic = DerivedType.Name + " Service";
                        if (!ActiveTopicData.Exists(activeTopic))
                        {
                            ActiveTopicData.Add(new ActiveTopicData(activeTopic, false, 1000, "", true, true, false, true, null, 0f, "", false));
                        }
                        CommonUtils.AddActions(activeTopic, LongTermRelationshipTypes.Default, false, "Dismiss", "Fire");
                        ServiceUtils.LoadedTypes.Add(DerivedType);
                    }
                    MethodInfo createMethod = DerivedType.GetMethod("Create");
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
                    IEnumerator<SimDescription> enumerator = Instance.Pool.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current != null && enumerator.Current.CreatedSim != null)
                        {
                            CommonUtils.UpdateMotiveTunings(enumerator.Current.CreatedSim, ServiceMotive);
                        }
                    }
                    if (typeof(IAmLiveInService).IsAssignableFrom(DerivedType))
                    {
                        foreach (Bed bed in Sims3.Gameplay.Queries.GetObjects<Bed>())
                        {
                            AddInteractions(bed);
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
            foreach (ServiceUtils.CommodityChange output in Outputs)
            {
                ServiceMotive.RemoveAsOutput(output.InteractionDefinitionType, output.TargetType, output.ConstantChange, output.Locked, output.ActualValue, output.UpdateType, output.TimeDependsOnCommodityFilling, output.UpdateEvenOnFailure, output.UpdateAboveAndBelowZero);
            }
        }
    }
}
