using System;
using System.Reflection;
using Sims3.UI.CAS;

namespace Destrospean.MoreFavorites
{
    public class Main
    {
        [Sims3.SimIFace.Tunable]
        protected static bool kInstantiator;

        static Main()
        {
            BindingFlags nonPublicInstance = BindingFlags.NonPublic | BindingFlags.Instance,
            nonPublicStatic = BindingFlags.NonPublic | BindingFlags.Static;
            ReplaceMethod(typeof(CASCharacter).GetMethod("GetFavoriteFood"), typeof(Replacements).GetMethod("GetFavoriteFood"));
            ReplaceMethod(typeof(CASCharacter).GetMethod("GetFavoriteFoodName", nonPublicStatic), typeof(Replacements).GetMethod("GetFavoriteFood"));
            ReplaceMethod(typeof(CASCharacter).GetMethod("GetFavoriteFoodPngName"), typeof(Replacements).GetMethod("GetFavoriteFoodPngName"));
            ReplaceMethod(typeof(CASCharacter).GetMethod("GetFavoriteFoodSmallIcon"), typeof(Replacements).GetMethod("GetFavoriteFoodSmallIcon"));
            ReplaceMethod(typeof(CASCharacter).GetMethod("GetFavoriteMusic"), typeof(Replacements).GetMethod("GetFavoriteMusic"));
            ReplaceMethod(typeof(CASCharacter).GetMethod("GetFavoriteMusicName", nonPublicStatic), typeof(Replacements).GetMethod("GetFavoriteMusic"));
            ReplaceMethod(typeof(CASCharacter).GetMethod("GetFavoriteMusicPngName"), typeof(Replacements).GetMethod("GetFavoriteMusicPngName"));
            ReplaceMethod(typeof(CASCharacter).GetMethod("GetFavoriteMusicSmallIcon"), typeof(Replacements).GetMethod("GetFavoriteMusicSmallIcon"));
            ReplaceMethod(typeof(CASCharacter).GetMethod("GetInstalledFavoriteFoodList"), typeof(Replacements).GetMethod("GetInstalledFavoriteFoodList"));
            ReplaceMethod(typeof(CASCharacter).GetMethod("GetInstalledFavoriteMusicList"), typeof(Replacements).GetMethod("GetInstalledFavoriteMusicList"));
            ReplaceMethod(typeof(CASCharacter).GetMethod("GetMusicIcon", nonPublicStatic), typeof(Replacements).GetMethod("GetMusicIcon"));
            ReplaceMethod(typeof(CASCharacter).GetMethod("OnFavoritesVisibilityChange", nonPublicInstance), typeof(Replacements).GetMethod("OnFavoritesVisibilityChange"));
            ReplaceMethod(typeof(CASCharacter).GetMethod("PopulateFavoritesGrid", nonPublicStatic), typeof(Replacements).GetMethod("PopulateFavoritesGrid"));
            ReplaceMethod(typeof(CASCharacter).GetMethod("RandomizeAllFavorites"), typeof(Replacements).GetMethod("RandomizeAllFavorites"));
            ReplaceMethod(typeof(Sims3.Gameplay.CAS.CASLogic).GetMethod("ChangeFavoriteFoodToVegetarian", nonPublicInstance), typeof(Replacements).GetMethod("ChangeFavoriteFoodToVegetarian"));
            ReplaceMethod(typeof(Sims3.Gameplay.CAS.CASLogic).GetMethod("GetRecipe"), typeof(Replacements).GetMethod("GetRecipe"));
            ReplaceMethod(typeof(Sims3.UI.ChangeFavoritesDialog).GetMethod("OnFavoritesVisibilityChange", nonPublicInstance), typeof(Replacements.ChangeFavoritesDialogPatch).GetMethod("OnFavoritesVisibilityChange"));
            ReplaceMethod(typeof(Sims3.UI.ChangeFavoritesDialog).GetMethod("PopulateFavoritesGrid", nonPublicInstance), typeof(Replacements.ChangeFavoritesDialogPatch).GetMethod("PopulateFavoritesGrid"));
            ReplaceMethod(typeof(Sims3.UI.ChangeFavoritesDialog).GetMethod("RandomizeAllFavorites", nonPublicInstance), typeof(Replacements.ChangeFavoritesDialogPatch).GetMethod("RandomizeAllFavorites"));
            ReplaceMethod(typeof(Sims3.Gameplay.Objects.CookingObjects.EatHeldFood).GetMethod("Run", nonPublicInstance), typeof(Replacements.EatHeldFoodPatch).GetMethod("Run"));
            ReplaceMethod(typeof(Sims3.Gameplay.Objects.Appliances.FutureBar).GetMethod("AddDrinkEffects"), typeof(Replacements).GetMethod("AddDrinkEffects"));
            ReplaceMethod(typeof(Sims3.Gameplay.Objects.Appliances.FutureBar).GetMethod("CreateDrinkList"), typeof(Replacements).GetMethod("CreateDrinkList"));
            ReplaceMethod(typeof(Sims3.Gameplay.Objects.Appliances.FutureBar.OrderDrinks).GetMethod("Run", nonPublicInstance), typeof(Replacements.OrderDrinksPatch).GetMethod("Run"));
            ReplaceMethod(typeof(Sims3.Gameplay.CAS.SimDescription).GetMethod("RandomizeFavoriteMusic", nonPublicInstance), typeof(Replacements).GetMethod("RandomizeFavoriteMusic"));
            ReplaceMethod(typeof(Sims3.Gameplay.CAS.SimDescription).GetMethod("RandomizePreferences"), typeof(Replacements).GetMethod("RandomizePreferences"));
            ReplaceMethod(typeof(Sims3.Gameplay.Objects.Electronics.Stereo).GetMethod("AddEnjoyingMusicCallback", nonPublicInstance), typeof(Replacements.StereoPatch).GetMethod("AddEnjoyingMusicCallback"));
            ReplaceMethod(typeof(Sims3.Gameplay.Objects.Electronics.StereoStationData).GetMethod("GetStationName"), typeof(Replacements).GetMethod("GetStationName"));
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                switch (assembly.GetName().Name)
                {
                    case "icarusallsorts.EatHeldFoodOverride":
                        ReplaceMethod(assembly.GetType("Sims3.Gameplay.Objects.CookingObjects.icarusallsorts.EatHeldFoodOverride").GetMethod("Run"), typeof(Replacements.EatHeldFoodPatch).GetMethod("RunIcarusAllSortsOverride"));
                        break;
                    case "NRaasMasterControllerCheats":
                        NRaasMasterControllerIntegration.Init();
                        break;
                }
            }
            Sims3.SimIFace.LoadSaveManager.ObjectGroupsPreLoad += FavoritesUtils.InitFavorites;
        }

        /// <summary>This method was borrowed from Lazy Duchess' Mono Patcher</summary>
        public static void ReplaceMethod(MethodInfo oldMethod, MethodInfo newMethod)
        {
            byte[] replacementByteArray = new byte[40];
            System.Runtime.InteropServices.Marshal.Copy(newMethod.MethodHandle.Value, replacementByteArray, 0, 40);
            System.Runtime.InteropServices.Marshal.Copy(replacementByteArray, 0, oldMethod.MethodHandle.Value, 24);
            System.Runtime.InteropServices.Marshal.Copy(replacementByteArray, 28, new IntPtr(oldMethod.MethodHandle.Value.ToInt32() + 28), 12);
        }
    }
}
