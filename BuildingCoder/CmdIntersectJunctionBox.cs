#region Header

//
// CmdIntersectJunctionBox.cs - determine conduits intersecting junction box
//
// Copyright (C) 2018-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdIntersectJunctionBox : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;

            var test_strange_intersect_result = true;
            if (test_strange_intersect_result)
            {
                TestIntersect(doc);
                return Result.Succeeded;
            }

            var e = Util.SelectSingleElement(
                uidoc, "a junction box");

            var bb = e.get_BoundingBox(null);

            var outLne = new Outline(bb.Min, bb.Max);

            // Use a quick bounding box filter - axis aligned

            ElementQuickFilter fbb
                = new BoundingBoxIntersectsFilter(outLne);

            var conduits
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(Conduit))
                    .WherePasses(fbb);

            // How many elements did we find?

            var nbb = conduits.GetElementCount();

            // Use a slow intersection filter - exact results

            ElementSlowFilter intersect_junction
                = new ElementIntersectsElementFilter(e);

            conduits = new FilteredElementCollector(doc)
                .OfClass(typeof(Conduit))
                .WherePasses(intersect_junction);

            // How many elements did we find?

            var nintersect = conduits.GetElementCount();

            Debug.Assert(nintersect <= nbb,
                "expected element intersection to be stricter"
                + "than bounding box containment");

            return Result.Succeeded;
        }

        #region Create Offset Conduits

        /// <summary>
        ///     Given a curve element, determine three parallel
        ///     curves on the right, left and above, offset by
        ///     a given radius or half diameter.
        /// </summary>
        private void CreateConduitOffsets(
            Element conduit,
            double diameter,
            double distance)
        {
            var doc = conduit.Document;

            // Given element location curve

            var obj = conduit.Location as LocationCurve;
            var start = obj.Curve.GetEndPoint(0);
            var end = obj.Curve.GetEndPoint(1);
            var vx = end - start;

            // Offsets

            var radius = 0.5 * diameter;
            var offset_lr = radius;
            var offset_up = radius * Math.Sqrt(3);

            // Assume vx is horizontal to start with

            var vz = XYZ.BasisZ;
            var vy = vz.CrossProduct(vx);
            var start1 = start + offset_lr * vy;
            var start2 = start - offset_lr * vy;
            var start3 = start + offset_up * vz;
            var end1 = start1 + vx;
            var end2 = start2 + vx;
            var end3 = start3 + vx;

            // Confusing sample code from 
            // https://forums.autodesk.com/t5/revit-api-forum/offset-conduit-only-by-z-axis/m-p/9972671

            var L = Math.Sqrt((start.X - end.X) * (start.X - end.X) + (start.Y - end.Y) * (start.Y - end.Y));
            var x1startO = start.X + distance * (end.Y - start.Y) / L;
            var x1endO = end.X + distance * (end.Y - start.Y) / L;
            var y1startO = start.Y + distance * (start.X - end.X) / L;
            var y1end0 = end.Y + distance * (start.X - end.X) / L;
            var conduit1 = Conduit.Create(doc, conduit.GetTypeId(), new XYZ(x1startO, y1startO, start.Z), new XYZ(x1endO, y1end0, end.Z), conduit.LevelId);
            conduit1.get_Parameter(BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM).Set(diameter);

            var x2startO = start.X - distance * (end.Y - start.Y) / L;
            var x2endO = end.X - distance * (end.Y - start.Y) / L;
            var y2startO = start.Y - distance * (start.X - end.X) / L;
            var y2end0 = end.Y - distance * (start.X - end.X) / L;

            var conduit2 = Conduit.Create(doc, conduit.GetTypeId(), new XYZ(x2startO, y2startO, start.Z), new XYZ(x2endO, y2end0, end.Z), conduit.LevelId);
            conduit2.get_Parameter(BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM).Set(diameter);

            var p0 = new XYZ(start.X - (end.Y - start.Y) * (1 / obj.Curve.ApproximateLength), start.Y + (end.X - start.X) * (1 / obj.Curve.ApproximateLength), start.Z);
            var p1 = new XYZ(start.X + (end.Y - start.Y) * (1 / obj.Curve.ApproximateLength), start.Y - (end.X - start.X) * (1 / obj.Curve.ApproximateLength), start.Z);

            Conduit conduit3;

            var copyCurve = obj.Curve.CreateOffset(-diameter * Math.Sqrt(3) / 2, Line.CreateBound(p0, p1).Direction);
            conduit3 = Conduit.Create(doc, conduit.GetTypeId(), copyCurve.GetEndPoint(0), copyCurve.GetEndPoint(1), conduit.LevelId);
            conduit3.get_Parameter(BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM).Set(diameter);
        }

        #endregion Create Offset Conduits

        #region Tiago Cerqueira

        private class FindIntersection
        {
            public readonly List<Conduit> GetListOfConduits = new();

            public FindIntersection(
                FamilyInstance jbox,
                UIDocument uiDoc)
            {
                var jboxPoint = (jbox.Location
                    as LocationPoint).Point;

                var filteredCloserConduits
                    = new FilteredElementCollector(uiDoc.Document);

                var listOfCloserConduit
                    = filteredCloserConduits
                        .OfClass(typeof(Conduit))
                        .ToList()
                        .Where(x
                            => ((x as Conduit).Location as LocationCurve).Curve
                               .GetEndPoint(0).DistanceTo(jboxPoint) < 30
                               || ((x as Conduit).Location as LocationCurve).Curve
                               .GetEndPoint(1).DistanceTo(jboxPoint) < 30)
                        .ToList();

                // getting the location of the box and all conduit around.

                var opt = new Options();
                opt.View = uiDoc.ActiveView;
                var geoEle = jbox.get_Geometry(opt);

                // getting the geometry of the element to 
                // access the geometry of the instance.

                foreach (var geomObje1 in geoEle)
                {
                    var geoInstance = (geomObje1
                        as GeometryInstance).GetInstanceGeometry();

                    // the geometry of the family instance can be 
                    // accessed by this method that returns a 
                    // GeometryElement type. so we must get the 
                    // GeometryObject again to access the Face of 
                    // the family instance.

                    if (geoInstance != null)
                        foreach (var geomObje2 in geoInstance)
                        {
                            var geoSolid = geomObje2 as Solid;
                            if (geoSolid != null)
                                foreach (Face face in geoSolid.Faces)
                                foreach (var cond in listOfCloserConduit)
                                {
                                    var con = cond as Conduit;
                                    var conCurve = (con.Location as LocationCurve).Curve;
                                    var set = face.Intersect(conCurve);
                                    if (set.ToString() == "Overlap")
                                        //getting the conduit the intersect the box.

                                        GetListOfConduits.Add(con);
                                }
                        }
                }
            }

            public Conduit ConduitRun { get; set; }

            public FamilyInstance Jbox { get; set; }
        }

        #endregion // Tiago Cerqueira

        #region Intersect strange result

        /// <summary>
        ///     Return all faces from first solid
        ///     of the given element.
        /// </summary>
        private IEnumerable<Face> GetFaces(Element e)
        {
            var opt = new Options();
            var faces = e
                .get_Geometry(opt)
                .OfType<Solid>()
                .First()
                .Faces
                .OfType<Face>();
            var n = faces.Count();
            Debug.Print("{0} has {1} face{2}.",
                e.GetType().Name, n, Util.PluralSuffix(n));
            return faces;
        }

        // This has been mentioned in this post from 2016 but maybe it's worth bringing up again as 2018 hasn't resolved the issue yet.
        // As far as I can tell, the Face.Intersect( face) method always returns FaceIntersectionFaceResult.Intersecting - or I am not implementing it correctly.When I run the code below in a view with a single wall and single floor, each face to face test returns an intersection. Can someone please verify (maybe 2019)?
        // https://forums.autodesk.com/t5/revit-api-forum/get-conection-type-and-geometry-between-two-elements-from-the/m-p/6465671
        // https://forums.autodesk.com/t5/revit-api-forum/surprising-results-from-face-intersect-face-method/m-p/8079881
        // /a/doc/revit/tbc/git/a/img/intersect_strange_result.png
        // /a/doc/revit/tbc/git/a/img/floor_wall_disjunct.png
        private void TestIntersect(Document doc)
        {
            var view = doc.ActiveView;

            var list = new FilteredElementCollector(doc, view.Id)
                .WhereElementIsNotElementType()
                .Where(e => e is Wall or Floor);

            var n = list.Count();

            Element floor = null;
            Element wall = null;

            if (2 == n)
            {
                floor = list.First() as Floor;
                if (null == floor)
                {
                    floor = list.Last() as Floor;
                    wall = list.First() as Wall;
                }
                else
                {
                    wall = list.Last() as Wall;
                }
            }

            if (null == floor || null == wall)
            {
                Util.ErrorMsg("Please run this command in a "
                              + "document with just one floor and one wall "
                              + "with no mutual intersection");
            }
            else
            {
                var opt = new Options();
                var floorFaces = GetFaces(floor);
                var wallFaces = GetFaces(wall);
                n = 0;
                foreach (var f1 in floorFaces)
                foreach (var f2 in wallFaces)
                    if (f1.Intersect(f2)
                        == FaceIntersectionFaceResult.Intersecting)
                    {
                        ++n;

                        if (MessageBox.Show(
                                "Intersects", "Continue",
                                MessageBoxButtons.OKCancel,
                                MessageBoxIcon.Exclamation)
                            == DialogResult.Cancel)
                            return;
                    }

                Debug.Print("{0} face-face intersection{1}.",
                    n, Util.PluralSuffix(n));
            }
        }

        #endregion
    }
}