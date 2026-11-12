using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Destrospean.Enums.HairTrouble;
using Destrospean.HairTrouble;
using Destrospean.Misc;
using Sims3.Gameplay.Utilities;

namespace Destrospean.Utils.HairTrouble
{
    public class FileUtils : FileUtilsBase<IDictionary<string, HairGrowthStates>>
    {
        private const string kLocalizationKey = "Destrospean/UI/Dialogs";

        private const string kSavedSettingsPrefix = "Destrospean.HairTrouble.";
        
        public static FileUtils Singleton = new FileUtils();

        public override void DeleteFile()
        {
            DeleteFile(kSavedSettingsPrefix, kLocalizationKey, Localization.LocalizeString(Interactions.DeleteHairClassificationCollection.LocalizationKey + ":Name"), Localization.LocalizeString(kLocalizationKey + "/DeleteHairClassificationCollectionSuccessDialog:Title"), Localization.LocalizeString(kLocalizationKey + "/DeleteHairClassificationCollectionSuccessDialog:Message"), Localization.LocalizeString(kLocalizationKey + "/NoHairClassificationCollectionsExistDialog:Title"), Localization.LocalizeString(kLocalizationKey + "/NoHairClassificationCollectionsExistDialog:Message"));
        }

        public override bool ExportToFile(string text, out bool found)
        {
            return ExportToFile(kSavedSettingsPrefix, text, Localization.LocalizeString(kLocalizationKey + "/ExportHairClassificationCollectionFilenameDialog:Title"), Localization.LocalizeString(kLocalizationKey + "/ExportHairClassificationCollectionFilenameDialog:Prompt"), Localization.LocalizeString(kLocalizationKey + "/ExportHairClassificationCollectionSuccessDialog:Title"), Localization.LocalizeString(kLocalizationKey + "/ExportHairClassificationCollectionSuccessDialog:Message"), Localization.LocalizeString(kLocalizationKey + "/HairClassificationCollectionExistsDialog:Title"), Localization.LocalizeString(kLocalizationKey + "/HairClassificationCollectionExistsDialog:Message"), out found);
        }

        public override string ExtractFromFile()
        {
            return ExtractFromFile(kSavedSettingsPrefix, kLocalizationKey, Localization.LocalizeString(Interactions.ImportHairClassificationCollection.LocalizationKey + ":Name"), Localization.LocalizeString(kLocalizationKey + "/NoHairClassificationCollectionsExistDialog:Title"), Localization.LocalizeString(kLocalizationKey + "/NoHairClassificationCollectionsExistDialog:Message"));
        }
        
        public override bool ImportFromFile()
        {
            return ImportFromFile(Localization.LocalizeString(kLocalizationKey + "/ImportHairClassificationCollectionSuccessDialog:Title"), Localization.LocalizeString(kLocalizationKey + "/ImportHairClassificationCollectionSuccessDialog:Message"));
        }

        public override bool ImportFromTuning(string name)
        {
            return ImportFromTuning(name, Localization.LocalizeString(kLocalizationKey + "/ImportHairClassificationCollectionSuccessDialog:Title"), Localization.LocalizeString(kLocalizationKey + "/ImportHairClassificationCollectionSuccessDialog:Message"));
        }
        
        public override void LoadXml(string text)
        {
            using (var stringReader = new StringReader(text))
            {
                using (var xmlReader = XmlReader.Create(stringReader, new XmlReaderSettings
                   {
                       IgnoreComments = true
                   }))
                {
                    while (xmlReader.Read())
                    {
                        if (xmlReader.NodeType == XmlNodeType.Element)
                        {
                            if (xmlReader.Name == "HairTroubleData")
                            {
                                xmlReader.MoveToContent();
                            }
                            else if (xmlReader.Name == "HairClassification")
                            {
                                var key = xmlReader.GetAttribute("partKey");
                                if (!string.IsNullOrEmpty(key) && !SimHairGrowth.HairGrowthStateMap.ContainsKey(key))
                                {
                                    try
                                    {
                                        SimHairGrowth.HairGrowthStateMap[key] = (HairGrowthStates)Enum.Parse(typeof(HairGrowthStates), xmlReader.GetAttribute("growthState") ?? "");
                                    }
                                    catch (ArgumentException)
                                    {
                                    }
                                }
                            }
                        }
                    }
                    xmlReader.Close();
                }
            }
        }
        
        public override string ToXml(IDictionary<string, HairGrowthStates> hairGrowthStateMap)
        {
            var stringBuilder = new StringBuilder();
            using (var stringWriter = new Utf8StringWriter(stringBuilder))
            {
                using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
                   {
                       Encoding = Encoding.UTF8
                   }))
                {
                    xmlWriter.WriteStartDocument();
                    xmlWriter.WriteStartElement("HairTroubleData");
                    foreach (var entry in hairGrowthStateMap)
                    {
                        xmlWriter.WriteStartElement("HairClassification");
                        xmlWriter.WriteAttributeString("partKey", entry.Key);
                        xmlWriter.WriteAttributeString("growthState", entry.Value.ToString());
                        xmlWriter.WriteEndElement();
                    }
                    xmlWriter.WriteEndElement();
                    xmlWriter.WriteEndDocument();
                    xmlWriter.Flush();
                }
                stringWriter.Flush();
            }
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(stringBuilder.ToString());
            stringBuilder.Remove(0, stringBuilder.Length);
            using (var stringWriter = new Utf8StringWriter(stringBuilder))
            {
                using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
                       {
                           Encoding = Encoding.UTF8,
                           Indent = true,
                           IndentChars = "  ",
                           OmitXmlDeclaration = false
                       }))
                {
                    xmlDocument.WriteTo(xmlWriter);
                    xmlWriter.Flush();
                }
                stringWriter.Flush();
            }
            return stringBuilder.ToString();
        }
    }
}
