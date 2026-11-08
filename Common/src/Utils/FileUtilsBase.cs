using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Destrospean.Misc;
using Destrospean.UI.Columns;
using Sims3.Gameplay;
using Sims3.Gameplay.CAS;
using Sims3.SimIFace;
using Sims3.UI;
using ObjectPickerDialog = Destrospean.UI.Dialogs.ObjectPickerDialog;

namespace Destrospean.Utils
{
    public abstract class FileUtilsBase<T>
    {
        public abstract void DeleteFile();
        
        public static void DeleteFile(string fileNamePrefix, string entryKey, string title, string successDialogTitle, string successDialogMessage, string noFilesExistDialogTitle, string noFilesExistDialogMessage)
        {
            BinModel.Singleton.PopulateExportBin();
            var savedSettings = new Dictionary<string, ulong>();
            foreach (ExportBinContents contents in BinModel.Singleton.ExportBinContents)
            {
                if (contents.HouseholdName != null && contents.HouseholdName.Contains(fileNamePrefix))
                {
                    savedSettings[contents.HouseholdName] = contents.ContentId;
                }
            }
            if (savedSettings.Count == 0)
            {
                SimpleMessageDialog.Show(noFilesExistDialogTitle, noFilesExistDialogMessage);
                return;
            }
            if (TryUIGetSelectedFileNames(out var savedSettingsNames, new List<string>(savedSettings.Keys).ConvertAll(x => x.Replace(fileNamePrefix, "")).ToArray(), entryKey, title))
            {
                foreach (var name in savedSettingsNames)
                {
                    BinModel.Singleton.DeleteFromExportBin(savedSettings[fileNamePrefix + name]);
                }
                SimpleMessageDialog.Show(successDialogTitle, successDialogMessage);
            }
        }

        public abstract bool ExportToFile(string text, out bool found);
        
        public static bool ExportToFile(string fileNamePrefix, string text, string fileNameDialogTitle, string successDialogTitle, string successDialogMessage, string fileNameDialogPrompt, string fileExistsDialogTitle, string fileExistsDialogMessage, out bool found)
        {
            string name = null;
            found = true;
            while (found)
            {
                name = StringInputDialog.Show(fileNameDialogTitle, fileNameDialogPrompt, "");
                if (string.IsNullOrEmpty(name))
                {
                    return false;
                }
                name = fileNamePrefix + name;
                BinModel.Singleton.PopulateExportBin();
                found = false;
                foreach (ExportBinContents contents in BinModel.Singleton.ExportBinContents)
                {
                    if (contents.HouseholdName == null || !contents.HouseholdName.Contains(fileNamePrefix))
                    {
                        continue;
                    }
                    if (contents.HouseholdName == name)
                    {
                        SimpleMessageDialog.Show(fileExistsDialogTitle, fileExistsDialogMessage);
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
            SimpleMessageDialog.Show(successDialogTitle, successDialogMessage);
            return !found;
        }

        public abstract string ExtractFromFile();

        public static string ExtractFromFile(string fileNamePrefix, string entryKey, string title, string fileExistsDialogTitle, string fileExistsDialogMessage)
        {
            BinModel.Singleton.PopulateExportBin();
            var files = new Dictionary<string, string>();
            foreach (ExportBinContents contents in BinModel.Singleton.ExportBinContents)
            {
                if (contents.HouseholdName != null && contents.HouseholdName.Contains(fileNamePrefix))
                {
                    files[contents.HouseholdName] = contents.HouseholdBio;
                }
            }
            if (files.Count == 0)
            {
                SimpleMessageDialog.Show(fileExistsDialogTitle, fileExistsDialogMessage);
                return null;
            }
            return TryUIGetSelectedFileNames(out var fileNames, new List<string>(files.Keys).ConvertAll(x => x.Replace(fileNamePrefix, "")).ToArray(), entryKey, title, 1) ? files[fileNamePrefix + fileNames[0]] : null;
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

        public abstract bool ImportFromFile();
        
        public bool ImportFromFile(string successDialogTitle, string successDialogMessage)
        {
            return ImportSettings(ExtractFromFile(), successDialogTitle, successDialogMessage);
        }

        public abstract bool ImportFromTuning(string name);

        public bool ImportFromTuning(string name, string successDialogTitle, string successDialogMessage)
        {
            return ImportSettings(ExtractFromTuning(name), successDialogTitle, successDialogMessage);
        }

        public bool ImportSettings(string text, string successDialogTitle, string successDialogMessage)
        {
            if (text == null)
            {
                return false;
            }
            LoadXml(text);
            SimpleMessageDialog.Show(successDialogTitle, successDialogMessage);
            return true;
        }
        
        public abstract void LoadXml(string text);

        public static bool TryUIGetSelectedFileNames(out string[] selectedNames, string[] allNames, string entryKey, string title = null, int selectableRowCount = int.MaxValue)
        {
            Array.Sort(allNames);
            while (true)
            {
                selectedNames = (ObjectPickerDialog.Show(title ?? Responder.Instance.LocalizationModel.LocalizeString(entryKey + ":Title"), new List<ObjectPicker.TabInfo>
                    {
                        new ObjectPicker.TabInfo("shop_all_r2", Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:All"), new List<string>(allNames).ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                    }, new List<ObjectPickerDialog.CommonHeaderInfo<string>>
                    {
                        new TextColumn(entryKey)
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
        
        public abstract string ToXml(T entries);
    }
}
