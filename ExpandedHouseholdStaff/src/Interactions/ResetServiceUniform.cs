using System;
using System.Collections.Generic;
using Destrospean.Utils;
using Destrospean.Utils.ExpandedHouseholdStaff;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Destrospean.Utils;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using ObjectPickerDialog = Destrospean.UI.Dialogs.ObjectPickerDialog;

namespace Destrospean.ExpandedHouseholdStaff.Interactions
{
    public class ResetServiceUniform : ImmediateInteraction<Sim, GameObject>
    {
        [DoesntRequireTuning]
        public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ResetServiceUniform>
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
                return !isAutonomous && ServiceUtils.ServiceProfiles.Exists(x => !x.IsImmutable && OutfitAssignmentUtils.OutfitAssignments.Exists(y => y.ServiceName == x.Name && (y.SimDescription == null || y.SimDescription == actor.SimDescription)));
            }
        }

        public static readonly string LocalizationKey = typeof(ResetServiceUniform).GetLocalizationKey();

        public static InteractionDefinition Singleton = new Definition();

        public override bool Run()
        {
            return !DebugUtils.TryDisplayScriptError(() =>
                {
                    while (true)
                    {
                        if (ServiceUtils.TryUIGetSelectedServiceProfiles(out var profiles, ServiceUtils.ServiceProfiles.FindAll(x => !x.IsImmutable && OutfitAssignmentUtils.OutfitAssignments.Exists(y => y.ServiceName == x.Name && (y.SimDescription == null || y.SimDescription == Actor.SimDescription))).ToArray(), Localization.LocalizeString(LocalizationKey + ":Name"), 1))
                        {
                            var entryKey = typeof(ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "ServiceOutfitDemographicDialog");
                            var ageGenders = new List<CASAgeGenderFlags[]>();
                            foreach (var outfitAssignment in OutfitAssignmentUtils.OutfitAssignments)
                            {
                                if (Array.Exists(profiles, x => x.Name == outfitAssignment.ServiceName))
                                {
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
                            }
                            var age = CASAgeGenderFlags.None;
                            foreach (var ageGender in ageGenders)
                            {
                                age |= ageGender[0];
                            }
                            var gender = CASAgeGenderFlags.None;
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
                                    if (!CommonUtils.ShowCASAgeGenderFlagListDialog(out age, CASAgeGenderFlags.None, age, Localization.LocalizeString(entryKey + "/Titles:Age"), entryKey, false))
                                    {
                                        step--;
                                        continue;
                                    }
                                    step++;
                                }
                                if (step == 1)
                                {
                                    if (!CommonUtils.ShowCASAgeGenderFlagListDialog(out gender, CASAgeGenderFlags.None, gender, Localization.LocalizeString(entryKey + "/Titles:Gender"), entryKey, false))
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
                            foreach (var profile in profiles)
                            {
                                var specialOutfitKey = (age | gender).GetGlobalAssignedOutfitPrefix() + profile.Name;
                                OutfitAssignmentUtils.AssignedOutfits.Remove(specialOutfitKey);
                                OutfitAssignmentUtils.OutfitAssignments.RemoveAll(x => x.SimDescription == null && x.SpecialOutfitKey == specialOutfitKey);
                                OutfitAssignmentUtils.IndexOutfitAssignments();
                            }
                        }
                        break;
                    }
                });
        }
    }
}
