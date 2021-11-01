// SayHello.cs
using Microsoft.Build.Framework;
using System;
using System.IO;
using System.Xml;

namespace Maussoft.Mvc.ViewGen
{
    public class GenerateViews : Microsoft.Build.Utilities.Task
    {
        public override bool Execute()
        {
            string directory = Directory.GetCurrentDirectory ();
			string viewDirectory = Path.Combine(directory,"Views");
			string viewNamespace = FindViewNamespaceInAppConfig (directory, "App.config");
			string rootNamespace = FindRootNamespaceInProjectFile (directory, "*.csproj");
			string language = "C#";
			if (rootNamespace == null) {
				rootNamespace = FindRootNamespaceInProjectFile (directory, "*.vbproj");
				language = "VB";
			}
			if (rootNamespace == null) {
				Log.LogMessage("Could not find project file (*.csproj or *.vbproj)");
                return false;
			}
			if (viewNamespace==null) {
				viewNamespace = rootNamespace + '.' + "Views";
			}
			string[] files = Directory.GetFiles(viewDirectory, "*.aspx", SearchOption.AllDirectories);

			Generator generator; 
			if (language == "C#") {
				generator = new CSharpGenerator (viewDirectory, viewNamespace);
			} else {
				generator = new VisualBasicGenerator (viewDirectory, viewNamespace, rootNamespace);
			}

			foreach (string filename in files) {
				Log.LogMessage ('.'+filename.Substring(directory.Length));
				generator.ConvertFile (filename);
			}
            return true;
		}

		static string FindViewNamespaceInAppConfig(string directory, string filename)
		{
			string fullPath = Path.Combine (directory, filename);
			if (File.Exists (fullPath)) {
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.Load(fullPath);
				XmlNamespaceManager nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
				nsManager.AddNamespace("c", xmlDoc.DocumentElement.NamespaceURI);
				XmlNode node = xmlDoc.SelectSingleNode("/c:configuration/c:appSettings/c:add[@key='Maussoft.Mvc.ViewNamespaces']/@value",nsManager);
				if (node!=null) {
					return (node as XmlAttribute).Value.Split(',')[0];
				}
			}
			return null;
		}

		static string FindRootNamespaceInProjectFile(string directory, string glob)
		{
			string[] files = Directory.GetFiles(directory, glob, SearchOption.TopDirectoryOnly);
			foreach (string fullPath in files) {
				if (File.Exists (fullPath)) {
					XmlDocument xmlDoc = new XmlDocument();
					xmlDoc.Load(fullPath);
					XmlNamespaceManager nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
					nsManager.AddNamespace("p", xmlDoc.DocumentElement.NamespaceURI);
					XmlNode node = xmlDoc.SelectSingleNode("/p:Project/p:PropertyGroup/p:RootNamespace",nsManager);
					if (node != null) {
						return node.InnerText;
					}
				}
			}
			return null;
		}


	}
}
