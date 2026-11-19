using System;
using System.Collections.Generic;
using Destrospean.Enums.HairTrouble;
using Destrospean.Utils;
using Destrospean.Utils.HairTrouble;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;

namespace Destrospean.HairTrouble
{
    public static class SimHairGrowth
    {
        [PersistableStatic(true)]
        private static Dictionary<string, HairGrowthStates> sHairGrowthStateMap = new Dictionary<string, HairGrowthStates>();
               
        public static Dictionary<string, HairGrowthStates> HairClassifications => sHairGrowthStateMap;
        
        public static Dictionary<ulong, AlarmHandle> HairGrowthAlarms = new Dictionary<ulong, AlarmHandle>();
        
        [PersistableStatic(true)]
        public static Dictionary<ulong, HairGrowthStates> HairGrowthStates = new Dictionary<ulong, HairGrowthStates>();
        
        [PersistableStatic(true)]
        public static Dictionary<ulong, HairGrowthStates> PreferredHairGrowthStates = new Dictionary<ulong, HairGrowthStates>();
        
        [PersistableStatic]
        public static Dictionary<ulong, long> TimesOfLastHairGrowth = new Dictionary<ulong, long>();
        
        public static void ApplyHairGrowthStateToAllOutfits(this SimDescription simDescription, HairGrowthStates hairGrowthState, bool spin = false, CASPart? specificHairstyle = null)
        {
            simDescription.ApplyToAllOutfits((simBuilder, outfitCategory, outfitIndex) => simDescription.ApplyHairGrowthStateToOutfit(simBuilder, outfitCategory, outfitIndex, hairGrowthState, ref specificHairstyle), spin);
        }

        public static SimOutfit ApplyHairGrowthStateToOutfit(this SimDescription simDescription, SimBuilder simBuilder, OutfitCategories outfitCategory, int outfitIndex, HairGrowthStates hairGrowthState, ref CASPart? specificHairstyle)
        {
            specificHairstyle = specificHairstyle ?? (simDescription.GetOutfit(OutfitCategories.Everyday, 0).TryGetHairGrowthState(out var outfitHairGrowthState, out specificHairstyle) && (outfitHairGrowthState & hairGrowthState) != Enums.HairTrouble.HairGrowthStates.Unknown ? specificHairstyle : GetValidHairstyles(simDescription.AgeGenderSpecies, hairGrowthState, allowHats: false).TryGetRandomItem(out specificHairstyle) ? specificHairstyle : null);
            if (specificHairstyle == null)
            {
                return null;
            }
            simBuilder.PrepareForOutfit(simDescription.GetOutfit(outfitCategory, outfitIndex));
            simBuilder.RemoveParts(BodyTypes.Hair);
            simBuilder.AddPart(specificHairstyle.Value);
            OutfitUtils.InjectBodyHairColor(simBuilder, simDescription.BodyHairColor.ActiveColor);
            OutfitUtils.InjectEyeBrowHairColor(simBuilder, simDescription.EyebrowColor.ActiveColor);
            OutfitUtils.InjectHairColor(simBuilder, Array.ConvertAll(simDescription.FacialHairColors, x => x.ActiveColor), BodyTypes.Beard);
            OutfitUtils.InjectHairColor(simBuilder, Array.ConvertAll(simDescription.HairColors, x => x.ActiveColor), BodyTypes.Hair);
            return new SimOutfit(simBuilder.CacheOutfit($"ApplyHairGrowthState_{simDescription.SimDescriptionId}_{outfitCategory}_{outfitIndex}"));
        }

        public static bool DecrementHairGrowthState(this SimDescription simDescription, int by = 1, bool haircut = true, HairGrowthStateChangeFlags additionalFlags = HairGrowthStateChangeFlags.Default, bool spin = false)
        {
            if (by < 1)
            {
                return false;
            }
            var newHairGrowthState = (int)simDescription.GetHairGrowthState() >> by;
            var growthStates = new List<int>();
            foreach (int value in Enum.GetValues(typeof(HairGrowthStates)))
            {
                growthStates.Add(value);
            }
            growthStates.Sort();
            if (newHairGrowthState < growthStates.FindLast(x => x > 0 && (x & (x - 1)) == 0))
            {
                simDescription.SetHairGrowthState(newHairGrowthState, (haircut ? HairGrowthStateChangeFlags.Haircut : HairGrowthStateChangeFlags.Default) | additionalFlags, spin);
                return true;
            }
            return false;
        }

        public static HairGrowthStates GetHairGrowthState(this SimOutfit outfit)
        {
            return outfit.TryGetHairGrowthState(out var hairGrowthState, out _) ? hairGrowthState : Enums.HairTrouble.HairGrowthStates.Unknown;
        }

        public static HairGrowthStates GetHairGrowthState(this SimDescription simDescription)
        {
            if (simDescription.TryGetHairGrowthState(out var hairGrowthState))
            {
                return hairGrowthState;
            }
            if (simDescription.GetOutfit(simDescription.CreatedSim?.CurrentOutfitCategory ?? OutfitCategories.Everyday, simDescription.CreatedSim?.CurrentOutfitIndex ?? 0).TryGetHairGrowthState(out hairGrowthState))
            {
                return HairGrowthStates[simDescription.SimDescriptionId] = hairGrowthState.GetLongestHairGrowthState();
            }
            return Enums.HairTrouble.HairGrowthStates.Unknown;
        }

        public static HairGrowthStates GetLongestHairGrowthState(this HairGrowthStates hairGrowthStates)
        {
            var longestHairGrowthState = Enums.HairTrouble.HairGrowthStates.Unknown;
            foreach (int value in Enum.GetValues(typeof(HairGrowthStates)))
            {
                if (value > 0 && (value & (value - 1)) == 0 && ((int)hairGrowthStates & value) != 0)
                {
                    longestHairGrowthState = (HairGrowthStates)value;
                }
            }
            return longestHairGrowthState;
        }

        public static CASPart[] GetValidHairstyles(CASAgeGenderFlags ageGenderSpecies, HairGrowthStates hairGrowthState, OutfitCategories outfitCategory = OutfitCategories.All, bool allowHats = true)
        {
            var validHairstyles = new List<CASPart>();
            foreach (var hairClassification in HairClassifications)
            {
                if (hairClassification.Value == hairGrowthState)
                {
                    var hairstyle = new CASPart(S3PIResourceUtils.FromS3PIFormatKeyString(hairClassification.Key));
                    if (hairstyle.Key != ResourceKey.kInvalidResourceKey && (hairstyle.Age & ageGenderSpecies) != CASAgeGenderFlags.None && (hairstyle.Gender & ageGenderSpecies) != CASAgeGenderFlags.None && (hairstyle.Species & ageGenderSpecies) != CASAgeGenderFlags.None && (hairstyle.CategoryFlags & (uint)outfitCategory) != 0u /*&& (hairstyle.CategoryFlags & (uint)OutfitCategoriesExtended.ValidForRandom) != 0*/ && (allowHats || (hairstyle.CategoryFlags & (uint)OutfitCategoriesExtended.IsHat) == 0u) && (hairstyle.CategoryFlags & (uint)OutfitCategoriesExtended.IsHiddenInCAS) == 0u)
                    {
                        validHairstyles.Add(hairstyle);
                    }
                }
            }
            return validHairstyles.ToArray();
        }

        public static bool IncrementHairGrowthState(this SimDescription simDescription, int by = 1, bool naturalGrowth = true, HairGrowthStateChangeFlags additionalFlags = HairGrowthStateChangeFlags.Default, bool spin = false)
        {
            if (by < 1)
            {
                return false;
            }
            var newHairGrowthState = (int)simDescription.GetHairGrowthState() << by;
            if (newHairGrowthState > 1)
            {
                simDescription.SetHairGrowthState(newHairGrowthState, (naturalGrowth ? HairGrowthStateChangeFlags.NaturalGrowth : HairGrowthStateChangeFlags.Default) | additionalFlags, spin);
                return true;
            }
            return false;
        }

        public static void OnHairGrowthStateChanged(this SimDescription simDescription, HairGrowthStates hairGrowthState, HairGrowthStateChangeFlags flags, bool spin = false, CASPart? specificHairstyle = null)
        {
            simDescription.ApplyHairGrowthStateToAllOutfits(hairGrowthState, spin, specificHairstyle);
            if ((flags & HairGrowthStateChangeFlags.NaturalGrowth) != HairGrowthStateChangeFlags.Default && !simDescription.HasOriginalHairColors())
            {
                simDescription.HasRootsShowing(true);
            }
            TimesOfLastHairGrowth[simDescription.SimDescriptionId] = SimClock.CurrentTicks;
        }

        public static void SetHairGrowthState(this SimDescription simDescription, HairGrowthStates hairGrowthState, HairGrowthStateChangeFlags flags = HairGrowthStateChangeFlags.Default, bool spin = false, CASPart? specificHairstyle = null)
        {
            simDescription.OnHairGrowthStateChanged(HairGrowthStates[simDescription.SimDescriptionId] = hairGrowthState.GetLongestHairGrowthState(), flags, spin, specificHairstyle);
        }

        public static void SetHairGrowthState(this SimDescription simDescription, int hairGrowthState, HairGrowthStateChangeFlags flags = HairGrowthStateChangeFlags.Default, bool spin = false, CASPart? specificHairstyle = null)
        {
            simDescription.SetHairGrowthState((HairGrowthStates)hairGrowthState, flags, spin, specificHairstyle);
        }

        public static bool TryGetHairGrowthState(this SimOutfit outfit, out HairGrowthStates hairGrowthState)
        {
            return outfit.TryGetHairGrowthState(out hairGrowthState, out _);
        }

        public static bool TryGetHairGrowthState(this SimOutfit outfit, out HairGrowthStates hairGrowthState, out CASPart? hairstyle)
        {
            var partIndex = Array.FindIndex(outfit.Parts, x => x.BodyType == BodyTypes.Hair);
            if (partIndex == -1)
            {
                hairGrowthState = 0;
                hairstyle = null;
                return false;
            }
            return (hairstyle = outfit.Parts[partIndex]).Value.TryGetHairGrowthState(out hairGrowthState);
        }

        public static bool TryGetHairGrowthState(this CASPart hairstyle, out HairGrowthStates hairGrowthState)
        {
            return HairClassifications.TryGetValue(hairstyle.Key.ToS3PIFormatKeyString(), out hairGrowthState);
        }

        public static bool TryGetHairGrowthState(this SimDescription simDescription, out HairGrowthStates hairGrowthState)
        {
            return HairGrowthStates.TryGetValue(simDescription.SimDescriptionId, out hairGrowthState);
        }
    }
}
