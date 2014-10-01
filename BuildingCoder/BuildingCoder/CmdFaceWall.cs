#region Header
//
// CmdFaceWall.cs - demonstrate FaceWall.Create
//
// Create and insert a conceptual mass family instance, 
// then create sloped walls on all its faces.
//
// Copyright (C) 2014 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  class CmdFaceWall : IExternalCommand
  {
    const string _conceptual_mass_template_path 
      = "C:/ProgramData/Autodesk/RVT 2015"
      + "/Family Templates/English/Conceptual Mass"
      + "/Metric Mass.rft";

    const string _family_name = "TestFamily";

    const string _family_path = "C:/" + _family_name + ".rfa";

    static ModelCurve MakeLine(
      Document doc,
      XYZ p,
      XYZ q )
    {
      // Create plane by the points

      Line line = Line.CreateBound( p, q );
      XYZ norm = p.CrossProduct( q );
      if( norm.GetLength() == 0 ) { norm = XYZ.BasisZ; }
      Plane plane = new Plane( norm, q );

      SketchPlane skplane = SketchPlane.Create(
        doc, plane );

      // Create line

      return doc.FamilyCreate.NewModelCurve(
        line, skplane );
    }

    /// <summary>
    /// Create an extrusion form in the given
    /// conceptual mass family document.
    /// </summary>
    static void CreateMassExtrusion(
      Document doc )
    {
      using( Transaction tx = new Transaction( doc ) )
      {
        tx.Start( "Create Mass" );

        // Create profile

        ReferenceArray refar = new ReferenceArray();

        XYZ[] pts = new XYZ[] {
          new XYZ( -10, -10, 0 ), 
          new XYZ( +10, -10, 0 ), 
          new XYZ( +10, +10, 0 ), 
          new XYZ( -10, +10, 0 ) };

        int j, n = pts.Length;

        for( int i = 0; i < n; ++i )
        {
          j = i + 1;

          if( j >= n ) { j = 0; }

          // The Creator.CreateModelLine method creates 
          // pretty arbitrary sketch planes, which causes
          // the NewExtrusionForm method to fail, saying 
          // "Cannot create extrude form."

          //ModelCurve c = Creator.CreateModelLine( doc, pts[i], pts[j] );

          ModelCurve c = MakeLine( doc, pts[i], pts[j] );

          refar.Append( c.GeometryCurve.Reference );
        }

        //doc.Regenerate();

        // The extrusion form direction and length.
        // The direction must be perpendicular to the 
        // plane determined by profile. The length 
        // must be non-zero.

        XYZ direction = new XYZ( /*-6*/ 0, 0, 20 );

        Form form = doc.FamilyCreate.NewExtrusionForm( // Cannot create extrude form.
          true, refar, direction );

        tx.Commit();
      }
    }

    static void CreateFaceWalls(
      Document doc )
    {
      Application app = doc.Application;

      Document massDoc = app.NewFamilyDocument( 
        _conceptual_mass_template_path );

      CreateMassExtrusion( massDoc );

      //if( File.Exists( _family_path ) )
      //  File.Delete( _family_path );

      SaveAsOptions opt = new SaveAsOptions();
      opt.OverwriteExistingFile = true;

      massDoc.SaveAs( _family_path, opt );

      using( Transaction tx = new Transaction( doc ) )
      {
        tx.Start( "Create FaceWall" );

        if( !doc.LoadFamily( _family_path ) )
          throw new Exception( "DID NOT LOAD FAMILY" );

        Family family = new FilteredElementCollector( doc )
          .OfClass( typeof( Family ) )
          .Where<Element>( x => x.Name.Equals( _family_name ) )
          .Cast<Family>()
          .FirstOrDefault();

        FamilySymbol fs = doc.GetElement(
          family.GetFamilySymbolIds().First<ElementId>() )
            as FamilySymbol;

        // Create a family instance

        Level level = doc.ActiveView.GenLevel;

        FamilyInstance fi = doc.Create.NewFamilyInstance(
          XYZ.Zero, fs, level, StructuralType.NonStructural );

        doc.Regenerate(); // required to generate the geometry!

        // Determine wall type.

        WallType wallType = new FilteredElementCollector( doc )
          .OfClass( typeof( WallType ) )
          .Cast<WallType>()
          .Where<WallType>( x => FaceWall.IsWallTypeValidForFaceWall( doc, x.Id ) )
          .FirstOrDefault();

        // Retrieve mass element geometry.

        Options options = app.Create.NewGeometryOptions();
        options.ComputeReferences = true;

        //options.View = doc.ActiveView; // conceptual mass is not visible in default view

        GeometryElement geo = fi.get_Geometry( options );

        // Create a sloped wall from the geometry.

        foreach( GeometryObject obj in geo )
        {
          Solid solid = obj as Solid;

          if( null != solid )
          {
            foreach( Face f in solid.Faces )
            {
              Debug.Assert( null != f.Reference, 
                "we asked for references, didn't we?" );

              PlanarFace pf = f as PlanarFace;

              if( null != pf 
                && pf.Normal.IsAlmostEqualTo( 
                  XYZ.BasisX ) )
              {
                FaceWall.Create( // Could not create a face wall.
                  doc, wallType.Id, 
                  WallLocationLine.CoreCenterline,
                  f.Reference );
              }
            }
          }
        }
        tx.Commit();
      }
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;

      //CreateMassExtrusion( doc );

      CreateFaceWalls( doc );

      return Result.Succeeded;
    }
  }
}
