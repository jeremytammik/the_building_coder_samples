#region Header

//
// CmdNewLightingFixture.cs - insert new lighting fixture family instance
//
// Copyright (C) 2009-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdNewLightingFixture : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var app = uiapp.Application;
            var doc = uidoc.Document;

            // Get a lighting fixture family symbol:

            var symbols
                = Util.GetElementsOfType(doc,
                    typeof(FamilySymbol),
                    BuiltInCategory.OST_LightingFixtures);

            if (symbols.FirstElement() is not FamilySymbol sym)
            {
                message = "No lighting fixture symbol found.";
                return Result.Failed;
            }

            // Pick the ceiling:

#if _2010
      uidoc.Selection.StatusbarTip
        = "Please select ceiling to host lighting fixture";

      uidoc.Selection.PickOne();

      Element ceiling = null;

      foreach( Element e in uidoc.Selection.Elements )
      {
        ceiling = e as Element;
        break;
      }
#endif // _2010

            var r = uidoc.Selection.PickObject(
                ObjectType.Element,
                "Please select ceiling to host lighting fixture");

            if (null == r)
            {
                message = "Nothing selected.";
                return Result.Failed;
            }

            // 'Autodesk.Revit.DB.Reference.Element' is
            // obsolete: Property will be removed. Use
            // Document.GetElement(Reference) instead.
            //Element ceiling = r.Element; // 2011

            Element ceiling = doc.GetElement(r) as Wall; // 2012

            // Get the level 1:

            if (Util.GetFirstElementOfTypeNamed(
                doc, typeof(Level), "Level 1") is not Level level)
            {
                message = "Level 1 not found.";
                return Result.Failed;
            }

            // Create the family instance:

            var p = app.Create.NewXYZ(-43, 28, 0);

            using var t = new Transaction(doc);
            t.Start("Place New Lighting Fixture Instance");

            var instLight
                = doc.Create.NewFamilyInstance(
                    p, sym, ceiling, level,
                    StructuralType.NonStructural);

            t.Commit();

            return Result.Succeeded;
        }

        #region PlaceFamilyInstanceOnFace

        /// <summary>
        ///     Place an instance of the given family symbol
        ///     on a selected face of an existing 3D element.
        /// </summary>
        private FamilyInstance PlaceFamilyInstanceOnFace(
            UIDocument uidoc,
            FamilySymbol symbol)
        {
            var doc = uidoc.Document;

            var r = uidoc.Selection.PickObject(
                ObjectType.Face, "Please pick a point on "
                                 + " a face for family instance insertion");

            var e = doc.GetElement(r.ElementId);

            var obj
                = e.GetGeometryObjectFromReference(r);

            switch (obj)
            {
                case PlanarFace planarFace:
                    // Handle planar face case ...
                    break;
                case CylindricalFace cylindricalFace:
                    // Handle cylindrical face case ...
                    break;
            }

            // Better than specialised individual handlers
            // for each specific case, handle the general 
            // case in a generic fashion.

            Debug.Assert(
                ElementReferenceType.REFERENCE_TYPE_SURFACE
                == r.ElementReferenceType,
                "expected PickObject with ObjectType.Face to "
                + "return a surface reference");

            var face = obj as Face;
            var q = r.UVPoint;
            var p = r.GlobalPoint;

#if DEBUG
            var ir = face.Project(p);
            var q2 = ir.UVPoint;
            Debug.Assert(q.IsAlmostEqualTo(q2),
                "expected same UV point");
#endif // DEBUG

            var t = face.ComputeDerivatives(q);
            var v = t.BasisX; // or BasisY, or whatever...

            return doc.Create.NewFamilyInstance(r, p, v, symbol);
        }

        #endregion // PlaceFamilyInstanceOnFace
    }
}