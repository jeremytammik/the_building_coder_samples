#region Header

//
// CmdDimensionWallsFindRefs.cs - create dimensioning elements
// between opposing walls using FindReferencesByDirection
//
// Copyright (C) 2011-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.Exceptions;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

#endregion // Namespaces

namespace BuildingCoder
{
    /// <summary>
    ///     Dimension two opposing parallel walls.
    ///     Prompt user to select the first wall, and
    ///     the second at the point at which to create
    ///     the dimensioning. Use FindReferencesByDirection
    ///     to determine the wall face references.
    ///     Second sample solution for case 1263303 [Case
    ///     Number 1263071 [Revit 2011 Dimension Wall]].
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    internal class CmdDimensionWallsFindRefs : IExternalCommand
    {
        private const string _prompt
            = "Please select two parallel straight walls"
              + " with a partial projected overlap.";

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var app = uiapp.Application;
            var doc = uidoc.Document;

            // Select two walls and the dimension line point:

            var sel = uidoc.Selection;
            var refs = new ReferenceArray();

            try
            {
                ISelectionFilter f
                    = new JtElementsOfClassSelectionFilter<Wall>();

                refs.Append(sel.PickObject(
                    ObjectType.Element, f,
                    "Please select first wall"));

                refs.Append(sel.PickObject(
                    ObjectType.Element, f,
                    "Please pick dimension line "
                    + "point on second wall"));

                //rFace = sel.PickObject( ObjectType.Face,
                //  "Please select face on second wall at dimension line point" );
                //
                //rPoint = sel.PickObject( ObjectType.PointOnElement,
                //  "Please select point on first wall" );
            }
            catch (OperationCanceledException)
            {
                message = "No two walls selected";
                return Result.Failed;
            }

            // Ensure the two selected walls are straight and
            // parallel; determine their mutual normal vector
            // and a point on each wall for distance
            // calculations:

            var walls = new Wall[2];
            var ids = new List<int>(2);
            var pts = new XYZ[2];
            var lines = new Line[2];
            IntersectionResult ir;
            XYZ normal = null;
            var i = 0;

            foreach (Reference r in refs)
            {
                // 'Autodesk.Revit.DB.Reference.Element' is
                // obsolete: Property will be removed. Use
                // Document.GetElement(Reference) instead.
                //Wall wall = r.Element as Wall; // 2011

                var wall = doc.GetElement(r) as Wall; // 2012

                walls[i] = wall;
                ids.Add(wall.Id.IntegerValue);

                // Obtain location curve and
                // check that it is straight:

                var lc = wall.Location
                    as LocationCurve;

                var curve = lc.Curve;
                lines[i] = curve as Line;

                if (null == lines[i])
                {
                    message = _prompt;
                    return Result.Failed;
                }

                // Obtain normal vectors
                // and ensure that they are equal,
                // i.e. walls are parallel:

                if (null == normal)
                {
                    normal = Util.Normal(lines[i]);
                }
                else
                {
                    if (!Util.IsParallel(normal,
                        Util.Normal(lines[i])))
                    {
                        message = _prompt;
                        return Result.Failed;
                    }
                }

                // Obtain pick points and project
                // onto wall location lines:

                var p = r.GlobalPoint;
                ir = lines[i].Project(p);

                if (null == ir)
                {
                    message = $"Unable to project pick point {i} onto wall location line.";

                    return Result.Failed;
                }

                pts[i] = ir.XYZPoint;

                Debug.Print(
                    "Wall {0} id {1} at {2}, {3} --> point {4}",
                    i, wall.Id.IntegerValue,
                    Util.PointString(lines[i].GetEndPoint(0)),
                    Util.PointString(lines[i].GetEndPoint(1)),
                    Util.PointString(pts[i]));

                if (0 < i)
                {
                    // Project dimension point selected on second wall
                    // back onto first wall, and ensure that normal
                    // points from second wall to first:

                    ir = lines[0].Project(pts[1]);
                    if (null == ir)
                    {
                        message = string.Format(
                            "Unable to project selected dimension "
                            + "line point {0} on second wall onto "
                            + "first wall's location line.",
                            Util.PointString(pts[1]));

                        return Result.Failed;
                    }

                    pts[0] = ir.XYZPoint;
                }

                ++i;
            }

            var v = pts[0] - pts[1];
            if (0 > v.DotProduct(normal)) normal = -normal;

            // Shoot a ray back from the second 
            // picked wall towards first:

            Debug.Print(
                "Shooting ray from {0} in direction {1}",
                Util.PointString(pts[1]),
                Util.PointString(normal));

            var view = Get3DView(doc);

            if (null == view)
            {
                message = "No 3D view named '{3D}' found; "
                          + "run the View > 3D View command once "
                          + "to generate it.";

                return Result.Failed;
            }

            //refs = doc.FindReferencesByDirection(
            //  pts[1], normal, view ); // 2011

            //IList<ReferenceWithContext> refs2
            //  = doc.FindReferencesWithContextByDirection(
            //    pts[1], normal, view ); // 2012

            // In the Revit 2014 API, the call to
            // FindReferencesWithContextByDirection causes a 
            // warning saying:
            // "FindReferencesWithContextByDirection is obsolete:
            // This method is deprecated in Revit 2014.  
            // Use the ReferenceIntersector class instead."

            var ri
                = new ReferenceIntersector(
                    walls[0].Id, FindReferenceTarget.Element, view);

            var ref2
                = ri.FindNearest(pts[1], normal);

            if (null == ref2)
            {
                message = "ReferenceIntersector.FindNearest"
                          + " returned null!";

                return Result.Failed;
            }

            #region Obsolete code to determine the closest reference

#if NEED_TO_DETERMINE_CLOSEST_REFERENCE
  // Store the references to the wall surfaces:

  Reference[] surfrefs = new Reference[2] {
    null, null };

  // Find the two closest intersection
  // points on each of the two walls:

  double[] minDistance = new double[2] {
    double.MaxValue,
    double.MaxValue };

  //foreach( Reference r in refs )
  foreach( ReferenceWithContext rc in refs2 )
  {
    // 'Autodesk.Revit.DB.Reference.Element' is
    // obsolete: Property will be removed. Use
    // Document.GetElement(Reference) instead.
    //Element e = r.Element; // 2011

    Reference r = rc.GetReference();
    Element e = doc.GetElement( r ); // 2012

    if( e is Wall )
    {
      i = ids.IndexOf( e.Id.IntegerValue );

      if( -1 < i
        && ElementReferenceType.REFERENCE_TYPE_SURFACE
          == r.ElementReferenceType )
      {
        //GeometryObject g = r.GeometryObject; // 2011
        GeometryObject g = e.GetGeometryObjectFromReference( r ); // 2012

        if( g is PlanarFace )
        {
          PlanarFace face = g as PlanarFace;

          Line line = ( e.Location as LocationCurve )
            .Curve as Line;

          Debug.Print(
            "Wall {0} at {1}, {2} surface {3} "
            + "normal {4} proximity {5}",
            e.Id.IntegerValue,
            Util.PointString( line.GetEndPoint( 0 ) ),
            Util.PointString( line.GetEndPoint( 1 ) ),
            Util.PointString( face.Origin ),
            Util.PointString( face.Normal ),
            rc.Proximity );

          // First reference: assert it is a face on this wall
          // and the distance is half the wall thickness.
          // Second reference: the first reference on the other
          // wall; assert the distance between the two references
          // equals the distance between the wall location lines
          // minus half of the sum of the two wall thicknesses.

          if( rc.Proximity < minDistance[i] )
          {
            surfrefs[i] = r;
            minDistance[i] = rc.Proximity;
          }
        }
      }
    }
  }

  if( null == surfrefs[0] )
  {
    message = "No suitable face intersection "
      + "points found on first wall.";

    return Result.Failed;
  }

  if( null == surfrefs[1] )
  {
    message = "No suitable face intersection "
      + "points found on second wall.";

    return Result.Failed;
  }

  CmdDimensionWallsIterateFaces
    .CreateDimensionElement( doc.ActiveView,
    pts[0], surfrefs[0], pts[1], surfrefs[1] );
#endif // NEED_TO_DETERMINE_CLOSEST_REFERENCE

            #endregion // Obsolete code to determine the closest reference

            CmdDimensionWallsIterateFaces
                .CreateDimensionElement(doc.ActiveView,
                    pts[0], ref2.GetReference(), pts[1], refs.get_Item(1));

            return Result.Succeeded;
        }

        #region Get3DView

        /// <summary>
        ///     Return a 3D view from the given document.
        /// </summary>
        private View3D Get3DView(Document doc)
        {
            var collector
                = new FilteredElementCollector(doc);

            collector.OfClass(typeof(View3D));

            foreach (View3D v in collector)
                // skip view templates here because they
                // are invisible in project browsers:

                if (v is {IsTemplate: false, Name: "{3D}"})
                    return v;
            return null;
        }

        #endregion // Get3DView

        #region Find ceiling face to place light fixture

        // http://thebuildingcoder.typepad.com/blog/2010/01/findreferencesbydirection.html#comment-4055509541
        /// <summary>
        ///     Return reference to ceiling face to place
        ///     lighting fixture above a given point.
        /// </summary>
        private Reference GetCeilingReferenceAbove(
            View3D view,
            XYZ p)
        {
            var filter = new ElementClassFilter(
                typeof(Ceiling));

            var refIntersector
                = new ReferenceIntersector(filter,
                    FindReferenceTarget.Face, view);

            refIntersector.FindReferencesInRevitLinks = true;

            var rwc = refIntersector.FindNearest(
                p, XYZ.BasisZ);

            var r = null == rwc
                ? null
                : rwc.GetReference();

            if (null == r) MessageBox.Show("no intersecting geometry");
            return r;
        }

        private void TestGetCeilingReferenceAbove(Document doc)
        {
            var view = doc.GetElement(new ElementId(147335)) as View3D;
            var space = doc.GetElement(new ElementId(151759)) as Space;
            var center = ((LocationPoint) space.Location).Point;

            var r = GetCeilingReferenceAbove(view, center);

            // Populate these as needed:

            XYZ startPoint = null;
            FamilySymbol sym = null;

            doc.Create.NewFamilyInstance(r, startPoint, XYZ.BasisY, sym);
        }

        #endregion // Find ceiling face to place light fixture
    }
}