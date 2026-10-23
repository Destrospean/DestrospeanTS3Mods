using System;
using System.Collections.Generic;
using System.Reflection;
using NRaas.MasterControllerSpace.SelectionCriteria;
using NRaas.MasterControllerSpace.Sims.Intermediate.Favorites;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.UI;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI.CAS;
using Tuning = Sims3.Gameplay.Destrospean.MoreFavorites;

namespace Destrospean.MoreFavorites
{
    public class NRaasMasterControllerIntegration
    {
        public class ChangeFavoriteColorPatch
        {
            public bool Run(SimDescription me, bool singleSelection)
            {
                FieldInfo favoriteColorField = typeof(ChangeFavoriteColor).GetField("mFavoriteColor", BindingFlags.NonPublic | BindingFlags.Instance);
                if (!((bool)typeof(ChangeFavoriteColor).GetProperty("ApplyAll", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this, null)))
                {
                    List<PreferenceColor.Item> options = new List<PreferenceColor.Item>();
                    foreach (CASCharacter.NameColorPair nameColorPair in CASCharacter.kColors)
                    {
                        if (!Tuning.kAllowBlacklistedFavoritesInMasterControllerDialogs && nameColorPair.IsBlacklisted() || !Tuning.kAllowHiddenFavoritesInMasterControllerDialogs && nameColorPair.IsHidden())
                        {
                            continue;
                        }
                        options.Add(new PreferenceColor.Item(nameColorPair.mColor, me.FavoriteColor == nameColorPair.mColor ? 1 : 0));
                    }
                    PreferenceColor.Item choice = new NRaas.CommonSpace.Selection.CommonSelection<PreferenceColor.Item>((string)typeof(ChangeFavoriteColor).GetProperty("Name").GetValue(this, null), me.FullName, options).SelectSingle();
                    if (choice == null)
                    {
                        return false;
                    }
                    favoriteColorField.SetValue(this, choice.Value);
                }
                me.FavoriteColor = (Color)favoriteColorField.GetValue(this);
                if (Sims3.Gameplay.Core.PlumbBob.SelectedActor == me.CreatedSim)
                {
                    ((HudModel)Responder.Instance.HudModel).OnSimFavoritesChanged(me.CreatedSim.ObjectId);
                }
                return true;
            }
        }

        public class ChangeFavoriteFoodPatch
        {
            public bool Run(SimDescription me, bool singleSelection)
            {
                FieldInfo favoriteFoodField = typeof(ChangeFavoriteFood).GetField("mFavoriteFood", BindingFlags.NonPublic | BindingFlags.Instance);
                if (!((bool)typeof(ChangeFavoriteFood).GetProperty("ApplyAll", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this, null)))
                {
                    List<PreferenceFood.Item> options = new List<PreferenceFood.Item>();
                    foreach (FavoriteFoodType foodType in Enum.GetValues(typeof(FavoriteFoodType)))
                    {
                        if (0 < foodType && foodType < FavoriteFoodType.Count)
                        {
                            if (!Tuning.kAllowBlacklistedFavoritesInMasterControllerDialogs && foodType.IsBlacklisted() || !Tuning.kAllowHiddenFavoritesInMasterControllerDialogs && foodType.IsHidden())
                            {
                                continue;
                            }
                            options.Add(new PreferenceFood.Item(foodType, me.FavoriteFood == foodType ? 1 : 0));
                        }
                    }
                    foreach (FavoriteFoodType foodType in FavoritesUtils.FavoriteFoodDictionary.Keys)
                    {
                        if (Array.Exists(Enum.GetNames(typeof(FavoriteFoodType)), x => x == FavoritesUtils.FavoriteFoodDictionary[foodType].Name) || !Tuning.kAllowBlacklistedFavoritesInMasterControllerDialogs && foodType.IsBlacklisted() || !Tuning.kAllowHiddenFavoritesInMasterControllerDialogs && foodType.IsHidden())
                        {
                            continue;
                        }
                        options.Add(new PreferenceFood.Item(foodType, me.FavoriteFood == foodType ? 1 : 0));
                    }
                    PreferenceFood.Item choice = new NRaas.CommonSpace.Selection.CommonSelection<PreferenceFood.Item>((string)typeof(ChangeFavoriteFood).GetProperty("Name").GetValue(this, null), me.FullName, options).SelectSingle();
                    if (choice == null)
                    {
                        return false;
                    }
                    favoriteFoodField.SetValue(this, choice.Value);
                }
                me.mFavouriteFoodType = (FavoriteFoodType)favoriteFoodField.GetValue(this);
                if (Sims3.Gameplay.Core.PlumbBob.SelectedActor == me.CreatedSim)
                {
                    ((HudModel)Responder.Instance.HudModel).OnSimFavoritesChanged(me.CreatedSim.ObjectId);
                }
                return true;
            }
        }

        public class ChangeFavoriteMusicPatch
        {
            public bool Run(SimDescription me, bool singleSelection)
            {
                FieldInfo favoriteMusicField = typeof(ChangeFavoriteMusic).GetField("mFavoriteMusic", BindingFlags.NonPublic | BindingFlags.Instance);
                if (!((bool)typeof(ChangeFavoriteMusic).GetProperty("ApplyAll", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this, null)))
                {
                    List<PreferenceMusic.Item> options = new List<PreferenceMusic.Item>();
                    foreach (FavoriteMusicType musicType in Enum.GetValues(typeof(FavoriteMusicType)))
                    {
                        if (0 < musicType && musicType < FavoriteMusicType.Count)
                        {
                            if (!Tuning.kAllowBlacklistedFavoritesInMasterControllerDialogs && musicType.IsBlacklisted() || !Tuning.kAllowHiddenFavoritesInMasterControllerDialogs && musicType.IsHidden())
                            {
                                continue;
                            }
                            options.Add(new PreferenceMusic.Item(musicType, me.FavoriteMusic == musicType ? 1 : 0));
                        }
                    }
                    foreach (FavoriteMusicType musicType in FavoritesUtils.FavoriteMusicDictionary.Keys)
                    {
                        if (Array.Exists(Enum.GetNames(typeof(FavoriteMusicType)), x => x == FavoritesUtils.FavoriteMusicDictionary[musicType].Name) || !Tuning.kAllowBlacklistedFavoritesInMasterControllerDialogs && musicType.IsBlacklisted() || !Tuning.kAllowHiddenFavoritesInMasterControllerDialogs && musicType.IsHidden())
                        {
                            continue;
                        }
                        options.Add(new PreferenceMusic.Item(musicType, me.FavoriteMusic == musicType ? 1 : 0));
                    }
                    PreferenceMusic.Item choice = new NRaas.CommonSpace.Selection.CommonSelection<PreferenceMusic.Item>((string)typeof(ChangeFavoriteMusic).GetProperty("Name").GetValue(this, null), me.FullName, options).SelectSingle();
                    if (choice == null)
                    {
                        return false;
                    }
                    favoriteMusicField.SetValue(this, choice.Value);
                }
                me.mFavouriteMusicType = (FavoriteMusicType)favoriteMusicField.GetValue(this);
                if (Sims3.Gameplay.Core.PlumbBob.SelectedActor == me.CreatedSim)
                {
                    ((HudModel)Responder.Instance.HudModel).OnSimFavoritesChanged(me.CreatedSim.ObjectId);
                }
                return true;
            }
        }

        public class PreferenceMusicItemPatch
        {
            public void SetValue(FavoriteMusicType value, FavoriteMusicType storeType)
            {
                typeof(PreferenceMusic.Item).GetField("mValue", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(this, value);
                typeof(PreferenceMusic.Item).GetField("mName", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(this, CASCharacter.GetFavoriteMusic(value));
                ((PreferenceMusic.Item)(object)this).SetThumbnail(ResourceKey.CreatePNGKey(CASCharacter.GetFavoriteMusicPngName(value), FavoritesUtils.FavoriteMusicDictionary.ContainsKey(storeType) ? 0 : ResourceUtils.ProductVersionToGroupId(Responder.Instance.GetProductVersionForStereoStation(storeType))));
            }
        }

        public static void Init()
        {
            MoreFavorites.Main.ReplaceMethod(typeof(ChangeFavoriteColor).GetMethod("Run", BindingFlags.NonPublic | BindingFlags.Instance), typeof(ChangeFavoriteColorPatch).GetMethod("Run"));
            MoreFavorites.Main.ReplaceMethod(typeof(ChangeFavoriteFood).GetMethod("Run", BindingFlags.NonPublic | BindingFlags.Instance), typeof(ChangeFavoriteFoodPatch).GetMethod("Run"));
            MoreFavorites.Main.ReplaceMethod(typeof(ChangeFavoriteMusic).GetMethod("Run", BindingFlags.NonPublic | BindingFlags.Instance), typeof(ChangeFavoriteMusicPatch).GetMethod("Run"));
            MoreFavorites.Main.ReplaceMethod(typeof(PreferenceMusic.Item).GetMethod("SetValue"), typeof(PreferenceMusicItemPatch).GetMethod("SetValue"));
        }
    }
}
