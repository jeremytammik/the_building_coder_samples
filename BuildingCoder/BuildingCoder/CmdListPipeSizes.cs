#region Header
//
// CmdListPipeSizes.cs - list pipe sizes in a project
//
// Copyright (C) 2015-2019 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using System;
using System.IO;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdListPipeSizes : IExternalCommand
  {
    const string _filename = "C:/pipesizes.txt";

    string FootToMmString( double a )
    {
      return Util.FootToMm( a )
        .ToString( "0.##" )
        .PadLeft( 8 );
    }

    /// <summary>
    /// List all the pipe segment sizes in the given document.
    /// </summary>
    /// <param name="doc"></param>
    void GetPipeSegmentSizes(
      Document doc )
    {
      FilteredElementCollector segments
        = new FilteredElementCollector( doc )
          .OfClass( typeof( Segment ) );

      using( StreamWriter file = new StreamWriter(
        _filename, true ) )
      {
        foreach( Segment segment in segments )
        {
          file.WriteLine( segment.Name );

          foreach( MEPSize size in segment.GetSizes() )
          {
            file.WriteLine( string.Format( "  {0} {1} {2}",
              FootToMmString( size.NominalDiameter ),
              FootToMmString( size.InnerDiameter ),
              FootToMmString( size.OuterDiameter ) ) );
          }
        }
      }
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;
      GetPipeSegmentSizes( doc );
      return Result.Succeeded;
    }
  }
}
