using System;
using System.Collections.Generic;
using Destrospean.Utils;
using Sims3.Gameplay.CAS;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;

namespace Destrospean.HairTrouble
{
    public static class SimHairColors
    {
        public enum Channels
        {
            Base,
            Highlight,
            Root,
            Tip
        }
        
        [Persistable]
        public class HairColor
        {
            public uint Base;

            public uint Highlight;

            public uint Root;

            public uint Tip;

            public HairColor()
            {
            }
            
            public HairColor(uint @base, uint highlight, uint root, uint tip)
            {
                Base = @base;
                Highlight = highlight;
                Root = root;
                Tip = tip;
            }
        }

        [PersistableStatic(true)]
        public static Dictionary<ulong, HairColor> sOriginalBodyHairColors = new Dictionary<ulong, HairColor>();

        [PersistableStatic(true)]
        public static Dictionary<ulong, HairColor> sOriginalEyebrowColors = new Dictionary<ulong, HairColor>();

        [PersistableStatic(true)]
        public static Dictionary<ulong, HairColor> sOriginalFacialHairColors = new Dictionary<ulong, HairColor>();

        [PersistableStatic(true)]
        public static Dictionary<ulong, HairColor> sOriginalHairColors = new Dictionary<ulong, HairColor>();

        public static void ApplyOverallHairColorsToAllOutfits(this SimDescription simDescription, uint bodyHairColor, uint eyebrowColor, uint[] facialHairColors, uint[] hairColors, bool spin = false)
        {
            simDescription.ActiveBodyHairColor = new Color(bodyHairColor);
            simDescription.ActiveEyebrowColor = new Color(eyebrowColor);
            simDescription.ActiveFacialHairColors = Array.ConvertAll(facialHairColors, x => new Color(x));
            simDescription.ActiveHairColors = Array.ConvertAll(hairColors, x => new Color(x));
            simDescription.ApplyToAllOutfits((simBuilder, outfitCategory, outfitIndex) => simDescription.ApplyOverallHairColorsToOutfit(simBuilder, outfitCategory, outfitIndex, bodyHairColor, eyebrowColor, facialHairColors, hairColors), spin);
        }

        public static SimOutfit ApplyOverallHairColorsToOutfit(this SimDescription simDescription, SimBuilder simBuilder, OutfitCategories outfitCategory, int outfitIndex, uint bodyHairColor, uint eyebrowColor, uint[] facialHairColors, uint[] hairColors)
        {
            simBuilder.PrepareForOutfit(simDescription.GetOutfit(outfitCategory, outfitIndex));
            OutfitUtils.InjectBodyHairColor(simBuilder, new Color(bodyHairColor));
            OutfitUtils.InjectEyeBrowHairColor(simBuilder, new Color(eyebrowColor));
            OutfitUtils.InjectHairColor(simBuilder, Array.ConvertAll(facialHairColors, x => new Color(x)), BodyTypes.Beard);
            OutfitUtils.InjectHairColor(simBuilder, Array.ConvertAll(hairColors, x => new Color(x)), BodyTypes.Hair);
            return new SimOutfit(simBuilder.CacheOutfit($"ApplyHairColors_{simDescription.SimDescriptionId}_{outfitCategory}_{outfitIndex}"));
        }

        public static void ClearOriginalOverallHairColors(this SimDescription simDescription)
        {
            sOriginalBodyHairColors.Remove(simDescription.SimDescriptionId);
            sOriginalEyebrowColors.Remove(simDescription.SimDescriptionId);
            sOriginalFacialHairColors.Remove(simDescription.SimDescriptionId);
            sOriginalHairColors.Remove(simDescription.SimDescriptionId);
        }

        public static HairColor GetOriginalBodyHairColor(this SimDescription simDescription)
        {
            return sOriginalBodyHairColors.TryGetValue(simDescription.SimDescriptionId, out var originalColor) ? originalColor : sOriginalBodyHairColors[simDescription.SimDescriptionId] = new HairColor(simDescription.ActiveBodyHairColor.ARGB, 0, 0, 0);
        }

        public static HairColor GetOriginalEyebrowColor(this SimDescription simDescription)
        {
            return sOriginalEyebrowColors.TryGetValue(simDescription.SimDescriptionId, out var originalColor) ? originalColor : sOriginalEyebrowColors[simDescription.SimDescriptionId] = new HairColor(simDescription.ActiveEyebrowColor.ARGB, 0, 0, 0);
        }

        public static HairColor GetOriginalFacialHairColors(this SimDescription simDescription)
        {
            return sOriginalFacialHairColors.TryGetValue(simDescription.SimDescriptionId, out var originalColors) ? originalColors : sOriginalFacialHairColors[simDescription.SimDescriptionId] = new HairColor(simDescription.ActiveFacialHairColors[0].ARGB, simDescription.ActiveFacialHairColors[1].ARGB, simDescription.ActiveFacialHairColors[2].ARGB, simDescription.ActiveFacialHairColors[3].ARGB);
        }

        public static HairColor GetOriginalHairColors(this SimDescription simDescription)
        {
            return sOriginalHairColors.TryGetValue(simDescription.SimDescriptionId, out var originalColors) ? originalColors : sOriginalHairColors[simDescription.SimDescriptionId] = new HairColor(simDescription.ActiveHairColors[0].ARGB, simDescription.ActiveHairColors[1].ARGB, simDescription.ActiveHairColors[2].ARGB, simDescription.ActiveHairColors[3].ARGB);
        }

        public static bool HasOriginalBodyHairColor(this SimDescription simDescription)
        {
            return simDescription.GetOriginalBodyHairColor().Base == simDescription.ActiveBodyHairColor.ARGB;
        }

        public static bool HasOriginalEyebrowColor(this SimDescription simDescription)
        {
            return simDescription.GetOriginalEyebrowColor().Base == simDescription.ActiveEyebrowColor.ARGB;
        }

        public static bool HasOriginalFacialHairColors(this SimDescription simDescription)
        {
            var facialHairColors = simDescription.GetOriginalFacialHairColors();
            return facialHairColors.Base == simDescription.ActiveFacialHairColors[0].ARGB && facialHairColors.Highlight == simDescription.ActiveFacialHairColors[1].ARGB && facialHairColors.Root == simDescription.ActiveFacialHairColors[2].ARGB && facialHairColors.Tip == simDescription.ActiveFacialHairColors[3].ARGB;
        }

        public static bool HasOriginalHairColors(this SimDescription simDescription)
        {
            var hairColors = simDescription.GetOriginalHairColors();
            return hairColors.Base == simDescription.ActiveHairColors[0].ARGB && hairColors.Highlight == simDescription.ActiveHairColors[1].ARGB && hairColors.Root == simDescription.ActiveHairColors[2].ARGB && hairColors.Tip == simDescription.ActiveHairColors[3].ARGB;
        }

        public static bool HasOriginalOverallHairColors(this SimDescription simDescription)
        {
            return simDescription.HasOriginalBodyHairColor() && simDescription.HasOriginalEyebrowColor() && simDescription.HasOriginalFacialHairColors() && simDescription.HasOriginalHairColors();
        }

        public static bool HasRootsShowing(this SimDescription simDescription, bool? value = null)
        {
            const int rootIndex = (int)Channels.Root;
            if (value == null)
            {
                if (!simDescription.HasOriginalHairColors() && SimHairData.RootsShowing.TryGetValue(simDescription.SimDescriptionId, out var rootsShowing))
                {
                    return rootsShowing;
                }
                simDescription.ActiveHairColors[rootIndex].ARGB = simDescription.GetOriginalHairColors().Root; 
                simDescription.ApplyOverallHairColorsToAllOutfits(simDescription.ActiveBodyHairColor.ARGB, simDescription.ActiveEyebrowColor.ARGB, Array.ConvertAll(simDescription.ActiveFacialHairColors, x => x.ARGB), Array.ConvertAll(simDescription.ActiveHairColors, x => x.ARGB));
                return SimHairData.RootsShowing[simDescription.SimDescriptionId] = false;
            }
            if (value.Value)
            {
                simDescription.ActiveHairColors[rootIndex].ARGB = simDescription.GetOriginalHairColors().Root; 
                simDescription.ApplyOverallHairColorsToAllOutfits(simDescription.ActiveBodyHairColor.ARGB, simDescription.ActiveEyebrowColor.ARGB, Array.ConvertAll(simDescription.ActiveFacialHairColors, x => x.ARGB), Array.ConvertAll(simDescription.ActiveHairColors, x => x.ARGB));
            }
            return SimHairData.RootsShowing[simDescription.SimDescriptionId] = value.Value;
        }

        public static void InitOriginalOverallHairColors(this SimDescription simDescription)
        {
            simDescription.GetOriginalBodyHairColor();
            simDescription.GetOriginalEyebrowColor();
            simDescription.GetOriginalFacialHairColors();
            simDescription.GetOriginalHairColors();
        }
    }
}
