using System;
using System.Globalization;
using Destrospean.Enums;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;

namespace Destrospean.Utils
{
    public static class SkillUtils
    {
        public static void LoadSkillData(string instanceName)
        {
            var xmlDbData = XmlDbData.ReadData(new ResourceKey(ResourceUtils.HashString64(instanceName), 0xA8D58BE5, 0x00000000), false);
            if (xmlDbData != null)
            {
                ParseSkillData(xmlDbData, true);
            }
        }
        
        public static void ParseSkillData(XmlDbData data, bool isStore)
        {
            if (data?.Tables == null)
            {
                return;
            }
            data.Tables.TryGetValue("SkillList", out var xmlDbTable);
            if (xmlDbTable == null)
            {
                return;
            }
            foreach (var row in xmlDbTable.Rows)
            {
                var isInvalid = false;
                var guidKey = row.GetString("Hex");
                var guid = (SkillNames)SkillManager.GetGuid(ref guidKey, isStore);
                if (guid == SkillNames.None)
                {
                    guid = (SkillNames)ResourceUtils.HashString64(guidKey);
                }
                CommonUtils.AddEnumValue<SkillNames>(guidKey, guid);
                if (!row.TryGetEnum("CodeVersion", out var version, ProductVersion.BaseGame) || !GameUtils.IsInstalled(version))
                {
                    isInvalid = true;
                }
                var skillVersion = 0.0;
                if (!isInvalid)
                {
                    Skill skill = null;
                    var customClassName = row.GetString("CustomClassName");
                    var hasCustomClass = customClassName.Length > 0;
                    if (hasCustomClass)
                    {
                        Type type = null;
                        if (isStore)
                        {
                            var customClassNameSplit = customClassName.Split(',');
                            if (customClassNameSplit.Length < 2)
                            {
                                isInvalid = true;
                            }
                            else
                            {
                                type = Type.GetType(customClassNameSplit[0] + ",Sims3StoreObjects");
                            }
                        }
                        if (type == null)
                        {
                            type = Type.GetType(customClassName);
                        }
                        if (type == null)
                        {
                            isInvalid = true;
                        }
                        else
                        {
                            var parameters = new object[]
                                {
                                    guid
                                };
                            var constructor = type.GetConstructor(Type.GetTypeArray(parameters));
                            if (constructor == null)
                            {
                                isInvalid = true;
                            }
                            else if ((skill = constructor.Invoke(parameters) as Skill) == null)
                            {
                                isInvalid = true;
                            }
                        }
                    }
                    else
                    {
                        skill = new Skill(guid);
                    }
                    if (!isInvalid)
                    {
                        var persistableSkillData = new Skill.NonPersistableSkillData();
                        skill.NonPersistableData = persistableSkillData;
                        var groupId = ResourceUtils.ProductVersionToGroupId(version);
                        persistableSkillData.SkillProductVersion = version;
                        persistableSkillData.Name = "Gameplay/Excel/Skills/SkillList:" + row.GetString("SkillName");
                        if (!isStore || Localization.HasLocalizationString(persistableSkillData.Name))
                        {
                            var skillDescription = row.GetString("SkillDescription");
                            persistableSkillData.Description = string.IsNullOrEmpty(skillDescription) ? "" : "Gameplay/Excel/Skills/SkillList:" + skillDescription;
                            persistableSkillData.MaxSkillLevel = row.GetInt("MaxSkillLevel", 0);
                            skill.Guid = guid;
                            persistableSkillData.ThoughtBalloonTopicString = row.GetString("ThoughtBalloonTopic");
                            var name = row.GetString("IconKey");
                            persistableSkillData.IconKey = ResourceKey.CreatePNGKey(name, groupId);
                            persistableSkillData.SkillUIIconKey = ResourceKey.CreatePNGKey(row.GetString("SkillUIIcon"), groupId);
                            var commodityKey = row.GetString("Commodity");
                            var commodity = (CommodityKind)SkillManager.GetCommodity(commodityKey, isStore);
                            if (commodity == CommodityKind.None && !string.IsNullOrEmpty(commodityKey))
                            {
                                commodity = CommodityUtils.GetCommodityKind(commodityKey, CommodityKindType.Skill);
                            }
                            persistableSkillData.Commodity = commodity;
                            if (commodity != CommodityKind.None)
                            {
                                CommonUtils.AddEnumValue<CommodityKind>(commodityKey, commodity);
                                SkillManager.SkillCommodityMap[commodity] = guid;
                            }
                            if (isStore)
                            {
                                skillVersion = Convert.ToDouble(row.GetString("Version"), CultureInfo.InvariantCulture);
                            }
                            persistableSkillData.SkillVersion = skillVersion;
                            if (row.GetBool("Physical"))
                            {
                                skill.AddCategoryToSkill(SkillCategory.Physical);
                            }
                            if (row.GetBool("Mental"))
                            {
                                skill.AddCategoryToSkill(SkillCategory.Mental);
                            }
                            if (row.GetBool("Musical"))
                            {
                                skill.AddCategoryToSkill(SkillCategory.Musical);
                            }
                            if (row.GetBool("Creative"))
                            {
                                skill.AddCategoryToSkill(SkillCategory.Creative);
                            }
                            if (row.GetBool("Artistic"))
                            {
                                skill.AddCategoryToSkill(SkillCategory.Artistic);
                            }
                            if (row.GetBool("Hidden"))
                            {
                                skill.AddCategoryToSkill(SkillCategory.Hidden);
                            }
                            if (row.GetBool("Certificate"))
                            {
                                skill.AddCategoryToSkill(SkillCategory.Certificate);
                            }
                            if (row.Exists("HiddenWithSkillProgress") && row.GetBool("HiddenWithSkillProgress") && skill.HasCategory(SkillCategory.Hidden))
                            {
                                skill.AddCategoryToSkill(SkillCategory.HiddenWithSkillProgress);
                            }
                            if (row.GetBool("CanDecay"))
                            {
                                skill.AddCategoryToSkill(SkillCategory.CanDecay);
                            }
                            var pointsForNextLevel = new int[skill.MaxSkillLevel];
                            var pointsForCurrentLevel = 0;
                            for (var i = 1; i <= skill.MaxSkillLevel; ++i)
                            {
                                var column = "Level_" + i;
                                pointsForCurrentLevel += row.GetInt(column, 0);
                                pointsForNextLevel[i - 1] = pointsForCurrentLevel;
                            }
                            persistableSkillData.PointsForNextLevel = pointsForNextLevel;
                            persistableSkillData.AlwaysDisplayLevelUpTns = row.GetBool("AlwaysDisplayTNS");
                            var levelUpStrings = new string[skill.MaxSkillLevel + 1];
                            for (var i = 2; i <= skill.MaxSkillLevel; ++i)
                            {
                                var column = $"Level_{i}_Text";
                                levelUpStrings[i - 1] = row.GetString(column);
                                if (levelUpStrings[i - 1] != "")
                                {
                                    levelUpStrings[i - 1] = "Gameplay/Excel/Skills/SkillList:" + levelUpStrings[i - 1];
                                }
                            }
                            levelUpStrings[skill.MaxSkillLevel] = row.GetString("Level_10_Text_Alternate");
                            if (levelUpStrings[skill.MaxSkillLevel] != "")
                            {
                                levelUpStrings[skill.MaxSkillLevel] = "Gameplay/Excel/Skills/SkillList:" + levelUpStrings[skill.MaxSkillLevel];
                            }
                            persistableSkillData.LevelUpStrings = levelUpStrings;
                            if (hasCustomClass)
                            {
                                var key = row.GetString("CustomDataSheet");
                                data.Tables.TryGetValue(key, out var skillData);
                                if (skillData == null && key.Length > 0)
                                {
                                    isInvalid = true;
                                    skill = null;
                                }
                                else if (!skill.ParseSkillData(skillData))
                                {
                                    isInvalid = true;
                                    skill = null;
                                }
                            }
                            persistableSkillData.AvailableAgeSpecies = ParserFunctions.ParseAllowableAgeSpecies(row, "AvailableAgeSpecies");
                            persistableSkillData.DreamsAndPromisesIcon = row.GetString("DreamsAndPromisesIcon");
                            persistableSkillData.DreamsAndPromisesIconKey = ResourceKey.CreatePNGKey(persistableSkillData.DreamsAndPromisesIcon, groupId);
                            persistableSkillData.LogicSkillBoost = row.GetBool("LogicSkillBoost");
                            if (!isInvalid)
                            {
                                if (GenericManager<SkillNames, Skill, Skill>.sDictionary.ContainsKey((ulong)guid))
                                {
                                    if (GenericManager<SkillNames, Skill, Skill>.sDictionary[(ulong)guid].SkillVersion < skill.SkillVersion)
                                    {
                                        GenericManager<SkillNames, Skill, Skill>.sDictionary[(ulong)guid] = skill;
                                    }
                                }
                                else
                                {
                                    GenericManager<SkillNames, Skill, Skill>.sDictionary.Add((ulong)guid, skill);
                                    SkillManager.sSkillEnumValues.AddNewEnumValue(guidKey, guid);
                                }
                            }
                        }
                    }
                }
            }
            if (isStore)
            {
                return;
            }
            SkillManager.ParsePetSkillFatigueRates(data);
        }
    }
}
