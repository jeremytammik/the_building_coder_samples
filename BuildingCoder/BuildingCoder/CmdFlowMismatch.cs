#region Header
//
// CmdFlowMismatch.cs - check MEP systems for flow mismatch and unconnected parts
//
// Copyright (C) 2018 Jared @wils02 Wilson and Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  class CmdFlowMismatch : IExternalCommand
  {
    static bool IsDesirableSystemPredicate(
      MEPSystem s )
    {
      return s is MechanicalSystem || s is PipingSystem
        && !s.Name.Equals( "unassigned" )
        && 0 < s.Elements.Size;
    }

    /// <summary>
    /// Add a system mismatch entry to the string builder
    /// </summary>
    static void ReportSystemMistmatch(
      StringBuilder sb,
      Element e,
      string cnctrSys,
      string cnntdSys )
    {
      Parameter p = e.get_Parameter(
        BuiltInParameter.ALL_MODEL_MARK );

      sb.Append( string.Format( "Family instance '{0}' "
        + "has a connector {{{1}}} that is connected to a "
        + "{{{2}}} system...\n\n",
        p.AsString(), cnctrSys, cnntdSys ) );
    }

    static void FindMismatch( Document doc )
    {
      string cnntdSys = "";
      string cnctrSys = "";

      StringBuilder sb = new StringBuilder();

      FilteredElementCollector systems
        = new FilteredElementCollector( doc )
          .OfClass( typeof( MEPSystem ) );

      IEnumerable<MEPSystem> desirableSystems = systems
        .Cast<MEPSystem>()
        .Where<MEPSystem>( s
          => IsDesirableSystemPredicate( s ) );

      foreach( MEPSystem system in desirableSystems )
      {
        if( system.GetType() == typeof( PipingSystem ) )
        {
          cnntdSys = ( system as PipingSystem ).SystemType.ToString();
        }
        if( system.GetType() == typeof( MechanicalSystem ) )
        {
          cnntdSys = ( system as MechanicalSystem ).SystemType.ToString();
        }

        ElementSet mepSet = system.Elements;
        foreach( Element e in mepSet )
        {
          ConnectorSet mepCS = MepSystemSearch.GetConnectors( e );

          foreach( Connector elemConnector in mepCS )
          {
            if( elemConnector.Domain.Equals(
              Domain.DomainPiping ) )
            {
              cnctrSys = elemConnector.PipeSystemType.ToString();

              if( elemConnector.MEPSystem != null
                && elemConnector.MEPSystem.Name.Equals(
                  system.Name ) )
              {
                if( elemConnector.PipeSystemType.Equals(
                  ( system as PipingSystem ).SystemType ) )
                {
                  // Do Nothing
                }
                else
                {
                  ReportSystemMistmatch( sb, e,
                    cnctrSys, cnntdSys );
                }
              }
            }

            if( null != elemConnector.MEPSystem
              && elemConnector.Domain.Equals(
                Domain.DomainHvac ) )
            {
              cnctrSys = elemConnector.DuctSystemType.ToString();

              if( elemConnector.MEPSystem.Name.Equals(
                system.Name ) )
              {
                if( elemConnector.DuctSystemType.Equals(
                  ( system as MechanicalSystem ).SystemType ) )
                {
                  // Do Nothing
                }
                else
                {
                  ReportSystemMistmatch( sb, e, 
                    cnctrSys, cnntdSys );
                }
              }
            }
          }
        }
      }
      TaskDialog.Show( "Flow Mismatch", sb + "\n" );
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;

      FindMismatch( doc );

      return Result.Succeeded;
    }
  }
}
