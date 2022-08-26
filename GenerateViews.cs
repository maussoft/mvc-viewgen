using System.IO;
using System.Xml;

namespace Maussoft.Mvc.ViewGen
{
    public class GenerateViews : Microsoft.Build.Utilities.Task
    {
        public override bool Execute()
        {
            string directory = Directory.GetCurrentDirectory();
            string viewDirectory = Path.Combine(directory, "Views");
            string rootNamespace = FindRootNamespaceInProjectFile(directory, "*.csproj");
            string viewNamespace = null;
            string sessionClass = null;
            string language = "C#";
            if (rootNamespace == null)
            {
                rootNamespace = FindRootNamespaceInProjectFile(directory, "*.vbproj");
                language = "VB";
            }
            if (rootNamespace == null)
            {
                Log.LogMessage("Could not find project file (*.csproj or *.vbproj)");
                return false;
            }
            viewNamespace = rootNamespace + '.' + "Views";
            sessionClass = rootNamespace + '.' + "Session";
            string[] files = Directory.GetFiles(viewDirectory, "*.aspx", SearchOption.AllDirectories);

            Generator generator;
            if (language == "C#")
            {
                generator = new CSharpGenerator(viewDirectory, viewNamespace, sessionClass);
            }
            else
            {
                generator = new VisualBasicGenerator(viewDirectory, viewNamespace, sessionClass, rootNamespace);
            }

            foreach (string filename in files)
            {
                Log.LogMessage('.' + filename.Substring(directory.Length));
                generator.ConvertFile(filename);
            }
            return true;
        }

        private static string FindRootNamespaceInProjectFile(string directory, string glob)
        {
            string[] files = Directory.GetFiles(directory, glob, SearchOption.TopDirectoryOnly);
            foreach (string fullPath in files)
            {
                if (File.Exists(fullPath))
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(fullPath);
                    XmlNamespaceManager nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
                    nsManager.AddNamespace("p", xmlDoc.DocumentElement.NamespaceURI);
                    XmlNode node = xmlDoc.SelectSingleNode("/p:Project/p:PropertyGroup/p:RootNamespace", nsManager);
                    if (node != null)
                    {
                        return node.InnerText;
                    }
                }
            }
            return null;
        }


    }
}
