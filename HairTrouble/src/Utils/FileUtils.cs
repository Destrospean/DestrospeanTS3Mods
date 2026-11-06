using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
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
                SimpleMessageDialog.Show(Localization.LocalizeString(kLocalizationKey + "/NoServiceCollectionsExistDialog:Title"), Localization.LocalizeString(kLocalizationKey + "/NoServiceCollectionsExistDialog:Message"));
                return;
            }
            if (TryUIGetSelectedSavedSettingsNames(out var savedSettingsNames, new List<string>(savedSettings.Keys).ConvertAll(x => x.Replace(kSavedSettingsPrefix, "")).ToArray(), Localization.LocalizeString(/*DeleteServiceCollection.LocalizationKey* +*/ ":Name")))
            {
                foreach (var name in savedSettingsNames)
                {
                    BinModel.Singleton.DeleteFromExportBin(savedSettings[kSavedSettingsPrefix + name]);
                }
                SimpleMessageDialog.Show(Localization.LocalizeString(kLocalizationKey + "/DeleteServiceCollectionSuccessDialog:Title"), Localization.LocalizeString(kLocalizationKey + "/DeleteServiceCollectionSuccessDialog:Message"));
            }
        }

        public static bool ExportToFile(string text, out bool found)
        {
            string name = null;
            found = true;
            while (found)
            {
                name = StringInputDialog.Show(Localization.LocalizeString(kLocalizationKey + "/ExportServiceCollectionFilenameDialog:Title"), Localization.LocalizeString(kLocalizationKey + "/ExportServiceCollectionFilenameDialog:Prompt"), "");
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
                        SimpleMessageDialog.Show(Localization.LocalizeString(kLocalizationKey + "/ServiceCollectionExistsDialog:Title"), Localization.LocalizeString(kLocalizationKey + "/ServiceCollectionExistsDialog:Message"));
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
            SimpleMessageDialog.Show(Localization.LocalizeString(kLocalizationKey + "/ExportServiceCollectionSuccessDialog:Title"), Localization.LocalizeString(kLocalizationKey + "/ExportServiceCollectionSuccessDialog:Message"));
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
                SimpleMessageDialog.Show(Localization.LocalizeString(kLocalizationKey + "/NoServiceCollectionsExistDialog:Title"), Localization.LocalizeString(kLocalizationKey + "/NoServiceCollectionsExistDialog:Message"));
                return null;
            }
            return TryUIGetSelectedSavedSettingsNames(out var savedSettingsNames, new List<string>(savedSettings.Keys).ConvertAll(x => x.Replace(kSavedSettingsPrefix, "")).ToArray(), Localization.LocalizeString(/*ImportServiceCollection.LocalizationKey +*/ ":Name"), 1) ? savedSettings[kSavedSettingsPrefix + savedSettingsNames[0]] : null;
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
            LoadServiceProfiles(text);
            SimpleMessageDialog.Show(Localization.LocalizeString(kLocalizationKey + "/ImportServiceCollectionSuccessDialog:Title"), Localization.LocalizeString(kLocalizationKey + "/ImportServiceCollectionSuccessDialog:Message"));
            return true;
        }

        public static void LoadServiceProfiles(string text)
        {
            using (var stringReader = new StringReader(text))
            {
                using (var xmlReader = XmlReader.Create(stringReader, new XmlReaderSettings
                    {
                        IgnoreComments = true
                    }))
                {
                    var xmlDocument = new XmlDocument();
                    xmlDocument.Load(xmlReader);
                    foreach (XmlNode node in xmlDocument.DocumentElement.ChildNodes)
                    {
                        if (node.Name == "ServiceProfile" && node is XmlElement element)
                        {
                        }
                    }
                }
            }
        }

        public static bool TryUIGetSelectedSavedSettingsNames(out string[] selectedNames, string[] allNames, string title = null, int selectableRowCount = int.MaxValue)
        {
            Array.Sort(allNames);
            while (true)
            {
                selectedNames = (ObjectPickerDialog.Show(title ?? Responder.Instance.LocalizationModel.LocalizeString(kLocalizationKey + "/ServiceCollectionListDialog:Title"), new List<ObjectPicker.TabInfo>
                    {
                        new ObjectPicker.TabInfo("shop_all_r2", Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:All"), new List<string>(allNames).ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                    }, new List<ObjectPickerDialog.CommonHeaderInfo<string>>
                    {
                        new TextColumn(kLocalizationKey + "/ServiceCollectionListDialog")
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

        public static string WrapInnerXmlAsDocument(XmlNode node)
        {
            if (string.IsNullOrEmpty(node.InnerXml))
            {
                return null;
            }
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(node.InnerXml);
            var xmlDeclaration = xmlDocument.CreateXmlDeclaration("1.0", "utf-8", null);
            xmlDocument.InsertBefore(xmlDeclaration, xmlDocument.DocumentElement);
            var stringBuilder = new StringBuilder();
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

