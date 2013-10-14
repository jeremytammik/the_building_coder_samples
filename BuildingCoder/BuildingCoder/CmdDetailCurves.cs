#region Header
//
// CmdDetailCurves.cs - create detail curves
//
// Copyright (C) 2009-2013 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Automatic )]
  class CmdDetailCurves : IExternalCommand
  {
    /// <summary>
    /// Return a point projected onto a plane defined by its normal.
    /// http://www.euclideanspace.com/maths/geometry/elements/plane
    /// Case 1259133 [Curve must be in the plane]
    /// </summary>
    XYZ ProjectPointOntoPlane(
      XYZ point,
      XYZ planeNormal )
    {
      double a = planeNormal.X;
      double b = planeNormal.Y;
      double c = planeNormal.Z;

      double dx = ( b * b + c * c ) * point.X - ( a * b ) * point.Y - ( a * c ) * point.Z;
      double dy = -( b * a ) * point.X + ( a * a + c * c ) * point.Y - ( b * c ) * point.Z;
      double dz = -( c * a ) * point.X - ( c * b ) * point.Y + ( a * a + b * b ) * point.Z;
      return new XYZ( dx, dy, dz );
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      Document doc = app.ActiveUIDocument.Document;
      View view = doc.ActiveView;

      Autodesk.Revit.Creation.Application creApp = app.Application.Create;
      Autodesk.Revit.Creation.Document creDoc = doc.Create;

      // Create a geometry line

      XYZ startPoint = new XYZ( 0, 0, 0 );
      XYZ endPoint = new XYZ( 10, 10, 0 );

      //Line geomLine = creApp.NewLine( startPoint, endPoint, true ); // 2013

      Line geomLine = Line.CreateBound( startPoint, endPoint ); // 2014

      // Create a geometry arc

      XYZ end0 = new XYZ( 0, 0, 0 );
      XYZ end1 = new XYZ( 10, 0, 0 );
      XYZ pointOnCurve = new XYZ( 5, 5, 0 );

      //Arc geomArc = creApp.NewArc( end0, end1, pointOnCurve ); // 2013

      Arc geomArc = Arc.Create( end0, end1, pointOnCurve ); // 2014

#if NEED_PLANE
      // Create a geometry plane

      XYZ origin = new XYZ( 0, 0, 0 );
      XYZ normal = new XYZ( 1, 1, 0 );

      Plane geomPlane = creApp.NewPlane(
        normal, origin );

      // Create a sketch plane in current document

      SketchPlane sketch = creDoc.NewSketchPlane(
        geomPlane );
#endif // NEED_PLANE

      // Create a DetailLine element using the
      // newly created geometry line and sketch plane

      DetailLine line = creDoc.NewDetailCurve(
        view, geomLine ) as DetailLine;

      // Create a DetailArc element using the
      // newly created geometry arc and sketch plane

      DetailArc arc = creDoc.NewDetailCurve(
        view, geomArc ) as DetailArc;

      // Change detail curve colour.
      // Initially, this only affects the newly
      // created curves. However, when the view
      // is refreshed, all detail curves will
      // be updated.

      GraphicsStyle gs = arc.LineStyle as GraphicsStyle;

      gs.GraphicsStyleCategory.LineColor
        = new Color( 250, 10, 10 );

      return Result.Succeeded;
    }
  }
}
