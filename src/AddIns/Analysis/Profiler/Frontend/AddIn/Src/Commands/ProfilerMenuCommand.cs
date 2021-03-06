﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

using ICSharpCode.Core;
using ICSharpCode.Profiler.AddIn.Views;
using ICSharpCode.Profiler.Controls;

namespace ICSharpCode.Profiler.AddIn.Commands
{
	/// <summary>
	/// Description of ProfilerMenuCommand.
	/// </summary>
	public abstract class ProfilerMenuCommand : AbstractMenuCommand
	{
		public abstract override void Run();
		
		protected virtual IEnumerable<CallTreeNodeViewModel> GetSelectedItems()
		{
			if (Owner is Shape)
				yield return (Owner as Shape).Tag as CallTreeNodeViewModel;
			else {
				var fe = TryToFindParent(typeof(QueryView)) as QueryView;
				
				if (fe != null) {
					foreach (var item in fe.SelectedItems)
						yield return item;
				}
			}
		}
		
		protected virtual ProfilerView Parent {
			get {
				return TryToFindParent(typeof(ProfilerView)) as ProfilerView;
			}
		}
		
		FrameworkElement TryToFindParent(Type type)
		{
			FrameworkElement start = Owner as FrameworkElement;
			
			if (start == null)
				return null;
			
			while (start != null && !start.GetType().Equals(type))
				start = start.Parent as FrameworkElement;
			
			return start;
		}
	}
}
