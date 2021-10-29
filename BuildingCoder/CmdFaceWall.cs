#region Header

//
// CmdFaceWall.cs - demonstrate FaceWall.Create
//
// Create and insert a conceptual mass family instance, 
// then create sloped walls on all its faces.
//
// Copyright (C) 2014-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

#endregion // Namespaces

namespace BuildingCoder
{
    #region Automatic Walls

    // For 13642689 [Mathematical Translations]
    // https://forums.autodesk.com/t5/revit-api-forum/mathematical-translations/m-p/7580510

    [Transaction(TransactionMode.Manual)]
    public class CreateWallsAutomaticallyCommand
        : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;

            var cubes = FindCubes(doc);

            using var transaction = new Transaction(doc);
            transaction.Start("create walls");

            foreach (var cube in cubes)
            {
                var countours = FindCountors(cube)
                    .SelectMany(x => x);

                var height = cube.LookupParameter("height")
                    .AsDouble();

                foreach (var countour in countours)
                {
                    var wall = CreateWall(cube, countour,
                        height);

                    CreateDoor(wall);
                }
            }

            transaction.Commit();

            return Result.Succeeded;
        }

        private static Wall CreateWall(
            FamilyInstance cube,
            Curve curve,
            double height)
        {
            var doc = cube.Document;

            var wallTypeId = doc.GetDefaultElementTypeId(
                ElementTypeGroup.WallType);

            return Wall.Create(doc, curve.CreateReversed(),
                wallTypeId, cube.LevelId, height, 0, false,
                false);
        }

        private static void CreateDoor(Wall wall)
        {
            var locationCurve = (LocationCurve) wall.Location;

            var position = locationCurve.Curve.Evaluate(
                0.5, true);

            var document = wall.Document;

            var level = (Level) document.GetElement(
                wall.LevelId);

            var symbolId = document.GetDefaultFamilyTypeId(
                new ElementId(BuiltInCategory.OST_Doors));

            var symbol = (FamilySymbol) document.GetElement(
                symbolId);

            if (!symbol.IsActive)
                symbol.Activate();

            document.Create.NewFamilyInstance(position, symbol,
                wall, level, StructuralType.NonStructural);
        }

        private static IEnumerable<FamilyInstance> FindCubes(
            Document doc)
        {
            var collector = new FilteredElementCollector(doc);

            return collector
                .OfCategory(BuiltInCategory.OST_GenericModel)
                .OfClass(typeof(FamilyInstance))
                .OfType<FamilyInstance>()
                .Where(x => x.Symbol.FamilyName == "cube");
        }

        private static IEnumerable<CurveLoop> FindCountors(
            FamilyInstance familyInstance)
        {
            return GetSolids(familyInstance)
                .SelectMany(x => GetCountours(x,
                    familyInstance));
        }

        private static IEnumerable<Solid> GetSolids(
            Element element)
        {
            var geometry = element
                .get_Geometry(new Options
                {
                    ComputeReferences = true,
                    IncludeNonVisibleObjects = true
                });

            if (geometry == null)
                return Enumerable.Empty<Solid>();

            return GetSolids(geometry)
                .Where(x => x.Volume > 0);
        }

        private static IEnumerable<Solid> GetSolids(
            IEnumerable<GeometryObject> geometryElement)
        {
            foreach (var geometry in geometryElement)
            {
                var solid = geometry as Solid;
                if (solid != null)
                    yield return solid;

                var instance = geometry as GeometryInstance;
                if (instance != null)
                    foreach (var instanceSolid in GetSolids(
                        instance.GetInstanceGeometry()))
                        yield return instanceSolid;

                var element = geometry as GeometryElement;
                if (element != null)
                    foreach (var elementSolid in GetSolids(element))
                        yield return elementSolid;
            }
        }

        private static IEnumerable<CurveLoop> GetCountours(
            Solid solid,
            Element element)
        {
            try
            {
                var plane = Plane.CreateByNormalAndOrigin(
                    XYZ.BasisZ, element.get_BoundingBox(null).Min);

                var analyzer = ExtrusionAnalyzer.Create(
                    solid, plane, XYZ.BasisZ);

                var face = analyzer.GetExtrusionBase();

                return face.GetEdgesAsCurveLoops();
            }
            catch (InvalidOperationException)
            {
                return Enumerable.Empty<CurveLoop>();
            }
        }
    }

    #endregion // Automatic Walls

    [Transaction(TransactionMode.Manual)]
    internal class CmdFaceWall : IExternalCommand
    {
        private const string _conceptual_mass_template_path
            = "C:/ProgramData/Autodesk/RVT 2015"
              + "/Family Templates/English/Conceptual Mass"
              + "/Metric Mass.rft";

        private const string _family_name = "TestFamily";

        private const string _family_path = "C:/" + _family_name + ".rfa";

        private static ModelCurve MakeLine(
            Document doc,
            XYZ p,
            XYZ q)
        {
            // Create plane by the points

            var line = Line.CreateBound(p, q);
            var norm = p.CrossProduct(q);
            if (norm.GetLength() == 0) norm = XYZ.BasisZ;

            //Plane plane = new Plane( norm, q ); // 2016

            var plane = Plane.CreateByNormalAndOrigin(norm, q); // 2017

            var skplane = SketchPlane.Create(
                doc, plane);

            // Create line

            return doc.FamilyCreate.NewModelCurve(
                line, skplane);
        }

        /// <summary>
        ///     Create an extrusion form in the given
        ///     conceptual mass family document.
        /// </summary>
        private static void CreateMassExtrusion(
            Document doc)
        {
            using var tx = new Transaction(doc);
            tx.Start("Create Mass");

            // Create profile

            var refar = new ReferenceArray();

            var pts = new[]
            {
                new(-10, -10, 0),
                new XYZ(+10, -10, 0),
                new XYZ(+10, +10, 0),
                new XYZ(-10, +10, 0)
            };

            int j, n = pts.Length;

            for (var i = 0; i < n; ++i)
            {
                j = i + 1;

                if (j >= n) j = 0;

                // The Creator.CreateModelLine method creates 
                // pretty arbitrary sketch planes, which causes
                // the NewExtrusionForm method to fail, saying 
                // "Cannot create extrude form."

                //ModelCurve c = Creator.CreateModelLine( doc, pts[i], pts[j] );

                var c = MakeLine(doc, pts[i], pts[j]);

                refar.Append(c.GeometryCurve.Reference);
            }

            //doc.Regenerate();

            // The extrusion form direction and length.
            // The direction must be perpendicular to the 
            // plane determined by profile. The length 
            // must be non-zero.

            var direction = new XYZ( /*-6*/ 0, 0, 20);

            var form = doc.FamilyCreate.NewExtrusionForm( // Cannot create extrude form.
                true, refar, direction);

            tx.Commit();
        }

        private static void CreateFaceWalls(
            Document doc)
        {
            var app = doc.Application;

            var massDoc = app.NewFamilyDocument(
                _conceptual_mass_template_path);

            CreateMassExtrusion(massDoc);

            //if( File.Exists( _family_path ) )
            //  File.Delete( _family_path );

            var opt = new SaveAsOptions();
            opt.OverwriteExistingFile = true;

            massDoc.SaveAs(_family_path, opt);

            using var tx = new Transaction(doc);
            tx.Start("Create FaceWall");

            if (!doc.LoadFamily(_family_path))
                throw new Exception("DID NOT LOAD FAMILY");

            var family = new FilteredElementCollector(doc)
                .OfClass(typeof(Family))
                .Where(x => x.Name.Equals(_family_name))
                .Cast<Family>()
                .FirstOrDefault();

            var fs = doc.GetElement(
                    family.GetFamilySymbolIds().First())
                as FamilySymbol;

            // Create a family instance

            var level = doc.ActiveView.GenLevel;

            var fi = doc.Create.NewFamilyInstance(
                XYZ.Zero, fs, level, StructuralType.NonStructural);

            doc.Regenerate(); // required to generate the geometry!

            // Determine wall type.

            var wallType = new FilteredElementCollector(doc)
                .OfClass(typeof(WallType))
                .Cast<WallType>()
                .Where(x => FaceWall.IsWallTypeValidForFaceWall(doc, x.Id))
                .FirstOrDefault();

            // Retrieve mass element geometry.

            var options = app.Create.NewGeometryOptions();
            options.ComputeReferences = true;

            //options.View = doc.ActiveView; // conceptual mass is not visible in default view

            var geo = fi.get_Geometry(options);

            // Create a sloped wall from the geometry.

            foreach (var obj in geo)
            {
                var solid = obj as Solid;

                if (null != solid)
                    foreach (Face f in solid.Faces)
                    {
                        Debug.Assert(null != f.Reference,
                            "we asked for references, didn't we?");

                        var pf = f as PlanarFace;

                        if (null != pf)
                        {
                            var v = pf.FaceNormal;

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

                            if (!Util.IsVertical(v))
                                FaceWall.Create(
                                    doc, wallType.Id,
                                    WallLocationLine.CoreCenterline,
                                    f.Reference);
                        }
                    }
            }

            tx.Commit();
        }

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;

            //CreateMassExtrusion( doc );

            CreateFaceWalls(doc);

            return Result.Succeeded;
        }

        #region CreateFaceWallsAndMassFloors

        // By Harry Mattison, Boost Your BIM,
        // Automating the Building Maker workflow
        // https://boostyourbim.wordpress.com/2014/02/11/automating-the-building-maker-workflow/
        // Face Wall and Mass Floor creation with the Revit API
        // https://youtu.be/nHWen2_lN6U

        /// <summary>
        ///     Create face walls and mass floors on and in selected mass element
        /// </summary>
        public void CreateFaceWallsAndMassFloors(UIDocument uidoc)
        {
            var doc = uidoc.Document;

            var fi = doc.GetElement(
                    uidoc.Selection.PickObject(
                        ObjectType.Element))
                as FamilyInstance;

            var wType = new FilteredElementCollector(doc)
                .OfClass(typeof(WallType))
                .Cast<WallType>().FirstOrDefault(q
                    => q.Name == "Generic - 6\" Masonry");

            var opt = new Options();
            opt.ComputeReferences = true;

            using var t = new Transaction(doc);
            t.Start("Create Face Walls & Mass Floors");

            foreach (var solid in fi.get_Geometry(opt)
                .Where(q => q is Solid).Cast<Solid>())
            foreach (Face f in solid.Faces)
                if (FaceWall.IsValidFaceReferenceForFaceWall(
                    doc, f.Reference))
                    FaceWall.Create(doc, wType.Id,
                        WallLocationLine.CoreExterior,
                        f.Reference);

            var levels
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level));

            foreach (Level level in levels)
                MassInstanceUtils.AddMassLevelDataToMassInstance(
                    doc, fi.Id, level.Id);

            t.Commit();
        }

        #endregion // CreateFaceWallsAndMassFloors

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