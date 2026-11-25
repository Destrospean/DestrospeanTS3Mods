extern alias ExpandedHouseholdStaff;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Destrospean.Enums.HairTrouble;
using Destrospean.HairTrouble;
using Destrospean.UI.Dialogs;
using ExpandedHouseholdStaff::Destrospean.Misc;
using ExpandedHouseholdStaff::Destrospean.Utils.ExpandedHouseholdStaff;
using ExpandedHouseholdStaff::Sims3.Gameplay.Interfaces.Destrospean.ExpandedHouseholdStaff;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Destrospean.Utils;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
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
                        ParseServiceProfiles(xmlDocument);
                        ParseServiceUniforms(xmlDocument);
                        ParseServiceUniformHairData(xmlDocument);
                    }
                }
            }

            public static void ParseServiceUniformHairData(XmlDocument xmlDocument)
            {
                if (xmlDocument.DocumentElement == null)
                {
                    return;
                }
                foreach (XmlNode node in xmlDocument.DocumentElement.ChildNodes)
                {
                    if (node.Name != "ServiceUniformHairData" || !(node is XmlElement))
                    {
                        continue;
                    }
                    var specialOutfitKey = OutfitAssignmentUtils.OutfitAssignmentGlobalPrefix + node.Attributes?["uniformKey"].Value;
                    if (string.IsNullOrEmpty(specialOutfitKey) || !OutfitAssignmentUtils.AssignedOutfits.ContainsKey(specialOutfitKey))
                    {
                        continue;
                    }
                    SimHairGrowth.PreferredHairstylesBySpecialOutfitAndGrowthState[specialOutfitKey] = new SimHairGrowth.OutfitHairData(0uL, ResourceKey.kInvalidResourceKey, OutfitCategories.None, 0);
                    foreach (XmlNode hairstyleNode in node.ChildNodes)
                    {
                        HairGrowthStates hairGrowthState;
                        try
                        {
                            hairGrowthState = (HairGrowthStates)Enum.Parse(typeof(HairGrowthStates), node.Attributes?["growthState"].Value ?? "");
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
                        var profileList = new List<IServiceProfile>(profiles);
                        WriteServiceProfiles(xmlWriter, profileList);
                        WriteServiceUniforms(xmlWriter, profileList);
                        WriteServiceUniformHairData(xmlWriter, profileList);
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

            public static void WriteServiceUniformHairData(XmlWriter xmlWriter, IEnumerable<IServiceProfile> profiles)
            {
                var profileList = new List<IServiceProfile>(profiles);
                foreach (var entry in SimHairGrowth.PreferredHairstylesBySpecialOutfitAndGrowthState)
                {
                    var outfitAssignmentIndex = OutfitAssignmentUtils.OutfitAssignments.FindIndex(x => x.SpecialOutfitKey == entry.Key);
                    if (outfitAssignmentIndex == -1 || !profileList.Exists(x => x.Name == OutfitAssignmentUtils.OutfitAssignments[outfitAssignmentIndex].ServiceName) || OutfitAssignmentUtils.OutfitAssignments[outfitAssignmentIndex].SimDescription != null)
                    {
                        continue;
                    }
                    xmlWriter.WriteStartElement("ServiceUniformHairData");
                    xmlWriter.WriteAttributeString("uniformKey", entry.Key.Substring(OutfitAssignmentUtils.OutfitAssignmentGlobalPrefix.Length));
                    foreach (var hairGrowthState in entry.Value.PreferredHairstylesByGrowthState.Keys)
                    {
                        xmlWriter.WriteStartElement("Hairstyle");
                        xmlWriter.WriteAttributeString("partKey", entry.Value.PreferredHairstylesByGrowthState[hairGrowthState].Key.ToString().ToUpperInvariant());
                        xmlWriter.WriteAttributeString("growthState", hairGrowthState.ToString());
                        xmlWriter.WriteRaw(entry.Value.PreferredHatPresetsByGrowthState[hairGrowthState].Substring(entry.Value.PreferredHatPresetsByGrowthState[hairGrowthState].IndexOf("\n", StringComparison.Ordinal)));
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();
                }
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
