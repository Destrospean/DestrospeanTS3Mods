extern alias OutfitAssignment;
using System;
using System.Collections.Generic;
using Destrospean.Enums.HairTrouble;
using Destrospean.HairTrouble;
using Destrospean.UI.Dialogs;
using OutfitAssignment::Destrospean.Misc;
using OutfitAssignment::Destrospean.OutfitAssignment;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using static Sims3.Gameplay.Gameflow;
using AssignOutfitToInteraction_Original = OutfitAssignment::Destrospean.OutfitAssignment.Interactions.AssignOutfitToInteraction;
using EditAssignedOutfit_Original = OutfitAssignment::Destrospean.OutfitAssignment.Interactions.EditAssignedOutfit;

namespace Destrospean.Utils.HairTrouble
{
    public static class OutfitAssignmentIntegration
    {
        public class AssignOutfitToInteraction : AssignOutfitToInteraction_Original
        {
            public new class Definition : ImmediateInteractionDefinition<Sim, GameObject, AssignOutfitToInteraction>
            {
                public bool IsGlobal;

                public bool IsPartial;

                public bool TargetIsSim;

                public override string GetInteractionName(Sim actor, GameObject target, Sims3.Gameplay.Autonomy.InteractionObjectPair iop)
                {
                    var targetSim = target as Sim;
                    return Common.Localize(targetSim != null && targetSim.IsFemale, sLocalizationKey + "/Names:" + (IsPartial ? "Partial" : "Full"));
                }

                public override string[] GetPath(bool isFemale)
                {
                    var basePath = Common.Localize(isFemale, sLocalizationKey + "/Paths:Base");
                    return TargetIsSim ? new[]
                        {
                            basePath
                        } : new[]
                        {
                            basePath,
                            Common.Localize(isFemale, sLocalizationKey + "/Paths:" + (IsGlobal ? "Global" : "Individual"))
                        };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    var targetSim = target as Sim;
                    return targetSim == null || targetSim.SimDescription != null && targetSim.IsHuman;
                }
            }
            
            public new static InteractionDefinition GlobalOutfitSingleton = new Definition
                {
                    IsGlobal = true
                };

            public new static InteractionDefinition PartialOutfitSimSingleton = new Definition
                {
                    IsPartial = true,
                    TargetIsSim = true
                };

            public new static InteractionDefinition PartialOutfitSingleton = new Definition
                {
                    IsPartial = true,
                };

            public new static InteractionDefinition SimSingleton = new Definition
                {
                    TargetIsSim = true
                };

            public new static InteractionDefinition Singleton = new Definition();
            
            public override bool Run()
            {
                var definition = (Definition)InteractionDefinition;
                if (definition.IsGlobal)
                {
                    return OutfitAssignmentIntegration.HandleGlobalOutfit(Actor, RunInternal, false);
                }
                return RunInternal(definition.IsGlobal, definition.IsPartial, Actor, Target, out _);
            }
            
            public static bool RunInternal(bool isGlobal, bool isPartial, Sim actor, GameObject target, out string specialOutfitKey)
            {
                specialOutfitKey = "";
                var targetSim = isGlobal ? null : target as Sim ?? actor;
                var targetOrActor = targetSim ?? actor;
                if (InteractionInstanceTypeUtils.TryGetSelectedInteractionInstanceTypes(out var selectedInteractionInstanceTypes, Array.FindAll(InteractionInstanceTypeUtils.InteractionInstanceTypes, x => !OutfitAssignmentUtils.OutfitAssignments.Exists(y => (y.SimDescription == null && targetSim.GetSimDescription() == null || y.SimDescription != null && targetSim.GetSimDescription() != null && y.SimDescription.SimDescriptionId == targetSim.GetSimDescription().SimDescriptionId) && (targetSim != null || y.SpecialOutfitKey.StartsWith(actor.GetGlobalAssignedOutfitPrefix())) && y.InteractionInstanceType == x.FullName))) && TryGetEntryCallbackType(targetSim, actor, selectedInteractionInstanceTypes[0], out var entryCallbackType) && TryGetExitCallbackType(targetSim, actor, selectedInteractionInstanceTypes[0], out var exitCallbackType))
                {
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
                    specialOutfitKey = string.IsNullOrEmpty(specialOutfitKey) ? (targetSim == null ? actor.GetGlobalAssignedOutfitPrefix() : "OutfitAssignment_") + Sims3.SimIFace.CustomContent.DownloadContent.GenerateGUID() : specialOutfitKey;
                    var isPreexisting = targetSim != null && !isPartial && targetSim.SimDescription.HasSpecialOutfit(specialOutfitKey);
                    if (targetSim == null && actor.SimDescription.HasSpecialOutfit(specialOutfitKey) || isPartial && (targetSim?.SimDescription.HasSpecialOutfit(specialOutfitKey) ?? false))
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
                        AssignHairstyleToGrowthState(targetSim, specialOutfitKey, isPartial);
                    }
                    else if (!isPreexisting)
                    {
                        targetOrActor.SimDescription.RemoveSpecialOutfit(specialOutfitKey);
                    }
                }
                else if (isGlobal)
                {
                    OutfitAssignmentIntegration.HandleGlobalOutfit(Sim.ActiveActor, RunInternal, false);
                }
                return true;
            }
        }

        public class EditAssignedOutfit : EditAssignedOutfit_Original
        {
            public new class Definition : ImmediateInteractionDefinition<Sim, GameObject, EditAssignedOutfit>
            {
                public bool IsGlobal;

                public bool TargetIsSim;

                public override string GetInteractionName(Sim actor, GameObject target, Sims3.Gameplay.Autonomy.InteractionObjectPair iop)
                {
                    var targetSim = IsGlobal ? null : target as Sim ?? actor;
                    return Common.Localize(targetSim != null && targetSim.IsFemale, sLocalizationKey + ":Name");
                }

                public override string[] GetPath(bool isFemale)
                {
                    var basePath = Common.Localize(isFemale, sLocalizationKey + "/Paths:Base");
                    return TargetIsSim ? new[]
                        {
                            basePath
                        } : new[]
                        {
                            basePath,
                            Common.Localize(isFemale, sLocalizationKey + "/Paths:" + (IsGlobal ? "Global" : "Individual"))
                        };
                }

                public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
                {
                    var targetSim = IsGlobal ? null : target as Sim ?? actor;
                    return Array.Exists(targetSim.GetSimDescription().GetAllOutfitAssignments(), x => !x.SpecialOutfitKey.StartsWith(OutfitAssignmentUtils.OutfitAssignmentCategoryPrefix) /*&& (targetSim != null || x.SpecialOutfitKey.StartsWith(actor.GetGlobalAssignedOutfitPrefix()))*/);
                }
            }
            
            public new static InteractionDefinition GlobalOutfitSingleton = new Definition
                {
                    IsGlobal = true
                };

            public new static InteractionDefinition SimSingleton = new Definition
                {
                    TargetIsSim = true
                };

            public new static InteractionDefinition Singleton = new Definition();

            public override bool Run()
            {
                var definition = (Definition)InteractionDefinition;
                return definition.IsGlobal ? HandleGlobalOutfit(Actor, RunInternal) : RunInternal(definition.IsGlobal, false, Actor, Target, out _);
            }

            public static bool RunInternal(bool isGlobal, bool isPartial, Sim actor, GameObject target, out string specialOutfitKey)
            {
                specialOutfitKey = "";
                var targetSim = isGlobal ? null : target as Sim ?? actor;
                var targetOrActor = targetSim ?? actor;
                if (InteractionInstanceTypeUtils.TryGetSelectedInteractionInstanceTypes(out var selectedInteractionInstanceTypes, Array.ConvertAll(Array.FindAll(targetSim.GetSimDescription().GetAllOutfitAssignments(), x => (targetSim != null || x.SpecialOutfitKey.StartsWith(actor.GetGlobalAssignedOutfitPrefix())) && !x.SpecialOutfitKey.StartsWith(OutfitAssignmentUtils.OutfitAssignmentCategoryPrefix)), x => Array.Find(InteractionInstanceTypeUtils.InteractionInstanceTypes, y => y.FullName == x.InteractionInstanceType))))
                {
                    OutfitAssignmentUtils.OutfitAssignment outfitAssignment = null;
                    foreach (var interactionInstanceType in selectedInteractionInstanceTypes)
                    {
                        if (targetSim.GetSimDescription().TryGetOutfitAssignment(interactionInstanceType, out var tempOutfitAssignment, actor.SimDescription) && specialOutfitKey != tempOutfitAssignment.SpecialOutfitKey)
                        {
                            if (!string.IsNullOrEmpty(specialOutfitKey))
                            {
                                return AssignOutfitToInteraction_Original.NotifyMultipleOutfitsFound(targetSim, targetOrActor, isGlobal);
                            }
                            outfitAssignment = tempOutfitAssignment;
                            specialOutfitKey = tempOutfitAssignment.SpecialOutfitKey;
                        }
                    }
                    isPartial = targetSim != null && OutfitAssignmentUtils.AssignedOutfits.ContainsKey(outfitAssignment?.SpecialOutfitKey ?? "");
                    if (targetSim == null && actor.SimDescription.HasSpecialOutfit(specialOutfitKey) || isPartial && targetSim.SimDescription.HasSpecialOutfit(specialOutfitKey))
                    {
                        targetOrActor.SimDescription.RemoveSpecialOutfit(specialOutfitKey);
                    }
                    if ((targetSim == null || isPartial) && OutfitAssignmentUtils.AssignedOutfits.ContainsKey(specialOutfitKey))
                    {
                        targetOrActor.AddAssignedOutfit(specialOutfitKey);
                    }
                    if (OutfitExtensions.EditSpecialOutfit(targetOrActor, specialOutfitKey))
                    {
                        foreach (var interactionInstanceType in selectedInteractionInstanceTypes)
                        {
                            targetSim.GetSimDescription().AssignOutfitToInteraction(specialOutfitKey, interactionInstanceType, outfitAssignment?.EntryCallbackType ?? default, outfitAssignment?.ExitCallbackType ?? default, actor.SimDescription);
                        }
                        if (targetSim == null || isPartial)
                        {
                            OutfitAssignmentUtils.AssignedOutfits[specialOutfitKey] = new OutfitAssignmentUtils.AssignedOutfit(targetOrActor.SimDescription.GetSpecialOutfit(specialOutfitKey))
                                {
                                    PartOverrides = OutfitAssignmentUtils.AssignedOutfits[specialOutfitKey].PartOverrides
                                };
                        }
                        AssignHairstyleToGrowthState(targetSim, specialOutfitKey, isPartial);
                    }
                    if ((targetSim == null || isPartial) && targetOrActor.SimDescription.HasSpecialOutfit(specialOutfitKey))
                    {
                        targetOrActor.SimDescription.RemoveSpecialOutfit(specialOutfitKey);
                    }
                }
                else if (isGlobal)
                {
                    HandleGlobalOutfit(Sim.ActiveActor, RunInternal);
                }
                return true;
            }
        }

        public delegate bool RunInternalDelegate(bool isGlobal, bool isPartial, Sim actor, GameObject target, out string specialOutfitKey);
        
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
                                if (string.IsNullOrEmpty(assignedSpecialOutfitKey) || !SimHairGrowth.PreferredHairstylesBySpecialOutfitandGrowthState.TryGetValue(assignedSpecialOutfitKey, out var outfitHairData) || !outfitHairData.PreferredHairstylesByGrowthState.TryGetValue(hairGrowthState, out var hairstyle))
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
        
        public static void AssignHairstyleToGrowthState(Sim sim, string specialOutfitKey, bool isPartial)
        {
            if (!isPartial || !sim.SimDescription.TryGetHairGrowthState(out var hairGrowthState))
            {
                return;
            }
            if (!SimHairGrowth.PreferredHairstylesBySpecialOutfitandGrowthState.TryGetValue(specialOutfitKey, out var outfitHairData))
            {
                outfitHairData = SimHairGrowth.PreferredHairstylesBySpecialOutfitandGrowthState[specialOutfitKey] = new SimHairGrowth.OutfitHairData(0uL, ResourceKey.kInvalidResourceKey, OutfitCategories.None, 0);
            }
            var savedPart = OutfitAssignmentUtils.AssignedOutfits[specialOutfitKey].Parts.Find(x => x.Part.BodyType == BodyTypes.Hair);
            outfitHairData.PreferredHairstylesByGrowthState[hairGrowthState] = savedPart.Part;
            outfitHairData.PreferredHatPresetsByGrowthState[hairGrowthState] = savedPart.Preset;
        }
        
        public static bool HandleGlobalOutfit(Sim actor, RunInternalDelegate runInternalCallback, bool filterHasOutfits = true)
        {
            const string entryKey = "Destrospean/OutfitAssignment/Dialogs/DemographicDialog";
            var ageGenders = new List<CASAgeGenderFlags[]>();
            foreach (var tempOutfitAssignment in OutfitAssignmentUtils.OutfitAssignments)
            {
                if (tempOutfitAssignment.SimDescription != null)
                {
                    continue;
                }
                var ageGenderPrefix = tempOutfitAssignment.SpecialOutfitKey.Substring(OutfitAssignmentUtils.OutfitAssignmentGlobalPrefix.Length, 2);
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
            var selectedHairGrowthState = HairGrowthStates.Unknown;
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
                    if (!CASAgeGenderFlagListDialog.ShowCASAgeGenderFlagListDialog(out selectedAge, CASAgeGenderFlags.None, filterHasOutfits ? age : CASAgeGenderFlags.AgeMask ^ CASAgeGenderFlags.Baby ^ CASAgeGenderFlags.YoungAdult, Localization.LocalizeString(entryKey + "/Titles:Age"), entryKey, false))
                    {
                        step--;
                        continue;
                    }
                    step++;
                }
                if (step == 1)
                {
                    if (!CASAgeGenderFlagListDialog.ShowCASAgeGenderFlagListDialog(out selectedGender, CASAgeGenderFlags.None, filterHasOutfits ? gender : CASAgeGenderFlags.GenderMask, Localization.LocalizeString(entryKey + "/Titles:Gender"), entryKey, false))
                    {
                        step--;
                        continue;
                    }
                    step++;
                }
                if (step == 2)
                {
                    if (!SimHairGrowth.TryUIGetHairGrowthState(out selectedHairGrowthState))
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
                    simDescription.FirstName = "HairTrouble_TempSim";
                    simDescription.SetHairGrowthState(selectedHairGrowthState);
                    var previousGameSpeed = CurrentGameSpeed;
                    ActionTask.Start(() =>
                        {
                            if (runInternalCallback(true, false, simDescription.CreatedSim, simDescription.CreatedSim, out var specialOutfitKey))
                            {
                                if (!SimHairGrowth.PreferredHairstylesBySpecialOutfitandGrowthState.TryGetValue(specialOutfitKey, out var outfitHairData))
                                {
                                    outfitHairData = SimHairGrowth.PreferredHairstylesBySpecialOutfitandGrowthState[specialOutfitKey] = new SimHairGrowth.OutfitHairData(0uL, ResourceKey.kInvalidResourceKey, OutfitCategories.None, 0);
                                }
                                var savedPart = OutfitAssignmentUtils.AssignedOutfits[specialOutfitKey].Parts.Find(x => x.Part.BodyType == BodyTypes.Hair);
                                outfitHairData.PreferredHairstylesByGrowthState[selectedHairGrowthState] = savedPart.Part;
                                outfitHairData.PreferredHatPresetsByGrowthState[selectedHairGrowthState] = savedPart.Preset;
                                SetGameSpeed(previousGameSpeed, SetGameSpeedContext.GameStates);
                            }
                        });
                    simDescription.CreatedSim?.RemoveFromWorld();
                    AlarmManager.Global.AddAlarm(1f, TimeUnit.Minutes, () =>
                        {
                            Household.TouristHousehold.RemoveTemporary(simDescription);
                            simDescription.Dispose();
                        }, "Remove SimDescription: " + simDescription.SimDescriptionId, AlarmType.AlwaysPersisted, simDescription);
                    SetGameSpeed(Gameflow.GameSpeed.Pause, SetGameSpeedContext.GameStates);
                    return ListenerAction.Remove;
                });
            Household.CreateTouristHousehold();
            Household.TouristHousehold.AddTemporary(simDescription);
            simDescription.Init(simDescription.GetOutfit(OutfitCategories.Everyday, 0));
            simDescription.Instantiate(actor.LotHome);
            return true;
        }

        public static void Init()
        {
            CommonUtils.ReplaceMethod(typeof(OutfitAssignmentUtils).GetMethod(nameof(AddAssignedOutfit), new[]
                {
                    typeof(SimDescription),
                    typeof(OutfitAssignmentUtils.AssignedOutfit),
                    typeof(string)
                }), typeof(OutfitAssignmentIntegration).GetMethod(nameof(AddAssignedOutfit)));
            LoadSaveManager.ObjectGroupsPreLoad += () =>
                {
                    AssignOutfitToInteraction_Original.GlobalOutfitSingleton = AssignOutfitToInteraction.GlobalOutfitSingleton;
                    AssignOutfitToInteraction_Original.PartialOutfitSingleton = AssignOutfitToInteraction.PartialOutfitSingleton;
                    AssignOutfitToInteraction_Original.PartialOutfitSimSingleton = AssignOutfitToInteraction.PartialOutfitSimSingleton;
                    AssignOutfitToInteraction_Original.SimSingleton = AssignOutfitToInteraction.SimSingleton;
                    AssignOutfitToInteraction_Original.Singleton = AssignOutfitToInteraction.Singleton;
                    EditAssignedOutfit_Original.GlobalOutfitSingleton = EditAssignedOutfit.GlobalOutfitSingleton;
                    EditAssignedOutfit_Original.SimSingleton = EditAssignedOutfit.SimSingleton;
                    EditAssignedOutfit_Original.Singleton = EditAssignedOutfit.Singleton;
                };
        }
    }
}
