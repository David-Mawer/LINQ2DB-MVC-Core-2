﻿//---------------------------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated by T4Model template for T4 (https://github.com/linq2db/linq2db).
//    Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
//---------------------------------------------------------------------------------------------------

#pragma warning disable 1591

using System;

using LinqToDB;
using LinqToDB.Mapping;

namespace LINQ2DB_MVC_Core_2.Data
{
	/// <summary>
	/// Database       : MVCLinq2DBTemplate
	/// Data Source    : (local)
	/// Server Version : 15.00.2070
	/// </summary>
	public partial class MyAppDB : LinqToDB.Data.DataConnection
	{
		public MyAppDB()
		{
			InitDataContext();
			InitMappingSchema();
		}

		public MyAppDB(string configuration)
			: base(configuration)
		{
			InitDataContext();
			InitMappingSchema();
		}

		partial void InitDataContext  ();
		partial void InitMappingSchema();
	}
}

#pragma warning restore 1591
