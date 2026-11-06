using System;
using System.Collections.Generic;
using System.Reflection;
using Destrospean.Utils;
using Sims3.Gameplay;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Objects.Alchemy;
using Sims3.Gameplay.Objects.Fishing;
using Sims3.Gameplay.Objects.FoodObjects;
using Sims3.Gameplay.Objects.Insect;
using Sims3.Gameplay.Objects.Miscellaneous.Shopping;
using Sims3.Gameplay.Objects.Register;
using Sims3.Gameplay.Objects.Spawners;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using Queries = Sims3.Gameplay.Queries;

namespace Destrospean.ExpandedAlchemy
{
    public class Main
    {
        public class DebugInfo
        {
            public Assembly Assembly;

            public Exception Exception;

            public XmlDbTable Table;

            public DebugInfoType Type;

            public DebugInfo(DebugInfoType type, Assembly assembly, XmlDbTable table, Exception exception)
            {
                Assembly = assembly;
                Exception = exception;
                Table = table;
                Type = type;
            }
        }

        public enum DebugInfoType : byte
        {
            Error,
            Warning
        }

        private const string kPotionConsignmentShopRegisterScriptClassName = "Sims3.Gameplay.Destrospean.ExpandedAlchemy.Register.PotionShopConsignmentRegister";

        [Tunable]
        protected static bool kDebug;

        [Tunable]
        protected static bool kReplaceAlchemyRecipeData = true;

        [Tunable]
        protected static bool kReplaceFishingData = true;

        [Tunable]
        protected static bool kReplaceGemCutData = true;

        [Tunable]
        protected static bool kReplaceRockGemMetalData = true;

        public static List<DebugInfo> ErrorsAndWarnings = new List<DebugInfo>();

        static Main()
        {
            var listeners = new List<EventListener>();
            World.OnObjectPlacedInLotEventHandler += (sender, e) =>
                {
                    if (e is World.OnObjectPlacedInLotEventArgs onObjectPlacedInLotEventArgs)
                    {
                        var gameObject = GameObject.GetObject(onObjectPlacedInLotEventArgs.ObjectId);
                        if (gameObject is PotionShopConsignmentRegister)
                        {
                            gameObject.TryChangeScriptClass(kPotionConsignmentShopRegisterScriptClassName);
                        }
                    }
                };
            World.sOnWorldLoadFinishedEventHandler += (sender, e) =>
                {
                    foreach (var register in Queries.GetObjects<PotionShopConsignmentRegister>())
                    {
                        register.TryChangeScriptClass(kPotionConsignmentShopRegisterScriptClassName);
                    }
                    listeners.Add(EventTracker.AddListener(EventTypeId.kExitInWorldSubState, evt =>
                        {
                            try
                            {
                                if (((InWorldSubStateEvent)evt).State is EditTownState)
                                {
                                    foreach (var register in Queries.GetObjects<PotionShopConsignmentRegister>())
                                    {
                                        register.TryChangeScriptClass(kPotionConsignmentShopRegisterScriptClassName);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                ((IScriptErrorWindow)AppDomain.CurrentDomain.GetData("ScriptErrorWindow")).DisplayScriptError(null, ex);
                            }
                            return ListenerAction.Keep;
                        }));
                };
            World.sOnWorldLoadStartedEventHandler += (sender, e) =>
                {
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        if (assembly.GetType("Destrospean.ExpandedAlchemy.Data") == null)
                        {
                            continue;
                        }
                        var data = XmlDbData.ReadData(assembly.GetName().Name);
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
                                foreach (var row in table.Rows)
                                {
                                    if (string.IsNullOrEmpty(row["Hex"]))
                                    {
                                        continue;
                                    }
                                    if (!ParserFunctions.TryParseEnum(row["Hex"], out var fishType, FishType.None))
                                    {
                                        CommonUtils.AddEnumValue<FishType>(row["Hex"], fishType = (FishType)ResourceUtils.HashString64(row["Hex"]));
                                    }
                                    if (Fish.sFishData.ContainsKey(fishType))
                                    {
                                        if (!kReplaceFishingData)
                                        {
                                            ErrorsAndWarnings.Add(new DebugInfo(DebugInfoType.Warning, assembly, table, new Exception("A Fish of type \"" + row["Hex"] +  "\" already exists.")));
                                            continue;
                                        }
                                        Fish.sFishData.Remove(fishType);
                                    }
                                    if (row.TryGetEnum("CodeVersion", out var version, ProductVersion.BaseGame) && GameUtils.IsInstalled(version))
                                    {
                                        var fishData = new FishData
                                        {
                                            Version = version,
                                            StringKeyName = "Gameplay/Excel/Fishing/Fish:" + row["Name"],
                                            StringKeyDescription = "Gameplay/Excel/Fishing/Fish:" + row["Description"],
                                            Rarity = "Gameplay/Excel/RockGemMetal/Rarity:" + row["Rarity"],
                                            MedatorName = row["MedatorName"],
                                            LocationFound = (WaterTypes)ParserFunctions.ParseInt(row["WaterTypes"], 0),
                                            PrefferedBait = row["Bait"],
                                            Level = ParserFunctions.ParseInt(row["Level"], 1),
                                            MinWeight = ParserFunctions.ParseFloat(row["MinWeight"], 0.1f),
                                            MaxWeight = ParserFunctions.ParseFloat(row["MaxWeight"], 10f),
                                            OverMax = ParserFunctions.ParseBool(row["OverMax"]),
                                            MinPrice = ParserFunctions.ParseInt(row["MinPrice"], 100)
                                        };
                                        var deltaWeight = fishData.MaxWeight - fishData.MinWeight;
                                        fishData.PriceWeightSlope = (ParserFunctions.ParseInt(row["MaxPrice"], 1000) - fishData.MinPrice) / deltaWeight;
                                        fishData.SkillPoints = ParserFunctions.ParseFloat(row["SkillPoints"], 30f);
                                        fishData.WeightSkillSlope = deltaWeight / Fish.kLevelsAboveForMaxWeight;
                                        ParserFunctions.TryParseEnum(row["EffectSize"], out fishData.EffectSize, EffectSize.Small);
                                        var price = -1;
                                        if (ParserFunctions.ParseBool(row["CanBuyFromStore"]))
                                        {
                                            price = fishData.MinPrice;
                                        }
                                        if (fishType == FishType.Goldfish)
                                        {
                                            Fish.kCostOfNewGoldfish = fishData.MinPrice;
                                        }
                                        fishData.FishBowlMaterialState = row["FishBowlMaterialState"];
                                        if (!string.IsNullOrEmpty(row["Ingredient_Key"]))
                                        {
                                            ParserFunctions.TryParseEnum(row["RequiredWorld"], out var requiredWorldName, WorldName.Undefined);
                                            fishData.IngredientData = new IngredientData(row["Ingredient_Key"], fishData.StringKeyName, row["Model_Name"], ParserFunctions.ParseFloat(row["Fertilizer_Value"], float.MinValue), ParserFunctions.ParseInt(row["Fertilizer_Days"], -1), price, requiredWorldName, version, version);
                                        }
                                        if (GameUtils.IsInstalled(ProductVersion.EP5))
                                        {
                                            fishData.ParseCatHuntingDataRow(row);
                                        }
                                        fishData.EdibleRawAnimation = row["EdibleRawAnimation"];
                                        Fish.sFishData.Add(fishType, fishData);
                                    }
                                }
                                Fishing.NumberOfFishTypes = Fish.sFishData.Count - 1;
                            }
                            if (data.Tables.TryGetValue("SpawnerFish", out table))
                            {
                                ParseFishingSpots(table);
                            }
                            if (data.Tables.TryGetValue("GlobalSpawnerFish", out table))
                            {
                                ParseGlobalFishingSpots(table, assembly);
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
                                foreach (var row in table.Rows)
                                {
                                    if (string.IsNullOrEmpty(row["Hex"]))
                                    {
                                        continue;
                                    }
                                    if (!ParserFunctions.TryParseEnum(row["CodeVersion"], out var version, ProductVersion.Undefined) || GameUtils.IsInstalled(version))
                                    {
                                        if (!ParserFunctions.TryParseEnum(row["Hex"], out var metalType, RockGemMetal.Iron))
                                        {
                                            CommonUtils.AddEnumValue<RockGemMetal>(row["Hex"], metalType = (RockGemMetal)ResourceUtils.HashString64(row["Hex"]));
                                        }
                                        if (RockGemMetalBase.sData.ContainsKey(metalType))
                                        {
                                            if (!kReplaceRockGemMetalData)
                                            {
                                                ErrorsAndWarnings.Add(new DebugInfo(DebugInfoType.Warning, assembly, table, new Exception("A Metal of type \"" + row["Hex"] +  "\" already exists.")));
                                                continue;
                                            }
                                            RockGemMetalBase.sMetals.Remove(metalType);
                                            RockGemMetalBase.sData.Remove(metalType);
                                        }
                                        RockGemMetalBase.sMetals.Add(metalType);
                                        var metalData = new MetalData();
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
                                foreach (var row in table.Rows)
                                {
                                    if (string.IsNullOrEmpty(row["Hex"]))
                                    {
                                        continue;
                                    }
                                    if (!ParserFunctions.TryParseEnum(row["CodeVersion"], out var version, ProductVersion.Undefined) || GameUtils.IsInstalled(version))
                                    {
                                        if (!ParserFunctions.TryParseEnum(row["Hex"], out var gemType, RockGemMetal.Aqua))
                                        {
                                            CommonUtils.AddEnumValue<RockGemMetal>(row["Hex"], gemType = (RockGemMetal)ResourceUtils.HashString64(row["Hex"]));
                                        }
                                        if (RockGemMetalBase.sData.ContainsKey(gemType))
                                        {
                                            if (!kReplaceRockGemMetalData)
                                            {
                                                ErrorsAndWarnings.Add(new DebugInfo(DebugInfoType.Warning, assembly, table, new Exception("A Gem of type \"" + row["Hex"] +  "\" already exists.")));
                                                continue;
                                            }
                                            RockGemMetalBase.sGems.Remove(gemType);
                                            RockGemMetalBase.sData.Remove(gemType);
                                        }
                                        RockGemMetalBase.sGems.Add(gemType);
                                        var gemData = new GemData();
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
                                if (Gem.sToughestCut != null && !RockGemMetalBase.sCutData.Contains(Gem.sToughestCut))
                                {
                                    RockGemMetalBase.sCutData.Add(Gem.sToughestCut);
                                }
                                foreach (var row in table.Rows)
                                {
                                    if (!ParserFunctions.TryParseEnum(row["CodeVersion"], out var version, ProductVersion.Undefined) || GameUtils.IsInstalled(version))
                                    {
                                        var name = "Gameplay/Excel/RockGemMetal/GemCuts:" + row["Name"];
                                        var foundIndex = RockGemMetalBase.sCutData.FindIndex(x => x.Name == name);
                                        if (foundIndex > -1)
                                        {
                                            if (!kReplaceGemCutData)
                                            {
                                                ErrorsAndWarnings.Add(new DebugInfo(DebugInfoType.Warning, assembly, table, new Exception("A GemCut with the name, \"" + row["Name"] +  "\" already exists.")));
                                                continue;
                                            }
                                            if (Gem.sToughestCut == RockGemMetalBase.sCutData[foundIndex])
                                            {
                                                Gem.sToughestCut = null;
                                                foreach (var cut in RockGemMetalBase.sCutData)
                                                {
                                                    if (cut != RockGemMetalBase.sCutData[foundIndex] && (Gem.sToughestCut == null || Gem.sToughestCut.NumberOfCuts < cut.NumberOfCuts))
                                                    {
                                                        Gem.sToughestCut = cut;
                                                    }
                                                }
                                            }
                                            RockGemMetalBase.sCutData.RemoveAt(foundIndex);
                                        }
                                        var cutData = new RockGemMetalBase.CutData
                                        {
                                            Name = name,
                                            CutName = row["cutString"],
                                            Multiplier = ParserFunctions.ParseFloat(row["Multiplier"], 1.5f),
                                            NumberOfCuts = ParserFunctions.ParseInt(row["Cuts"], 5),
                                            CostToCut = ParserFunctions.ParseInt(row["Cost"], 100)
                                        };
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
                                foreach (var row in table.Rows)
                                {
                                    if (string.IsNullOrEmpty(row["Hex"]))
                                    {
                                        continue;
                                    }
                                    if (!ParserFunctions.TryParseEnum(row["Hex"], out var rockType, RockGemMetal.SpaceRockSmall))
                                    {
                                        CommonUtils.AddEnumValue<RockGemMetal>(row["Hex"], rockType = (RockGemMetal)ResourceUtils.HashString64(row["Hex"]));
                                    }
                                    if (RockGemMetalBase.sData.ContainsKey(rockType))
                                    {
                                        if (!kReplaceRockGemMetalData)
                                        {
                                            ErrorsAndWarnings.Add(new DebugInfo(DebugInfoType.Warning, assembly, table, new Exception("A Rock of type \"" + row["Hex"] +  "\" already exists.")));
                                            continue;
                                        }
                                        RockGemMetalBase.sData.Remove(rockType);
                                    }
                                    var rockData = new RockGemMetalData();
                                    rockData.ParseRow(row, "Rocks", CollectedRgmType.Rock);
                                    RockGemMetalBase.sData.Add(rockType, rockData);
                                }
                            }
                            if (data.Tables.TryGetValue("RockNames", out table))
                            {
                                //Rock.ParseNames(xmlDbTable);
                                Rock.RockNames = Rock.RockNames ?? new List<Rock.RockName>();
                                foreach (var row in table.Rows)
                                {
                                    var name = "Gameplay/Excel/RockGemMetal/RockNames:" + row["Name"];
                                    var foundIndex = Rock.RockNames.FindIndex(x => x.Name == name);
                                    if (foundIndex > -1)
                                    {
                                        if (!kReplaceRockGemMetalData)
                                        {
                                            ErrorsAndWarnings.Add(new DebugInfo(DebugInfoType.Warning, assembly, table, new Exception("A RockName with the name, \"" + row["Name"] +  "\" already exists.")));
                                            continue;
                                        }
                                        Rock.RockNames.RemoveAt(foundIndex);
                                    }
                                    var rockName = new Rock.RockName
                                    {
                                        Name = name,
                                        Multiplier = ParserFunctions.ParseFloat(row["ValueMultiplier"], 1f)
                                    };
                                    Rock.RockNames.Add(rockName);
                                }
                                RockGemMetalData.NumberOfRocks = Rock.RockNames.Count;
                            }
                            if (data.Tables.TryGetValue("SpawnersRockGemMetal", out table))
                            {
                                //RockGemMetalSpawner.ParseSpawnerData(xmlDbTable);
                                foreach (var row in table.Rows)
                                {
                                    var parseSuccessful = ParserFunctions.TryParseCommaSeparatedList(row["Spawns"], out var rockGemMetalList, RockGemMetal.Iron);
                                    if (parseSuccessful)
                                    {
                                        parseSuccessful = ParserFunctions.ParseCommaSeperatedInt(row["Probability"], out var probability);
                                        if (parseSuccessful)
                                        {
                                            for (var i = rockGemMetalList.Count - 1; i > -1; i--)
                                            {
                                                if (!RockGemMetalBase.sData.ContainsKey(rockGemMetalList[i]))
                                                {
                                                    rockGemMetalList.RemoveAt(i);
                                                    probability.RemoveAt(i);
                                                }
                                            }
                                            if (CommonUtils.TryGetType(row["SpawnerClassName"], out var spawnerClassType))
                                            {
                                                spawnerClassType.GetField("kSpawnerData", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).SetValue(null, new RockGemMetalSpawner.RgmSpawnerData(rockGemMetalList, probability, ParserFunctions.ParseInt(row["MaxSpawnCapacity"], 2), new[]
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
                                foreach (var row in table.Rows)
                                {
                                    var alchemyRecipe = new AlchemyRecipe(row);
                                    if (AlchemyRecipe.NameToRecipeMap.ContainsKey(alchemyRecipe.Key))
                                    {
                                        if (!kReplaceAlchemyRecipeData)
                                        {
                                            ErrorsAndWarnings.Add(new DebugInfo(DebugInfoType.Warning, assembly, table, new Exception("An AlchemyRecipe with the key, \"" + alchemyRecipe.Key +  "\" already exists.")));
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
                            if (kDebug)
                            {
                                var errors = ErrorsAndWarnings.FindAll(x => x.Type == DebugInfoType.Error);
                                var warnings = ErrorsAndWarnings.FindAll(x => x.Type == DebugInfoType.Warning);
                                var initMessage = "Expanded Alchemy -- Initialization ";
                                if (errors.Count == 0)
                                {
                                    if (warnings.Count == 0)
                                    {
                                        initMessage += "successful";
                                    }
                                    else
                                    {
                                        initMessage += "successful with " + warnings.Count + " warnings";
                                    }
                                }
                                else if (warnings.Count == 0)
                                {
                                    initMessage += "failed with " + errors.Count + " errors";
                                }
                                else
                                {
                                    initMessage += "failed with " + errors.Count + " errors and " + warnings.Count + " warnings";
                                }
                                StyledNotification.Show(new StyledNotification.Format(initMessage, StyledNotification.NotificationStyle.kDebugAlert));
                            }
                            foreach (var debugInfo in ErrorsAndWarnings)
                            {
                                if (!kDebug && debugInfo.Type == DebugInfoType.Warning)
                                {
                                    continue;
                                }
                                var infoText = "XML: " + debugInfo.Assembly.GetName().Name + "\nTable: " + debugInfo.Table.Name + "\nInfo: " + debugInfo.Exception.Message;
                                StyledNotification.Show(new StyledNotification.Format("Expanded Alchemy -- " + debugInfo.Type + ":\n" + infoText, StyledNotification.NotificationStyle.kDebugAlert));
                                var fileHandle = 0u;
                                Simulator.CreateExportFile(ref fileHandle, "ExpandedAlchemy_" + debugInfo.Type + "_");
                                if (fileHandle != 0u)
                                {
                                    var xmlWriter = new CustomXmlWriter(fileHandle);
                                    xmlWriter.WriteToBuffer(infoText);
                                    xmlWriter.WriteEndDocument();
                                }
                            }
                        }, "Expanded Alchemy -- Show Debug Info Alarm", AlarmType.NeverPersisted, null);
                };
            World.sOnWorldQuitEventHandler += (sender, e) =>
                {
                    foreach (var listener in listeners)
                    {
                        EventTracker.RemoveListener(listener);
                    }
                    listeners.Clear();
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
                if (!ParserFunctions.TryParseEnum(guidKey, out var guid, default(T)))
                {
                    CommonUtils.AddEnumValue<T>(guidKey, guid = (T)(object)ResourceUtils.HashString64(guidKey));
                }
                return guid;
            }
            var nameGuidPair = guidKey.Split('=');
            if (nameGuidPair.Length == 2)
            {
                name = nameGuidPair[0].Trim();
                return (T)(object)Convert.ToUInt64(nameGuidPair[1], 16);
            }
            return default(T);
        }

        public static void ParseData(XmlDbData data, bool isStore, string nameSpreadsheet, InsectClassification insectClassification)
        {
            if (!data.Tables.TryGetValue(nameSpreadsheet, out var table) || table == null)
            {
                return;
            }
            foreach (var row in table.Rows)
            {
                if (!ParserFunctions.TryParseEnum(row["CodeVersion"], out var productVersion, ProductVersion.Undefined) || GameUtils.IsInstalled(productVersion))
                {
                    var version = 0f;
                    if (isStore)
                    {
                        version = ParserFunctions.ParseFloat(row["Version"], 1f);
                    }
                    var guidKey = row["Type"];
                    var guid = GetGuid<InsectType>(guidKey, isStore, ref guidKey);
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
                        if (insectData == null)
                        {
                            continue;
                        }
                        insectData.mVersion = version;
                        insectData.ParseRow(row, nameSpreadsheet);
                        if (!InsectData.sData.TryGetValue(guid, out var value))
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

        public static void ParseFishingSpots(XmlDbTable table)
        {
            if (table == null)
            {
                return;
            }
            foreach (var row in table.Rows)
            {
                var parseSuccessful = ParserFunctions.TryParseCommaSeparatedList(row["InactiveFish"], out var inactiveFish, FishType.None);
                if (parseSuccessful)
                {
                    parseSuccessful = ParserFunctions.ParseCommaSeperatedInt(row["InactiveProbability"], out var inactiveProb);
                    if (parseSuccessful)
                    {
                        var activeMaxFish = ParserFunctions.ParseInt(row["ActiveMaxFish"], -1);
                        var maxRange = ParserFunctions.ParseFloat(row["MaxRange"], 1f);
                        if (maxRange > FishingSpot.sMaxDistanceForSpawnerSearch)
                        {
                            FishingSpot.sMaxDistanceForSpawnerSearch = maxRange;
                        }
                        FishingSpotData fishingSpotData;
                        if (activeMaxFish < 1)
                        {
                            fishingSpotData = new FishingSpotData(inactiveFish, inactiveProb, maxRange, ParserFunctions.ParseFloat(row["AvgLength"], 30f));
                        }
                        else
                        {
                            parseSuccessful = ParserFunctions.TryParseCommaSeparatedList(row["ActiveFish"], out var activeFish, FishType.None);
                            if (!parseSuccessful)
                            {
                                continue;
                            }
                            parseSuccessful = ParserFunctions.ParseCommaSeperatedInt(row["ActiveProbability"], out var activeProb);
                            if (!parseSuccessful)
                            {
                                continue;
                            }
                            ParserFunctions.ParseCommaSeparatedString(row["ActiveVfx"], out var activeVfx);
                            fishingSpotData = new FishingSpotData(inactiveFish, inactiveProb, activeFish, activeProb, activeMaxFish, activeVfx, maxRange, ParserFunctions.ParseFloat(row["HourActive"], -1f), ParserFunctions.ParseFloat(row["HourDepletionStarts"], -1f), ParserFunctions.ParseFloat(row["HourInactive"], -1f), ParserFunctions.ParseFloat(row["AvgLength"], 30f));
                        }
                        if (CommonUtils.TryGetType(row["SpawnerClassName"], out var spawnerClassType))
                        {
                            spawnerClassType.GetField("sFishingSpotData", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(null, fishingSpotData);
                        }
                    }
                }
            }
        }

        public static void ParseGlobalFishingSpots(XmlDbTable table, Assembly assembly = null)
        {
            if (table == null)
            {
                return;
            }
            foreach (var row in table.Rows)
            {
                var parseSuccessful = ParserFunctions.TryParseCommaSeparatedList(row["Fish"], out var inactiveFish, FishType.None);
                if (parseSuccessful)
                {
                    parseSuccessful = row.TryGetEnum("WorldName", out var worldName, WorldName.SunsetValley);
                    if (parseSuccessful)
                    {
                        parseSuccessful = ParserFunctions.ParseCommaSeperatedInt(row["Probability"], out var inactiveProb);
                        if (parseSuccessful)
                        {
                            var waterType = (WaterTypes)ParserFunctions.ParseInt(row["WaterAffected"], 0);
                            var fishingData = new FishingData(inactiveFish, inactiveProb, ParserFunctions.ParseFloat(row["AvgLength"], 30f));
                            if (FishingSpot.sGlobalFishingData.TryGetValue(worldName, out var fishingDataByWaterType))
                            {
                                if (fishingDataByWaterType.ContainsKey(waterType))
                                {
                                    if (!kReplaceFishingData)
                                    {
                                        ErrorsAndWarnings.Add(new DebugInfo(DebugInfoType.Warning, assembly, table, new Exception("A FishingData for WaterTypes \"" + waterType +  "\" already exists for WorldName \"" + worldName + "\"")));
                                        continue;
                                    }
                                    fishingDataByWaterType.Remove(waterType);
                                }
                                fishingDataByWaterType.Add(waterType, fishingData);
                            }
                            else
                            {
                                fishingDataByWaterType = new Dictionary<WaterTypes, FishingData>
                                {
                                    {
                                        waterType,
                                        fishingData
                                    }
                                };
                                FishingSpot.sGlobalFishingData.Add(worldName, fishingDataByWaterType);
                            }
                        }
                    }
                }
            }
        }

        public static void ParseInsectSpawnerData(XmlDbData data, bool isStore)
        {
            var version = 0f;
            if (!data.Tables.TryGetValue("SpawnersInsect", out var table) || table == null)
            {
                return;
            }
            foreach (var row in table.Rows)
            {
                var parseSuccessful = Insect.sInsectsTypes.TryParseSuperSetEnumCommaSeparatedList(row["Spawns"], out var insectTypeList, InsectType.Undefined);
                if (parseSuccessful)
                {
                    parseSuccessful = ParserFunctions.ParseCommaSeperatedInt(row["Probability"], out var probability);
                    if (parseSuccessful)
                    {
                        var insectTypeCount = insectTypeList.Count;
                        for (var i = insectTypeCount - 1; i > -1; i--)
                        {
                            if (!InsectData.sData.ContainsKey(insectTypeList[i]))
                            {
                                insectTypeList.RemoveAt(i);
                                probability.RemoveAt(i);
                            }
                        }
                        insectTypeCount = insectTypeList.Count;
                        var probabilityCount = probability.Count;
                        var insectProbabilities = new InsectSpawner.InsectProbability[insectTypeCount];
                        for (var i = 0; i < insectTypeCount; i++)
                        {
                            var weightedProbability = 1f;
                            if (i <= probabilityCount - 1)
                            {
                                weightedProbability = probability[i];
                            }
                            insectProbabilities[i] = new InsectSpawner.InsectProbability(insectTypeList[i], weightedProbability);
                        }
                        if (isStore)
                        {
                            version = ParserFunctions.ParseFloat(row["Version"], 1f);
                        }
                        var insectSpawnerData = new InsectSpawner.InsectSpawnerData(insectProbabilities, new[]
                            {
                                ParserFunctions.ParseFloat(row["TimeOfDayStart"], 0f),
                                ParserFunctions.ParseFloat(row["TimeOfDayStop"], 24f)
                            }, ParserFunctions.ParseListOfDaysToFlagEnum(ParserFunctions.ParseDayList(row["DaysOfWeek"])), ParserFunctions.ParseInt(row["MaxSpawnCapacity"], 2), new[]
                            {
                                ParserFunctions.ParseFloat(row["MinSpawnFrequency"], 0.5f),
                                ParserFunctions.ParseFloat(row["MaxSpawnFrequency"], 2f)
                            }, ParserFunctions.ParseFloat(row["SpawnChance"], 1f), ParserFunctions.ParseFloat(row["DeleteUntaken"], 2f), ParserFunctions.ParseBool(row["ButterflySpawner"]), ParserFunctions.ParseBool(row["BeetleSpawner"]), version);
                        var spawnerClassName = row["SpawnerClassName"];
                        Type spawnerClassType = null;
                        if (isStore)
                        {
                            var spawnerClassNameSeparated = spawnerClassName.Split('|');
                            if (spawnerClassNameSeparated.Length > 1)
                            {
                                if (!CommonUtils.TryGetType(spawnerClassName = spawnerClassNameSeparated[0] + ",Sims3StoreObjects", out spawnerClassType))
                                {
                                    spawnerClassName = spawnerClassNameSeparated[0] + ',' + spawnerClassNameSeparated[1];
                                }
                            }
                        }
                        if (spawnerClassType == null)
                        {
                            CommonUtils.TryGetType(spawnerClassName, out spawnerClassType);
                        }
                        if (spawnerClassType != null)
                        {
                            var field = spawnerClassType.GetField("kSpawnerData", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                            if (field?.GetValue(null) == null)
                            {
                                field?.SetValue(null, insectSpawnerData);
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
