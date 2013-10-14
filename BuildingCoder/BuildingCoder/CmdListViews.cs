#region Header
//
// CmdListViews.cs - determine all the view
// ports of a drawing sheet and vice versa
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

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdListViews : IExternalCommand
  {
    /// <summary>
    /// Return the viewport on the given
    /// sheet displaying the given view.
    /// </summary>
    Element GetViewport( ViewSheet sheet, View view )
    {
      Document doc = sheet.Document;

      // filter for view name:

      BuiltInParameter bip
        = BuiltInParameter.VIEW_NAME;

      ParameterValueProvider provider
        = new ParameterValueProvider(
          new ElementId( bip ) );

      FilterStringRuleEvaluator evaluator
        = new FilterStringEquals();

      FilterRule rule = new FilterStringRule(
        provider, evaluator, view.Name, true );

      ElementParameterFilter name_filter
        = new ElementParameterFilter( rule );

      BuiltInCategory bic
        = BuiltInCategory.OST_Viewports;

      // retrieve the specific named viewport:

      //Element viewport
      //  = new FilteredElementCollector( doc )
      //    .OfCategory( bic )
      //    .WherePasses( name_filter )
      //    .FirstElement();
      //return viewport;

      // unfortunately, there are not just one,
      // but two candidate elements. apparently,
      // we can distibuish them using the
      // owner view id property:

      List<Element> viewports
        = new List<Element>(
          new FilteredElementCollector( doc )
            .OfCategory( bic )
            .WherePasses( name_filter )
            .ToElements() );

      Debug.Assert( viewports[0].OwnerViewId.Equals( ElementId.InvalidElementId ),
        "expected the first viewport to have an invalid owner view id" );

      Debug.Assert( !viewports[1].OwnerViewId.Equals( ElementId.InvalidElementId ),
        "expected the second viewport to have a valid owner view id" );

      int i = 1;

      return viewports[i];
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      Document doc = app.ActiveUIDocument.Document;

      FilteredElementCollector sheets
        = new FilteredElementCollector( doc );

      sheets.OfClass( typeof( ViewSheet ) );

      // map with key = sheet element id and
      // value = list of viewport element ids:

      Dictionary<ElementId, List<ElementId>>
        mapSheetToViewport =
        new Dictionary<ElementId, List<ElementId>>();

      // map with key = viewport element id and
      // value = sheet element id:

      Dictionary<ElementId, ElementId>
        mapViewportToSheet =
        new Dictionary<ElementId, ElementId>();

      foreach( ViewSheet sheet in sheets )
      {
        int n = sheet.Views.Size;

        Debug.Print(
          "Sheet {0} contains {1} view{2}: ",
          Util.ElementDescription( sheet ),
          n, Util.PluralSuffix( n ) );

        ElementId idSheet = sheet.Id;

        int i = 0;

        foreach( View v in sheet.Views )
        {
          BoundingBoxXYZ bb;

          bb = v.get_BoundingBox( doc.ActiveView );

          Debug.Assert( null == bb,
            "expected null view bounding box" );

          bb = v.get_BoundingBox( sheet );

          Debug.Assert( null == bb,
            "expected null view bounding box" );

          Element viewport = GetViewport( sheet, v );

          // null if not in active view:

          bb = viewport.get_BoundingBox( doc.ActiveView );

          BoundingBoxUV outline = v.Outline;

          Debug.WriteLine( string.Format(
            "  {0} {1} bb {2} outline {3}",
            ++i, Util.ElementDescription( v ),
            (null == bb ? "<null>" : Util.BoundingBoxString( bb )),
            Util.BoundingBoxString( outline ) ) );

          if( !mapSheetToViewport.ContainsKey( idSheet ) )
          {
            mapSheetToViewport.Add( idSheet,
              new List<ElementId>() );
          }
          mapSheetToViewport[idSheet].Add( v.Id );

          Debug.Assert(
            !mapViewportToSheet.ContainsKey( v.Id ),
            "expected viewport to be contained"
            + " in only one single sheet" );

          mapViewportToSheet.Add( v.Id, idSheet );
        }
      }
      return Result.Cancelled;
    }
  }
}

// C:\a\j\adn\case\bsd\1266302\attach\rst_basic_sample_project_reinf.rvt