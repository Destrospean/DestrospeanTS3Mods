using System.Collections.Generic;
using Destrospean.Utils;
using Sims3.Gameplay.CAS;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;

namespace Destrospean.HairTrouble
{
    public static class SimHairColors
    {
        [Persistable]
        public class HairColor
        {
            public Color AsColor => new Color(Base);

            public Color[] AsColors => new[]
                {
                    new Color(Base),
                    new Color(Highlight),
                    new Color(Root),
                    new Color(Tip)
                };
            
            public uint Base;

            public uint Highlight;

            public uint Root;

            public uint Tip;

            public HairColor()
            {
            }
            
            public HairColor(uint argb)
            {
                Base = argb;
            }
            
            public HairColor(uint baseArgb, uint highlightArgb, uint rootArgb, uint tipArgb)
            {
                Base = baseArgb;
                Highlight = highlightArgb;
                Root = rootArgb;
                Tip = tipArgb;
            }
            
            public HairColor(Color color) : this(color.ARGB)
            {
            }
            
            public HairColor(Color[] colors) : this(colors[ChannelIndexBase].ARGB, colors[ChannelIndexHighlight].ARGB, colors[ChannelIndexRoot].ARGB, colors[ChannelIndexTip].ARGB)
            {
            }
            
            public static bool operator ==(HairColor a, Color b)
            {
                return a == new HairColor(b);
            }
            
            public static bool operator ==(HairColor a, Color[] b)
            {
                return a == new HairColor(b);
            }

            public static bool operator ==(HairColor a, HairColor b)
            {
                return a?.Base == b?.Base && a?.Highlight == b?.Highlight && a?.Root == b?.Root && a?.Tip == b?.Tip;
            }
            
            public static bool operator !=(HairColor a, Color b)
            {
                return !(a == b);
            }
            
            public static bool operator !=(HairColor a, Color[] b)
            {
                return !(a == b);
            }
            
            public static bool operator !=(HairColor a, HairColor b)
            {
                return !(a == b);
            }
            
            public override bool Equals(object obj)
            {
                if (obj == null)
                {
                    return false;
                }
                if (ReferenceEquals(this, obj))
                {
                    return true;
                }
                if (obj.GetType() != GetType())
                {
                    return false;
                }
                return Equals((HairColor)obj);
            }
            
            public bool Equals(HairColor other)
            {
                return Base == other.Base && Highlight == other.Highlight && Root == other.Root && Tip == other.Tip;
            }
            
            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (int)Base;
                    hashCode = (hashCode * 397) ^ (int)Highlight;
                    hashCode = (hashCode * 397) ^ (int)Root;
                    hashCode = (hashCode * 397) ^ (int)Tip;
                    return hashCode;
                }
            }
        }
        
        public const int ChannelIndexBase = 0;
        
        public const int ChannelIndexHighlight = 1;
        
        public const int ChannelIndexRoot = 2;
        
        public const int ChannelIndexTip = 3;

        [PersistableStatic(true)]
        public static Dictionary<SimDescription, HairColor> OriginalBodyHairColors = new Dictionary<SimDescription, HairColor>();

        [PersistableStatic(true)]
        public static Dictionary<SimDescription, HairColor> OriginalEyebrowColors = new Dictionary<SimDescription, HairColor>();

        [PersistableStatic(true)]
        public static Dictionary<SimDescription, HairColor> OriginalFacialHairColors = new Dictionary<SimDescription, HairColor>();

        [PersistableStatic(true)]
        public static Dictionary<SimDescription, HairColor> OriginalHairColors = new Dictionary<SimDescription, HairColor>();
        
        [PersistableStatic(true)]
        public static Dictionary<SimDescription, bool> RootsShowing = new Dictionary<SimDescription, bool>();

        public static void ApplyOverallHairColorsToAllOutfits(this SimDescription simDescription, Color bodyHairColor, Color eyebrowColor, Color[] facialHairColors, Color[] hairColors, bool spin = false)
        {
            simDescription.ActiveBodyHairColor = bodyHairColor;
            simDescription.ActiveEyebrowColor = eyebrowColor;
            simDescription.ActiveFacialHairColors = facialHairColors;
            simDescription.ActiveHairColors = hairColors;
            simDescription.ApplyToAllOutfits((simBuilder, outfitCategory, outfitIndex) => simDescription.ApplyOverallHairColorsToOutfit(simBuilder, outfitCategory, outfitIndex, bodyHairColor, eyebrowColor, facialHairColors, hairColors), spin);
        }

        public static void ApplyOverallHairColorsToAllOutfits(this SimDescription simDescription, HairColor bodyHairColor, HairColor eyebrowColor, HairColor facialHairColors, HairColor hairColors, bool spin = false)
        {
            simDescription.ApplyOverallHairColorsToAllOutfits(bodyHairColor.AsColor, eyebrowColor.AsColor, facialHairColors.AsColors, hairColors.AsColors, spin);
        }

        public static SimOutfit ApplyOverallHairColorsToOutfit(this SimDescription simDescription, SimBuilder simBuilder, OutfitCategories outfitCategory, int outfitIndex, Color bodyHairColor, Color eyebrowColor, Color[] facialHairColors, Color[] hairColors)
        {
            simBuilder.PrepareForOutfit(simDescription.GetOutfit(outfitCategory, outfitIndex));
            OutfitUtils.InjectBodyHairColor(simBuilder, bodyHairColor);
            OutfitUtils.InjectEyeBrowHairColor(simBuilder, eyebrowColor);
            OutfitUtils.InjectHairColor(simBuilder, facialHairColors, BodyTypes.Beard);
            OutfitUtils.InjectHairColor(simBuilder, hairColors, BodyTypes.Hair);
            return new SimOutfit(simBuilder.CacheOutfit($"ApplyHairColors_{simDescription.SimDescriptionId}_{outfitCategory}_{outfitIndex}"));
        }
        
        public static SimOutfit ApplyOverallHairColorsToOutfit(this SimDescription simDescription, SimBuilder simBuilder, OutfitCategories outfitCategory, int outfitIndex, HairColor bodyHairColor, HairColor eyebrowColor, HairColor facialHairColors, HairColor hairColors)
        {
            return simDescription.ApplyOverallHairColorsToOutfit(simBuilder, outfitCategory, outfitIndex, bodyHairColor.AsColor, eyebrowColor.AsColor, facialHairColors.AsColors, hairColors.AsColors);
        }

        public static void ClearOriginalOverallHairColors(this SimDescription simDescription)
        {
            if (simDescription == null)
            {
                return;
            }
            OriginalBodyHairColors.Remove(simDescription);
            OriginalEyebrowColors.Remove(simDescription);
            OriginalFacialHairColors.Remove(simDescription);
            OriginalHairColors.Remove(simDescription);
        }

        public static HairColor GetOriginalBodyHairColor(this SimDescription simDescription)
        {
            if (simDescription == null)
            {
                return null;
            }
            if (OriginalBodyHairColors.TryGetValue(simDescription, out var originalColor))
            {
                return originalColor;
            }
            return OriginalBodyHairColors[simDescription] = new HairColor(simDescription.ActiveBodyHairColor);
        }

        public static HairColor GetOriginalEyebrowColor(this SimDescription simDescription)
        {
            if (simDescription == null)
            {
                return null;
            }
            if (OriginalEyebrowColors.TryGetValue(simDescription, out var originalColor))
            {
                return originalColor;
            }
            return OriginalEyebrowColors[simDescription] = new HairColor(simDescription.ActiveEyebrowColor);
        }

        public static HairColor GetOriginalFacialHairColors(this SimDescription simDescription)
        {
            if (simDescription == null)
            {
                return null;
            }
            if (OriginalFacialHairColors.TryGetValue(simDescription, out var originalColors))
            {
                return originalColors;
            }
            return OriginalFacialHairColors[simDescription] = new HairColor(simDescription.ActiveFacialHairColors);
        }

        public static HairColor GetOriginalHairColors(this SimDescription simDescription)
        {
            if (simDescription == null)
            {
                return null;
            }
            if (OriginalHairColors.TryGetValue(simDescription, out var originalColors))
            {
                return originalColors;
            }
            return OriginalHairColors[simDescription] = new HairColor(simDescription.ActiveHairColors);
        }

        public static bool HasOriginalBodyHairColor(this SimDescription simDescription)
        {
            if (simDescription == null)
            {
                return false;
            }
            return simDescription.GetOriginalBodyHairColor() == simDescription.ActiveBodyHairColor;
        }

        public static bool HasOriginalEyebrowColor(this SimDescription simDescription)
        {
            if (simDescription == null)
            {
                return false;
            }
            return simDescription.GetOriginalEyebrowColor() == simDescription.ActiveEyebrowColor;
        }

        public static bool HasOriginalFacialHairColors(this SimDescription simDescription)
        {
            if (simDescription == null)
            {
                return false;
            }
            return simDescription.GetOriginalFacialHairColors() == simDescription.ActiveFacialHairColors;
        }

        public static bool HasOriginalHairColors(this SimDescription simDescription)
        {
            if (simDescription == null)
            {
                return false;
            }
            return simDescription.GetOriginalHairColors() == simDescription.ActiveHairColors;
        }

        public static bool HasOriginalOverallHairColors(this SimDescription simDescription)
        {
            return simDescription != null && simDescription.HasOriginalBodyHairColor() && simDescription.HasOriginalEyebrowColor() && simDescription.HasOriginalFacialHairColors() && simDescription.HasOriginalHairColors();
        }

        public static bool HasRootsShowing(this SimDescription simDescription, bool? value = null)
        {
            if (simDescription == null)
            {
                return false;
            }
            if (value == null)
            {
                if (!simDescription.HasOriginalHairColors() && RootsShowing.TryGetValue(simDescription, out var rootsShowing))
                {
                    return rootsShowing;
                }
                simDescription.ActiveHairColors[ChannelIndexRoot] = simDescription.GetOriginalHairColors().AsColors[ChannelIndexRoot]; 
                simDescription.ApplyOverallHairColorsToAllOutfits(simDescription.ActiveBodyHairColor, simDescription.ActiveEyebrowColor, simDescription.ActiveFacialHairColors, simDescription.ActiveHairColors);
                return RootsShowing[simDescription] = false;
            }
            if (value.Value)
            {
                simDescription.ActiveHairColors[ChannelIndexRoot] = simDescription.GetOriginalHairColors().AsColors[ChannelIndexRoot]; 
                simDescription.ApplyOverallHairColorsToAllOutfits(simDescription.ActiveBodyHairColor, simDescription.ActiveEyebrowColor, simDescription.ActiveFacialHairColors, simDescription.ActiveHairColors);
            }
            return RootsShowing[simDescription] = value.Value;
        }

        public static void InitOriginalOverallHairColors(this SimDescription simDescription)
        {
            simDescription?.GetOriginalBodyHairColor();
            simDescription?.GetOriginalEyebrowColor();
            simDescription?.GetOriginalFacialHairColors();
            simDescription?.GetOriginalHairColors();
        }
    }
}
