#region Header
//
// CmdWallOpenings.cs - determine wall opening side faces and report their start and end points along location line
//
// Copyright (C) 2015-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
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
  /// <summary>
  /// A simple class with two coordinates 
  /// and some other basic info.
  /// </summary>
  class WallOpening2d
  {
    //public ElementId Id { get; set; }
    public XYZ Start { get; set; }
    public XYZ End { get; set; }
    override public string ToString()
    {
      return "("
        //+ Id.ToString() + "@"
        + Util.PointString( Start ) + "-"
        + Util.PointString( End ) + ")";
    }
  }

  [Transaction( TransactionMode.Manual )]
  class CmdWallOpenings : IExternalCommand
  {
    /// <summary>
    /// Move out of wall and up from floor a bit
    /// </summary>
    const double _offset = 0.1; // feet

    /// <summary>
    /// A small number
    /// </summary>
    //const double _eps = .1e-5;

    /// <summary>
    /// Predicate: is the given number even?
    /// </summary>
    static bool IsEven( int i )
    {
      return 0 == i % 2;
    }

    /// <summary>
    /// Predicate: does the given reference refer to a surface?
    /// </summary>
    static bool IsSurface( Reference r )
    {
      return ElementReferenceType.REFERENCE_TYPE_SURFACE
        == r.ElementReferenceType;
    }

    class XyzProximityComparerNotUsed : IComparer<XYZ>
    {
      XYZ _p;

      public XyzProximityComparerNotUsed( XYZ p )
      {
        _p = p;
      }

      public int Compare( XYZ x, XYZ y )
      {
        double dx = x.DistanceTo( _p );
        double dy = y.DistanceTo( _p );
        return Util.IsEqual( dx, dy ) ? 0
          : (dx < dy ? -1 : 1);
      }
    }

    class XyzEqualityComparer : IEqualityComparer<XYZ>
    {
      public bool Equals( XYZ a, XYZ b )
      {
        return Util.IsEqual( a, b );
      }

      public int GetHashCode( XYZ a )
      {
        return Util.PointString( a ).GetHashCode();
      }
    }

    /// <summary>
    /// Retrieve all wall openings, 
    /// including at start and end of wall.
    /// </summary>
    List<WallOpening2d> GetWallOpenings(
      Wall wall,
      View3D view )
    {
      Document doc = wall.Document;
      Level level = doc.GetElement( wall.LevelId ) as Level;
      double elevation = level.Elevation;
      Curve c = (wall.Location as LocationCurve).Curve;
      XYZ wallOrigin = c.GetEndPoint( 0 );
      XYZ wallEndPoint = c.GetEndPoint( 1 );
      XYZ wallDirection = wallEndPoint - wallOrigin;
      double wallLength = wallDirection.GetLength();
      wallDirection = wallDirection.Normalize();
      UV offsetOut = _offset * new UV( wallDirection.X, wallDirection.Y );

      XYZ rayStart = new XYZ( wallOrigin.X - offsetOut.U,
        wallOrigin.Y - offsetOut.V, elevation + _offset );

      ReferenceIntersector intersector
        = new ReferenceIntersector( wall.Id,
          FindReferenceTarget.Face, view );

      IList<ReferenceWithContext> refs
        = intersector.Find( rayStart, wallDirection );

      // Extract the intersection points:
      // - only surfaces
      // - within wall length plus offset at each end
      // - sorted by proximity
      // - eliminating duplicates

      List<XYZ> pointList = new List<XYZ>( refs
        .Where<ReferenceWithContext>( r => IsSurface(
          r.GetReference() ) )
        .Where<ReferenceWithContext>( r => r.Proximity
          < wallLength + _offset + _offset )
        .OrderBy<ReferenceWithContext, double>(
          r => r.Proximity )
        .Select<ReferenceWithContext, XYZ>( r
          => r.GetReference().GlobalPoint )
        .Distinct<XYZ>( new XyzEqualityComparer() ) );

      // Check if first point is at the wall start.
      // If so, the wall does not begin with an opening,
      // so that point can be removed. Else, add it.

      XYZ q = wallOrigin + _offset * XYZ.BasisZ;

      bool wallHasFaceAtStart = Util.IsEqual(
        pointList[ 0 ], q );

      if( wallHasFaceAtStart )
      {
        pointList.RemoveAll( p
          //=> _eps > p.DistanceTo( q ) );
          => Util.IsEqual( p, q ) );
      }
      else
      {
        pointList.Insert( 0, wallOrigin );
      }

      // Check if last point is at the wall end.
      // If so, the wall does not end with an opening, 
      // so that point can be removed. Else, add it.

      q = wallEndPoint + _offset * XYZ.BasisZ;

      bool wallHasFaceAtEnd = Util.IsEqual(
        pointList.Last(), q );

      if( wallHasFaceAtEnd )
      {
        pointList.RemoveAll( p
          //=> _eps > p.DistanceTo( q ) );
          => Util.IsEqual( p, q ) );
      }
      else
      {
        pointList.Add( wallEndPoint );
      }

      int n = pointList.Count;

      Debug.Assert( IsEven( n ),
        "expected an even number of opening sides" );

      var wallOpenings = new List<WallOpening2d>(
        n / 2 );

      for( int i = 0; i < n; i += 2 )
      {
        wallOpenings.Add( new WallOpening2d
        {
          Start = pointList[ i ],
          End = pointList[ i + 1 ]
        } );
      }
      return wallOpenings;
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;

      if( null == doc )
      {
        message = "Please run this command in a valid document.";
        return Result.Failed;
      }

      View3D view = doc.ActiveView as View3D;

      if( null == view )
      {
        message = "Please run this command in a 3D view.";
        return Result.Failed;
      }

      Element e = Util.SelectSingleElementOfType(
        uidoc, typeof( Wall ), "wall", true );

      List<WallOpening2d> openings = GetWallOpenings(
        e as Wall, view );

      int n = openings.Count;

      string msg = string.Format(
        "{0} opening{1} found{2}",
        n, Util.PluralSuffix( n ),
        Util.DotOrColon( n ) );

      Util.InfoMsg2( msg, string.Join(
        "\r\n", openings ) );

      return Result.Succeeded;
    }

    #region Determine walls in linked file intersecting pipe
    /// <summary>
    /// Determine walls in linked file intersecting pipe
    /// </summary>
    public void GetWalls( UIDocument uidoc )
    {
      Document doc = uidoc.Document;

      Reference pipeRef = uidoc.Selection.PickObject(
        ObjectType.Element );

      Element pipeElem = doc.GetElement( pipeRef );

      LocationCurve lc = pipeElem.Location as LocationCurve;
      Curve curve = lc.Curve;

      ReferenceComparer reference1 = new ReferenceComparer();

      ElementFilter filter = new ElementCategoryFilter(
        BuiltInCategory.OST_Walls );

      FilteredElementCollector collector
        = new FilteredElementCollector( doc );

      Func<View3D, bool> isNotTemplate = v3 => !(v3.IsTemplate);
      View3D view3D = collector
        .OfClass( typeof( View3D ) )
        .Cast<View3D>()
        .First<View3D>( isNotTemplate );

      ReferenceIntersector refIntersector
        = new ReferenceIntersector(
          filter, FindReferenceTarget.Element, view3D );

      refIntersector.FindReferencesInRevitLinks = true;
      IList<ReferenceWithContext> referenceWithContext
        = refIntersector.Find(
          curve.GetEndPoint( 0 ),
          (curve as Line).Direction );

      IList<Reference> references
        = referenceWithContext
          .Select( p => p.GetReference() )
          .Distinct( reference1 )
          .Where( p => p.GlobalPoint.DistanceTo(
            curve.GetEndPoint( 0 ) ) < curve.Length )
          .ToList();

      IList<Element> walls = new List<Element>();
      foreach( Reference reference in references )
      {
        RevitLinkInstance instance = doc.GetElement( reference )
        as RevitLinkInstance;
        Document linkDoc = instance.GetLinkDocument();
        Element element = linkDoc.GetElement( reference.LinkedElementId );
        walls.Add( element );
      }
      TaskDialog.Show( "Count of wall", walls.Count.ToString() );
    }

    /// <summary>
    /// Compare references with linked file support.
    /// </summary>
    public class ReferenceComparer : IEqualityComparer<Reference>
    {
      public bool Equals( Reference x, Reference y )
      {
        if( x.ElementId == y.ElementId )
        {
          if( x.LinkedElementId == y.LinkedElementId )
          {
            return true;
          }
          return false;
        }
        return false;
      }

      public int GetHashCode( Reference obj )
      {
        int hashName = obj.ElementId.GetHashCode();
        int hashId = obj.LinkedElementId.GetHashCode();
        return hashId ^ hashId;
      }
    }

    /// <summary>
    /// Return a `StableRepresentation` for a linked wall's exterior face.
    /// </summary>
    public string GetFaceRefRepresentation(
      Wall wall,
      Document doc,
      RevitLinkInstance instance )
    {
      Reference faceRef = HostObjectUtils.GetSideFaces(
        wall, ShellLayerType.Exterior ).FirstOrDefault();
      Reference stRef = faceRef.CreateLinkReference( instance );
      string stable = stRef.ConvertToStableRepresentation( doc );
      return stable;
    }
    #endregion // Determine walls in linked file intersecting pipe

    #region Find Beams and Slabs intersecting Columns
    // https://forums.autodesk.com/t5/revit-api-forum/ray-projection-not-picking-up-beams/m-p/10388868
    void AdjustColumnHeightsUsingBoundingBox(
      Document doc,
      IList<ElementId> ids )
    {
      View view = doc.ActiveView;

      int allColumns = 0;
      int successColumns = 0;

      if( view is View3D )
      {
        using( Transaction tx = new Transaction( doc ) )
        {
          tx.Start( "Adjust Column Heights" );

          foreach( ElementId elemId in ids )
          {
            Element elem = doc.GetElement( elemId );

            // Check if element is column

            if( (BuiltInCategory) elem.Category.Id.IntegerValue 
              == BuiltInCategory.OST_StructuralColumns )
            {
              allColumns++;

              FamilyInstance column = elem as FamilyInstance;

              // Collect beams and slabs within bounding box

              List<BuiltInCategory> builtInCats = new List<BuiltInCategory>();
              builtInCats.Add( BuiltInCategory.OST_Floors );
              builtInCats.Add( BuiltInCategory.OST_StructuralFraming );
              ElementMulticategoryFilter beamSlabFilter 
                = new ElementMulticategoryFilter( builtInCats );

              BoundingBoxXYZ bb = elem.get_BoundingBox( view );
              Outline myOutLn = new Outline( bb.Min, bb.Max + 100 * XYZ.BasisZ );
              BoundingBoxIntersectsFilter bbFilter 
                = new BoundingBoxIntersectsFilter( myOutLn );

              FilteredElementCollector collector 
                = new FilteredElementCollector( doc )
                  .WherePasses( beamSlabFilter )
                  .WherePasses( bbFilter );

              List<Element> intersectingBeams = new List<Element>();
              List<Element> intersectingSlabs = new List<Element>();

              if( ColumnAttachment.GetColumnAttachment( 
                column, 1 ) != null )
              {
                // Change color of columns to green

                Color color = new Color( (byte) 0, (byte) 255, (byte) 0 );
                OverrideGraphicSettings ogs = new OverrideGraphicSettings();
                ogs.SetProjectionLineColor( color );
                view.SetElementOverrides( elem.Id, ogs );
              }
              else
              {
                foreach( Element e in collector )
                {
                  if( e.Category.Name == "Structural Framing" )
                  {
                    intersectingBeams.Add( e );
                  }
                  else if( e.Category.Name == "Floors" )
                  {
                    intersectingSlabs.Add( e );
                  }
                }
                if( intersectingBeams.Any() )
                {
                  Element lowestBottomElem = intersectingBeams.First();
                  foreach( Element beam in intersectingBeams )
                  {
                    BoundingBoxXYZ thisBeamBB = beam.get_BoundingBox( view );
                    BoundingBoxXYZ currentLowestBB = lowestBottomElem.get_BoundingBox( view );
                    if( thisBeamBB.Min.Z < currentLowestBB.Min.Z )
                    {
                      lowestBottomElem = beam;
                    }
                  }
                  ColumnAttachment.AddColumnAttachment( 
                    doc, column, lowestBottomElem, 1, 
                    ColumnAttachmentCutStyle.None, 
                    ColumnAttachmentJustification.Minimum, 
                    0 );
                  successColumns++;
                }
                else if( intersectingSlabs.Any() )
                {
                  Element lowestBottomElem = intersectingSlabs.First();
                  foreach( Element slab in intersectingSlabs )
                  {
                    BoundingBoxXYZ thisSlabBB = slab.get_BoundingBox( view );
                    BoundingBoxXYZ currentLowestBB = lowestBottomElem.get_BoundingBox( view );
                    if( thisSlabBB.Min.Z < currentLowestBB.Min.Z )
                    {
                      lowestBottomElem = slab;
                    }
                  }
                  ColumnAttachment.AddColumnAttachment( 
                    doc, column, lowestBottomElem, 1, 
                    ColumnAttachmentCutStyle.None, 
                    ColumnAttachmentJustification.Minimum, 
                    0 );
                  successColumns++;
                }
                else
                {
                  // Change color of columns to red

                  Color color = new Color( (byte) 255, (byte) 0, (byte) 0 );
                  OverrideGraphicSettings ogs = new OverrideGraphicSettings();
                  ogs.SetProjectionLineColor( color );
                  view.SetElementOverrides( elem.Id, ogs );
                }
              }
            }
          }
          tx.Commit();
        }
        TaskDialog.Show( "Columns Changed",
          string.Format( "{0} of {1} Columns Changed",
          successColumns, allColumns ) );
      }
      else
      {
        TaskDialog.Show( "Revit", "Run Script in 3D View." );
      }
    }

    void AdjustColumnHeightsUsingReferenceIntersector(
      Document doc,
      IList<ElementId> ids )
    {
      View3D view = doc.ActiveView as View3D;

      if( null == view )
      {
        throw new Exception( 
          "Please run this command in a 3D view." );
      }

      int allColumns = 0;
      int successColumns = 0;

      using( Transaction tx = new Transaction( doc ) )
      {
        tx.Start( "Attach Columns Tops" );

        foreach( ElementId elemId in ids )
        {
          Element elem = doc.GetElement( elemId );

          if( (BuiltInCategory) elem.Category.Id.IntegerValue 
            == BuiltInCategory.OST_StructuralColumns )
          {
            allColumns++;

            FamilyInstance column = elem as FamilyInstance;

            // Collect beams and slabs

            List<BuiltInCategory> builtInCats = new List<BuiltInCategory>();
            builtInCats.Add( BuiltInCategory.OST_Floors );
            builtInCats.Add( BuiltInCategory.OST_StructuralFraming );
            ElementMulticategoryFilter filter 
              = new ElementMulticategoryFilter( builtInCats );

            // Remove old column attachement

            if( ColumnAttachment.GetColumnAttachment( column, 1 ) != null )
            {
              ColumnAttachment.RemoveColumnAttachment( column, 1 );
            }

            BoundingBoxXYZ elemBB = elem.get_BoundingBox( view );

            XYZ elemLoc = (elem.Location as LocationPoint).Point;
            XYZ elemCenter = new XYZ( elemLoc.X, elemLoc.Y, elemLoc.Z + 0.1 );
            XYZ b1 = new XYZ( elemBB.Min.X, elemBB.Min.Y, elemBB.Min.Z + 0.1 );
            XYZ b2 = new XYZ( elemBB.Max.X, elemBB.Max.Y, elemBB.Min.Z + 0.1 );
            XYZ b3 = new XYZ( elemBB.Min.X, elemBB.Max.Y, elemBB.Min.Z + 0.1 );
            XYZ b4 = new XYZ( elemBB.Max.X, elemBB.Min.Y, elemBB.Min.Z + 0.1 );

            List<XYZ> points = new List<XYZ>( 5 );
            points.Add( b1 );
            points.Add( b2 );
            points.Add( b3 );
            points.Add( b4 );
            points.Add( elemCenter );

            ReferenceIntersector refI = new ReferenceIntersector(
              filter, FindReferenceTarget.All, view );

            XYZ rayd = XYZ.BasisZ;
            ReferenceWithContext refC = null;
            foreach( XYZ pt in points )
            {
              refC = refI.FindNearest( pt, rayd );
              if( refC != null )
              {
                break;
              }
            }

            if( refC != null )
            {
              Reference reference = refC.GetReference();
              ElementId id = reference.ElementId;
              Element e = doc.GetElement( id );

              ColumnAttachment.AddColumnAttachment( 
                doc, column, e, 1, 
                ColumnAttachmentCutStyle.None, 
                ColumnAttachmentJustification.Minimum, 
                0 );

              successColumns++;
            }
            else
            {
              // Change color of columns to red

              Color color = new Color( (byte) 255, (byte) 0, (byte) 0 );
              OverrideGraphicSettings ogs = new OverrideGraphicSettings();
              ogs.SetProjectionLineColor( color );
              view.SetElementOverrides( elem.Id, ogs );
            }
          }
        }
        tx.Commit();
      }
    }
    #endregion // Find Beams and Slabs intersecting Columns
  }
}
