using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Maussoft.Mvc.ViewGen
{
	public class VisualBasicGenerator:Generator
	{
		private string _baseDirecory;
		private string _defaultNamespace;
		private string _rootNamespace;

		public VisualBasicGenerator (string baseDirecory, string defaultNamespace, string rootNamespace)
		{
			_baseDirecory = baseDirecory;
			_defaultNamespace = defaultNamespace;
			_rootNamespace = rootNamespace;
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
			if (properties ["Namespace"].StartsWith(_rootNamespace)) {
				properties ["Namespace"] = properties ["Namespace"].Substring(_rootNamespace.Length+1);
			}

			EscapeProperties (properties);

			string output = GenerateClass(properties,statements);

			string filename2 = Path.ChangeExtension(filename,".vb");

			/*Console.WriteLine (filename2);

			Console.WriteLine (output);*/

			File.WriteAllText (filename2,output);
		}

		private void EscapeProperties(Dictionary <string,string> properties)
		{
			List<string> keywords = new List<string> ("AddHandler,AddressOf,Alias,And,AndAlso,As,Boolean,ByRef,Byte,ByVal,Call,Case,Catch,CBool,CByte,CChar,CDate,CDec,CDbl,Char,CInt,Class,CLng,CObj,Const,Continue,CSByte,CShort,CSng,CStr,CType,CUInt,CULng,CUShort,Date,Decimal,Declare,Default,Delegate,Dim,DirectCast,Do,Double,Each,Else,ElseIf,End,EndIf,Enum,Erase,Error,Event,Exit,False,Finally,For,Friend,Function,Get,GetType,GetXMLNamespace,Global,GoSub,GoTo,Handles,If,Implements,Imports,In,Inherits,Integer,Interface,Is,IsNot,Let,Lib,Like,Long,Loop,Me,Mod,Module,MustInherit,MustOverride,MyBase,MyClass,Namespace,Narrowing,New,Next,Not,Nothing,NotInheritable,NotOverridable,Object,Of,On,Operator,Option,Optional,Or,OrElse,Overloads,Overridable,Overrides,ParamArray,Partial,Private,Property,Protected,Public,RaiseEvent,ReadOnly,ReDim,REM,RemoveHandler,Resume,Return,SByte,Select,Set,Shadows,Shared,Short,Single,Static,Step,Stop,String,Structure,Sub,SyncLock,Then,Throw,To,True,Try,TryCast,TypeOf,Variant,Wend,UInteger,ULong,UShort,Using,When,While,Widening,With,WithEvents,WriteOnly,Xor".Split (','));
			string[] keys = new string[]{"Class","Inherits"};
			foreach (string key in keys) {
				if (keywords.Contains (properties [key])) {
					properties [key] = '[' + properties [key] + ']';
				}
			}
		}

		private string GenerateClass(Dictionary <string,string> properties, List<Statement> statements)
		{
			StringBuilder output = new StringBuilder ();

			output.Append ("'\n' WARNING: Generated file, do not edit, changes will be lost!\n'\n\n");

			if (properties.ContainsKey ("Using")) {
				string[] spaceNames = properties ["Using"].Split (',');
				foreach (string spaceName in spaceNames) {
					if (spaceName.Trim () != "System") {
						output.Append ("Imports " + spaceName.Trim () + "\n");
					}
				}
			}

			string functionName = "Content";
			if (properties.ContainsKey ("Type")) {
				if (properties ["Type"] == "Master") {
					functionName = "Header";
				}
			}

			output.Append ("\nNamespace " + properties ["Namespace"] + "\n\t");
			output.Append ("Public Class " + properties ["Class"] + "(Of TSession As New)\n\t\tInherits " + properties ["Inherits"] + "(Of TSession)\n\t\t\n\t\t");
			output.Append ("Public Overrides Sub "+functionName+"()\n\t\t\t");

			if (properties ["Type"] == "Master") {
				foreach (Statement statement in statements) {
					if (statement.Type == "Code" && statement.Tokens.Length == 1) {
						if (statement.Tokens [0].Value.Trim () == "RenderViewContent()") {
							statement.Tokens [0].Value = "\n\t\tEnd Sub\n\n\t\tPublic Overrides Sub Footer()\n\t\t\t";
						}
						break;
					}
				}
			}

			output.Append (GenerateStatements(statements));
			output.Append ("\n\t\tEnd Sub\n\tEnd Class\nEnd Namespace");

			output.Replace (" :  : ", " : ").Replace (" : \n", "\n").Replace ("\n\t\t\t : ", "\n\t\t\t");

			return output.ToString();
		}

		private string GenerateStatements(List<Statement> statements)
		{
			StringBuilder output = new StringBuilder();

			for (int i = 0; i < statements.Count; i++) {
				Token[] tokens = statements[i].Tokens;
				if (tokens [0].Type == "Code") {
					output.Append (" : ");
					output.Append (tokens [0].Value);
				} else if (tokens [0].Type == "NewLine") {
					output.Append ("\n\t\t\t");
				} else if (tokens [0].Type == "Text" || tokens [0].Type == "TextLine") {
					output.Append (" : ");
					if (tokens.Length == 1 && tokens [0].Value == "") {
						if (tokens [0].Type == "TextLine") {
							output.Append ("WriteLine()");
						}
						continue;
					}
					List<string> arguments = new List<string> ();
					if (tokens [0].Type == "TextLine") {
						output.Append ("WriteLine(\"");
					} else {
						output.Append ("Write(\"");
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
						output.Append ("\", "+String.Join(", ",arguments.ToArray())+")");
					} else {
						output.Append ("\")");
					}
				}
			}

			return output.ToString();
		}

		private Dictionary <string,string> GetDefaultProperties()
		{
			Dictionary <string,string> properties = new Dictionary <string,string> ();
			properties.Add ("Inherits", "Global.Maussoft.Mvc.View");
			return properties;
		}
	}
}

