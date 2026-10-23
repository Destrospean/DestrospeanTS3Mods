using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Xml;
using Mono.Cecil;
using s3pi.Interfaces;

namespace Destrospean.MoreFavorites.Generator
{
    class Program
    {
        enum Locales
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
                    attributes.SetColorMatrix(new ColorMatrix(new float[][]
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
            var executable = System.Reflection.Assembly.GetExecutingAssembly();

            // Create a new package to clone to
            var package = s3pi.Package.Package.NewPackage(0);

            // Get the XML
            var xmlDocument = new XmlDocument();
            if (args.Length == 0)
            {
                var templateXmlPath = AppDomain.CurrentDomain.BaseDirectory + "Template.xml";
                if (!File.Exists(templateXmlPath))
                {
                    File.WriteAllText(templateXmlPath, new StreamReader(executable.GetManifestResourceStream("Template.xml")).ReadToEnd());
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
            string identifier = Console.ReadLine(),
            assemblyName = "MoreFavorites_" + (string.IsNullOrEmpty(identifier) ? FNV32.GetHash(Guid.NewGuid().ToString()).ToString() : identifier);

            // Get the assembly
            var assembly = AssemblyDefinition.ReadAssembly(executable.GetManifestResourceStream("MoreFavorites_Base.dll"));

            // Get the icons for the white color
            Bitmap largeColorIMAG = new Bitmap(executable.GetManifestResourceStream("cas_favorites_color_i_white_r2.png")),
            largeFoodIMAG = new Bitmap(executable.GetManifestResourceStream("cas_favorites_food_i_hamburger_r2.png")),
            largeMusicIMAG = new Bitmap(executable.GetManifestResourceStream("cas_favorites_music_i_electronica_r2.png")),
            smallColorIMAG = new Bitmap(executable.GetManifestResourceStream("cas_favorites_color_i_white_s_r2.png")),
            smallFoodIMAG = new Bitmap(executable.GetManifestResourceStream("cas_favorites_food_i_hamburger_s_r2.png")),
            smallMusicIMAG = new Bitmap(executable.GetManifestResourceStream("cas_favorites_music_i_electronica_s_r2.png"));

            // Copy the elements from the XML to put into the new package
            var rootNode = xmlDocument.SelectSingleNode("Favorites") ?? xmlDocument.SelectSingleNode("Favourites");
            List<XmlElement> favoriteColorElements = new List<XmlElement>(),
            favoriteFoodElements = new List<XmlElement>(),
            favoriteMusicElements = new List<XmlElement>(),
            unfavoriteElements = new List<XmlElement>();
            foreach (XmlNode node in rootNode.ChildNodes)
            {
                if ((node.Name == "FavoriteColor" || node.Name == "FavouriteColour") && !favoriteColorElements.Exists(x => node.Attributes["Name"].Value == x.GetAttribute("Name")))
                {
                    favoriteColorElements.Add((XmlElement)node);
                }
                if ((node.Name == "FavoriteFood" || node.Name == "FavouriteFood") && !favoriteFoodElements.Exists(x => node.Attributes["Recipe_Key"].Value == x.GetAttribute("Recipe_Key")))
                {
                    favoriteFoodElements.Add((XmlElement)node);
                }
                if ((node.Name == "FavoriteMusic" || node.Name == "FavouriteMusic") && !favoriteMusicElements.Exists(x => node.Attributes["Station_Name"].Value == x.GetAttribute("Station_Name")))
                {
                    favoriteMusicElements.Add((XmlElement)node);
                }
                if ((node.Name == "Unfavorite" || node.Name == "Unfavourite") && !unfavoriteElements.Exists(x => node.Attributes["Name"].Value == x.GetAttribute("Name") && (((XmlElement)node).GetAttribute("Favorite_Type") ?? ((XmlElement)node).GetAttribute("Favourite_Type")) == (x.GetAttribute("Favorite_Type") ?? x.GetAttribute("Favourite_Type"))))
                {
                    unfavoriteElements.Add((XmlElement)node);
                }
            }
            favoriteColorElements.RemoveAt(0);
            favoriteFoodElements.RemoveAt(0);
            favoriteMusicElements.RemoveAt(0);
            unfavoriteElements.RemoveAt(0);
            rootNode.RemoveAll();
            foreach (var favoriteColorElement in favoriteColorElements)
            {
                rootNode.AppendChild(favoriteColorElement);
            }
            foreach (var favoriteFoodElement in favoriteFoodElements)
            {
                rootNode.AppendChild(favoriteFoodElement);
            }
            foreach (var favoriteMusicElement in favoriteMusicElements)
            {
                rootNode.AppendChild(favoriteMusicElement);
            }
            foreach (var unfavoriteElement in unfavoriteElements)
            {
                rootNode.AppendChild(unfavoriteElement);
            }

            // Rename the assembly for the new package
            assembly.Name.Name = assemblyName;
            assembly.MainModule.Name = assemblyName + ".dll";

            Stream assemblyStream = new MemoryStream(),
            xmlStream = new MemoryStream();

            // Save the assembly with the new name
            assembly.Write(assemblyStream);

            // Save the new XML to a stream
            xmlDocument.Save(xmlStream);

            // Add the resources
            var s3saKeyInstance = FNV64.GetHash(assemblyName);
            var nameMapResource = new NameMapResource.NameMapResource(0, null);
            var stblResources = new StblResource.StblResource[Enum.GetValues(typeof(Locales)).Length];
            for (var i = 0; i < stblResources.Length && favoriteColorElements.Count > 0; i++)
            {
                var stblKeyInstance = ulong.Parse(i.ToString("X2") + s3saKeyInstance.ToString("X16").Substring(2), System.Globalization.NumberStyles.HexNumber);
                var stblName = "Strings_" + (Locales)i + "_0x" + stblKeyInstance.ToString("X16");
                stblResources[i] = new StblResource.StblResource(0, null);
                foreach (var favoriteColorElement in favoriteColorElements)
                {
                    var colorName = favoriteColorElement.GetAttribute("Name");
                    stblResources[i].Add(FNV64.GetHash("Gameplay/Objects/Appliances/FutureBar:" + colorName), favoriteColorElement.GetAttribute("Drink_Display_Name") ?? "Gameplay/Objects/Appliances/FutureBar:" + colorName);
                    stblResources[i].Add(FNV64.GetHash("Gameplay/Objects/Plumbing/SonicShower:" + colorName), favoriteColorElement.GetAttribute("Display_Name") ?? "Gameplay/Objects/Plumbing/SonicShower:" + colorName);
                    stblResources[i].Add(FNV64.GetHash("Ui/Caption/CAS/Favorites/Color:" + colorName), favoriteColorElement.GetAttribute("Display_Name") ?? "Ui/Caption/CAS/Favorites/Color:" + colorName);
                }
                package.AddResource(new ResourceKey(0x220557DA, 0, stblKeyInstance), stblResources[i].Stream, true);
                nameMapResource.Add(stblKeyInstance, stblName);
            }
            foreach (var favoriteColorElement in favoriteColorElements)
            {
                string hex = favoriteColorElement.GetAttribute("Hex"),
                imageKeyInstanceBase = "cas_favorites_color_i_" + favoriteColorElement.GetAttribute("Name");
                ulong largeIMAGKeyInstance = FNV64.GetHash(imageKeyInstanceBase + "_r2"),
                smallIMAGKeyInstance = FNV64.GetHash(imageKeyInstanceBase + "_s_r2");
                uint argb;
                int startIndex;
                if (hex == null || !uint.TryParse(("FFFFFFFF".Remove(0, hex.Length - (startIndex = hex.StartsWith("#") ? 1 : hex.StartsWith("0x") ? 2 : 0))) + hex.Substring(startIndex), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out argb))
                {
                    continue;
                }
                Stream largeIMAGStream = new MemoryStream(),
                smallIMAGStream = new MemoryStream();
                Colorize(largeColorIMAG, Color.FromArgb((int)(argb | 0xFF000000))).Save(largeIMAGStream, ImageFormat.Png);
                Colorize(smallColorIMAG, Color.FromArgb((int)(argb | 0xFF000000))).Save(smallIMAGStream, ImageFormat.Png);
                package.AddResource(new ResourceKey(0x2F7D0004, 0, largeIMAGKeyInstance), largeIMAGStream, true);
                package.AddResource(new ResourceKey(0x2F7D0004, 0, smallIMAGKeyInstance), smallIMAGStream, true);
                nameMapResource.Add(largeIMAGKeyInstance, imageKeyInstanceBase + "_r2");
                nameMapResource.Add(smallIMAGKeyInstance, imageKeyInstanceBase + "_s_r2");
            }
            foreach (var favoriteFoodElement in favoriteFoodElements)
            {
                string name = favoriteFoodElement.GetAttribute("Recipe_Key"),
                largeIMAGKey = string.IsNullOrEmpty(favoriteFoodElement.GetAttribute("Icon_Key")) ? "cas_favs_food_i_" + name : favoriteFoodElement.GetAttribute("Icon_Key"),
                smallIMAGKey = string.IsNullOrEmpty(favoriteFoodElement.GetAttribute("Small_Icon_Key")) ? "cas_favs_food_i_" + name + "_s" : favoriteFoodElement.GetAttribute("Small_Icon_Key");
                ulong largeIMAGKeyInstance = FNV64.GetHash(largeIMAGKey),
                smallIMAGKeyInstance = FNV64.GetHash(smallIMAGKey);
                Stream largeIMAGStream = new MemoryStream(),
                smallIMAGStream = new MemoryStream();
                largeFoodIMAG.Save(largeIMAGStream, ImageFormat.Png);
                smallFoodIMAG.Save(smallIMAGStream, ImageFormat.Png);
                package.AddResource(new ResourceKey(0x2F7D0004, 0, largeIMAGKeyInstance), largeIMAGStream, true);
                package.AddResource(new ResourceKey(0x2F7D0004, 0, smallIMAGKeyInstance), smallIMAGStream, true);
                nameMapResource.Add(largeIMAGKeyInstance, largeIMAGKey);
                nameMapResource.Add(smallIMAGKeyInstance, smallIMAGKey);
            }
            foreach (var favoriteMusicElement in favoriteMusicElements)
            {
                string name = favoriteMusicElement.GetAttribute("Station_Name"),
                largeIMAGKey = string.IsNullOrEmpty(favoriteMusicElement.GetAttribute("Icon_Key")) ? "cas_favs_music_i_" + name : favoriteMusicElement.GetAttribute("Icon_Key"),
                smallIMAGKey = string.IsNullOrEmpty(favoriteMusicElement.GetAttribute("Small_Icon_Key")) ? "cas_favs_music_i_" + name + "_s" : favoriteMusicElement.GetAttribute("Small_Icon_Key");
                ulong largeIMAGKeyInstance = FNV64.GetHash(largeIMAGKey),
                smallIMAGKeyInstance = FNV64.GetHash(smallIMAGKey);
                Stream largeIMAGStream = new MemoryStream(),
                smallIMAGStream = new MemoryStream();
                largeMusicIMAG.Save(largeIMAGStream, ImageFormat.Png);
                smallMusicIMAG.Save(smallIMAGStream, ImageFormat.Png);
                package.AddResource(new ResourceKey(0x2F7D0004, 0, largeIMAGKeyInstance), largeIMAGStream, true);
                package.AddResource(new ResourceKey(0x2F7D0004, 0, smallIMAGKeyInstance), smallIMAGStream, true);
                nameMapResource.Add(largeIMAGKeyInstance, largeIMAGKey);
                nameMapResource.Add(smallIMAGKeyInstance, smallIMAGKey);
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
