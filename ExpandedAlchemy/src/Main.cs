using Sims3.Gameplay;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Objects.Alchemy;
using Sims3.Gameplay.Objects.Fishing;
using Sims3.Gameplay.Objects.FoodObjects;
using Sims3.Gameplay.Objects.Insect;
using Sims3.Gameplay.Objects.Register;
using Sims3.Gameplay.Objects.Spawners;
using Sims3.Gameplay.Objects.Miscellaneous.Shopping;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Utilities;
using Sims3.Gameplay.Destrospean.ExpandedAlchemy;
using Sims3.SimIFace;
using Sims3.UI;
using System;
using System.Collections.Generic;
using System.Reflection;
using Destrospean.Utils;

namespace Destrospean.ExpandedAlchemy
{
    public class Main
    {
        public enum DebugInfoType
        {
            Error,
            Warning
        }

        public class DebugInfo
        {
            public Assembly Assembly;

            public Exception Exception;

            public XmlDbTable Table;

            public DebugInfoType Type;

            public DebugInfo(DebugInfoType type, Assembly assembly, XmlDbTable table, Exception exception)
            {
                Type = type;
                Assembly = assembly;
                Table = table;
                Exception = exception;
            }
        }

        [Tunable]
        protected static bool kDebug;

        public static List<DebugInfo> ErrorsAndWarnings;

        static Main()
        {
            World.sOnWorldLoadStartedEventHandler += (sender, e) =>
                {
                    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        if (assembly.GetType("Destrospean.ExpandedAlchemy.Data") == null)
                        {
                            continue;
                        }
                        XmlDbData data = XmlDbData.ReadData(assembly.GetName().Name);
                        if (data == null)
                        {
                            continue;
                        }
                        XmlDbTable table = null;
                        try
                        {
                            // Load fish data
                            if (data.Tables.TryGetValue("Fish", out table))
                            {
                                //Fish.ParseFish(table);
                                foreach (XmlDbRow row in table.Rows)
                                {
                                    if (string.IsNullOrEmpty(row["Hex"]))
                                    {
                                        continue;
                                    }
                                    FishType fishType;
                                    if (!ParserFunctions.TryParseEnum<FishType>(row["Hex"], out fishType, FishType.None))
                                    {
                                        CommonUtils.AddEnumValue<FishType>(row["Hex"], fishType = (FishType)ResourceUtils.HashString64(row["Hex"]));
                                    }
                                    ProductVersion version;
                                    if (row.TryGetEnum<ProductVersion>("CodeVersion", out version, ProductVersion.BaseGame) && GameUtils.IsInstalled(version))
                                    {
                                        FishData fishData = new FishData();
                                        fishData.Version = version;
                                        fishData.StringKeyName = "Gameplay/Excel/Fishing/Fish:" + row["Name"];
                                        fishData.StringKeyDescription = "Gameplay/Excel/Fishing/Fish:" + row["Description"];
                                        fishData.Rarity = "Gameplay/Excel/RockGemMetal/Rarity:" + row["Rarity"];
                                        fishData.MedatorName = row["MedatorName"];
                                        fishData.LocationFound = (WaterTypes)ParserFunctions.ParseInt(row["WaterTypes"], 0);
                                        fishData.PrefferedBait = row["Bait"];
                                        fishData.Level = ParserFunctions.ParseInt(row["Level"], 1);
                                        fishData.MinWeight = ParserFunctions.ParseFloat(row["MinWeight"], 0.1f);
                                        fishData.MaxWeight = ParserFunctions.ParseFloat(row["MaxWeight"], 10f);
                                        fishData.OverMax = ParserFunctions.ParseBool(row["OverMax"]);
                                        fishData.MinPrice = ParserFunctions.ParseInt(row["MinPrice"], 100);
                                        float deltaWeight = fishData.MaxWeight - fishData.MinWeight;
                                        fishData.PriceWeightSlope = (float)(ParserFunctions.ParseInt(row["MaxPrice"], 1000) - fishData.MinPrice) / deltaWeight;
                                        fishData.SkillPoints = ParserFunctions.ParseFloat(row["SkillPoints"], 30f);
                                        fishData.WeightSkillSlope = deltaWeight / Fish.kLevelsAboveForMaxWeight;
                                        ParserFunctions.TryParseEnum<EffectSize>(row["EffectSize"], out fishData.EffectSize, EffectSize.Small);
                                        int price = -1;
                                        if (ParserFunctions.ParseBool(row["CanBuyFromStore"]))
                                        {
                                            price = fishData.MinPrice;
                                        }
                                        if (fishType == FishType.Goldfish)
                                        {
                                            Fish.kCostOfNewGoldfish = fishData.MinPrice;
                                        }
                                        fishData.FishBowlMaterialState = row["FishBowlMaterialState"];
                                        string text = row["Ingredient_Key"];
                                        if (!string.IsNullOrEmpty(text))
                                        {
                                            WorldName requiredWorldName;
                                            ParserFunctions.TryParseEnum<WorldName>(row["RequiredWorld"], out requiredWorldName, WorldName.Undefined);
                                            fishData.IngredientData = new IngredientData(text, fishData.StringKeyName, row["Model_Name"], ParserFunctions.ParseFloat(row["Fertilizer_Value"], float.MinValue), ParserFunctions.ParseInt(row["Fertilizer_Days"], -1), price, requiredWorldName, version, version);
                                        }
                                        if (GameUtils.IsInstalled(ProductVersion.EP5))
                                        {
                                            fishData.ParseCatHuntingDataRow(row);
                                        }
                                        fishData.EdibleRawAnimation = row["EdibleRawAnimation"];
                                        Fish.sFishData.Add(fishType, fishData);
                                    }
                                }
                                Fishing.NumberOfFishTypes = (double)(Fish.sFishData.Count - 1);
                            }
                            if (data.Tables.TryGetValue("SpawnerFish", out table))
                            {
                                FishingSpot.ParseFishingSpots(table);
                            }
                            if (data.Tables.TryGetValue("GlobalSpawnerFish", out table))
                            {
                                FishingSpot.ParseGlobalFishingSpots(table);
                            }
                            if (data.Tables.TryGetValue("BoxItemsFish", out table))
                            {
                                FishHere.ParseBoxData(table);
                            }

                            // Load rock, gem, metal data
                            if (data.Tables.TryGetValue("Metals", out table))
                            {
                                //Metal.ParseData(xmlDbTable);
                                RockGemMetalBase.sMetals = RockGemMetalBase.sMetals ?? new List<RockGemMetal>();
                                foreach (XmlDbRow row in table.Rows)
                                {
                                    if (string.IsNullOrEmpty(row["Hex"]))
                                    {
                                        continue;
                                    }
                                    ProductVersion version;
                                    if (!ParserFunctions.TryParseEnum<ProductVersion>(row["CodeVersion"], out version, ProductVersion.Undefined) || GameUtils.IsInstalled(version))
                                    {
                                        RockGemMetal metalType;
                                        if (!ParserFunctions.TryParseEnum<RockGemMetal>(row["Hex"], out metalType, RockGemMetal.Iron))
                                        {
                                            CommonUtils.AddEnumValue<RockGemMetal>(row["Hex"], metalType = (RockGemMetal)ResourceUtils.HashString64(row["Hex"]));
                                        }
                                        RockGemMetalBase.sMetals.Add(metalType);
                                        MetalData metalData = new MetalData();
                                        metalData.ParseRow(row, "Metals", CollectedRgmType.Metal);
                                        metalData.RockMaterialName = row["RockMaterialName"];
                                        metalData.BarMaterialName = row["BarMaterialName"];
                                        RockGemMetalBase.sData.Add(metalType, metalData);
                                    }
                                }
                            }
                            if (data.Tables.TryGetValue("Gems", out table))
                            {
                                //Gem.ParseData(xmlDbTable);
                                RockGemMetalBase.sGems = RockGemMetalBase.sGems ?? new List<RockGemMetal>();
                                foreach (XmlDbRow row in table.Rows)
                                {
                                    if (string.IsNullOrEmpty(row["Hex"]))
                                    {
                                        continue;
                                    }
                                    ProductVersion version;
                                    if (!ParserFunctions.TryParseEnum(row["CodeVersion"], out version, ProductVersion.Undefined) || GameUtils.IsInstalled(version))
                                    {
                                        RockGemMetal gemType;
                                        if (!ParserFunctions.TryParseEnum<RockGemMetal>(row["Hex"], out gemType, RockGemMetal.Aqua))
                                        {
                                            CommonUtils.AddEnumValue<RockGemMetal>(row["Hex"], gemType = (RockGemMetal)ResourceUtils.HashString64(row["Hex"]));
                                        }
                                        RockGemMetalBase.sGems.Add(gemType);
                                        GemData gemData = new GemData();
                                        gemData.ParseRow(row, "Gems", CollectedRgmType.Gem);
                                        gemData.RockMaterialName = row["RockMaterialName"];
                                        gemData.GemMaterialName = row["GemMaterialName"];
                                        RockGemMetalBase.sData.Add(gemType, gemData);
                                    }
                                }
                            }
                            if (data.Tables.TryGetValue("GemCuts", out table))
                            {
                                //Gem.ParseCuts(table);
                                foreach (XmlDbRow row in table.Rows)
                                {
                                    ProductVersion version;
                                    if (!ParserFunctions.TryParseEnum<ProductVersion>(row["CodeVersion"], out version, ProductVersion.Undefined) || GameUtils.IsInstalled(version))
                                    {
                                        RockGemMetalBase.CutData cutData = new RockGemMetalBase.CutData();
                                        cutData.Name = "Gameplay/Excel/RockGemMetal/GemCuts:" + row["Name"];
                                        cutData.CutName = row["cutString"];
                                        cutData.Multiplier = ParserFunctions.ParseFloat(row["Multiplier"], 1.5f);
                                        cutData.NumberOfCuts = ParserFunctions.ParseInt(row["Cuts"], 5);
                                        cutData.CostToCut = ParserFunctions.ParseInt(row["Cost"], 100);
                                        RockGemMetalBase.sCutData.Add(cutData);
                                        if (Gem.sToughestCut == null || Gem.sToughestCut.NumberOfCuts < cutData.NumberOfCuts)
                                        {
                                            Gem.sToughestCut = cutData;
                                        }
                                    }
                                }
                                RockGemMetalBase.sCutData.Remove(Gem.sToughestCut);
                            }
                            if (data.Tables.TryGetValue("Rocks", out table))
                            {
                                //Rock.ParseData(xmlDbTable);
                                foreach (XmlDbRow row in table.Rows)
                                {
                                    if (string.IsNullOrEmpty(row["Hex"]))
                                    {
                                        continue;
                                    }
                                    RockGemMetal rockType;
                                    if (!ParserFunctions.TryParseEnum<RockGemMetal>(row["Hex"], out rockType, RockGemMetal.SpaceRockSmall))
                                    {
                                        CommonUtils.AddEnumValue<RockGemMetal>(row["Hex"], rockType = (RockGemMetal)ResourceUtils.HashString64(row["Hex"]));
                                    }
                                    RockGemMetalData rockData = new RockGemMetalData();
                                    rockData.ParseRow(row, "Rocks", CollectedRgmType.Rock);
                                    RockGemMetalBase.sData.Add(rockType, rockData);
                                }
                            }
                            if (data.Tables.TryGetValue("RockNames", out table))
                            {
                                //Rock.ParseNames(xmlDbTable);
                                Rock.RockNames = Rock.RockNames ?? new List<Rock.RockName>();
                                foreach (XmlDbRow row in table.Rows)
                                {
                                    Rock.RockName rockName = new Rock.RockName();
                                    rockName.Name = "Gameplay/Excel/RockGemMetal/RockNames:" + row["Name"];
                                    rockName.Multiplier = ParserFunctions.ParseFloat(row["ValueMultiplier"], 1f);
                                    Rock.RockNames.Add(rockName);
                                }
                                RockGemMetalData.NumberOfRocks = Rock.RockNames.Count;
                            }
                            if (data.Tables.TryGetValue("SpawnersRockGemMetal", out table))
                            {
                                //RockGemMetalSpawner.ParseSpawnerData(xmlDbTable);
                                foreach (XmlDbRow row in table.Rows)
                                {
                                    List<RockGemMetal> rockGemMetalList;
                                    bool parseSuccessful = ParserFunctions.TryParseCommaSeparatedList<RockGemMetal>(row["Spawns"], out rockGemMetalList, RockGemMetal.Iron);
                                    if (parseSuccessful)
                                    {
                                        List<int> probability;
                                        parseSuccessful = ParserFunctions.ParseCommaSeperatedInt(row["Probability"], out probability);
                                        if (parseSuccessful)
                                        {
                                            for (int i = rockGemMetalList.Count - 1; i > -1; i--)
                                            {
                                                if (!RockGemMetalBase.sData.ContainsKey(rockGemMetalList[i]))
                                                {
                                                    rockGemMetalList.RemoveAt(i);
                                                    probability.RemoveAt(i);
                                                }
                                            }
                                            Type spawnerClassType;
                                            if (CommonUtils.TryGetType(row["SpawnerClassName"], out spawnerClassType))
                                            {
                                                spawnerClassType.GetField("kSpawnerData", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, new RockGemMetalSpawner.RgmSpawnerData(rockGemMetalList, probability, ParserFunctions.ParseInt(row["MaxSpawnCapacity"], 2), new[]
                                                    {
                                                        ParserFunctions.ParseFloat(row["SpawnMinTime"], 0.5f),
                                                        ParserFunctions.ParseFloat(row["SpawnMaxTime"], 2f)
                                                    }, ParserFunctions.ParseFloat(row["SpawnChance"], 0.9f), ParserFunctions.ParseFloat(row["DeleteUntaken"], 2f)));
                                            }
                                        }
                                    }
                                }
                            }

                            // Load insect data
                            ParseData(data, false, "Butterflies", InsectClassification.Butterfly);
                            ParseData(data, false, "Beetles", InsectClassification.Beetle);
                            ParseData(data, false, "GlowBugs", InsectClassification.GlowBug);
                            ParseData(data, false, "Mosquitoes", InsectClassification.Mosquito);
                            ParseData(data, false, "Nanites", InsectClassification.Nanite);
                            ParseInsectSpawnerData(data, false);

                            // Load alchemy recipe data
                            if (data.Tables.TryGetValue("AlchemyRecipes", out table))
                            {
                                foreach (XmlDbRow row in table.Rows)
                                {
                                    AlchemyRecipe alchemyRecipe = new AlchemyRecipe(row);
                                    if (AlchemyRecipe.NameToRecipeMap.ContainsKey(alchemyRecipe.Key))
                                    {
                                        if (!Settings.kReplaceAlchemyRecipes)
                                        {
                                            ErrorsAndWarnings.Add(new DebugInfo(DebugInfoType.Warning, assembly, table, new Exception("An alchemy recipe with the key, \"" + alchemyRecipe.Key +  "\" already exists")));
                                            continue;
                                        }
                                        AlchemyRecipe.AlchemyRecipes.Remove(alchemyRecipe);
                                        AlchemyRecipe.NameToRecipeMap.Remove(alchemyRecipe.Key);
                                    }
                                    AlchemyRecipe.AlchemyRecipes.Add(alchemyRecipe);
                                    AlchemyRecipe.NameToRecipeMap.Add(alchemyRecipe.Key, alchemyRecipe);
                                }
                            }

                            // Load shopping register data
                            if (GameUtils.IsInstalled(ProductVersion.EP1))
                            {
                                if (data.Tables.TryGetValue("GeneralStore", out table))
                                {
                                    GeneralStoreRegister.ParseData(table);
                                }
                                if (data.Tables.TryGetValue("Relics", out table))
                                {
                                    RelicStoreRegister.ParseData(table);
                                }
                                if (data.Tables.TryGetValue("Nectar", out table))
                                {
                                    NectarRegister.ParseData(table);
                                }
                            }
                            if (GameUtils.IsInstalled(ProductVersion.EP2) && data.Tables.TryGetValue("Consignment", out table))
                            {
                                ConsignmentRegister.ParseData(table);
                            }
                            if (GameUtils.IsInstalled(ProductVersion.EP7) && data.Tables.TryGetValue("PotionShopConsignment", out table))
                            {
                                PotionShopConsignmentRegister.ParseData(table);
                            }
                            if (World.ResourceExists(GlobalFunctions.CreateProductKey("registerPetstore", ProductVersion.Store)) && data.Tables.TryGetValue("PetStore", out table))
                            {
                                PetstoreRegister.ParseData(table);
                            }
                            if (GameUtils.IsInstalled(ProductVersion.EP8))
                            {
                                if (data.Tables.TryGetValue("ConcessionsStand", out table))
                                {
                                    ConcessionsStand.SeasonalFoodData.ParseData(table);
                                }
                            }
                            if (GameUtils.IsInstalled(ProductVersion.EP10))
                            {
                                if (data.Tables.TryGetValue("ResortFoodStand", out table))
                                {
                                    ResortFoodStand.ResortFoodData.ParseData(table);
                                }
                            }
                            if (GameUtils.IsInstalled(ProductVersion.EP11) && data.Tables.TryGetValue("BotConsignmentShop", out table))
                            {
                                BotShopRegister.ParseData(table);
                            }
                        }
                        catch (Exception exception)
                        {
                            ErrorsAndWarnings.Add(new DebugInfo(DebugInfoType.Error, assembly, table, exception));
                        }
                    }
                    AlarmManager.Global.AddAlarm(5f, TimeUnit.Minutes, () =>
                        {
                            foreach (DebugInfo debugInfo in ErrorsAndWarnings)
                            {
                                if (!kDebug && debugInfo.Type == DebugInfoType.Warning)
                                {
                                    continue;
                                }
                                string infoText = "XML: " + debugInfo.Assembly.GetName().Name + "\nTable: " + debugInfo.Table.Name + "\nInfo: " + debugInfo.Exception.Message;
                                StyledNotification.Show(new StyledNotification.Format("Expanded Alchemy -- " + debugInfo.Type + ":\n" + infoText, StyledNotification.NotificationStyle.kDebugAlert));
                                uint fileHandle = 0u;
                                Simulator.CreateExportFile(ref fileHandle, "ExpandedAlchemy_" + debugInfo.Type + "_");
                                if (fileHandle != 0u)
                                {
                                    CustomXmlWriter xmlWriter = new CustomXmlWriter(fileHandle);
                                    xmlWriter.WriteToBuffer(infoText);
                                    xmlWriter.WriteEndDocument();
                                }
                            }
                        }, "Expanded Alchemy -- Show Debug Info Alarm", AlarmType.NeverPersisted, null);
                };
        }

        public static T GetGuid<T>(string guidKey, bool isStore, ref string name) where T : struct
        {
            if (string.IsNullOrEmpty(guidKey))
            {
                return default(T);
            }
            if (!isStore)
            {
                T guid;
                if (!ParserFunctions.TryParseEnum<T>(guidKey, out guid, default(T)))
                {
                    CommonUtils.AddEnumValue<T>(guidKey, guid = (T)(object)ResourceUtils.HashString64(guidKey));
                }
                return guid;
            }
            string[] nameGuidPair = guidKey.Split(new[]
                {
                    '='
                });
            if (nameGuidPair.Length == 2)
            {
                name = nameGuidPair[0].Trim();
                return (T)(object)Convert.ToUInt64(nameGuidPair[1], 16);
            }
            return default(T);
        }

        public static void ParseData(XmlDbData data, bool isStore, string nameSpreadsheet, InsectClassification insectClassification)
        {
            XmlDbTable table;
            if (!data.Tables.TryGetValue(nameSpreadsheet, out table) || table == null)
            {
                return;
            }
            foreach (XmlDbRow row in table.Rows)
            {
                ProductVersion productVersion;
                if (!ParserFunctions.TryParseEnum<ProductVersion>(row["CodeVersion"], out productVersion, ProductVersion.Undefined) || GameUtils.IsInstalled(productVersion))
                {
                    float version = 0f;
                    if (isStore)
                    {
                        version = ParserFunctions.ParseFloat(row["Version"], 1f);
                    }
                    string guidKey = row["Type"];
                    InsectType guid = GetGuid<InsectType>(guidKey, isStore, ref guidKey);
                    if (guid != InsectType.Undefined)
                    {
                        InsectData insectData = null;
                        switch (insectClassification)
                        {
                            case InsectClassification.Butterfly:
                                insectData = new ButterflyData();
                                break;
                            case InsectClassification.Beetle:
                                insectData = new BeetleData();
                                break;
                            case InsectClassification.GlowBug:
                                insectData = new GlowBugData();
                                break;
                            case InsectClassification.Mosquito:
                                insectData = new MosquitoData();
                                break;
                            case InsectClassification.Nanite:
                                insectData = new NaniteData();
                                break;
                        }
                        insectData.mVersion = version;
                        insectData.ParseRow(row, nameSpreadsheet);
                        InsectData value;
                        if (!InsectData.sData.TryGetValue(guid, out value))
                        {
                            InsectData.sData.Add(guid, insectData);
                            Insect.sInsectsTypes.AddNewEnumValue(guidKey, guid);
                            insectData.IncrementTotalNumber();
                        }
                        else if (value.mVersion < insectData.mVersion)
                        {
                            InsectData.sData[guid] = insectData;
                        }
                    }
                }
            }
        }

        public static void ParseInsectSpawnerData(XmlDbData data, bool isStore)
        {
            float version = 0f;
            XmlDbTable table;
            if (!data.Tables.TryGetValue("SpawnersInsect", out table) || table == null)
            {
                return;
            }
            foreach (XmlDbRow row in table.Rows)
            {
                List<InsectType> insectTypeList = new List<InsectType>();
                bool parseSuccessful = Insect.sInsectsTypes.TryParseSuperSetEnumCommaSeparatedList(row["Spawns"], out insectTypeList, InsectType.Undefined);
                if (parseSuccessful)
                {
                    List<int> probability;
                    parseSuccessful = ParserFunctions.ParseCommaSeperatedInt(row["Probability"], out probability);
                    if (parseSuccessful)
                    {
                        int insectTypeCount = insectTypeList.Count;
                        int probabilityCount = probability.Count;
                        for (int i = insectTypeCount - 1; i > -1; i--)
                        {
                            if (!InsectData.sData.ContainsKey(insectTypeList[i]))
                            {
                                insectTypeList.RemoveAt(i);
                                probability.RemoveAt(i);
                            }
                        }
                        insectTypeCount = insectTypeList.Count;
                        probabilityCount = probability.Count;
                        InsectSpawner.InsectProbability[] insectProbabilities = new InsectSpawner.InsectProbability[insectTypeCount];
                        for (int i = 0; i < insectTypeCount; i++)
                        {
                            float weightedProbability = 1f;
                            if (i <= probabilityCount - 1)
                            {
                                weightedProbability = (float)probability[i];
                            }
                            insectProbabilities[i] = new InsectSpawner.InsectProbability(insectTypeList[i], weightedProbability);
                        }
                        if (isStore)
                        {
                            version = ParserFunctions.ParseFloat(row["Version"], 1f);
                        }
                        InsectSpawner.InsectSpawnerData insectSpawnerData = new InsectSpawner.InsectSpawnerData(insectProbabilities, new[]
                            {
                                ParserFunctions.ParseFloat(row["TimeOfDayStart"], 0f),
                                ParserFunctions.ParseFloat(row["TimeOfDayStop"], 24f)
                            }, ParserFunctions.ParseListOfDaysToFlagEnum(ParserFunctions.ParseDayList(row["DaysOfWeek"])), ParserFunctions.ParseInt(row["MaxSpawnCapacity"], 2), new[]
                            {
                                ParserFunctions.ParseFloat(row["MinSpawnFrequency"], 0.5f),
                                ParserFunctions.ParseFloat(row["MaxSpawnFrequency"], 2f)
                            }, ParserFunctions.ParseFloat(row["SpawnChance"], 1f), ParserFunctions.ParseFloat(row["DeleteUntaken"], 2f), ParserFunctions.ParseBool(row["ButterflySpawner"]), ParserFunctions.ParseBool(row["BeetleSpawner"]), version);
                        string spawnerClassName = row["SpawnerClassName"];
                        Type spawnerClassType = null;
                        if (isStore)
                        {
                            string[] spawnerClassNameSeparated = spawnerClassName.Split(new[]
                                {
                                    '|'
                                });
                            if (spawnerClassNameSeparated.Length > 1)
                            {
                                spawnerClassName = spawnerClassNameSeparated[0] + ",Sims3StoreObjects";
                                if (!CommonUtils.TryGetType(row["SpawnerClassName"], out spawnerClassType))
                                {
                                    spawnerClassName = spawnerClassNameSeparated[0] + ',' + spawnerClassNameSeparated[1];
                                }
                            }
                        }
                        if (spawnerClassType == null)
                        {
                            CommonUtils.TryGetType(row["SpawnerClassName"], out spawnerClassType);
                        }
                        if (spawnerClassType != null)
                        {
                            FieldInfo field = spawnerClassType.GetField("kSpawnerData", BindingFlags.Static | BindingFlags.NonPublic);
                            if (field.GetValue(null) == null)
                            {
                                field.SetValue(null, insectSpawnerData);
                            }
                            else if (((InsectSpawner.InsectSpawnerData)field.GetValue(null)).mVersion < insectSpawnerData.mVersion)
                            {
                                field.SetValue(null, insectSpawnerData);
                            }
                        }
                    }
                }
            }
        }
    }
}
