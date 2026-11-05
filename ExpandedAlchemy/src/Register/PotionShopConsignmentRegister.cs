using System.Collections.Generic;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Objects;
using Sims3.Gameplay.Objects.Insect;
using Sims3.Gameplay.Objects.Spawners;
using Sims3.SimIFace;

namespace Sims3.Gameplay.Destrospean.ExpandedAlchemy.Register
{
    public class PotionShopConsignmentRegister : Objects.Register.PotionShopConsignmentRegister
    {
        public new ObjectGuid CreateObjectCallBack(object customData, ref Simulator.ObjectInitParameters initData, Quality quality)
        {
            sItemsWereBought = true;
            var potionShopConsignmentRegisterObjectData = (PotionShopConsignmentRegisterObjectData)customData;
            mObjectsForSale.Remove(potionShopConsignmentRegisterObjectData);
            if (potionShopConsignmentRegisterObjectData.mObject is RockGemMetalBase rockGemMetal)
            {
                var guid = rockGemMetal.Guid;
                if (rockGemMetal is Gem gem)
                {
                    var newGem = gem.IsCut() ? Gem.MakeCutGem(guid, gem.CutName, gem.IsLargeSize, true) : (Gem)RockGemMetalBase.Make(guid, true);
                    newGem.mIsLargeSize = gem.IsLargeSize;
                    potionShopConsignmentRegisterObjectData.Destroy();
                    return newGem.ObjectId;
                }
                if (rockGemMetal is Metal metal)
                {
                    var newMetal = metal.IsMatchingCut("smelted") ? Metal.MakeSmeltedMetal(guid, metal.mIsLargeSize, true) : (Metal)RockGemMetalBase.Make(guid, true);
                    newMetal.mIsLargeSize = metal.mIsLargeSize;
                    potionShopConsignmentRegisterObjectData.Destroy();
                    return newMetal.ObjectId;
                }
                potionShopConsignmentRegisterObjectData.Destroy();
                return RockGemMetalBase.Make(guid, true)?.ObjectId ?? ObjectGuid.InvalidObjectGuid;
            }
            if (potionShopConsignmentRegisterObjectData.mObject is Terrarium terrarium)
            {
                var guid = terrarium.InsectType;
                potionShopConsignmentRegisterObjectData.Destroy();
                return Insect.Create(guid)?.ObjectId ?? ObjectGuid.InvalidObjectGuid;
            }
            return potionShopConsignmentRegisterObjectData.ObjectId;
        }

        public override Dictionary<string, List<StoreItem>> ItemDictionary(Sim sim)
        {
            if (mObjectsForSale == null)
            {
                mObjectsForSale = new List<PotionShopConsignmentRegisterObjectData>();
            }
            var num = 0;
            while (num < mObjectsForSale.Count)
            {
                if (mObjectsForSale[num].IsExpired)
                {
                    mObjectsForSale.RemoveAt(num);
                }
                else
                {
                    num++;
                }
            }
            if (mObjectsForSale.Count < kNumberOfObjectsForSale)
            {
                var list = new List<PotionShopConsignmentRegisterData>(sData);
                foreach (var item in mObjectsForSale)
                {
                    for (var i = 0; i < list.Count; i++)
                    {
                        if (item.DataGuid == list[i].Guid)
                        {
                            list.RemoveAt(i);
                            break;
                        }
                    }
                }
                while (mObjectsForSale.Count < kNumberOfObjectsForSale && list.Count > 0)
                {
                    var weightedRandomObjectFromList = RandomUtil.GetWeightedRandomObjectFromList(list);
                    var potionShopConsignmentRegisterObjectData = PotionShopConsignmentRegisterObjectData.Create(weightedRandomObjectFromList);
                    if (potionShopConsignmentRegisterObjectData != null)
                    {
                        mObjectsForSale.Add(potionShopConsignmentRegisterObjectData);
                    }
                    list.Remove(weightedRandomObjectFromList);
                }
                if (mObjectsForSale.Count < kNumberOfObjectsForSale + kNumRotatingPotions)
                {
                    for (var i = 0; i < kNumRotatingPotions; i++)
                    {
                        var potionShopConsignmentRegisterData = new PotionShopConsignmentRegisterData();
                        potionShopConsignmentRegisterData.CreatePotion();
                        var potionShopConsignmentRegisterObjectData = PotionShopConsignmentRegisterObjectData.Create(potionShopConsignmentRegisterData);
                        potionShopConsignmentRegisterObjectData.ShowTooltip = true;
                        mObjectsForSale.Add(potionShopConsignmentRegisterObjectData);
                    }
                }
            }
            var dictionary = new Dictionary<string, List<StoreItem>>();
            var storeItems = new List<StoreItem>();
            foreach (var objectForSale in mObjectsForSale)
            {
                var limitedQuantityStoreItemWithTooltip = new LimitedQuantityStoreItemWithTooltip(objectForSale.GetLocalizedName(), objectForSale.GetValue(), objectForSale, objectForSale.GetThumbnailKey(), objectForSale.GetStoreUiItemId(), CreateObjectCallBack, null, null, 1, objectForSale.CreateTooltip)
                {
                    SetPurchasedPrice = false,
                    HideQuantity = true
                };
                storeItems.Add(limitedQuantityStoreItemWithTooltip);
            }
            if (Sim.ActiveActor != null)
            {
                var consignedObjects = new List<ConsignedObject>();
                foreach (var simDescription in Sim.ActiveActor.Household.SimDescriptions)
                {
                    if (sConsignedObjects.TryGetValue(simDescription.SimDescriptionId, out var value))
                    {
                        consignedObjects.AddRange(value);
                    }
                }
                if (consignedObjects.Count > 0)
                {
                    RandomUtil.RandomizeListOfObjects(consignedObjects);
                    for (var i = 0; i < kMaximumNumberOfPlayerItemsDisplayed && i < consignedObjects.Count; i++)
                    {
                        var name = LocalizeString("UserConsignedItem", consignedObjects[i].DisplayName, Sim.ActiveActor.Household.Name);
                        float price = PotionShopConsignmentRegisterData.GetRoundedValue(GetValueOfSale(consignedObjects[i], null));
                        var limitedQuantityStoreItemWithTooltip = new LimitedQuantityStoreItemWithTooltip(name, price, consignedObjects[i].Object, consignedObjects[i].Thumbnail, consignedObjects[i].StoreUIItemID, null, null, null, 0, consignedObjects[i].CreateTooltip)
                        {
                            SetPurchasedPrice = false,
                            HideQuantity = true
                        };
                        storeItems.Add(limitedQuantityStoreItemWithTooltip);
                    }
                }
            }
            dictionary.Add("All", storeItems);
            return dictionary;
        }
    }
}
