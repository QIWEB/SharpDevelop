﻿/*
 * Created by SharpDevelop.
 * User: Peter Forstmeier
 * Date: 20.05.2013
 * Time: 18:15
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;

using ICSharpCode.Reporting.BaseClasses;
using ICSharpCode.Reporting.DataManager.Listhandling;
using ICSharpCode.Reporting.DataSource;
using ICSharpCode.Reporting.Items;
using NUnit.Framework;

namespace ICSharpCode.Reporting.Test.DataSource
{
	[TestFixture]
	public class CollectionHandlingFixture
	{
	
		private ContributorCollection list;

		[Test]
		public void CanInitDataCollection()
		{
			var collectionSource = new CollectionSource	(list,new ReportSettings());
			Assert.That(collectionSource,Is.Not.Null);
		}
		
		
		[Test]
		public void CollectionCountIsEqualToListCount() {
			var collectionSource = new CollectionSource	(list,new ReportSettings());
			Assert.That(collectionSource.Count,Is.EqualTo(list.Count));
		}
		
		
		[Test]
		public void AvailableFieldsEqualContibutorsPropertyCount() {
			var collectionSource = new CollectionSource	(list,new ReportSettings());
			Assert.That(collectionSource.AvailableFields.Count,Is.EqualTo(6));
		}
		
		#region Grouping
		
		[Test]
		public void GroupbyOneColumn () {
			var rs = new ReportSettings();
			rs.GroupColumnCollection.Add( new GroupColumn("GroupItem",1,ListSortDirection.Ascending));
			var collectionSource = new CollectionSource	(list,rs);
			collectionSource.Bind();
		}
		
		#endregion
		
		#region Sort

			
		[Test]
		public void CreateUnsortedIndex() {
			var collectionSource = new CollectionSource	(list,new ReportSettings());
			collectionSource.Bind();
			Assert.That(collectionSource.IndexList.Count,Is.EqualTo(collectionSource.Count));
			Assert.That(collectionSource.IndexList.CurrentPosition,Is.EqualTo(-1));
		}
		
		
		[Test]
		[ExpectedException(typeof(InvalidOperationException))]
		public void SortColumnNotExist() {
			var rs = new ReportSettings();
			rs.SortColumnsCollection.Add(new SortColumn("aa",ListSortDirection.Ascending));
			var collectionSource = new CollectionSource	(list,rs);
			collectionSource.Bind();
			Assert.That(collectionSource.IndexList,Is.Not.Null);
			Assert.That(collectionSource.IndexList.Count,Is.EqualTo(0));
		}
		
		
		[Test]
		public void SortOneColumnAscending() {
			var rs = new ReportSettings();
			rs.SortColumnsCollection.Add(new SortColumn("Lastname",ListSortDirection.Ascending));
			var collectionSource = new CollectionSource	(list,rs);
			collectionSource.Bind();
			string compare = collectionSource.IndexList[0].ObjectArray[0].ToString();
			foreach (var element in collectionSource.IndexList) {
				string result = String.Format("{0} - {1}",element.ListIndex,element.ObjectArray[0]);
				Console.WriteLine(result);
				Assert.That(compare,Is.LessThanOrEqualTo(element.ObjectArray[0].ToString()));
				compare = element.ObjectArray[0].ToString();
			}
		}
		
		
		[Test]
		public void SortTwoColumnsAscending() {
			var rs = new ReportSettings();
			rs.SortColumnsCollection.Add(new SortColumn("Lastname",ListSortDirection.Ascending));
			rs.SortColumnsCollection.Add(new SortColumn("RandomInt",ListSortDirection.Ascending));
			var collectionSource = new CollectionSource	(list,rs);
			collectionSource.Bind();
			string compare = collectionSource.IndexList[0].ObjectArray[0].ToString();
			foreach (var element in collectionSource.IndexList) {
				string result = String.Format("{0} - {1} - {2}",element.ListIndex,element.ObjectArray[0],element.ObjectArray[1].ToString());
				Console.WriteLine(result);
				Assert.That(compare,Is.LessThanOrEqualTo(element.ObjectArray[0].ToString()));
				compare = element.ObjectArray[0].ToString();
			}
		}
		
		#endregion
		
		
		[SetUp]
		public void CreateList() {
			var contributorList = new ContributorsList();
			list = contributorList.ContributorCollection;
		}	
	}
}