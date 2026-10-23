using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Interfaces.Destrospean.ExpandedHouseholdStaff;
using Sims3.Gameplay.Destrospean.Utils;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using System;
using System.Collections.Generic;
using Destrospean.UI.Columns;

namespace Destrospean.Utils.ExpandedHouseholdStaff
{
    public static class OutfitAssignmentUtils
    {
        [Persistable]
        public class AssignedOutfit
        {
            public List<BodyTypes> PartOverrides = new List<BodyTypes>(OverridableBodyTypes);

            public List<SavedPart> Parts = new List<SavedPart>();

            [Persistable]
            public class SavedPart
            {
                public CASPart Part;

                public string Preset;

                public SavedPart()
                {
                }

                public SavedPart(CASPart part, string preset)
                {
                    Part = part;
                    Preset = preset;
                }
            }

            public AssignedOutfit()
            {
            }

            public AssignedOutfit(AssignedOutfit outfit)
            {
                Parts = outfit.Parts.ConvertAll(x => new SavedPart(x.Part, x.Preset));
                PartOverrides = new List<BodyTypes>(outfit.PartOverrides);
            }

            public AssignedOutfit(SimOutfit outfit)
            {
                Parts = new List<CASPart>(outfit.Parts).ConvertAll(x => new SavedPart(x, outfit.GetPartPreset(x.Key)));
            }
        }

        [Persistable]
        public class Outfit
        {
            public OutfitCategories Category;

            public int Index;

            public SimDescription SimDescription;
        }

        [Persistable]
        public class OutfitAssignment
        {
            public string ServiceName;

            public string SpecialOutfitKey;

            public SimDescription SimDescription;

            public OutfitAssignment()
            {
            }

            public OutfitAssignment(SimDescription simDescription, string specialOutfitKey, IServiceProfile serviceProfile)
            {
                ServiceName = serviceProfile.Name;
                SimDescription = simDescription;
                SpecialOutfitKey = specialOutfitKey;
            }
        }

        static Dictionary<string, OutfitAssignment> sIndexedOutfitAssignments;

        [PersistableStatic(true)]
        public static Dictionary<string, AssignedOutfit> AssignedOutfits = new Dictionary<string, AssignedOutfit>();

        public static Dictionary<string, OutfitAssignment> IndexedOutfitAssignments
        {
            get
            {
                if (sIndexedOutfitAssignments == null)
                {
                    IndexOutfitAssignments();
                }
                return sIndexedOutfitAssignments;
            }
        }

        public const string OutfitAssignmentCategoryPrefix = "ExpandedHouseholdStaff_OutfitAssignment_Category_";

        public const string OutfitAssignmentGlobalPrefix = "ExpandedHouseholdStaff_OutfitAssignment_Global_";

        [PersistableStatic(true)]
        public static List<OutfitAssignment> OutfitAssignments = new List<OutfitAssignment>();

        public static readonly BodyTypes[] OverridableBodyTypes;

        [PersistableStatic(true)]
        public static List<Outfit> PreviousOutfits = new List<Outfit>();

        static OutfitAssignmentUtils()
        {
            List<BodyTypes> overridableBodyTypes = new List<BodyTypes>();
            foreach (BodyTypes bodyType in Enum.GetValues(typeof(BodyTypes)))
            {
                if (bodyType < BodyTypes.PetBody && !overridableBodyTypes.Contains(bodyType))
                {
                    switch (bodyType)
                    {
                        case BodyTypes.AgeWeathering:
                        case BodyTypes.BirthMark:
                        case BodyTypes.Dental:
                        case BodyTypes.EyeColor:
                        case BodyTypes.Face:
                        case BodyTypes.Freckles:
                        case BodyTypes.Moles:
                        case BodyTypes.None:
                        case BodyTypes.Scalp:
                        case BodyTypes.Tattoo:
                        case BodyTypes.TattooTemplate:
                            continue;
                    }
                    overridableBodyTypes.Add(bodyType);
                }
            }
            OverridableBodyTypes = overridableBodyTypes.ToArray();
        }

        public static bool AddAssignedOutfit(this SimDescription simDescription, AssignedOutfit assignedOutfit, string specialOutfitKey)
        {
            if (simDescription.HasSpecialOutfit(specialOutfitKey))
            {
                simDescription.RemoveSpecialOutfit(specialOutfitKey);
            }
            using (SimBuilder simBuilder = new SimBuilder
                {
                    UseCompression = true
                })
            {
                simBuilder.PrepareForOutfit(simDescription.CreatedSim == null ? simDescription.GetOutfit(OutfitCategories.Everyday, 0) : simDescription.CreatedSim.CurrentOutfit);
                foreach (BodyTypes bodyType in OverridableBodyTypes)
                {
                    if (!assignedOutfit.PartOverrides.Contains(bodyType))
                    {
                        continue;
                    }
                    int savedPartIndex = assignedOutfit.Parts.FindIndex(x => x.Part.BodyType == bodyType);
                    if (savedPartIndex == -1)
                    {
                        simBuilder.RemoveParts(bodyType);
                        continue;
                    }
                    AssignedOutfit.SavedPart savedPart = assignedOutfit.Parts[savedPartIndex];
                    switch (bodyType)
                    {
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
                    if (!string.IsNullOrEmpty(savedPart.Preset))
                    {
                        if (CASUtils.ApplyPresetToPart(simBuilder, savedPart.Part, savedPart.Preset))
                        {
                            simBuilder.SetPartPreset(savedPart.Part.Key, null, savedPart.Preset);
                        }
                    }
                    OutfitUtils.AdjustPresetForHairColor(simBuilder, savedPart.Part, simDescription);
                }
                return simDescription.AddSpecialOutfit(new SimOutfit(simBuilder.CacheOutfit(specialOutfitKey + "_" + simDescription.SimDescriptionId)), specialOutfitKey) > -1;
            }
        }

        public static bool AddAssignedOutfit(this Sim sim, string assignedSpecialOutfitKey, string simSpecialOutfitKey = null)
        {
            AssignedOutfit assignedOutfit;
            return AssignedOutfits.TryGetValue(assignedSpecialOutfitKey, out assignedOutfit) && sim.SimDescription.AddAssignedOutfit(assignedOutfit, simSpecialOutfitKey ?? assignedSpecialOutfitKey);
        }

        public static void AssignOutfitToService(this SimDescription simDescription, string specialOutfitKey, IServiceProfile serviceProfile, SimDescription fallbackSimDescription)
        {
            if (simDescription == null)
            {
                fallbackSimDescription.UnassignGlobalOutfitToService(serviceProfile);
            }
            else
            {
                simDescription.UnassignOutfitToService(serviceProfile);
            }
            OutfitAssignments.Add(new OutfitAssignment(simDescription, specialOutfitKey, serviceProfile));
            IndexOutfitAssignments();
        }

        public static void CreateOutfitForCategoryIfNecessary(this SimDescription simDescription, OutfitCategories outfitCategory)
        {
            switch (outfitCategory)
            {
                case OutfitCategories.Singed:
                    BuffSinged.SetupSingedOutfit(simDescription.CreatedSim);
                    break;
                case OutfitCategories.SkinnyDippingTowel:
                    simDescription.RemoveOutfits(OutfitCategories.SkinnyDippingTowel, true);
                    if (simDescription.HasSpecialOutfit("SkinnyDipTowel"))
                    {
                        simDescription.AddOutfit(simDescription.GetSpecialOutfit("SkinnyDipTowel"), OutfitCategories.SkinnyDippingTowel, true);
                    }
                    else
                    {
                        OutfitUtils.CreateOutfitForSim(simDescription, ResourceKey.CreateOutfitKeyFromProductVersion(OutfitUtils.GetGenderPrefix(simDescription.Gender) + OutfitUtils.GetAgePrefix(simDescription.Age, true) + "_towel", ProductVersion.EP3), OutfitCategories.SkinnyDippingTowel, OutfitCategories.Swimwear, true);
                    }
                    break;
            }
        }

        public static OutfitAssignment[] GetAllOutfitAssignments(this SimDescription simDescription)
        {
            return OutfitAssignments.FindAll(x => x.SimDescription == simDescription).ToArray();
        }

        public static string GetGlobalAssignedOutfitPrefix(this Sim sim, bool isCategory = false)
        {
            return sim.SimDescription.GetGlobalAssignedOutfitPrefix(isCategory);
        }

        public static string GetGlobalAssignedOutfitPrefix(this CASAgeGenderFlags ageGender, bool isCategory = false)
        {
            return OutfitAssignmentGlobalPrefix + OutfitUtils.GetAgePrefix(ageGender & CASAgeGenderFlags.AgeMask, true) + OutfitUtils.GetGenderPrefix(ageGender & CASAgeGenderFlags.GenderMask) + (isCategory ? "_Category_" : "_");
        }

        public static string GetGlobalAssignedOutfitPrefix(this SimDescription simDescription, bool isCategory = false)
        {
            return simDescription.AgeGenderSpecies.GetGlobalAssignedOutfitPrefix(isCategory);
        }

        public static void IndexOutfitAssignments()
        {
            sIndexedOutfitAssignments = new Dictionary<string, OutfitAssignment>();
            foreach (OutfitAssignment outfitAssignment in OutfitAssignments)
            {
                sIndexedOutfitAssignments[(outfitAssignment.SimDescription == null ? outfitAssignment.SpecialOutfitKey.Substring(OutfitAssignmentGlobalPrefix.Length, 2) + "_" : "") + outfitAssignment.ServiceName + (outfitAssignment.SimDescription == null ? "" : ("_" + outfitAssignment.SimDescription.SimDescriptionId))] = outfitAssignment;
            }
        }

        public static void RemoveAllOutfitAssignments(this SimDescription simDescription, bool removeSpecialOutfits = false)
        {
            foreach (OutfitAssignment outfitAssignment in new List<OutfitAssignment>(OutfitAssignments))
            {
                if (outfitAssignment.SimDescription == simDescription)
                {
                    OutfitAssignments.Remove(outfitAssignment);
                    if (AssignedOutfits.ContainsKey(outfitAssignment.SpecialOutfitKey))
                    {
                        AssignedOutfits.Remove(outfitAssignment.SpecialOutfitKey);
                    }
                    if (removeSpecialOutfits && simDescription.HasSpecialOutfit(outfitAssignment.SpecialOutfitKey))
                    {
                        simDescription.RemoveSpecialOutfit(outfitAssignment.SpecialOutfitKey);
                    }
                }
            }
            IndexOutfitAssignments();
        }

        public static bool ShowPartOverrideListDialog(AssignedOutfit assignedOutfit, out BodyTypes[] partOverrides, BodyTypes[] preSelectedPartOverrides = null)
        {
            bool retVal;
            BodyTypes[] tempPartOverrides = null;
            if (DebugUtils.TryDisplayScriptError(() =>
                {
                    string entryKey = typeof(UI.Dialogs.ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "PartOverrideListDialog");
                    List<BodyTypes> partOverrideList = new List<BodyTypes>(preSelectedPartOverrides ?? assignedOutfit.PartOverrides.ToArray());
                    bool cancelled, confirmed;
                    while (true)
                    {
                        List<BodyTypes> selectedPartOverrides = UI.Dialogs.ObjectPickerDialog.Show(Responder.Instance.LocalizationModel.LocalizeString(entryKey + ":Title"), new List<ObjectPicker.TabInfo>
                            {
                                new ObjectPicker.TabInfo("shop_all_r2", Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:All"), new List<BodyTypes>(OverridableBodyTypes).ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                            }, new List<UI.Dialogs.ObjectPickerDialog.CommonHeaderInfo<BodyTypes>>
                            {
                                new BodyTypeColumn(entryKey),
                                new BodyTypeEnabledColumn(entryKey, partOverrideList.ToArray())
                            }, 1, out confirmed, out cancelled, true);
                        if (cancelled)
                        {
                            tempPartOverrides = null;
                            return false;
                        }
                        if (confirmed)
                        {
                            tempPartOverrides = partOverrideList.ToArray();
                            return true;
                        }
                        if (partOverrideList.Contains(selectedPartOverrides[0]))
                        {
                            partOverrideList.Remove(selectedPartOverrides[0]);
                        }
                        else
                        {
                            partOverrideList.Add(selectedPartOverrides[0]);
                        }
                    }
                }, out retVal))
            {
                partOverrides = null;
                return false;
            }
            partOverrides = tempPartOverrides;
            return retVal;
        }

        public static void SwitchToAssignedOutfit(this Sim sim, OutfitAssignment outfitAssignment, bool spin = true)
        {
            if (sim.BuffManager.HasElement(BuffNames.Singed) || sim.BuffManager.HasElement(BuffNames.SingedElectricity) || sim.BuffManager.HasElement(BuffNames.EmbarrassedClothesHidden) || sim.BuffManager.DisallowClothesChange() || sim.OccultManager.DisallowClothesChange())
            {
                return;
            }
            OutfitCategories outfitCategory;
            int outfitIndex;
            string categoryForGlobalKey = null;
            if (outfitAssignment.SpecialOutfitKey.StartsWith(OutfitAssignmentCategoryPrefix) || outfitAssignment.SpecialOutfitKey.StartsWith(categoryForGlobalKey = sim.GetGlobalAssignedOutfitPrefix(true)))
            {
                if ((outfitCategory = (OutfitCategories)Enum.Parse(typeof(OutfitCategories), outfitAssignment.SpecialOutfitKey.Substring((categoryForGlobalKey ?? OutfitAssignmentCategoryPrefix).Length))) == 0)
                {
                    return;
                }
                outfitIndex = 0;
            }
            else
            {
                if (AssignedOutfits.ContainsKey(outfitAssignment.SpecialOutfitKey))
                {
                    sim.AddAssignedOutfit(outfitAssignment.SpecialOutfitKey);
                }
                outfitCategory = OutfitCategories.Special;
                outfitIndex = sim.SimDescription.GetSpecialOutfitIndexFromKey(ResourceUtils.HashString32(outfitAssignment.SpecialOutfitKey));
            }
            sim.SimDescription.CreateOutfitForCategoryIfNecessary(outfitCategory);
            if (spin && !(sim.Posture is SittingPosture))
            {
                sim.SwitchToOutfitWithSpin(outfitCategory, outfitIndex);
            }
            else
            {
                sim.SwitchToOutfitWithoutSpin(outfitCategory, outfitIndex);
            }
        }

        public static void SwitchToPreviousOutfit(this Sim sim, bool spin = true)
        {
            int previousOutfitIndex = PreviousOutfits.FindIndex(x => x.SimDescription == sim.SimDescription);
            if (previousOutfitIndex > -1)
            {
                if (!sim.BuffManager.HasElement(BuffNames.Singed) && !sim.BuffManager.HasElement(BuffNames.SingedElectricity) && !sim.BuffManager.HasElement(BuffNames.EmbarrassedClothesHidden) && !sim.BuffManager.DisallowClothesChange() && !sim.OccultManager.DisallowClothesChange())
                {
                    if (spin && !(sim.Posture is SittingPosture))
                    {
                        sim.SwitchToOutfitWithSpin(PreviousOutfits[previousOutfitIndex].Category, PreviousOutfits[previousOutfitIndex].Index);
                    }
                    else
                    {
                        sim.SwitchToOutfitWithoutSpin(PreviousOutfits[previousOutfitIndex].Category, PreviousOutfits[previousOutfitIndex].Index);
                    }
                }
                PreviousOutfits.RemoveAt(previousOutfitIndex);
            }
        }

        public static bool TryGetGlobalOutfitAssignment(this SimDescription simDescription, IServiceProfile serviceProfile, out OutfitAssignment outfitAssignment)
        {
            return IndexedOutfitAssignments.TryGetValue(OutfitUtils.GetAgePrefix(simDescription.Age, true) + OutfitUtils.GetGenderPrefix(simDescription.Gender) + "_" + serviceProfile.Name, out outfitAssignment);
        }

        public static bool TryGetOutfitAssignment(this SimDescription simDescription, IServiceProfile serviceProfile, out OutfitAssignment outfitAssignment, SimDescription fallbackSimDescription = null)
        {
            return simDescription == null ? fallbackSimDescription.TryGetGlobalOutfitAssignment(serviceProfile, out outfitAssignment) : IndexedOutfitAssignments.TryGetValue(serviceProfile.Name + "_" + simDescription.SimDescriptionId, out outfitAssignment);
        }

        public static void UnassignGlobalOutfitToService(this SimDescription simDescription, IServiceProfile serviceProfile)
        {
            OutfitAssignment outfitAssignment;
            if (simDescription.TryGetGlobalOutfitAssignment(serviceProfile, out outfitAssignment))
            {
                OutfitAssignments.Remove(outfitAssignment);
                IndexOutfitAssignments();
            }
        }

        public static void UnassignOutfitToService(this SimDescription simDescription, IServiceProfile serviceProfile)
        {
            OutfitAssignment outfitAssignment;
            if (simDescription.TryGetOutfitAssignment(serviceProfile, out outfitAssignment))
            {
                OutfitAssignments.Remove(outfitAssignment);
                IndexOutfitAssignments();
            }
        }
    }
}
