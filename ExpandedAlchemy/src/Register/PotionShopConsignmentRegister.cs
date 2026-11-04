using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Objects;
using Sims3.Gameplay.Objects.Insect;
using Sims3.Gameplay.Objects.Register;
using Sims3.Gameplay.Objects.Spawners;
using Sims3.SimIFace;
using System.Collections.Generic;

namespace Sims3.Gameplay.Destrospean.ExpandedAlchemy.Register
{
    public class PotionShopConsignmentRegister : Objects.Register.PotionShopConsignmentRegister
    {
        public new ObjectGuid CreateObjectCallBack(object customData, ref Simulator.ObjectInitParameters initData, Quality quality)
        {
            sItemsWereBought = true;
            PotionShopConsignmentRegisterObjectData potionShopConsignmentRegisterObjectData = (PotionShopConsignmentRegisterObjectData)customData;
            mObjectsForSale.Remove(potionShopConsignmentRegisterObjectData);
            if (potionShopConsignmentRegisterObjectData.mObject is RockGemMetalBase rockGemMetal)
            {
                RockGemMetal guid = rockGemMetal.Guid;
                if (rockGemMetal is Gem gem)
                {
                    Gem newGem = gem.IsCut() ? Gem.MakeCutGem(guid, gem.CutName, gem.IsLargeSize, true) : (Gem)Gem.Make(guid, true);
                    newGem.mIsLargeSize = gem.IsLargeSize;
                    potionShopConsignmentRegisterObjectData.Destroy();
                    return newGem?.ObjectId ?? ObjectGuid.InvalidObjectGuid;
                }
                if (rockGemMetal is Metal metal)
                {
                    Metal newMetal = metal.IsMatchingCut("smelted") ? Metal.MakeSmeltedMetal(guid, metal.mIsLargeSize, true) : (Metal)Metal.Make(guid, true);
                    newMetal.mIsLargeSize = metal.mIsLargeSize;
                    potionShopConsignmentRegisterObjectData.Destroy();
                    return newMetal?.ObjectId ?? ObjectGuid.InvalidObjectGuid;
                }
                potionShopConsignmentRegisterObjectData.Destroy();
                return RockGemMetalBase.Make(guid, true)?.ObjectId ?? ObjectGuid.InvalidObjectGuid;
            }
            if (potionShopConsignmentRegisterObjectData.mObject is Terrarium terrarium)
            {
                InsectType guid = terrarium.InsectType;
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
            int num = 0;
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
                List<PotionShopConsignmentRegisterData> list = new List<PotionShopConsignmentRegisterData>(sData);
                foreach (PotionShopConsignmentRegisterObjectData item in mObjectsForSale)
                {
                    for (int i = 0; i < list.Count; i++)
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
                    PotionShopConsignmentRegisterData weightedRandomObjectFromList = RandomUtil.GetWeightedRandomObjectFromList(list);
                    PotionShopConsignmentRegisterObjectData potionShopConsignmentRegisterObjectData = PotionShopConsignmentRegisterObjectData.Create(weightedRandomObjectFromList);
                    if (potionShopConsignmentRegisterObjectData != null)
                    {
                        mObjectsForSale.Add(potionShopConsignmentRegisterObjectData);
                    }
                    list.Remove(weightedRandomObjectFromList);
                }
                if (mObjectsForSale.Count < kNumberOfObjectsForSale + kNumRotatingPotions)
                {
                    for (int i = 0; i < kNumRotatingPotions; i++)
                    {
                        PotionShopConsignmentRegisterData potionShopConsignmentRegisterData = new PotionShopConsignmentRegisterData();
                        potionShopConsignmentRegisterData.CreatePotion();
                        PotionShopConsignmentRegisterObjectData potionShopConsignmentRegisterObjectData = PotionShopConsignmentRegisterObjectData.Create(potionShopConsignmentRegisterData);
                        potionShopConsignmentRegisterObjectData.ShowTooltip = true;
                        mObjectsForSale.Add(potionShopConsignmentRegisterObjectData);
                    }
                }
            }
            Dictionary<string, List<StoreItem>> dictionary = new Dictionary<string, List<StoreItem>>();
            List<StoreItem> storeItems = new List<StoreItem>();
            foreach (PotionShopConsignmentRegisterObjectData objectForSale in mObjectsForSale)
            {
                LimitedQuantityStoreItemWithTooltip limitedQuantityStoreItemWithTooltip = new LimitedQuantityStoreItemWithTooltip(objectForSale.GetLocalizedName(), objectForSale.GetValue(), objectForSale, objectForSale.GetThumbnailKey(), objectForSale.GetStoreUiItemId(), CreateObjectCallBack, null, null, 1, objectForSale.CreateTooltip);
                limitedQuantityStoreItemWithTooltip.SetPurchasedPrice = false;
                limitedQuantityStoreItemWithTooltip.HideQuantity = true;
                storeItems.Add(limitedQuantityStoreItemWithTooltip);
            }
            if (Sim.ActiveActor != null)
            {
                List<ConsignedObject> consignedObjects = new List<ConsignedObject>();
                foreach (SimDescription simDescription in Sim.ActiveActor.Household.SimDescriptions)
                {
                    if (sConsignedObjects.TryGetValue(simDescription.SimDescriptionId, out List<ConsignedObject> value))
                    {
                        consignedObjects.AddRange(value);
                    }
                }
                if (consignedObjects.Count > 0)
                {
                    RandomUtil.RandomizeListOfObjects(consignedObjects);
                    for (int i = 0; i < kMaximumNumberOfPlayerItemsDisplayed && i < consignedObjects.Count; i++)
                    {
                        string name = ShoppingRegister.LocalizeString("UserConsignedItem", consignedObjects[i].DisplayName, Sim.ActiveActor.Household.Name);
                        float price = PotionShopConsignmentRegisterData.GetRoundedValue(GetValueOfSale(consignedObjects[i], null));
                        LimitedQuantityStoreItemWithTooltip limitedQuantityStoreItemWithTooltip = new LimitedQuantityStoreItemWithTooltip(name, price, consignedObjects[i].Object, consignedObjects[i].Thumbnail, consignedObjects[i].StoreUIItemID, null, null, null, 0, consignedObjects[i].CreateTooltip);
                        limitedQuantityStoreItemWithTooltip.SetPurchasedPrice = false;
                        limitedQuantityStoreItemWithTooltip.HideQuantity = true;
                        storeItems.Add(limitedQuantityStoreItemWithTooltip);
                    }
                }
            }
            dictionary.Add("All", storeItems);
            return dictionary;
        }
    }
}
