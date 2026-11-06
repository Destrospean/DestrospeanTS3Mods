using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using Destrospean.Enums;
using Destrospean.Misc;
using Destrospean.UI.Columns;
using Sims3.Gameplay;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.Utilities;
using Sims3.Metadata;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using Sims3.UI.Controller;
using ComboSelectionDialog = Destrospean.UI.Dialogs.ComboSelectionDialog;
using ObjectPickerDialog = Destrospean.UI.Dialogs.ObjectPickerDialog;
using Queries = Sims3.Gameplay.Queries;

namespace Destrospean.Utils
{
    public static class CommonUtils
    {
        private const string kAuthorName = "Destrospean";

        private static bool ChangePreset(DesignModeSwap swap, SortedList<string, Complate> patterns, ref string presetXml, bool undoable, SortedList<string, bool> enabledStencils)
        {
            if (string.IsNullOrEmpty(presetXml))
            {
                return false;
            }
            var patternComplates = new Dictionary<string, Complate>();
            var processedPreset = Complate.ProcessPreset(presetXml, patternComplates);
            if (string.IsNullOrEmpty(processedPreset) || !patternComplates.TryGetValue(processedPreset, out var presetComplate))
            {
                return false;
            }
            var variables = new List<Complate.Variable>();
            foreach (var variable in presetComplate.Variables)
            {
                bool result;
                if (enabledStencils != null && variable.Type == Complate.Variable.Types.Bool)
                {
                    if (enabledStencils.TryGetValue(variable.Name, out result))
                    {
                        variable.Value = result.ToString();
                    }
                }
                else
                {
                    if (variable.Type != Complate.Variable.Types.Pattern)
                    {
                        continue;
                    }
                    var enabled = variable[Complate.kEnabledAttribute];
                    if (enabled != "")
                    {
                        enabled = presetComplate[enabled];
                        if (!bool.TryParse(enabled, out result) || !result)
                        {
                            continue;
                        }
                    }
                    variables.Add(variable);
                    if (enabledStencils == null && variables.Count == 4)
                    {
                        break;
                    }
                }
            }
            if (variables.Count == 0)
            {
                return false;
            }
            foreach (var texturePart in presetComplate.Parts)
            {
                foreach (var textureDestination in texturePart.Destinations)
                {
                    foreach (var textureStep in textureDestination.Steps)
                    {
                        if (textureStep[Complate.kTypeAttribute] == "DrawFabric")
                        {
                            var variableName = textureStep["pattern"];
                            if (variableName.StartsWith("($daeFileName)"))
                            {
                                variableName = variableName.Remove(0, 14);
                            }
                            textureStep["patternkey"] = swap.GetPatternKey(variableName).ToString();
                        }
                    }
                }
            }
            foreach (var pattern in patterns)
            {
                if (patternComplates.ContainsKey(pattern.Key))
                {
                    patternComplates[pattern.Key] = pattern.Value;
                }
            }
            for (var i = 0; i < variables.Count; i++)
            {
                if (!patternComplates.TryGetValue(variables[i].Name, out var pattern))
                {
                    swap.Dispose();
                    return false;
                }
                pattern.Process();
                var textureCompositor = pattern.CreateTextureCompositor(null, null, 0u);
                if (textureCompositor != null)
                {
                    swap.SetNewPattern(variables[i].Name, textureCompositor.ExportData(null, null, null));
                }
            }
            presetComplate.Process();
            var hashtable = presetComplate.CreateTextureCompositors(0u);
            foreach (DictionaryEntry entry in hashtable)
            {
                var key = ((string)entry.Key).ToLowerInvariant();
                if (key.EndsWith("diffusemap"))
                {
                    swap.SetNewCompositor("diffusemap", ((TextureCompositor)entry.Value).ExportData(null, null, null));
                }
                else if (key.EndsWith("specmap"))
                {
                    swap.SetNewCompositor("specularmap", ((TextureCompositor)entry.Value).ExportData(null, null, null));
                }
            }
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml("<preset />");
            presetComplate.AddToPreset(xmlDocument, patternComplates);
            Complate.ConvertPresetToResourceKeys(xmlDocument, null);
            presetXml = xmlDocument.OuterXml;
            return true;
        }

        /// <summary>
        /// Adds actions to an active topic.
        /// </summary>
        /// <param name="activeTopic">Active topic.</param>
        /// <param name="grouping">Grouping.</param>
        /// <param name="isActive">If set to <c>true</c>, the action is for the first person actor; otherwise, it's for the second person actor.</param>
        /// <param name="actionsToAdd">Actions to add.</param>
        public static void AddActions(string activeTopic, LongTermRelationshipTypes grouping, bool isActive, params string[] actionsToAdd)
        {
            if (!ActionAvailabilityData.sActiveTopicInteractions.TryGetValue(activeTopic, out var groups))
            {
                groups = new Dictionary<LongTermRelationshipTypes, Dictionary<bool, List<string>>>();
                ActionAvailabilityData.sActiveTopicInteractions.Add(activeTopic, groups);
            }
            if (!groups.TryGetValue(grouping, out var group))
            {
                group = new Dictionary<bool, List<string>>();
                groups.Add(grouping, group);
            }
            if (!group.TryGetValue(isActive, out var actions))
            {
                actions = new List<string>();
                group.Add(isActive, actions);
            }
            actions.AddRange(actionsToAdd);
        }

        /// <summary>
        /// Adds the commodity kind as a commodity change output to an interaction tuning.
        /// </summary>
        /// <param name="commodityKind">Commodity kind.</param>
        /// <param name="constantChange">Advertised value.</param>
        /// <param name="locked">If set to <c>true</c>, locked.</param>
        /// <param name="actualValue">Actual value.</param>
        /// <param name="updateType">Update type.</param>
        /// <param name="timeDependsOnCommodityFilling">If set to <c>true</c> time depends on commodity filling.</param>
        /// <param name="updateEvenOnFailure">If set to <c>true</c> update even on failure.</param>
        /// <param name="updateAboveAndBelowZero">Update above and below zero.</param>
        /// <typeparam name="InteractionDefinition">Interaction definition type.</typeparam>
        /// <typeparam name="Target">Target type.</typeparam>
        public static void AddAsOutput<InteractionDefinition, Target>(this CommodityKind commodityKind, float constantChange, bool locked, float actualValue, OutputUpdateType updateType, bool timeDependsOnCommodityFilling = false, bool updateEvenOnFailure = false, UpdateAboveAndBelowZeroType updateAboveAndBelowZero = UpdateAboveAndBelowZeroType.Either) where InteractionDefinition : Sims3.Gameplay.Interactions.InteractionDefinition where Target : IGameObject
        {
            commodityKind.AddAsOutput(typeof(InteractionDefinition), typeof(Target), constantChange, locked, actualValue, updateType, timeDependsOnCommodityFilling, updateEvenOnFailure, updateAboveAndBelowZero);
        }

        /// <summary>
        /// Adds the commodity kind as a commodity change output to an interaction tuning.
        /// </summary>
        /// <param name="commodityKind">Commodity kind.</param>
        /// <param name="interactionDefinitionType">Interaction definition type.</param>
        /// <param name="targetType">Target type.</param>
        /// <param name="constantChange">Advertised value.</param>
        /// <param name="locked">If set to <c>true</c>, locked.</param>
        /// <param name="actualValue">Actual value.</param>
        /// <param name="updateType">Update type.</param>
        /// <param name="timeDependsOnCommodityFilling">If set to <c>true</c> time depends on commodity filling.</param>
        /// <param name="updateEvenOnFailure">If set to <c>true</c> update even on failure.</param>
        /// <param name="updateAboveAndBelowZero">Update above and below zero.</param>
        public static void AddAsOutput(this CommodityKind commodityKind, Type interactionDefinitionType, Type targetType, float constantChange, bool locked, float actualValue, OutputUpdateType updateType, bool timeDependsOnCommodityFilling = false, bool updateEvenOnFailure = false, UpdateAboveAndBelowZeroType updateAboveAndBelowZero = UpdateAboveAndBelowZeroType.Either)
        {
            var outputs = AutonomyTuning.GetTuning(interactionDefinitionType.FullName, targetType)?.mTradeoff?.mOutputs;
            if (outputs == null)
            {
                return;
            }
            outputs.RemoveAll(x => x.Commodity == commodityKind && x.ConstantChange == constantChange && x.mLocked == locked && x.mActualValue == actualValue && x.mTimeDependsOnCommodityFilling == timeDependsOnCommodityFilling && x.mUpdateEvenOnFailure == updateEvenOnFailure && x.mUpdateAboveAndBelowZero == updateAboveAndBelowZero);
            outputs.Add(new CommodityChange(commodityKind, constantChange, locked, actualValue, updateType, timeDependsOnCommodityFilling, updateEvenOnFailure, updateAboveAndBelowZero));
            RefreshInteractionObjectPairs(interactionDefinitionType, targetType);
        }

        /// <summary>
        /// Adds the commodity kind as a commodity change output to an interaction tuning.
        /// </summary>
        /// <param name="commodityKind">Commodity kind.</param>
        /// <param name="interactionDefinitionType">Interaction definition type full name.</param>
        /// <param name="targetType">Target type full name.</param>
        /// <param name="constantChange">Advertised value.</param>
        /// <param name="locked">If set to <c>true</c>, locked.</param>
        /// <param name="actualValue">Actual value.</param>
        /// <param name="updateType">Update type.</param>
        /// <param name="timeDependsOnCommodityFilling">If set to <c>true</c> time depends on commodity filling.</param>
        /// <param name="updateEvenOnFailure">If set to <c>true</c> update even on failure.</param>
        /// <param name="updateAboveAndBelowZero">Update above and below zero.</param>
        public static void AddAsOutput(this CommodityKind commodityKind, string interactionDefinitionType, string targetType, float constantChange, bool locked, float actualValue, OutputUpdateType updateType, bool timeDependsOnCommodityFilling = false, bool updateEvenOnFailure = false, UpdateAboveAndBelowZeroType updateAboveAndBelowZero = UpdateAboveAndBelowZeroType.Either)
        {
            commodityKind.AddAsOutput(InteractionObjectTypeUtils.InteractionDefinitionTypes[interactionDefinitionType], InteractionObjectTypeUtils.GameObjectTypes[targetType], constantChange, locked, actualValue, updateType, timeDependsOnCommodityFilling, updateEvenOnFailure, updateAboveAndBelowZero);
        }

        public static void AddEnumValue<T>(string key, object value) where T : struct
        {
            var enumType = typeof(T);
            if (!ParserFunctions.sCaseInsensitiveEnumParsers.TryGetValue(enumType, out var caseInsensitiveEnumParser))
            {
                caseInsensitiveEnumParser = new EnumParser(enumType, true);
                ParserFunctions.sCaseInsensitiveEnumParsers.Add(enumType, caseInsensitiveEnumParser);
            }
            if (!ParserFunctions.sCaseSensitiveEnumParsers.TryGetValue(enumType, out var caseSensitiveEnumParser))
            {
                caseSensitiveEnumParser = new EnumParser(enumType, false);
                ParserFunctions.sCaseSensitiveEnumParsers.Add(enumType, caseSensitiveEnumParser);
            }
            if (!caseInsensitiveEnumParser.mLookup.ContainsKey(key.ToLowerInvariant()) && !caseSensitiveEnumParser.mLookup.ContainsKey(key))
            {
                caseInsensitiveEnumParser.mLookup.Add(key.ToLowerInvariant(), value);
                caseSensitiveEnumParser.mLookup.Add(key, value);
            }
        }

        public static void ApplyPreset(this IGameObject gameObject, string presetXml)
        {
            var enabledStencils = new SortedList<string, bool>();
            Complate.SetupDesignSwap(gameObject.ObjectId, ExtractPatterns(presetXml, enabledStencils), false, enabledStencils)?.ApplyToObject();
        }

        public static void CopyTuning(Type baseType, Type oldType, Type newType)
        {
            if (AutonomyTuning.GetTuning(newType.FullName, baseType.FullName) == null)
            {
                var tuning = AutonomyTuning.GetTuning(oldType, oldType.FullName, baseType);
                if (tuning != null)
                {
                    AutonomyTuning.AddTuning(newType.FullName, baseType.FullName, tuning);
                }
            }
            InteractionObjectPair.sTuningCache.Remove(new Pair<Type, Type>(newType, baseType));
        }

        public static SortedList<string, Complate> ExtractPatterns(string presetXml, SortedList<string, bool> enabledStencils)
        {
            if (string.IsNullOrEmpty(presetXml))
            {
                return new SortedList<string, Complate>();
            }
            var patternComplates = new Dictionary<string, Complate>();
            var processedPreset = Complate.ProcessPreset(presetXml, patternComplates);
            if (string.IsNullOrEmpty(processedPreset))
            {
                return new SortedList<string, Complate>();
            }
            if (enabledStencils != null && patternComplates.TryGetValue(processedPreset, out var patternComplate))
            {
                foreach (var variable in patternComplate.Variables)
                {
                    if (variable.Type == Complate.Variable.Types.Bool)
                    {
                        var variableName = variable.Name.ToLowerInvariant();
                        if (variableName.StartsWith("stencil ") && variableName.EndsWith(" enabled"))
                        {
                            enabledStencils.Add(variable.Name, bool.Parse(variable.Value));
                        }
                    }
                }
            }
            patternComplates.Remove(processedPreset);
            return new SortedList<string, Complate>(patternComplates);
        }

        /// <summary>
        /// Gets a valid commodity kind value from a string.
        /// </summary>
        /// <returns>The commodity kind.</returns>
        /// <param name="name">The name of the commodity kind.</param>
        /// <param name="type">The type of commodity.</param>
        public static CommodityKind GetCommodityKind(string name, CommodityKindType type)
        {
            var retVal = ResourceUtils.HashString32(name);
            switch (type)
            {
                case CommodityKindType.Motive:
                    retVal = retVal & 0xF0FFFFFF | 0x01000000;
                    break;
                case CommodityKindType.Skill:
                    retVal = retVal & 0x0FFFFFFF | 0x20000000;
                    break;
                case CommodityKindType.Posture:
                    retVal = retVal & 0x00FFFFFF | 0x04000000;
                    break;
                case CommodityKindType.PostureCheck:
                    retVal = retVal & 0x00FFFFFF | 0x05000000;
                    break;
                case CommodityKindType.Trait:
                    retVal = retVal & 0x0FFFFFFF | 0x10000000;
                    break;
            }
            return (CommodityKind)retVal;
        }

        public static string GetCurrentPreset(this IGameObject gameObject)
        {
            var preset = ObjectDesigner.GetObjectDesignPreset(gameObject.ObjectId);
            if (string.IsNullOrEmpty(preset))
            {
                return null;
            }
            var designModeSwap = new DesignModeSwap();
            designModeSwap.SetSourceObject(gameObject.ObjectId);
            var enabledStencils = new SortedList<string, bool>();
            if (ChangePreset(designModeSwap, Complate.ExtractPatterns(gameObject.ObjectId, enabledStencils), ref preset, false, enabledStencils))
            {
                var xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(preset);
                var xmlDeclaration = xmlDocument.CreateXmlDeclaration("1.0", "utf-8", null);
                xmlDocument.InsertBefore(xmlDeclaration, xmlDocument.DocumentElement);
                var stringBuilder = new StringBuilder();
                var stringWriter = new Utf8StringWriter(stringBuilder);
                var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
                    {
                        Encoding = Encoding.UTF8,
                        Indent = true,
                        IndentChars = "  ",
                        OmitXmlDeclaration = false
                    });
                xmlDocument.WriteTo(xmlWriter);
                xmlWriter.Flush();
                xmlWriter.Close();
                stringWriter.Flush();
                stringWriter.Close();
                preset = stringBuilder.ToString();
            }
            return preset;
        }

        /// <summary>
        /// Gets the localization key from the class name and namespace of the object type.
        /// </summary>
        /// <returns>The localization key.</returns>
        /// <param name="type">Type.</param>
        public static string GetLocalizationKey(this Type type)
        {
            return type.Namespace?.Substring(type.Namespace.IndexOf(kAuthorName, StringComparison.Ordinal)).Replace('.', '/') + "/" + type.Name;
        }
        
        public static T GetRandomItem<T>(this IEnumerable<T> enumerable)
        {
            var list = new List<T>(enumerable);
            return list[RandomUtil.GetInt(list.Count - 1)];
        }

        public static void LoadMotive(string instanceName)
        {
            MotiveTuning.LoadTuning(Simulator.LoadXML(instanceName));
        }

        public static void LoadSocialData(string instanceName)
        {
            if (!string.IsNullOrEmpty(instanceName))
            {
                var isEp5Installed = GameUtils.IsInstalled(ProductVersion.EP5);
                var xmlDocument = Simulator.LoadXML(instanceName);
                foreach (var element in new XmlElementLookup(xmlDocument)["Action"])
                {
                    ParserFunctions.TryParseEnum(element.GetAttribute("com"), out var commodityTypes, CommodityTypes.Undefined);
                    ActionData.Add(new ActionData(element.GetAttribute("key"), commodityTypes, ProductVersion.BaseGame, new XmlElementLookup(element), isEp5Installed));
                }
            }
        }

        public static void LoadSocializingActionAvailability(string instanceName)
        {
            var data = XmlDbData.ReadData(instanceName);
            if (data != null)
            {
                if (data.Tables.ContainsKey("SAA"))
                {
                    SocialManager.ParseStcActionAvailability(data);
                }
                if (data.Tables.ContainsKey("TAA"))
                {
                    SocialManager.ParseActiveTopic(data);
                }
            }
        }

        /// <summary>
        /// Refreshes the interaction object pairs of a specified interaction definition for all existing objects of a specified type in the world.
        /// </summary>
        /// <typeparam name="InteractionDefinition">Interaction definition type.</typeparam>
        /// <typeparam name="Target">Target type.</typeparam>
        public static void RefreshInteractionObjectPairs<InteractionDefinition, Target>() where InteractionDefinition : Sims3.Gameplay.Interactions.InteractionDefinition where Target : IGameObject
        {
            RefreshInteractionObjectPairs(typeof(InteractionDefinition), typeof(Target));
        }

        /// <summary>
        /// Refreshes the interaction object pairs of a specified interaction definition for all existing objects of a specified type in the world.
        /// </summary>
        /// <param name="interactionDefinitionType">Interaction definition type.</param>
        /// <param name="targetType">Target type.</param>
        public static void RefreshInteractionObjectPairs(Type interactionDefinitionType, Type targetType)
        {
            foreach (var gameObject in Queries.GetObjects<GameObject>())
            {
                if (targetType.IsAssignableFrom(gameObject.GetType()))
                {
                    foreach (var interaction in new List<InteractionObjectPair>(gameObject.Interactions))
                    {
                        if (interactionDefinitionType.IsAssignableFrom(interaction.mInteraction.GetType()) && targetType.IsAssignableFrom(interaction.mTargetType))
                        {
                            gameObject.RemoveInteraction(interaction);
                            gameObject.AddInteraction(new InteractionObjectPair(interaction.InteractionDefinition, interaction.Target, AutonomyTuning.GetTuning(interactionDefinitionType.FullName, targetType)));
                            if (gameObject.ItemComp != null && gameObject.ItemComp.InteractionsInventory.Contains(interaction))
                            {
                                gameObject.ItemComp.InteractionsInventory.Remove(interaction);
                                gameObject.AddInventoryInteraction(interaction.InteractionDefinition);
                            }
                        }
                    }
                }
            }
            foreach (var sim in Queries.GetObjects<Sim>())
            {
                sim.UpdateCommodityInteractionMap();
            }
        }

        /// <summary>
        /// Refreshes the interaction object pairs of a specified interaction definition for all existing objects of a specified type in the world.
        /// </summary>
        /// <param name="interactionDefinitionType">Interaction definition type full name.</param>
        /// <param name="targetType">Target type full name.</param>
        public static void RefreshInteractionObjectPairs(string interactionDefinitionType, string targetType)
        {
            RefreshInteractionObjectPairs(InteractionObjectTypeUtils.InteractionDefinitionTypes[interactionDefinitionType], InteractionObjectTypeUtils.GameObjectTypes[targetType]);
        }

        /// <summary>
        /// Removes actions from an active topic.
        /// </summary>
        /// <param name="activeTopic">Active topic.</param>
        /// <param name="grouping">Grouping.</param>
        /// <param name="isActive">If set to <c>true</c>, the action is for the first person actor; otherwise, it's for the second person actor.</param>
        /// <param name="actionsToRemove">Actions to remove.</param>
        public static void RemoveActions(string activeTopic, LongTermRelationshipTypes grouping, bool isActive, params string[] actionsToRemove)
        {
            if (ActionAvailabilityData.sActiveTopicInteractions.TryGetValue(activeTopic, out var groups) && groups.TryGetValue(grouping, out var group) && group.TryGetValue(isActive, out var actions))
            {
                actions.RemoveAll(x => Array.Exists(actionsToRemove, y => y == x));
            }
        }

        public static void RemoveEnumValue<T>(string key) where T : struct
        {
            if (ParserFunctions.sCaseInsensitiveEnumParsers.TryGetValue(typeof(T), out var caseInsensitiveEnumParser) && caseInsensitiveEnumParser.mLookup.ContainsKey(key.ToLowerInvariant()))
            {
                caseInsensitiveEnumParser.mLookup.Remove(key.ToLowerInvariant());
            }
            if (ParserFunctions.sCaseSensitiveEnumParsers.TryGetValue(typeof(T), out var caseSensitiveEnumParser) && caseSensitiveEnumParser.mLookup.ContainsKey(key))
            {
                caseSensitiveEnumParser.mLookup.Remove(key);
            }
        }

        /// <summary>
        /// Removes the commodity kind as a commodity change output from an interaction tuning.
        /// </summary>
        /// <param name="commodityKind">Commodity kind.</param>
        /// <param name="constantChange">Advertised value.</param>
        /// <param name="locked">If set to <c>true</c>, locked.</param>
        /// <param name="actualValue">Actual value.</param>
        /// <param name="updateType">Update type.</param>
        /// <param name="timeDependsOnCommodityFilling">If set to <c>true</c>, time depends on commodity filling.</param>
        /// <param name="updateEvenOnFailure">If set to <c>true</c>, update even on failure.</param>
        /// <param name="updateAboveAndBelowZero">Update above and below zero.</param>
        /// <typeparam name="InteractionDefinition">Interaction definition type.</typeparam>
        /// <typeparam name="Target">Target type.</typeparam>
        public static void RemoveAsOutput<InteractionDefinition, Target>(this CommodityKind commodityKind, float constantChange, bool locked, float actualValue, OutputUpdateType updateType, bool timeDependsOnCommodityFilling = false, bool updateEvenOnFailure = false, UpdateAboveAndBelowZeroType updateAboveAndBelowZero = UpdateAboveAndBelowZeroType.Either) where InteractionDefinition : Sims3.Gameplay.Interactions.InteractionDefinition where Target : IGameObject
        {
            commodityKind.RemoveAsOutput(typeof(InteractionDefinition), typeof(Target), constantChange, locked, actualValue, updateType, timeDependsOnCommodityFilling, updateEvenOnFailure, updateAboveAndBelowZero);
        }

        /// <summary>
        /// Removes the commodity kind as a commodity change output from an interaction tuning.
        /// </summary>
        /// <param name="commodityKind">Commodity kind.</param>
        /// <param name="interactionDefinitionType">Interaction definition type.</param>
        /// <param name="targetType">Target type.</param>
        /// <param name="constantChange">Advertised value.</param>
        /// <param name="locked">If set to <c>true</c>, locked.</param>
        /// <param name="actualValue">Actual value.</param>
        /// <param name="updateType">Update type.</param>
        /// <param name="timeDependsOnCommodityFilling">If set to <c>true</c>, time depends on commodity filling.</param>
        /// <param name="updateEvenOnFailure">If set to <c>true</c>, update even on failure.</param>
        /// <param name="updateAboveAndBelowZero">Update above and below zero.</param>
        public static void RemoveAsOutput(this CommodityKind commodityKind, Type interactionDefinitionType, Type targetType, float constantChange, bool locked, float actualValue, OutputUpdateType updateType, bool timeDependsOnCommodityFilling = false, bool updateEvenOnFailure = false, UpdateAboveAndBelowZeroType updateAboveAndBelowZero = UpdateAboveAndBelowZeroType.Either)
        {
            var outputs = AutonomyTuning.GetTuning(interactionDefinitionType.FullName, targetType)?.mTradeoff?.mOutputs ?? new List<CommodityChange>();
            outputs.RemoveAll(x => x.Commodity == commodityKind && x.ConstantChange == constantChange && x.mLocked == locked && x.mActualValue == actualValue && x.mTimeDependsOnCommodityFilling == timeDependsOnCommodityFilling && x.mUpdateEvenOnFailure == updateEvenOnFailure && x.mUpdateAboveAndBelowZero == updateAboveAndBelowZero);
            RefreshInteractionObjectPairs(interactionDefinitionType, targetType);
        }

        /// <summary>
        /// Removes the commodity kind as a commodity change output from an interaction tuning.
        /// </summary>
        /// <param name="commodityKind">Commodity kind.</param>
        /// <param name="interactionDefinitionType">Interaction definition type full name.</param>
        /// <param name="targetType">Target type full name.</param>
        /// <param name="constantChange">Advertised value.</param>
        /// <param name="locked">If set to <c>true</c>, locked.</param>
        /// <param name="actualValue">Actual value.</param>
        /// <param name="updateType">Update type.</param>
        /// <param name="timeDependsOnCommodityFilling">If set to <c>true</c>, time depends on commodity filling.</param>
        /// <param name="updateEvenOnFailure">If set to <c>true</c>, update even on failure.</param>
        /// <param name="updateAboveAndBelowZero">Update above and below zero.</param>
        public static void RemoveAsOutput(this CommodityKind commodityKind, string interactionDefinitionType, string targetType, float constantChange, bool locked, float actualValue, OutputUpdateType updateType, bool timeDependsOnCommodityFilling = false, bool updateEvenOnFailure = false, UpdateAboveAndBelowZeroType updateAboveAndBelowZero = UpdateAboveAndBelowZeroType.Either)
        {
            commodityKind.RemoveAsOutput(InteractionObjectTypeUtils.InteractionDefinitionTypes[interactionDefinitionType], InteractionObjectTypeUtils.GameObjectTypes[targetType], constantChange, locked, actualValue, updateType, timeDependsOnCommodityFilling, updateEvenOnFailure, updateAboveAndBelowZero);
        }

        /// <summary>
        /// Replaces a method (and its overloads) with another method.
        /// Note: only the overloads defined in the new type will replace the corresponding overloads of the old type.
        /// </summary>
        /// <typeparam name="OldType">The class that holds the method to be replaced.</typeparam>
        /// <typeparam name="NewType">The class that holds the new method. Note: only the overloads defined in the new type will replace the corresponding overloads of the old type.</typeparam>
        public static void ReplaceMethod<OldType, NewType>(string methodName)
        {
            ReplaceMethod<OldType, NewType>(methodName, methodName);
        }

        /// <summary>
        /// This method was borrowed from Lazy Duchess' Mono Patcher.
        /// </summary>
        public static void ReplaceMethod(MethodInfo oldMethod, MethodInfo newMethod)
        {
            var replacementByteArray = new byte[40];
            Marshal.Copy(newMethod.MethodHandle.Value, replacementByteArray, 0, 40);
            Marshal.Copy(replacementByteArray, 0, oldMethod.MethodHandle.Value, 24);
            Marshal.Copy(replacementByteArray, 28, new IntPtr(oldMethod.MethodHandle.Value.ToInt32() + 28), 12);
        }

        /// <summary>
        /// Replaces a method (and its overloads) with another method.
        /// Note: only the overloads defined in the new type will replace the corresponding overloads of the old type.
        /// </summary>
        /// <param name="oldMethodName">The name of the method to be replaced.</param>
        /// <param name="newMethodName">The name of the new method.</param>
        /// <typeparam name="OldType">The class that holds the method to be replaced.</typeparam>
        /// <typeparam name="NewType">The class that holds the new method. Note: only the overloads defined in the new type will replace the corresponding overloads of the old type.</typeparam>
        public static void ReplaceMethod<OldType, NewType>(string oldMethodName, string newMethodName)
        {
            foreach (var method in typeof(NewType).GetMethods((BindingFlags)0x3C))
            {
                if (method.Name == newMethodName)
                {
                    var oldMethod = typeof(OldType).GetMethod(oldMethodName, (BindingFlags)0x3C, null, Array.ConvertAll(method.GetParameters(), x => x.ParameterType), null);
                    if (oldMethod != null)
                    {
                        ReplaceMethod(oldMethod, method);
                    }
                }
            }
        }

        public static bool ShowCASAgeGenderFlagListDialog(out CASAgeGenderFlags flags, CASAgeGenderFlags? preSelectedFlags = null, CASAgeGenderFlags mask = CASAgeGenderFlags.AgeMask | CASAgeGenderFlags.GenderMask, string title = null, string entryKey = null, bool okayButtonAlwaysEnabled = true)
        {
            flags = CASAgeGenderFlags.None;
            entryKey = entryKey ?? typeof(ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "CASAgeGenderFlagListDialog");
            var flagList = new List<CASAgeGenderFlags>(Array.FindAll((CASAgeGenderFlags[])Enum.GetValues(typeof(CASAgeGenderFlags)), x => preSelectedFlags.HasValue ? (preSelectedFlags & mask & x) == x && x != CASAgeGenderFlags.None && x != CASAgeGenderFlags.AgeMask && x != CASAgeGenderFlags.GenderMask : false));
            while (true)
            {
                if (flagList.Count == 0 && okayButtonAlwaysEnabled)
                {
                    foreach (CASAgeGenderFlags flag in Enum.GetValues(typeof(CASAgeGenderFlags)))
                    {
                        if ((mask & flag) == flag && flag != CASAgeGenderFlags.None && flag != CASAgeGenderFlags.AgeMask && flag != CASAgeGenderFlags.GenderMask)
                        {
                            flagList.Add(flag);
                        }
                    }
                }
                var selectedFlags = ObjectPickerDialog.Show(title ?? Responder.Instance.LocalizationModel.LocalizeString(entryKey + ":Title"), new List<ObjectPicker.TabInfo>
                    {
                        new ObjectPicker.TabInfo("shop_all_r2", Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:All"), new List<CASAgeGenderFlags>(Array.FindAll((CASAgeGenderFlags[])Enum.GetValues(typeof(CASAgeGenderFlags)), x => (x & mask) == x && x != CASAgeGenderFlags.None && x != CASAgeGenderFlags.AgeMask && x != CASAgeGenderFlags.GenderMask)).ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                    }, okayButtonAlwaysEnabled ? new List<ObjectPickerDialog.CommonHeaderInfo<CASAgeGenderFlags>>
                    {
                        new CASAgeGenderFlagColumn(entryKey),
                        new CASAgeGenderFlagEnabledColumn(entryKey, flagList.ToArray())
                    } : new List<ObjectPickerDialog.CommonHeaderInfo<CASAgeGenderFlags>>
                    {
                        new CASAgeGenderFlagColumn(entryKey),
                    }, 1, out var confirmed, out var cancelled, okayButtonAlwaysEnabled);
                if (cancelled)
                {
                    return false;
                }
                if (confirmed)
                {
                    if (!okayButtonAlwaysEnabled)
                    {
                        flagList.Add(selectedFlags[0]);
                    }
                    foreach (var flag in flagList)
                    {
                        flags |= flag;
                    }
                    return true;
                }
                if (flagList.Contains(selectedFlags[0]))
                {
                    flagList.Remove(selectedFlags[0]);
                }
                else
                {
                    flagList.Add(selectedFlags[0]);
                }
            }
        }

        public static bool ShowCommodityKindListDialog(out CommodityKind[] selectedCommodityKinds, CommodityKind[] allCommodityKinds, CommodityKind[] preSelectedCommodityKinds = null, string title = null, string entryKey = null)
        {
            var allNames = new List<string>();
            var preSelectedNames = new List<string>();
            foreach (var entry in ParserFunctions.sCaseSensitiveEnumParsers[typeof(CommodityKind)].mLookup)
            {
                foreach (var motive in allCommodityKinds)
                {
                    if ((int)entry.Value == (int)motive)
                    {
                        allNames.Add(entry.Key);
                    }
                }
                foreach (var motive in preSelectedCommodityKinds ?? new CommodityKind[0])
                {
                    if ((int)entry.Value == (int)motive)
                    {
                        preSelectedNames.Add(entry.Key);
                    }
                }
            }
            entryKey = entryKey ?? typeof(ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "CommodityKindListDialog");
            var nameList = new List<string>(preSelectedNames);
            while (true)
            {
                var selectedNames = ObjectPickerDialog.Show(title ?? Responder.Instance.LocalizationModel.LocalizeString(entryKey + ":Title"), new List<ObjectPicker.TabInfo>
                    {
                        new ObjectPicker.TabInfo("shop_all_r2", Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:All"), allNames.ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                    }, new List<ObjectPickerDialog.CommonHeaderInfo<string>>
                    {
                        new TextColumn(entryKey, 400),
                        new TextInListColumn(entryKey, nameList.ToArray(), 40)
                    }, 1, out var confirmed, out var cancelled, true);
                if (cancelled)
                {
                    selectedCommodityKinds = null;
                    return false;
                }
                if (confirmed)
                {
                    selectedCommodityKinds = nameList.ConvertAll(x => (CommodityKind)ParserFunctions.sCaseSensitiveEnumParsers[typeof(CommodityKind)].mLookup[x]).ToArray();
                    return true;
                }
                if (nameList.Contains(selectedNames[0]))
                {
                    nameList.Remove(selectedNames[0]);
                }
                else
                {
                    nameList.Add(selectedNames[0]);
                }
            }
        }

        public static bool ShowSkillListDialog(CASAgeGenderFlags age, CASAgeGenderFlags species, List<SkillLevelPair> currentSkills, List<SkillLevelPair> allSkills = null, string title = null)
        {
            var ageSpecies = CASUtils.CASAGSAvailabilityFlagsFromCASAgeGenderFlags(age | species);
            if (allSkills == null)
            {
                allSkills = new List<SkillLevelPair>(currentSkills);
                foreach (var skill in SkillManager.SkillDictionary)
                {
                    if ((skill.AvailableAgeSpecies & ageSpecies) != 0 && !allSkills.Exists(x => x.SkillName == skill.Guid))
                    {
                        allSkills.Add(new SkillLevelPair(skill.Guid, 0));
                    }
                }
            }
            else
            {
                allSkills.RemoveAll(x => (SkillManager.GetStaticSkill(x.SkillName).AvailableAgeSpecies & ageSpecies) == 0);
            }
            var entryKey = typeof(ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "SkillListDialog");
            var skillList = new List<SkillLevelPair>(currentSkills);
            while (true)
            {
                var selectedSkills = ObjectPickerDialog.Show(title ?? Responder.Instance.LocalizationModel.LocalizeString(entryKey + ":Title"), new List<ObjectPicker.TabInfo>
                    {
                        new ObjectPicker.TabInfo("shop_all_r2", Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:All"), allSkills.ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                    }, new List<ObjectPickerDialog.CommonHeaderInfo<SkillLevelPair>>
                    {
                        new SkillColumn(entryKey),
                        new SkillLevelColumn(entryKey)
                    }, 1, out var confirmed, out var cancelled, true);
                if (cancelled)
                {
                    return false;
                }
                if (confirmed)
                {
                    currentSkills.Clear();
                    currentSkills.AddRange(skillList);
                    return true;
                }
                var skillLevel = StringInputDialog.Show(Localization.LocalizeString(entryKey + "/SubmenuTitles:SkillLevel"), Localization.LocalizeString(entryKey + "/SubmenuPrompts:SkillLevel"), selectedSkills[0].SkillLevel.ToString(), -1, ThumbnailKey.kInvalidThumbnailKey, new Vector2(-1f, -1f), StringInputDialog.Validation.Number, false, ModalDialog.PauseMode.PauseSimulator, false, true);
                selectedSkills[0].SkillLevel = skillLevel == null ? selectedSkills[0].SkillLevel : int.Parse(skillLevel);
                var skill = SkillManager.GetStaticSkill(selectedSkills[0].SkillName);
                if (selectedSkills[0].SkillLevel == -1)
                {
                    selectedSkills[0].SkillLevel = skill.MaxSkillLevel;
                }
                else if (selectedSkills[0].SkillLevel > skill.MaxSkillLevel)
                {
                    selectedSkills[0].SkillLevel = skill.MaxSkillLevel;
                }
                if (selectedSkills[0].SkillLevel == 0 && skillList.Contains(selectedSkills[0]))
                {
                    skillList.Remove(selectedSkills[0]);
                    continue;
                }
                if (selectedSkills[0].SkillLevel != 0 && !skillList.Contains(selectedSkills[0]))
                {
                    skillList.Add(selectedSkills[0]);
                }
            }
        }

        public static bool ShowTraitListDialog(CASAgeGenderFlags age, CASAgeGenderFlags gender, CASAgeGenderFlags species, List<Trait> currentTraits, List<Trait> allTraits = null, string title = null)
        {
            var ageSpecies = CASUtils.CASAGSAvailabilityFlagsFromCASAgeGenderFlags(age | species);
            if (allTraits == null)
            {
                allTraits = new List<Trait>();
                foreach (var trait in TraitManager.GetDictionaryTraits)
                {
                    if (trait.TraitValidForAgeSpecies(ageSpecies) && !trait.IsHidden && !trait.IsReward)
                    {
                        allTraits.Add(trait);
                    }
                }
            }
            else
            {
                allTraits.RemoveAll(x => !x.TraitValidForAgeSpecies(ageSpecies));
            }
            var entryKey = typeof(ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "TraitListDialog");
            var traitList = new List<Trait>(currentTraits);
            while (true)
            {
                var selectedTraits = ObjectPickerDialog.Show(title ?? Responder.Instance.LocalizationModel.LocalizeString(entryKey + ":Title"), new List<ObjectPicker.TabInfo>
                    {
                        new ObjectPicker.TabInfo("shop_all_r2", Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:All"), allTraits.ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                    }, new List<ObjectPickerDialog.CommonHeaderInfo<Trait>>
                    {
                        new TraitColumn(entryKey, gender == CASAgeGenderFlags.Female),
                        new TraitEnabledColumn(entryKey, traitList.ToArray())
                    }, 1, out var confirmed, out var cancelled, true);
                if (cancelled)
                {
                    return false;
                }
                if (confirmed)
                {
                    currentTraits.Clear();
                    currentTraits.AddRange(traitList);
                    return true;
                }
                if (traitList.Contains(selectedTraits[0]))
                {
                    traitList.Remove(selectedTraits[0]);
                }
                else
                {
                    traitList.Add(selectedTraits[0]);
                }
            }
        }

        public static bool TryChangeScriptClass(this GameObject gameObject, string newScriptClassName)
        {
            if (string.IsNullOrEmpty(newScriptClassName))
            {
                return false;
            }
            var resourceKey = gameObject.GetResourceKey();
            var newGameObject = GlobalFunctions.CreateObject(new ResourceKey(resourceKey.InstanceId, 0x319E4F1D, resourceKey.GroupId), gameObject.Position, gameObject.Level, gameObject.ForwardVector, newScriptClassName, null) as GameObject;
            if (newGameObject == null || newGameObject is FailureObject)
            {
                return false;
            }
            var enabledStencils = new SortedList<string, bool>();
            Complate.SetupDesignSwap(newGameObject.ObjectId, Complate.ExtractPatterns(gameObject.ObjectId, enabledStencils), false, enabledStencils)?.ApplyToObject();
            var slot = Slot.None;
            var parent = gameObject.Parent as GameObject;
            if (parent != null)
            {
                foreach (var tempSlot in parent.GetContainmentSlots())
                {
                    if (parent.GetContainedObject(tempSlot) == gameObject)
                    {
                        slot = tempSlot;
                        break;
                    }
                }
            }
            newGameObject.mPurchasedPrice = gameObject.mPurchasedPrice;
            newGameObject.mValueModifier = gameObject.mValueModifier;
            gameObject.Destroy();
            gameObject.Dispose();
            if (slot != Slot.None && !newGameObject.ParentToSlot(parent, slot))
            {
                GlobalFunctions.PlaceWithHandTool(newGameObject);
            }
            return true;
        }
        
        public static bool TryGetRandomItem<T>(this IEnumerable<T> enumerable, out T item) where T : class
        {
            var list = new List<T>(enumerable);
            return (item = list.Count == 0 ? null : list[RandomUtil.GetInt(list.Count - 1)]) != null;
        }

        public static bool TryGetRandomItem<T>(this IEnumerable<T> enumerable, out T? item) where T : struct
        {
            var list = new List<T>(enumerable);
            return (item = list.Count == 0 ? (T?)null : list[RandomUtil.GetInt(list.Count - 1)]).HasValue;
        }

        public static bool TryGetType(string fullName, out Type type)
        {
            type = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var tempType in assembly.GetTypes())
                {
                    if (tempType.FullName == fullName)
                    {
                        type = tempType;
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool TryUIGetActiveTopicActionActiveness(string title, out ActiveTopicActionActiveness activeTopicActionActiveness, ActiveTopicActionActiveness defaultValue = ActiveTopicActionActiveness.SPA)
        {
            var entryKey = typeof(ComboSelectionDialog).GetLocalizationKey().Replace("ComboSelectionDialog", "ActiveTopicActionActivenessDialog/Options:");
            if (ComboSelectionDialog.Show(title, new SortedDictionary<string, object>(new DummyComparer())
                {
                    {
                        Localization.LocalizeString(entryKey + ActiveTopicActionActiveness.FPA),
                        nameof(ActiveTopicActionActiveness.FPA)
                    },
                    {
                        Localization.LocalizeString(entryKey + ActiveTopicActionActiveness.SPA),
                        nameof(ActiveTopicActionActiveness.SPA)
                    }
                }, defaultValue.ToString()) is string text)
            {
                activeTopicActionActiveness = (ActiveTopicActionActiveness)Enum.Parse(typeof(ActiveTopicActionActiveness), text);
                return true;
            }
            activeTopicActionActiveness = defaultValue;
            return false;
        }

        public static bool TryUIGetBooleanValue(string title, out bool boolean, bool defaultValue = false)
        {
            if (ComboSelectionDialog.Show(title, new SortedDictionary<string, object>(new DummyComparer())
                {
                    {
                        Localization.LocalizeString(0xC83C121BA92C23BF),
                        true.ToString()
                    },
                    {
                        Localization.LocalizeString(0x918EEA8E85AB6760),
                        false.ToString()
                    }
                }, defaultValue.ToString()) is string text)
            {
                boolean = bool.Parse(text);
                return true;
            }
            boolean = false;
            return false;
        }

        public static bool TryUIGetLongTermRelationshipType(string title, CASAgeGenderFlags gender, out LongTermRelationshipTypes longTermRelationshipType, LongTermRelationshipTypes defaultValue = LongTermRelationshipTypes.Default)
        {
            var entryKey = typeof(ComboSelectionDialog).GetLocalizationKey().Replace("ComboSelectionDialog", "LongTermRelationshipTypeDialog/Options:");
            const string longTermRelationshipEntryKey = "Gameplay/Excel/Socializing/LTR:";
            if (ComboSelectionDialog.Show(title, new SortedDictionary<string, object>(new DummyComparer())
                {
                    {
                        Localization.LocalizeString(entryKey + LongTermRelationshipTypes.All),
                        nameof(LongTermRelationshipTypes.All)
                    },
                    {
                        Localization.LocalizeString(entryKey + LongTermRelationshipTypes.Default),
                        nameof(LongTermRelationshipTypes.Default)
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.Stranger),
                        nameof(LongTermRelationshipTypes.Stranger)
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.Acquaintance),
                        nameof(LongTermRelationshipTypes.Acquaintance)
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.DistantFriend),
                        nameof(LongTermRelationshipTypes.DistantFriend)
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.Friend),
                        nameof(LongTermRelationshipTypes.Friend)
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.GoodFriend),
                        nameof(LongTermRelationshipTypes.GoodFriend)
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.BestFriend),
                        nameof(LongTermRelationshipTypes.BestFriend)
                    },
                    {
                        Localization.LocalizeString(0xC86B0C71108DA632),
                        nameof(LongTermRelationshipTypes.BestFriendsForever)
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.OldFriend),
                        nameof(LongTermRelationshipTypes.OldFriend)
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.RomanticInterest),
                        nameof(LongTermRelationshipTypes.RomanticInterest)
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.Partner),
                        nameof(LongTermRelationshipTypes.Partner)
                    },
                    {
                        Localization.LocalizeString((gender & CASAgeGenderFlags.Male) == 0 ? 0x560E2FCA95B005B2 : 0xA85D4CDDDC0663AE),
                        nameof(LongTermRelationshipTypes.Fiancee)
                    },
                    {
                        Localization.LocalizeString(entryKey + LongTermRelationshipTypes.Spouse),
                        nameof(LongTermRelationshipTypes.Spouse)
                    },
                    {
                        Localization.LocalizeString(entryKey + LongTermRelationshipTypes.Ex),
                        nameof(LongTermRelationshipTypes.Ex)
                    },
                    {
                        Localization.LocalizeString(entryKey + LongTermRelationshipTypes.ExSpouse),
                        nameof(LongTermRelationshipTypes.ExSpouse)
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.Disliked),
                        nameof(LongTermRelationshipTypes.Disliked)
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.Enemy),
                        nameof(LongTermRelationshipTypes.Enemy)
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.OldEnemies),
                        nameof(LongTermRelationshipTypes.OldEnemies)
                    }
                }, defaultValue.ToString()) is string text)
            {
                
                longTermRelationshipType = (LongTermRelationshipTypes)Enum.Parse(typeof(LongTermRelationshipTypes), text);
                return true;
            }
            longTermRelationshipType = defaultValue;
            return false;
        }

        public static bool TryUIGetSelectedTypes(out Type[] selectedTypes, Type[] allTypes, string namespaceListTitle = null, string typeListTitle = null, int selectableRowCount = int.MaxValue)
        {
            var entryKey = typeof(ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "");
            Array.Sort(allTypes, (a, b) => string.Compare(a.FullName, b.FullName, StringComparison.Ordinal));
            var namespaces = new List<string>();
            foreach (var type in allTypes)
            {
                if (!namespaces.Contains(type.Namespace))
                {
                    namespaces.Add(type.Namespace);
                }
            }
            while (true)
            {
                var selectedNamespaces = ObjectPickerDialog.Show(namespaceListTitle ?? Responder.Instance.LocalizationModel.LocalizeString(entryKey + "NamespaceListDialog:Title"), new List<ObjectPicker.TabInfo>
                    {
                        new ObjectPicker.TabInfo("shop_all_r2", Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:All"), namespaces.ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                    }, new List<ObjectPickerDialog.CommonHeaderInfo<string>>
                    {
                        new TextColumn(entryKey + "NamespaceListDialog")
                    }, 1, out var confirmed, out var cancelled);
                if (cancelled)
                {
                    selectedTypes = null;
                    return false;
                }
                selectedTypes = (ObjectPickerDialog.Show(typeListTitle ?? Responder.Instance.LocalizationModel.LocalizeString(entryKey + "TypeListDialog:Title"), new List<ObjectPicker.TabInfo>
                    {
                        new ObjectPicker.TabInfo("shop_all_r2", selectedNamespaces[0], new List<Type>(allTypes).FindAll(x => x.Namespace == selectedNamespaces[0]).ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                    }, new List<ObjectPickerDialog.CommonHeaderInfo<Type>>
                    {
                        new TypeColumn(entryKey + "TypeListDialog")
                    }, selectableRowCount, out confirmed, out cancelled) ?? new List<Type>()).ToArray();
                if (confirmed)
                {
                    return true;
                }
            }
        }

        public static bool TryUIGetUpdateType(string title, out OutputUpdateType updateType, OutputUpdateType defaultValue = OutputUpdateType.ContinuousFlow)
        {
            var entryKey = typeof(ComboSelectionDialog).GetLocalizationKey().Replace("ComboSelectionDialog", "UpdateTypeDialog/Options:");
            if (ComboSelectionDialog.Show(title, new SortedDictionary<string, object>(new DummyComparer())
                {
                    {
                        Localization.LocalizeString(entryKey + OutputUpdateType.ContinuousFlow),
                        OutputUpdateType.ContinuousFlow.ToString()
                    },
                    {
                        Localization.LocalizeString(entryKey + OutputUpdateType.ImmediateDelta),
                        nameof(OutputUpdateType.ImmediateDelta)
                    }
                }, defaultValue.ToString()) is string text)
            {
                updateType = (OutputUpdateType)Enum.Parse(typeof(OutputUpdateType), text);
                return true;
            }
            updateType = defaultValue;
            return false;
        }

        public static void UpdateMotiveTunings(Sim sim, CommodityKind commodityKind)
        {
            var allTunings = MotiveTuning.GetAllTunings(commodityKind);
            if (allTunings == null)
            {
                return;
            }
            MotiveTuning motiveTuning = null;
            var score = float.MinValue;
            foreach (var tuning in allTunings)
            {
                var tempScore = sim.ScoreMotiveTuning(tuning);
                if (tempScore > score)
                {
                    score = tempScore;
                    motiveTuning = tuning;
                }
            }
            if (motiveTuning != null)
            {
                sim.mMotiveTuning[(int)commodityKind] = motiveTuning;
            }
        }
    }
}
