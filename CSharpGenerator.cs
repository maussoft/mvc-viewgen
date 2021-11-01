using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Maussoft.Mvc.ViewGen
{
	public class CSharpGenerator:Generator
	{
		private string _baseDirecory;
		private string _defaultNamespace;

		public CSharpGenerator (string baseDirecory, string defaultNamespace)
		{
			_baseDirecory = baseDirecory;
			_defaultNamespace = defaultNamespace;
		}

		public override void ConvertFile(string filename)
		{
			string relative = filename.Substring(_baseDirecory.TrimEnd(Path.DirectorySeparatorChar).Length+1);

			//Console.WriteLine (filename2);
			//Console.WriteLine (_baseDirecory);
			//Console.WriteLine (relative);

			List<Token> input = Tokenize (File.ReadAllText(filename));

			/*for (int i = 0; i < input.Count; i++) {
				Token token = input [i];
				Console.WriteLine (token.Type + " '" + token.Value + "'");
			}*/
			Dictionary <string,string> properties = GetDefaultProperties ();

			string reldir = Path.GetDirectoryName (relative);
			string spaceName = _defaultNamespace;

			if (reldir.Length > 0) {
				spaceName = spaceName + '.' + reldir.Replace(Path.DirectorySeparatorChar,'.');
			}

			properties ["Namespace"] = spaceName;

			properties ["Class"] = Path.GetFileNameWithoutExtension (relative);

			List<Statement> statements = Parse (input, properties);

			/*foreach (string key in properties.Keys) {
				Console.WriteLine (key + " '" + properties[key] + "'");
			}*/

			Generate (properties, statements, filename);
		}

		private void Generate(Dictionary <string,string> properties, List<Statement> statements, string filename)
		{
			EscapeProperties (properties);

			string output = GenerateClass(properties,statements);

			string filename2 = Path.ChangeExtension(filename,".cs");

			/*Console.WriteLine (filename2);

			Console.WriteLine (output);*/

			File.WriteAllText (filename2,output);
		}

		private void EscapeProperties(Dictionary <string,string> properties)
		{
			List<string> keywords = new List<string> ("abstract,as,base,bool,break,byte,case,catch,char,checked,class,const,continue,decimal,default,delegate,do,double,else,enum,event,explicit,extern,false,finally,fixed,float,for,foreach,goto,if,implicit,in,in (generic modifier),int,interface,internal,is,lock,long,namespace,new,null,object,operator,out,out (generic modifier),override,params,private,protected,public,readonly,ref,return,sbyte,sealed,short,sizeof,stackalloc,static,string,struct,switch,this,throw,true,try,typeof,uint,ulong,unchecked,unsafe,ushort,using,virtual,void,volatile,while".Split (','));
			string[] keys = new string[]{"Class","Inherits"};
			foreach (string key in keys) {
				if (keywords.Contains (properties [key])) {
					properties [key] = '@' + properties [key];
				}
			}
		}

		private string GenerateClass(Dictionary <string,string> properties, List<Statement> statements)
		{
			StringBuilder output = new StringBuilder ();

			output.Append ("/**\n * WARNING: Generated file, do not edit, changes will be lost!\n **/\n\n");

			output.Append ("using System;\n");
			if (properties.ContainsKey ("Using")) {
				string[] spaceNames = properties ["Using"].Split (',');
				foreach (string spaceName in spaceNames) {
					output.Append ("using " + spaceName.Trim () + ";\n");
				}
			}

			string functionName = "Content";
			if (properties ["Type"] == "Master") {
				functionName = "Header";
			}

			output.Append ("\nnamespace " + properties ["Namespace"] + "\n{\n\t");
			output.Append ("public class " + properties ["Class"] + "<TSession>: " + properties ["Inherits"] + "<TSession> where TSession : new()\n\t{\n\t\t");
			output.Append ("public override void "+functionName+"()\n\t\t{\n\t\t\t");

			if (properties ["Type"] == "Master") {
				foreach (Statement statement in statements) {
					if (statement.Type == "Code" && statement.Tokens.Length == 1) {
						if (statement.Tokens [0].Value.Trim () == "RenderViewContent();") {
							statement.Tokens [0].Value = "\n\t\t}\n\n\t\tpublic override void Footer()\n\t\t{\n\t\t\t";
						}
						break;
					}
				}
			}

			output.Append (GenerateStatements(statements));
			output.Append ("\n\t\t}\n\t}\n}");

			output.Replace ("\n\t\t\t\n", "\n");

			return output.ToString();
		}

		private string GenerateStatements(List<Statement> statements)
		{
			StringBuilder output = new StringBuilder();

			for (int i = 0; i < statements.Count; i++) {
				Token[] tokens = statements[i].Tokens;
				if (tokens [0].Type == "Code") {
					output.Append (tokens [0].Value);
				} else if (tokens [0].Type == "NewLine") {
					output.Append ("\n\t\t\t");
				} else if (tokens [0].Type == "Text" || tokens [0].Type == "TextLine") {
					if (tokens.Length == 1 && tokens [0].Value == "") {
						if (tokens [0].Type == "TextLine") {
							output.Append ("WriteLine();");
						}
						continue;
					}
					List<string> arguments = new List<string> ();
					if (tokens [0].Type == "TextLine") {
						output.Append ("WriteLine(@\"");
					} else {
						output.Append ("Write(@\"");
					}
					for (int j = 0; j < tokens.Length; j++) {
						Token token = tokens [j];
						if (token.Type == "Expr") {
							output.Append ("{" + arguments.Count + "}");
							arguments.Add (token.Value);
						} else if (token.Type == "Text" || tokens [0].Type == "TextLine") {
							output.Append (token.Value.Replace("\"","\"\""));
						}
					}
					if (arguments.Count > 0) {
						output.Append ("\", "+String.Join(", ",arguments.ToArray())+");");
					} else {
						output.Append ("\");");
					}
				}
			}

			return output.ToString();
		}

		private Dictionary <string,string> GetDefaultProperties()
		{
			Dictionary <string,string> properties = new Dictionary <string,string> ();
			properties.Add ("Inherits", "Maussoft.Mvc.View");
			return properties;
		}
	}
}

