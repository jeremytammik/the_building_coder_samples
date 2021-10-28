#region Header

//
// CmdNestedInstanceGeo.cs - analyse
// nested instance geometry and structure
//
// Copyright (C) 2009-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
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
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdNestedInstanceGeo : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;

            var a = new List<Element>();

            if (!Util.GetSelectedElementsOrAll(a, uidoc,
                typeof(FamilyInstance)))
            {
                var sel = uidoc.Selection;
                message = 0 < sel.GetElementIds().Count
                    ? "Please select some family instances."
                    : "No family instances found.";
                return Result.Failed;
            }

            var inst = a[0] as FamilyInstance;

            // Here are two ways to traverse the nested instance geometry.
            // The first way can get the right position, but can't get the right structure.
            // The second way can get the right structure, but can't get the right position.
            // What I want is the right structure and right position.

            // First way:

            // In the current project project1.rvt, I can get myFamily3 instance via API,
            // the class is Autodesk.Revit.Elements.FamilyInstance.
            // Then i try to get its geometry:

            var opt = app.Application.Create.NewGeometryOptions();
            var geoElement = inst.get_Geometry(opt);

            //GeometryObjectArray a1 = geoElement.Objects; // 2012
            //int n = a1.Size; // 2012

            var n = geoElement.Count(); // 2013

            Debug.Print(
                "Family instance geometry has {0} geometry object{1}{2}",
                n, Util.PluralSuffix(n), Util.DotOrColon(n));

            var i = 0;

            //foreach( GeometryObject o1 in a1 ) // 2012
            foreach (var o1 in geoElement) // 2013
            {
                var geoInstance = o1 as GeometryInstance;
                if (null != geoInstance)
                {
                    // geometry includes one instance, so get its geometry:

                    var symbolGeo = geoInstance.SymbolGeometry;

                    //GeometryObjectArray a2 = symbolGeo.Objects; // 2012
                    //foreach( GeometryObject o2 in a2 ) // 2012

                    // the symbol geometry contains five solids.
                    // how can I find out which solid belongs to which column?
                    // how to relate the solid to the family instance?

                    foreach (var o2 in symbolGeo)
                    {
                        var s = o2 as Solid;
                        if (null != s && 0 < s.Edges.Size)
                        {
                            var vertices = new List<XYZ>();
                            GetVertices(vertices, s);
                            n = vertices.Count;

                            Debug.Print("Solid {0} has {1} vertices{2} {3}",
                                i++, n, Util.DotOrColon(n),
                                Util.PointArrayString(vertices));
                        }
                    }
                }
            }

            // In the Revit 2009 API, we can use
            // FamilyInstance.Symbol.Family.Components
            // to obtain the nested family instances
            // within the top level family instance.

            // In the Revit 2010 API, this property has been
            // removed, since we can iterate through the elements
            // of a family just like any other document;
            // cf. What's New in the RevitAPI.chm:


#if REQUIRES_REVIT_2009_API
      ElementSet components = inst.Symbol.Family.Components;
      n = components.Size;
#endif // REQUIRES_REVIT_2009_API

            var fdoc = doc.EditFamily(inst.Symbol.Family);

#if REQUIRES_REVIT_2010_API
      List<Element> components = new List<Element>();
      fdoc.get_Elements( typeof( FamilyInstance ), components );
      n = components.Count;
#endif // REQUIRES_REVIT_2010_API

            var collector
                = new FilteredElementCollector(fdoc);

            collector.OfClass(typeof(FamilyInstance));
            var components = collector.ToElements();

            Debug.Print(
                "Family instance symbol family has {0} component{1}{2}",
                n, Util.PluralSuffix(n), Util.DotOrColon(n));

            foreach (var e in components)
            {
                // there are 3 FamilyInstance: Column, myFamily1, myFamily2
                // then we can loop myFamily1, myFamily2 also.
                // then get all the Column geometry
                // But all the Column's position is the same,
                // because the geometry is defined by the Symbol.
                // Not the actually position in project1.rvt

                var lp = e.Location as LocationPoint;
                Debug.Print("{0} at {1}",
                    Util.ElementDescription(e),
                    Util.PointString(lp.Point));
            }

            return Result.Failed;
        }

        private static void GetVertices(List<XYZ> vertices, Solid s)
        {
            Debug.Assert(0 < s.Edges.Size,
                "expected a non-empty solid");

            var a
                = new Dictionary<XYZ, int>(
                    new XyzEqualityComparer());

            foreach (Face f in s.Faces)
            {
                var m = f.Triangulate();
                foreach (var p in m.Vertices)
                    if (!a.ContainsKey(p))
                        a.Add(p, 1);
                    else
                        ++a[p];
            }

            var keys = new List<XYZ>(a.Keys);

            Debug.Assert(8 == keys.Count,
                "expected eight vertices for a rectangular column");

            keys.Sort((p, q) => Util.Compare(p, q));

            foreach (var p in keys)
            {
                Debug.Assert(3 == a[p],
                    "expected every vertex of solid to appear in exactly three faces");

                vertices.Add(p);
            }
        }

        /// <summary>
        ///     Define equality between XYZ objects, ensuring
        ///     that almost equal points compare equal.
        /// </summary>
        private class XyzEqualityComparer : IEqualityComparer<XYZ>
        {
            public bool Equals(XYZ p, XYZ q)
            {
                return p.IsAlmostEqualTo(q);
            }

            public int GetHashCode(XYZ p)
            {
                return Util.PointString(p).GetHashCode();
            }
        }
    }
}