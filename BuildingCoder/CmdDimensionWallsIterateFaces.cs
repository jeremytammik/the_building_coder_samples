#region Header

//
// CmdDimensionWallsIterateFaces.cs - create dimensioning elements
// between opposing walls by iterating over their faces
//
// Copyright (C) 2011-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    /// <summary>
    ///     Dimension two opposing parallel walls.
    ///     For simplicity, the dimension is defined from
    ///     wall midpoint to midpoint, so the walls have
    ///     to be exactly opposite each other for it to work.
    ///     Iterate the wall solid faces to find the two
    ///     closest opposing faces and use references to
    ///     them to define the dimension element.
    ///     First sample solution for case
    ///     1263071 [Revit 2011 Dimension Wall].
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    internal class CmdDimensionWallsIterateFaces : IExternalCommand
    {
        private const string _prompt
            = "Please select two parallel opposing straight walls.";

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var app = uiapp.Application;
            var doc = uidoc.Document;

            // obtain the current selection and pick
            // out all walls from it:

            //Selection sel = uidoc.Selection; // 2014

            var ids = uidoc.Selection
                .GetElementIds(); // 2015

            var walls = new List<Wall>(2);

            //foreach( Element e in sel.Elements ) // 2014

            foreach (var id in ids) // 2015
            {
                var e = doc.GetElement(id);

                if (e is Wall wall) walls.Add(wall);
            }

            if (2 != walls.Count)
            {
                message = _prompt;
                return Result.Failed;
            }

            // ensure the two selected walls are straight and
            // parallel; determine their mutual normal vector
            // and a point on each wall for distance
            // calculations:

            var lines = new List<Line>(2);
            var midpoints = new List<XYZ>(2);
            XYZ normal = null;

            foreach (var wall in walls)
            {
                var lc = wall.Location as LocationCurve;
                var curve = lc.Curve;

                if (!(curve is Line line))
                {
                    message = _prompt;
                    return Result.Failed;
                }

                lines.Add(line);
                midpoints.Add(Util.Midpoint(line));

                if (null == normal)
                {
                    normal = Util.Normal(line);
                }
                else
                {
                    if (!Util.IsParallel(normal, Util.Normal(line)))
                    {
                        message = _prompt;
                        return Result.Failed;
                    }
                }
            }

            // find the two closest facing faces on the walls;
            // they are vertical faces that are parallel to the
            // wall curve and closest to the other wall.

            var opt = app.Create.NewGeometryOptions();

            opt.ComputeReferences = true;

            var faces = new List<Face>(2);
            faces.Add(GetClosestFace(walls[0], midpoints[1], normal, opt));
            faces.Add(GetClosestFace(walls[1], midpoints[0], normal, opt));

            // create the dimensioning:

            CreateDimensionElement(doc.ActiveView,
                midpoints[0], faces[0].Reference,
                midpoints[1], faces[1].Reference);

            return Result.Succeeded;
        }

        #region CreateDimensionElement

        /// <summary>
        ///     Create a new dimension element using the given
        ///     references and dimension line end points.
        ///     This method opens and commits its own transaction,
        ///     assuming that no transaction is open yet and manual
        ///     transaction mode is being used.
        ///     Note that this has only been tested so far using
        ///     references to surfaces on planar walls in a plan
        ///     view.
        /// </summary>
        public static void CreateDimensionElement(
            View view,
            XYZ p1,
            Reference r1,
            XYZ p2,
            Reference r2)
        {
            var doc = view.Document;

            var ra = new ReferenceArray();

            ra.Append(r1);
            ra.Append(r2);

            var line = Line.CreateBound(p1, p2);

            using var t = new Transaction(doc);
            t.Start("Create New Dimension");

            var dim = doc.Create.NewDimension(
                view, line, ra);

            t.Commit();
        }

        #endregion // CreateDimensionElement

        #region GetClosestFace

        /// <summary>
        ///     Return the closest planar face to a given point
        ///     p on the element e with a given normal vector.
        /// </summary>
        private static Face GetClosestFace(
            Element e,
            XYZ p,
            XYZ normal,
            Options opt)
        {
            Face face = null;
            var min_distance = double.MaxValue;
            var geo = e.get_Geometry(opt);

            //GeometryObjectArray objects = geo.Objects; // 2012
            //foreach( GeometryObject obj in objects ) // 2012

            foreach (var obj in geo) // 2013
            {
                var solid = obj as Solid;
                if (solid != null)
                {
                    var fa = solid.Faces;
                    foreach (Face f in fa)
                    {
                        var pf = f as PlanarFace;

                        Debug.Assert(null != pf,
                            "expected planar wall faces");

                        if (null != pf
                            //&& normal.IsAlmostEqualTo( pf.Normal )
                            && Util.IsParallel(normal, pf.FaceNormal))
                        {
                            //XYZ q = pf.Project( p ).XYZPoint; // Project returned null once
                            //double d = q.DistanceTo( p );

                            var v = p - pf.Origin;
                            var d = v.DotProduct(-pf.FaceNormal);
                            if (d < min_distance)
                            {
                                face = f;
                                min_distance = d;
                            }
                        }
                    }
                }
            }

            return face;
        }

        #endregion // GetClosestFace

        #region Dimension Filled Region Alexander

        [Transaction(TransactionMode.Manual)]
        public class CreateFillledRegionDimensionsCommand : IExternalCommand
        {
            public Result Execute(
                ExternalCommandData commandData,
                ref string message,
                ElementSet elements)
            {
                var uiapp = commandData.Application;
                var uidoc = uiapp.ActiveUIDocument;
                var doc = uidoc.Document;

                var view = uidoc.ActiveGraphicalView;

                var filledRegions = FindFilledRegions(doc, view.Id);

                using var transaction = new Transaction(doc,
                    "filled regions dimensions");
                transaction.Start();

                foreach (var filledRegion in filledRegions)
                {
                    CreateDimensions(filledRegion,
                        -1 * view.RightDirection);

                    CreateDimensions(filledRegion, view.UpDirection);
                }

                transaction.Commit();

                return Result.Succeeded;
            }

            private static void CreateDimensions(
                FilledRegion filledRegion,
                XYZ dimensionDirection)
            {
                var document = filledRegion.Document;

                var view = (View) document.GetElement(
                    filledRegion.OwnerViewId);

                var edgesDirection = dimensionDirection.CrossProduct(
                    view.ViewDirection);

                var edges = FindRegionEdges(filledRegion)
                    .Where(x => IsEdgeDirectionSatisfied(x, edgesDirection))
                    .ToList();

                if (edges.Count < 2)
                    return;

                //var shift = UnitUtils.ConvertToInternalUnits(
                //  -10 * view.Scale, DisplayUnitType.DUT_MILLIMETERS ) // 2020
                //  * edgesDirection;

                var shift = UnitUtils.ConvertToInternalUnits(
                                -10 * view.Scale, UnitTypeId.Millimeters) // 2021
                            * edgesDirection;

                var dimensionLine = Line.CreateUnbound(
                    filledRegion.get_BoundingBox(view).Min
                    + shift, dimensionDirection);

                var references = new ReferenceArray();

                foreach (var edge in edges)
                    references.Append(edge.Reference);

                document.Create.NewDimension(view, dimensionLine,
                    references);
            }

            private static bool IsEdgeDirectionSatisfied(
                Edge edge,
                XYZ edgeDirection)
            {
                var edgeCurve = edge.AsCurve() as Line;

                if (edgeCurve == null)
                    return false;

                return edgeCurve.Direction.CrossProduct(
                    edgeDirection).IsAlmostEqualTo(XYZ.Zero);
            }

            private static IEnumerable<Edge> FindRegionEdges(
                FilledRegion filledRegion)
            {
                var view = (View) filledRegion.Document.GetElement(
                    filledRegion.OwnerViewId);

                var options = new Options
                {
                    View = view,
                    ComputeReferences = true
                };

                return filledRegion
                    .get_Geometry(options)
                    .OfType<Solid>()
                    .SelectMany(x => x.Edges.Cast<Edge>());
            }

            private static IEnumerable<FilledRegion>
                FindFilledRegions(
                    Document document,
                    ElementId viewId)
            {
                var collector = new FilteredElementCollector(
                    document, viewId);

                return collector
                    .OfClass(typeof(FilledRegion))
                    .Cast<FilledRegion>();
            }
        }

        #endregion // Dimension Filled Region Alexander

        #region Developer Guide Sample Code

        public void DuplicateDimension(
            Document doc,
            Dimension dimension)
        {
            var line = dimension.Curve as Line;

            if (null != line)
            {
                var view = dimension.View;

                var references = dimension.References;

                var newDimension = doc.Create.NewDimension(
                    view, line, references);
            }
        }

        public Dimension CreateLinearDimension(
            Document doc)
        {
            var app = doc.Application;

            // first create two lines

            var pt1 = new XYZ(5, 5, 0);
            var pt2 = new XYZ(5, 10, 0);
            var line = Line.CreateBound(pt1, pt2);

            //Plane plane = app.Create.NewPlane( pt1.CrossProduct( pt2 ), pt2 ); // 2016

            var plane = Plane.CreateByNormalAndOrigin(pt1.CrossProduct(pt2), pt2); // 2017

            //SketchPlane skplane = doc.FamilyCreate.NewSketchPlane( plane ); // 2013

            var skplane = SketchPlane.Create(doc, plane); // 2014

            var modelcurve1 = doc.FamilyCreate
                .NewModelCurve(line, skplane);

            pt1 = new XYZ(10, 5, 0);
            pt2 = new XYZ(10, 10, 0);
            line = Line.CreateBound(pt1, pt2);
            //plane = app.Create.NewPlane( pt1.CrossProduct( pt2 ), pt2 ); // 2016
            plane = Plane.CreateByNormalAndOrigin(pt1.CrossProduct(pt2), pt2); // 2017

            //skplane = doc.FamilyCreate.NewSketchPlane( plane ); // 2013

            skplane = SketchPlane.Create(doc, plane); // 2014

            var modelcurve2 = doc.FamilyCreate
                .NewModelCurve(line, skplane);

            // now create a linear dimension between them

            var ra = new ReferenceArray();
            ra.Append(modelcurve1.GeometryCurve.Reference);
            ra.Append(modelcurve2.GeometryCurve.Reference);

            pt1 = new XYZ(5, 10, 0);
            pt2 = new XYZ(10, 10, 0);
            line = Line.CreateBound(pt1, pt2);
            var dim = doc.FamilyCreate
                .NewLinearDimension(doc.ActiveView, line, ra);

            // create a label for the dimension called "width"

            var param = doc.FamilyManager
                .AddParameter("width",
                    //BuiltInParameterGroup.PG_CONSTRAINTS, // 2021
                    GroupTypeId.Constraints, // 2022
                    //ParameterType.Length, // 2021
                    SpecTypeId.Length, // 2022
                    false);

            //dim.Label = param; // 2013
            dim.FamilyLabel = param; // 2014

            return dim;
        }

        #endregion // Developer Guide Sample Code

        #region Dimension Filled Region Jorge

        private void CreateDimensions(
            FilledRegion filledRegion,
            XYZ dimensionDirection,
            string typeName)
        {
            var document = filledRegion.Document;

            var view = (View) document.GetElement(
                filledRegion.OwnerViewId);

            var edgesDirection = dimensionDirection.CrossProduct(
                view.ViewDirection);

            var edges = FindRegionEdges(filledRegion)
                .Where(x => IsEdgeDirectionSatisfied(x, edgesDirection))
                .ToList();

            if (edges.Count < 2)
                return;

            // Se hace este ajuste para que la distancia no 
            // depende de la escala. <<<<<< evaluar para 
            // informaciуn de acotado y etiquetado!!!

            //var shift = UnitUtils.ConvertToInternalUnits(
            //  5 * view.Scale, DisplayUnitType.DUT_MILLIMETERS ) // 2020
            //  * edgesDirection;

            var shift = UnitUtils.ConvertToInternalUnits(
                            5 * view.Scale, UnitTypeId.Millimeters) // 2021
                        * edgesDirection;

            var dimensionLine = Line.CreateUnbound(
                filledRegion.get_BoundingBox(view).Min + shift,
                dimensionDirection);

            var references = new ReferenceArray();

            foreach (var edge in edges)
                references.Append(edge.Reference);

            var dim = document.Create.NewDimension(
                view, dimensionLine, references);

            var dr_id = DimensionTypeId(
                document, typeName);

            if (dr_id != null) dim.ChangeTypeId(dr_id);
        }

        private static bool IsEdgeDirectionSatisfied(
            Edge edge,
            XYZ edgeDirection)
        {
            var edgeCurve = edge.AsCurve() as Line;

            if (edgeCurve == null)
                return false;

            return edgeCurve.Direction.CrossProduct(
                edgeDirection).IsAlmostEqualTo(XYZ.Zero);
        }

        private static IEnumerable<FilledRegion>
            FindFilledRegions(
                Document document,
                ElementId viewId)
        {
            var collector = new FilteredElementCollector(
                document, viewId);

            return collector
                .OfClass(typeof(FilledRegion))
                .Cast<FilledRegion>();
        }

        private static IEnumerable<Edge>
            FindRegionEdges(
                FilledRegion filledRegion)
        {
            var view = (View) filledRegion.Document.GetElement(
                filledRegion.OwnerViewId);

            var options = new Options
            {
                View = view,
                ComputeReferences = true
            };

            return filledRegion
                .get_Geometry(options)
                .OfType<Solid>()
                .SelectMany(x => x.Edges.Cast<Edge>());
        }

        private static ElementId DimensionTypeId(
            Document doc,
            string typeName)
        {
            var mt_coll
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(DimensionType))
                    .WhereElementIsElementType();

            DimensionType dimType = null;

            foreach (var type in mt_coll)
                if (type is DimensionType dimensionType)
                    if (dimensionType.Name == typeName)
                    {
                        dimType = dimensionType;
                        break;
                    }

            return dimType.Id;
        }

        #endregion // Dimension Filled Region Jorge
    }
}