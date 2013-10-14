#region Header
//
// CmdOmniClassParams.cs - extract OmniClass
// parameter data from all elements
//
// Copyright (C) 2009-2013 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdOmniClassParams : IExternalCommand
  {
    BuiltInParameter _bipCode
      = BuiltInParameter.OMNICLASS_CODE;

    BuiltInParameter _bipDesc
      = BuiltInParameter.OMNICLASS_DESCRIPTION;

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      Document doc = app.ActiveUIDocument.Document;

#if _2010
      List<Element> set = new List<Element>();

      ParameterFilter f
        = app.Create.Filter.NewParameterFilter(
          _bipCode,
          CriteriaFilterType.NotEqual,
          string.Empty );

      ElementIterator it = doc.get_Elements( f );
#endif

      using( StreamWriter sw
        = File.CreateText( "C:/omni.txt" ) )
      {
        FilteredElementCollector collector = new FilteredElementCollector( doc );
        collector.WhereElementIsNotElementType();
        // in 2011, we should probably add some more quick filters here ...
        // and make use of something like:
        //ParameterValueProvider provider = new ParameterValueProvider( new ElementId( Bip.SystemType ) );
        //FilterStringRuleEvaluator evaluator = new FilterStringEquals();
        //string ruleString = ParameterValue.SupplyAir;
        //FilterRule rule = new FilterStringRule( provider, evaluator, ruleString, false );
        //ElementParameterFilter filter = new ElementParameterFilter( rule );
        //collector.WherePasses( filter );

        foreach( Element e in collector )
        {
          Parameter p = e.get_Parameter( _bipCode );
          if( null != p )
          {
            sw.WriteLine( string.Format(
              "{0} code {1} desc {2}",
              Util.ElementDescription( e ),
              p.AsString(),
              e.get_Parameter( _bipDesc ).AsString() ) );
          }
        }
        sw.Close();
      }
      return Result.Failed;
    }
  }
}
