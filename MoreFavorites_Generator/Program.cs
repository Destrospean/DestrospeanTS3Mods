using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Xml;
using Mono.Cecil;
using s3pi.Package;

namespace Destrospean.MoreFavorites.Generator
{
    class Program
    {
        private enum Locales
        {
            ENG_US,
            CHS_CN,
            CHT_CN,
            CZE_CZ,
            DAN_DK,
            DUT_NL,
            FIN_FI,
            FRE_FR,
            GER_DE,
            GRE_GR,
            HUN_HU,
            ITA_IT,
            JPN_JP,
            KOR_KR,
            NOR_NO,
            POL_PL,
            POR_PT,
            POR_BR,
            RUS_RU,
            SPA_ES,
            SPA_MX,
            SWE_SE,
            THA_TH
        }

        public static Bitmap Colorize(Bitmap source, Color color)
        {
            var result = new Bitmap(source.Width, source.Height);
            using (var graphics = Graphics.FromImage(result))
            {
                using (var attributes = new ImageAttributes())
                {
                    attributes.SetColorMatrix(new ColorMatrix(new[]
                    {
                            new[]
                            {
                                (float)color.R / byte.MaxValue,
                                0,
                                0,
                                0,
                                0
                            },
                            new[]
                            {
                                0,
                                (float)color.G / byte.MaxValue,
                                0,
                                0,
                                0
                            },
                            new[]
                            {
                                0,
                                0,
                                (float)color.B / byte.MaxValue,
                                0,
                                0
                            },
                            new float[]
                            {
                                0,
                                0,
                                0,
                                1,
                                0
                            },
                            new float[]
                            {
                                0,
                                0,
                                0,
                                0,
                                1
                            }
                        }));
                    graphics.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
                }
            }
            return result;
        }

        public static void Main(string[] args)
        {
            var executable = Assembly.GetExecutingAssembly();

            // Create a new package to clone to
            var package = Package.NewPackage(0);

            // Get the XML
            var xmlDocument = new XmlDocument();
            if (args.Length == 0)
            {
                var templateXmlPath = AppDomain.CurrentDomain.BaseDirectory + "Template.xml";
                if (!File.Exists(templateXmlPath))
                {
                    File.WriteAllText(templateXmlPath, new StreamReader(executable.GetManifestResourceStream("Template.xml") ?? throw new InvalidOperationException()).ReadToEnd());
                    return;
                }
                xmlDocument.Load(templateXmlPath);
            }
            else
            {
                xmlDocument.Load(args[0]);
            }

            Console.Write("Specify a unique suffix for the batch of favorites entries (leave blank for a random number suffix): ");

            // Get a unique name for the assembly and _XML resource
            var identifier = Console.ReadLine();
            var assemblyName = "MoreFavorites_" + (string.IsNullOrEmpty(identifier) ? FNV32.GetHash(Guid.NewGuid().ToString()).ToString() : identifier);

            // Get the assembly
            var assembly = AssemblyDefinition.ReadAssembly(executable.GetManifestResourceStream("MoreFavorites_Base.dll"));

            // Get the icons for the white color
            var largeColorIMAG = new Bitmap(executable.GetManifestResourceStream("cas_favorites_color_i_white_r2.png") ?? throw new InvalidOperationException());
            var largeFoodIMAG = new Bitmap(executable.GetManifestResourceStream("cas_favorites_food_i_hamburger_r2.png") ?? throw new InvalidOperationException());
            var largeMusicIMAG = new Bitmap(executable.GetManifestResourceStream("cas_favorites_music_i_electronica_r2.png") ?? throw new InvalidOperationException());
            var smallColorIMAG = new Bitmap(executable.GetManifestResourceStream("cas_favorites_color_i_white_s_r2.png") ?? throw new InvalidOperationException());
            var smallFoodIMAG = new Bitmap(executable.GetManifestResourceStream("cas_favorites_food_i_hamburger_s_r2.png") ?? throw new InvalidOperationException());
            var smallMusicIMAG = new Bitmap(executable.GetManifestResourceStream("cas_favorites_music_i_electronica_s_r2.png") ?? throw new InvalidOperationException());

            // Copy the elements from the XML to put into the new package
            var rootNode = xmlDocument.SelectSingleNode("Favorites") ?? xmlDocument.SelectSingleNode("Favourites");
            var favoriteColorElements = new List<XmlElement>();
            var favoriteFoodElements = new List<XmlElement>();
            var favoriteMusicElements = new List<XmlElement>();
            var unfavoriteElements = new List<XmlElement>();
            if (rootNode?.ChildNodes != null)
            {
                foreach (XmlNode node in rootNode.ChildNodes)
                {
                    if (!(node is XmlElement element))
                    {
                        continue;
                    }

                    if ((element.Name == "FavoriteColor" || element.Name == "FavouriteColour") && !favoriteColorElements.Exists(x => element.GetAttribute("Name") == x.GetAttribute("Name")))
                    {
                        favoriteColorElements.Add(element);
                    }

                    if ((element.Name == "FavoriteFood" || element.Name == "FavouriteFood") && !favoriteFoodElements.Exists(x => element.GetAttribute("Recipe_Key") == x.GetAttribute("Recipe_Key")))
                    {
                        favoriteFoodElements.Add(element);
                    }

                    if ((element.Name == "FavoriteMusic" || element.Name == "FavouriteMusic") && !favoriteMusicElements.Exists(x => element.GetAttribute("Station_Name") == x.GetAttribute("Station_Name")))
                    {
                        favoriteMusicElements.Add(element);
                    }

                    var favoriteType = element.GetAttribute("Favorite_Type");
                    if ((element.Name == "Unfavorite" || element.Name == "Unfavourite") && !unfavoriteElements.Exists(x =>
                        {
                            var foundFavoriteType = x.GetAttribute("Favorite_Type");
                            return element.GetAttribute("Name") == x.GetAttribute("Name") && (string.IsNullOrEmpty(favoriteType) ? element.GetAttribute("Favourite_Type") : favoriteType) == (string.IsNullOrEmpty(foundFavoriteType) ? x.GetAttribute("Favourite_Type") : foundFavoriteType);
                        }))
                    {
                        unfavoriteElements.Add(element);
                    }
                }
            }

            favoriteColorElements.RemoveAt(0);
            favoriteFoodElements.RemoveAt(0);
            favoriteMusicElements.RemoveAt(0);
            unfavoriteElements.RemoveAt(0);
            rootNode?.RemoveAll();
            foreach (var favoriteColorElement in favoriteColorElements)
            {
                rootNode?.AppendChild(favoriteColorElement);
            }
            foreach (var favoriteFoodElement in favoriteFoodElements)
            {
                rootNode?.AppendChild(favoriteFoodElement);
            }
            foreach (var favoriteMusicElement in favoriteMusicElements)
            {
                rootNode?.AppendChild(favoriteMusicElement);
            }
            foreach (var unfavoriteElement in unfavoriteElements)
            {
                rootNode?.AppendChild(unfavoriteElement);
            }

            // Rename the assembly for the new package
            assembly.Name.Name = assemblyName;
            assembly.MainModule.Name = assemblyName + ".dll";

            var assemblyStream = new MemoryStream();
            var xmlStream = new MemoryStream();

            // Save the assembly with the new name
            assembly.Write(assemblyStream);

            // Save the new XML to a stream
            xmlDocument.Save(xmlStream);

            // Add the resources
            var s3saKeyInstance = FNV64.GetHash(assemblyName);
            var nameMapResource = new NameMapResource.NameMapResource(0, null);
            var stblResources = new StblResource.StblResource[Enum.GetValues(typeof(Locales)).Length];
            for (var i = 0; i < stblResources.Length && (favoriteColorElements.Count > 0 || favoriteFoodElements.Exists(x => bool.TryParse(x.GetAttribute("IsGroup"), out var isGroup) && isGroup) || favoriteMusicElements.Exists(x => bool.TryParse(x.GetAttribute("IsGroup"), out var isGroup) && isGroup)); i++)
            {
                var stblKeyInstance = ulong.Parse(i.ToString("X2") + s3saKeyInstance.ToString("X16").Substring(2), NumberStyles.HexNumber);
                var stblName = "Strings_" + (Locales)i + "_0x" + stblKeyInstance.ToString("X16");
                stblResources[i] = new StblResource.StblResource(0, null);
                foreach (var favoriteColorElement in favoriteColorElements)
                {
                    var colorName = favoriteColorElement.GetAttribute("Name");
                    var attribute = favoriteColorElement.GetAttribute("Drink_Display_Name");
                    stblResources[i].Add(FNV64.GetHash("Gameplay/Objects/Appliances/FutureBar:" + colorName), string.IsNullOrEmpty(attribute) ? "Gameplay/Objects/Appliances/FutureBar:" + colorName : attribute);
                    attribute = favoriteColorElement.GetAttribute("Display_Name");
                    stblResources[i].Add(FNV64.GetHash("Gameplay/Objects/Plumbing/SonicShower:" + colorName), string.IsNullOrEmpty(attribute) ? "Gameplay/Objects/Plumbing/SonicShower:" + colorName : attribute);
                    attribute = favoriteColorElement.GetAttribute("Display_Name");
                    stblResources[i].Add(FNV64.GetHash("Ui/Caption/CAS/Favorites/Color:" + colorName), string.IsNullOrEmpty(attribute) ? "Ui/Caption/CAS/Favorites/Color:" + colorName : attribute);
                }
                foreach (var favoriteFoodElement in favoriteFoodElements)
                {
                    var recipeKey = favoriteFoodElement.GetAttribute("Recipe_Key");
                    if (bool.TryParse(favoriteFoodElement.GetAttribute("IsGroup"), out var isGroup) && isGroup)
                    {
                        var groupDisplayName = favoriteFoodElement.GetAttribute("Group_Display_Name");
                        stblResources[i].Add(FNV64.GetHash("Gameplay/Excel/RecipeMasterList/Data:" + recipeKey), string.IsNullOrEmpty(groupDisplayName) ? "Gameplay/Excel/RecipeMasterList/Data:" + recipeKey : groupDisplayName);
                    }
                }
                foreach (var favoriteMusicElement in favoriteMusicElements)
                {
                    var stationName = favoriteMusicElement.GetAttribute("Station_Name");
                    if (bool.TryParse(favoriteMusicElement.GetAttribute("IsGroup"), out var isGroup) && isGroup)
                    {
                        var groupDisplayName = favoriteMusicElement.GetAttribute("Group_Display_Name");
                        stblResources[i].Add(FNV64.GetHash("Gameplay/Excel/Stereo/Stations:" + stationName), string.IsNullOrEmpty(groupDisplayName) ? "Gameplay/Excel/Stereo/Stations:" + stationName : groupDisplayName);
                    }
                }
                package.AddResource(new ResourceKey(0x220557DA, 0, stblKeyInstance), stblResources[i].Stream, true);
                nameMapResource.Add(stblKeyInstance, stblName);
            }
            foreach (var favoriteColorElement in favoriteColorElements)
            {
                var hex = favoriteColorElement.GetAttribute("Hex");
                var imageKeyInstanceBase = "cas_favorites_color_i_" + favoriteColorElement.GetAttribute("Name");
                var largeIMAGKeyInstance = FNV64.GetHash(imageKeyInstanceBase + "_r2");
                var smallIMAGKeyInstance = FNV64.GetHash(imageKeyInstanceBase + "_s_r2");
                int startIndex;
                if (string.IsNullOrEmpty(hex) || !uint.TryParse("FFFFFFFF".Remove(0, hex.Length - (startIndex = hex.StartsWith("#") ? 1 : hex.StartsWith("0x") ? 2 : 0)) + hex.Substring(startIndex), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var argb))
                {
                    continue;
                }
                var largeIMAGStream = new MemoryStream();
                var smallIMAGStream = new MemoryStream();
                Colorize(largeColorIMAG, Color.FromArgb((int)(argb | 0xFF000000))).Save(largeIMAGStream, ImageFormat.Png);
                Colorize(smallColorIMAG, Color.FromArgb((int)(argb | 0xFF000000))).Save(smallIMAGStream, ImageFormat.Png);
                package.AddResource(new ResourceKey(0x2F7D0004, 0, largeIMAGKeyInstance), largeIMAGStream, true);
                package.AddResource(new ResourceKey(0x2F7D0004, 0, smallIMAGKeyInstance), smallIMAGStream, true);
                if (!nameMapResource.ContainsKey(largeIMAGKeyInstance))
                {
                    nameMapResource.Add(largeIMAGKeyInstance, imageKeyInstanceBase + "_r2");
                }
                if (!nameMapResource.ContainsKey(smallIMAGKeyInstance))
                {
                    nameMapResource.Add(smallIMAGKeyInstance, imageKeyInstanceBase + "_s_r2");
                }
            }
            foreach (var favoriteFoodElement in favoriteFoodElements)
            {
                var name = favoriteFoodElement.GetAttribute("Recipe_Key");
                var attribute = favoriteFoodElement.GetAttribute("Icon_Key");
                var largeIMAGKey = string.IsNullOrEmpty(attribute) ? "cas_favs_food_i_" + name : attribute;
                attribute = favoriteFoodElement.GetAttribute("Small_Icon_Key");
                var smallIMAGKey = string.IsNullOrEmpty(attribute) ? "cas_favs_food_i_" + name + "_s" : attribute;
                var largeIMAGKeyInstance = FNV64.GetHash(largeIMAGKey);
                var smallIMAGKeyInstance = FNV64.GetHash(smallIMAGKey);
                var largeIMAGStream = new MemoryStream();
                var smallIMAGStream = new MemoryStream();
                largeFoodIMAG.Save(largeIMAGStream, ImageFormat.Png);
                smallFoodIMAG.Save(smallIMAGStream, ImageFormat.Png);
                package.AddResource(new ResourceKey(0x2F7D0004, 0, largeIMAGKeyInstance), largeIMAGStream, true);
                package.AddResource(new ResourceKey(0x2F7D0004, 0, smallIMAGKeyInstance), smallIMAGStream, true);
                if (!nameMapResource.ContainsKey(largeIMAGKeyInstance))
                {
                    nameMapResource.Add(largeIMAGKeyInstance, largeIMAGKey);
                }
                if (!nameMapResource.ContainsKey(smallIMAGKeyInstance))
                {
                    nameMapResource.Add(smallIMAGKeyInstance, smallIMAGKey);
                }
            }
            foreach (var favoriteMusicElement in favoriteMusicElements)
            {
                var name = favoriteMusicElement.GetAttribute("Station_Name");
                var attribute = favoriteMusicElement.GetAttribute("Icon_Key");
                var largeIMAGKey = string.IsNullOrEmpty(attribute) ? "cas_favs_music_i_" + name : attribute;
                attribute = favoriteMusicElement.GetAttribute("Small_Icon_Key");
                var smallIMAGKey = string.IsNullOrEmpty(attribute) ? "cas_favs_music_i_" + name + "_s" : attribute;
                var largeIMAGKeyInstance = FNV64.GetHash(largeIMAGKey);
                var smallIMAGKeyInstance = FNV64.GetHash(smallIMAGKey);
                var largeIMAGStream = new MemoryStream();
                var smallIMAGStream = new MemoryStream();
                largeMusicIMAG.Save(largeIMAGStream, ImageFormat.Png);
                smallMusicIMAG.Save(smallIMAGStream, ImageFormat.Png);
                package.AddResource(new ResourceKey(0x2F7D0004, 0, largeIMAGKeyInstance), largeIMAGStream, true);
                package.AddResource(new ResourceKey(0x2F7D0004, 0, smallIMAGKeyInstance), smallIMAGStream, true);
                if (!nameMapResource.ContainsKey(largeIMAGKeyInstance))
                {
                    nameMapResource.Add(largeIMAGKeyInstance, largeIMAGKey);
                }
                if (!nameMapResource.ContainsKey(smallIMAGKeyInstance))
                {
                    nameMapResource.Add(smallIMAGKeyInstance, smallIMAGKey);
                }
            }
            nameMapResource.Add(s3saKeyInstance, assemblyName);
            package.AddResource(new ResourceKey(0x166038C, 0, 0), nameMapResource.Stream, true);
            package.AddResource(new ResourceKey(0x333406C, 0, s3saKeyInstance), xmlStream, true);
            package.AddResource(new ResourceKey(0x73FAA07, 0, s3saKeyInstance), new ScriptResource.ScriptResource(0, null)
                {
                    Assembly = new BinaryReader(assemblyStream)
                }.Stream, true);

            // Save the new package with the new name
            package.SaveAs(AppDomain.CurrentDomain.BaseDirectory + assemblyName + ".package");
        }
    }
}
