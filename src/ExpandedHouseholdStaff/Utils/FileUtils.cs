using Sims3.Gameplay;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Interfaces.Destrospean.ExpandedHouseholdStaff;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Utilities;
using Sims3.Gameplay.Destrospean.Utils;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.SimIFace.CustomContent;
using Sims3.UI;
using Sims3.UI.Controller;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using Destrospean.Enums;
using Destrospean.Enums.ExpandedHouseholdStaff;
using Destrospean.ExpandedHouseholdStaff.Interactions;
using Destrospean.Misc;
using Destrospean.UI.Columns;
using ObjectPickerDialog = Destrospean.UI.Dialogs.ObjectPickerDialog;

namespace Destrospean.Utils.ExpandedHouseholdStaff
{
    public class FileUtils
    {
        const string kLocalizationKey = "Destrospean/UI/Dialogs";

        const string kSavedSettingsPrefix = "Destrospean.ExpandedHouseholdStaff.";

        public static void DeleteFile()
        {
            BinModel.Singleton.PopulateExportBin();
            Dictionary<string, ulong> savedSettings = new Dictionary<string, ulong>();
            foreach (ExportBinContents contents in BinModel.Singleton.ExportBinContents)
            {
                if (contents.HouseholdName != null && contents.HouseholdName.Contains(kSavedSettingsPrefix))
                {
                    savedSettings[contents.HouseholdName] = contents.ContentId;
                }
            }
            if (savedSettings.Count == 0)
            {
                SimpleMessageDialog.Show(Localization.LocalizeString(kLocalizationKey + "/NoServiceCollectionsExistDialog:Title"), Localization.LocalizeString(kLocalizationKey + "/NoServiceCollectionsExistDialog:Message"));
                return;
            }
            string[] savedSettingsNames;
            if (TryUIGetSelectedSavedSettingsNames(out savedSettingsNames, new List<string>(savedSettings.Keys).ConvertAll(x => x.Replace(kSavedSettingsPrefix, "")).ToArray(), Localization.LocalizeString(DeleteServiceCollection.LocalizationKey + ":Name")))
            {
                foreach (string name in savedSettingsNames)
                {
                    BinModel.Singleton.DeleteFromExportBin(savedSettings[kSavedSettingsPrefix + name]);
                }
                SimpleMessageDialog.Show(Localization.LocalizeString(kLocalizationKey + "/DeleteServiceCollectionSuccessDialog:Title"), Localization.LocalizeString(kLocalizationKey + "/DeleteServiceCollectionSuccessDialog:Message"));
            }
        }

        public static bool ExportToFile(string text)
        {
            string name = null;
            bool found = true;
            while (found)
            {
                name = StringInputDialog.Show(Localization.LocalizeString(kLocalizationKey + "/ExportServiceCollectionFilenameDialog:Title"), Localization.LocalizeString(kLocalizationKey + "/ExportServiceCollectionFilenameDialog:Prompt"), "");
                if (string.IsNullOrEmpty(name))
                {
                    return false;
                }
                name = kSavedSettingsPrefix + name;
                BinModel.Singleton.PopulateExportBin();
                found = false;
                foreach (ExportBinContents contents in BinModel.Singleton.ExportBinContents)
                {
                    if (contents.HouseholdName == null || !contents.HouseholdName.Contains(kSavedSettingsPrefix))
                    {
                        continue;
                    }
                    if (contents.HouseholdName == name)
                    {
                        SimpleMessageDialog.Show(Localization.LocalizeString(kLocalizationKey + "/ServiceCollectionExistsDialog:Title"), Localization.LocalizeString(kLocalizationKey + "/ServiceCollectionExistsDialog:Message"));
                        found = true;
                        break;
                    }
                }
            }
            Household dummyHousehold = Household.Create();
            dummyHousehold.SetName(name);
            dummyHousehold.BioText = text;
            BinModel.Singleton.AddToExportBin(dummyHousehold);
            dummyHousehold.Destroy();
            SimpleMessageDialog.Show(Localization.LocalizeString(kLocalizationKey + "/ExportServiceCollectionSuccessDialog:Title"), Localization.LocalizeString(kLocalizationKey + "/ExportServiceCollectionSuccessDialog:Message"));
            return true;
        }

        public static string ExtractFromFile()
        {
            BinModel.Singleton.PopulateExportBin();
            Dictionary<string, string> savedSettings = new Dictionary<string, string>();
            foreach (ExportBinContents contents in BinModel.Singleton.ExportBinContents)
            {
                if (contents.HouseholdName != null && contents.HouseholdName.Contains(kSavedSettingsPrefix))
                {
                    savedSettings[contents.HouseholdName] = contents.HouseholdBio;
                }
            }
            if (savedSettings.Count == 0)
            {
                SimpleMessageDialog.Show(Localization.LocalizeString(kLocalizationKey + "/NoServiceCollectionsExistDialog:Title"), Localization.LocalizeString(kLocalizationKey + "/NoServiceCollectionsExistDialog:Message"));
                return null;
            }
            string[] savedSettingsNames;
            return TryUIGetSelectedSavedSettingsNames(out savedSettingsNames, new List<string>(savedSettings.Keys).ConvertAll(x => x.Replace(kSavedSettingsPrefix, "")).ToArray(), Localization.LocalizeString(ImportServiceCollection.LocalizationKey + ":Name"), 1) ? savedSettings[kSavedSettingsPrefix + savedSettingsNames[0]] : null;
        }

        public static string ExtractFromTuning(string name)
        {
            XmlDocument xmlDocument = Simulator.LoadXML(name);
            if (xmlDocument == null)
            {
                return null; 
            }
            StringBuilder stringBuilder = new StringBuilder();
            using (Utf8StringWriter stringWriter = new Utf8StringWriter(stringBuilder))
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
                    {
                        Encoding = Encoding.UTF8,
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

        public static string GetXml(IEnumerable<IServiceProfile> profiles)
        {
            StringBuilder stringBuilder = new StringBuilder();
            using (Utf8StringWriter stringWriter = new Utf8StringWriter(stringBuilder))
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
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
                        foreach (KeyValuePair<string, object> entry in ParserFunctions.sCaseSensitiveEnumParsers[typeof(CommodityKind)].mLookup)
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

                        xmlWriter.WriteElementString("DelayBeforeArriving", profile.DelayBeforeArriving.ToString());

                        xmlWriter.WriteElementString("DelayBeforeLeaving", profile.DelayBeforeLeaving.ToString());

                        xmlWriter.WriteElementString("ExtraWaitTimeAfterSocializing", profile.ExtraWaitTimeAfterSocializing.ToString());

                        xmlWriter.WriteElementString("ValidAges", profile.ValidAges.ToString());

                        xmlWriter.WriteElementString("ValidGenders", profile.ValidGenders.ToString());

                        xmlWriter.WriteElementString("ServiceProfileFlags", profile.GetFlags().ToString());

                        xmlWriter.WriteElementString("TimeToSpendWorking", profile.TimeToSpendWorking.ToString());

                        xmlWriter.WriteElementString("Cost", profile.Cost.ToString());

                        xmlWriter.WriteStartElement("Motives");
                        foreach (KeyValuePair<string, object> entry in ParserFunctions.sCaseSensitiveEnumParsers[typeof(CommodityKind)].mLookup)
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
                        foreach (TraitNames trait in profile.Traits)
                        {
                            xmlWriter.WriteStartElement("Trait");
                            xmlWriter.WriteAttributeString("name", trait.ToString());
                            xmlWriter.WriteEndElement();
                        }
                        xmlWriter.WriteFullEndElement();

                        xmlWriter.WriteStartElement("PotentialTraits");
                        xmlWriter.WriteAttributeString("count", profile.PotentialTraitCount.ToString());
                        foreach (TraitNames trait in profile.PotentialTraits)
                        {
                            xmlWriter.WriteStartElement("Trait");
                            xmlWriter.WriteAttributeString("name", trait.ToString());
                            xmlWriter.WriteEndElement();
                        }
                        xmlWriter.WriteFullEndElement();

                        xmlWriter.WriteStartElement("HiddenTraits");
                        foreach (TraitNames trait in profile.HiddenTraits)
                        {
                            xmlWriter.WriteStartElement("Trait");
                            xmlWriter.WriteAttributeString("name", trait.ToString());
                            xmlWriter.WriteEndElement();
                        }
                        xmlWriter.WriteFullEndElement();

                        xmlWriter.WriteStartElement("Skills");
                        foreach (SkillLevelPair skill in profile.Skills)
                        {
                            xmlWriter.WriteStartElement("Skill");
                            xmlWriter.WriteAttributeString("name", skill.SkillName.ToString());
                            xmlWriter.WriteAttributeString("level", skill.SkillLevel.ToString());
                            xmlWriter.WriteEndElement();
                        }
                        xmlWriter.WriteFullEndElement();

                        xmlWriter.WriteStartElement("Actions");
                        foreach (ServiceUtils.ActiveTopicAction action in profile.Actions)
                        {
                            xmlWriter.WriteStartElement(action.IsActive ? "FPA" : "SPA");
                            xmlWriter.WriteAttributeString("group", action.Grouping.ToString());
                            xmlWriter.WriteAttributeString("name", action.Name);
                            xmlWriter.WriteEndElement();
                        }
                        xmlWriter.WriteFullEndElement();

                        xmlWriter.WriteStartElement("Outputs");
                        foreach (ServiceUtils.CommodityChange output in profile.Outputs)
                        {
                            xmlWriter.WriteStartElement("CommodityChange");
                            xmlWriter.WriteAttributeString("interactionDefinitionType", output.InteractionDefinitionType);
                            xmlWriter.WriteAttributeString("targetType", output.TargetType);
                            xmlWriter.WriteAttributeString("advertised", output.ConstantChange.ToString());
                            xmlWriter.WriteAttributeString("locked", output.Locked.ToString());
                            xmlWriter.WriteAttributeString("actual", output.ActualValue.ToString());
                            xmlWriter.WriteAttributeString("updateType", output.UpdateType == OutputUpdateType.ContinuousFlow ? "ContinuousFlow" : output.UpdateType.ToString());
                            xmlWriter.WriteAttributeString("timeDependsOnCommodityFilling", output.TimeDependsOnCommodityFilling.ToString());
                            xmlWriter.WriteAttributeString("updateEvenOnFailure", output.UpdateEvenOnFailure.ToString());
                            xmlWriter.WriteAttributeString("updateAboveAndBelowZero", output.UpdateAboveAndBelowZero.ToString());
                            xmlWriter.WriteEndElement();
                        }
                        xmlWriter.WriteFullEndElement();

                        xmlWriter.WriteStartElement("Inventory");
                        foreach (InventoryObjectCreationParameters inventoryObjectCreationParameters in profile.Inventory)
                        {
                            xmlWriter.WriteStartElement("InventoryObject");
                            xmlWriter.WriteAttributeString("instanceName", inventoryObjectCreationParameters.InstanceName);
                            xmlWriter.WriteAttributeString("instance", inventoryObjectCreationParameters.InstanceId.ToString("X16"));
                            xmlWriter.WriteAttributeString("group", inventoryObjectCreationParameters.GroupId.ToString("X8"));
                            xmlWriter.WriteAttributeString("count", inventoryObjectCreationParameters.Count.ToString());
                            xmlWriter.WriteRaw(inventoryObjectCreationParameters.Preset?.Substring(inventoryObjectCreationParameters.Preset.IndexOf("\n")) ?? "");
                            xmlWriter.WriteEndElement();
                        }
                        xmlWriter.WriteFullEndElement();

                        xmlWriter.WriteStartElement("Car");
                        xmlWriter.WriteAttributeString("instanceName", profile.CarInstanceName ?? "");
                        xmlWriter.WriteAttributeString("instance", profile.CarInstanceId.ToString("X16"));
                        xmlWriter.WriteAttributeString("group", profile.CarGroupId.ToString("X8"));
                        xmlWriter.WriteRaw(profile.CarPreset?.Substring(profile.CarPreset.IndexOf("\n")) ?? "");
                        xmlWriter.WriteEndElement();

                        xmlWriter.WriteElementString("CheckTime", profile.CheckTime.ToString());

                        xmlWriter.WriteElementString("DriveTime", profile.CheckTime.ToString());

                        xmlWriter.WriteElementString("MaxNumNPCsInPool", profile.MaxNumNPCsInPool.ToString());

                        xmlWriter.WriteElementString("RelationshipLevelForQuit", profile.RelationshipLevelForQuit.ToString());

                        xmlWriter.WriteElementString("TimeWaitBeforePutawayLeftovers", profile.TimeWaitBeforePutawayLeftovers.ToString());

                        xmlWriter.WriteElementString("UseObjectInSameRoomAsSleeperMultiplier", profile.UseObjectInSameRoomAsSleeperMultiplier.ToString());

                        xmlWriter.WriteEndElement(); // Ends writing the "ServiceProfile" element.
                    }
                    foreach (OutfitAssignmentUtils.OutfitAssignment outfitAssignment in OutfitAssignmentUtils.OutfitAssignments)
                    {
                        OutfitAssignmentUtils.AssignedOutfit assignedOutfit;
                        if (!new List<IServiceProfile>(profiles).Exists(x => x.Name == outfitAssignment.ServiceName) || outfitAssignment.SimDescription != null || !OutfitAssignmentUtils.AssignedOutfits.TryGetValue(outfitAssignment.SpecialOutfitKey, out assignedOutfit))
                        {
                            continue;
                        }
                        string ageGenderPrefix = outfitAssignment.SpecialOutfitKey.Substring(OutfitAssignmentUtils.OutfitAssignmentGlobalPrefix.Length, 2);
                        CASAgeGenderFlags age = CASAgeGenderFlags.Adult;
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
                        CASAgeGenderFlags gender = CASAgeGenderFlags.Male;
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
                        foreach (OutfitAssignmentUtils.AssignedOutfit.SavedPart part in assignedOutfit.Parts)
                        {
                            xmlWriter.WriteStartElement("Part");
                            xmlWriter.WriteAttributeString("key", part.Part.Key.ToString().ToUpperInvariant());
                            xmlWriter.WriteRaw(part.Preset?.Substring(part.Preset.IndexOf("\n")) ?? "");
                            xmlWriter.WriteEndElement();
                        }
                        xmlWriter.WriteEndElement();

                        xmlWriter.WriteStartElement("PartOverrides");
                        foreach (BodyTypes partType in assignedOutfit.PartOverrides)
                        {
                            xmlWriter.WriteStartElement("PartType");
                            xmlWriter.WriteAttributeString("name", partType.ToString());
                            xmlWriter.WriteEndElement();
                        }
                        xmlWriter.WriteEndElement();

                        xmlWriter.WriteEndElement(); // Ends writing the "ServiceUniform" element.
                    }
                    xmlWriter.WriteEndElement();
                    xmlWriter.WriteEndDocument();
                    xmlWriter.Flush();
                }
                stringWriter.Flush();
            }
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(stringBuilder.ToString());
            stringBuilder.Remove(0, stringBuilder.Length);
            using (Utf8StringWriter stringWriter = new Utf8StringWriter(stringBuilder))
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
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

        public static bool ImportFromFile()
        {
            return ImportSettings(ExtractFromFile());
        }

        public static bool ImportFromTuning(string name)
        {
            return ImportSettings(ExtractFromTuning(name));
        }

        public static bool ImportSettings(string text)
        {
            if (text == null)
            {
                return false;
            }
            LoadServiceProfiles(text);
            SimpleMessageDialog.Show(Localization.LocalizeString(kLocalizationKey + "/ImportServiceCollectionSuccessDialog:Title"), Localization.LocalizeString(kLocalizationKey + "/ImportServiceCollectionSuccessDialog:Message"));
            return true;
        }

        public static void LoadServiceProfiles(string text)
        {
            using (StringReader stringReader = new StringReader(text))
            {
                using (XmlReader xmlReader = XmlReader.Create(stringReader, new XmlReaderSettings
                    {
                        IgnoreComments = true
                    }))
                {
                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.Load(xmlReader);
                    foreach (XmlNode node in xmlDocument.DocumentElement.ChildNodes)
                    {
                        XmlElement element = node as XmlElement;
                        if (element == null)
                        {
                            continue;
                        }
                        if (node.Name == "ServiceProfile")
                        {
                            string serviceName = element.GetAttribute("name");
                            string importedServiceMotiveName = element.GetAttribute("workMotive") ?? "Be" + serviceName;
                            ServiceUtils.ServiceProfile profile = new ServiceUtils.ServiceProfile(serviceName, null)
                                {
                                    ServiceMotive = CommonUtils.GetCommodityKind(importedServiceMotiveName, CommodityKindType.Motive),
                                    VersionString = element.GetAttribute("version")
                                };
                            string serviceMotiveName = importedServiceMotiveName;
                            while (ServiceUtils.ServiceMotiveExists(profile.ServiceMotive))
                            {
                                serviceMotiveName = importedServiceMotiveName + "_" + DownloadContent.GenerateGUID();
                                profile.ServiceMotive = CommonUtils.GetCommodityKind(serviceMotiveName, CommodityKindType.Motive);
                            }
                            CommonUtils.AddEnumValue<CommodityKind>(serviceMotiveName, profile.ServiceMotive);
                            profile.Motives = new List<CommodityKind>
                                {
                                    profile.ServiceMotive
                                };
                            foreach (XmlNode profilePropertyNode in node.ChildNodes)
                            {
                                XmlElement profilePropertyElement = profilePropertyNode as XmlElement;
                                if (profilePropertyElement == null)
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
                                if (profilePropertyNode.Name == "TimeToSpendWorking")
                                {
                                    profile.TimeToSpendWorking = ParserFunctions.ParseFloat(profilePropertyNode.InnerText, profile.TimeToSpendWorking);
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
                                        CommodityKind motive;
                                        if (motiveNode.Attributes["name"].Value != importedServiceMotiveName && ParserFunctions.TryParseEnum(motiveNode.Attributes["name"].Value, out motive, CommodityKind.None))
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
                                        string traitName = traitNode.Attributes["name"].Value;
                                        TraitNames trait;
                                        profile.AddTraits(ParserFunctions.TryParseEnum(traitName, out trait, TraitNames.Unknown) ? trait : (TraitNames)ParserFunctions.ParseUlong(traitName, 0uL));
                                    }
                                    profile.RemoveTraits(x => x == TraitNames.Unknown);
                                    continue;
                                }
                                if (profilePropertyNode.Name == "PotentialTraits")
                                {
                                    profile.PotentialTraitCount = ParserFunctions.ParseInt(profilePropertyElement.GetAttribute("count"), profile.PotentialTraitCount);
                                    foreach (XmlNode traitNode in profilePropertyNode.ChildNodes)
                                    {
                                        string traitName = traitNode.Attributes["name"].Value;
                                        TraitNames trait;
                                        profile.AddPotentialTraits(ParserFunctions.TryParseEnum(traitName, out trait, TraitNames.Unknown) ? trait : (TraitNames)ParserFunctions.ParseUlong(traitName, 0uL));
                                    }
                                    profile.RemovePotentialTraits(x => x == TraitNames.Unknown);
                                    continue;
                                }
                                if (profilePropertyNode.Name == "HiddenTraits")
                                {
                                    foreach (XmlNode traitNode in profilePropertyNode.ChildNodes)
                                    {
                                        string traitName = traitNode.Attributes["name"].Value;
                                        TraitNames trait;
                                        profile.AddHiddenTraits(ParserFunctions.TryParseEnum(traitName, out trait, TraitNames.Unknown) ? trait : (TraitNames)ParserFunctions.ParseUlong(traitName, 0uL));
                                    }
                                    profile.RemoveHiddenTraits(x => x == TraitNames.Unknown);
                                    continue;
                                }
                                if (profilePropertyNode.Name == "Skills")
                                {
                                    foreach (XmlNode skillNode in profilePropertyNode.ChildNodes)
                                    {
                                        SkillNames skill;
                                        string skillName = skillNode.Attributes["name"].Value;
                                        profile.AddSkills(new SkillLevelPair(ParserFunctions.TryParseEnum(skillName, out skill, SkillNames.None) ? skill : (SkillNames)ParserFunctions.ParseUlong(skillName, 0uL), ParserFunctions.ParseInt((skillNode as XmlElement)?.GetAttribute("level"), -1)));
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
                                        LongTermRelationshipTypes grouping;
                                        profile.AddActions(new ServiceUtils.ActiveTopicAction(actionNode.Attributes["name"].Value, ParserFunctions.TryParseEnum((actionNode as XmlElement)?.GetAttribute("group"), out grouping, LongTermRelationshipTypes.Undefined) ? grouping : LongTermRelationshipTypes.Default, isActive));
                                    }
                                    continue;
                                }
                                if (profilePropertyNode.Name == "Outputs")
                                {
                                    foreach (XmlNode outputNode in profilePropertyNode.ChildNodes)
                                    {
                                        XmlElement outputElement = outputNode as XmlElement;
                                        if (outputElement == null)
                                        {
                                            continue;
                                        }
                                        OutputUpdateType updateType;
                                        UpdateAboveAndBelowZeroType updateAboveAndBelowZero;
                                        profile.AddOutputs(new ServiceUtils.CommodityChange(outputElement.GetAttribute("interactionDefinitionType"), outputElement.GetAttribute("targetType"), ParserFunctions.ParseFloat(outputElement.GetAttribute("advertised"), 200f), ParserFunctions.ParseBool("locked"), ParserFunctions.ParseFloat(outputElement.GetAttribute("actual"), 200f), ParserFunctions.TryParseEnum(outputElement.GetAttribute("updateType"), out updateType, OutputUpdateType.ContinuousFlow) ? updateType : OutputUpdateType.ContinuousFlow, ParserFunctions.ParseBool(outputElement.GetAttribute("timeDependsOnCommodityFilling")), ParserFunctions.ParseBool(outputElement.GetAttribute("updateEvenOnFailure")), ParserFunctions.TryParseEnum(outputElement.GetAttribute("updateAboveAndBelowZero"), out updateAboveAndBelowZero, UpdateAboveAndBelowZeroType.Either) ? updateAboveAndBelowZero : UpdateAboveAndBelowZeroType.Either));
                                    }
                                    continue;
                                }
                                if (profilePropertyNode.Name == "Inventory")
                                {
                                    foreach (XmlNode inventoryObjectNode in profilePropertyNode.ChildNodes)
                                    {
                                        string instanceName = inventoryObjectNode.Attributes["instanceName"].Value;
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
                        XmlElement element = node as XmlElement;
                        if (element == null)
                        {
                            continue;
                        }
                        if (node.Name == "ServiceUniform")
                        {
                            OutfitAssignmentUtils.AssignedOutfit assignedOutfit = new OutfitAssignmentUtils.AssignedOutfit();
                            assignedOutfit.PartOverrides.Clear();
                            string serviceName = node.Attributes["profile"].Value;
                            int profileIndex = ServiceUtils.ServiceProfiles.FindIndex(x => x.Name == serviceName);
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
                                        continue;
                                    }
                                }
                                if (serviceUniformPropertyNode.Name == "PartOverrides")
                                {
                                    foreach (XmlNode partOverrideNode in serviceUniformPropertyNode.ChildNodes)
                                    {
                                        BodyTypes partType;
                                        if (ParserFunctions.TryParseEnum(partOverrideNode.Attributes["name"].Value, out partType, BodyTypes.None))
                                        {
                                            assignedOutfit.PartOverrides.Add(partType);
                                        }
                                    }
                                }
                            }
                            CASAgeGenderFlags age, gender;
                            string specialOutfitKey = OutfitAssignmentUtils.GetGlobalAssignedOutfitPrefix((ParserFunctions.TryParseEnum(node.Attributes["age"].Value, out age, CASAgeGenderFlags.None) ? age : CASAgeGenderFlags.None) | (ParserFunctions.TryParseEnum(node.Attributes["gender"].Value, out gender, CASAgeGenderFlags.None) ? gender : CASAgeGenderFlags.None)) + serviceName;
                            OutfitAssignmentUtils.OutfitAssignments.Add(new OutfitAssignmentUtils.OutfitAssignment(null, specialOutfitKey, ServiceUtils.ServiceProfiles[profileIndex]));
                            OutfitAssignmentUtils.IndexOutfitAssignments();
                            OutfitAssignmentUtils.AssignedOutfits[specialOutfitKey] = assignedOutfit;
                        }
                    }
                }
            }
        }

        public static bool TryUIGetSelectedSavedSettingsNames(out string[] selectedNames, string[] allNames, string title = null, int selectableRowCount = int.MaxValue)
        {
            bool retVal;
            string[] tempSelectedNames = null;
            if (DebugUtils.TryDisplayScriptError(() =>
                {
                    Array.Sort(allNames);
                    bool cancelled, confirmed;
                    while (true)
                    {
                        tempSelectedNames = (ObjectPickerDialog.Show(title ?? Responder.Instance.LocalizationModel.LocalizeString(kLocalizationKey + "/ServiceCollectionListDialog:Title"), new List<ObjectPicker.TabInfo>
                            {
                                new ObjectPicker.TabInfo("shop_all_r2", Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:All"), new List<string>(allNames).ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                            }, new List<ObjectPickerDialog.CommonHeaderInfo<string>>
                            {
                                new TextColumn(kLocalizationKey + "/ServiceCollectionListDialog")
                            }, selectableRowCount, out confirmed, out cancelled) ?? new List<string>()).ToArray();
                        if (cancelled)
                        {
                            tempSelectedNames = null;
                            return false;
                        }
                        if (confirmed)
                        {
                            return true;
                        }
                    }
                }, out retVal))
            {
                selectedNames = null;
                return false;
            }
            selectedNames = tempSelectedNames;
            return retVal;
        }

        public static string WrapInnerXmlAsDocument(XmlNode node)
        {
            if (string.IsNullOrEmpty(node.InnerXml))
            {
                return null;
            }
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(node.InnerXml);
            XmlDeclaration xmlDeclaration = xmlDocument.CreateXmlDeclaration("1.0", "utf-8", null);
            xmlDocument.InsertBefore(xmlDeclaration, xmlDocument.DocumentElement);
            StringBuilder stringBuilder = new StringBuilder();
            using (Utf8StringWriter stringWriter = new Utf8StringWriter(stringBuilder))
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
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
}

