using System;
using System.Collections.Generic;
using Sims3.Gameplay.Objects.Appliances;
using Sims3.Gameplay.Objects.Electronics;
using Sims3.Gameplay.Objects.FoodObjects;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI.CAS;

namespace Destrospean.MoreFavorites
{
    public static class FavoritesUtils
    {
        public static readonly List<string> FavoriteColorBlacklist = new List<string>(),
        FavoriteColorHiddenList = new List<string>(),
        OriginalFavoriteColors = new List<string>(Array.ConvertAll(CASCharacter.kColors, x => x.mName));

        public static readonly Dictionary<string, string> FavoriteColorParentDictionary = new Dictionary<string, string>();

        public static readonly List<FavoriteFoodType> FavoriteFoodBlacklist = new List<FavoriteFoodType>(),
        FavoriteFoodHiddenList = new List<FavoriteFoodType>();

        public static readonly Dictionary<FavoriteFoodType, FavoriteFood> FavoriteFoodDictionary = new Dictionary<FavoriteFoodType, FavoriteFood>();

        public static readonly List<FavoriteMusicType> FavoriteMusicBlacklist = new List<FavoriteMusicType>(),
        FavoriteMusicHiddenList = new List<FavoriteMusicType>();

        public static readonly Dictionary<FavoriteMusicType, FavoriteMusic> FavoriteMusicDictionary = new Dictionary<FavoriteMusicType, FavoriteMusic>();

        public static readonly uint[] FutureBarGlassRGBValues =
            {
                0x000000,
                0x000055,
                0x0000AA,
                0x0000FF,
                0x002400,
                0x002455,
                0x0024AA,
                0x0024FF,
                0x004900,
                0x004955,
                0x0049AA,
                0x0049FF,
                0x006D00,
                0x006D55,
                0x006DAA,
                0x006DFF,
                0x009200,
                0x009255,
                0x0092AA,
                0x0092FF,
                0x00B600,
                0x00B655,
                0x00B6AA,
                0x00B6FF,
                0x00DB00,
                0x00DB55,
                0x00DBAA,
                0x00DBFF,
                0x00FF00,
                0x00FF55,
                0x00FFAA,
                0x00FFFF,
                0x240000,
                0x240055,
                0x2400AA,
                0x2400FF,
                0x242400,
                0x242455,
                0x2424AA,
                0x2424FF,
                0x244900,
                0x244955,
                0x2449AA,
                0x2449FF,
                0x246D00,
                0x246D55,
                0x246DAA,
                0x246DFF,
                0x249200,
                0x249255,
                0x2492AA,
                0x2492FF,
                0x24B600,
                0x24B655,
                0x24B6AA,
                0x24B6FF,
                0x24DB00,
                0x24DB55,
                0x24DBAA,
                0x24DBFF,
                0x24FF00,
                0x24FF55,
                0x24FFAA,
                0x24FFFF,
                0x490000,
                0x490055,
                0x4900AA,
                0x4900FF,
                0x492400,
                0x492455,
                0x4924AA,
                0x4924FF,
                0x494900,
                0x494955,
                0x4949AA,
                0x4949FF,
                0x496D00,
                0x496D55,
                0x496DAA,
                0x496DFF,
                0x499200,
                0x499255,
                0x4992AA,
                0x4992FF,
                0x49B600,
                0x49B655,
                0x49B6AA,
                0x49B6FF,
                0x49DB00,
                0x49DB55,
                0x49DBAA,
                0x49DBFF,
                0x49FF00,
                0x49FF55,
                0x49FFAA,
                0x49FFFF,
                0x6D0000,
                0x6D0055,
                0x6D00AA,
                0x6D00FF,
                0x6D2400,
                0x6D2455,
                0x6D24AA,
                0x6D24FF,
                0x6D4900,
                0x6D4955,
                0x6D49AA,
                0x6D49FF,
                0x6D6D00,
                0x6D6D55,
                0x6D6DAA,
                0x6D6DFF,
                0x6D9200,
                0x6D9255,
                0x6D92AA,
                0x6D92FF,
                0x6DB600,
                0x6DB655,
                0x6DB6AA,
                0x6DB6FF,
                0x6DDB00,
                0x6DDB55,
                0x6DDBAA,
                0x6DDBFF,
                0x6DFF00,
                0x6DFF55,
                0x6DFFAA,
                0x6DFFFF,
                0x920000,
                0x920055,
                0x9200AA,
                0x9200FF,
                0x922400,
                0x922455,
                0x9224AA,
                0x9224FF,
                0x924900,
                0x924955,
                0x9249AA,
                0x9249FF,
                0x926D00,
                0x926D55,
                0x926DAA,
                0x926DFF,
                0x929200,
                0x929255,
                0x9292AA,
                0x9292FF,
                0x92B600,
                0x92B655,
                0x92B6AA,
                0x92B6FF,
                0x92DB00,
                0x92DB55,
                0x92DBAA,
                0x92DBFF,
                0x92FF00,
                0x92FF55,
                0x92FFAA,
                0x92FFFF,
                0xB60000,
                0xB60055,
                0xB600AA,
                0xB600FF,
                0xB62400,
                0xB62455,
                0xB624AA,
                0xB624FF,
                0xB64900,
                0xB64955,
                0xB649AA,
                0xB649FF,
                0xB66D00,
                0xB66D55,
                0xB66DAA,
                0xB66DFF,
                0xB69200,
                0xB69255,
                0xB692AA,
                0xB692FF,
                0xB6B600,
                0xB6B655,
                0xB6B6AA,
                0xB6B6FF,
                0xB6DB00,
                0xB6DB55,
                0xB6DBAA,
                0xB6DBFF,
                0xB6FF00,
                0xB6FF55,
                0xB6FFAA,
                0xB6FFFF,
                0xDB0000,
                0xDB0055,
                0xDB00AA,
                0xDB00FF,
                0xDB2400,
                0xDB2455,
                0xDB24AA,
                0xDB24FF,
                0xDB4900,
                0xDB4955,
                0xDB49AA,
                0xDB49FF,
                0xDB6D00,
                0xDB6D55,
                0xDB6DAA,
                0xDB6DFF,
                0xDB9200,
                0xDB9255,
                0xDB92AA,
                0xDB92FF,
                0xDBB600,
                0xDBB655,
                0xDBB6AA,
                0xDBB6FF,
                0xDBDB00,
                0xDBDB55,
                0xDBDBAA,
                0xDBDBFF,
                0xDBFF00,
                0xDBFF55,
                0xDBFFAA,
                0xDBFFFF,
                0xFF0000,
                0xFF0055,
                0xFF00AA,
                0xFF00FF,
                0xFF2400,
                0xFF2455,
                0xFF24AA,
                0xFF24FF,
                0xFF4900,
                0xFF4955,
                0xFF49AA,
                0xFF49FF,
                0xFF6D00,
                0xFF6D55,
                0xFF6DAA,
                0xFF6DFF,
                0xFF9200,
                0xFF9255,
                0xFF92AA,
                0xFF92FF,
                0xFFB600,
                0xFFB655,
                0xFFB6AA,
                0xFFB6FF,
                0xFFDB00,
                0xFFDB55,
                0xFFDBAA,
                0xFFDBFF,
                0xFFFF00,
                0xFFFF55,
                0xFFFFAA,
                0xFFFFFF
            };

        public class FavoriteBase
        {
            public string IconKey
            {
                get;
                protected set;
            }

            public readonly string Name, Parent;

            public string SmallIconKey
            {
                get;
                protected set;
            }

            public FavoriteBase(string name, string iconKey, string smallIconKey, string parent)
            {
                IconKey = iconKey;
                Name = name;
                Parent = parent;
                SmallIconKey = smallIconKey;
            }
        }

        public class FavoriteFood : FavoriteBase
        {
            public readonly Recipe Recipe;

            public FavoriteFood(string name, Recipe recipe, string iconKey, string smallIconKey, string parent) : base(name, iconKey, smallIconKey, parent)
            {
                IconKey = string.IsNullOrEmpty(iconKey) ? "cas_favs_food_i_" + name : iconKey;
                Recipe = recipe;
                SmallIconKey = string.IsNullOrEmpty(smallIconKey) ? "cas_favs_food_i_" + name + "_s" : smallIconKey;
            }
        }

        public class FavoriteMusic : FavoriteBase
        {
            public readonly StereoStationData StereoStationData;

            public FavoriteMusic(string name, StereoStationData stereoStationData, string iconKey, string smallIconKey, string parent) : base(name, iconKey, smallIconKey, parent)
            {
                IconKey = string.IsNullOrEmpty(iconKey) ? "cas_favs_music_i_" + name : iconKey;
                SmallIconKey = string.IsNullOrEmpty(smallIconKey) ? "cas_favs_music_i_" + name + "_s" : smallIconKey;
                StereoStationData = stereoStationData;
            }
        }

        public static Color FindClosestColor(IEnumerable<Color> palette, Color color)
        {
            List<Color> colors = new List<Color>(palette);
            colors.Sort((a, b) => GetDistance(a, color).CompareTo(GetDistance(b, color)));
            return colors[0];
        }

        public static double GetDistance(Color a, Color b)
        {
            return Math.Sqrt(Math.Pow(a.Red - b.Red, 2) + Math.Pow(a.Green - b.Green, 2) + Math.Pow(a.Blue - b.Blue, 2));
        }

        public static string GetMaterialStateName(this FutureBar.OrderDrinks.Definition definition)
        {
            return OriginalFavoriteColors.Contains(definition.DrinkName) ? "drink" + definition.DrinkName : "MoreFavs_" + FindClosestColor(Array.ConvertAll(FutureBarGlassRGBValues, x => new Color(x | 0xFF000000)), definition.DrinkColor).ARGB.ToString("X8").Substring(2);
        }

        public static void InitFavorites()
        {
            List<CASCharacter.NameColorPair> favoriteColorList = new List<CASCharacter.NameColorPair>(CASCharacter.kColors);
            List<FutureBar.FavesAndOpposites> favoriteAndOppositeDrinkList = new List<FutureBar.FavesAndOpposites>(FutureBar.kFavesAndOpposites);
            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetType("Destrospean.MoreFavorites.Data") == null)
                {
                    continue;
                }
                System.Xml.XmlReader reader = Simulator.ReadXml(new ResourceKey(ResourceUtils.HashString64(assembly.GetName().Name), 0x333406C, 0));
                while (reader.Read())
                {
                    if (reader.NodeType == System.Xml.XmlNodeType.Element)
                    {
                        if (reader.Name == "Favorites" || reader.Name == "Favourites")
                        {
                            reader.MoveToContent();
                        }
                        else if (reader.Name == "FavoriteColor" || reader.Name == "FavouriteColour")
                        {
                            string hex = reader.GetAttribute("Hex"),
                            name = reader.GetAttribute("Name"),
                            opposite = reader.GetAttribute("Opposite");
                            uint argb;
                            int index, startIndex;
                            if (!string.IsNullOrEmpty(hex) && !string.IsNullOrEmpty(name) && uint.TryParse(("FFFFFFFF".Remove(0, hex.Length - (startIndex = hex.StartsWith("#") ? 1 : hex.StartsWith("0x") ? 2 : 0))) + hex.Substring(startIndex), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out argb))
                            {
                                if ((index = favoriteColorList.FindIndex(x => x.mName == name)) == -1)
                                {
                                    favoriteColorList.Add(new CASCharacter.NameColorPair(name, new Color(argb | 0xFF000000)));
                                }
                                else
                                {
                                    favoriteColorList[index] = new CASCharacter.NameColorPair(name, new Color(argb | 0xFF000000));
                                }
                                FavoriteColorParentDictionary.Add(name, reader.GetAttribute("Parent"));
                                if (!string.IsNullOrEmpty(opposite))
                                {
                                    if ((index = favoriteAndOppositeDrinkList.FindIndex(x => x.mFaveDrink == name || x.mOppositeDrink == name)) > -1)
                                    {
                                        favoriteAndOppositeDrinkList.RemoveAt(index);
                                    }
                                    favoriteAndOppositeDrinkList.Add(new FutureBar.FavesAndOpposites(name, opposite));
                                }
                            }
                            bool hidden;
                            if (bool.TryParse(reader.GetAttribute("Hidden"), out hidden) && hidden)
                            {
                                FavoriteColorHiddenList.Add(name);
                            }
                        }
                        else if (reader.Name == "FavoriteFood" || reader.Name == "FavouriteFood")
                        {
                            string recipeKey = reader.GetAttribute("Recipe_Key");
                            Recipe recipe;
                            bool isGroup,
                            isHidden,
                            isValid = Recipe.NameToRecipeHash.TryGetValue(recipeKey, out recipe);
                            if (string.IsNullOrEmpty(recipeKey) || !isValid && (!bool.TryParse(reader.GetAttribute("IsGroup"), out isGroup) || isGroup))
                            {
                                continue;
                            }
                            FavoriteFoodType favoriteFoodType = Array.Exists(Enum.GetNames(typeof(FavoriteFoodType)), x => x == recipeKey) && isValid && recipe != null ? (FavoriteFoodType)Enum.Parse(typeof(FavoriteFoodType), recipeKey) : (FavoriteFoodType)ResourceUtils.HashString32(recipeKey);
                            FavoriteFoodDictionary[favoriteFoodType] = new FavoriteFood(recipeKey, isValid ? recipe : null, reader.GetAttribute("Icon_Key"), reader.GetAttribute("Small_Icon_Key"), reader.GetAttribute("Parent"));
                            if (bool.TryParse(reader.GetAttribute("Hidden"), out isHidden) && isHidden)
                            {
                                FavoriteFoodHiddenList.Add(favoriteFoodType);
                            }
                        }
                        else if (reader.Name == "FavoriteMusic" || reader.Name == "FavouriteMusic")
                        {
                            string stationName = reader.GetAttribute("Station_Name");
                            StereoStationData stereoStationData;
                            bool isGroup,
                            isHidden,
                            isValid = StereoStationData.sStereoStationDictionary.TryGetValue("Gameplay/Excel/Stereo/Stations:" + stationName, out stereoStationData);
                            if (string.IsNullOrEmpty(stationName) || !isValid && (!bool.TryParse(reader.GetAttribute("IsGroup"), out isGroup) || isGroup))
                            {
                                continue;
                            }
                            FavoriteMusicType favoriteMusicType = Array.Exists(Enum.GetNames(typeof(FavoriteMusicType)), x => x == stationName) && IsMusicTypeInstalled((FavoriteMusicType)Enum.Parse(typeof(FavoriteMusicType), stationName)) ? (FavoriteMusicType)Enum.Parse(typeof(FavoriteMusicType), stationName) : (FavoriteMusicType)ResourceUtils.HashString32("Gameplay/Excel/Stereo/Stations:" + stationName);
                            FavoriteMusicDictionary[favoriteMusicType] = new FavoriteMusic(stationName, isValid ? stereoStationData : null, reader.GetAttribute("Icon_Key"), reader.GetAttribute("Small_Icon_Key"), reader.GetAttribute("Parent"));
                            if (bool.TryParse(reader.GetAttribute("Hidden"), out isHidden) && isHidden)
                            {
                                FavoriteMusicHiddenList.Add(favoriteMusicType);
                            }
                        }
                        else if (reader.Name == "Unfavorite" || reader.Name == "Unfavourite")
                        {
                            int index;
                            string name = reader.GetAttribute("Name");
                            switch ((reader.GetAttribute("Favorite_Type") ?? reader.GetAttribute("Favourite_Type") ?? "").ToLowerInvariant())
                            {
                                case "color":
                                case "colour":
                                    FavoriteColorBlacklist.Add(name);
                                    break;
                                case "food":
                                    FavoriteFoodBlacklist.Add((index = Array.FindIndex(Enum.GetNames(typeof(FavoriteFoodType)), x => x.ToLowerInvariant() == name.ToLowerInvariant())) > -1 ? (FavoriteFoodType)Enum.Parse(typeof(FavoriteFoodType), Enum.GetNames(typeof(FavoriteFoodType))[index]) : (FavoriteFoodType)ResourceUtils.HashString32(name));
                                    break;
                                case "music":
                                    FavoriteMusicBlacklist.Add((index = Array.FindIndex(Enum.GetNames(typeof(FavoriteMusicType)), x => x.ToLowerInvariant() == name.ToLowerInvariant())) > -1 ? (FavoriteMusicType)Enum.Parse(typeof(FavoriteMusicType), Enum.GetNames(typeof(FavoriteMusicType))[index]) : (FavoriteMusicType)ResourceUtils.HashString32("Gameplay/Excel/Stereo/Stations:" + name));
                                    break;
                            }
                        }
                    }
                }
                reader.Close();
            }
            foreach (string colorName in FavoriteColorBlacklist)
            {
                int favoriteAndOppositeDrinkIndex = favoriteAndOppositeDrinkList.FindIndex(x => x.mFaveDrink == colorName);
                if (favoriteAndOppositeDrinkIndex > -1)
                {
                    favoriteAndOppositeDrinkList.RemoveAt(favoriteAndOppositeDrinkIndex);
                }
            }
            CASCharacter.kColors = favoriteColorList.ToArray();
            FutureBar.kFavesAndOpposites = favoriteAndOppositeDrinkList.ToArray();
        }

        public static bool IsBlacklisted(this FavoriteFoodType foodType)
        {
            return FavoriteFoodBlacklist.Contains(foodType);
        }

        public static bool IsBlacklisted(this FavoriteMusicType musicType)
        {
            return FavoriteMusicBlacklist.Contains(musicType);
        }

        public static bool IsBlacklisted(this CASCharacter.NameColorPair nameColorPair)
        {
            return FavoriteColorBlacklist.Contains(nameColorPair.mName);
        }

        public static bool IsFavoriteOfSim(this Recipe recipe, ISimDescription simDescription)
        {
            FavoriteFood favoriteFood;
            return simDescription.FavoriteFood != 0 && recipe != null && (recipe.Favorite == simDescription.FavoriteFood || FavoriteFoodDictionary.TryGetValue(simDescription.FavoriteFood, out favoriteFood) && (favoriteFood.Name == recipe.Key || new List<FavoriteFoodType>(FavoriteFoodDictionary.Keys).Exists(x => !x.IsBlacklisted() && FavoriteFoodDictionary[x].Parent == favoriteFood.Name && FavoriteFoodDictionary[x].Name == recipe.Key)));
        }

        public static bool IsFavoriteOfSim(this StereoStationData stereoStationData, ISimDescription simDescription)
        {
            FavoriteMusic favoriteMusic;
            return stereoStationData != null && (stereoStationData.MusicType == simDescription.FavoriteMusic || FavoriteMusicDictionary.TryGetValue(simDescription.FavoriteMusic, out favoriteMusic) && (favoriteMusic.Name == stereoStationData.mStationName.Split(':')[1] || new List<FavoriteMusicType>(FavoriteMusicDictionary.Keys).Exists(x => !x.IsBlacklisted() && FavoriteMusicDictionary[x].Parent == favoriteMusic.Name && FavoriteMusicDictionary[x].Name == stereoStationData.mStationName.Split(':')[1])));
        }

        public static bool IsHidden(this FavoriteFoodType foodType)
        {
            return FavoriteFoodHiddenList.Contains(foodType);
        }

        public static bool IsHidden(this FavoriteMusicType musicType)
        {
            return FavoriteMusicHiddenList.Contains(musicType);
        }

        public static bool IsHidden(this CASCharacter.NameColorPair nameColorPair)
        {
            return FavoriteColorHiddenList.Contains(nameColorPair.mName);
        }

        public static bool IsMusicTypeInstalled(FavoriteMusicType music)
        {
            switch (music)
            {
                case FavoriteMusicType.None:
                case FavoriteMusicType.Count:
                    return false;
                case FavoriteMusicType.France:
                case FavoriteMusicType.China:
                case FavoriteMusicType.Egypt:
                    return GameUtils.IsInstalled(ProductVersion.EP1);
                case FavoriteMusicType.Roots:
                case FavoriteMusicType.Soul:
                    return GameUtils.IsInstalled(ProductVersion.EP2);
                case FavoriteMusicType.Rockabilly:
                    return GameUtils.IsInstalled(ProductVersion.SP2);
                case FavoriteMusicType.HipHop:
                    return GameUtils.IsInstalled(ProductVersion.EP3);
                case FavoriteMusicType.Country:
                case FavoriteMusicType.RnB:
                case FavoriteMusicType.Songwriter:
                    return GameUtils.IsInstalled(ProductVersion.EP5);
                case FavoriteMusicType.DarkWave:
                    return GameUtils.IsInstalled(ProductVersion.EP7);
                case FavoriteMusicType.Disco:
                case FavoriteMusicType.Rap:
                case FavoriteMusicType.Rock:
                    return GameUtils.IsInstalled(ProductVersion.SP8);
                case FavoriteMusicType.GeekRock:
                    return GameUtils.IsInstalled(ProductVersion.EP9);
                case FavoriteMusicType.BeachParty:
                case FavoriteMusicType.IslandLife:
                    return GameUtils.IsInstalled(ProductVersion.EP10);
                case FavoriteMusicType.Horror:
                case FavoriteMusicType.Epic:
                case FavoriteMusicType.Spaghetti_Western:
                    return GameUtils.IsInstalled(ProductVersion.SP9);
                case FavoriteMusicType.FutureWorld:
                    return GameUtils.IsInstalled(ProductVersion.EP11);
            }
            return true;
        }
    }
}
