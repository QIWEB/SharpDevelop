﻿// <file>
//     <copyright see="prj:///doc/copyright.txt">2002-2005 AlphaSierraPapa</copyright>
//     <license see="prj:///doc/license.txt">GNU General Public License</license>
//     <owner name="David Srbecký" email="dsrbecky@gmail.com"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Threading;

namespace CustomSinks
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Thread.CurrentThread.Name = "Server (STA)";

			RemotingConfiguration.Configure("Server.exe.config");

			Console.WriteLine("Chat server started. Press ENTER to exit.");
			Console.ReadLine();
		}
	}
}
