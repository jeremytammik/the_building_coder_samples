#region Header
//
// CmdPickPoint3d.cs - set active work plane to pick a point in 3d
//
// Copyright (C) 2011-2020 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
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
  [Transaction( TransactionMode.Manual )]
  public class CmdPickPoint3d : IExternalCommand
  {
    public void PickPointsForArea(
      UIDocument uidoc )
    {
      Document doc = uidoc.Document;
      View view = doc.ActiveView;

      #region COMPLICATED_UNRELIABLE_DOES_NOT_WORK
#if COMPLICATED_UNRELIABLE_DOES_NOT_WORK

using( Transaction t = new Transaction( doc ) )
{
  t.Start( "Set Work Plane" );

  Plane plane = new Plane(
    view.ViewDirection,
    view.Origin );

  //SketchPlane sp = doc.Create.NewSketchPlane( plane );

  SketchPlane sp = SketchPlane.Create( doc, plane );

  view.SketchPlane = sp;
  view.ShowActiveWorkPlane();

  t.Commit();
}

double differenceX;
double differenceY;
double differenceZ;
double area;

XYZ pt1 = uidoc.Selection.PickPoint();
XYZ pt2 = uidoc.Selection.PickPoint();

double pt1x = pt1.X;
double pt1y = pt1.Y;
double pt1z = pt1.Z;

double pt2x = pt2.X;
double pt2y = pt2.Y;
double pt2z = pt2.Z;

bool b;
int caseSwitch = 0;

if( b = ( pt1z == pt2z ) )
{ caseSwitch = 1; }

if( b = ( pt1y == pt2y ) )
{ caseSwitch = 2; }

if( b = ( pt1x == pt2x ) )
{ caseSwitch = 3; }

switch( caseSwitch )
{
  case 1:
    differenceX = pt2x - pt1x;
    differenceY = pt1y - pt2y;
    area = differenceX * differenceY;
    break;

  case 2:
    differenceX = pt2x - pt1x;
    differenceZ = pt1z - pt2z;
    area = differenceX * differenceZ;
    break;

  default:
    differenceY = pt2y - pt1y;
    differenceZ = pt1z - pt2z;
    area = differenceY * differenceZ;
    break;
}
#endif // COMPLICATED_UNRELIABLE_DOES_NOT_WORK
      #endregion // COMPLICATED_UNRELIABLE_DOES_NOT_WORK

      XYZ p1, p2;

      try
      {
        p1 = uidoc.Selection.PickPoint(
          "Please pick first point for area" );

        p2 = uidoc.Selection.PickPoint(
          "Please pick second point for area" );
      }
      catch( Autodesk.Revit.Exceptions.OperationCanceledException )
      {
        return;
      }

      Plane plane = view.SketchPlane.GetPlane();

      UV q1 = plane.ProjectInto( p1 );
      UV q2 = plane.ProjectInto( p2 );
      UV d = q2 - q1;

      double area = d.U * d.V;

      area = Math.Round( area, 2 );

      if( area < 0 )
      {
        area = area * ( -1 );
      }

      TaskDialog.Show( "Area", area.ToString() );
    }

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

      try
      {
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
            //Plane plane = new Plane( face.FaceNormal, face.Origin ); // 2016
            Plane plane = Plane.CreateByNormalAndOrigin( 
              face.FaceNormal, face.Origin ); // 2017

            using( Transaction t = new Transaction( doc ) )
            {
              t.Start( "Temporarily set work plane"
                + " to pick point in 3D" );

              //SketchPlane sp = doc.Create.NewSketchPlane( plane ); // 2013

              SketchPlane sp = SketchPlane.Create( doc, plane ); // 2014

              uidoc.ActiveView.SketchPlane = sp;
              uidoc.ActiveView.ShowActiveWorkPlane();

              point_in_3d = uidoc.Selection.PickPoint(
                "Please pick a point on the plane"
                + " defined by the selected face" );

              t.RollBack();
            }
          }
        }
      }
      catch( Autodesk.Revit.Exceptions.OperationCanceledException )
      {
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

      //PickPointsForArea( uidoc );

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
        message = "3D point selection cancelled or failed";
        return Result.Failed;
      }
    }
  }
}
