#region Header
//
// CmdSteelStairBeams.cs - create a series of connected mitered steel beams for a steel stair
//
// Copyright (C) 2011-2013 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  class SteelStairs
  {
    Document _doc;

    public SteelStairs( Document doc )
    {
      _doc = doc;
    }

    public void Run()
    {
      View view = _doc.ActiveView;

      Level level = view.GenLevel;

      if( null == level )
      {
        throw new Exception(
          "No level associated with view" );
      }

      XYZ pt1 = Util.MmToFoot( new XYZ( 0, 0, 1000 ) );
      XYZ pt2 = Util.MmToFoot( new XYZ( 1000, 0, 1000 ) );
      XYZ pt3 = Util.MmToFoot( new XYZ( 2000, 0, 2500 ) );
      XYZ pt4 = Util.MmToFoot( new XYZ( 3000, 0, 2500 ) );

      FamilySymbol familySymbol = Util.FindFamilySymbol(
        _doc,
        CmdSteelStairBeams.FamilyName,
        CmdSteelStairBeams.SymbolName );

      if( familySymbol == null )
      {
        throw new Exception( "Beam Family not found" );
      }

      CreateBeam( familySymbol, level, pt1, pt2 );
      CreateBeam( familySymbol, level, pt2, pt3 );
      CreateBeam( familySymbol, level, pt3, pt4 );
    }

    FamilyInstance CreateBeam(
      FamilySymbol familySymbol,
      Level level,
      XYZ startPt,
      XYZ endPt )
    {
      StructuralType structuralType
        = StructuralType.Beam;

      //Line line = _doc.Application.Create.NewLineBound( startPt, endPt ); // 2013
      Line line = Line.CreateBound( startPt, endPt ); // 2014

      FamilyInstance beam = _doc.Create
        .NewFamilyInstance( startPt, familySymbol,
          level, structuralType );

      LocationCurve beamCurve
        = beam.Location as LocationCurve;

      if( null != beamCurve )
      {
        beamCurve.Curve = line;
      }
      return beam;
    }
  }

  [Transaction( TransactionMode.Manual )]
  class CmdSteelStairBeams : IExternalCommand
  {
    public const string FamilyName
      = "RHS-Rectangular Hollow Section";

    public const string SymbolName
      = "160x80x4RHS";

    const string _extension
      = ".rfa";

    const string _directory
      = "C:/ProgramData/Autodesk/RAC 2012/Libraries"
      + "/US Metric/Structural/Framing/Steel/";

    const string _family_path
      = _directory + FamilyName + _extension;

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Application app = uiapp.Application;
      Document doc = uidoc.Document;

      Transaction t = new Transaction( doc );

      t.Start( "Create Steel Stair Beams" );

      // Check whether the required family is loaded:

      FilteredElementCollector collector
        = new FilteredElementCollector( doc )
          .OfClass( typeof( Family ) );

      // If the family is not already loaded, do so:

      if( !collector.Any<Element>(
        e => e.Name.Equals( FamilyName ) ) )
      {
        FamilySymbol symbol;

        if( !doc.LoadFamilySymbol(
          _family_path, SymbolName, out symbol ) )
        {
          message = string.Format(
            "Unable to load '{0}' from '{1}'.",
            SymbolName, _family_path );

          t.RollBack();

          return Result.Failed;
        }
      }

      try
      {
        // Create a couple of connected beams:

        SteelStairs s = new SteelStairs( doc );

        s.Run();

        t.Commit();

        return Result.Succeeded;
      }
      catch( Exception ex )
      {
        message = ex.Message;

        t.RollBack();

        return Result.Failed;
      }
    }
  }
}

// C:\ProgramData\Autodesk\
//
// \RAC 2012\Libraries\US Metric\Structural\Framing\Steel\RHS-Rectangular Hollow Section.rfa
// \RST 2012\Libraries\US Metric\Structural\Framing\Steel\RHS-Rectangular Hollow Section.rfa
