using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Destrospean.Enums.HairTrouble;
using Destrospean.HairTrouble;
using Destrospean.Misc;
using Destrospean.UI.Columns;
using Sims3.Gameplay;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using ObjectPickerDialog = Destrospean.UI.Dialogs.ObjectPickerDialog;

namespace Destrospean.Utils.HairTrouble
{
    public class FileUtils
    {
        private const string kLocalizationKey = "Destrospean/UI/Dialogs";

        private const string kSavedSettingsPrefix = "Destrospean.HairTrouble.";

        public static void DeleteFile()
        {
            BinModel.Singleton.PopulateExportBin();
            var savedSettings = new Dictionary<string, ulong>();
            foreach (ExportBinContents contents in BinModel.Singleton.ExportBinContents)
            {
                if (contents.HouseholdName != null && contents.HouseholdName.Contains(kSavedSettingsPrefix))
                {
                    savedSettings[contents.HouseholdName] = contents.ContentId;
                }
            }
            if (savedSettings.Count == 0)
            {
                SimpleMessageDialog.Show(Localization.LocalizeString(kLocalizationKey + "/NoHairClassificationCollectionsExistDialog:Title"), Localization.LocalizeString(kLocalizationKey + "/NoHairClassificationCollectionsExistDialog:Message"));
                return;
            }
            if (TryUIGetSelectedSavedSettingsNames(out var savedSettingsNames, new List<string>(savedSettings.Keys).ConvertAll(x => x.Replace(kSavedSettingsPrefix, "")).ToArray(), Localization.LocalizeString(/*DeleteHairClassificationCollection.LocalizationKey* +*/ ":Name")))
            {
                foreach (var name in savedSettingsNames)
                {
                    BinModel.Singleton.DeleteFromExportBin(savedSettings[kSavedSettingsPrefix + name]);
                }
                SimpleMessageDialog.Show(Localization.LocalizeString(kLocalizationKey + "/DeleteHairClassificationCollectionSuccessDialog:Title"), Localization.LocalizeString(kLocalizationKey + "/DeleteHairClassificationCollectionSuccessDialog:Message"));
            }
        }

        public static bool ExportToFile(string text, out bool found)
        {
            string name = null;
            found = true;
            while (found)
            {
                name = StringInputDialog.Show(Localization.LocalizeString(kLocalizationKey + "/ExportHairClassificationCollectionFilenameDialog:Title"), Localization.LocalizeString(kLocalizationKey + "/ExportHairClassificationCollectionFilenameDialog:Prompt"), "");
                if (string.IsNullOrEmpty(name))
                {
                    return false;
                }
                name = kSavedSettingsPrefix + name;
                BinModel.Singleton.PopulateExportBin();
                found = false;
                foreach (ExportBinContents contents in BinModel.Singleton.ExportBinContents)
                {
                    if (contents.HouseholdName == null || !contents.HouseholdName.Contains(kSavedSettingsPrefix))
                    {
                        continue;
                    }
                    if (contents.HouseholdName == name)
                    {
                        SimpleMessageDialog.Show(Localization.LocalizeString(kLocalizationKey + "/HairClassificationCollectionExistsDialog:Title"), Localization.LocalizeString(kLocalizationKey + "/HairClassificationCollectionExistsDialog:Message"));
                        found = true;
                        break;
                    }
                }
            }
            var dummyHousehold = Household.Create();
            dummyHousehold.SetName(name);
            dummyHousehold.BioText = text;
            BinModel.Singleton.AddToExportBin(dummyHousehold);
            dummyHousehold.Destroy();
            SimpleMessageDialog.Show(Localization.LocalizeString(kLocalizationKey + "/ExportHairClassificationCollectionSuccessDialog:Title"), Localization.LocalizeString(kLocalizationKey + "/ExportHairClassificationCollectionSuccessDialog:Message"));
            return !found;
        }

        public static string ExtractFromFile()
        {
            BinModel.Singleton.PopulateExportBin();
            var savedSettings = new Dictionary<string, string>();
            foreach (ExportBinContents contents in BinModel.Singleton.ExportBinContents)
            {
                if (contents.HouseholdName != null && contents.HouseholdName.Contains(kSavedSettingsPrefix))
                {
                    savedSettings[contents.HouseholdName] = contents.HouseholdBio;
                }
            }
            if (savedSettings.Count == 0)
            {
                SimpleMessageDialog.Show(Localization.LocalizeString(kLocalizationKey + "/NoHairClassificationCollectionsExistDialog:Title"), Localization.LocalizeString(kLocalizationKey + "/NoHairClassificationCollectionsExistDialog:Message"));
                return null;
            }
            return TryUIGetSelectedSavedSettingsNames(out var savedSettingsNames, new List<string>(savedSettings.Keys).ConvertAll(x => x.Replace(kSavedSettingsPrefix, "")).ToArray(), Localization.LocalizeString(/*ImportHairClassificationCollection.LocalizationKey +*/ ":Name"), 1) ? savedSettings[kSavedSettingsPrefix + savedSettingsNames[0]] : null;
        }

        public static string ExtractFromTuning(string name)
        {
            var xmlDocument = Simulator.LoadXML(name);
            if (xmlDocument == null)
            {
                return null; 
            }
            var stringBuilder = new StringBuilder();
            using (var stringWriter = new Utf8StringWriter(stringBuilder))
            {
                using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
                    {
                        Encoding = Encoding.UTF8,
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

        public static string GetXml(IEnumerable<object> profiles)
        {
            var stringBuilder = new StringBuilder();
            using (var stringWriter = new Utf8StringWriter(stringBuilder))
            {
                using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
                    {
                        Encoding = Encoding.UTF8
                    }))
                {
                    foreach (var entry in SimHairGrowth.HairGrowthStateMap)
                    {
                        xmlWriter.WriteElementString("CASPartKey", entry.Key);
                        xmlWriter.WriteElementString("GrowthState", entry.Value.ToString());
                    }
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

        public static bool ImportFromFile()
        {
            return ImportSettings(ExtractFromFile());
        }

        public static bool ImportFromTuning(string name)
        {
            return ImportSettings(ExtractFromTuning(name));
        }

        public static bool ImportSettings(string text)
        {
            if (text == null)
            {
                return false;
            }
            LoadHairClassifications(text);
            SimpleMessageDialog.Show(Localization.LocalizeString(kLocalizationKey + "/ImportHairClassificationCollectionSuccessDialog:Title"), Localization.LocalizeString(kLocalizationKey + "/ImportHairClassificationCollectionSuccessDialog:Message"));
            return true;
        }

        public static void LoadHairClassifications(string text)
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
                            if (xmlReader.Name == "HairGrowthStateMap")
                            {
                                xmlReader.MoveToContent();
                            }
                            else if (xmlReader.Name == "Entry")
                            {
                                SimHairGrowth.HairGrowthStateMap[xmlReader.GetAttribute("CASPartKey") ?? ""] = (HairGrowthStates)Enum.Parse(typeof(HairGrowthStates), xmlReader.GetAttribute("GrowthState") ?? "");
                            }
                        }
                    }
                    xmlReader.Close();
                }
            }
        }

        public static bool TryUIGetSelectedSavedSettingsNames(out string[] selectedNames, string[] allNames, string title = null, int selectableRowCount = int.MaxValue)
        {
            Array.Sort(allNames);
            while (true)
            {
                selectedNames = (ObjectPickerDialog.Show(title ?? Responder.Instance.LocalizationModel.LocalizeString(kLocalizationKey + "/HairClassificationCollectionListDialog:Title"), new List<ObjectPicker.TabInfo>
                    {
                        new ObjectPicker.TabInfo("shop_all_r2", Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:All"), new List<string>(allNames).ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                    }, new List<ObjectPickerDialog.CommonHeaderInfo<string>>
                    {
                        new TextColumn(kLocalizationKey + "/HairClassificationCollectionListDialog")
                    }, selectableRowCount, out var confirmed, out var cancelled) ?? new List<string>()).ToArray();
                if (cancelled)
                {
                    selectedNames = null;
                    return false;
                }
                if (confirmed)
                {
                    return true;
                }
            }
        }
    }
}
