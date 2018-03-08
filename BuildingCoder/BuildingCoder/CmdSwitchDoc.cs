#region Header
//
// CmdSwitchDoc.cs - switch document or view
//
// Copyright (C) 2018 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
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
  [Transaction( TransactionMode.Manual )]
  class CmdSwitchDoc : IExternalCommand
  {
    /// <summary>
    /// Zoom to the given elements, switching view if needed.
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="message">Error message on failure</param>
    /// <param name="elements">Elements causing failure</param>
    /// <returns></returns>
    Result ZoomToElements( 
      UIDocument uidoc,
      ICollection<ElementId> ids,
      ref string message,
      ElementSet elements )
    {
      int n = ids.Count;

      if( 0 == n )
      {
        message = "Please select at least one element to zoom to.";
        return Result.Cancelled;
      }
      try
      {
        uidoc.ShowElements( ids );
      }
      catch
      {
        Document doc = uidoc.Document;

        foreach( ElementId id in ids )
        {
          Element e = doc.GetElement( id );
          elements.Insert( e );
        }

        message = string.Format( 
          "Cannot zoom to element{0}.",
          1 == n ? "" : "s" );

        return Result.Failed;
      }
      return Result.Succeeded;
    }

    /// <summary>
    /// Toggle back and forth between two different documents
    /// </summary>
    void ToggleViews( 
      View view1, 
      string filepath2 )
    {
      Document doc = view1.Document;
      UIDocument uidoc = new UIDocument( doc );
      Application app = doc.Application;
      UIApplication uiapp = new UIApplication( app );

      // Select some elements in the first document

      ICollection<ElementId> idsView1
        = new FilteredElementCollector( doc, view1.Id )
          .WhereElementIsNotElementType()
          .ToElementIds();

      // Open the second file

      UIDocument uidoc2 = uiapp
        .OpenAndActivateDocument( filepath2 );

      Document doc2 = uidoc2.Document;

      // Do something in second file

      using( Transaction tx = new Transaction( doc2 ) )
      {
        tx.Start( "Change Scale" );
        doc2.ActiveView.get_Parameter(
          BuiltInParameter.VIEW_SCALE_PULLDOWN_METRIC )
            .Set( 20 );
        tx.Commit();
      }

      // Save modified second file

      SaveAsOptions opt = new SaveAsOptions
      {
        OverwriteExistingFile = true
      };

      doc2.SaveAs( filepath2, opt );

      // Switch back to original file;
      // in a new file, doc.PathName is empty

      if( !string.IsNullOrEmpty( doc.PathName ) )
      {
        uiapp.OpenAndActivateDocument(
          doc.PathName );

        doc2.Close( false ); // no problem here, says Remy
      }
      else
      {
        // Avoid using OpenAndActivateDocument

        uidoc.ShowElements( idsView1 );
        uidoc.RefreshActiveView();

        //doc2.Close( false ); // Remy says: Revit throws the exception and doesn't close the file
      }
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
      Result rc = Result.Succeeded;

      ICollection<ElementId> ids
        = uidoc.Selection.GetElementIds();

      bool zoomToPreselectedElements 
        = ( 0 < ids.Count );

      if( zoomToPreselectedElements )
      {
        rc = ZoomToElements( uidoc, ids, 
          ref message, elements );
      }
      else
      {
        string filepath = "C:/test/xyz.rfa";

        ToggleViews( doc.ActiveView, filepath );
      }
      return rc;
    }
  }
}
