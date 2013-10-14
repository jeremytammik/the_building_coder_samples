#region Header
//
// CmdRotatedBeamLocation.cs - determine location of rotated beam
//
// Copyright (C) 2009-2013 by Jeremy Tammik,
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
#endregion // Namespaces

// C:\a\doc\revit\blog\img\three_beams.png
// C:\a\doc\revit\blog\img\rotated_beam.jpg

namespace BuildingCoder
{
  [Transaction( TransactionMode.Automatic )]
  class CmdRotatedBeamLocation : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;

      FamilyInstance beam = Util.SelectSingleElementOfType(
        uidoc, typeof( FamilyInstance ), "a beam", false ) as FamilyInstance;

      BuiltInCategory bic
        = BuiltInCategory.OST_StructuralFraming;

      if( null == beam
        || null == beam.Category
        || !beam.Category.Id.IntegerValue.Equals( (int) bic ) )
      {
        message = "Please select a single beam element.";
      }
      else
      {
        LocationCurve curve
          = beam.Location as LocationCurve;

        if( null == curve )
        {
          message = "No curve available";
          return Result.Failed;
        }

        XYZ p = curve.Curve.GetEndPoint( 0 );
        XYZ q = curve.Curve.GetEndPoint( 1 );
        XYZ v = 0.1 * (q - p);
        p = p - v;
        q = q + v;

        Creator creator = new Creator( doc );
        creator.CreateModelLine( p, q );
      }
      return Result.Succeeded;
    }
  }
}
