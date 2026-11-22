using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Destrospean.Misc;
using Sims3.Gameplay.Interfaces;
using Sims3.Metadata;
using Sims3.SimIFace;

namespace Destrospean.Utils
{
    public static class PresetUtils
    {
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
        
        public static void ApplyPreset(this IGameObject gameObject, string presetXml)
        {
            var enabledStencils = new SortedList<string, bool>(StringComparer.Ordinal);
            Complate.SetupDesignSwap(gameObject.ObjectId, ExtractPatterns(presetXml, enabledStencils), false, enabledStencils)?.ApplyToObject();
        }
        
        public static SortedList<string, Complate> ExtractPatterns(string presetXml, SortedList<string, bool> enabledStencils)
        {
            if (string.IsNullOrEmpty(presetXml))
            {
                return new SortedList<string, Complate>(StringComparer.Ordinal);
            }
            var patternComplates = new Dictionary<string, Complate>();
            var processedPreset = Complate.ProcessPreset(presetXml, patternComplates);
            if (string.IsNullOrEmpty(processedPreset))
            {
                return new SortedList<string, Complate>(StringComparer.Ordinal);
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
            return new SortedList<string, Complate>(patternComplates, StringComparer.Ordinal);
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
    }
}
