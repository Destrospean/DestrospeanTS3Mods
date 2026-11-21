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
using Sims3.Gameplay.Destrospean.HairTrouble;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using ComboSelectionDialog = Destrospean.UI.Dialogs.ComboSelectionDialog;
using ObjectPickerDialog = Destrospean.UI.Dialogs.ObjectPickerDialog;

namespace Destrospean.HairTrouble.Interactions
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
                    Localization.LocalizeString(isFemale, LocalizationKey.Replace("/ClassifyHairstyles", ":Path"))
                };
            }

            public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                return Settings.kShowCheatInteractions && !isAutonomous;
            }
        }

        public static readonly string LocalizationKey = typeof(ClassifyHairstyles).GetLocalizationKey();

        public static InteractionDefinition Singleton = new Definition();

        public override bool Run()
        {
            var hairClassifications = new Dictionary<string, HairGrowthStates>();
            foreach (var hairClassification in SimHairGrowth.HairClassifications)
            {
                hairClassifications[hairClassification.Key] = hairClassification.Value;
            }
            var entryKey = typeof(ObjectPickerDialog).GetLocalizationKey().Replace(nameof(ObjectPickerDialog), "DemographicDialog");
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
                        foreach (var hairClassification in hairClassifications)
                        {
                            SimHairGrowth.HairClassifications[hairClassification.Key] = hairClassification.Value;
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
                    if (!TryUIGetHairGrowthState(Localization.LocalizeString(typeof(ObjectPickerDialog).GetLocalizationKey().Replace(nameof(ObjectPickerDialog), "HairGrowthStateMapDialog/SubmenuTitles:HairGrowthState")), out hairGrowthState))
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
            var entryKey = typeof(ObjectPickerDialog).GetLocalizationKey().Replace(nameof(ObjectPickerDialog), "HairGrowthStateListDialog");
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
            var entryKey = typeof(ObjectPickerDialog).GetLocalizationKey().Replace(nameof(ObjectPickerDialog), "HairGrowthStateMapDialog");
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
            var entryKey = typeof(ComboSelectionDialog).GetLocalizationKey().Replace(nameof(ComboSelectionDialog), "HairGrowthStateMapDialog/Options/HairGrowthState:");
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
}
