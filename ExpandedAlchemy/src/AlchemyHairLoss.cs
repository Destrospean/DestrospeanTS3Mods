using Sims3.Gameplay.Actors;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Interactions;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Destrospean.Utils;

namespace Sims3.Gameplay.Objects.Alchemy
{
    public class AlchemyHairLoss : AlchemyPotion
    {
        public override bool DoDrinkBehavior(Sim actor, Sim target, InteractionInstance interaction)
        {
            base.DoDrinkBehavior(actor, target, interaction);
            target.SimDescription.ApplyToAllOutfits((simBuilder, outfitCategory, outfitIndex) =>
                {
                    SimOutfit outfit = target.SimDescription.GetOutfit(outfitCategory, outfitIndex);
                    ResourceKey partKey = new ResourceKey(ResourceUtils.HashString64(target.SimDescription.Toddler ? "puhairnone" : (target.SimDescription.Teen ? "a" : OutfitUtils.GetAgePrefix(target.SimDescription.Age, true)) + OutfitUtils.GetGenderPrefix(target.SimDescription.Gender) + "hairep08hairless"), 0x034AEECB, 0x00000000);
                    if (partKey == ResourceKey.kInvalidResourceKey)
                    {
                        return outfit;
                    }
                    simBuilder.PrepareForOutfit(outfit);
                    simBuilder.RemoveParts(BodyTypes.Hair);
                    simBuilder.AddPart(partKey);
                    return new SimOutfit(simBuilder.CacheOutfit(string.Format("AlchemyHairLoss_{0}_{1}_{2}", target.SimDescription.SimDescriptionId, outfitCategory, outfitIndex)));
                });
            return true;
        }
    }
}
