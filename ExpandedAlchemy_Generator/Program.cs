using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Xml;
using Mono.Cecil;
using s3pi.Interfaces;

namespace Destrospean.ExpandedAlchemy.Generator
{
    class Program
    {
        enum Locales
        {
            ENG_US,
            CHS_CN,
            CHT_CN,
            CZE_CZ,
            DAN_DK,
            DUT_NL,
            FIN_FI,
            FRE_FR,
            GER_DE,
            GRE_GR,
            HUN_HU,
            ITA_IT,
            JPN_JP,
            KOR_KR,
            NOR_NO,
            POL_PL,
            POR_PT,
            POR_BR,
            RUS_RU,
            SPA_ES,
            SPA_MX,
            SWE_SE,
            THA_TH
        }

        public static void Main(string[] args)
        {
            var executable = System.Reflection.Assembly.GetExecutingAssembly();

            // Create a new package to clone to
            var package = s3pi.Package.Package.NewPackage(0);

            // Get the XML
            var xmlDocument = new XmlDocument();
            if (args.Length == 0)
            {
                var templateXmlPath = AppDomain.CurrentDomain.BaseDirectory + "Template.xml";
                if (!File.Exists(templateXmlPath))
                {
                    File.WriteAllText(templateXmlPath, new StreamReader(executable.GetManifestResourceStream("Template.xml")).ReadToEnd());
                    return;
                }
                xmlDocument.Load(templateXmlPath);
            }
            else
            {
                xmlDocument.Load(args[0]);
            }

            Console.Write("Specify a unique suffix for the package (leave blank for a random number suffix): ");

            // Get a unique name for the assembly and _XML resource
            string identifier = Console.ReadLine(),
            assemblyName = "ExpandedAlchemy_" + (string.IsNullOrEmpty(identifier) ? FNV32.GetHash(Guid.NewGuid().ToString()).ToString() : identifier);

            // Get the assembly
            var assembly = AssemblyDefinition.ReadAssembly(executable.GetManifestResourceStream("ExpandedAlchemy_Base.dll"));

            // Copy the elements from the XML to put into the new package
            var rootNode = xmlDocument.SelectSingleNode("ExpandedAlchemyData");
            var elements = new List<XmlNode>();
            foreach (XmlNode node in rootNode.ChildNodes)
            {
                if (node is XmlElement)
                {
                    elements.Add(node);
                }
            }
            rootNode.RemoveAll();
            foreach (var element in elements)
            {
                rootNode.AppendChild(element);
            }

            // Rename the assembly for the new package
            assembly.Name.Name = assemblyName;
            assembly.MainModule.Name = assemblyName + ".dll";

            Stream assemblyStream = new MemoryStream(),
            xmlStream = new MemoryStream();

            // Save the assembly with the new name
            assembly.Write(assemblyStream);

            // Save the new XML to a stream
            using (var xmlWriter = XmlWriter.Create(xmlStream, new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  "
                }))
            {
                xmlDocument.Save(xmlWriter);
            }

            // Add the resources
            var s3saKeyInstance = FNV64.GetHash(assemblyName);
            var nameMapResource = new NameMapResource.NameMapResource(0, null);
            var stblResources = new StblResource.StblResource[Enum.GetValues(typeof(Locales)).Length];
            for (var i = 0; i < stblResources.Length; i++)
            {
                var stblKeyInstance = ulong.Parse(i.ToString("X2") + s3saKeyInstance.ToString("X16").Substring(2), System.Globalization.NumberStyles.HexNumber);
                var stblName = "Strings_" + (Locales)i + "_0x" + stblKeyInstance.ToString("X16");
                stblResources[i] = new StblResource.StblResource(0, null);
                foreach (var element in elements)
                {
                    var path = "";
                    switch (element.Name)
                    {
                        case "AlchemyRecipes":
                            path = "Gameplay/Excel/AlchemyRecipes/AlchemyRecipes:";
                            break;
                        case "Beetles":
                        case "Butterflies":
                        case "GlowBugs":
                        case "Mosquitoes":
                        case "Nanites":
                            path = "Gameplay/Excel/Insects/" + element.Name + ":";
                            break;
                        case "Fish":
                            path = "Gameplay/Excel/Fishing/Fish:";
                            break;
                        case "GemCuts":
                        case "Gems":
                        case "Metals":
                        case "RockNames":
                        case "Rocks":
                            path = "Gameplay/Excel/RockGemMetal/" + element.Name + ":";
                            break;
                    }
                    foreach (XmlNode childNode in element.ChildNodes)
                    {
                        if ((childNode.Name == "Name" || childNode.Name == "Description" || childNode.Name == "LocalizedName" || childNode.Name == "LocalizedDescription" || childNode.Name.EndsWith("TNS")) && !string.IsNullOrEmpty(childNode.InnerText))
                        {
                            stblResources[i].Add(FNV64.GetHash(path + childNode.InnerText), childNode.InnerText);
                        }
                    }
                }
                package.AddResource(new ResourceKey(0x220557DA, 0, stblKeyInstance), stblResources[i].Stream, true);
                nameMapResource.Add(stblKeyInstance, stblName);
            }
            nameMapResource.Add(s3saKeyInstance, assemblyName);
            package.AddResource(new ResourceKey(0x166038C, 0, 0), nameMapResource.Stream, true);
            package.AddResource(new ResourceKey(0x333406C, 0, s3saKeyInstance), xmlStream, true);
            package.AddResource(new ResourceKey(0x73FAA07, 0, s3saKeyInstance), new ScriptResource.ScriptResource(0, null)
                {
                    Assembly = new BinaryReader(assemblyStream)
                }.Stream, true);

            // Save the new package with the new name
            package.SaveAs(AppDomain.CurrentDomain.BaseDirectory + assemblyName + ".package");
        }
    }
}
