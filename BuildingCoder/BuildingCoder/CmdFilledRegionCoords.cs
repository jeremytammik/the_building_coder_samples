#region Header
//
// CmdFilledRegionCoords.cs - retrieve FilledRegion coordinates
//
// Copyright (C) 2011-2013 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdFilledRegionCoords : IExternalCommand
  {
    List<XYZ> GetBoundaryCorners( FilledRegion region )
    {
      List<XYZ> result = new List<XYZ>();

      ElementId id = new ElementId(
        region.Id.IntegerValue - 1 );

      Sketch sketch = region.Document.GetElement(
        id ) as Sketch;

      if( null != sketch )
      {
        CurveArray curves = sketch.Profile.get_Item( 0 );

        if( null != curves )
        {
          foreach( Curve cur in curves )
          {
            XYZ corner = cur.GetEndPoint( 0 );
            result.Add( corner );
          }
        }
      }
      return result;
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Application app = uiapp.Application;
      Document doc = uidoc.Document;

      List<Element> filledRegions
        = new List<Element>();

      if( Util.GetSelectedElementsOrAll(
        filledRegions, uidoc, typeof( FilledRegion ) ) )
      {
        int n = filledRegions.Count;

        string[] results = new string [n];

        int i = 0;

        foreach( FilledRegion region in
          filledRegions.Cast<FilledRegion>() )
        {
          string desc = Util.ElementDescription( region );

          List<XYZ> corners = GetBoundaryCorners(
            region );

          string result = ( null == corners ) ? "failed"
            : string.Join( ", ",
              corners.ConvertAll<string>(
                p => Util.PointString( p ) )
                  .ToArray() );

          results[i++] = string.Format( "{0}: {1}",
            desc, result );
        }
        string s = string.Format(
          "Retrieving corners for {0} filled region{1}{2}",
          n, Util.PluralSuffix( n ), Util.DotOrColon( n ) );

        string t = string.Join( "\r\n", results );

        Util.InfoMsg2( s, t );
      }
      return Result.Succeeded;
    }
  }
}
