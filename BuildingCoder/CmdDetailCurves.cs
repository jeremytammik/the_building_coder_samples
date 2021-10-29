#region Header

//
// CmdDetailCurves.cs - create detail curves
//
// Copyright (C) 2009-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdDetailCurves : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;
            var view = doc.ActiveView;

            var creDoc
                = doc.Create;

            #region Check for pre-selected wall element

            var sel = uidoc.Selection;
            var ids = sel.GetElementIds();

            if (1 == ids.Count)
            {
                var e = doc.GetElement(ids.First());
                if (e is Wall)
                {
                    var lc = e.Location as LocationCurve;
                    var curve = lc.Curve;

                    using var tx = new Transaction(doc);
                    tx.Start("Create Detail Line in Wall Centre");
                    creDoc.NewDetailCurve(view, curve);
                    tx.Commit();

                    return Result.Succeeded;
                }
            }

            #endregion // Check for pre-selected wall element

            // Create a geometry line

            var startPoint = new XYZ(0, 0, 0);
            var endPoint = new XYZ(10, 10, 0);

            //Line geomLine = creApp.NewLine( startPoint, endPoint, true ); // 2013

            var geomLine = Line.CreateBound(startPoint, endPoint); // 2014

            // Create a geometry arc

            var end0 = new XYZ(0, 0, 0);
            var end1 = new XYZ(10, 0, 0);
            var pointOnCurve = new XYZ(5, 5, 0);

            //Arc geomArc = creApp.NewArc( end0, end1, pointOnCurve ); // 2013

            var geomArc = Arc.Create(end0, end1, pointOnCurve); // 2014

#if NEED_PLANE
      // Create a geometry plane

      XYZ origin = new XYZ( 0, 0, 0 );
      XYZ normal = new XYZ( 1, 1, 0 );

      Plane geomPlane = creApp.NewPlane(
        normal, origin );

      // Create a sketch plane in current document

      SketchPlane sketch = creDoc.NewSketchPlane(
        geomPlane );
#endif // NEED_PLANE

            using (var tx = new Transaction(doc))
            {
                tx.Start("Create Detail Line and Arc");

                // Create a DetailLine element using the
                // newly created geometry line and sketch plane

                var line = creDoc.NewDetailCurve(
                    view, geomLine) as DetailLine;

                // Create a DetailArc element using the
                // newly created geometry arc and sketch plane

                var arc = creDoc.NewDetailCurve(
                    view, geomArc) as DetailArc;

                // Change detail curve colour.
                // Initially, this only affects the newly
                // created curves. However, when the view
                // is refreshed, all detail curves will
                // be updated.

                var gs = arc.LineStyle as GraphicsStyle;

                gs.GraphicsStyleCategory.LineColor
                    = new Color(250, 10, 10);

                tx.Commit();
            }

            return Result.Succeeded;
        }

        /// <summary>
        ///     Return a point projected onto a plane defined by its normal.
        ///     http://www.euclideanspace.com/maths/geometry/elements/plane
        ///     Case 1259133 [Curve must be in the plane]
        /// </summary>
        private XYZ ProjectPointOntoPlane(
            XYZ point,
            XYZ planeNormal)
        {
            var a = planeNormal.X;
            var b = planeNormal.Y;
            var c = planeNormal.Z;

            var dx = (b * b + c * c) * point.X - a * b * point.Y - a * c * point.Z;
            var dy = -(b * a) * point.X + (a * a + c * c) * point.Y - b * c * point.Z;
            var dz = -(c * a) * point.X - c * b * point.Y + (a * a + b * b) * point.Z;
            return new XYZ(dx, dy, dz);
        }
    }
}