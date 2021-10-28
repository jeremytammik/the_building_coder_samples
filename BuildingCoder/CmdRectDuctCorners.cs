#region Header

//
// CmdRectDuctCorners.cs - determine the corners of a rectangular duct
//
// Copyright (C) 2010-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdRectDuctCorners : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;

            if (ProductType.MEP != app.Application.Product)
            {
                message = "Please run this command in Revit MEP.";
                return Result.Failed;
            }

            //SelElementSet sel = uidoc.Selection.Elements; // 2014

            var ids = uidoc.Selection.GetElementIds(); // 2015

            //if( 0 == sel.Size ) // 2014

            if (0 == ids.Count) // 2015
            {
                message = "Please select some rectangular ducts.";
                return Result.Failed;
            }

            // set up log file:

            var log = $"{Assembly.GetExecutingAssembly().Location}.{DateTime.Now:yyyyMMdd}.log";

            if (File.Exists(log)) File.Delete(log);

            TraceListener listener
                = new TextWriterTraceListener(log);

            Trace.Listeners.Add(listener);

            try
            {
                Trace.WriteLine("Begin");

                // loop over all selected ducts:

                //foreach( Duct duct in sel ) // 2014

                foreach (var id in ids) // 2015
                    if (doc.GetElement(id) is not Duct duct)
                    {
                        Trace.TraceError("The selection is not a duct!");
                    }
                    else
                    {
                        // process each duct:

                        Trace.WriteLine("========================");
                        Trace.WriteLine($"Duct: Id = {duct.Id.IntegerValue}");

                        AnalyseDuct(duct);
                    }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
            finally
            {
                Trace.Flush();
                listener.Close();
                Trace.Close();
                Trace.Listeners.Remove(listener);
            }

            return Result.Failed;
        }

        private static XYZ Test1(Connector connector)
        {
            var p = connector.CoordinateSystem.OfPoint(
                new XYZ(connector.Width / 2,
                    connector.Height / 2, 0));

            return p;
        }

        private static XYZ Test2(Connector connector)
        {
            var p = connector.CoordinateSystem.OfPoint(
                new XYZ(connector.Height / 2,
                    connector.Width / 2, 0));

            return p;
        }

        /// <summary>
        ///     Return the first rectangular connector of the given duct element.
        /// </summary>
        private static bool GetFirstRectangularConnector(
            Duct duct,
            out Connector c1)
        {
            c1 = null;

            var connectors
                = duct.ConnectorManager.Connectors;

            if (0 < connectors.Size)
                foreach (Connector c in connectors)
                    if (ConnectorProfileType.Rectangular
                        == c.Shape)
                    {
                        c1 = c;
                        break;
                    }
                    else
                    {
                        Trace.WriteLine($"Connector shape: {c.Shape}");
                    }

            return null != c1;
        }

        /// <summary>
        ///     Return true if the given face contains the given connector.
        /// </summary>
        private static bool FaceContainsConnector(
            Face face,
            Connector c)
        {
            var p = c.Origin;

            var result = face.Project(p);

            return null != result
                   && Math.Abs(result.Distance) < 1e-9;
        }

        /// <summary>
        ///     Analyse the given duct element:
        ///     determine its first rectangular connector,
        ///     retrieve its solid,
        ///     find the face containing the connector,
        ///     and list its four vertices.
        /// </summary>
        private static bool AnalyseDuct(Duct duct)
        {
            var rc = false;

            Connector c1;
            if (!GetFirstRectangularConnector(duct, out c1))
            {
                Trace.TraceError("The duct is not rectangular!");
            }
            else
            {
                var opt = new Options();
                opt.DetailLevel = ViewDetailLevel.Fine;
                var geoElement = duct.get_Geometry(opt);

                //foreach( GeometryObject obj in geoElement.Objects ) // 2012

                foreach (var obj in geoElement) // 2013
                {
                    var solid = obj as Solid;
                    if (solid != null)
                    {
                        var foundFace = false;
                        foreach (Face face in solid.Faces)
                        {
                            foundFace = FaceContainsConnector(face, c1);
                            if (foundFace)
                            {
                                Trace.WriteLine("==> Four face corners:");

                                var a = face.EdgeLoops.get_Item(0);

                                foreach (Edge e in a)
                                {
                                    var p = e.Evaluate(0.0);

                                    Trace.WriteLine($"Point = {Util.PointString(p)}");
                                }

                                rc = true;
                                break;
                            }
                        }

                        if (!foundFace) Trace.WriteLine("[Error] Face not found");
                    }
                }
            }

            return rc;
        }

        #region Determine Elbow Centre Point

        // for https://forums.autodesk.com/t5/revit-api-forum/how-to-calculate-the-center-point-of-elbow/m-p/9803893
        /// <summary>
        ///     Return elbow connectors.
        ///     Return null if the given element is not a
        ///     family instance with exactly two connectors.
        /// </summary>
        private List<Connector> GetElbowConnectors(Element e)
        {
            List<Connector> cons = null;
            if (e is FamilyInstance fi)
            {
                var m = fi.MEPModel;
                if (null != m)
                {
                    var cm = m.ConnectorManager;
                    if (null != cm)
                    {
                        var cs = cm.Connectors;
                        if (2 == cs.Size)
                        {
                            cons = new List<Connector>(2);
                            var first = true;
                            foreach (Connector c in cs)
                                if (first)
                                    cons[0] = c;
                                else
                                    cons[1] = c;
                        }
                    }
                }
            }

            return cons;
        }

        /// <summary>
        ///     Return elbow centre point.
        ///     Return null if the start and end points
        ///     and direction vectors are not all coplanar.
        /// </summary>
        private XYZ GetElbowCentre(Element e)
        {
            XYZ pc = null;
            var cons = GetElbowConnectors(e);
            if (null != cons)
            {
                // Get start and end point and direction

                var ps = cons[0].CoordinateSystem.Origin;
                var vs = cons[0].CoordinateSystem.BasisZ;

                var pe = cons[1].CoordinateSystem.Origin;
                var ve = cons[1].CoordinateSystem.BasisZ;

                var vd = pe - ps;

                // For a regular elbow, Z vector is normal 
                // of the 2D plane spanned by the coplanar
                // start and end points and direction vectors.

                var vz = vs.CrossProduct(vd);

                if (!vz.IsZeroLength())
                {
                    var vxs = vs.CrossProduct(vz);
                    var vxe = ve.CrossProduct(vz);
                    pc = Util.LineLineIntersection(
                        ps, vxs, pe, vxe);
                }
            }

            return pc;
        }

        #endregion // Determine Elbow Centre Point
    }
}