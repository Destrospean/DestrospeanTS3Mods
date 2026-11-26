using System;
using System.Collections.Generic;
using Destrospean.OutfitAssignment.UI.Columns;
using Destrospean.Utils;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using ObjectPickerDialog = Destrospean.OutfitAssignment.UI.Dialogs.ObjectPickerDialog;
using Tuning = Sims3.Gameplay.Destrospean.OutfitAssignment;

namespace Destrospean.OutfitAssignment
{
    public static class OutfitAssignmentUtils
    {
        [Persistable]
        public class AssignedOutfit
        {
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
            
            public List<BodyTypes> PartOverrides = new List<BodyTypes>(OverridableBodyTypes);

            public List<SavedPart> Parts;
            
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
            public InteractionInstanceTypeUtils.CallbackTypes EntryCallbackType;
            
            public InteractionInstanceTypeUtils.CallbackTypes ExitCallbackType;
            
            public string InteractionInstanceType;
            
            public SimDescription SimDescription;
            
            public string SpecialOutfitKey;

            public OutfitAssignment()
            {
            }

            public OutfitAssignment(SimDescription simDescription, string specialOutfitKey, Type interactionInstanceType, InteractionInstanceTypeUtils.CallbackTypes entryCallbackType, InteractionInstanceTypeUtils.CallbackTypes exitCallbackType)
            {
                EntryCallbackType = entryCallbackType;
                ExitCallbackType = exitCallbackType;
                InteractionInstanceType = interactionInstanceType.FullName;
                SimDescription = simDescription;
                SpecialOutfitKey = specialOutfitKey;
            }
        }
        
        private static Dictionary<string, OutfitAssignment> sIndexedOutfitAssignments;

        public static Dictionary<string, AssignedOutfit> AssignedOutfits => (Dictionary<string, AssignedOutfit>)typeof(OutfitAssignmentUtils).GetField("GlobalOutfits").GetValue(null);

        [Obsolete("Use `AssignedOutfits` instead", true)]
        [PersistableStatic(true)]
        public static Dictionary<string, AssignedOutfit> GlobalOutfits = new Dictionary<string, AssignedOutfit>();

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

        public const string OutfitAssignmentCategoryPrefix = "OutfitAssignment_Category_";

        public const string OutfitAssignmentGlobalPrefix = "OutfitAssignment_Global_";

        [PersistableStatic(true)]
        public static List<OutfitAssignment> OutfitAssignments = new List<OutfitAssignment>();

        public static readonly BodyTypes[] OverridableBodyTypes;

        [PersistableStatic(true)]
        public static List<Outfit> PreviousOutfits = new List<Outfit>();

        public static List<SimDescription> TimeToChangeBackList = new List<SimDescription>();

        static OutfitAssignmentUtils()
        {
            var overridableBodyTypes = new List<BodyTypes>();
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
            using (var simBuilder = new SimBuilder())
            {
                simBuilder.UseCompression = true;
                simBuilder.PrepareForOutfit(simDescription.CreatedSim == null ? simDescription.GetOutfit(OutfitCategories.Everyday, 0) : simDescription.CreatedSim.CurrentOutfit);
                foreach (var bodyType in OverridableBodyTypes)
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

        public static bool AddAssignedOutfit(this Sim sim, string assignedSpecialOutfitKey, string simSpecialOutfitKey = null)
        {
            return AssignedOutfits.TryGetValue(assignedSpecialOutfitKey, out var assignedOutfit) && sim.SimDescription.AddAssignedOutfit(assignedOutfit, simSpecialOutfitKey ?? assignedSpecialOutfitKey);
        }

        public static void AssignOutfitToInteraction(this SimDescription simDescription, string specialOutfitKey, Type interactionInstanceType, InteractionInstanceTypeUtils.CallbackTypes entryCallbackType, InteractionInstanceTypeUtils.CallbackTypes exitCallbackType, SimDescription fallbackSimDescription)
        {
            if (simDescription == null)
            {
                fallbackSimDescription.UnassignGlobalOutfitToInteraction(interactionInstanceType);
            }
            else
            {
                simDescription.UnassignOutfitToInteraction(interactionInstanceType);
            }
            OutfitAssignments.Add(new OutfitAssignment(simDescription, specialOutfitKey, interactionInstanceType, entryCallbackType, exitCallbackType));
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
            return OutfitAssignments.FindAll(x => simDescription == null && x.SimDescription == null || simDescription != null && x.SimDescription != null && simDescription.SimDescriptionId == x.SimDescription.SimDescriptionId).ToArray();
        }

        public static string GetGlobalAssignedOutfitPrefix(this Sim sim, bool isCategory = false)
        {
            return OutfitAssignmentGlobalPrefix + OutfitUtils.GetAgePrefix(sim.SimDescription.Age, true) + OutfitUtils.GetGenderPrefix(sim.SimDescription.Gender) + (isCategory ? "_Category_" : "_");
        }

        public static void IndexOutfitAssignments()
        {
            sIndexedOutfitAssignments = new Dictionary<string, OutfitAssignment>();
            foreach (var outfitAssignment in OutfitAssignments)
            {
                sIndexedOutfitAssignments[(outfitAssignment.SimDescription == null ? outfitAssignment.SpecialOutfitKey.Substring(OutfitAssignmentGlobalPrefix.Length, 2) + "_" : "") + outfitAssignment.InteractionInstanceType + (outfitAssignment.SimDescription == null ? "" : "_" + outfitAssignment.SimDescription.SimDescriptionId)] = outfitAssignment;
            }
        }

        public static void RemoveAllOutfitAssignments(this SimDescription simDescription, bool removeSpecialOutfits = false)
        {
            foreach (var outfitAssignment in new List<OutfitAssignment>(OutfitAssignments))
            {
                if (simDescription == null && outfitAssignment.SimDescription == null || simDescription != null && outfitAssignment.SimDescription != null && outfitAssignment.SimDescription.SimDescriptionId == simDescription.SimDescriptionId)
                {
                    OutfitAssignments.Remove(outfitAssignment);
                    AssignedOutfits.Remove(outfitAssignment.SpecialOutfitKey);
                    if (removeSpecialOutfits && simDescription != null && simDescription.HasSpecialOutfit(outfitAssignment.SpecialOutfitKey))
                    {
                        simDescription.RemoveSpecialOutfit(outfitAssignment.SpecialOutfitKey);
                    }
                }
            }
            IndexOutfitAssignments();
        }

        public static bool ShowPartOverrideListDialog(AssignedOutfit assignedOutfit, out BodyTypes[] partOverrides, BodyTypes[] preSelectedPartOverrides = null)
        {
            try
            {
                const string localizationPath = Common.LocalizationPath + "/Dialogs/PartOverrideListDialog";
                var partOverrideList = new List<BodyTypes>(preSelectedPartOverrides ?? assignedOutfit.PartOverrides.ToArray());
                while (true)
                {
                    var selectedPartOverrides = ObjectPickerDialog.Show(Responder.Instance.LocalizationModel.LocalizeString(localizationPath + ":Title"), new List<ObjectPicker.TabInfo>
                        {
                            new ObjectPicker.TabInfo("shop_all_r2", Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:All"), new List<BodyTypes>(OverridableBodyTypes).ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                        }, new List<ObjectPickerDialog.CommonHeaderInfo<BodyTypes>>
                        {
                            new BodyTypeColumn(localizationPath),
                            new PartOverrideEnabledColumn(localizationPath, partOverrideList.ToArray())
                        }, 1, out var confirmed, out var cancelled, true);
                    if (cancelled)
                    {
                        partOverrides = null;
                        return false;
                    }
                    if (confirmed)
                    {
                        partOverrides = partOverrideList.ToArray();
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
            }
            catch (Exception ex)
            {
                ((IScriptErrorWindow)AppDomain.CurrentDomain.GetData("ScriptErrorWindow")).DisplayScriptError(null, ex);
                partOverrides = null;
                return false;
            }
        }

        public static void SwitchToAssignedOutfit(this Sim sim, OutfitAssignment outfitAssignment, bool spin = true)
        {
            if (sim.BuffManager.HasAnyElement(BuffNames.Singed, BuffNames.SingedElectricity, BuffNames.EmbarrassedClothesHidden) || sim.BuffManager.DisallowClothesChange() || sim.OccultManager.DisallowClothesChange())
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
                outfitIndex = Tuning.kPickRandomOutfitIndex ? RandomUtil.GetInt(sim.SimDescription.GetOutfitCount(outfitCategory) - 1) : 0;
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
            TimeToChangeBackList.Add(sim.SimDescription);
            var previousOutfitIndex = PreviousOutfits.FindIndex(x => x.SimDescription.SimDescriptionId == sim.SimDescription.SimDescriptionId);
            if (previousOutfitIndex > -1)
            {
                if (!sim.BuffManager.HasAnyElement(BuffNames.Singed, BuffNames.SingedElectricity, BuffNames.EmbarrassedClothesHidden) && !sim.BuffManager.DisallowClothesChange() && !sim.OccultManager.DisallowClothesChange())
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
            TimeToChangeBackList.RemoveAll(x => x.SimDescriptionId == sim.SimDescription.SimDescriptionId);
        }

        public static bool TryGetGlobalOutfitAssignment(this SimDescription simDescription, InteractionInstance interactionInstance, out OutfitAssignment outfitAssignment)
        {
            return simDescription.TryGetGlobalOutfitAssignment(interactionInstance.GetType(), out outfitAssignment);
        }

        public static bool TryGetGlobalOutfitAssignment(this SimDescription simDescription, Type interactionInstanceType, out OutfitAssignment outfitAssignment)
        {
            return IndexedOutfitAssignments.TryGetValue(OutfitUtils.GetAgePrefix(simDescription.Age, true) + OutfitUtils.GetGenderPrefix(simDescription.Gender) + "_" + interactionInstanceType.FullName, out outfitAssignment);
        }

        public static bool TryGetOutfitAssignment(this SimDescription simDescription, InteractionInstance interactionInstance, out OutfitAssignment outfitAssignment, SimDescription fallbackSimDescription = null)
        {
            return simDescription.TryGetOutfitAssignment(interactionInstance.GetType(), out outfitAssignment, fallbackSimDescription);
        }

        public static bool TryGetOutfitAssignment(this SimDescription simDescription, Type interactionInstanceType, out OutfitAssignment outfitAssignment, SimDescription fallbackSimDescription = null)
        {
            return simDescription == null ? fallbackSimDescription.TryGetGlobalOutfitAssignment(interactionInstanceType, out outfitAssignment) : IndexedOutfitAssignments.TryGetValue(interactionInstanceType.FullName + "_" + simDescription.SimDescriptionId, out outfitAssignment);
        }

        public static void UnassignGlobalOutfitToInteraction(this SimDescription simDescription, Type interactionInstanceType)
        {
            if (simDescription.TryGetGlobalOutfitAssignment(interactionInstanceType, out var outfitAssignment))
            {
                OutfitAssignments.Remove(outfitAssignment);
                IndexOutfitAssignments();
            }
        }

        public static void UnassignOutfitToInteraction(this SimDescription simDescription, Type interactionInstanceType)
        {
            if (simDescription.TryGetOutfitAssignment(interactionInstanceType, out var outfitAssignment))
            {
                OutfitAssignments.Remove(outfitAssignment);
                IndexOutfitAssignments();
            }
        }
    }
}
