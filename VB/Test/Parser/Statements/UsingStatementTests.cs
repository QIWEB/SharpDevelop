﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.VB.Parser;
using ICSharpCode.NRefactory.VB.Dom;

namespace ICSharpCode.NRefactory.VB.Tests.Dom
{
	[TestFixture]
	public class UsingStatementTests
	{
		#region VB.NET
		[Test]
		public void VBNetUsingStatementTest()
		{
			string usingText = @"
Using nf As New System.Drawing.Font(""Arial"", 12.0F, FontStyle.Bold)
        c.Font = nf
        c.Text = ""This is 12-point Arial bold""
End Using";
			UsingStatement usingStmt = ParseUtil.ParseStatement<UsingStatement>(usingText);
			// TODO : Extend test.
		}
		[Test]
		public void VBNetUsingStatementTest2()
		{
			string usingText = @"
Using nf As Font = New Font()
	Bla(nf)
End Using";
			UsingStatement usingStmt = ParseUtil.ParseStatement<UsingStatement>(usingText);
			// TODO : Extend test.
		}
		[Test]
		public void VBNetUsingStatementTest3()
		{
			string usingText = @"
Using nf As New Font(), nf2 As New List(Of Font)(), nf3 = Nothing
	Bla(nf)
End Using";
			UsingStatement usingStmt = ParseUtil.ParseStatement<UsingStatement>(usingText);
			// TODO : Extend test.
		}
		#endregion
	}
}
