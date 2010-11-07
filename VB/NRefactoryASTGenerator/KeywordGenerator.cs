﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NRefactoryASTGenerator
{
	static class KeywordGenerator
	{
		static readonly string baseDir = "../../../Project/Lexer/";
		static readonly string testBaseDir = "../../../Test/Lexer/";
		static readonly string parserBaseDir = "../../../Project/Parser/";
		
		public static void Generate()
		{
			try {
				Dictionary<string, string> properties = new Dictionary<string, string>();
				Dictionary<string, string[]> sets = new Dictionary<string, string[]>();
				List<string> keywords = new List<string>();
				List<string> terminals = new List<string>();
				Dictionary<string, string> specialChars = new Dictionary<string, string>();

				ReadFile(properties, sets, keywords, terminals, specialChars);

				GenerateFiles(properties, sets, keywords, terminals, specialChars);
			} catch (Exception e) {
				Debug.Print(e.ToString());
			}
		}
		
		static void GenerateFiles(Dictionary<string, string> properties, Dictionary<string, string[]> sets,
		                          List<string> keywords, List<string> terminals, Dictionary<string, string> specialChars)
		{
			GenerateKeywords(properties, keywords);
			GenerateTokens(properties, sets, keywords, terminals, specialChars);
			GenerateTests(keywords, specialChars);
			GenerateKeywordSection(keywords, terminals, specialChars);
		}
		
		static void GenerateKeywordSection(List<string> keywords, List<string> terminals, Dictionary<string, string> specialChars)
		{
			string sourceDir = Path.Combine(parserBaseDir, "vbnet.atg");
			StringBuilder builder = new StringBuilder();
			
			builder.AppendLine("/* START AUTOGENERATED TOKENS SECTION */");
			builder.AppendLine("TOKENS");
			builder.AppendLine("\t/* ----- terminal classes ----- */");
			builder.AppendLine("\t/* EOF is 0 */");
			
			foreach (string terminal in terminals) {
				if (terminal == "EOF")
					continue;
				if (terminal == "Identifier") {
					builder.AppendLine("\tident");
					continue;
				}
				builder.AppendLine("\t" + terminal);
			}
			
			builder.AppendLine();
			builder.AppendLine("\t/* ----- special character ----- */");
			foreach (string specialChar in specialChars.Values) {
				builder.AppendLine("\t" + specialChar);
			}
			
			builder.AppendLine();
			builder.AppendLine("\t/* ----- keywords ----- */");
			foreach (string keyword in keywords) {
				builder.AppendLine("\t\"" + keyword + "\"");
			}
			
			builder.AppendLine("/* END AUTOGENERATED TOKENS SECTION */");
			
			string[] generatedLines = builder.ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
			string[] lines = File.ReadAllLines(sourceDir);
			
			var newContent = lines
				.TakeWhile(l => l != "/* START AUTOGENERATED TOKENS SECTION */")
				.Concat(generatedLines)
				.Concat(lines.SkipWhile(l => l != "/* END AUTOGENERATED TOKENS SECTION */").Skip(2))
				.ToArray();

			File.WriteAllLines(sourceDir, newContent);
		}

		static void GenerateTests(List<string> keywords, Dictionary<string, string> specialChars)
		{
			string sourceDir = Path.Combine(testBaseDir, "LexerTests.cs");
			using (StreamWriter writer = new StreamWriter(new FileStream(sourceDir, FileMode.Create))) {
				writer.WriteLine("// this file was autogenerated by a tool.");
				writer.WriteLine("using System;");
				writer.WriteLine("using System.IO;");
				writer.WriteLine("using NUnit.Framework;");
				writer.WriteLine("using ICSharpCode.NRefactory.VB;");
				writer.WriteLine("using ICSharpCode.NRefactory.VB.Parser;");
				writer.WriteLine("using ICSharpCode.NRefactory.VB.PrettyPrinter;");
				writer.WriteLine();
				writer.WriteLine("namespace ICSharpCode.NRefactory.VB.Tests.Lexer");
				writer.WriteLine("{");
				writer.WriteLine("\t[TestFixture]");
				writer.WriteLine("\tpublic sealed class LexerTests");
				writer.WriteLine("\t{");
				writer.WriteLine("\t\tILexer GenerateLexer(StringReader sr)");
				writer.WriteLine("\t\t{");
				writer.WriteLine("\t\t\treturn ParserFactory.CreateLexer(sr);");
				writer.WriteLine("\t\t}");
				for (int i = 0; i < specialChars.Values.Count; i++) {
					writer.WriteLine();
					writer.WriteLine("\t\t[Test]");
					writer.WriteLine("\t\tpublic void Test{0}()", specialChars.Keys.ElementAt(i));
					writer.WriteLine("\t\t{");
					writer.WriteLine("\t\t\tILexer lexer = GenerateLexer(new StringReader({0}));", specialChars.Values.ElementAt(i));
					writer.WriteLine("\t\t\tAssert.AreEqual(Tokens.{0}, lexer.NextToken().Kind);", specialChars.Keys.ElementAt(i));
					writer.WriteLine("\t\t}");
				}
				foreach (string keyword in keywords) {
					if (keyword == "Rem")
						continue;
					writer.WriteLine();
					writer.WriteLine("\t\t[Test]");
					writer.WriteLine("\t\tpublic void Test{0}()", UpperCaseFirst(keyword));
					writer.WriteLine("\t\t{");
					writer.WriteLine("\t\t\tILexer lexer = GenerateLexer(new StringReader(\"{0}\"));", keyword);
					writer.WriteLine("\t\t\tAssert.AreEqual(Tokens.{0}, lexer.NextToken().Kind);", UpperCaseFirst(keyword));
					writer.WriteLine("\t\t}");
				}
				writer.WriteLine("\t}");
				writer.WriteLine("}");
			}
		}

		static void GenerateTokens(Dictionary<string, string> properties, Dictionary<string, string[]> sets, List<string> keywords, List<string> terminals, Dictionary<string, string> specialChars)
		{
			string sourceDir = Path.Combine(baseDir, "Tokens.cs");
			using (StreamWriter writer = new StreamWriter(new FileStream(sourceDir, FileMode.Create))) {
				writer.WriteLine("// this file was autogenerated by a tool.");
				writer.WriteLine("using System;");
				writer.WriteLine("using System.Collections;");
				writer.WriteLine();
				writer.WriteLine("namespace {0}", properties["Namespace"]);
				writer.WriteLine("{");
				writer.WriteLine("\tpublic static class Tokens");
				writer.WriteLine("\t{");
				writer.WriteLine("\t\t// ----- terminal classes -----");
				int tokenValue = 0;
				foreach (string terminal in terminals)
					writer.WriteToken(terminal, ref tokenValue);
				writer.WriteLine();
				writer.WriteLine("\t\t// ----- special character -----");
				foreach (string specialChar in specialChars.Keys)
					writer.WriteToken(specialChar, ref tokenValue);
				writer.WriteLine();
				writer.WriteLine("\t\t// ----- keywords -----");
				foreach (string keyword in keywords)
					writer.WriteToken(keyword, ref tokenValue);
				writer.WriteLine();
				writer.WriteLine("\t\tpublic const int MaxToken = {0};", tokenValue);
				if (sets.Any()) {
					writer.WriteLine("\t\tstatic BitArray NewSet(params int[] values)");
					writer.WriteLine("\t\t{");
					writer.WriteLine("\t\t\tBitArray bitArray = new BitArray(MaxToken);");
					writer.WriteLine("\t\t\tforeach (int val in values) {");
					writer.WriteLine("\t\t\tbitArray[val] = true;");
					writer.WriteLine("\t\t\t}");
					writer.WriteLine("\t\t\treturn bitArray;");
					writer.WriteLine("\t\t}");
					foreach (var pair in sets) {
						StringBuilder builder = new StringBuilder();
						PrintList(pair.Value, builder, sets, specialChars);
						writer.WriteLine("\t\tpublic static BitArray {0} = NewSet({1});", pair.Key, builder.ToString());
					}
					writer.WriteLine();
				}

				// write token number --> string function.
				writer.WriteLine("\t\tstatic string[] tokenList = new string[] {");

				writer.WriteLine("\t\t\t// ----- terminal classes -----");
				foreach (string terminal in terminals)
					writer.WriteLine("\t\t\t\"<{0}>\",", terminal);

				writer.WriteLine("\t\t\t// ----- special character -----");
				foreach (string specialChar in specialChars.Values)
					writer.WriteLine("\t\t\t{0},", specialChar);

				writer.WriteLine("\t\t\t// ----- keywords -----");
				foreach (string keyword in keywords)
					writer.WriteLine("\t\t\t\"{0}\",", keyword);

				writer.WriteLine("\t\t};");

				writer.WriteLine("\t\tpublic static string GetTokenString(int token)");
				writer.WriteLine("\t\t{");
				writer.WriteLine("\t\t\tif (token >= 0 && token < tokenList.Length) {");
				writer.WriteLine("\t\t\t\treturn tokenList[token];");
				writer.WriteLine("\t\t\t}");
				writer.WriteLine("\t\t\tthrow new System.NotSupportedException(\"Unknown token:\" + token);");
				writer.WriteLine("\t\t}");

				writer.WriteLine("\t}");
				writer.WriteLine("}");
			}
		}

		static void PrintList(string[] value, StringBuilder builder, Dictionary<string, string[]> sets, Dictionary<string, string> specialChars)
		{
			for (int i = 0; i < value.Length; i++) {
				string item = value[i];
				if (Regex.IsMatch(item, "\\\"(\\w+)\\\"")) // keywords
					builder.Append(UpperCaseFirst(item.Trim('"', ' ', '\t')));
				else if (Regex.IsMatch(item, "\\\"(\\W+)\\\"")) // special chars
					builder.Append(specialChars.Keys.ElementAt(specialChars.Values.FindIndex(it => item == it)));
				else if (Regex.IsMatch(item, "@(\\w+)")) // other list
					PrintList(sets[item.Substring(1)], builder, sets, specialChars);
				else
					builder.Append(item);
				if (i + 1 < value.Length)
					builder.Append(", ");
			}
		}
		
		static void GenerateKeywords(Dictionary<string, string> properties, List<string> keywords)
		{
			string sourceDir = Path.Combine(baseDir, "Keywords.cs");
			using (StreamWriter writer = new StreamWriter(new FileStream(sourceDir, FileMode.Create))) {
				writer.WriteLine("// this file was autogenerated by a tool.");
				writer.WriteLine("using System;");
				writer.WriteLine();
				writer.WriteLine("namespace {0}", properties["Namespace"]);
				writer.WriteLine("{");
				writer.WriteLine("\tpublic static class Keywords");
				writer.WriteLine("\t{");
				writer.WriteLine("\t\tstatic readonly string[] keywordList = {");
				for (int i = 0; i < keywords.Count; i++) {
					writer.Write("\t\t\t\"{0}\"", properties["UpperCaseKeywords"] == "True" ? keywords[i].ToUpperInvariant() : keywords[i]);
					if (i + 1 < keywords.Count)
						writer.Write(",");
					writer.WriteLine();
				}
				writer.WriteLine("\t\t};");
				writer.WriteLine("\t\t");
				writer.WriteLine("\t\tstatic LookupTable keywords = new LookupTable({0});", properties["UpperCaseKeywords"] == "True" ? "false" : "true");
				writer.WriteLine("\t\t");
				writer.WriteLine("\t\tstatic Keywords()");
				writer.WriteLine("\t\t{");
				writer.WriteLine("\t\t\tfor (int i = 0; i < keywordList.Length; ++i) {");
				writer.WriteLine("\t\t\t\tkeywords[keywordList[i]] = i + Tokens.{0};", UpperCaseFirst(keywords[0]));
				writer.WriteLine("\t\t\t}");
				writer.WriteLine("\t\t}");
				writer.WriteLine("\t\t");
				writer.WriteLine("\t\tpublic static int GetToken(string keyword)");
				writer.WriteLine("\t\t{");
				writer.WriteLine("\t\t\treturn keywords[keyword];");
				writer.WriteLine("\t\t}");
				writer.WriteLine("\t\t");
				writer.WriteLine("\t\tpublic static bool IsNonIdentifierKeyword(string word)");
				writer.WriteLine("\t\t{");
				writer.WriteLine("\t\t\tint token = GetToken(word);");
				writer.WriteLine("\t\t\tif (token < 0)");
				writer.WriteLine("\t\t\t\treturn false;");
				writer.WriteLine("\t\t\treturn !Tokens.IdentifierTokens[token];");
				writer.WriteLine("\t\t}");

				writer.WriteLine("\t}");
				writer.WriteLine("}");

				writer.Close();
			}
		}

		#region input
		static void ReadFile(Dictionary<string, string> properties, Dictionary<string, string[]> sets,
		                     List<string> keywords, List<string> terminals, Dictionary<string, string> specialChars)
		{
			string sourceDir = Path.Combine(baseDir, "KeywordList.txt");
			
			using (StreamReader reader = new StreamReader(new FileStream(sourceDir, FileMode.Open))) {
				string line = reader.ReadLine();
				while (line != null) {
					ReadProperty(properties, line);
					ReadKeyword(keywords, line);
					ReadSet(sets, line);
					ReadTerminalSymbol(terminals, line);
					ReadSpecialChar(specialChars, line);
					line = reader.ReadLine();
				}
				reader.Close();
			}
		}

		static void ReadProperty(Dictionary<string, string> properties, string line)
		{
			// properties form: $PROPERTY = "VALUE"
			Match match = Regex.Match(line, @"^\s*\$(\w+)\s*=\s*(\S+)\s*$");
			
			if (match.Success) {
				properties.Add(match.Groups[1].Value, match.Groups[2].Value);
			}
		}

		static void ReadKeyword(List<string> keywords, string line)
		{
			// special keywords form: "VALUE"
			Match match = Regex.Match(line, "^\\s*\\\"(\\S+)\\s*\\\"\\s*$");
			
			if (match.Success) {
				keywords.Add(match.Groups[1].Value);
			}
		}

		static void ReadSet(Dictionary<string, string[]> sets, string line)
		{
			// sets form: NAME(comma separated list)
			Match match = Regex.Match(line, @"^\s*(\w+)\s*\((.*)\)\s*$");
			
			if (match.Success) {
				sets.Add(
					match.Groups[1].Value,
					match.Groups[2].Value.Split(new[] {", "}, StringSplitOptions.None)
				);
			}
		}

		static void ReadTerminalSymbol(List<string> terminals, string line)
		{
			// special terminal classes form: name
			Match match = Regex.Match(line, @"^\s*(\w+)\s*$");
			
			if (match.Success) {
				terminals.Add(match.Groups[1].Value);
			}
		}

		static void ReadSpecialChar(Dictionary<string, string> specialChars, string line)
		{
			// special characters form: name = "VALUE"
			Match match = Regex.Match(line, @"^\s*(\w+)\s*=\s*(\S+)\s*$");
			
			if (match.Success) {
				specialChars.Add(match.Groups[1].Value, match.Groups[2].Value);
			}
		}
		#endregion
		
		#region helpers
		static string UpperCaseFirst(string keyword)
		{
			return char.ToUpperInvariant(keyword[0]) + keyword.Substring(1);
		}
		
		static void WriteToken(this StreamWriter writer, string tokenName, ref int tokenValue)
		{
			string formattedName = UpperCaseFirst(tokenName).PadRight(20);
			if (tokenName == "GetType" || tokenName.ToLowerInvariant() == "equals")
				writer.WriteLine("\t\tnew public const int {0} = {1};", formattedName, tokenValue);
			else
				writer.WriteLine("\t\tpublic const int {0} = {1};", formattedName, tokenValue);
			tokenValue++;
		}
		
		static int FindIndex<T>(this IEnumerable<T> items, Func<T, bool> f)
		{
			int index = -1;
			foreach (T item in items) {
				index++;
				if (f(item))
					return index;
			}
			
			return -1;
		}
		#endregion
	}
}
