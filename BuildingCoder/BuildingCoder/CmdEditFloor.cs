#region Header
//
// CmdEditFloor.cs - read existing floor geometry and create a new floor
//
// Copyright (C) 2008-2021 by Jeremy Tammik,
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
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  class CmdEditFloor : IExternalCommand
  {
    /// The example below shows how to use Floor.Create 
    /// method to create a new Floor with a specified 
    /// elevation on a level using a geometry profile 
    /// and a floor type from Revit 2022 onwards. 
    /// It shows how to adapt your old code using the
    /// NewFloor and NewSlab methods, which became 
    /// obsolete with Revit 2022.
    /// In this sample, the geometry profile is a 
    /// CurveLoop of lines; you can also use arcs, 
    /// ellipses and splines.
    Floor CreateFloorAtElevation(
      Document document,
      double elevation )
    {
      // Get a floor type for floor creation
      // You must provide a valid floor type (unlike the 
      // obsolete NewFloor and NewSlab methods).

      ElementId floorTypeId = Floor.GetDefaultFloorType(
        document, false );

      // Get a level
      // You must provide a valid level (unlike the 
      // obsolete NewFloor and NewSlab methods).

      double offset;
      ElementId levelId = Level.GetNearestLevelId(
        document, elevation, out offset );

      // Build a floor profile for the floor creation

      XYZ first = new XYZ( 0, 0, 0 );
      XYZ second = new XYZ( 20, 0, 0 );
      XYZ third = new XYZ( 20, 15, 0 );
      XYZ fourth = new XYZ( 0, 15, 0 );
      CurveLoop profile = new CurveLoop();
      profile.Append( Line.CreateBound( first, second ) );
      profile.Append( Line.CreateBound( second, third ) );
      profile.Append( Line.CreateBound( third, fourth ) );
      profile.Append( Line.CreateBound( fourth, first ) );

      // The elevation of the curve loops is not taken 
      // into account (unlike the obsolete NewFloor and 
      // NewSlab methods).
      // If the default elevation is not what you want, 
      // you need to set it explicitly.

      var floor = Floor.Create( document, new List<CurveLoop> {
        profile }, floorTypeId, levelId );

      Parameter param = floor.get_Parameter(
        BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM );

      param.Set( offset );

      return floor;
    }

    #region Super simple floor creation
#if BEFORE_FLOOR_CREATE_METHOD
    Result Execute2(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;

      using( Transaction tx = new Transaction( doc ) )
      {
        tx.Start( "Create a Floor" );

        int n = 4;
        XYZ[] points = new XYZ[ n ];
        points[ 0 ] = XYZ.Zero;
        points[ 1 ] = new XYZ( 10.0, 0.0, 0.0 );
        points[ 2 ] = new XYZ( 10.0, 10.0, 0.0 );
        points[ 3 ] = new XYZ( 0.0, 10.0, 0.0 );

        // Code for Revit 2021 using CurveArray:

        CurveArray curve = new CurveArray();

        for( int i = 0; i < n; i++ )
        {
          Line line = Line.CreateBound( points[ i ],
            points[ (i < n - 1) ? i + 1 : 0 ] );

          curve.Append( line );
        }

        doc.Create.NewFloor( curve, true ); // 2021

        tx.Commit();
      }
      return Result.Succeeded;
    }
#endif // BEFORE_FLOOR_CREATE_METHOD
    #endregion // Super simple floor creation

    /// <summary>
    /// Return the uppermost horizontal face
    /// of a given "horizontal" solid object
    /// such as a floor slab. Currently only
    /// supports planar faces.
    /// </summary>
    PlanarFace GetTopFace( Solid solid )
    {
      PlanarFace topFace = null;
      FaceArray faces = solid.Faces;
      foreach( Face f in faces )
      {
        PlanarFace pf = f as PlanarFace;
        if( null != pf
          && Util.IsHorizontal( pf ) )
        {
          if( (null == topFace)
            || (topFace.Origin.Z < pf.Origin.Z) )
          {
            topFace = pf;
          }
        }
      }
      return topFace;
    }

    #region Attempt to include inner loops
#if ATTEMPT_TO_INCLUDE_INNER_LOOPS
    /// <summary>
    /// Convert an EdgeArrayArray to a CurveArray,
    /// possibly including multiple loops.
    /// All non-linear segments are approximated by
    /// the edge curve tesselation.
    /// </summary>
    CurveArray Convert( EdgeArrayArray eaa )
    {
      CurveArray ca = new CurveArray();
      List<XYZ> pts = new List<XYZ>();

      XYZ q;
      string s;
      int iLoop = 0;

      foreach( EdgeArray ea in eaa )
      {
        q = null;
        s = string.Empty;
        pts.Clear();

        foreach( Edge e in ea )
        {
          IList<XYZ> a = e.Tessellate();
          bool first = true;
          //XYZ p0 = null;

          foreach( XYZ p in a )
          {
            if( first )
            {
              if( null == q )
              {
                s += Util.PointString( p );
                pts.Add( p );
              }
              else
              {
                Debug.Assert( p.IsAlmostEqualTo( q ), "expected connected sequential edges" );
              }
              first = false;
              //p0 = p;
              q = p;
            }
            else
            {
              s += " --> " + Util.PointString( p );
              //ca.Append( Line.get_Bound( q, p ) );
              pts.Add( p );
              q = p;
            }
          }
          //ca.Append( Line.get_Bound( q, p0 ) );
        }

        Debug.Print( "{0}: {1}", iLoop++, s );

        // test case: break after first edge loop,
        // which we assume to be the outer:

        //break;

        {
          // try reversing all the inner loops:

          if( 1 < iLoop )
          {
            pts.Reverse();
          }

          bool first = true;

          foreach( XYZ p in pts )
          {
            if( first )
            {
              first = false;
            }
            else
            {
              ca.Append( Line.get_Bound( q, p ) );
            }
            q = p;
          }
        }
      }
      return ca;
    }
#endif // ATTEMPT_TO_INCLUDE_INNER_LOOPS
    #endregion // Attempt to include inner loops

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;

      // Retrieve selected floors, or all floors, if nothing is selected:

      List<Element> floors = new List<Element>();
      if( !Util.GetSelectedElementsOrAll(
        floors, uidoc, typeof( Floor ) ) )
      {
        Selection sel = uidoc.Selection;
        message = (0 < sel.GetElementIds().Count)
          ? "Please select some floor elements."
          : "No floor elements found.";
        return Result.Failed;
      }

      // Determine top face of each selected floor:

      int nNullFaces = 0;
      List<Face> topFaces = new List<Face>();
      Options opt = app.Application.Create.NewGeometryOptions();

      foreach( Floor floor in floors )
      {
        GeometryElement geo = floor.get_Geometry( opt );

        //GeometryObjectArray objects = geo.Objects; // 2012

        foreach( GeometryObject obj in geo )
        {
          Solid solid = obj as Solid;
          if( solid != null )
          {
            PlanarFace f = GetTopFace( solid );
            if( null == f )
            {
              Debug.WriteLine(
                Util.ElementDescription( floor )
                + " has no top face." );
              ++nNullFaces;
            }
            topFaces.Add( f );
          }
        }
      }

      using( Transaction t = new Transaction( doc ) )
      {
        t.Start( "Create Model Lines and Floor" );

        // Create new floors from the top faces found.
        // Before creating the new floor, we would obviously
        // apply whatever modifications are required to the
        // new floor profile:

        Autodesk.Revit.Creation.Application creApp = app.Application.Create;
        Autodesk.Revit.Creation.Document creDoc = doc.Create;

        int i = 0;
        int n = topFaces.Count - nNullFaces;

        Debug.Print(
          "{0} top face{1} found.",
          n, Util.PluralSuffix( n ) );

        foreach( Face f in topFaces )
        {
          Floor floor = floors[ i++ ] as Floor;

          if( null != f )
          {
            EdgeArrayArray eaa = f.EdgeLoops;

            // Code for Revit 2021 and earlier:

            //CurveArray profile; // 2021

            #region Attempt to include inner loops
#if ATTEMPT_TO_INCLUDE_INNER_LOOPS
          bool use_original_loops = true;
          if( use_original_loops )
          {
            profile = Convert( eaa );
          }
          else
#endif // ATTEMPT_TO_INCLUDE_INNER_LOOPS
            #endregion // Attempt to include inner loops

            //{
            //  profile = new CurveArray();

            //  // Only use first edge array,
            //  // the outer boundary loop,
            //  // skip the further items
            //  // representing holes:

            //  EdgeArray ea = eaa.get_Item( 0 );
            //  foreach ( Edge e in ea )
            //  {
            //    IList<XYZ> pts = e.Tessellate();
            //    int m = pts.Count;
            //    XYZ p = pts[0];
            //    XYZ q = pts[m - 1];
            //    Line line = Line.CreateBound( p, q );
            //    profile.Append( line );
            //  }
            //}

            List<CurveLoop> loops = new List<CurveLoop>(); // 2022

            {
              CurveLoop loop = new CurveLoop();

              // Only use first edge array,
              // the outer boundary loop,
              // skip the further items
              // representing holes:

              EdgeArray ea = eaa.get_Item( 0 );
              foreach( Edge e in ea )
              {
                IList<XYZ> pts = e.Tessellate();
                int m = pts.Count;
                XYZ p = pts[ 0 ];
                XYZ q = pts[ m - 1 ];
                Line line = Line.CreateBound( p, q );
                loop.Append( line );
              }
              loops = new List<CurveLoop>();
              loops.Add( loop );
            }

            //Level level = floor.Level; // 2013

            //Level level = doc.GetElement( floor.LevelId )
            //  as Level; // 2014

            // In this case we have a valid floor type given.
            // In general, not that NewFloor will only accept 
            // floor types whose IsFoundationSlab predicate
            // is false.

            //floor = creDoc.NewFloor( profile, // 2021
            //  floor.FloorType, level, true );

            floor = Floor.Create( doc, loops,
              floor.FloorType.Id, floor.LevelId ); // 2022

            XYZ v = new XYZ( 5, 5, 0 );

            //doc.Move( floor, v ); // 2011

            ElementTransformUtils.MoveElement( doc, floor.Id, v ); // 2012
          }
        }
        t.Commit();
      }
      return Result.Succeeded;
    }

    #region Set Floor Level and Offset
    void SetFloorLevelAndOffset( Document doc )
    {
      // Pick first floor found

      Floor floor
        = new FilteredElementCollector( doc )
          .WhereElementIsNotElementType()
          .OfCategory( BuiltInCategory.OST_Floors )
          .OfClass( typeof( Floor ) )
          .FirstElement() as Floor;

      // Get first level not used by floor

      int levelIdInt = floor.LevelId.IntegerValue;

      Element level
        = new FilteredElementCollector( doc )
          .WhereElementIsNotElementType()
          .OfCategory( BuiltInCategory.OST_Levels )
          .OfClass( typeof( Level ) )
          .FirstOrDefault<Element>( e
            => e.Id.IntegerValue.Equals(
              levelIdInt ) );

      if( null != level )
      {
        // from https://forums.autodesk.com/t5/revit-api-forum/changing-the-level-id-and-offset-height-of-floors/m-p/8714247

        Parameter p = floor.get_Parameter(
          BuiltInParameter.LEVEL_PARAM );

        Parameter p1 = floor.get_Parameter(
          BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM );

        using( Transaction tx = new Transaction( doc ) )
        {
          tx.Start( "Set Floor Level" );
          p.Set( level.Id ); // set new level Id
          p1.Set( 2 ); // set new offset from level
          tx.Commit();
        }
      }
    }
    #endregion // Set Floor Level and Offset

    #region SketchEditScope sample
    class SketchEditScopeSample
    {
      // Here's a snippet.
      // It was uploaded to the preview release:
      // https://feedback.autodesk.com/project/forum/thread.html?cap=cb0fd5af18bb49b791dfa3f5efc[…]01b3deb82a76}&topid={9C3D609F-0A86-4766-BB12-6315F49BCF03}
      /*
       * Sample - Edit Floor Sketch.cs
       * Created by SharpDevelop.
       * User: t_matva
       * Date: 11/17/2020
       * Time: 11:18 AM
       * 
       * To change this template use Tools | Options | Coding | Edit Standard Headers.
       */
      //using System;
      //using Autodesk.Revit.UI;
      //using Autodesk.Revit.DB;
      //using Autodesk.Revit.UI.Selection;
      //using System.Collections.Generic;
      //using System.Linq;
      //​
      //namespace Sample
      //{
      //    [Autodesk.Revit.Attributes.Transaction( Autodesk.Revit.Attributes.TransactionMode.Manual )]
      //    [Autodesk.Revit.DB.Macros.AddInId( "994A64E6-839B-4C1F-B473-1E7C614A5455" )]
      //    public partial class ThisDocument
      //    {
      //      private void Module_Startup( object sender, EventArgs e )
      //      {
      //      }
      //​
      //      private void Module_Shutdown( object sender, EventArgs e )
      //      {
      //      }
      //​
      //      #region Revit Macros generated code
      //      private void InternalStartup()
      //      {
      //        this.Startup += new System.EventHandler( Module_Startup );
      //        this.Shutdown += new System.EventHandler( Module_Shutdown );
      //      }
      //      #endregion

      public void CreateFloor( Document doc )
      {
        Curve left = Line.CreateBound( new XYZ( 0, 0, 0 ), new XYZ( 0, 100, 0 ) );
        Curve upper = Line.CreateBound( new XYZ( 0, 100, 0 ), new XYZ( 100, 100, 0 ) );
        Curve right = Line.CreateBound( new XYZ( 100, 100, 0 ), new XYZ( 100, 0, 0 ) );
        Curve lower = Line.CreateBound( new XYZ( 100, 0, 0 ), new XYZ( 0, 0, 0 ) );

        CurveLoop floorProfile = new CurveLoop();
        floorProfile.Append( left );
        floorProfile.Append( upper );
        floorProfile.Append( right );
        floorProfile.Append( lower );

        ElementId levelId = Level.GetNearestLevelId( doc, 0.0 );

        using( Transaction transaction = new Transaction( doc ) )
        {
          transaction.Start( "Create floor" );
          ElementId floorTypeId = Floor.GetDefaultFloorType( doc, false );
          Floor floor = Floor.Create( doc,
            new List<CurveLoop>() { floorProfile },
            floorTypeId, levelId );
          transaction.Commit();
        }
      }

      // Find a line in a sketch, delete it and create an arc in its place.
      public void ReplaceBoundaryLine( Document doc )
      {
        FilteredElementCollector floorCollector
          = new FilteredElementCollector( doc )
            .WhereElementIsNotElementType()
            .OfCategory( BuiltInCategory.OST_Floors )
            .OfClass( typeof( Floor ) );

        Floor floor = floorCollector.FirstOrDefault() as Floor;
        if( floor == null )
        {
          TaskDialog.Show( "Error", "doc does not contain a floor." );
          return;
        }

        Sketch sketch = doc.GetElement( floor.SketchId ) as Sketch;
        Line line = null;
        foreach( CurveArray curveArray in sketch.Profile )
        {
          foreach( Curve curve in curveArray )
          {
            line = curve as Line;
            if( line != null )
            {
              break;
            }
          }
          if( line != null )
          {
            break;
          }
        }

        if( line == null )
        {
          TaskDialog.Show( "Error",
            "Sketch does not contain a straight line." );
          return;
        }

        // Start a sketch edit scope
        SketchEditScope sketchEditScope = new SketchEditScope( doc,
          "Replace line with an arc" );

        sketchEditScope.Start( sketch.Id );

        using( Transaction transaction = new Transaction( doc,
          "Modify sketch" ) )
        {
          transaction.Start();

          // Create arc
          XYZ normal = line.Direction.CrossProduct( XYZ.BasisZ ).Normalize().Negate();
          XYZ middle = line.GetEndPoint( 0 ).Add( line.Direction.Multiply( line.Length / 2 ) );
          Curve arc = Arc.Create( line.GetEndPoint( 0 ), line.GetEndPoint( 1 ),
            middle.Add( normal.Multiply( 20 ) ) );

          // Remove element referenced by the found line. 
          doc.Delete( line.Reference.ElementId );

          // Model curve creation automatically puts the curve 
          // into the sketch, if sketch edit scope is running.

          doc.Create.NewModelCurve( arc, sketch.SketchPlane );

          transaction.Commit();
        }
        sketchEditScope.Commit( new FailuresPreprocessor() );
      }
      /// <summary>
      /// Add new profile to the sketch.
      /// </summary>
      public void MakeHole( Document doc )
      {
        FilteredElementCollector floorCollector
          = new FilteredElementCollector( doc )
            .WhereElementIsNotElementType()
            .OfCategory( BuiltInCategory.OST_Floors )
            .OfClass( typeof( Floor ) );

        Floor floor = floorCollector.FirstOrDefault() as Floor;
        if( floor == null )
        {
          TaskDialog.Show( "Error", "Document does not contain a floor." );
          return;
        }

        Sketch sketch = doc.GetElement( floor.SketchId ) as Sketch;
        // Create a circle inside the floor
        // Start a sketch edit scope
        SketchEditScope sketchEditScope = new SketchEditScope( doc,
          "Add profile to the sketch" );
        sketchEditScope.Start( sketch.Id );

        using( Transaction transaction = new Transaction( doc,
          "Make a hole" ) )
        {
          transaction.Start();
          // Create and add an ellipse
          Curve circle = Ellipse.CreateCurve( new XYZ( 50, 50, 0 ),
            10, 10, XYZ.BasisX, XYZ.BasisY, 0, 2 * Math.PI );

          // Model curve creation automatically puts the curve 
          // into the sketch, if sketch edit scope is running.

          doc.Create.NewModelCurve( circle, sketch.SketchPlane );
          transaction.Commit();
        }
        sketchEditScope.Commit( new FailuresPreprocessor() );
      }
    }

    public class FailuresPreprocessor : IFailuresPreprocessor
    {
      public FailureProcessingResult PreprocessFailures(
        FailuresAccessor failuresAccessor )
      {
        return FailureProcessingResult.Continue;
      }
    }
    #endregion // SketchEditScope sample
  }
}
