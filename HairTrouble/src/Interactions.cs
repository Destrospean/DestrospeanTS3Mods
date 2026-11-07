using System;
using System.Collections.Generic;
using Destrospean.Enums.HairTrouble;
using Destrospean.Misc;
using Destrospean.UI.Columns;
using Destrospean.Utils;
using Destrospean.Utils.HairTrouble;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Objects.Decorations;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using ComboSelectionDialog = Destrospean.UI.Dialogs.ComboSelectionDialog;
using ObjectPickerDialog = Destrospean.UI.Dialogs.ObjectPickerDialog;
using Tuning = Sims3.Gameplay.Destrospean.HairTrouble.Settings;

namespace Destrospean.HairTrouble
{
    public class Interactions
    {
        public class ClassifyHairstyles : ImmediateInteraction<Sim, GameObject>
        {
            [DoesntRequireTuning]
            public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ClassifyHairstyles>
            {
                public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair iop)
                {
                    return Localization.LocalizeString(actor.IsFemale, kLocalizationKey + ":Name");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new[]
                    {
                        Localization.LocalizeString(isFemale, kLocalizationPath + ":Path")
                    };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return Tuning.kShowCheatInteractions;
                }
            }

            private const string kLocalizationKey = kLocalizationPath + "/ClassifyHairstyles";

            public static InteractionDefinition Singleton = new Definition();

            public override bool Run()
            {
                var hairGrowthStateMap = new Dictionary<string, HairGrowthStates>();
                foreach (var entry in SimHairGrowth.HairGrowthStateMap)
                {
                    hairGrowthStateMap[entry.Key] = entry.Value;
                }
                var hairstyles = new List<CASPart>();
                foreach (ResourceKey key in new KeySearch(0x034AEECB))
                {
                    var casPart = new CASPart(key);
                    if (casPart.BodyType == BodyTypes.Hair && (casPart.Age & Actor.SimDescription.Age) != 0 && (casPart.Gender & Actor.SimDescription.Gender) != 0)
                    {
                        hairstyles.Add(casPart);
                    }
                }
                if (!ShowHairGrowthStateMapDialog(hairstyles))
                {
                    SimHairGrowth.HairGrowthStateMap.Clear();
                    foreach (var entry in hairGrowthStateMap)
                    {
                        SimHairGrowth.HairGrowthStateMap[entry.Key] = entry.Value;
                    }
                }
                return true;
            }
            
            public static bool ShowHairGrowthStateMapDialog(List<CASPart> allHairstyles, string title = null)
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
                            new CASPartHairGrowthStateColumn(entryKey, SimHairGrowth.HairGrowthStateMap)
                        }, 1, out var confirmed, out var cancelled, true);
                    if (cancelled)
                    {
                        return false;
                    }
                    if (confirmed)
                    {
                        return true;
                    }
                    var keyString = selectedHairstyles[0].Key.ToS3PIFormatKeyString();
                    TryUIGetHairGrowthState(Localization.LocalizeString(entryKey + "/SubmenuTitles:HairGrowthState"), out var hairGrowthState, SimHairGrowth.HairGrowthStateMap.TryGetValue(keyString, out var defaultValue) ? defaultValue : HairGrowthStates.Unknown);
                    SimHairGrowth.HairGrowthStateMap[keyString] = hairGrowthState;
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
                            Localization.LocalizeString(entryKey + HairGrowthStates.Any),
                            nameof(HairGrowthStates.Any)
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
                            Localization.LocalizeString(entryKey + HairGrowthStates.ShortOrAbove),
                            nameof(HairGrowthStates.ShortOrAbove)
                        },
                        {
                            Localization.LocalizeString(entryKey + HairGrowthStates.Medium),
                            nameof(HairGrowthStates.Medium)
                        },
                        {
                            Localization.LocalizeString(entryKey + HairGrowthStates.MediumOrAbove),
                            nameof(HairGrowthStates.MediumOrAbove)
                        },
                        {
                            Localization.LocalizeString(entryKey + HairGrowthStates.Long),
                            nameof(HairGrowthStates.Long)
                        },
                        {
                            Localization.LocalizeString(entryKey + HairGrowthStates.LongOrAbove),
                            nameof(HairGrowthStates.LongOrAbove)
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
        
        public class DecrementHairGrowthState : ImmediateInteraction<Sim, Sim>
        {
            [DoesntRequireTuning]
            public class Definition : ImmediateInteractionDefinition<Sim, Sim, DecrementHairGrowthState>
            {
                public override string GetInteractionName(Sim actor, Sim target, InteractionObjectPair iop)
                {
                    return Localization.LocalizeString(target.IsFemale, kLocalizationKey + ":Name");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new[]
                    {
                        Localization.LocalizeString(isFemale, kLocalizationPath + ":Path")
                    };
                }

                public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return Tuning.kShowCheatInteractions;
                }
            }

            private const string kLocalizationKey = kLocalizationPath + "/DecrementHairGrowthState";

            public static InteractionDefinition Singleton = new Definition();

            public override bool Run()
            {
                Target.SimDescription.DecrementHairGrowthState();
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
                    return Localization.LocalizeString(target.IsFemale, kLocalizationKey + ":Name");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new[]
                    {
                        Localization.LocalizeString(isFemale, kLocalizationPath + ":Path")
                    };
                }

                public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return Tuning.kShowCheatInteractions;
                }
            }

            private const string kLocalizationKey = kLocalizationPath + "/IncrementHairGrowthState";

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
                    return Localization.LocalizeString(actor.IsFemale, kLocalizationKey + ":Name");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new[]
                    {
                        Localization.LocalizeString(isFemale, kLocalizationPath + ":Path")
                    };
                }

                public override bool Test(Sim actor, IMirror target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    if (actor.SimDescription.HasOriginalOverallHairColors() || actor.CurrentOutfitCategory == OutfitCategories.Singed || actor.SimDescription.Age == CASAgeGenderFlags.Baby || actor.OccultManager.DisallowClothesChange())
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

            private const string kLocalizationKey = kLocalizationPath + "/RemoveHairDye";

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
                    return Localization.LocalizeString(target.IsFemale, kLocalizationKey + ":Name");
                }

                public override string[] GetPath(bool isFemale)
                {
                    return new[]
                    {
                        Localization.LocalizeString(isFemale, kLocalizationPath + ":Path")
                    };
                }

                public override bool Test(Sim actor, Sim target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    return Tuning.kShowCheatInteractions;
                }
            }

            private const string kLocalizationKey = kLocalizationPath + "/ResetOriginalHair";

            public static InteractionDefinition Singleton = new Definition();

            public override bool Run()
            {
                Target.SimDescription.ClearOriginalOverallHairColors();
                Target.SimDescription.InitOriginalOverallHairColors();
                return true;
            }
        }

        private const string kLocalizationPath = "Destrospean/HairTrouble/Interactions";
    }
}
