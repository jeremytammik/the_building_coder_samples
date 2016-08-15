#region Header
//
// CmdCropToRoom.cs - set 3D view crop box to room extents
//
// Copyright (C) 2009-2016 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  class CmdCropToRoom : IExternalCommand
  {
    #region SetSectionBox
    /// <summary>
    /// Set 3D view section box to selected element extents.
    /// </summary>
    private void SectionBox(UIDocument uidoc )
    {
      Document doc = uidoc.Document;
      View view = doc.ActiveView;

      double Min_X = double.MaxValue;
      double Min_Y = double.MaxValue;
      double Min_Z = double.MaxValue;

      double Max_X = Min_X;
      double Max_Y = Min_Y;
      double Max_Z = Min_Z;

      ICollection<ElementId> ids 
        = uidoc.Selection.GetElementIds();

      foreach( ElementId id in ids )
      {
        Element elm = doc.GetElement( id );
        BoundingBoxXYZ box = elm.get_BoundingBox( view );
        if( box.Max.X > Max_X )
        {
          Max_X = box.Max.X;
        }
        if( box.Max.Y > Max_Y )
        {
          Max_Y = box.Max.Y;
        }
        if( box.Max.Z > Max_Z )
        {
          Max_Z = box.Max.Z;
        }

        if( box.Min.X < Min_X )
        {
          Min_X = box.Min.X;
        }
        if( box.Min.Y < Min_Y )
        {
          Min_Y = box.Min.Y;
        }
        if( box.Min.Z < Min_Z )
        {
          Min_Z = box.Min.Z;
        }
      }
      XYZ Max = new XYZ( Max_X, Max_Y, Max_Z );
      XYZ Min = new XYZ( Min_X, Min_Y, Min_Z );

      BoundingBoxXYZ myBox = new BoundingBoxXYZ();

      myBox.Min = Min;
      myBox.Max = Max;

      ( view as View3D ).SetSectionBox( myBox );
    }
    #endregion // SetSectionBox


    #region Element in View Crop Box Predicate
    /// <summary>
    /// Return true if element is outside of view crop box
    /// </summary>
    bool IsElementOutsideCropBox( Element e, View v )
    {
      bool rc = v.CropBoxActive;

      if( rc )
      {
        BoundingBoxXYZ vBox = v.CropBox;
        BoundingBoxXYZ eBox = e.get_BoundingBox( v );

        Transform tInv = v.CropBox.Transform.Inverse;
        eBox.Max = tInv.OfPoint( eBox.Max );
        eBox.Min = tInv.OfPoint( eBox.Min );

        rc = ( eBox.Min.X > vBox.Max.X )
          || ( eBox.Max.X < vBox.Min.X )
          || ( eBox.Min.Y > vBox.Max.Y )
          || ( eBox.Max.Y < vBox.Min.Y );
      }
      return rc;
    }
    #endregion // Element in View Crop Box Predicate

    static int _i = -1;

    /// <summary>
    /// Increment and return the current room index.
    /// Every call to this method increments the current room index by one.
    /// If it exceeds the number of rooms in the model, loop back to zero.
    /// </summary>
    /// <param name="room_count">Number of rooms in the model.</param>
    /// <returns>Incremented current room index, looping around to zero when max room count is reached.</returns>
    static int BumpRoomIndex( int room_count )
    {
      ++_i;

      if( _i >= room_count )
      {
        _i = 0;
      }
      return _i;
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      Document doc = app.ActiveUIDocument.Document;
      View3D view3d = doc.ActiveView as View3D;

      if( null == view3d )
      {
        message = "Please activate a 3D view"
          + " before running this command.";

        return Result.Failed;
      }

      using ( Transaction t = new Transaction( doc ) )
      {
        t.Start( "Crop to Room" );

        // get the 3d view crop box:

        BoundingBoxXYZ bb = view3d.CropBox;

        // get the transform from the current view
        // to the 3D model:

        Transform transform = bb.Transform;

        // get the transform from the 3D model
        // to the current view:

        Transform transformInverse = transform.Inverse;

        // get all rooms in the model:

        FilteredElementCollector collector
          = new FilteredElementCollector( doc );

        collector.OfClass( typeof( Room ) );
        IList<Element> rooms = collector.ToElements();
        int n = rooms.Count;

        Room room = ( 0 < n )
          ? rooms[BumpRoomIndex( n )] as Room
          : null;

        if ( null == room )
        {
          message = "No room element found in project.";
          return Result.Failed;
        }

        // Collect all vertices of room closed shell
        // to determine its extents:

        GeometryElement e = room.ClosedShell;
        List<XYZ> vertices = new List<XYZ>();

        //foreach( GeometryObject o in e.Objects ) // 2012

        foreach ( GeometryObject o in e ) // 2013
        {
          if ( o is Solid )
          {
            // Iterate over all the edges of all solids:

            Solid solid = o as Solid;

            foreach ( Edge edge in solid.Edges )
            {
              foreach ( XYZ p in edge.Tessellate() )
              {
                // Collect all vertices,
                // including duplicates:

                vertices.Add( p );
              }
            }
          }
        }

        List<XYZ> verticesIn3dView = new List<XYZ>();

        foreach ( XYZ p in vertices )
        {
          verticesIn3dView.Add(
            transformInverse.OfPoint( p ) );
        }

        // Ignore the Z coorindates and find the
        // min and max X and Y in the 3d view:

        double xMin = 0, yMin = 0, xMax = 0, yMax = 0;

        bool first = true;
        foreach ( XYZ p in verticesIn3dView )
        {
          if ( first )
          {
            xMin = p.X;
            yMin = p.Y;
            xMax = p.X;
            yMax = p.Y;
            first = false;
          }
          else
          {
            if ( xMin > p.X )
              xMin = p.X;
            if ( yMin > p.Y )
              yMin = p.Y;
            if ( xMax < p.X )
              xMax = p.X;
            if ( yMax < p.Y )
              yMax = p.Y;
          }
        }

        // Grow the crop box by one twentieth of its
        // size to include the walls of the room:

        double d = 0.05 * ( xMax - xMin );
        xMin = xMin - d;
        xMax = xMax + d;

        d = 0.05 * ( yMax - yMin );
        yMin = yMin - d;
        yMax = yMax + d;

        bb.Max = new XYZ( xMax, yMax, bb.Max.Z );
        bb.Min = new XYZ( xMin, yMin, bb.Min.Z );

        view3d.CropBox = bb;

        // Change the crop view setting manually or
        // programmatically to see the result:

        view3d.CropBoxActive = true;
        view3d.CropBoxVisible = true;
        t.Commit();
      }
      return Result.Succeeded;
    }
  }
}
