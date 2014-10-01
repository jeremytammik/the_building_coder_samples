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

            if( null != pf )
            {
              XYZ v = pf.Normal;

              // Errors:
              //
              // Could not create a face wall.
              //
              // Caused by using ActiveView.Level 
              // instead of ActiveView.GenLevel.
              //
              // This reference cannot be applied to a face wall.
              //
              // Caused by using this on a horizontal face.

              if( !Util.IsVertical( v ) )
              {
                FaceWall.Create(
                  doc, wallType.Id,
                  WallLocationLine.CoreCenterline,
                  f.Reference );
              }
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

    #region Original code
#if REVIT_2012_CODE
    public static void SlopedWallTest(
      ExternalCommandData revit )
    {
      Document massDoc = revit.Application.Application.NewFamilyDocument(
          @"C:\ProgramData\Autodesk\RAC 2012\Family Templates\English_I\Conceptual Mass\Mass.rft" );

      Transaction transaction = new Transaction( massDoc );
      transaction.SetName( "TEST" );
      transaction.Start();

      ExternalCommandData cdata = revit;
      Autodesk.Revit.ApplicationServices.Application app = revit.Application.Application;
      app = revit.Application.Application;

      // Create one profile
      ReferenceArray ref_ar = new ReferenceArray();

      Autodesk.Revit.DB.XYZ ptA = new XYZ( 0, 0, 0 );
      XYZ ptB = new XYZ( 0, 30, 0 );
      ModelCurve modelcurve = MakeLine( revit.Application, ptA, ptB, massDoc );
      ref_ar.Append( modelcurve.GeometryCurve.Reference );

      ptA = new XYZ( 0, 30, 0 );
      ptB = new XYZ( 2, 30, 0 );
      modelcurve = MakeLine( revit.Application, ptA, ptB, massDoc );
      ref_ar.Append( modelcurve.GeometryCurve.Reference );

      ptA = new XYZ( 2, 30, 0 );
      ptB = new XYZ( 2, 0, 0 );
      modelcurve = MakeLine( revit.Application, ptA, ptB, massDoc );
      ref_ar.Append( modelcurve.GeometryCurve.Reference );

      ptA = new XYZ( 2, 0, 0 );
      ptB = new XYZ( 0, 0, 0 );
      modelcurve = MakeLine( revit.Application, ptA, ptB, massDoc );
      ref_ar.Append( modelcurve.GeometryCurve.Reference );

      // The extrusion form direction
      XYZ direction = new XYZ( -6, 0, 50 );
      Form form = massDoc.FamilyCreate.NewExtrusionForm( true, ref_ar, direction );
      transaction.Commit();

      if( File.Exists( @"C:\TestFamily.rfa" ) )
        File.Delete( @"C:\TestFamily.rfa" );

      massDoc.SaveAs( @"C:\TestFamily.rfa" );

      if( !revit.Application.ActiveUIDocument.Document.LoadFamily( @"C:\TestFamily.rfa" ) )
        throw new Exception( "DID NOT LOAD FAMILY" );

      Family family = null;
      foreach( Element el in new FilteredElementCollector(
          revit.Application.ActiveUIDocument.Document ).WhereElementIsNotElementType().ToElements() )
      {
        if( el is Family )
        {
          if( ( (Family) el ).Name.ToUpper().Trim().StartsWith( "TEST" ) )
            family = (Family) el;
        }
      }

      FamilySymbol fs = null;
      foreach( FamilySymbol sym in family.Symbols )
        fs = sym;

      // Create a family instance.
      revit.Application.ActiveUIDocument.Document.Create.NewFamilyInstance(
          new XYZ( 0, 0, 0 ), fs, revit.Application.ActiveUIDocument.Document.ActiveView.Level,
          StructuralType.NonStructural );

      WallType wallType = null;
      foreach( WallType wt in revit.Application.ActiveUIDocument.Document.WallTypes )
      {
        if( FaceWall.IsWallTypeValidForFaceWall( revit.Application.ActiveUIDocument.Document, wt.Id ) )
        {
          wallType = wt;
          break;
        }
      }

      foreach( Element el in new FilteredElementCollector(
          revit.Application.ActiveUIDocument.Document ).WhereElementIsNotElementType().ToElements() )
      {
        if( el is FamilyInstance )
        {
          if( ( (FamilyInstance) el ).Symbol.Family.Name.ToUpper().StartsWith( "TEST" ) )
          {
            Options options = revit.Application.Application.Create.NewGeometryOptions();
            options.ComputeReferences = true;
            options.View = revit.Application.ActiveUIDocument.Document.ActiveView;
            GeometryElement geoel = el.get_Geometry( options );

            // Attempt to create a slopped wall from the geometry.
            for( int i = 0; i < geoel.Objects.Size; i++ )
            {
              if( geoel.Objects.get_Item( i ) is Solid )
              {
                Solid solid = (Solid) geoel.Objects.get_Item( i );
                for( int j = 0; j < solid.Faces.Size; j++ )
                {
                  try
                  {
                    if( solid.Faces.get_Item( i ).Reference != null )
                    {
                      FaceWall.Create( revit.Application.ActiveUIDocument.Document,
                          wallType.Id, WallLocationLine.CoreCenterline,
                          solid.Faces.get_Item( i ).Reference );
                    }
                  }
                  catch( System.Exception e )
                  {
                    System.Windows.Forms.MessageBox.Show( e.Message );
                  }
                }
              }
            }
          }
        }
      }
    }

    public static ModelCurve MakeLine( UIApplication app, XYZ ptA, XYZ ptB, Document doc )
    {
      // Create plane by the points
      Line line = app.Application.Create.NewLine( ptA, ptB, true );
      XYZ norm = ptA.CrossProduct( ptB );
      if( norm.GetLength() == 0 ) norm = XYZ.BasisZ;
      Plane plane = app.Application.Create.NewPlane( norm, ptB );
      SketchPlane skplane = doc.FamilyCreate.NewSketchPlane( plane );
      // Create line here
      ModelCurve modelcurve = doc.FamilyCreate.NewModelCurve( line, skplane );
      return modelcurve;
    }
#endif // REVIT_2012_CODE
    #endregion // Original code
  }
}
