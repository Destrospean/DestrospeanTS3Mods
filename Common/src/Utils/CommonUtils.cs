using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.Utilities;
using Sims3.Metadata;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using Sims3.UI.Controller;
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
using ComboSelectionDialog = Destrospean.UI.Dialogs.ComboSelectionDialog;
using ObjectPickerDialog = Destrospean.UI.Dialogs.ObjectPickerDialog;

namespace Destrospean.Utils
{
    public static class CommonUtils
    {
        const string kAuthorName = "Destrospean";

        static bool ChangePreset(DesignModeSwap swap, SortedList<string, Complate> patterns, ref string presetXml, bool undoable, SortedList<string, bool> enabledStencils)
        {
            if (string.IsNullOrEmpty(presetXml))
            {
                return false;
            }
            Dictionary<string, Complate> patternComplates = new Dictionary<string, Complate>();
            string processedPreset = Complate.ProcessPreset(presetXml, patternComplates);
            if (string.IsNullOrEmpty(processedPreset))
            {
                return false;
            }
            Complate presetComplate = null;
            if (!patternComplates.TryGetValue(processedPreset, out presetComplate))
            {
                return false;
            }
            List<Complate.Variable> variables = new List<Complate.Variable>();
            bool result = false;
            foreach (Complate.Variable variable in presetComplate.Variables)
            {
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
                    string enabled = variable[Complate.kEnabledAttribute];
                    if (enabled != "")
                    {
                        enabled = presetComplate[enabled];
                        result = false;
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
            foreach (Complate.TexturePart texturePart in presetComplate.Parts)
            {
                foreach (Complate.TextureDestination textureDestination in texturePart.Destinations)
                {
                    foreach (Complate.TextureStep textureStep in textureDestination.Steps)
                    {
                        if (textureStep[Complate.kTypeAttribute] == "DrawFabric")
                        {
                            string variableName = textureStep["pattern"];
                            if (variableName.StartsWith("($daeFileName)"))
                            {
                                variableName = variableName.Remove(0, 14);
                            }
                            textureStep["patternkey"] = swap.GetPatternKey(variableName).ToString();
                        }
                    }
                }
            }
            foreach (KeyValuePair<string, Complate> pattern in patterns)
            {
                if (patternComplates.ContainsKey(pattern.Key))
                {
                    patternComplates[pattern.Key] = pattern.Value;
                }
            }
            for (int i = 0; i < variables.Count; i++)
            {
                Complate pattern;
                if (!patternComplates.TryGetValue(variables[i].Name, out pattern))
                {
                    swap.Dispose();
                    return false;
                }
                pattern.Process();
                TextureCompositor textureCompositor = pattern.CreateTextureCompositor(null, null, 0u);
                if (textureCompositor != null)
                {
                    swap.SetNewPattern(variables[i].Name, textureCompositor.ExportData(null, null, null));
                }
            }
            presetComplate.Process();
            Hashtable hashtable = presetComplate.CreateTextureCompositors(0u);
            foreach (DictionaryEntry entry in hashtable)
            {
                string key = ((string)entry.Key).ToLower();
                if (key.EndsWith("diffusemap"))
                {
                    swap.SetNewCompositor("diffusemap", ((TextureCompositor)entry.Value).ExportData(null, null, null));
                }
                else if (key.EndsWith("specmap"))
                {
                    swap.SetNewCompositor("specularmap", ((TextureCompositor)entry.Value).ExportData(null, null, null));
                }
            }
            XmlDocument xmlDocument = new XmlDocument();
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
            Dictionary<LongTermRelationshipTypes, Dictionary<bool, List<string>>> groups;
            if (!ActionAvailabilityData.sActiveTopicInteractions.TryGetValue(activeTopic, out groups))
            {
                groups = new Dictionary<LongTermRelationshipTypes, Dictionary<bool, List<string>>>();
                ActionAvailabilityData.sActiveTopicInteractions.Add(activeTopic, groups);
            }
            Dictionary<bool, List<string>> group;
            if (!groups.TryGetValue(grouping, out group))
            {
                group = new Dictionary<bool, List<string>>();
                groups.Add(grouping, group);
            }
            List<string> actions;
            if (!group.TryGetValue(isActive, out actions))
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
            List<CommodityChange> outputs = AutonomyTuning.GetTuning(interactionDefinitionType.FullName, targetType)?.mTradeoff?.mOutputs;
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
            Type enumType = typeof(T);
            EnumParser caseInsensitiveEnumParser;
            if (!ParserFunctions.sCaseInsensitiveEnumParsers.TryGetValue(enumType, out caseInsensitiveEnumParser))
            {
                caseInsensitiveEnumParser = new EnumParser(enumType, true);
                ParserFunctions.sCaseInsensitiveEnumParsers.Add(enumType, caseInsensitiveEnumParser);
            }
            EnumParser caseSensitiveEnumParser;
            if (!ParserFunctions.sCaseSensitiveEnumParsers.TryGetValue(enumType, out caseSensitiveEnumParser))
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
            SortedList<string, bool> enabledStencils = new SortedList<string, bool>();
            Complate.SetupDesignSwap(gameObject.ObjectId, ExtractPatterns(presetXml, enabledStencils), false, enabledStencils)?.ApplyToObject();
        }

        public static void CopyTuning(Type baseType, Type oldType, Type newType)
        {
            if (AutonomyTuning.GetTuning(newType.FullName, baseType.FullName) == null)
            {
                InteractionTuning tuning = AutonomyTuning.GetTuning(oldType, oldType.FullName, baseType);
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
            Dictionary<string, Complate> patternComplates = new Dictionary<string, Complate>();
            string processedPreset = Complate.ProcessPreset(presetXml, patternComplates);
            if (string.IsNullOrEmpty(processedPreset))
            {
                return new SortedList<string, Complate>();
            }
            Complate patternComplate;
            if (enabledStencils != null && patternComplates.TryGetValue(processedPreset, out patternComplate))
            {
                foreach (Complate.Variable variable in patternComplate.Variables)
                {
                    if (variable.Type == Complate.Variable.Types.Bool)
                    {
                        string variableName = variable.Name.ToLower();
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
            uint retVal = ResourceUtils.HashString32(name);
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
            string preset = ObjectDesigner.GetObjectDesignPreset(gameObject.ObjectId);
            if (string.IsNullOrEmpty(preset))
            {
                return null;
            }
            DesignModeSwap designModeSwap = new DesignModeSwap();
            designModeSwap.SetSourceObject(gameObject.ObjectId);
            SortedList<string, bool> enabledStencils = new SortedList<string, bool>();
            if (ChangePreset(designModeSwap, Complate.ExtractPatterns(gameObject.ObjectId, enabledStencils), ref preset, false, enabledStencils))
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.LoadXml(preset);
                XmlDeclaration xmlDeclaration = xmlDocument.CreateXmlDeclaration("1.0", "utf-8", null);
                xmlDocument.InsertBefore(xmlDeclaration, xmlDocument.DocumentElement);
                StringBuilder stringBuilder = new StringBuilder();
                Utf8StringWriter stringWriter = new Utf8StringWriter(stringBuilder);
                XmlWriter xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
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
            return type.Namespace.Substring(type.Namespace.IndexOf(kAuthorName)).Replace('.', '/') + "/" + type.Name;
        }

        public static void LoadMotive(string instanceName)
        {
            MotiveTuning.LoadTuning(Simulator.LoadXML(instanceName));
        }

        public static void LoadSocialData(string instanceName)
        {
            XmlDocument xmlDocument = Simulator.LoadXML(instanceName);
            bool isEp5Installed = GameUtils.IsInstalled(ProductVersion.EP5);
            if (instanceName != null)
            {
                foreach (XmlElement element in new XmlElementLookup(xmlDocument)["Action"])
                {
                    CommodityTypes commodityTypes;
                    ParserFunctions.TryParseEnum<CommodityTypes>(element.GetAttribute("com"), out commodityTypes, CommodityTypes.Undefined);
                    ActionData.Add(new ActionData(element.GetAttribute("key"), commodityTypes, ProductVersion.BaseGame, new XmlElementLookup(element), isEp5Installed));
                }
            }
        }

        public static void LoadSocializingActionAvailability(string instanceName)
        {
            XmlDbData data = XmlDbData.ReadData(instanceName);
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
            foreach (GameObject gameObject in Sims3.Gameplay.Queries.GetObjects<GameObject>())
            {
                if (targetType.IsAssignableFrom(gameObject.GetType()))
                {
                    foreach (InteractionObjectPair interaction in new List<InteractionObjectPair>(gameObject.Interactions))
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
            foreach (Sim sim in Sims3.Gameplay.Queries.GetObjects<Sim>())
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
            List<string> actions;
            Dictionary<bool, List<string>> group;
            Dictionary<LongTermRelationshipTypes, Dictionary<bool, List<string>>> groups;
            if (ActionAvailabilityData.sActiveTopicInteractions.TryGetValue(activeTopic, out groups) && groups.TryGetValue(grouping, out group) && group.TryGetValue(isActive, out actions))
            {
                actions.RemoveAll(x => Array.Exists(actionsToRemove, y => y == x));
            }
        }

        public static void RemoveEnumValue<T>(string key) where T : struct
        {
            Type enumType = typeof(T);
            EnumParser caseInsensitiveEnumParser;
            if (ParserFunctions.sCaseInsensitiveEnumParsers.TryGetValue(enumType, out caseInsensitiveEnumParser) && caseInsensitiveEnumParser.mLookup.ContainsKey(key.ToLowerInvariant()))
            {
                caseInsensitiveEnumParser.mLookup.Remove(key.ToLowerInvariant());
            }
            EnumParser caseSensitiveEnumParser;
            if (ParserFunctions.sCaseSensitiveEnumParsers.TryGetValue(enumType, out caseSensitiveEnumParser) && caseSensitiveEnumParser.mLookup.ContainsKey(key))
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
            List<CommodityChange> outputs = AutonomyTuning.GetTuning(interactionDefinitionType.FullName, targetType)?.mTradeoff?.mOutputs ?? new List<CommodityChange>();
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
            byte[] replacementByteArray = new byte[40];
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
            foreach (MethodInfo method in typeof(NewType).GetMethods((BindingFlags)0x3C))
            {
                if (method.Name == newMethodName)
                {
                    MethodInfo oldMethod = typeof(OldType).GetMethod(oldMethodName, (BindingFlags)0x3C, null, Array.ConvertAll(method.GetParameters(), x => x.ParameterType), null);
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
            List<CASAgeGenderFlags> flagList = new List<CASAgeGenderFlags>(Array.FindAll((CASAgeGenderFlags[])Enum.GetValues(typeof(CASAgeGenderFlags)), x => preSelectedFlags.HasValue ? (preSelectedFlags & mask & x) == x && x != CASAgeGenderFlags.None && x != CASAgeGenderFlags.AgeMask && x != CASAgeGenderFlags.GenderMask : false));
            bool cancelled, confirmed;
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
                List<CASAgeGenderFlags> selectedFlags = ObjectPickerDialog.Show(title ?? Responder.Instance.LocalizationModel.LocalizeString(entryKey + ":Title"), new List<ObjectPicker.TabInfo>
                    {
                        new ObjectPicker.TabInfo("shop_all_r2", Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:All"), new List<CASAgeGenderFlags>(Array.FindAll((CASAgeGenderFlags[])Enum.GetValues(typeof(CASAgeGenderFlags)), x => (x & mask) == x && x != CASAgeGenderFlags.None && x != CASAgeGenderFlags.AgeMask && x != CASAgeGenderFlags.GenderMask)).ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                    }, okayButtonAlwaysEnabled ? new List<ObjectPickerDialog.CommonHeaderInfo<CASAgeGenderFlags>>
                    {
                        new CASAgeGenderFlagColumn(entryKey),
                        new CASAgeGenderFlagEnabledColumn(entryKey, flagList.ToArray())
                    } : new List<ObjectPickerDialog.CommonHeaderInfo<CASAgeGenderFlags>>
                    {
                        new CASAgeGenderFlagColumn(entryKey),
                    }, 1, out confirmed, out cancelled, okayButtonAlwaysEnabled);
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
                    foreach (CASAgeGenderFlags flag in flagList)
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
            List<string> allNames = new List<string>();
            List<string> preSelectedNames = new List<string>();
            foreach (KeyValuePair<string, object> entry in ParserFunctions.sCaseSensitiveEnumParsers[typeof(CommodityKind)].mLookup)
            {
                foreach (CommodityKind motive in allCommodityKinds)
                {
                    if ((int)entry.Value == (int)motive)
                    {
                        allNames.Add(entry.Key);
                    }
                }
                foreach (CommodityKind motive in preSelectedCommodityKinds)
                {
                    if ((int)entry.Value == (int)motive)
                    {
                        preSelectedNames.Add(entry.Key);
                    }
                }
            }
            entryKey = entryKey ?? typeof(ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "CommodityKindListDialog");
            List<string> nameList = new List<string>(preSelectedNames);
            bool cancelled, confirmed;
            while (true)
            {
                List<string> selectedNames = ObjectPickerDialog.Show(title ?? Responder.Instance.LocalizationModel.LocalizeString(entryKey + ":Title"), new List<ObjectPicker.TabInfo>
                    {
                        new ObjectPicker.TabInfo("shop_all_r2", Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:All"), allNames.ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                    }, new List<ObjectPickerDialog.CommonHeaderInfo<string>>
                    {
                        new TextColumn(entryKey, 400),
                        new TextInListColumn(entryKey, nameList.ToArray(), 40)
                    }, 1, out confirmed, out cancelled, true);
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
            CASAGSAvailabilityFlags ageSpecies = CASUtils.CASAGSAvailabilityFlagsFromCASAgeGenderFlags(age | species);
            if (allSkills == null)
            {
                allSkills = new List<SkillLevelPair>(currentSkills);
                foreach (Skill skill in SkillManager.SkillDictionary)
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
            string entryKey = typeof(ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "SkillListDialog");
            List<SkillLevelPair> skillList = new List<SkillLevelPair>(currentSkills);
            bool cancelled, confirmed;
            while (true)
            {
                List<SkillLevelPair> selectedSkills = ObjectPickerDialog.Show(title ?? Responder.Instance.LocalizationModel.LocalizeString(entryKey + ":Title"), new List<ObjectPicker.TabInfo>
                    {
                        new ObjectPicker.TabInfo("shop_all_r2", Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:All"), allSkills.ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                    }, new List<ObjectPickerDialog.CommonHeaderInfo<SkillLevelPair>>
                    {
                        new SkillColumn(entryKey),
                        new SkillLevelColumn(entryKey, skillList.ToArray())
                    }, 1, out confirmed, out cancelled, true);
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
                string skillLevel = StringInputDialog.Show(Localization.LocalizeString(entryKey + "/SubmenuTitles:SkillLevel"), Localization.LocalizeString(entryKey + "/SubmenuPrompts:SkillLevel"), selectedSkills[0].SkillLevel.ToString(), -1, ThumbnailKey.kInvalidThumbnailKey, new Vector2(-1f, -1f), StringInputDialog.Validation.Number, false, ModalDialog.PauseMode.PauseSimulator, false, true);
                selectedSkills[0].SkillLevel = skillLevel == null ? selectedSkills[0].SkillLevel : int.Parse(skillLevel);
                Skill skill = SkillManager.GetStaticSkill(selectedSkills[0].SkillName);
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
            CASAGSAvailabilityFlags ageSpecies = CASUtils.CASAGSAvailabilityFlagsFromCASAgeGenderFlags(age | species);
            if (allTraits == null)
            {
                allTraits = new List<Trait>();
                foreach (Trait trait in TraitManager.GetDictionaryTraits)
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
            string entryKey = typeof(ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "TraitListDialog");
            List<Trait> traitList = new List<Trait>(currentTraits);
            bool cancelled, confirmed;
            while (true)
            {
                List<Trait> selectedTraits = ObjectPickerDialog.Show(title ?? Responder.Instance.LocalizationModel.LocalizeString(entryKey + ":Title"), new List<ObjectPicker.TabInfo>
                    {
                        new ObjectPicker.TabInfo("shop_all_r2", Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:All"), allTraits.ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                    }, new List<ObjectPickerDialog.CommonHeaderInfo<Trait>>
                    {
                        new TraitColumn(entryKey, gender == CASAgeGenderFlags.Female),
                        new TraitEnabledColumn(entryKey, traitList.ToArray())
                    }, 1, out confirmed, out cancelled, true);
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

        public static bool TryGetType(string fullName, out Type type)
        {
            type = null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type tempType in assembly.GetTypes())
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
            string entryKey = typeof(ComboSelectionDialog).GetLocalizationKey().Replace("ComboSelectionDialog", "ActiveTopicActionActivenessDialog/Options:");
            string text = ComboSelectionDialog.Show(title, new SortedDictionary<string, object>(new DummyComparer())
                {
                    {
                        Localization.LocalizeString(entryKey + ActiveTopicActionActiveness.FPA),
                        ActiveTopicActionActiveness.FPA.ToString()
                    },
                    {
                        Localization.LocalizeString(entryKey + ActiveTopicActionActiveness.SPA),
                        ActiveTopicActionActiveness.SPA.ToString()
                    }
                }, defaultValue.ToString()) as string;
            if (text == null)
            {
                activeTopicActionActiveness = 0;
                return false;
            }
            activeTopicActionActiveness = (ActiveTopicActionActiveness)Enum.Parse(typeof(ActiveTopicActionActiveness), text);
            return true;
        }

        public static bool TryUIGetBooleanValue(string title, out bool boolean, bool defaultValue = false)
        {
            string text = ComboSelectionDialog.Show(title, new SortedDictionary<string, object>(new DummyComparer())
                {
                    {
                        Localization.LocalizeString(0xC83C121BA92C23BF),
                        true.ToString()
                    },
                    {
                        Localization.LocalizeString(0x918EEA8E85AB6760),
                        false.ToString()
                    }
                }, defaultValue.ToString()) as string;
            if (text == null)
            {
                boolean = false;
                return false;
            }
            boolean = bool.Parse(text);
            return true;
        }

        public static bool TryUIGetLongTermRelationshipType(string title, CASAgeGenderFlags gender, out LongTermRelationshipTypes longTermRelationshipType, LongTermRelationshipTypes defaultValue = LongTermRelationshipTypes.Default)
        {
            string entryKey = typeof(ComboSelectionDialog).GetLocalizationKey().Replace("ComboSelectionDialog", "LongTermRelationshipTypeDialog/Options:");
            string longTermRelationshipEntryKey = "Gameplay/Excel/Socializing/LTR:";
            string text = ComboSelectionDialog.Show(title, new SortedDictionary<string, object>(new DummyComparer())
                {
                    {
                        Localization.LocalizeString(entryKey + LongTermRelationshipTypes.All),
                        LongTermRelationshipTypes.All.ToString()
                    },
                    {
                        Localization.LocalizeString(entryKey + LongTermRelationshipTypes.Default),
                        LongTermRelationshipTypes.Default.ToString()
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.Stranger),
                        LongTermRelationshipTypes.Stranger.ToString()
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.Acquaintance),
                        LongTermRelationshipTypes.Acquaintance.ToString()
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.DistantFriend),
                        LongTermRelationshipTypes.DistantFriend.ToString()
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.Friend),
                        LongTermRelationshipTypes.Friend.ToString()
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.GoodFriend),
                        LongTermRelationshipTypes.GoodFriend.ToString()
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.BestFriend),
                        LongTermRelationshipTypes.BestFriend.ToString()
                    },
                    {
                        Localization.LocalizeString(0xC86B0C71108DA632),
                        LongTermRelationshipTypes.BestFriendsForever.ToString()
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.OldFriend),
                        LongTermRelationshipTypes.OldFriend.ToString()
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.RomanticInterest),
                        LongTermRelationshipTypes.RomanticInterest.ToString()
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.Partner),
                        LongTermRelationshipTypes.Partner.ToString()
                    },
                    {
                        Localization.LocalizeString((gender & CASAgeGenderFlags.Male) == 0 ? 0x560E2FCA95B005B2 : 0xA85D4CDDDC0663AE),
                        LongTermRelationshipTypes.Fiancee.ToString()
                    },
                    {
                        Localization.LocalizeString(entryKey + LongTermRelationshipTypes.Spouse),
                        LongTermRelationshipTypes.Spouse.ToString()
                    },
                    {
                        Localization.LocalizeString(entryKey + LongTermRelationshipTypes.Ex),
                        LongTermRelationshipTypes.Ex.ToString()
                    },
                    {
                        Localization.LocalizeString(entryKey + LongTermRelationshipTypes.ExSpouse),
                        LongTermRelationshipTypes.ExSpouse.ToString()
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.Disliked),
                        LongTermRelationshipTypes.Disliked.ToString()
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.Enemy),
                        LongTermRelationshipTypes.Enemy.ToString()
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.OldEnemies),
                        LongTermRelationshipTypes.OldEnemies.ToString()
                    }
                }, defaultValue.ToString()) as string;
            if (text == null)
            {
                longTermRelationshipType = 0;
                return false;
            }
            longTermRelationshipType = (LongTermRelationshipTypes)Enum.Parse(typeof(LongTermRelationshipTypes), text);
            return true;
        }

        public static bool TryUIGetSelectedTypes(out Type[] selectedTypes, Type[] allTypes, string namespaceListTitle = null, string typeListTitle = null, int selectableRowCount = int.MaxValue)
        {
            string entryKey = typeof(ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "");
            Array.Sort(allTypes, (a, b) => a.FullName.CompareTo(b.FullName));
            List<string> namespaces = new List<string>();
            foreach (Type type in allTypes)
            {
                if (!namespaces.Contains(type.Namespace))
                {
                    namespaces.Add(type.Namespace);
                }
            }
            bool cancelled, confirmed;
            while (true)
            {
                List<string> selectedNamespaces = ObjectPickerDialog.Show(namespaceListTitle ?? Responder.Instance.LocalizationModel.LocalizeString(entryKey + "NamespaceListDialog:Title"), new List<ObjectPicker.TabInfo>
                    {
                        new ObjectPicker.TabInfo("shop_all_r2", Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:All"), namespaces.ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                    }, new List<ObjectPickerDialog.CommonHeaderInfo<string>>
                    {
                        new TextColumn(entryKey + "NamespaceListDialog")
                    }, 1, out confirmed, out cancelled);
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

        public static bool TryUIGetUpdateType(string title, out OutputUpdateType updateType)
        {
            string entryKey = typeof(ComboSelectionDialog).GetLocalizationKey().Replace("ComboSelectionDialog", "UpdateTypeDialog/Options:");
            string text = ComboSelectionDialog.Show(title, new SortedDictionary<string, object>(new DummyComparer())
                {
                    {
                        Localization.LocalizeString(entryKey + OutputUpdateType.ContinuousFlow),
                        OutputUpdateType.ContinuousFlow.ToString()
                    },
                    {
                        Localization.LocalizeString(entryKey + OutputUpdateType.ImmediateDelta),
                        OutputUpdateType.ImmediateDelta.ToString()
                    }
                }, OutputUpdateType.ContinuousFlow.ToString()) as string;
            if (text == null)
            {
                updateType = 0;
                return false;
            }
            updateType = (OutputUpdateType)Enum.Parse(typeof(OutputUpdateType), text);
            return true;
        }

        public static void UpdateMotiveTunings(Sim sim, CommodityKind commodityKind)
        {
            IEnumerable<MotiveTuning> allTunings = MotiveTuning.GetAllTunings(commodityKind);
            if (allTunings == null)
            {
                return;
            }
            MotiveTuning motiveTuning = null;
            float score = float.MinValue;
            foreach (MotiveTuning tuning in allTunings)
            {
                float tempScore = sim.ScoreMotiveTuning(tuning);
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
