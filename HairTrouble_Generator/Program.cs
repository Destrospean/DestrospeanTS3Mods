using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Xml;
using Mono.Cecil;
using s3pi.Package;

namespace Destrospean.HairTrouble.Generator
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            var executable = Assembly.GetExecutingAssembly();

            // Create a new package to clone to
            var package = Package.NewPackage(0);

            // Get the XML
            var xmlDocument = new XmlDocument();
            if (args.Length == 0)
            {
                var templateXmlPath = AppDomain.CurrentDomain.BaseDirectory + "Template.xml";
                if (!File.Exists(templateXmlPath))
                {
                    File.WriteAllText(templateXmlPath, new StreamReader(executable.GetManifestResourceStream("Template.xml") ?? throw new InvalidOperationException()).ReadToEnd());
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
            var identifier = Console.ReadLine();
            var assemblyName = "HairTrouble_" + (string.IsNullOrEmpty(identifier) ? FNV32.GetHash(Guid.NewGuid().ToString()).ToString() : identifier);

            // Get the assembly
            var assembly = AssemblyDefinition.ReadAssembly(executable.GetManifestResourceStream("HairTrouble_Base.dll"));

            // Copy the elements from the XML to put into the new package
            var rootNode = xmlDocument.SelectSingleNode("HairTroubleData");
            var elements = new List<XmlElement>();
            if (rootNode != null)
            {
                foreach (XmlNode node in rootNode.ChildNodes)
                {
                    if (node is XmlElement element && element.Name == "HairClassification" && element.GetAttribute("partKey") != "0x034AEECB-0x00000000-0x0000000000000000")
                    {
                        elements.Add(element);
                    }
                }

                rootNode.RemoveAll();
                foreach (var element in elements)
                {
                    rootNode.AppendChild(element);
                }
            }

            // Rename the assembly for the new package
            assembly.Name.Name = assemblyName;
            assembly.MainModule.Name = assemblyName + ".dll";

            var assemblyStream = new MemoryStream();
            var xmlStream = new MemoryStream();

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
