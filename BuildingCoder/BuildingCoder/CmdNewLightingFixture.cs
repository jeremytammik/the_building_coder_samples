#region Header
//
// CmdNewLightingFixture.cs - insert new lighting fixture family instance
//
// Copyright (C) 2009-2015 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Automatic )]
  class CmdNewLightingFixture : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Application app = uiapp.Application;
      Document doc = uidoc.Document;

      // Get a lighting fixture family symbol:

      FilteredElementCollector symbols
        = Util.GetElementsOfType( doc,
          typeof( FamilySymbol ),
          BuiltInCategory.OST_LightingFixtures );

      FamilySymbol sym = symbols.FirstElement()
        as FamilySymbol;

      if( null == sym )
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

      Reference r = uidoc.Selection.PickObject( 
        ObjectType.Element,
        "Please select ceiling to host lighting fixture" );

      if( null == r )
      {
        message = "Nothing selected.";
        return Result.Failed;
      }

      // 'Autodesk.Revit.DB.Reference.Element' is
      // obsolete: Property will be removed. Use
      // Document.GetElement(Reference) instead.
      //Element ceiling = r.Element; // 2011

      Element ceiling = doc.GetElement( r ) as Wall; // 2012

      // Get the level 1:

      Level level = Util.GetFirstElementOfTypeNamed(
        doc, typeof( Level ), "Level 1" ) as Level;

      if( null == level )
      {
        message = "Level 1 not found.";
        return Result.Failed;
      }

      // Create the family instance:

      XYZ p = app.Create.NewXYZ( -43, 28, 0 );

      FamilyInstance instLight
        = doc.Create.NewFamilyInstance(
          p, sym, ceiling, level,
          StructuralType.NonStructural );

      return Result.Succeeded;
    }

    #region 
    /// <summary>
    /// Place an instance of the given family symbol
    /// on a selected face of an existing 3D element.
    /// </summary>
    FamilyInstance PlaceFamilyInstanceOnFace(
      UIDocument uidoc,
      FamilySymbol symbol )
    {
      Document doc = uidoc.Document;

      Reference r = uidoc.Selection.PickObject(
        ObjectType.Face, "Please pick a point on "
        + " a face for family instance insertion");

      Element e = doc.GetElement( r.ElementId );

      GeometryObject obj 
        = e.GetGeometryObjectFromReference( r );

      XYZ p = r.GlobalPoint;

      if( obj is PlanarFace )
      {
        PlanarFace planarFace = obj as PlanarFace;

        // Handle planar face case ...
      }
      else if (obj is CylindricalFace)
      {
        CylindricalFace cylindricalFace = obj 
          as CylindricalFace;

        // Handle cylindrical face case ...
      }

      // Better than specialised individual handlers
      // for each specific case, handle the general 
      // case in a generic fashion.

      Face face = obj as Face;
      IntersectionResult ir = face.Project( p );
      UV q = ir.UVPoint;
      Transform t = face.ComputeDerivatives( q );
      XYZ v = t.BasisX; // or BasisY, or whatever...

      return doc.Create.NewFamilyInstance(r, p, v, symbol);
    }
    #endregion
  }
}
