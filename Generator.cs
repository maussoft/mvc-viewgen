using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Maussoft.Mvc.ViewGen
{
	public class Token
	{
		public Token(string type,string value)
		{ 
			Type = type;
			Value = value;
		}

		public string Type;
		public string Value;
	}

	public class Statement
	{
		public Statement(string type, Token[] tokens)
		{ 
			Type = type;
			Tokens = tokens;
		}

		public string Type;
		public Token[] Tokens;
	}

	public class Generator
	{
		public virtual void ConvertFile (string filename)
		{

		}

		protected void AddTokenListAsStatement(List<Statement> statements, List<Token> statement)
		{
			if (statement.Count > 0) {
				statements.Add (new Statement(statement[0].Type, statement.ToArray()));
			}
		}

		protected void ParsePropertiesInHead(Dictionary <string,string> properties, Token head)
		{
			// simple parser
			string regex = @"([a-zA-Z]+)(\s*=\s*""([^""]+?)"")?";
			foreach (Match match in Regex.Matches (head.Value, regex, RegexOptions.IgnoreCase)) {
				string key = match.Groups [1].Value;
				if ( match.Groups [2].Value.Length>0) {
					string value = match.Groups [3].Value;
					properties [key] = value;
				} else {
					properties ["Type"] = key;
				}
			}
		}

		protected List<Statement> Parse(List<Token> input, Dictionary <string,string> properties)
		{
			List<Statement> statements = new List<Statement> ();
			List<Token> statement = new List<Token>();

			for (int i=0;i<input.Count;i++) {
				if (input [i].Type == "Head") {
					AddTokenListAsStatement (statements, statement);
					ParsePropertiesInHead(properties, input [i]);
					statement = new List<Token>();
				} else if (input [i].Type == "Code") {
					AddTokenListAsStatement (statements, statement);
					statements.Add (new Statement("Code",new Token[]{input [i]}));
					statement = new List<Token>();
				} else if (input [i].Type == "NewLine") {
					if (statement.Count > 0) {
						if (statement [0].Type == "Text") {
							statement [0].Type = "TextLine";
						}
					}
					AddTokenListAsStatement (statements, statement);
					statements.Add (new Statement("NewLine",new Token[]{input [i]}));
					statement = new List<Token>();
				} else {
					statement.Add (input [i]);
				}
			}
			AddTokenListAsStatement (statements, statement);

			/*for (int i = 0; i < statements.Count; i++) {
				Token[] tokens = statements[i].Tokens;
				for (int j = 0; j < tokens.Length; j++) {
					Token token = tokens [j];
					Console.Write (token.Type+"("+token.Value+")");
				}
				Console.WriteLine ();
			}*/

			return statements;
		}


		protected void AddTokens(List<Token> tokens, string type, string value)
		{
			string[] lines = value.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
			for (int i=0;i<lines.Length;i++) {
				tokens.Add (new Token (type, lines [i]));
				if (i < lines.Length - 1) {
					tokens.Add (new Token ("NewLine", ""));
				}
			}
		}

		protected List<Token> Tokenize(string input)
		{
			int start,end = 0;
			List<Token> tokens = new List<Token>();

			while ((start = input.IndexOf("<%",end))>=0) {
				AddTokens(tokens,"Text",input.Substring (end, start - end));
				end = input.IndexOf("%>",start);
				if (end<0) break;
				if (input[start+2]=='=') {
					AddTokens(tokens,"Expr",input.Substring (start+3,end-(start+3)));
				} else if (input[start+2]=='@') {
					AddTokens(tokens,"Head",input.Substring (start+3,end-(start+3)));
				} else {
					AddTokens(tokens,"Code",input.Substring (start+3,end-(start+3)));
				}
				end = end + 2;
			}
			AddTokens(tokens,"Text",input.Substring (end));

			return tokens;
		}
	}
}

