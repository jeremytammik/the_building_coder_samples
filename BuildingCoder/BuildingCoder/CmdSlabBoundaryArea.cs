#region Header
//
// CmdSlabBoundaryArea.cs - determine
// slab boundary polygon loops and areas
//
// Copyright (C) 2008-2017 by Jeremy Tammik,
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
  class CmdSlabBoundaryArea : IExternalCommand
  {
    #region Rpthomas108 improved solution
    // In Revit API discussion forum thread
    // https://forums.autodesk.com/t5/revit-api-forum/outer-loops-of-planar-face-with-separate-parts/m-p/7461348

    public Result GetPlanarFaceOuterLoops(
        ExternalCommandData commandData,
        ref string message,
        ElementSet elements )
    {
      UIApplication IntApp = commandData.Application;
      UIDocument IntUIDoc = IntApp.ActiveUIDocument;
      if( IntUIDoc == null )
        return Result.Failed;
      Document IntDoc = IntUIDoc.Document;

      Reference R = null;
      try
      {
        R = IntUIDoc.Selection.PickObject( ObjectType.Face );
      }
      catch
      {
      }
      if( R == null )
        return Result.Cancelled;

      Element F_El = IntDoc.GetElement( R.ElementId );
      if( F_El == null )
        return Result.Failed;

      PlanarFace F = F_El.GetGeometryObjectFromReference( R )
        as PlanarFace;

      if( F == null )
        return Result.Failed;

      //Create individual CurveLoops to compare from 
      // the orginal CurveLoopArray
      //If floor has separate parts these will now be 
      // separated out into individual faces rather 
      // than one face with multiple loops.
      List<Tuple<PlanarFace, CurveLoop, int>> CLoop
        = new List<Tuple<PlanarFace, CurveLoop, int>>();

      int Ix = 0;
      foreach( CurveLoop item in F.GetEdgesAsCurveLoops() )
      {
        List<CurveLoop> CLL = new List<CurveLoop>();
        CLL.Add( item );
        //Create a solid extrusion for each CurveLoop 
        // ( we want to get the planarFace from this 
        // to use built in functionality (.PlanarFace.IsInside).
        //Would be nice if you could skip this step and 
        // create PlanarFaces directly from CuveLoops? 
        // Does not appear to be possible, I only looked 
        // in GeometryCreationUtilities.
        //Below creates geometry in memory rather than 
        // actual geometry in the document, therefore 
        // no transaction required.
        Solid S = GeometryCreationUtilities
          .CreateExtrusionGeometry( CLL, F.FaceNormal, 1 );

        foreach( Face Fx in S.Faces )
        {
          PlanarFace PFx = Fx as PlanarFace;
          if( PFx == null )
            continue;
          if( PFx.FaceNormal.IsAlmostEqualTo(
            F.FaceNormal ) )
          {
            Ix += 1;
            CLoop.Add( new Tuple<PlanarFace,
              CurveLoop, int>( PFx, item, Ix ) );
          }
        }
      }

      List<CurveLoop> OuterLoops = new List<CurveLoop>();
      //If there is more than one outerloop we know the 
      // original face has separate parts.
      //We could therefore stop the creation of floors 
      // with separate parts via posting failures etc. 
      // or more passively create a geometry checking
      // utility to identify them.
      List<CurveLoop> InnerLoops = new List<CurveLoop>();
      foreach( Tuple<PlanarFace, CurveLoop, int> item in CLoop )
      {
        //To identify an inner loop we just need to see 
        // if any of it's points are inside another face.
        //The exception to this is a loop compared to the
        // face it was taken from. This will also be 
        // considered inside as the points are on the boundary.
        //Therefore give each item an integer ID to ensure
        // it isn't self comparing. An alternative would
        // be to look for J=1 instead of J=0 below (perhaps).

        int J = CLoop.ToList().FindAll( z
          => FirstPointIsInsideFace( item.Item2, z.Item1 )
            == true && z.Item3 != item.Item3 ).Count;

        if( J == 0 )
        {
          OuterLoops.Add( item.Item2 );
        }
        else
        {
          InnerLoops.Add( item.Item2 );
        }
      }

      using( Transaction Tx = new Transaction( IntDoc,
        "Outer loops" ) )
      {
        if( Tx.Start() == TransactionStatus.Started )
        {
          SketchPlane SKP = SketchPlane.Create( IntDoc,
            Plane.CreateByThreePoints( F.Origin,
              F.Origin + F.XVector, F.Origin + F.YVector ) );

          foreach( CurveLoop Crv in OuterLoops )
          {
            foreach( Curve C in Crv )
            {
              IntDoc.Create.NewModelCurve( C, SKP );
            }
          }
          Tx.Commit();
        }
      }
      return Result.Succeeded;
    }

    public bool FirstPointIsInsideFace(
      CurveLoop CL,
      PlanarFace PFace )
    {
      Transform Trans = PFace.ComputeDerivatives(
        new UV( 0, 0 ) );
      if( CL.Count() == 0 )
        return false;
      XYZ Pt = Trans.Inverse.OfPoint(
        CL.ToList()[0].GetEndPoint( 0 ) );
      IntersectionResult Res = null;
      bool outval = PFace.IsInside(
        new UV( Pt.X, Pt.Y ), out Res );
      return outval;
    }
    #endregion // Rpthomas108 improved solution

    #region Rpthomas108 first solution searching for minimum point
    // In Revit API discussion forum thread
    // https://forums.autodesk.com/t5/revit-api-forum/is-the-first-edgeloop-still-the-outer-loop/m-p/7225379

    public static double MinU( Curve C, Face F )
    {
      return C.Tessellate()
        .Select<XYZ, IntersectionResult>( p => F.Project( p ) )
        .Min<IntersectionResult>( ir => ir.UVPoint.U );
    }

    public static double MinX( Curve C, Transform Tinv )
    {
      return C.Tessellate()
        .Select<XYZ, XYZ>( p => Tinv.OfPoint( p ) )
        .Min<XYZ>( p => p.X );
    }

    public static EdgeArray OuterLoop( Face F )
    {
      EdgeArray eaMin = null;
      EdgeArrayArray loops = F.EdgeLoops;
      double uMin = double.MaxValue;
      foreach( EdgeArray a in loops )
      {
        double uMin2 = double.MaxValue;
        foreach( Edge e in a )
        {
          double min = MinU( e.AsCurve(), F );
          if( min < uMin2 ) { uMin2 = min; }
        }
        if( uMin2 < uMin )
        {
          uMin = uMin2;
          eaMin = a;
        }
      }
      return eaMin;
    }

    public static EdgeArray PlanarFaceOuterLoop( Face F )
    {
      PlanarFace face = F as PlanarFace;
      if( face == null )
      {
        return null;
      }
      Transform T = Transform.Identity;
      T.BasisZ = face.FaceNormal;
      T.BasisX = face.XVector;
      T.BasisY = face.YVector;
      T.Origin = face.Origin;
      Transform Tinv = T.Inverse;

      EdgeArray eaMin = null;
      EdgeArrayArray loops = F.EdgeLoops;
      double uMin = double.MaxValue;
      foreach( EdgeArray a in loops )
      {
        double uMin2 = double.MaxValue;
        foreach( Edge e in a )
        {
          double min = MinX( e.AsCurve(), Tinv );
          if( min < uMin2 ) { uMin2 = min; }
        }
        if( uMin2 < uMin )
        {
          uMin = uMin2;
          eaMin = a;
        }
      }
      return eaMin;
    }
    #endregion // Rpthomas108 first solution searching for minimum point

    #region Flatten, i.e. project from 3D to 2D by dropping the Z coordinate
    /// <summary>
    /// Eliminate the Z coordinate.
    /// </summary>
    static UV Flatten( XYZ point )
    {
      return new UV( point.X, point.Y );
    }

    /// <summary>
    /// Eliminate the Z coordinate.
    /// </summary>
    static public List<UV> Flatten( List<XYZ> polygon )
    {
      double z = polygon[0].Z;
      List<UV> a = new List<UV>( polygon.Count );
      foreach( XYZ p in polygon )
      {
        Debug.Assert( Util.IsEqual( p.Z, z ),
          "expected horizontal polygon" );
        a.Add( Flatten( p ) );
      }
      return a;
    }

    /// <summary>
    /// Eliminate the Z coordinate.
    /// </summary>
    static List<List<UV>> Flatten( List<List<XYZ>> polygons )
    {
      double z = polygons[0][0].Z;
      List<List<UV>> a = new List<List<UV>>( polygons.Count );
      foreach( List<XYZ> polygon in polygons )
      {
        Debug.Assert( Util.IsEqual( polygon[0].Z, z ),
          "expected horizontal polygons" );
        a.Add( Flatten( polygon ) );
      }
      return a;
    }
    #endregion // Flatten, i.e. project from 3D to 2D by dropping the Z coordinate

    #region Two-dimensional polygon area
    /// <summary>
    /// Use the formula
    ///
    /// area = sign * 0.5 * sum( xi * ( yi+1 - yi-1 ) )
    ///
    /// to determine the winding direction (clockwise
    /// or counter) and area of a 2D polygon.
    /// Cf. also GetPolygonPlane.
    /// </summary>
    static public double GetSignedPolygonArea( List<UV> p )
    {
      int n = p.Count;
      double sum = p[0].U * ( p[1].V - p[n - 1].V ); // loop at beginning
      for( int i = 1; i < n - 1; ++i )
      {
        sum += p[i].U * ( p[i + 1].V - p[i - 1].V );
      }
      sum += p[n - 1].U * ( p[0].V - p[n - 2].V ); // loop at end
      return 0.5 * sum;
    }
    #endregion // Two-dimensional polygon area

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;

      List<Element> floors = new List<Element>();
      if( !Util.GetSelectedElementsOrAll(
        floors, uidoc, typeof( Floor ) ) )
      {
        Selection sel = uidoc.Selection;
        message = ( 0 < sel.GetElementIds().Count )
          ? "Please select some floor elements."
          : "No floor elements found.";
        return Result.Failed;
      }

      Options opt = app.Application.Create.NewGeometryOptions();

      List<List<XYZ>> polygons
        = CmdSlabBoundary.GetFloorBoundaryPolygons(
          floors, opt );

      List<List<UV>> flat_polygons
        = Flatten( polygons );

      int i = 0, n = flat_polygons.Count;
      double[] areas = new double[n];
      double a, maxArea = 0.0;
      foreach( List<UV> polygon in flat_polygons )
      {
        a = GetSignedPolygonArea( polygon );
        if( Math.Abs( maxArea ) < Math.Abs( a ) )
        {
          maxArea = a;
        }
        areas[i++] = a;
      }

      Debug.Print(
        "{0} boundary loop{1} found.",
        n, Util.PluralSuffix( n ) );

      for( i = 0; i < n; ++i )
      {
        Debug.Print(
          "  Loop {0} area is {1} square feet{2}",
          i,
          Util.RealString( areas[i] ),
          ( areas[i].Equals( maxArea )
            ? ", outer loop of largest floor slab"
            : "" ) );
      }

      using( Transaction t = new Transaction( doc ) )
      {
        t.Start( "Draw Polygons" );

        Creator creator = new Creator( doc );
        creator.DrawPolygons( polygons );

        t.Commit();
      }
      return Result.Succeeded;
    }
  }
}
