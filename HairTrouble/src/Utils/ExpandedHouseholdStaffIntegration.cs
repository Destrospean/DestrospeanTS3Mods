extern alias ExpandedHouseholdStaff;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using Destrospean.Enums.HairTrouble;
using Destrospean.HairTrouble;
using Destrospean.UI.Dialogs;
using ExpandedHouseholdStaff::Destrospean.Enums;
using ExpandedHouseholdStaff::Destrospean.Enums.ExpandedHouseholdStaff;
using ExpandedHouseholdStaff::Destrospean.Misc;
using ExpandedHouseholdStaff::Destrospean.Utils;
using ExpandedHouseholdStaff::Destrospean.Utils.ExpandedHouseholdStaff;
using ExpandedHouseholdStaff::Sims3.Gameplay.Interfaces.Destrospean.ExpandedHouseholdStaff;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Destrospean.Utils;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.SimIFace.CustomContent;
using Sims3.UI.Controller;
using static Sims3.Gameplay.Gameflow;
using EditServiceUniform_Original = ExpandedHouseholdStaff::Destrospean.ExpandedHouseholdStaff.Interactions.EditServiceUniform;
using FileUtils_Original = ExpandedHouseholdStaff::Destrospean.Utils.ExpandedHouseholdStaff.FileUtils;

namespace Destrospean.Utils.HairTrouble
{
    public static class ExpandedHouseholdStaffIntegration
    {
        public class EditServiceUniform : EditServiceUniform_Original
        {
            [DoesntRequireTuning]
            public new class Definition : ImmediateInteractionDefinition<Sim, GameObject, EditServiceUniform>
            {
                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair iop)
                {
                    return Localization.LocalizeString(actor.IsFemale, LocalizationKey + ":Name");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new[]
                    {
                        Localization.LocalizeString(isFemale, LocalizationKey + ":Path")
                    };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return !isAutonomous && ServiceUtils.ServiceProfiles.Exists(x => !x.IsImmutable);
                }
            }

            public new static Definition Singleton = new Definition();
            
            public override bool Run()
            {
                return !DebugUtils.TryDisplayScriptError(() =>
                    {
                        while (true)
                        {
                            if (ServiceUtils.TryUIGetSelectedServiceProfiles(out var profiles, ServiceUtils.ServiceProfiles.FindAll(x => !x.IsImmutable).ToArray(), Localization.LocalizeString(LocalizationKey + ":Name"), 1))
                            {
                                var entryKey = typeof(ObjectPickerDialog).GetLocalizationKey().Replace(nameof(ObjectPickerDialog), "ServiceOutfitDemographicDialog");
                                var age = CASAgeGenderFlags.None;
                                var gender = CASAgeGenderFlags.None;
                                var hairGrowthState = HairGrowthStates.Unknown;
                                sbyte step = 0;
                                while (true)
                                {
                                    if (step == 0)
                                    {
                                        if (!CASAgeGenderFlagListDialog.ShowCASAgeGenderFlagListDialog(out age, CASAgeGenderFlags.None, (profiles[0].ValidAges & CASAgeGenderFlags.YoungAdult) == 0 ? profiles[0].ValidAges : profiles[0].ValidAges ^ CASAgeGenderFlags.YoungAdult | CASAgeGenderFlags.Adult, Localization.LocalizeString(entryKey + "/Titles:Age"), entryKey, false))
                                        {
                                            step--;
                                            continue;
                                        }
                                        step++;
                                    }
                                    if (step == 1)
                                    {
                                        if (!CASAgeGenderFlagListDialog.ShowCASAgeGenderFlagListDialog(out gender, CASAgeGenderFlags.None, profiles[0].ValidGenders == CASAgeGenderFlags.None ? CASAgeGenderFlags.GenderMask : profiles[0].ValidGenders, Localization.LocalizeString(entryKey + "/Titles:Gender"), entryKey, false))
                                        {
                                            step--;
                                            continue;
                                        }
                                        step++;
                                    }
                                    if (step == 2)
                                    {
                                        if (!SimHairGrowth.TryUIGetHairGrowthState(out hairGrowthState))
                                        {
                                            step--;
                                            continue;
                                        }
                                        step++;
                                    }
                                    break;
                                }
                                if (step == -1)
                                {
                                    continue;
                                }
                                var simDescription = Genetics.MakeSim(age, gender, GameUtils.GetCurrentWorld(), uint.MaxValue);
                                EventTracker.AddListener(EventTypeId.kSimInstantiated, evt =>
                                    {
                                        var listenerAction = ListenerAction.Keep;
                                        DebugUtils.TryDisplayScriptError(() =>
                                            {
                                                if (evt.TargetObject as Sim != simDescription.CreatedSim)
                                                {
                                                    return;
                                                }
                                                simDescription.FirstName = "HairTrouble_TempSim";
                                                simDescription.SetHairGrowthState(hairGrowthState);
                                                var specialOutfitKey = simDescription.GetGlobalAssignedOutfitPrefix() + profiles[0].Name;
                                                BodyTypes[] preSelectedPartOverrides = null;
                                                if (simDescription.TryGetGlobalOutfitAssignment(profiles[0], out var outfitAssignment))
                                                {
                                                    simDescription.AddAssignedOutfit(OutfitAssignmentUtils.AssignedOutfits[outfitAssignment.SpecialOutfitKey], specialOutfitKey);
                                                    preSelectedPartOverrides = OutfitAssignmentUtils.AssignedOutfits[specialOutfitKey].PartOverrides.ToArray();
                                                }
                                                var previousGameSpeed = CurrentGameSpeed;
                                                if (OutfitExtensions.EditSpecialOutfit(simDescription.CreatedSim, specialOutfitKey))
                                                {
                                                    OutfitAssignmentUtils.AssignOutfitToService(null, specialOutfitKey, profiles[0], simDescription);
                                                    ActionTask.Start(() => DebugUtils.TryDisplayScriptError(() =>
                                                        {
                                                            if (OutfitAssignmentUtils.ShowPartOverrideListDialog(OutfitAssignmentUtils.AssignedOutfits[specialOutfitKey] = new OutfitAssignmentUtils.AssignedOutfit(simDescription.GetSpecialOutfit(specialOutfitKey)), out var partOverrides, preSelectedPartOverrides))
                                                            {
                                                                OutfitAssignmentUtils.AssignedOutfits[specialOutfitKey].PartOverrides = new List<BodyTypes>(partOverrides);
                                                                if (!SimHairGrowth.PreferredHairstylesBySpecialOutfitAndGrowthState.TryGetValue(specialOutfitKey, out var outfitHairData))
                                                                {
                                                                    outfitHairData = SimHairGrowth.PreferredHairstylesBySpecialOutfitAndGrowthState[specialOutfitKey] = new SimHairGrowth.OutfitHairData(0uL, ResourceKey.kInvalidResourceKey, OutfitCategories.None, 0);
                                                                }
                                                                var savedPart = OutfitAssignmentUtils.AssignedOutfits[specialOutfitKey].Parts.Find(x => x.Part.BodyType == BodyTypes.Hair);
                                                                outfitHairData.PreferredHairstylesByGrowthState[hairGrowthState] = savedPart.Part;
                                                                outfitHairData.PreferredHatPresetsByGrowthState[hairGrowthState] = savedPart.Preset;
                                                            }
                                                            SetGameSpeed(previousGameSpeed, SetGameSpeedContext.GameStates);
                                                        }));
                                                }
                                                simDescription.CreatedSim?.RemoveFromWorld();
                                                AlarmManager.Global.AddAlarm(1f, TimeUnit.Minutes, () =>
                                                    {
                                                        Household.TouristHousehold.RemoveTemporary(simDescription);
                                                        simDescription.Dispose();
                                                    }, "Remove SimDescription: " + simDescription.SimDescriptionId, AlarmType.AlwaysPersisted, simDescription);
                                                SetGameSpeed(Gameflow.GameSpeed.Pause, SetGameSpeedContext.GameStates);
                                                listenerAction = ListenerAction.Remove;
                                            });
                                        return listenerAction;
                                    });
                                Household.CreateTouristHousehold();
                                Household.TouristHousehold.AddTemporary(simDescription);
                                simDescription.Init(simDescription.GetOutfit(OutfitCategories.Everyday, 0));
                                simDescription.Instantiate(Actor.LotHome);
                            }
                            break;
                        }
                    });
            }
        }

        public class FileUtils : FileUtils_Original
        {
            public override void LoadXml(string text)
            {
                using (var stringReader = new StringReader(text))
                {
                    using (var xmlReader = XmlReader.Create(stringReader, new XmlReaderSettings
                        {
                            IgnoreComments = true
                        }))
                    {
                        var xmlDocument = new XmlDocument();
                        xmlDocument.Load(xmlReader);
                        foreach (XmlNode node in xmlDocument.DocumentElement.ChildNodes)
                        {
                            if (node.Name == "ServiceProfile" && node is XmlElement element)
                            {
                                var serviceName = element.GetAttribute("name");
                                var importedServiceMotiveName = element.GetAttribute("workMotive") ?? "Be" + serviceName;
                                var profile = new ServiceUtils.ServiceProfile(serviceName, null)
                                    {
                                        ServiceMotive = CommodityUtils.GetCommodityKind(importedServiceMotiveName, CommodityKindType.Motive),
                                        VersionString = element.GetAttribute("version")
                                    };
                                var serviceMotiveName = importedServiceMotiveName;
                                while (ServiceUtils.ServiceMotiveExists(profile.ServiceMotive))
                                {
                                    serviceMotiveName = importedServiceMotiveName + "_" + DownloadContent.GenerateGUID();
                                    profile.ServiceMotive = CommodityUtils.GetCommodityKind(serviceMotiveName, CommodityKindType.Motive);
                                }
                                CommonUtils.AddEnumValue<CommodityKind>(serviceMotiveName, profile.ServiceMotive);
                                profile.Motives = new List<CommodityKind>
                                    {
                                        profile.ServiceMotive
                                    };
                                foreach (XmlNode profilePropertyNode in node.ChildNodes)
                                {
                                    if (!(profilePropertyNode is XmlElement profilePropertyElement))
                                    {
                                        continue;
                                    }
                                    if (profilePropertyNode.Name == "Title")
                                    {
                                        profile.Title = ParserFunctions.ParseBool(profilePropertyElement.GetAttribute("localize")) ? Localization.LocalizeString(profilePropertyNode.InnerText) : profilePropertyNode.InnerText;
                                        continue;
                                    }
                                    if (profilePropertyNode.Name == "RequestedMessage")
                                    {
                                        profile.RequestedMessage = ParserFunctions.ParseBool(profilePropertyElement.GetAttribute("localize")) ? Localization.LocalizeString(profilePropertyNode.InnerText) : profilePropertyNode.InnerText;
                                        continue;
                                    }
                                    if (profilePropertyNode.Name == "CancelledMessage")
                                    {
                                        profile.CancelledMessage = ParserFunctions.ParseBool(profilePropertyElement.GetAttribute("localize")) ? Localization.LocalizeString(profilePropertyNode.InnerText) : profilePropertyNode.InnerText;
                                        continue;
                                    }
                                    if (profilePropertyNode.Name == "CancelledWhileActiveMessage")
                                    {
                                        profile.CancelledWhileActiveMessage = ParserFunctions.ParseBool(profilePropertyElement.GetAttribute("localize")) ? Localization.LocalizeString(profilePropertyNode.InnerText) : profilePropertyNode.InnerText;
                                        continue;
                                    }
                                    if (profilePropertyNode.Name == "DelayBeforeArriving")
                                    {
                                        profile.DelayBeforeArriving = ParserFunctions.ParseFloat(profilePropertyNode.InnerText, profile.DelayBeforeArriving);
                                        continue;
                                    }
                                    if (profilePropertyNode.Name == "DelayBeforeLeaving")
                                    {
                                        profile.DelayBeforeLeaving = ParserFunctions.ParseFloat(profilePropertyNode.InnerText, profile.DelayBeforeLeaving);
                                        continue;
                                    }
                                    if (profilePropertyNode.Name == "ExtraWaitTimeAfterSocializing")
                                    {
                                        profile.ExtraWaitTimeAfterSocializing = ParserFunctions.ParseFloat(profilePropertyNode.InnerText, profile.ExtraWaitTimeAfterSocializing);
                                        continue;
                                    }
                                    if (profilePropertyNode.Name == "ValidAges")
                                    {
                                        try
                                        {
                                            profile.ValidAges = (CASAgeGenderFlags)Enum.Parse(typeof(CASAgeGenderFlags), profilePropertyNode.InnerText);
                                        }
                                        catch (ArgumentException)
                                        {
                                        }
                                        continue;
                                    }
                                    if (profilePropertyNode.Name == "ValidGenders")
                                    {
                                        try
                                        {
                                            profile.ValidGenders = (CASAgeGenderFlags)Enum.Parse(typeof(CASAgeGenderFlags), profilePropertyNode.InnerText);
                                        }
                                        catch (ArgumentException)
                                        {
                                        }
                                        continue;
                                    }
                                    if (profilePropertyNode.Name == "ServiceProfileFlags")
                                    {
                                        try
                                        {
                                            profile.SetFlags((ServiceProfileFlags)Enum.Parse(typeof(ServiceProfileFlags), profilePropertyNode.InnerText));
                                        }
                                        catch (ArgumentException)
                                        {
                                        }
                                        continue;
                                    }
                                    if (profilePropertyNode.Name == "TimeToStartWorking")
                                    {
                                        profile.TimeToStartWorking = ParserFunctions.ParseFloat(profilePropertyNode.InnerText, profile.TimeToStartWorking);
                                        continue;
                                    }
                                    if (profilePropertyNode.Name == "TimeToStopWorking")
                                    {
                                        profile.TimeToStopWorking = ParserFunctions.ParseFloat(profilePropertyNode.InnerText, profile.TimeToStopWorking);
                                        continue;
                                    }
                                    if (profilePropertyNode.Name == "Cost")
                                    {
                                        profile.Cost = ParserFunctions.ParseInt(profilePropertyNode.InnerText, profile.Cost);
                                        continue;
                                    }
                                    if (profilePropertyNode.Name == "Motives")
                                    {
                                        foreach (XmlNode motiveNode in profilePropertyNode.ChildNodes)
                                        {
                                            if (motiveNode.Attributes["name"].Value != importedServiceMotiveName && ParserFunctions.TryParseEnum(motiveNode.Attributes["name"].Value, out var motive, CommodityKind.None))
                                            {
                                                profile.AddMotives(motive);
                                            }
                                        }
                                        continue;
                                    }
                                    if (profilePropertyNode.Name == "Traits")
                                    {
                                        foreach (XmlNode traitNode in profilePropertyNode.ChildNodes)
                                        {
                                            var traitName = traitNode.Attributes["name"].Value;
                                            profile.AddTraits(ParserFunctions.TryParseEnum(traitName, out var trait, TraitNames.Unknown) ? trait : (TraitNames)ParserFunctions.ParseUlong(traitName, 0uL));
                                        }
                                        profile.RemoveTraits(x => x == TraitNames.Unknown);
                                        continue;
                                    }
                                    if (profilePropertyNode.Name == "PotentialTraits")
                                    {
                                        profile.PotentialTraitCount = ParserFunctions.ParseInt(profilePropertyElement.GetAttribute("count"), profile.PotentialTraitCount);
                                        foreach (XmlNode traitNode in profilePropertyNode.ChildNodes)
                                        {
                                            var traitName = traitNode.Attributes["name"].Value;
                                            profile.AddPotentialTraits(ParserFunctions.TryParseEnum(traitName, out var trait, TraitNames.Unknown) ? trait : (TraitNames)ParserFunctions.ParseUlong(traitName, 0uL));
                                        }
                                        profile.RemovePotentialTraits(x => x == TraitNames.Unknown);
                                        continue;
                                    }
                                    if (profilePropertyNode.Name == "HiddenTraits")
                                    {
                                        foreach (XmlNode traitNode in profilePropertyNode.ChildNodes)
                                        {
                                            var traitName = traitNode.Attributes["name"].Value;
                                            profile.AddHiddenTraits(ParserFunctions.TryParseEnum(traitName, out var trait, TraitNames.Unknown) ? trait : (TraitNames)ParserFunctions.ParseUlong(traitName, 0uL));
                                        }
                                        profile.RemoveHiddenTraits(x => x == TraitNames.Unknown);
                                        continue;
                                    }
                                    if (profilePropertyNode.Name == "Skills")
                                    {
                                        foreach (XmlNode skillNode in profilePropertyNode.ChildNodes)
                                        {
                                            var skillName = skillNode.Attributes["name"].Value;
                                            profile.AddSkills(new SkillLevelPair(ParserFunctions.TryParseEnum(skillName, out var skill, SkillNames.None) ? skill : (SkillNames)ParserFunctions.ParseUlong(skillName, 0uL), ParserFunctions.ParseInt((skillNode as XmlElement)?.GetAttribute("level"), -1)));
                                        }
                                        profile.RemoveSkills(x => x.SkillName == SkillNames.None);
                                        continue;
                                    }
                                    if (profilePropertyNode.Name == "Actions")
                                    {
                                        foreach (XmlNode actionNode in profilePropertyNode.ChildNodes)
                                        {
                                            bool isActive;
                                            switch (actionNode.Name)
                                            {
                                                case "FPA":
                                                    isActive = true;
                                                    break;
                                                case "SPA":
                                                    isActive = false;
                                                    break;
                                                default:
                                                    continue;
                                            }
                                            profile.AddActions(new ServiceUtils.ActiveTopicAction(actionNode.Attributes["name"].Value, ParserFunctions.TryParseEnum((actionNode as XmlElement)?.GetAttribute("group"), out var grouping, LongTermRelationshipTypes.Undefined) ? grouping : LongTermRelationshipTypes.Default, isActive));
                                        }
                                        continue;
                                    }
                                    if (profilePropertyNode.Name == "Outputs")
                                    {
                                        foreach (XmlNode outputNode in profilePropertyNode.ChildNodes)
                                        {
                                            if (outputNode is XmlElement outputElement)
                                            {
                                                profile.AddOutputs(new ServiceUtils.CommodityChange(outputElement.GetAttribute("interactionDefinitionType"), outputElement.GetAttribute("targetType"), ParserFunctions.ParseFloat(outputElement.GetAttribute("advertised"), 200f), ParserFunctions.ParseBool("locked"), ParserFunctions.ParseFloat(outputElement.GetAttribute("actual"), 200f), ParserFunctions.TryParseEnum(outputElement.GetAttribute("updateType"), out var updateType, OutputUpdateType.ContinuousFlow) ? updateType : OutputUpdateType.ContinuousFlow, ParserFunctions.ParseBool(outputElement.GetAttribute("timeDependsOnCommodityFilling")), ParserFunctions.ParseBool(outputElement.GetAttribute("updateEvenOnFailure")), ParserFunctions.TryParseEnum(outputElement.GetAttribute("updateAboveAndBelowZero"), out var updateAboveAndBelowZero, UpdateAboveAndBelowZeroType.Either) ? updateAboveAndBelowZero : UpdateAboveAndBelowZeroType.Either));
                                            }
                                        }
                                        continue;
                                    }
                                    if (profilePropertyNode.Name == "Inventory")
                                    {
                                        foreach (XmlNode inventoryObjectNode in profilePropertyNode.ChildNodes)
                                        {
                                            var instanceName = inventoryObjectNode.Attributes["instanceName"].Value;
                                            profile.Inventory.Add(new InventoryObjectCreationParameters(instanceName, ulong.Parse((inventoryObjectNode as XmlElement)?.GetAttribute("instance") ?? NameGuidMap.GetGuidByName(instanceName).ToString("X16"), NumberStyles.HexNumber), uint.Parse(inventoryObjectNode.Attributes["group"].Value, NumberStyles.HexNumber), int.Parse(inventoryObjectNode.Attributes["count"].Value), WrapInnerXmlAsDocument(profilePropertyNode)));
                                        }
                                        continue;
                                    }
                                    if (profilePropertyNode.Name == "Car")
                                    {
                                        profile.CarInstanceName = profilePropertyElement.GetAttribute("instanceName");
                                        profile.CarInstanceId = ulong.Parse(profilePropertyElement.GetAttribute("instance") ?? NameGuidMap.GetGuidByName(profile.CarInstanceName).ToString("X16"), NumberStyles.HexNumber);
                                        profile.CarGroupId = uint.Parse(profilePropertyElement.GetAttribute("group"), NumberStyles.HexNumber);
                                        profile.CarPreset = WrapInnerXmlAsDocument(profilePropertyNode);
                                        continue;
                                    }
                                    if (profilePropertyNode.Name == "CheckTime")
                                    {
                                        profile.CheckTime = ParserFunctions.ParseFloat(profilePropertyNode.InnerText, profile.CheckTime);
                                        continue;
                                    }
                                    if (profilePropertyNode.Name == "DriveTime")
                                    {
                                        profile.DriveTime = ParserFunctions.ParseFloat(profilePropertyNode.InnerText, profile.DriveTime);
                                        continue;
                                    }
                                    if (profilePropertyNode.Name == "MaxNumNPCsInPool")
                                    {
                                        profile.MaxNumNPCsInPool = ParserFunctions.ParseInt(profilePropertyNode.InnerText, profile.MaxNumNPCsInPool);
                                        continue;
                                    }
                                    if (profilePropertyNode.Name == "RelationshipLevelForQuit")
                                    {
                                        profile.RelationshipLevelForQuit = ParserFunctions.ParseFloat(profilePropertyNode.InnerText, profile.RelationshipLevelForQuit);
                                        continue;
                                    }
                                    if (profilePropertyNode.Name == "TimeWaitBeforePutawayLeftovers")
                                    {
                                        profile.TimeWaitBeforePutawayLeftovers = ParserFunctions.ParseFloat(profilePropertyNode.InnerText, profile.TimeWaitBeforePutawayLeftovers);
                                        continue;
                                    }
                                    if (profilePropertyNode.Name == "UseObjectInSameRoomAsSleeperMultiplier")
                                    {
                                        profile.UseObjectInSameRoomAsSleeperMultiplier = ParserFunctions.ParseFloat(profilePropertyNode.InnerText, profile.UseObjectInSameRoomAsSleeperMultiplier);
                                    }
                                }
                                profile.FixUp();
                                if (profile.CanAddServiceToSaveGame())
                                {
                                    ServiceUtils.ServiceProfiles.Add(profile);
                                }
                            }
                        }
                        foreach (XmlNode node in xmlDocument.DocumentElement.ChildNodes)
                        {
                            if (node.Name == "ServiceUniform" && node is XmlElement)
                            {
                                var assignedOutfit = new OutfitAssignmentUtils.AssignedOutfit();
                                assignedOutfit.PartOverrides.Clear();
                                var serviceName = node.Attributes["profile"].Value;
                                var profileIndex = ServiceUtils.ServiceProfiles.FindIndex(x => x.Name == serviceName);
                                if (profileIndex == -1)
                                {
                                    continue;
                                }
                                foreach (XmlNode serviceUniformPropertyNode in node.ChildNodes)
                                {
                                    if (serviceUniformPropertyNode.Name == "Parts")
                                    {
                                        foreach (XmlNode partNode in serviceUniformPropertyNode.ChildNodes)
                                        {
                                            assignedOutfit.Parts.Add(new OutfitAssignmentUtils.AssignedOutfit.SavedPart(new CASPart(ResourceKey.FromString(partNode.Attributes["key"].Value)), WrapInnerXmlAsDocument(partNode)));
                                        }
                                    }
                                    if (serviceUniformPropertyNode.Name == "PartOverrides")
                                    {
                                        foreach (XmlNode partOverrideNode in serviceUniformPropertyNode.ChildNodes)
                                        {
                                            if (ParserFunctions.TryParseEnum(partOverrideNode.Attributes["name"].Value, out var partType, BodyTypes.None))
                                            {
                                                assignedOutfit.PartOverrides.Add(partType);
                                            }
                                        }
                                    }
                                }
                                var specialOutfitKey = ((ParserFunctions.TryParseEnum(node.Attributes["age"].Value, out var age, CASAgeGenderFlags.None) ? age : CASAgeGenderFlags.None) | (ParserFunctions.TryParseEnum(node.Attributes["gender"].Value, out var gender, CASAgeGenderFlags.None) ? gender : CASAgeGenderFlags.None)).GetGlobalAssignedOutfitPrefix() + serviceName;
                                OutfitAssignmentUtils.OutfitAssignments.Add(new OutfitAssignmentUtils.OutfitAssignment(null, specialOutfitKey, ServiceUtils.ServiceProfiles[profileIndex]));
                                OutfitAssignmentUtils.IndexOutfitAssignments();
                                OutfitAssignmentUtils.AssignedOutfits[specialOutfitKey] = assignedOutfit;
                            }
                        }
                        foreach (XmlNode node in xmlDocument.DocumentElement.ChildNodes)
                        {
                            if (node.Name == "ServiceUniformHairstyles" && node is XmlElement)
                            {
                                var specialOutfitKey = node.Attributes["uniformKey"].Value;
                                if (!OutfitAssignmentUtils.AssignedOutfits.ContainsKey(specialOutfitKey))
                                {
                                    continue;
                                }
                                SimHairGrowth.PreferredHairstylesBySpecialOutfitAndGrowthState[specialOutfitKey] = new SimHairGrowth.OutfitHairData(0uL, ResourceKey.kInvalidResourceKey, OutfitCategories.None, 0);
                                foreach (XmlNode hairstyleNode in node.ChildNodes)
                                {
                                    HairGrowthStates hairGrowthState;
                                    try
                                    {
                                        hairGrowthState = (HairGrowthStates)Enum.Parse(typeof(HairGrowthStates), node.Attributes["growthState"].Value);
                                    }
                                    catch (ArgumentException)
                                    {
                                        continue;
                                    }
                                    SimHairGrowth.PreferredHairstylesBySpecialOutfitAndGrowthState[specialOutfitKey].PreferredHairstylesByGrowthState[hairGrowthState] = new CASPart(ResourceKey.FromString(node.Attributes["partKey"].Value));
                                    SimHairGrowth.PreferredHairstylesBySpecialOutfitAndGrowthState[specialOutfitKey].PreferredHatPresetsByGrowthState[hairGrowthState] = WrapInnerXmlAsDocument(hairstyleNode);
                                }
                            }
                        }
                    }
                }
            }
            
            public override string ToXml(IEnumerable<IServiceProfile> profiles)
            {
                var stringBuilder = new StringBuilder();
                using (var stringWriter = new Utf8StringWriter(stringBuilder))
                {
                    using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
                        {
                            Encoding = Encoding.UTF8
                        }))
                    {
                        xmlWriter.WriteStartDocument();
                        xmlWriter.WriteStartElement("ExpandedHouseholdStaffData");
                        foreach (ServiceUtils.ServiceProfile profile in profiles)
                        {
                            xmlWriter.WriteStartElement("ServiceProfile");
                            xmlWriter.WriteAttributeString("version", profile.VersionString);
                            xmlWriter.WriteAttributeString("name", profile.Name);
                            foreach (var entry in ParserFunctions.sCaseSensitiveEnumParsers[typeof(CommodityKind)].mLookup)
                            {
                                if ((CommodityKind)entry.Value == profile.ServiceMotive)
                                {
                                    xmlWriter.WriteAttributeString("workMotive", entry.Key);
                                    break;
                                }
                            }

                            xmlWriter.WriteStartElement("Title");
                            xmlWriter.WriteAttributeString("localize", false.ToString());
                            xmlWriter.WriteString(profile.Title);
                            xmlWriter.WriteEndElement();

                            xmlWriter.WriteStartElement("RequestedMessage");
                            xmlWriter.WriteAttributeString("localize", false.ToString());
                            xmlWriter.WriteString(profile.RequestedMessage);
                            xmlWriter.WriteEndElement();

                            xmlWriter.WriteStartElement("CancelledMessage");
                            xmlWriter.WriteAttributeString("localize", false.ToString());
                            xmlWriter.WriteString(profile.CancelledMessage);
                            xmlWriter.WriteEndElement();

                            xmlWriter.WriteStartElement("CancelledWhileActiveMessage");
                            xmlWriter.WriteAttributeString("localize", false.ToString());
                            xmlWriter.WriteString(profile.CancelledWhileActiveMessage);
                            xmlWriter.WriteEndElement();

                            xmlWriter.WriteElementString("DelayBeforeArriving", profile.DelayBeforeArriving.ToString(CultureInfo.InvariantCulture));

                            xmlWriter.WriteElementString("DelayBeforeLeaving", profile.DelayBeforeLeaving.ToString(CultureInfo.InvariantCulture));

                            xmlWriter.WriteElementString("ExtraWaitTimeAfterSocializing", profile.ExtraWaitTimeAfterSocializing.ToString(CultureInfo.InvariantCulture));

                            xmlWriter.WriteElementString("ValidAges", profile.ValidAges.ToString());

                            xmlWriter.WriteElementString("ValidGenders", profile.ValidGenders.ToString());

                            xmlWriter.WriteElementString("ServiceProfileFlags", profile.GetFlags().ToString());

                            xmlWriter.WriteElementString("TimeToStartWorking", profile.TimeToStartWorking.ToString(CultureInfo.InvariantCulture));

                            xmlWriter.WriteElementString("TimeToStopWorking", profile.TimeToStopWorking.ToString(CultureInfo.InvariantCulture));

                            xmlWriter.WriteElementString("Cost", profile.Cost.ToString());

                            xmlWriter.WriteStartElement("Motives");
                            foreach (var entry in ParserFunctions.sCaseSensitiveEnumParsers[typeof(CommodityKind)].mLookup)
                            {
                                if (profile.Motives.Contains((CommodityKind)entry.Value))
                                {
                                    xmlWriter.WriteStartElement("Motive");
                                    xmlWriter.WriteAttributeString("name", entry.Key);
                                    xmlWriter.WriteEndElement();
                                }
                            }
                            xmlWriter.WriteFullEndElement();

                            xmlWriter.WriteStartElement("Traits");
                            foreach (var trait in profile.Traits)
                            {
                                xmlWriter.WriteStartElement("Trait");
                                xmlWriter.WriteAttributeString("name", trait.ToString());
                                xmlWriter.WriteEndElement();
                            }
                            xmlWriter.WriteFullEndElement();

                            xmlWriter.WriteStartElement("PotentialTraits");
                            xmlWriter.WriteAttributeString("count", profile.PotentialTraitCount.ToString());
                            foreach (var trait in profile.PotentialTraits)
                            {
                                xmlWriter.WriteStartElement("Trait");
                                xmlWriter.WriteAttributeString("name", trait.ToString());
                                xmlWriter.WriteEndElement();
                            }
                            xmlWriter.WriteFullEndElement();

                            xmlWriter.WriteStartElement("HiddenTraits");
                            foreach (var trait in profile.HiddenTraits)
                            {
                                xmlWriter.WriteStartElement("Trait");
                                xmlWriter.WriteAttributeString("name", trait.ToString());
                                xmlWriter.WriteEndElement();
                            }
                            xmlWriter.WriteFullEndElement();

                            xmlWriter.WriteStartElement("Skills");
                            foreach (var skill in profile.Skills)
                            {
                                xmlWriter.WriteStartElement("Skill");
                                xmlWriter.WriteAttributeString("name", skill.SkillName.ToString());
                                xmlWriter.WriteAttributeString("level", skill.SkillLevel.ToString());
                                xmlWriter.WriteEndElement();
                            }
                            xmlWriter.WriteFullEndElement();

                            xmlWriter.WriteStartElement("Actions");
                            foreach (var action in profile.Actions)
                            {
                                xmlWriter.WriteStartElement(action.IsActive ? "FPA" : "SPA");
                                xmlWriter.WriteAttributeString("group", action.Grouping.ToString());
                                xmlWriter.WriteAttributeString("name", action.Name);
                                xmlWriter.WriteEndElement();
                            }
                            xmlWriter.WriteFullEndElement();

                            xmlWriter.WriteStartElement("Outputs");
                            foreach (var output in profile.Outputs)
                            {
                                xmlWriter.WriteStartElement("CommodityChange");
                                xmlWriter.WriteAttributeString("interactionDefinitionType", output.InteractionDefinitionType);
                                xmlWriter.WriteAttributeString("targetType", output.TargetType);
                                xmlWriter.WriteAttributeString("advertised", output.ConstantChange.ToString(CultureInfo.InvariantCulture));
                                xmlWriter.WriteAttributeString("locked", output.Locked.ToString());
                                xmlWriter.WriteAttributeString("actual", output.ActualValue.ToString(CultureInfo.InvariantCulture));
                                xmlWriter.WriteAttributeString("updateType", output.UpdateType == OutputUpdateType.ContinuousFlow ? "ContinuousFlow" : output.UpdateType.ToString());
                                xmlWriter.WriteAttributeString("timeDependsOnCommodityFilling", output.TimeDependsOnCommodityFilling.ToString());
                                xmlWriter.WriteAttributeString("updateEvenOnFailure", output.UpdateEvenOnFailure.ToString());
                                xmlWriter.WriteAttributeString("updateAboveAndBelowZero", output.UpdateAboveAndBelowZero.ToString());
                                xmlWriter.WriteEndElement();
                            }
                            xmlWriter.WriteFullEndElement();

                            xmlWriter.WriteStartElement("Inventory");
                            foreach (var inventoryObjectCreationParameters in profile.Inventory)
                            {
                                xmlWriter.WriteStartElement("InventoryObject");
                                xmlWriter.WriteAttributeString("instanceName", inventoryObjectCreationParameters.InstanceName);
                                xmlWriter.WriteAttributeString("instance", inventoryObjectCreationParameters.InstanceId.ToString("X16"));
                                xmlWriter.WriteAttributeString("group", inventoryObjectCreationParameters.GroupId.ToString("X8"));
                                xmlWriter.WriteAttributeString("count", inventoryObjectCreationParameters.Count.ToString());
                                xmlWriter.WriteRaw(inventoryObjectCreationParameters.Preset?.Substring(inventoryObjectCreationParameters.Preset.IndexOf("\n", StringComparison.Ordinal)) ?? "");
                                xmlWriter.WriteEndElement();
                            }
                            xmlWriter.WriteFullEndElement();

                            xmlWriter.WriteStartElement("Car");
                            xmlWriter.WriteAttributeString("instanceName", profile.CarInstanceName ?? "");
                            xmlWriter.WriteAttributeString("instance", profile.CarInstanceId.ToString("X16"));
                            xmlWriter.WriteAttributeString("group", profile.CarGroupId.ToString("X8"));
                            xmlWriter.WriteRaw(profile.CarPreset?.Substring(profile.CarPreset.IndexOf("\n", StringComparison.Ordinal)) ?? "");
                            xmlWriter.WriteEndElement();

                            xmlWriter.WriteElementString("CheckTime", profile.CheckTime.ToString(CultureInfo.InvariantCulture));

                            xmlWriter.WriteElementString("DriveTime", profile.DriveTime.ToString(CultureInfo.InvariantCulture));

                            xmlWriter.WriteElementString("MaxNumNPCsInPool", profile.MaxNumNPCsInPool.ToString());

                            xmlWriter.WriteElementString("RelationshipLevelForQuit", profile.RelationshipLevelForQuit.ToString(CultureInfo.InvariantCulture));

                            xmlWriter.WriteElementString("TimeWaitBeforePutawayLeftovers", profile.TimeWaitBeforePutawayLeftovers.ToString(CultureInfo.InvariantCulture));

                            xmlWriter.WriteElementString("UseObjectInSameRoomAsSleeperMultiplier", profile.UseObjectInSameRoomAsSleeperMultiplier.ToString(CultureInfo.InvariantCulture));

                            xmlWriter.WriteEndElement(); // Ends writing the "ServiceProfile" element.
                        }
                        foreach (var outfitAssignment in OutfitAssignmentUtils.OutfitAssignments)
                        {
                            if (!new List<IServiceProfile>(profiles).Exists(x => x.Name == outfitAssignment.ServiceName) || outfitAssignment.SimDescription != null || !OutfitAssignmentUtils.AssignedOutfits.TryGetValue(outfitAssignment.SpecialOutfitKey, out var assignedOutfit))
                            {
                                continue;
                            }
                            var ageGenderPrefix = outfitAssignment.SpecialOutfitKey.Substring(OutfitAssignmentUtils.OutfitAssignmentGlobalPrefix.Length, 2);
                            var age = CASAgeGenderFlags.Adult;
                            switch (ageGenderPrefix[0])
                            {
                                case 'a':
                                    age = CASAgeGenderFlags.Adult;
                                    break;
                                case 'b':
                                    age = CASAgeGenderFlags.Baby;
                                    break;
                                case 'c':
                                    age = CASAgeGenderFlags.Child;
                                    break;
                                case 'e':
                                    age = CASAgeGenderFlags.Elder;
                                    break;
                                case 'p':
                                    age = CASAgeGenderFlags.Toddler;
                                    break;
                                case 't':
                                    age = CASAgeGenderFlags.Teen;
                                    break;
                                case 'y':
                                    age = CASAgeGenderFlags.YoungAdult;
                                    break;
                            }
                            var gender = CASAgeGenderFlags.Male;
                            switch (ageGenderPrefix[1])
                            {
                                case 'f':
                                    gender = CASAgeGenderFlags.Female;
                                    break;
                                case 'm':
                                    gender = CASAgeGenderFlags.Male;
                                    break;
                            }
                            xmlWriter.WriteStartElement("ServiceUniform");
                            xmlWriter.WriteAttributeString("profile", outfitAssignment.ServiceName);
                            xmlWriter.WriteAttributeString("age", age.ToString());
                            xmlWriter.WriteAttributeString("gender", gender.ToString());

                            xmlWriter.WriteStartElement("Parts");
                            foreach (var part in assignedOutfit.Parts)
                            {
                                xmlWriter.WriteStartElement("Part");
                                xmlWriter.WriteAttributeString("key", part.Part.Key.ToString().ToUpperInvariant());
                                xmlWriter.WriteRaw(part.Preset?.Substring(part.Preset.IndexOf("\n", StringComparison.Ordinal)) ?? "");
                                xmlWriter.WriteEndElement();
                            }
                            xmlWriter.WriteEndElement();

                            xmlWriter.WriteStartElement("PartOverrides");
                            foreach (var partType in assignedOutfit.PartOverrides)
                            {
                                xmlWriter.WriteStartElement("PartType");
                                xmlWriter.WriteAttributeString("name", partType.ToString());
                                xmlWriter.WriteEndElement();
                            }
                            xmlWriter.WriteEndElement();

                            xmlWriter.WriteEndElement(); // Ends writing the "ServiceUniform" element.
                        }
                        foreach (var entry in SimHairGrowth.PreferredHairstylesBySpecialOutfitAndGrowthState)
                        {
                            var outfitAssignmentIndex = OutfitAssignmentUtils.OutfitAssignments.FindIndex(x => x.SpecialOutfitKey == entry.Key);
                            if (outfitAssignmentIndex == -1 || !new List<IServiceProfile>(profiles).Exists(x => x.Name == OutfitAssignmentUtils.OutfitAssignments[outfitAssignmentIndex].ServiceName) || OutfitAssignmentUtils.OutfitAssignments[outfitAssignmentIndex].SimDescription != null)
                            {
                                continue;
                            }
                            xmlWriter.WriteStartElement("ServiceUniformHairstyles");
                            xmlWriter.WriteAttributeString("uniformKey", entry.Key);
                            foreach (var hairGrowthState in entry.Value.PreferredHairstylesByGrowthState.Keys)
                            {
                                xmlWriter.WriteStartElement("Hairstyle");
                                xmlWriter.WriteAttributeString("partKey", entry.Value.PreferredHairstylesByGrowthState[hairGrowthState].Key.ToString().ToUpperInvariant());
                                xmlWriter.WriteAttributeString("growthState", hairGrowthState.ToString());
                                xmlWriter.WriteRaw(entry.Value.PreferredHatPresetsByGrowthState[hairGrowthState].Substring(entry.Value.PreferredHatPresetsByGrowthState[hairGrowthState].IndexOf("\n", StringComparison.Ordinal)) ?? "");
                                xmlWriter.WriteEndElement();
                            }
                            xmlWriter.WriteEndElement();
                        }
                        xmlWriter.WriteEndElement();
                        xmlWriter.WriteEndDocument();
                        xmlWriter.Flush();
                    }
                    stringWriter.Flush();
                }
                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(stringBuilder.ToString());
                stringBuilder.Remove(0, stringBuilder.Length);
                using (var stringWriter = new Utf8StringWriter(stringBuilder))
                {
                    using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
                        {
                            Encoding = Encoding.UTF8,
                            Indent = true,
                            IndentChars = "  ",
                            OmitXmlDeclaration = false
                        }))
                    {
                        xmlDocument.WriteTo(xmlWriter);
                        xmlWriter.Flush();
                    }
                    stringWriter.Flush();
                }
                return stringBuilder.ToString();
            }
        }

        public static bool AddAssignedOutfit(this SimDescription simDescription, OutfitAssignmentUtils.AssignedOutfit assignedOutfit, string specialOutfitKey)
        {
            if (simDescription.HasSpecialOutfit(specialOutfitKey))
            {
                simDescription.RemoveSpecialOutfit(specialOutfitKey);
            }
            using (var simBuilder = new SimBuilder())
            {
                simBuilder.UseCompression = true;
                simBuilder.PrepareForOutfit(simDescription.CreatedSim == null ? simDescription.GetOutfit(OutfitCategories.Everyday, 0) : simDescription.CreatedSim.CurrentOutfit);
                foreach (var bodyType in OutfitAssignmentUtils.OverridableBodyTypes)
                {
                    if (!assignedOutfit.PartOverrides.Contains(bodyType))
                    {
                        continue;
                    }
                    var savedPartIndex = assignedOutfit.Parts.FindIndex(x => x.Part.BodyType == bodyType);
                    if (savedPartIndex == -1)
                    {
                        simBuilder.RemoveParts(bodyType);
                        continue;
                    }
                    var savedPart = assignedOutfit.Parts[savedPartIndex];
                    switch (bodyType)
                    {
                        case BodyTypes.Hair:
                            var hairGrowthState = simDescription.GetHairGrowthState();
                            if (savedPart.Part.TryGetHairGrowthState(out var hairGrowthStates) && (hairGrowthStates & hairGrowthState) == HairGrowthStates.Unknown)
                            {
                                var assignedSpecialOutfitKey = "";
                                foreach (var entry in OutfitAssignmentUtils.AssignedOutfits)
                                {
                                    if (entry.Value == assignedOutfit)
                                    {
                                        assignedSpecialOutfitKey = entry.Key;
                                        break;
                                    }
                                }
                                if (string.IsNullOrEmpty(assignedSpecialOutfitKey) || !SimHairGrowth.PreferredHairstylesBySpecialOutfitAndGrowthState.TryGetValue(assignedSpecialOutfitKey, out var outfitHairData) || !outfitHairData.PreferredHairstylesByGrowthState.TryGetValue(hairGrowthState, out var hairstyle))
                                {
                                    continue;
                                }
                                savedPart = new OutfitAssignmentUtils.AssignedOutfit.SavedPart(hairstyle, outfitHairData.PreferredHatPresetsByGrowthState[hairGrowthState]);
                            }
                            break;
                        case BodyTypes.FullBody:
                            simBuilder.RemoveParts(BodyTypes.LowerBody, BodyTypes.UpperBody);
                            break;
                        case BodyTypes.LowerBody:
                        case BodyTypes.UpperBody:
                            simBuilder.RemoveParts(BodyTypes.FullBody);
                            break;
                    }
                    simBuilder.RemoveParts(bodyType);
                    simBuilder.AddPart(savedPart.Part);
                    if (!string.IsNullOrEmpty(savedPart.Preset) && CASUtils.ApplyPresetToPart(simBuilder, savedPart.Part, savedPart.Preset))
                    {
                        simBuilder.SetPartPreset(savedPart.Part.Key, null, savedPart.Preset);
                    }
                    OutfitUtils.AdjustPresetForHairColor(simBuilder, savedPart.Part, simDescription);
                }
                return simDescription.AddSpecialOutfit(new SimOutfit(simBuilder.CacheOutfit(specialOutfitKey + "_" + simDescription.SimDescriptionId)), specialOutfitKey) > -1;
            }
        }

        public static void Init()
        {
            CommonUtils.ReplaceMethod(typeof(OutfitAssignmentUtils).GetMethod(nameof(AddAssignedOutfit), new[]
                {
                    typeof(SimDescription),
                    typeof(OutfitAssignmentUtils.AssignedOutfit),
                    typeof(string)
                }), typeof(ExpandedHouseholdStaffIntegration).GetMethod(nameof(AddAssignedOutfit)));
            FileUtils_Original.Singleton = new FileUtils();
            LoadSaveManager.ObjectGroupsPreLoad += () => EditServiceUniform_Original.Singleton = EditServiceUniform.Singleton;
            ServiceUtils.ServiceProfileRemoved += () =>
                {
                    foreach (var specialOutfitKey in new List<string>(SimHairGrowth.PreferredHairstylesBySpecialOutfitAndGrowthState.Keys))
                    {
                        if (!OutfitAssignmentUtils.OutfitAssignments.Exists(x => x.SpecialOutfitKey == specialOutfitKey))
                        {
                            SimHairGrowth.PreferredHairstylesBySpecialOutfitAndGrowthState.Remove(specialOutfitKey);
                        }
                    }
                };
        }
    }
}
