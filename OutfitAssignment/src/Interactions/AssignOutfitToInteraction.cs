using System;
using System.Collections.Generic;
using Destrospean.Misc;
using Destrospean.OutfitAssignment.UI.Columns;
using Destrospean.Utils;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.SimIFace.CustomContent;
using Sims3.UI;
using ComboSelectionDialog = Destrospean.OutfitAssignment.UI.Dialogs.ComboSelectionDialog;
using Gameflow = Sims3.Gameplay.Gameflow;
using ObjectPickerDialog = Destrospean.OutfitAssignment.UI.Dialogs.ObjectPickerDialog;

namespace Destrospean.OutfitAssignment.Interactions
{
    public class AssignOutfitToInteraction : ImmediateInteraction<Sim, GameObject>
    {
        public class Definition : ImmediateInteractionDefinition<Sim, GameObject, AssignOutfitToInteraction>
        {
            public bool IsGlobal;

            public bool IsPartial;

            public bool TargetIsSim;

            public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair iop)
            {
                var targetSim = target as Sim;
                return Common.Localize(targetSim != null && targetSim.IsFemale, kLocalizationKey + "/Names:" + (IsPartial ? "Partial" : "Full"));
            }

            public override string[] GetPath(bool isFemale)
            {
                var basePath = Common.Localize(isFemale, kLocalizationKey + "/Paths:Base");
                return TargetIsSim ? new[]
                {
                    basePath
                } : new[]
                {
                    basePath,
                    Common.Localize(isFemale, kLocalizationKey + "/Paths:" + (IsGlobal ? "Global" : "Individual"))
                };
            }

            public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                var targetSim = target as Sim;
                return targetSim == null || targetSim.SimDescription != null && targetSim.IsHuman;
            }
        }

        public class DummyComparer : IComparer<string>
        {
            public int Compare(string a, string b)
            {
                return 1;
            }
        }
        
        private const string kLocalizationKey = "/Interactions/AssignOutfitToInteraction";
        
        public static InteractionDefinition GlobalOutfitSingleton = new Definition
            {
                IsGlobal = true
            };

        public static InteractionDefinition PartialOutfitSimSingleton = new Definition
            {
                IsPartial = true,
                TargetIsSim = true
            };

        public static InteractionDefinition PartialOutfitSingleton = new Definition
            {
                IsPartial = true,
            };

        public static InteractionDefinition SimSingleton = new Definition
            {
                TargetIsSim = true
            };

        public static InteractionDefinition Singleton = new Definition();
        
        public delegate bool RunInternalDelegate(bool isGlobal, bool isPartial, Sim actor, GameObject target);

        public static bool HandleGlobalOutfit(Sim actor, RunInternalDelegate runInternalCallback, bool filterHasOutfits = true)
        {
            const string entryKey = "Destrospean/OutfitAssignment/Dialogs/DemographicDialog";
            var ageGenders = new List<CASAgeGenderFlags[]>();
            foreach (var outfitAssignment in OutfitAssignmentUtils.OutfitAssignments)
            {
                if (outfitAssignment.SimDescription != null)
                {
                    continue;
                }
                var ageGenderPrefix = outfitAssignment.SpecialOutfitKey.Substring(OutfitAssignmentUtils.OutfitAssignmentGlobalPrefix.Length, 2);
                var tempAge = CASAgeGenderFlags.None;
                switch (ageGenderPrefix[0])
                {
                    case 'a':
                        tempAge = CASAgeGenderFlags.Adult;
                        break;
                    case 'b':
                        tempAge = CASAgeGenderFlags.Baby;
                        break;
                    case 'c':
                        tempAge = CASAgeGenderFlags.Child;
                        break;
                    case 'e':
                        tempAge = CASAgeGenderFlags.Elder;
                        break;
                    case 'p':
                        tempAge = CASAgeGenderFlags.Toddler;
                        break;
                    case 't':
                        tempAge = CASAgeGenderFlags.Teen;
                        break;
                    case 'y':
                        tempAge = CASAgeGenderFlags.YoungAdult;
                        break;
                }
                var tempGender = CASAgeGenderFlags.None;
                switch (ageGenderPrefix[1])
                {
                    case 'f':
                        tempGender = CASAgeGenderFlags.Female;
                        break;
                    case 'm':
                        tempGender = CASAgeGenderFlags.Male;
                        break;
                }
                ageGenders.Add(new[]
                    {
                        tempAge,
                        tempGender
                    });
            }
            var age = CASAgeGenderFlags.None;
            var gender = CASAgeGenderFlags.None;
            var selectedAge = CASAgeGenderFlags.None;
            var selectedGender = CASAgeGenderFlags.None;
            foreach (var ageGender in ageGenders)
            {
                age |= ageGender[0];
            }
            foreach (var ageGender in ageGenders)
            {
                if ((age & ageGender[0]) == ageGender[0])
                {
                    gender |= ageGender[1];
                }
            }
            sbyte step = 0;
            while (true)
            {
                if (step == 0)
                {
                    if (!ShowCASAgeGenderFlagListDialog(out selectedAge, CASAgeGenderFlags.None, filterHasOutfits ? age : CASAgeGenderFlags.AgeMask ^ CASAgeGenderFlags.Baby ^ CASAgeGenderFlags.YoungAdult, Localization.LocalizeString(entryKey + "/Titles:Age"), entryKey, false))
                    {
                        step--;
                        continue;
                    }
                    step++;
                }
                if (step == 1)
                {
                    if (!ShowCASAgeGenderFlagListDialog(out selectedGender, CASAgeGenderFlags.None, filterHasOutfits ? gender : CASAgeGenderFlags.GenderMask, Localization.LocalizeString(entryKey + "/Titles:Gender"), entryKey, false))
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
                return true;
            }
            var simDescription = Genetics.MakeSim(selectedAge, selectedGender, GameUtils.GetCurrentWorld(), uint.MaxValue);
            EventTracker.AddListener(EventTypeId.kSimInstantiated, evt =>
                {
                    if (evt.TargetObject as Sim != simDescription.CreatedSim)
                    {
                        return ListenerAction.Keep;
                    }
                    var previousGameSpeed = Gameflow.CurrentGameSpeed;
                    ActionTask.Start(() =>
                        {
                            if (runInternalCallback(true, false, simDescription.CreatedSim, simDescription.CreatedSim))
                            {
                                Gameflow.SetGameSpeed(previousGameSpeed, Gameflow.SetGameSpeedContext.GameStates);
                            }
                        });
                    simDescription.CreatedSim?.RemoveFromWorld();
                    AlarmManager.Global.AddAlarm(1, TimeUnit.Minutes, () =>
                        {
                            Household.TouristHousehold.RemoveTemporary(simDescription);
                            simDescription.Dispose();
                        }, "Remove SimDescription: " + simDescription.SimDescriptionId, AlarmType.AlwaysPersisted, simDescription);
                    Gameflow.SetGameSpeed(Sims3.SimIFace.Gameflow.GameSpeed.Pause, Gameflow.SetGameSpeedContext.GameStates);
                    return ListenerAction.Remove;
                });
            Household.CreateTouristHousehold();
            Household.TouristHousehold.AddTemporary(simDescription);
            simDescription.Init(simDescription.GetOutfit(OutfitCategories.Everyday, 0));
            simDescription.Instantiate(actor.LotHome);
            return true;
        }

        public static bool NotifyMultipleOutfitsFound(Sim targetSim, Sim targetOrActor, bool isGlobal)
        {
            Common.Notify(Common.Localize(targetSim == null || targetSim.IsFemale, kLocalizationKey + "/Messages:Multiple" + (isGlobal ? "Global" : "Individual") + "OutfitsForInteractionsFound", targetOrActor), targetSim.GetSimDescription(), StyledNotification.NotificationStyle.kSystemMessage);
            return true;
        }

        public override bool Run()
        {
            var definition = (Definition)InteractionDefinition;
            return definition.IsGlobal ? HandleGlobalOutfit(Actor, RunInternal, false) : RunInternal(definition.IsGlobal, definition.IsPartial, Actor, Target);
        }

        public static bool RunInternal(bool isGlobal, bool isPartial, Sim actor, GameObject target)
        {
            Sim targetSim = isGlobal ? null : target as Sim ?? actor,
            targetOrActor = targetSim ?? actor;
            if (InteractionInstanceTypeUtils.TryGetSelectedInteractionInstanceTypes(out var selectedInteractionInstanceTypes, Array.FindAll(InteractionInstanceTypeUtils.InteractionInstanceTypes, x => !OutfitAssignmentUtils.OutfitAssignments.Exists(y => (y.SimDescription == null && targetSim.GetSimDescription() == null || y.SimDescription != null && targetSim.GetSimDescription() != null && y.SimDescription.SimDescriptionId == targetSim.GetSimDescription().SimDescriptionId) && (targetSim != null || y.SpecialOutfitKey.StartsWith(actor.GetGlobalAssignedOutfitPrefix())) && y.InteractionInstanceType == x.FullName))) && TryGetEntryCallbackType(targetSim, actor, selectedInteractionInstanceTypes[0], out var entryCallbackType) && TryGetExitCallbackType(targetSim, actor, selectedInteractionInstanceTypes[0], out var exitCallbackType))
            {
                var specialOutfitKey = "";
                foreach (var interactionInstanceType in selectedInteractionInstanceTypes)
                {
                    if (targetSim.GetSimDescription().TryGetOutfitAssignment(interactionInstanceType, out var outfitAssignment, actor.SimDescription) && specialOutfitKey != outfitAssignment.SpecialOutfitKey)
                    {
                        if (!string.IsNullOrEmpty(specialOutfitKey))
                        {
                            return NotifyMultipleOutfitsFound(targetSim, targetOrActor, isGlobal);
                        }
                        specialOutfitKey = outfitAssignment.SpecialOutfitKey;
                    }
                }
                specialOutfitKey = string.IsNullOrEmpty(specialOutfitKey) ? (targetSim == null ? actor.GetGlobalAssignedOutfitPrefix() : "OutfitAssignment_") + DownloadContent.GenerateGUID() : specialOutfitKey;
                var isPreexisting = targetSim != null && !isPartial && targetSim.SimDescription.HasSpecialOutfit(specialOutfitKey);
                if (targetSim == null && actor.SimDescription.HasSpecialOutfit(specialOutfitKey) || isPartial && targetOrActor.SimDescription.HasSpecialOutfit(specialOutfitKey))
                {
                    targetOrActor.SimDescription.RemoveSpecialOutfit(specialOutfitKey);
                }
                if ((targetSim == null || isPartial) && OutfitAssignmentUtils.AssignedOutfits.ContainsKey(specialOutfitKey))
                {
                    targetOrActor.AddAssignedOutfit(specialOutfitKey);
                }
                if (!isPreexisting && targetSim != null)
                {
                    var globalSpecialOutfitKey = "";
                    foreach (var interactionInstanceType in selectedInteractionInstanceTypes)
                    {
                        if (targetSim.SimDescription.TryGetGlobalOutfitAssignment(interactionInstanceType, out var outfitAssignment) && globalSpecialOutfitKey != outfitAssignment.SpecialOutfitKey)
                        {
                            if (!string.IsNullOrEmpty(globalSpecialOutfitKey))
                            {
                                globalSpecialOutfitKey = "";
                                break;
                            }
                            globalSpecialOutfitKey = outfitAssignment.SpecialOutfitKey;
                        }
                    }
                    if (!string.IsNullOrEmpty(globalSpecialOutfitKey))
                    {
                        targetSim.AddAssignedOutfit(globalSpecialOutfitKey, specialOutfitKey);
                    }
                }
                if (OutfitExtensions.EditSpecialOutfit(targetOrActor, specialOutfitKey))
                {
                    foreach (var interactionInstanceType in selectedInteractionInstanceTypes)
                    {
                        targetSim.GetSimDescription().AssignOutfitToInteraction(specialOutfitKey, interactionInstanceType, entryCallbackType ?? default, exitCallbackType ?? default, actor.SimDescription);
                    }
                    if ((targetSim == null || isPartial) && OutfitAssignmentUtils.ShowPartOverrideListDialog(OutfitAssignmentUtils.AssignedOutfits[specialOutfitKey] = new OutfitAssignmentUtils.AssignedOutfit(targetOrActor.SimDescription.GetSpecialOutfit(specialOutfitKey)), out var partOverrides, targetSim == null ? null : new BodyTypes[0]))
                    {
                        OutfitAssignmentUtils.AssignedOutfits[specialOutfitKey].PartOverrides = new List<BodyTypes>(partOverrides);
                        if (targetOrActor.SimDescription.HasSpecialOutfit(specialOutfitKey))
                        {
                            targetOrActor.SimDescription.RemoveSpecialOutfit(specialOutfitKey);
                        }
                    }
                }
                else if (!isPreexisting)
                {
                    targetOrActor.SimDescription.RemoveSpecialOutfit(specialOutfitKey);
                }
            }
            else if (isGlobal)
            {
                HandleGlobalOutfit(Sim.ActiveActor, RunInternal, false);
            }
            return true;
        }

        public static bool ShowCASAgeGenderFlagListDialog(out CASAgeGenderFlags flags, CASAgeGenderFlags? preSelectedFlags = null, CASAgeGenderFlags mask = CASAgeGenderFlags.AgeMask | CASAgeGenderFlags.GenderMask, string title = null, string entryKey = null, bool okayButtonAlwaysEnabled = true)
        {
            flags = CASAgeGenderFlags.None;
            var flagList = new List<CASAgeGenderFlags>(Array.FindAll((CASAgeGenderFlags[])Enum.GetValues(typeof(CASAgeGenderFlags)), x => preSelectedFlags.HasValue && (preSelectedFlags & mask & x) == x && x != CASAgeGenderFlags.None && x != CASAgeGenderFlags.AgeMask && x != CASAgeGenderFlags.GenderMask));
            while (true)
            {
                if (flagList.Count == 0 && okayButtonAlwaysEnabled)
                {
                    foreach (CASAgeGenderFlags flag in Enum.GetValues(typeof(CASAgeGenderFlags)))
                    {
                        if ((mask & flag) == flag && flag != CASAgeGenderFlags.None && flag != CASAgeGenderFlags.AgeMask && flag != CASAgeGenderFlags.GenderMask)
                        {
                            flagList.Add(flag);
                        }
                    }
                }
                var selectedFlags = ObjectPickerDialog.Show(title ?? Responder.Instance.LocalizationModel.LocalizeString(entryKey + ":Title"), new List<ObjectPicker.TabInfo>
                    {
                        new ObjectPicker.TabInfo("shop_all_r2", Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:All"), new List<CASAgeGenderFlags>(Array.FindAll((CASAgeGenderFlags[])Enum.GetValues(typeof(CASAgeGenderFlags)), x => (x & mask) == x && x != CASAgeGenderFlags.None && x != CASAgeGenderFlags.AgeMask && x != CASAgeGenderFlags.GenderMask)).ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                    }, okayButtonAlwaysEnabled ? new List<ObjectPickerDialog.CommonHeaderInfo<CASAgeGenderFlags>>
                    {
                        new CASAgeGenderFlagColumn(entryKey),
                        new CASAgeGenderFlagEnabledColumn(entryKey, flagList.ToArray())
                    } : new List<ObjectPickerDialog.CommonHeaderInfo<CASAgeGenderFlags>>
                    {
                        new CASAgeGenderFlagColumn(entryKey),
                    }, 1, out var confirmed, out var cancelled, okayButtonAlwaysEnabled);
                if (cancelled)
                {
                    return false;
                }
                if (confirmed)
                {
                    if (!okayButtonAlwaysEnabled)
                    {
                        flagList.Add(selectedFlags[0]);
                    }
                    foreach (var flag in flagList)
                    {
                        flags |= flag;
                    }
                    return true;
                }
                if (flagList.Contains(selectedFlags[0]))
                {
                    flagList.Remove(selectedFlags[0]);
                }
                else
                {
                    flagList.Add(selectedFlags[0]);
                }
            }
        }

        public static bool TryGetEntryCallbackType(Sim sim, Sim fallbackSim, Type interactionInstanceType, out InteractionInstanceTypeUtils.CallbackTypes? callbackType)
        {
            const string localizationKey = "/Dialogs/EntryCallbackTypeDialog";
            if (ComboSelectionDialog.Show(Common.Localize(sim != null && sim.IsFemale, localizationKey + ":Title"), new SortedDictionary<string, object>(new DummyComparer())
                {
                    {
                        Common.Localize(sim != null && sim.IsFemale, localizationKey + "/Options:InteractionStarted"),
                        nameof(InteractionInstanceTypeUtils.CallbackTypes.InteractionStarted)
                    },
                    {
                        Common.Localize(sim != null && sim.IsFemale, localizationKey + "/Options:StandardEntry"),
                        nameof(InteractionInstanceTypeUtils.CallbackTypes.StandardEntry)
                    },
                    {
                        Common.Localize(sim != null && sim.IsFemale, localizationKey + "/Options:SyncLevelRouted"),
                        nameof(InteractionInstanceTypeUtils.CallbackTypes.SyncLevelRouted)
                    },
                    {
                        Common.Localize(sim != null && sim.IsFemale, localizationKey + "/Options:SyncLevelCommitted"),
                        nameof(InteractionInstanceTypeUtils.CallbackTypes.SyncLevelCommitted)
                    }
                }, sim.GetSimDescription().TryGetOutfitAssignment(interactionInstanceType, out var outfitAssignment, fallbackSim.SimDescription) ? (outfitAssignment.EntryCallbackType == InteractionInstanceTypeUtils.CallbackTypes.OutfitChanged ? InteractionInstanceTypeUtils.CallbackTypes.StandardEntry : outfitAssignment.EntryCallbackType).ToString() : nameof(InteractionInstanceTypeUtils.CallbackTypes.InteractionStarted)) is string text)
            {
                callbackType = (InteractionInstanceTypeUtils.CallbackTypes)Enum.Parse(typeof(InteractionInstanceTypeUtils.CallbackTypes), text);
                return true;
            }
            callbackType = null;
            return false;
        }

        public static bool TryGetExitCallbackType(Sim sim, Sim fallbackSim, Type interactionInstanceType, out InteractionInstanceTypeUtils.CallbackTypes? callbackType)
        {
            const string localizationKey = "/Dialogs/ExitCallbackTypeDialog";
            if (ComboSelectionDialog.Show(Common.Localize(sim != null && sim.IsFemale, localizationKey + ":Title"), new SortedDictionary<string, object>(new DummyComparer())
                {
                    {
                        Common.Localize(sim != null && sim.IsFemale, localizationKey + "/Options:InteractionEnded"),
                        nameof(InteractionInstanceTypeUtils.CallbackTypes.InteractionEnded)
                    },
                    {
                        Common.Localize(sim != null && sim.IsFemale, localizationKey + "/Options:StandardExit"),
                        nameof(InteractionInstanceTypeUtils.CallbackTypes.StandardExit)
                    },
                    {
                        Common.Localize(sim != null && sim.IsFemale, localizationKey + "/Options:SyncLevelCompleted"),
                        nameof(InteractionInstanceTypeUtils.CallbackTypes.SyncLevelCompleted)
                    },
                    {
                        Common.Localize(sim != null && sim.IsFemale, localizationKey + "/Options:Never"),
                        nameof(InteractionInstanceTypeUtils.CallbackTypes.Never)
                    }
                }, sim.GetSimDescription().TryGetOutfitAssignment(interactionInstanceType, out var outfitAssignment, fallbackSim.SimDescription) ? outfitAssignment.ExitCallbackType.ToString() : nameof(InteractionInstanceTypeUtils.CallbackTypes.InteractionEnded)) is string text)
            {
                callbackType = (InteractionInstanceTypeUtils.CallbackTypes)Enum.Parse(typeof(InteractionInstanceTypeUtils.CallbackTypes), text);
                return true;
            }
            callbackType = null;
            return false;
        }
    }
}
