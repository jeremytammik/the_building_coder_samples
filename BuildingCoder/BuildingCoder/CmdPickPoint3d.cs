#region Header
//
// CmdPickPoint3d.cs - set active work plane to pick a point in 3d
//
// Copyright (C) 2011-2013 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  public class CmdPickPoint3d : IExternalCommand
  {
    /// <summary>
    /// Prompt the user to select a face on an element
    /// and then pick a point on that face. The first
    /// picking of the face on the element temporarily
    /// redefines the active work plane, on which the
    /// second point can be picked.
    /// </summary>
    bool PickFaceSetWorkPlaneAndPickPoint(
      UIDocument uidoc,
      out XYZ point_in_3d )
    {
      point_in_3d = null;

      Document doc = uidoc.Document;

      Reference r = uidoc.Selection.PickObject(
        ObjectType.Face,
        "Please select a planar face to define work plane" );

      Element e = doc.GetElement( r.ElementId );

      if( null != e )
      {
        PlanarFace face 
          = e.GetGeometryObjectFromReference( r )
            as PlanarFace;

        if( face != null )
        {
          Plane plane = new Plane(
            face.Normal, face.Origin );

          Transaction t = new Transaction( doc );

          t.Start( "Temporarily set work plane"
            + " to pick point in 3D" );

          //SketchPlane sp = doc.Create.NewSketchPlane( plane ); // 2013

          SketchPlane sp = SketchPlane.Create( doc, plane ); // 2014

          uidoc.ActiveView.SketchPlane = sp;
          uidoc.ActiveView.ShowActiveWorkPlane();

          try
          {
            point_in_3d = uidoc.Selection.PickPoint(
              "Please pick a point on the plane"
              + " defined by the selected face" );
          }
          catch( OperationCanceledException )
          {
          }

          t.RollBack();
        }
      }
      return null != point_in_3d;
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;

      XYZ point_in_3d;

      if( PickFaceSetWorkPlaneAndPickPoint(
        uidoc, out point_in_3d ) )
      {
        TaskDialog.Show( "3D Point Selected",
          "3D point picked on the plane"
          + " defined by the selected face: "
          + Util.PointString( point_in_3d ) );

        return Result.Succeeded;
      }
      else
      {
        message = "3D point selection failed";
        return Result.Failed;
      }
    }
  }
}
