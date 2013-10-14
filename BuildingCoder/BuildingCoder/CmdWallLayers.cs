#region Header
//
// CmdWallLayers.cs - analyse wall compound
// layer structure and geometry
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
  class CmdWallLayers : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = app.ActiveUIDocument.Document;

      // retrieve selected walls, or all walls,
      // if nothing is selected:

      List<Element> walls = new List<Element>();
      if( !Util.GetSelectedElementsOrAll(
        walls, uidoc, typeof( Wall ) ) )
      {
        Selection sel = uidoc.Selection;
        message = ( 0 < sel.Elements.Size )
          ? "Please select some wall elements."
          : "No wall elements found.";
        return Result.Failed;
      }

      //int i; // 2011
      int n;
      double halfThickness, layerOffset;
      Creator creator = new Creator( doc );
      XYZ lcstart, lcend, v, w, p, q;

      foreach( Wall wall in walls )
      {
        string desc = Util.ElementDescription( wall );

        LocationCurve curve
          = wall.Location as LocationCurve;

        if( null == curve )
        {
          message = desc + ": No wall curve found.";
          return Result.Failed;
        }

        // wall centre line and thickness:

        lcstart = curve.Curve.GetEndPoint( 0 );
        lcend = curve.Curve.GetEndPoint( 1 );
        halfThickness = 0.5 * wall.WallType.Width;
        v = lcend - lcstart;
        v = v.Normalize(); // one foot long
        w = XYZ.BasisZ.CrossProduct( v ).Normalize();
        if( wall.Flipped ) { w = -w; }

        p = lcstart - 2 * v;
        q = lcend + 2 * v;
        creator.CreateModelLine( p, q );

        q = p + halfThickness * w;
        creator.CreateModelLine( p, q );

        // exterior edge

        p = lcstart - v + halfThickness * w;
        q = lcend + v + halfThickness * w;
        creator.CreateModelLine( p, q );

        //CompoundStructure structure = wall.WallType.CompoundStructure; // 2011
        CompoundStructure structure = wall.WallType.GetCompoundStructure(); // 2012

        //CompoundStructureLayerArray layers = structure.Layers; // 2011
        IList<CompoundStructureLayer> layers = structure.GetLayers(); // 2012

        //i = 0; // 2011
        //n = layers.Size; // 2011
        n = layers.Count; // 2012

        Debug.Print(
          "{0} with thickness {1}"
          + " has {2} layer{3}{4}",
          desc,
          Util.MmString( 2 * halfThickness ),
          n, Util.PluralSuffix( n ),
          Util.DotOrColon( n ) );

        if( 0 == n )
        {
          // interior edge
          p = lcstart - v - halfThickness * w;
          q = lcend + v - halfThickness * w;
          creator.CreateModelLine( p, q );
        }
        else
        {
          layerOffset = halfThickness;
          foreach( CompoundStructureLayer layer
            in layers )
          {
            Debug.Print(
              "  Layer {0}: function {1}, "
              + "thickness {2}",
              //++i, // 2011
              layers.IndexOf( layer ), // 2012
              layer.Function,
              Util.MmString( layer.Width ) );

            //layerOffset -= layer.Thickness; // 2011
            layerOffset -= layer.Width; // 2012

            p = lcstart - v + layerOffset * w;
            q = lcend + v + layerOffset * w;
            creator.CreateModelLine( p, q );
          }
        }
      }
      return Result.Succeeded;
    }
  }
}
