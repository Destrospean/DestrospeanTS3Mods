using System;
using System.Collections.Generic;
using System.Xml;
using Destrospean.Enums.HairTrouble;
using Destrospean.Utils;
using Destrospean.Utils.HairTrouble;
using Sims3.Gameplay.CAS;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;

namespace Destrospean.HairTrouble
{
    public static class SimHairGrowth
    {
        [PersistableStatic(true)]
        private static Dictionary<string, HairGrowthStates> sHairGrowthStateMap;

        public static Dictionary<string, HairGrowthStates> HairGrowthStateMap
        {
            get
            {
                if (sHairGrowthStateMap == null)
                {
                    InitHairGrowthStateMap();
                }
                return sHairGrowthStateMap;
            }
        }

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

        public static bool DecrementHairGrowthState(this SimDescription simDescription, int by = 1, bool haircut = true, HairGrowthStateChangeFlags additionalFlags = 0)
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

        public static HairGrowthStates GetHairGrowthState(this SimOutfit outfit)
        {
            return outfit.TryGetHairGrowthState(out var hairGrowthState, out _) ? hairGrowthState : 0;
        }

        public static HairGrowthStates GetHairGrowthState(this SimDescription simDescription)
        {
            if (simDescription.TryGetHairGrowthState(out var hairGrowthState))
            {
                return hairGrowthState;
            }
            if (simDescription.GetOutfit(simDescription.CreatedSim?.CurrentOutfitCategory ?? OutfitCategories.Everyday, simDescription.CreatedSim?.CurrentOutfitIndex ?? 0).TryGetHairGrowthState(out hairGrowthState))
            {
                simDescription.SetHairGrowthState(hairGrowthState);
                return simDescription.GetHairGrowthState();
            }
            return 0;
        }

        public static CASPart[] GetValidHairstyles(CASAgeGenderFlags ageGenderSpecies, HairGrowthStates hairGrowthState, OutfitCategories outfitCategory = OutfitCategories.All, bool allowHats = true)
        {
            var validHairstyles = new List<CASPart>();
            foreach (var hairGrowthStateMapKvp in HairGrowthStateMap)
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

        public static bool IncrementHairGrowthState(this SimDescription simDescription, int by = 1, bool naturalGrowth = true, HairGrowthStateChangeFlags additionalFlags = 0)
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

        public static void InitHairGrowthStateMap()
        {
            sHairGrowthStateMap = new Dictionary<string, HairGrowthStates>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetType("Destrospean.HairTrouble.Data") == null)
                {
                    continue;
                }
                var reader = Simulator.ReadXml(new ResourceKey(ResourceUtils.HashString64(assembly.GetName().Name), 0x0333406C, 0x00000000));
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name == "HairGrowthStateMap")
                        {
                            reader.MoveToContent();
                        }
                        else if (reader.Name == "Entry")
                        {
                            sHairGrowthStateMap[reader.GetAttribute("CASPartKey") ?? ""] = (HairGrowthStates)Enum.Parse(typeof(HairGrowthStates), reader.GetAttribute("GrowthState") ?? "");
                        }
                    }
                }
                reader.Close();
            }
        }

        public static void OnHairGrowthStateChanged(this SimDescription simDescription, HairGrowthStates hairGrowthState, HairGrowthStateChangeFlags flags)
        {
            simDescription.ApplyHairGrowthStateToAllOutfits(hairGrowthState);
            if (!simDescription.HasRootsShowing() && (flags & HairGrowthStateChangeFlags.NaturalGrowth) != 0 && !simDescription.HasOriginalHairColors())
            {
                simDescription.HasRootsShowing(true);
            }
        }

        public static void SetHairGrowthState(this SimDescription simDescription, HairGrowthStates hairGrowthState, HairGrowthStateChangeFlags flags = 0)
        {
            HairGrowthStates longestHairGrowthState = 0;
            foreach (int value in Enum.GetValues(typeof(HairGrowthStates)))
            {
                if (value > 0 && (value & (value - 1)) == 0 && ((int)hairGrowthState & value) != 0)
                {
                    longestHairGrowthState = (HairGrowthStates)value;
                }
            }
            SimHairData.GrowthStates[simDescription.SimDescriptionId] = longestHairGrowthState;
            simDescription.OnHairGrowthStateChanged(longestHairGrowthState, flags);
        }

        public static void SetHairGrowthState(this SimDescription simDescription, int hairGrowthState, HairGrowthStateChangeFlags flags = 0)
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
            return HairGrowthStateMap.TryGetValue(hairstyle.Key.ToS3PIFormatKeyString(), out hairGrowthState);
        }

        public static bool TryGetHairGrowthState(this SimDescription simDescription, out HairGrowthStates hairGrowthState)
        {
            return SimHairData.GrowthStates.TryGetValue(simDescription.SimDescriptionId, out hairGrowthState);
        }
    }
}
