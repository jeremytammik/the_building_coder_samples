#region Header
//
// CmdLinq.cs - test linq.
//
// Copyright (C) 2009-2010 by Joel Karr and Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdLinq : IExternalCommand
  {
    class InstanceData
    {
      #region Properties
      // auto-implemented properties, cf.
      // http://msdn.microsoft.com/en-us/library/bb384054.aspx

      public Element Instance { get; set; }
      public String Param1 { get; set; }
      public bool Param2 { get; set; }
      public int Param3 { get; set; }
      #endregion

      #region Constructor
      public InstanceData( Element instance )
      {
        Instance = instance;

        ParameterMap m = Instance.ParametersMap;

        Parameter p = m.get_Item( "Param1" );
        Param1 = ( p == null ) ? string.Empty : p.AsString();

        p = m.get_Item( "Param2" );
        Param2 = ( p == null ) ? false : ( 0 != p.AsInteger() );

        p = m.get_Item( "Param3" );
        Param3 = ( p == null ) ? 0 : p.AsInteger();
      }
      #endregion
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      Document doc = app.ActiveUIDocument.Document;

      FilteredElementCollector collector
        = new FilteredElementCollector( doc );

      collector.OfClass( typeof( FamilyInstance ) );

      List<InstanceData> instanceDataList
        = new List<InstanceData>();

      foreach( Element e in collector )
      {
        instanceDataList.Add(
          new InstanceData( e ) );
      }

      string s = "value1";
      bool b = true;
      int i = 42;

      var found = from instance in instanceDataList where
        (instance.Param1.Equals( s )
        && b == instance.Param2
        && i < instance.Param3)
      select instance;

      foreach( InstanceData instance in found )
      {
        // Do whatever you would like
      }

      return Result.Failed;
    }
  }
}
