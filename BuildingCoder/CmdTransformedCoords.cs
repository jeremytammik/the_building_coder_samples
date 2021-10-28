#region Header

//
// CmdTransformedCoords.cs - retrieve coordinates
// from family instance transformed into world
// coordinate system
//
// Copyright (C) 2009-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdTransformedCoords : IExternalCommand
    {
        /// <summary>
        ///     Sample file is at
        ///     C:\a\j\adn\case\bsd\1242980\attach\mullion.rvt
        /// </summary>
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = app.ActiveUIDocument.Document;
            var sel = uidoc.Selection;

            var options = app.Application.Create.NewGeometryOptions();
            string s, msg = string.Empty;
            int n;
            foreach (var id in sel.GetElementIds())
                if (doc.GetElement(id) is Mullion mullion)
                {
                    //Location location = mullion.AsFamilyInstance.Location; // seems to be uninitialised // 2011

                    var location = mullion.Location; // 2012

                    var lp
                        = mullion.Location
                            as LocationPoint;

                    Debug.Assert(null != lp,
                        "expected a valid mullion location point");

                    Debug.Assert(null != mullion.LocationCurve, // 2012
                        "in Revit 2012, the mullion also has a valid location curve"); // 2012

                    var geoElem
                        = mullion.get_Geometry(options);

                    //GeometryObjectArray objects = geoElem.Objects; // 2012
                    //n = objects.Size; // 2012

                    n = geoElem.Count(); // 2013

                    s = string.Format(
                        "Mullion <{0} {1}> at {2} rotation"
                        + " {3} has {4} geo object{5}:",
                        mullion.Name, mullion.Id.IntegerValue,
                        Util.PointString(lp.Point),
                        Util.RealString(lp.Rotation),
                        n, Util.PluralSuffix(n));

                    if (0 < msg.Length) msg += "\n\n";
                    msg += s;

                    //foreach( GeometryObject obj in objects ) // 2012

                    foreach (var obj in geoElem) // 2013
                    {
                        var inst = obj as GeometryInstance;
                        var t = inst.Transform;

                        s = $"  Transform {Util.TransformString(t)}";
                        msg += $"\n{s}";

                        var elem2 = inst.SymbolGeometry;

                        //foreach( GeometryObject obj2 in elem2.Objects ) // 2012

                        foreach (var obj2 in elem2) // 2013
                        {
                            var solid = obj2 as Solid;
                            if (null != solid)
                            {
                                var faces = solid.Faces;
                                n = faces.Size;

                                s = $"  {n} face{Util.PluralSuffix(n)}, face point > WCS point:";

                                msg += $"\n{s}";

                                foreach (Face face in solid.Faces)
                                {
                                    s = string.Empty;
                                    var mesh = face.Triangulate();
                                    foreach (var p in mesh.Vertices)
                                    {
                                        s += 0 == s.Length ? "    " : ", ";
                                        s += $"{Util.PointString(p)} > {Util.PointString(t.OfPoint(p))}";
                                    }

                                    msg += $"\n{s}";
                                }
                            }
                        }
                    }
                }

            if (0 == msg.Length) msg = "Please select some mullions.";
            Util.InfoMsg(msg);
            return Result.Failed;
        }
    }
}