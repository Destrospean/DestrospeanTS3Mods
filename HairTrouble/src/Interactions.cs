using System;
using System.Collections.Generic;
using Destrospean.Enums.HairTrouble;
using Destrospean.Misc;
using Destrospean.UI.Columns;
using Destrospean.UI.Columns.HairTrouble;
using Destrospean.Utils;
using Destrospean.Utils.HairTrouble;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Destrospean.HairTrouble;
using Sims3.Gameplay.Destrospean.Utils;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Objects.Decorations;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.SimIFace.CustomContent;
using Sims3.UI;
using ComboSelectionDialog = Destrospean.UI.Dialogs.ComboSelectionDialog;
using ObjectPickerDialog = Destrospean.UI.Dialogs.ObjectPickerDialog;

namespace Destrospean.HairTrouble
{
    public static class Interactions
    {
        public class ClassifyHairstyles : ImmediateInteraction<Sim, GameObject>
        {
            [DoesntRequireTuning]
            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ClassifyHairstyles>
            {
                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair iop)
                {
                    return Localization.LocalizeString(actor.IsFemale, LocalizationKey + ":Name");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new[]
                    {
                        Localization.LocalizeString(isFemale, LocalizationPath + ":Path")
                    };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return Settings.kShowCheatInteractions && !isAutonomous;
                }
            }

            public const string LocalizationKey = LocalizationPath + "/ClassifyHairstyles";

            public static InteractionDefinition Singleton = new Definition();

            public override bool Run()
            {
                var hairGrowthStateMap = new Dictionary<string, HairGrowthStates>();
                foreach (var hairClassification in SimHairGrowth.HairClassifications)
                {
                    hairGrowthStateMap[hairClassification.Key] = hairClassification.Value;
                }
                var entryKey = typeof(ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "DemographicDialog");
                var age = CASAgeGenderFlags.None;
                var gender = CASAgeGenderFlags.None;
                var hairGrowthState = HairGrowthStates.Unknown;
                byte step = 0;
                while (true)
                {
                    if (step == 0)
                    {
                        if (!CommonUtils.ShowCASAgeGenderFlagListDialog(out age, CASAgeGenderFlags.None, CASAgeGenderFlags.AgeMask ^ CASAgeGenderFlags.Baby, Localization.LocalizeString(entryKey + "/Titles:Age"), entryKey, false))
                        {
                            SimHairGrowth.HairClassifications.Clear();
                            foreach (var entry in hairGrowthStateMap)
                            {
                                SimHairGrowth.HairClassifications[entry.Key] = entry.Value;
                            }
                            return true;
                        }
                        step++;
                    }
                    if (step == 1)
                    {
                        if (!CommonUtils.ShowCASAgeGenderFlagListDialog(out gender, CASAgeGenderFlags.None, CASAgeGenderFlags.GenderMask, Localization.LocalizeString(entryKey + "/Titles:Gender"), entryKey, false))
                        {
                            step--;
                            continue;
                        }
                        step++;
                    }
                    if (step == 2)
                    {
                        if (!TryUIGetHairGrowthState(Localization.LocalizeString(typeof(ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "HairGrowthStateMapDialog/SubmenuTitles:HairGrowthState")), out hairGrowthState))
                        {
                            step--;
                            continue;
                        }
                        step++;
                    }
                    if (step == 3)
                    {
                        var hairstyles = new List<CASPart>();
                        foreach (ResourceKey key in new KeySearch(0x034AEECB))
                        {
                            var casPart = new CASPart(key);
                            if (casPart.BodyType == BodyTypes.Hair && (casPart.Age & age) != CASAgeGenderFlags.None && (casPart.Gender & gender) != CASAgeGenderFlags.None && (SimHairGrowth.HairClassifications.TryGetValue(key.ToS3PIFormatKeyString(), out var value) ? (hairGrowthState & value) != HairGrowthStates.Unknown || hairGrowthState == value : hairGrowthState == HairGrowthStates.Unknown))
                            {
                                hairstyles.Add(casPart);
                            }
                        }
                        if (!ShowHairGrowthStateMapDialog(hairGrowthState, hairstyles))
                        {
                            step--;
                            continue;
                        }
                    }
                    return true;
                }
            }
            
            public static bool ShowHairGrowthStateListDialog(out HairGrowthStates hairGrowthStates, HairGrowthStates? preSelectedHairGrowthStates = null)
            {
                hairGrowthStates = HairGrowthStates.Unknown;
                var entryKey = typeof(ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "HairGrowthStateListDialog");
                var hairGrowthStateList = new List<HairGrowthStates>(Array.FindAll((HairGrowthStates[])Enum.GetValues(typeof(HairGrowthStates)), x => preSelectedHairGrowthStates.HasValue && (preSelectedHairGrowthStates & x) == x));
                while (true)
                {
                    if (hairGrowthStateList.Count == 0)
                    {
                        hairGrowthStateList.Add(HairGrowthStates.Unknown);
                    }
                    if (hairGrowthStateList.Contains(HairGrowthStates.Unknown) && hairGrowthStateList.Count > 1)
                    {
                        hairGrowthStateList.Remove(HairGrowthStates.Unknown);
                    }
                    if (hairGrowthStateList.Count == Enum.GetNames(typeof(HairGrowthStates)).Length - 2)
                    {
                        hairGrowthStateList.Add(HairGrowthStates.Any);
                    }
                    var selectedHairGrowthStates = ObjectPickerDialog.Show(Responder.Instance.LocalizationModel.LocalizeString(entryKey + ":Title"), new List<ObjectPicker.TabInfo>
                        {
                            new ObjectPicker.TabInfo("shop_all_r2", Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:All"), new List<HairGrowthStates>((HairGrowthStates[])Enum.GetValues(typeof(HairGrowthStates))).ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                        }, new List<ObjectPickerDialog.CommonHeaderInfo<HairGrowthStates>>
                        {
                            new HairGrowthStateColumn(entryKey),
                            new HairGrowthStateEnabledColumn(entryKey, hairGrowthStateList.ToArray())
                        }, 1, out var confirmed, out var cancelled, true);
                    if (cancelled)
                    {
                        return false;
                    }
                    if (confirmed)
                    {
                        foreach (var hairGrowthState in hairGrowthStateList)
                        {
                            hairGrowthStates |= hairGrowthState;
                        }
                        return true;
                    }
                    if (selectedHairGrowthStates[0] == HairGrowthStates.Any)
                    {
                        hairGrowthStateList.Clear();
                        foreach (HairGrowthStates hairGrowthState in Enum.GetValues(typeof(HairGrowthStates)))
                        {
                            hairGrowthStateList.Add(hairGrowthState);
                        }
                    }
                    else if (hairGrowthStateList.Contains(selectedHairGrowthStates[0]))
                    {
                        hairGrowthStateList.Remove(selectedHairGrowthStates[0]);
                        if (hairGrowthStateList.Contains(HairGrowthStates.Any))
                        {
                            hairGrowthStateList.Remove(HairGrowthStates.Any);
                        }
                    }
                    else
                    {
                        if (selectedHairGrowthStates[0] == HairGrowthStates.Unknown)
                        {
                            hairGrowthStateList.Clear();
                        }
                        hairGrowthStateList.Add(selectedHairGrowthStates[0]);
                    }
                }
            }
            
            public static bool ShowHairGrowthStateMapDialog(HairGrowthStates currentHairGrowthState, List<CASPart> allHairstyles, string title = null)
            {
                var entryKey = typeof(ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "HairGrowthStateMapDialog");
                while (true)
                {
                    var selectedHairstyles = ObjectPickerDialog.Show(title ?? Responder.Instance.LocalizationModel.LocalizeString(entryKey + ":Title"), new List<ObjectPicker.TabInfo>
                        {
                            new ObjectPicker.TabInfo("shop_all_r2", Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:All"), allHairstyles.ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                        }, new List<ObjectPickerDialog.CommonHeaderInfo<CASPart>>
                        {
                            new CASPartColumn(entryKey),
                            new CASPartHairGrowthStateColumn(entryKey, SimHairGrowth.HairClassifications)
                        }, int.MaxValue, out var confirmed, out var cancelled, true, 1);
                    if (cancelled)
                    {
                        return false;
                    }
                    if (confirmed && selectedHairstyles.Count == 0)
                    {
                        return true;
                    }
                    if (ShowHairGrowthStateListDialog(out var hairGrowthStates, SimHairGrowth.HairClassifications.TryGetValue(selectedHairstyles[0].Key.ToS3PIFormatKeyString(), out var defaultValue) ? defaultValue : HairGrowthStates.Unknown))
                    {
                        foreach (var hairstyle in selectedHairstyles)
                        {
                            SimHairGrowth.HairClassifications[hairstyle.Key.ToS3PIFormatKeyString()] = hairGrowthStates;
                            if ((currentHairGrowthState & hairGrowthStates) == HairGrowthStates.Unknown && currentHairGrowthState != hairGrowthStates)
                            {
                                allHairstyles.Remove(hairstyle);
                            }
                        }
                    }
                }
            }
            
            public static bool TryUIGetHairGrowthState(string title, out HairGrowthStates hairGrowthState, HairGrowthStates defaultValue = HairGrowthStates.Unknown)
            {
                var entryKey = typeof(ComboSelectionDialog).GetLocalizationKey().Replace("ComboSelectionDialog", "HairGrowthStateMapDialog/Options/HairGrowthState:");
                if (ComboSelectionDialog.Show(title, new SortedDictionary<string, object>(new DummyComparer())
                    {
                        {
                            Localization.LocalizeString(entryKey + HairGrowthStates.Unknown),
                            nameof(HairGrowthStates.Unknown)
                        },
                        {
                            Localization.LocalizeString(entryKey + HairGrowthStates.Bald),
                            nameof(HairGrowthStates.Bald)
                        },
                        {
                            Localization.LocalizeString(entryKey + HairGrowthStates.Shaved),
                            nameof(HairGrowthStates.Shaved)
                        },
                        {
                            Localization.LocalizeString(entryKey + HairGrowthStates.Short),
                            nameof(HairGrowthStates.Short)
                        },
                        {
                            Localization.LocalizeString(entryKey + HairGrowthStates.Medium),
                            nameof(HairGrowthStates.Medium)
                        },
                        {
                            Localization.LocalizeString(entryKey + HairGrowthStates.Long),
                            nameof(HairGrowthStates.Long)
                        },
                        {
                            Localization.LocalizeString(entryKey + HairGrowthStates.VeryLong),
                            nameof(HairGrowthStates.VeryLong)
                        }
                    }, defaultValue.ToString()) is string text)
                {
                    
                    hairGrowthState = (HairGrowthStates)Enum.Parse(typeof(HairGrowthStates), text);
                    return true;
                }
                hairGrowthState = defaultValue;
                return false;
            }
        }
        
        public class CutOwnHair : Interaction<Sim, IMirror>
        {
            public class Definition : InteractionDefinition<Sim, IMirror, CutOwnHair>
            {
                public HairGrowthStates HairGrowthState;

                public Definition()
                {
                }

                public Definition(HairGrowthStates hairGrowthState)
                {
                    HairGrowthState = hairGrowthState;
                }
                
                public override void AddInteractions(InteractionObjectPair iop, Sim actor, IMirror target, List<InteractionObjectPair> results)
                {
                    foreach (HairGrowthStates hairGrowthState in Enum.GetValues(typeof(HairGrowthStates)))
                    {
                        if (hairGrowthState > HairGrowthStates.Unknown && hairGrowthState < HairGrowthStates.VeryLong)
                        {
                            results.Add(new InteractionObjectPair(new Definition(hairGrowthState), target));
                        }
                    }
                }

                public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
                {
                    var cutOwnHair = new CutOwnHair();
                    cutOwnHair.SetHairGrowthState(HairGrowthState);
                    cutOwnHair.Init(ref parameters);
                    return cutOwnHair;
                }
                
                public override string GetInteractionName(Sim actor, IMirror target, InteractionObjectPair iop)
                {
                    return Localization.LocalizeString(actor.IsFemale, LocalizationKey + "/Names:" + HairGrowthState);
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new[]
                    {
                        Localization.LocalizeString(isFemale, LocalizationPath + ":Path"),
                        Localization.LocalizeString(isFemale, LocalizationKey + ":Path")
                    };
                }
                
                public override bool Test(Sim actor, IMirror target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    if (actor.SimDescription == null || actor.CurrentOutfitCategory == OutfitCategories.Singed || actor.SimDescription.Age == CASAgeGenderFlags.Baby || actor.OccultManager.DisallowClothesChange())
                    {
                        return false;
                    }
                    if (actor.BuffManager.HasTransformBuff())
                    {
                        greyedOutTooltipCallback = CreateTooltipCallback(Localization.LocalizeString(actor.IsFemale, "Gameplay/Buffs/BuffTransformation:CantChangeClothesTooltip", actor));
                        return false;
                    }
                    if (HairGrowthStates.Bald == actor.SimDescription.GetHairGrowthState())
                    {
                        greyedOutTooltipCallback = CreateTooltipCallback(Localization.LocalizeString(actor.IsFemale, LocalizationKey + "/Tooltips:SimHasNoHair", actor));
                        return false;
                    }
                    if (HairGrowthState > actor.SimDescription.GetHairGrowthState())
                    {
                        greyedOutTooltipCallback = CreateTooltipCallback(Localization.LocalizeString(actor.IsFemale, LocalizationKey + "/Tooltips:SimHasHairTooShort", actor));
                        return false;
                    }
                    if (HairGrowthState == actor.SimDescription.GetHairGrowthState())
                    {
                        greyedOutTooltipCallback = CreateTooltipCallback(Localization.LocalizeString(actor.IsFemale, LocalizationKey + "/Tooltips:SimHasHairSameLength", actor));
                        return false;
                    }
                    return true;
                }
            }
            
            public HairGrowthStates HairGrowthState;

            public const string LocalizationKey = LocalizationPath + "/CutOwnHair";

            public static InteractionDefinition Singleton = new Definition();
            
            public override bool Run()
            {
                if (!Target.Route(this))
                {
                    return false;
                }
                StandardEntry();
                Mirror.EnterStateMachine(this);
                AnimateSim("ChangeAppearance");
                if (!Actor.IsSelectable)
                {
                    StandardExit();
                    return false;
                }
                Actor.SimDescription.SetHairGrowthState(HairGrowthState, HairGrowthStateChangeFlags.Haircut, true);
                StandardExit();
                return true;
            }
            
            public void SetHairGrowthState(HairGrowthStates hairGrowthState)
            {
                HairGrowthState = hairGrowthState;
            }
        }
        
        public class DecrementHairGrowthState : ImmediateInteraction<Sim, Sim>
        {
            [DoesntRequireTuning]
            public class Definition : ImmediateInteractionDefinition<Sim, Sim, DecrementHairGrowthState>
            {
                public override string GetInteractionName(Sim actor, Sim target, InteractionObjectPair iop)
                {
                    return Localization.LocalizeString(target.IsFemale, LocalizationKey + ":Name");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new[]
                    {
                        Localization.LocalizeString(isFemale, LocalizationPath + ":Path")
                    };
                }

                public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return Settings.kShowCheatInteractions && !isAutonomous;
                }
            }

            public const string LocalizationKey = LocalizationPath + "/DecrementHairGrowthState";

            public static InteractionDefinition Singleton = new Definition();

            public override bool Run()
            {
                Target.SimDescription.DecrementHairGrowthState();
                return true;
            }
        }
        
        public class DeleteHairClassificationCollection : ImmediateInteraction<Sim, GameObject>
        {
            [DoesntRequireTuning]
            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, DeleteHairClassificationCollection>
            {
                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair iop)
                {
                    return Localization.LocalizeString(actor.IsFemale, LocalizationKey + ":Name");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new[]
                    {
                        Localization.LocalizeString(isFemale, LocalizationPath + ":Path")
                    };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return Settings.kShowCheatInteractions && !isAutonomous;
                }
            }

            public const string LocalizationKey = LocalizationPath + "/DeleteHairClassificationCollection";

            public static InteractionDefinition Singleton = new Definition();

            public override bool Run()
            {
                FileUtils.Singleton.DeleteFile();
                return true;
            }
        }
        
        public class ExportHairClassificationCollection : ImmediateInteraction<Sim, GameObject>
        {
            [DoesntRequireTuning]
            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ExportHairClassificationCollection>
            {
                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair iop)
                {
                    return Localization.LocalizeString(actor.IsFemale, LocalizationKey + ":Name");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new[]
                    {
                        Localization.LocalizeString(isFemale, LocalizationPath + ":Path")
                    };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return Settings.kShowCheatInteractions && new List<HairGrowthStates>(SimHairGrowth.HairClassifications.Values).Exists(x => x > HairGrowthStates.Unknown) && !isAutonomous;
                }
            }

            public const string LocalizationKey = LocalizationPath + "/ExportHairClassificationCollection";

            public static InteractionDefinition Singleton = new Definition();

            public override bool Run()
            {
                return !DebugUtils.TryDisplayScriptError(() =>
                    {
                        var hairGrowthStateMapList = new List<KeyValuePair<string, HairGrowthStates>>();
                        foreach (var entry in SimHairGrowth.HairClassifications)
                        {
                            if (entry.Value > HairGrowthStates.Unknown)
                            {
                                hairGrowthStateMapList.Add(entry);
                            }
                        }
                        hairGrowthStateMapList.Sort((a, b) => a.Value.CompareTo(b.Value));
                        var hairGrowthStateMap = new SortedDictionary<string, HairGrowthStates>(new DummyComparer());
                        foreach (var entry in hairGrowthStateMapList)
                        {
                            hairGrowthStateMap.Add(entry.Key, entry.Value);
                        }
                        var xml = FileUtils.Singleton.ToXml(hairGrowthStateMap);
                        if (!FileUtils.Singleton.ExportToFile(xml, out _))
                        {
                            return;
                        }
                        if (Settings.kExportHairClassificationCollectionsAsXMLs)
                        {
                            hairGrowthStateMapList.Clear();
                            foreach (var entry in SimHairGrowth.HairClassifications)
                            {
                                if (entry.Value > HairGrowthStates.Unknown && !DownloadContent.IsCustomContent(S3PIResourceUtils.FromS3PIFormatKeyString(entry.Key)))
                                {
                                    hairGrowthStateMapList.Add(entry);
                                }
                            }
                            hairGrowthStateMapList.Sort((a, b) => a.Value.CompareTo(b.Value));
                            hairGrowthStateMap.Clear();
                            foreach (var entry in hairGrowthStateMapList)
                            {
                                hairGrowthStateMap.Add(entry.Key, entry.Value);
                            }
                            xml = FileUtils.Singleton.ToXml(hairGrowthStateMap);
                            var fileHandle = 0u;
                            Simulator.CreateExportFile(ref fileHandle, "HairClassifications");
                            if (fileHandle != 0u)
                            {
                                var customXmlWriter = new CustomXmlWriter(fileHandle);
                                var postDeclarationIndex = xml.IndexOf("\r\n", StringComparison.Ordinal);
                                var lineEndingLength = 2;
                                if (postDeclarationIndex == -1)
                                {
                                    postDeclarationIndex = xml.IndexOf("\n", StringComparison.Ordinal);
                                    lineEndingLength = 1;
                                }
                                customXmlWriter.WriteToBuffer(xml.Remove(postDeclarationIndex + lineEndingLength));
                                customXmlWriter.WriteComment(" These XMLs are only exported when `kExportHairClassificationCollectionsAsXMLs` is enabled in the tuning. ");
                                customXmlWriter.WriteToBuffer(xml.Substring(postDeclarationIndex + lineEndingLength));
                                customXmlWriter.WriteEndDocument();
                            }
                        }
                    });
            }
        }
        
        public class ImportHairClassificationCollection : ImmediateInteraction<Sim, GameObject>
        {
            [DoesntRequireTuning]
            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ImportHairClassificationCollection>
            {
                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair iop)
                {
                    return Localization.LocalizeString(actor.IsFemale, LocalizationKey + ":Name");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new[]
                    {
                        Localization.LocalizeString(isFemale, LocalizationPath + ":Path")
                    };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return Settings.kShowCheatInteractions && !isAutonomous;
                }
            }

            public const string LocalizationKey = LocalizationPath + "/ImportHairClassificationCollection";

            public static InteractionDefinition Singleton = new Definition();

            public override bool Run()
            {
                FileUtils.Singleton.ImportFromFile();
                return true;
            }
        }

        public class IncrementHairGrowthState : ImmediateInteraction<Sim, Sim>
        {
            [DoesntRequireTuning]
            public class Definition : ImmediateInteractionDefinition<Sim, Sim, IncrementHairGrowthState>
            {
                public override string GetInteractionName(Sim actor, Sim target, InteractionObjectPair iop)
                {
                    return Localization.LocalizeString(target.IsFemale, LocalizationKey + ":Name");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new[]
                    {
                        Localization.LocalizeString(isFemale, LocalizationPath + ":Path")
                    };
                }

                public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return Settings.kShowCheatInteractions && !isAutonomous;
                }
            }

            public const string LocalizationKey = LocalizationPath + "/IncrementHairGrowthState";

            public static InteractionDefinition Singleton = new Definition();

            public override bool Run()
            {
                Target.SimDescription.IncrementHairGrowthState();
                return true;
            }
        }

        public class RemoveHairDye : Interaction<Sim, IMirror>
        {
            public class Definition : InteractionDefinition<Sim, IMirror, RemoveHairDye>
            {
                public override string GetInteractionName(Sim actor, IMirror target, InteractionObjectPair iop)
                {
                    return Localization.LocalizeString(actor.IsFemale, LocalizationKey + ":Name");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new[]
                    {
                        Localization.LocalizeString(isFemale, LocalizationPath + ":Path")
                    };
                }

                public override bool Test(Sim actor, IMirror target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    if (actor.SimDescription == null || actor.SimDescription.HasOriginalOverallHairColors() || actor.CurrentOutfitCategory == OutfitCategories.Singed || actor.SimDescription.Age == CASAgeGenderFlags.Baby || actor.OccultManager.DisallowClothesChange())
                    {
                        return false;
                    }
                    if (actor.BuffManager.HasTransformBuff())
                    {
                        greyedOutTooltipCallback = CreateTooltipCallback(Localization.LocalizeString(actor.IsFemale, "Gameplay/Buffs/BuffTransformation:CantChangeClothesTooltip", actor));
                        return false;
                    }
                    return true;
                }
            }

            public const string LocalizationKey = LocalizationPath + "/RemoveHairDye";

            public static InteractionDefinition Singleton = new Definition();

            public override bool Run()
            {
                if (!Target.Route(this))
                {
                    return false;
                }
                StandardEntry();
                Mirror.EnterStateMachine(this);
                AnimateSim("ChangeAppearance");
                if (!Actor.IsSelectable)
                {
                    StandardExit();
                    return false;
                }
                Actor.SimDescription.ApplyOverallHairColorsToAllOutfits(Actor.SimDescription.GetOriginalBodyHairColor(), Actor.SimDescription.GetOriginalEyebrowColor(), Actor.SimDescription.GetOriginalFacialHairColors(), Actor.SimDescription.GetOriginalHairColors(), true);
                StandardExit();
                return true;
            }
        }

        public class ResetOriginalHair : ImmediateInteraction<Sim, Sim>
        {
            [DoesntRequireTuning]
            public class Definition : ImmediateInteractionDefinition<Sim, Sim, ResetOriginalHair>
            {
                public override string GetInteractionName(Sim actor, Sim target, InteractionObjectPair iop)
                {
                    return Localization.LocalizeString(target.IsFemale, LocalizationKey + ":Name");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new[]
                    {
                        Localization.LocalizeString(isFemale, LocalizationPath + ":Path")
                    };
                }

                public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return Settings.kShowCheatInteractions && !isAutonomous;
                }
            }

            public const string LocalizationKey = LocalizationPath + "/ResetOriginalHair";

            public static InteractionDefinition Singleton = new Definition();

            public override bool Run()
            {
                Target.SimDescription.ClearOriginalOverallHairColors();
                Target.SimDescription.InitOriginalOverallHairColors();
                return true;
            }
        }
        
        public class StyleOwnHair : CutOwnHair
        {
            public new class Definition : CutOwnHair.Definition
            {
                public Definition()
                {
                }

                public Definition(HairGrowthStates hairGrowthState)
                {
                    HairGrowthState = hairGrowthState;
                }
                
                public override void AddInteractions(InteractionObjectPair iop, Sim actor, IMirror target, List<InteractionObjectPair> results)
                {
                    foreach (HairGrowthStates hairGrowthState in Enum.GetValues(typeof(HairGrowthStates)))
                    {
                        if (hairGrowthState > HairGrowthStates.Unknown && hairGrowthState <= HairGrowthStates.VeryLong)
                        {
                            results.Add(new InteractionObjectPair(new Definition(hairGrowthState), target));
                        }
                    }
                }

                public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
                {
                    var styleOwnHair = new StyleOwnHair();
                    styleOwnHair.SetHairGrowthState(HairGrowthState);
                    styleOwnHair.Init(ref parameters);
                    return styleOwnHair;
                }
                
                public override string GetInteractionName(Sim actor, IMirror target, InteractionObjectPair iop)
                {
                    return Localization.LocalizeString(actor.IsFemale, LocalizationKey + "/Names:" + HairGrowthState) + Localization.Ellipsis;
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new[]
                    {
                        Localization.LocalizeString(isFemale, LocalizationPath + ":Path"),
                        Localization.LocalizeString(isFemale, LocalizationKey + ":Path")
                    };
                }
                
                public override void PopulatePieMenuPicker(ref InteractionInstanceParameters parameters, out List<ObjectPicker.TabInfo> selectedObjects, out List<ObjectPicker.HeaderInfo> headers, out int numSelectableRows)
                {
                    numSelectableRows = 1;
                    var casPartColumn = new CASPartColumn(typeof(ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "HairGrowthStateMapDialog"));
                    headers = new List<ObjectPicker.HeaderInfo>
                        {
                            new ObjectPicker.HeaderInfo(casPartColumn.HeaderTextId, casPartColumn.HeaderTooltipId, casPartColumn.Width)
                        };
                    selectedObjects = new List<ObjectPicker.TabInfo>
                        {
                            new ObjectPicker.TabInfo("shop_all_r2", Localization.LocalizeString(parameters.Actor.IsFemale, LocalizationKey + "/Names:" + HairGrowthState), new List<ObjectPicker.RowInfo>())
                        };
                    foreach (var hairClassification in SimHairGrowth.HairClassifications)
                    {
                        if ((hairClassification.Value & ((Definition)parameters.InteractionDefinition).HairGrowthState) != HairGrowthStates.Unknown)
                        {
                            var hairstyle = new CASPart(S3PIResourceUtils.FromS3PIFormatKeyString(hairClassification.Key));
                            if (hairstyle.Key != ResourceKey.kInvalidResourceKey && (hairstyle.Age & parameters.Actor.SimDescription.Age) != CASAgeGenderFlags.None && (hairstyle.Gender & parameters.Actor.SimDescription.Gender) != CASAgeGenderFlags.None && (hairstyle.Species & parameters.Actor.SimDescription.Species) != CASAgeGenderFlags.None && (hairstyle.CategoryFlags & (uint)OutfitCategoriesExtended.IsHiddenInCAS) == 0u)
                            {
                                selectedObjects[0].RowInfo.Add(new ObjectPicker.RowInfo(hairstyle, new List<ObjectPicker.ColumnInfo>
                                    {
                                        casPartColumn.GetValue(hairstyle)
                                    }));
                            }
                        }
                    }
                }

                public override bool Test(Sim actor, IMirror target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    if (actor.SimDescription == null || actor.CurrentOutfitCategory == OutfitCategories.Singed || actor.SimDescription.Age == CASAgeGenderFlags.Baby || actor.OccultManager.DisallowClothesChange())
                    {
                        return false;
                    }
                    if (actor.BuffManager.HasTransformBuff())
                    {
                        greyedOutTooltipCallback = CreateTooltipCallback(Localization.LocalizeString(actor.IsFemale, "Gameplay/Buffs/BuffTransformation:CantChangeClothesTooltip", actor));
                        return false;
                    }
                    var hairGrowthState = actor.SimDescription.GetHairGrowthState();
                    if (HairGrowthState > hairGrowthState)
                    {
                        greyedOutTooltipCallback = CreateTooltipCallback(Localization.LocalizeString(actor.IsFemale, LocalizationKey + "/Tooltips:SimHas" + (hairGrowthState == HairGrowthStates.Bald ? "NoHair" : "HairTooShort"), actor));
                        return false;
                    }
                    return true;
                }
            }

            public new const string LocalizationKey = LocalizationPath + "/StyleOwnHair";

            public new static InteractionDefinition Singleton = new Definition();
            
            public override bool Run()
            {
                if (!Target.Route(this))
                {
                    return false;
                }
                StandardEntry();
                Mirror.EnterStateMachine(this);
                AnimateSim("ChangeAppearance");
                if (!Actor.IsSelectable || SelectedObjects == null || SelectedObjects.Count != 1)
                {
                    StandardExit();
                    return false;
                }
                if (SelectedObjects[0] is CASPart hairstyle)
                {
                    var hairGrowthState = Actor.SimDescription.GetHairGrowthState();
                    if (HairGrowthState == hairGrowthState || SimHairGrowth.HairClassifications.TryGetValue(hairstyle.Key.ToS3PIFormatKeyString(), out var hairGrowthStates) && (hairGrowthStates & hairGrowthState) != HairGrowthStates.Unknown)
                    {
                        Actor.SimDescription.ApplyToAllOutfits((simBuilder, outfitCategory, outfitIndex) =>
                            {
                                simBuilder.PrepareForOutfit(Actor.SimDescription.GetOutfit(outfitCategory, outfitIndex));
                                simBuilder.RemoveParts(BodyTypes.Hair);
                                simBuilder.AddPart(hairstyle);
                                OutfitUtils.InjectBodyHairColor(simBuilder, Actor.SimDescription.BodyHairColor.ActiveColor);
                                OutfitUtils.InjectEyeBrowHairColor(simBuilder, Actor.SimDescription.EyebrowColor.ActiveColor);
                                OutfitUtils.InjectHairColor(simBuilder, Array.ConvertAll(Actor.SimDescription.FacialHairColors, x => x.ActiveColor), BodyTypes.Beard);
                                OutfitUtils.InjectHairColor(simBuilder, Array.ConvertAll(Actor.SimDescription.HairColors, x => x.ActiveColor), BodyTypes.Hair);
                                return new SimOutfit(simBuilder.CacheOutfit($"ApplyHairGrowthState_{Actor.SimDescription.SimDescriptionId}_{outfitCategory}_{outfitIndex}"));
                            });
                    }
                    else
                    {
                        Actor.SimDescription.SetHairGrowthState(HairGrowthState, HairGrowthStateChangeFlags.Haircut, true, hairstyle);
                    }
                }
                StandardExit();
                return true;
            }
        }

        public const string LocalizationPath = "Destrospean/HairTrouble/Interactions";
    }
}
