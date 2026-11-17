using System;
using System.Collections.Generic;
using Destrospean.Enums.HairTrouble;
using Destrospean.Misc;
using Destrospean.Utils;
using Destrospean.Utils.HairTrouble;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Destrospean.HairTrouble;
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
        
        [PersistableStatic]
        public static Dictionary<ulong, long> TimesOfLastHairGrowth = new Dictionary<ulong, long>();
        
        public static void ApplyHairGrowthStateToAllOutfits(this SimDescription simDescription, HairGrowthStates hairGrowthState, bool spin = false)
        {
            CASPart? hairstyle = null;
            simDescription.ApplyToAllOutfits((simBuilder, outfitCategory, outfitIndex) => simDescription.ApplyHairGrowthStateToOutfit(simBuilder, outfitCategory, outfitIndex, hairGrowthState, ref hairstyle), spin);
        }

        public static SimOutfit ApplyHairGrowthStateToOutfit(this SimDescription simDescription, SimBuilder simBuilder, OutfitCategories outfitCategory, int outfitIndex, HairGrowthStates hairGrowthState, ref CASPart? hairstyle)
        {
            hairstyle = hairstyle ?? (simDescription.GetOutfit(OutfitCategories.Everyday, 0).TryGetHairGrowthState(out var outfitHairGrowthState, out hairstyle) && (outfitHairGrowthState & hairGrowthState) != 0 ? hairstyle : GetValidHairstyles(simDescription.AgeGenderSpecies, hairGrowthState, allowHats: false).TryGetRandomItem(out hairstyle) ? hairstyle : null);
            if (hairstyle == null)
            {
                return null;
            }
            simBuilder.PrepareForOutfit(simDescription.GetOutfit(outfitCategory, outfitIndex));
            simBuilder.RemoveParts(BodyTypes.Hair);
            simBuilder.AddPart(hairstyle.Value);
            OutfitUtils.InjectBodyHairColor(simBuilder, simDescription.BodyHairColor.ActiveColor);
            OutfitUtils.InjectEyeBrowHairColor(simBuilder, simDescription.EyebrowColor.ActiveColor);
            OutfitUtils.InjectHairColor(simBuilder, Array.ConvertAll(simDescription.FacialHairColors, x => x.ActiveColor), BodyTypes.Beard);
            OutfitUtils.InjectHairColor(simBuilder, Array.ConvertAll(simDescription.HairColors, x => x.ActiveColor), BodyTypes.Hair);
            return new SimOutfit(simBuilder.CacheOutfit($"ApplyHairGrowthState_{simDescription.SimDescriptionId}_{outfitCategory}_{outfitIndex}"));
        }

        public static bool DecrementHairGrowthState(this SimDescription simDescription, int by = 1, bool haircut = true, HairGrowthStateChangeFlags additionalFlags = HairGrowthStateChangeFlags.Default)
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
                simDescription.SetHairGrowthState(newHairGrowthState, (haircut ? HairGrowthStateChangeFlags.Haircut : HairGrowthStateChangeFlags.Default) | additionalFlags);
                return true;
            }
            return false;
        }
        
        public static void FixUpHairData(this SimDescription simDescription)
        {
            if (simDescription == null)
            {
                return;
            }
            simDescription.InitOriginalOverallHairColors();
            simDescription.GetHairGrowthState();
            if (HairGrowthAlarms.TryGetValue(simDescription.SimDescriptionId, out var hairGrowthAlarmHandle) && hairGrowthAlarmHandle != AlarmHandle.kInvalidHandle)
            {
                AlarmManager.Global.RemoveAlarm(hairGrowthAlarmHandle);
            }
            HairGrowthAlarms[simDescription.SimDescriptionId] = AlarmManager.Global.AddAlarmRepeating(1f, TimeUnit.Minutes, () =>
                {
                    if (simDescription.Household == Household.ActiveHousehold && SimClock.CurrentTicks - (TimesOfLastHairGrowth.TryGetValue(simDescription.SimDescriptionId, out var timeOfLastHairGrowth) ? timeOfLastHairGrowth : TimesOfLastHairGrowth[simDescription.SimDescriptionId] = SimClock.CurrentTicks) >= SimClock.ConvertToTicks(Settings.kHairGrowthInterval, Settings.kHairGrowthIntervalUnit))
                    {
                        TimesOfLastHairGrowth[simDescription.SimDescriptionId] = SimClock.CurrentTicks;
                        ActionScriptTask.Start(() => simDescription.IncrementHairGrowthState());
                    }
                }, 1f, TimeUnit.Minutes, "Hair Growth Alarm -- SimDescriptionId: " + simDescription.SimDescriptionId, AlarmType.NeverPersisted, simDescription);
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
            HairGrowthStates longestHairGrowthState = Enums.HairTrouble.HairGrowthStates.Unknown;
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
            foreach (var hairGrowthStateMapKvp in HairClassifications)
            {
                if (hairGrowthStateMapKvp.Value == hairGrowthState)
                {
                    var hairstyle = new CASPart(S3PIResourceUtils.FromS3PIFormatKeyString(hairGrowthStateMapKvp.Key));
                    if (hairstyle.Key != ResourceKey.kInvalidResourceKey && (hairstyle.Age & ageGenderSpecies) != 0 && (hairstyle.Gender & ageGenderSpecies) != 0 && (hairstyle.Species & ageGenderSpecies) != 0 && (hairstyle.CategoryFlags & (uint)outfitCategory) != 0 /*&& (hairstyle.CategoryFlags & (uint)OutfitCategoriesExtended.ValidForRandom) != 0*/ && (allowHats || (hairstyle.CategoryFlags & (uint)OutfitCategoriesExtended.IsHat) == 0) && (hairstyle.CategoryFlags & (uint)OutfitCategoriesExtended.IsHiddenInCAS) == 0)
                    {
                        validHairstyles.Add(hairstyle);
                    }
                }
            }
            return validHairstyles.ToArray();
        }

        public static bool IncrementHairGrowthState(this SimDescription simDescription, int by = 1, bool naturalGrowth = true, HairGrowthStateChangeFlags additionalFlags = HairGrowthStateChangeFlags.Default)
        {
            if (by < 1)
            {
                return false;
            }
            var newHairGrowthState = (int)simDescription.GetHairGrowthState() << by;
            if (newHairGrowthState > 1)
            {
                simDescription.SetHairGrowthState(newHairGrowthState, (naturalGrowth ? HairGrowthStateChangeFlags.NaturalGrowth : HairGrowthStateChangeFlags.Default) | additionalFlags);
                return true;
            }
            return false;
        }

        public static void OnHairGrowthStateChanged(this SimDescription simDescription, HairGrowthStates hairGrowthState, HairGrowthStateChangeFlags flags)
        {
            simDescription.ApplyHairGrowthStateToAllOutfits(hairGrowthState);
            if (!simDescription.HasRootsShowing() && (flags & HairGrowthStateChangeFlags.NaturalGrowth) != 0 && !simDescription.HasOriginalHairColors())
            {
                simDescription.HasRootsShowing(true);
            }
            TimesOfLastHairGrowth[simDescription.SimDescriptionId] = SimClock.CurrentTicks;
        }

        public static void SetHairGrowthState(this SimDescription simDescription, HairGrowthStates hairGrowthState, HairGrowthStateChangeFlags flags = HairGrowthStateChangeFlags.Default)
        {
            simDescription.OnHairGrowthStateChanged(HairGrowthStates[simDescription.SimDescriptionId] = hairGrowthState.GetLongestHairGrowthState(), flags);
        }

        public static void SetHairGrowthState(this SimDescription simDescription, int hairGrowthState, HairGrowthStateChangeFlags flags = HairGrowthStateChangeFlags.Default)
        {
            simDescription.SetHairGrowthState((HairGrowthStates)hairGrowthState, flags);
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
