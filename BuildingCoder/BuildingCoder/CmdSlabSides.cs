#region Header
//
// CmdSlabSides.cs - determine vertical slab 'side' faces
//
// Copyright (C) 2008-2013 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Automatic )]
  class CmdSlabSides : IExternalCommand
  {
    /// <summary>
    /// Determine the vertical boundary faces
    /// of a given "horizontal" solid object
    /// such as a floor slab. Currently only
    /// supports planar and cylindrical faces.
    /// </summary>
    /// <param name="verticalFaces">Return solid vertical boundary faces, i.e. 'sides'</param>
    /// <param name="solid">Input solid</param>
    void GetSideFaces(
      List<Face> verticalFaces,
      Solid solid )
    {
      FaceArray faces = solid.Faces;
      foreach( Face f in faces )
      {
        if( f is PlanarFace )
        {
          if( Util.IsVertical( f as PlanarFace ) )
          {
            verticalFaces.Add( f );
          }
        }
        if( f is CylindricalFace )
        {
          if( Util.IsVertical( f as CylindricalFace ) )
          {
            verticalFaces.Add( f );
          }
        }
      }
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;

      List<Element> floors = new List<Element>();
      if( !Util.GetSelectedElementsOrAll(
        floors, uidoc, typeof( Floor ) ) )
      {
        Selection sel = uidoc.Selection;
        message = ( 0 < sel.Elements.Size )
          ? "Please select some floor elements."
          : "No floor elements found.";
        return Result.Failed;
      }

      List<Face> faces = new List<Face>();
      Options opt = app.Application.Create.NewGeometryOptions();

      foreach( Floor floor in floors )
      {
        GeometryElement geo = floor.get_Geometry( opt );
        //GeometryObjectArray objects = geo.Objects; // 2012
        //foreach( GeometryObject obj in objects ) // 2012
        foreach( GeometryObject obj in geo ) // 2013
        {
          Solid solid = obj as Solid;
          if( solid != null )
          {
            GetSideFaces( faces, solid );
          }
        }
      }

      int n = faces.Count;

      Debug.Print(
        "{0} side face{1} found.",
        n, Util.PluralSuffix( n ) );

      Creator creator = new Creator( doc );
      foreach( Face f in faces )
      {
        creator.DrawFaceTriangleNormals( f );
      }
      return Result.Succeeded;
    }
  }
}
