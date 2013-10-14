#region Header
//
// CmdBoundingBox.cs - eplore element bounding box
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
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdBoundingBox : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;

      Element e = Util.SelectSingleElement(
        uidoc, "an element" );

      if( null == e )
      {
        message = "No element selected";
        return Result.Failed;
      }

      // trying to call this property returns the
      // compile time error: Property, indexer, or
      // event 'BoundingBox' is not supported by
      // the language; try directly calling
      // accessor method 'get_BoundingBox( View )'

      //BoundingBoxXYZ b = e.BoundingBox[null];

      View v = null;

      BoundingBoxXYZ b = e.get_BoundingBox( v );

      if( null == b )
      {
        v = commandData.View;
        b = e.get_BoundingBox( v );
      }

      if( null == b )
      {
        Util.InfoMsg(
          Util.ElementDescription( e )
          + " has no bounding box." );
      }
      else
      {
        Debug.Assert( b.Transform.IsIdentity,
          "expected identity bounding box transform" );

        string in_view = ( null == v )
          ? "model space"
          : "view " + v.Name;

        Util.InfoMsg( string.Format(
          "Element bounding box of {0} in "
          + "{1} extends from {2} to {3}.",
          Util.ElementDescription( e ),
          in_view,
          Util.PointString( b.Min ),
          Util.PointString( b.Max ) ) );
      }
      return Result.Succeeded;
    }
  }
}
