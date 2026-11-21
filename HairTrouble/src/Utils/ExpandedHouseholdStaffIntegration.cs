extern alias ExpandedHouseholdStaff;
using Destrospean.Enums.HairTrouble;
using Destrospean.HairTrouble;
using Destrospean.Utils;
using ExpandedHouseholdStaff::Destrospean.Utils.ExpandedHouseholdStaff;
using Sims3.Gameplay.CAS;
using Sims3.SimIFace.CAS;

namespace Sims3.Gameplay.Destrospean.Utils.HairTrouble
{
    public static class ExpandedHouseholdStaffIntegration
    {
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
                            if (savedPart.Part.TryGetHairGrowthState(out var hairGrowthStates) && (hairGrowthStates & simDescription.GetHairGrowthState()) == HairGrowthStates.Unknown)
                            {
                                // Add code to replace hair here.
                                continue;
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

        public static void Init()
        {
            CommonUtils.ReplaceMethod(typeof(OutfitAssignmentUtils).GetMethod(nameof(AddAssignedOutfit), new[]
                {
                    typeof(SimDescription),
                    typeof(OutfitAssignmentUtils.AssignedOutfit),
                    typeof(string)
                }), typeof(ExpandedHouseholdStaffIntegration).GetMethod(nameof(AddAssignedOutfit)));
        }
    }
}