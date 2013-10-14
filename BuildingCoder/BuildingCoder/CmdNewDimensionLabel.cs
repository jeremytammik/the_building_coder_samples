#region Header
//
// CmdNewDimensionLabel.cs - create a new dimension label in a family document
//
// Copyright (C) 2010-2013 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  /// <summary>
  /// Create a new dimension label in a family document.
  /// </summary>
  [Transaction( TransactionMode.Automatic )]
  class CmdNewDimensionLabel : IExternalCommand
  {
    /// <summary>
    /// Return a sketch plane from the given document with
    /// the specified normal vector, if one exists, else null.
    /// </summary>
    static SketchPlane findSketchPlane(
      Document doc,
      XYZ normal )
    {
      FilteredElementCollector collector
        = new FilteredElementCollector( doc );

      collector.OfClass( typeof( SketchPlane ) );

#if EXPLICIT_CODE
      SketchPlane result = null;
      foreach( SketchPlane e in collector )
      {
        if( e.Plane.Normal.IsAlmostEqualTo( normal ) )
        {
          result = e;
          break;
        }
      }
      return result;
#endif // EXPLICIT_CODE

      //Func<SketchPlane, bool> normalEquals = e => e.Plane.Normal.IsAlmostEqualTo( normal ); // 2013

      Func<SketchPlane, bool> normalEquals = e => e.GetPlane().Normal.IsAlmostEqualTo( normal ); // 2014

      return collector.Cast<SketchPlane>().First<SketchPlane>( normalEquals );
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      Document doc = app.ActiveUIDocument.Document;

      if( !doc.IsFamilyDocument )
      {
        message = "Please run this command in afamily document.";
        return Result.Failed;
      }

      Autodesk.Revit.Creation.Application creApp = app.Application.Create;
      Autodesk.Revit.Creation.Document creDoc = doc.Create;

      SketchPlane skplane = findSketchPlane( doc, XYZ.BasisZ );

      if( null == skplane )
      {
        Plane geometryPlane = creApp.NewPlane(
          XYZ.BasisZ, XYZ.Zero );

        //skplane = doc.FamilyCreate.NewSketchPlane( geometryPlane ); // 2013

        skplane = SketchPlane.Create( doc, geometryPlane ); // 2014
      }

      double length = 1.23;

      XYZ start = XYZ.Zero;
      XYZ end = creApp.NewXYZ( 0, length, 0 );

      //Line line = creApp.NewLine( start, end, true ); // 2013

      Line line = Line.CreateBound( start, end ); // 2014

      ModelCurve modelCurve
        = doc.FamilyCreate.NewModelCurve(
          line, skplane );

      ReferenceArray ra = new ReferenceArray();

      ra.Append( modelCurve.GeometryCurve.Reference );

      start = creApp.NewXYZ( length, 0, 0 );
      end = creApp.NewXYZ( length, length, 0 );

      line = Line.CreateBound( start, end );

      modelCurve = doc.FamilyCreate.NewModelCurve(
        line, skplane );

      ra.Append( modelCurve.GeometryCurve.Reference );

      start = creApp.NewXYZ( 0, 0.2 * length, 0 );
      end = creApp.NewXYZ( length, 0.2 * length, 0 );

      line = Line.CreateBound( start, end );

      Dimension dim
        = doc.FamilyCreate.NewLinearDimension(
          doc.ActiveView, line, ra );

      FamilyParameter familyParam
        = doc.FamilyManager.AddParameter(
          "length",
          BuiltInParameterGroup.PG_IDENTITY_DATA,
          ParameterType.Length, false );

      //dim.Label = familyParam; // 2013
      dim.FamilyLabel = familyParam; // 2014

      return Result.Succeeded;
    }
  }
}
